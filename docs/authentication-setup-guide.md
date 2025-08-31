# Azure AD B2C Authentication Setup Guide

This document provides comprehensive instructions for implementing Azure AD B2C authentication in the FinAid Assistant application.

## Overview

The FinAid Assistant uses Azure Active Directory B2C (Azure AD B2C) for user authentication and identity management. This provides:

- Secure user registration and sign-in
- Social identity provider integration
- Multi-factor authentication (MFA)
- Password reset and profile management
- Claims-based authorization
- FERPA-compliant identity handling

## Prerequisites

- Azure subscription with Azure AD B2C enabled
- Completed Azure resources setup (see [Azure Setup Guide](./azure-setup-guide.md))
- Application deployed to Azure App Service

## Azure AD B2C Tenant Setup

### 1. Create Azure AD B2C Tenant

```bash
# Create B2C tenant (must be done through Azure Portal)
# Navigate to: Azure Portal > Create a resource > Identity > Azure Active Directory B2C
```

**Tenant Configuration:**
- **Tenant Name**: finaidassistant
- **Initial Domain**: finaidassistant.onmicrosoft.com
- **Country**: United States
- **Data Residency**: United States

### 2. Register Application

Navigate to Azure AD B2C > App registrations > New registration

**Application Settings:**
- **Name**: FinAid Assistant Web App
- **Supported Account Types**: Accounts in any identity provider or organizational directory
- **Redirect URI**: `https://app-finaid-prod.azurewebsites.net/signin-oidc`
- **Front-channel logout URL**: `https://app-finaid-prod.azurewebsites.net/signout-oidc`

**Additional Configuration:**
- Enable ID tokens
- Enable Access tokens
- Configure implicit grant flow

### 3. Configure API Permissions

**Required Permissions:**
- `openid` (Sign users in)
- `offline_access` (Maintain access to data)
- `https://graph.microsoft.com/User.Read` (Read user profile)

## User Flows Configuration

### 1. Sign Up and Sign In Flow

**Flow Name**: `B2C_1_signupsignin`

**Identity Providers:**
- Local Account (Email)
- Google (optional)
- Facebook (optional)

**User Attributes to Collect:**
- Given Name (required)
- Surname (required)
- Email Address (required)
- Date of Birth (required)
- Phone Number (optional)
- Street Address (optional)
- City (optional)
- State/Province (optional)
- Postal Code (optional)

**Claims to Return:**
- Given Name
- Surname
- Email Address
- Object ID
- Date of Birth
- Phone Number (if provided)
- Address fields (if provided)

### 2. Password Reset Flow

**Flow Name**: `B2C_1_passwordreset`

**Configuration:**
- Email verification required
- Password complexity requirements
- Account lockout policy

### 3. Profile Editing Flow

**Flow Name**: `B2C_1_profileediting`

**Editable Attributes:**
- Given Name
- Surname
- Phone Number
- Address fields
- Communication preferences

## Social Identity Providers

### Google Configuration

1. **Google Developers Console Setup:**
   - Create OAuth 2.0 credentials
   - Configure authorized redirect URIs
   - Note Client ID and Client Secret

2. **Azure AD B2C Configuration:**
   ```
   Provider: Google
   Client ID: [Google OAuth Client ID]
   Client Secret: [Google OAuth Client Secret]
   Redirect URI: https://finaidassistant.b2clogin.com/finaidassistant.onmicrosoft.com/oauth2/authresp
   ```

### Facebook Configuration (Optional)

1. **Facebook Developers Setup:**
   - Create Facebook app
   - Configure OAuth settings
   - Note App ID and App Secret

2. **Azure AD B2C Configuration:**
   ```
   Provider: Facebook
   Client ID: [Facebook App ID]
   Client Secret: [Facebook App Secret]
   ```

## Application Integration

### 1. NuGet Packages

Add required authentication packages:

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.OpenIdConnect" Version="8.0.0" />
<PackageReference Include="Microsoft.Identity.Web" Version="2.16.0" />
<PackageReference Include="Microsoft.Graph" Version="5.36.0" />
```

### 2. Configuration Settings

Update `appsettings.Production.json`:

```json
{
  "AzureAdB2C": {
    "Instance": "https://finaidassistant.b2clogin.com/",
    "Domain": "finaidassistant.onmicrosoft.com",
    "ClientId": "@Microsoft.KeyVault(SecretUri=https://kv-finaid-prod.vault.azure.net/secrets/b2c-client-id/)",
    "ClientSecret": "@Microsoft.KeyVault(SecretUri=https://kv-finaid-prod.vault.azure.net/secrets/b2c-client-secret/)",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-oidc",
    "SignUpSignInPolicyId": "B2C_1_signupsignin",
    "ResetPasswordPolicyId": "B2C_1_passwordreset",
    "EditProfilePolicyId": "B2C_1_profileediting"
  }
}
```

### 3. Program.cs Configuration

Replace the placeholder authentication setup:

```csharp
// Remove placeholder authentication
// builder.Services.AddAuthentication()
//     .AddCookie();

