using System.ComponentModel.DataAnnotations;

namespace finaid.Models.Federal;

public class AuthenticationRequest
{
    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    public string ClientSecret { get; set; } = string.Empty;

    [Required]
    public string GrantType { get; set; } = "client_credentials";

    public string Scope { get; set; } = string.Empty;
}

public class AuthenticationResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public string Scope { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime ExpiresAt => IssuedAt.AddSeconds(ExpiresIn);
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt.AddMinutes(-5); // 5-minute buffer
}

public class OAuthAuthorizationRequest
{
    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    public string ResponseType { get; set; } = "code";

    [Required]
    public string Scope { get; set; } = string.Empty;

    [Required]
    [Url]
    public string RedirectUri { get; set; } = string.Empty;

    public string? State { get; set; }
    public string? CodeChallenge { get; set; }
    public string? CodeChallengeMethod { get; set; } = "S256";
}

public class AuthorizationCodeRequest
{
    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    public string ClientSecret { get; set; } = string.Empty;

    [Required]
    public string GrantType { get; set; } = "authorization_code";

    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Url]
    public string RedirectUri { get; set; } = string.Empty;

    public string? CodeVerifier { get; set; }
}