// Add Azure AD B2C authentication
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.Bind("AzureAdB2C", options);
        
        // Configure additional options
        options.Events = new OpenIdConnectEvents
        {
            OnTokenValidated = async context =>
            {
                // Sync user profile with local database
                var userService = context.HttpContext.RequestServices
                    .GetRequiredService<IUserService>();
                await userService.SyncUserProfileAsync(context.Principal);
            },
            OnRedirectToIdentityProvider = context =>
            {
                // Customize the redirect based on the action
                if (context.Properties.Items.ContainsKey("policy"))
                {
                    context.ProtocolMessage.SetParameter("p", 
                        context.Properties.Items["policy"]);
                }
                return Task.CompletedTask;
            }
        };
    })
    .EnableTokenAcquisitionToCallDownstreamApi(options => 
        builder.Configuration.Bind("AzureAdB2C", options))
    .AddInMemoryTokenCaches();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAuthenticatedUser", policy =>
        policy.RequireAuthenticatedUser());
});
```

### 4. User Service Implementation

Create `Services/UserService.cs`:

```csharp
public interface IUserService
{
    Task<User> SyncUserProfileAsync(ClaimsPrincipal principal);
    Task<User?> GetCurrentUserAsync();
    Task UpdateUserProfileAsync(User user);
    Task<bool> IsFirstTimeUserAsync(string userId);
}

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<UserService> _logger;

    public UserService(
        ApplicationDbContext context, 
        IHttpContextAccessor httpContextAccessor,
        ILogger<UserService> logger)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<User> SyncUserProfileAsync(ClaimsPrincipal principal)
    {
        var userId = principal.GetObjectId();
        var email = principal.GetDisplayName() ?? principal.Identity?.Name;
        
        var user = await _context.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id.ToString() == userId);

        if (user == null)
        {
            user = new User
            {
                Id = Guid.Parse(userId),
                Email = email ?? throw new InvalidOperationException("Email is required"),
                FirstName = principal.GetGivenName() ?? "",
                LastName = principal.GetSurname() ?? "",
                DateOfBirth = DateTime.TryParse(principal.GetDateOfBirth(), out var dob) 
                    ? dob : DateTime.MinValue,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
        }
        else
        {
            // Update user information from claims
            user.Email = email ?? user.Email;
            user.FirstName = principal.GetGivenName() ?? user.FirstName;
            user.LastName = principal.GetSurname() ?? user.LastName;
            user.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return user;
    }

    // Additional methods...
}
```

### 5. Update Login Components

Update `Components/Layout/LoginDisplay.razor`:

```razor
@using Microsoft.AspNetCore.Components.Authorization
@using Microsoft.Identity.Web
@inject IConfiguration Configuration
@inject NavigationManager Navigation

<AuthorizeView>
    <Authorized>
        <div class="d-flex align-items-center">
            <div class="me-3">
                <span class="text-muted">Welcome,</span>
                <strong>@context.User.GetDisplayName()</strong>
            </div>
            <div class="btn-group">
                <button class="btn btn-outline-primary btn-sm" @onclick="EditProfile">
                    <span class="bi bi-person-gear me-1"></span>
                    Profile
                </button>
                <button class="btn btn-outline-danger btn-sm" @onclick="SignOut">
                    <span class="bi bi-box-arrow-right me-1"></span>
                    Sign Out
                </button>
            </div>
        </div>
    </Authorized>
    <NotAuthorized>
        <button class="btn btn-primary" @onclick="SignIn">
            <span class="bi bi-person-check me-1"></span>
            Sign In
        </button>
    </NotAuthorized>
</AuthorizeView>

@code {
    private void SignIn()
    {
        var returnUrl = Uri.EscapeDataString(Navigation.Uri);
        Navigation.NavigateTo($"MicrosoftIdentity/Account/SignIn?returnUrl={returnUrl}", true);
    }

    private void SignOut()
    {
        Navigation.NavigateTo("MicrosoftIdentity/Account/SignOut", true);
    }

    private void EditProfile()
    {
        var returnUrl = Uri.EscapeDataString(Navigation.Uri);
        var editProfileUrl = $"MicrosoftIdentity/Account/EditProfile?returnUrl={returnUrl}";
        Navigation.NavigateTo(editProfileUrl, true);
    }
}
```

### 6. Authorization Policies

Add role-based and claims-based authorization:

```csharp
// In Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAuthenticatedUser", policy =>
        policy.RequireAuthenticatedUser());
    
    options.AddPolicy("RequireStudent", policy =>
        policy.RequireClaim("extension_UserType", "Student"));
    
    options.AddPolicy("RequireParent", policy =>
        policy.RequireClaim("extension_UserType", "Parent"));
    
    options.AddPolicy("RequireCompletedProfile", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("extension_ProfileCompleted", "true")));
});
```

## Multi-Factor Authentication

### 1. Enable MFA in User Flows

In Azure AD B2C User Flow settings:
- Enable multi-factor authentication
- Configure phone/email verification
- Set MFA requirements (Always, Per User, Conditional)

### 2. Conditional Access (Premium Feature)

Configure conditional access policies based on:
- Risk-based authentication
- Device compliance
- Location-based access
- Application sensitivity

## Claims Transformation

Create custom claims transformation:

```csharp
public class CustomClaimsTransformation : IClaimsTransformation
{
    private readonly ApplicationDbContext _context;

    public CustomClaimsTransformation(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated == true)
        {
            var userId = principal.GetObjectId();
            var user = await _context.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Id.ToString() == userId);

            if (user != null)
            {
                var claimsIdentity = (ClaimsIdentity)principal.Identity;
                
                // Add custom claims
                claimsIdentity.AddClaim(new Claim("profile_completed", 
                    user.Profile?.OnboardingCompleted.ToString().ToLower() ?? "false"));
                
                claimsIdentity.AddClaim(new Claim("first_time_user", 
                    (!user.Profile?.OnboardingCompleted ?? true).ToString().ToLower()));
            }
        }

        return principal;
    }
}

// Register in Program.cs
builder.Services.AddScoped<IClaimsTransformation, CustomClaimsTransformation>();
```

## Security Best Practices

### 1. Token Management

- Enable token refresh
- Configure appropriate token lifetimes
- Implement secure token storage
- Handle token expiration gracefully

### 2. Session Management

```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});
```

### 3. HTTPS Enforcement

```csharp
builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
    options.HttpsPort = 443;
});
```

## Testing Authentication

### 1. Unit Tests

```csharp
[Test]
public async Task SyncUserProfile_NewUser_CreatesUserRecord()
{
    // Arrange
    var claims = new List<Claim>
    {
        new(ClaimConstants.ObjectId, "test-user-id"),
        new(ClaimConstants.GivenName, "John"),
        new(ClaimConstants.Surname, "Doe"),
        new(ClaimConstants.Emails, "john.doe@example.com")
    };
    var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

    // Act
    var user = await _userService.SyncUserProfileAsync(principal);

    // Assert
    Assert.NotNull(user);
    Assert.Equal("John", user.FirstName);
    Assert.Equal("Doe", user.LastName);
}
```

### 2. Integration Tests

Test authentication flows with test users in Azure AD B2C.

## FERPA Compliance

### 1. Data Privacy

- Configure data retention policies
- Implement data export functionality
- Enable user data deletion
- Audit trail for data access

### 2. Consent Management

- Implement privacy policy acceptance
- Track consent changes
- Provide consent withdrawal options

## Monitoring and Logging

### 1. Authentication Events

```csharp
services.AddApplicationInsightsTelemetry();

// Custom telemetry
public void TrackAuthenticationEvent(string userId, string eventName)
{
    _telemetryClient.TrackEvent("Authentication", new Dictionary<string, string>
    {
        ["UserId"] = userId,
        ["Event"] = eventName,
        ["Timestamp"] = DateTime.UtcNow.ToString()
    });
}
```

### 2. Security Monitoring

- Failed authentication attempts
- Unusual login patterns
- Token expiration events
- MFA challenges

## Troubleshooting

### Common Issues

1. **Redirect URI Mismatch**
   - Verify redirect URIs in app registration
   - Check HTTPS configuration
   - Validate domain settings

2. **Claims Not Available**
   - Review user flow configuration
   - Check claim mapping
   - Verify token configuration

3. **Session Timeout Issues**
   - Review token lifetime settings
   - Check cookie configuration
   - Validate refresh token handling

## Next Steps

After completing authentication setup:

1. Test all authentication flows
2. Implement user profile synchronization
3. Set up authorization policies
4. Configure monitoring and alerting
5. Perform security testing

For CI/CD pipeline setup that includes authentication configuration, see [CI/CD Pipeline Setup Guide](./cicd-setup-guide.md).