using finaid.Components;
using finaid.Configuration;
using finaid.Data;
using finaid.Data.Extensions;
using finaid.Services;
using finaid.Services.Federal;
using finaid.Services.Eligibility;
using finaid.Services.FAFSA;
using finaid.Services.Background;
using finaid.Services.AI;
using finaid.Services.Forms;
using finaid.Services.Knowledge;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Polly;
using Polly.Extensions.Http;
using FluentValidation;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    
    if (builder.Environment.IsDevelopment())
    {
        // Use SQLite for development
        options.UseSqlite(connectionString);
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
    else
    {
        // Use SQL Server for production
        options.UseSqlServer(connectionString);
    }
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add authentication services (placeholder for future implementation)
builder.Services.AddAuthentication()
    .AddCookie();
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Add application services
builder.Services.AddSingleton<AppStateService>();
builder.Services.AddMemoryCache();

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// Configure Federal API settings
builder.Services.Configure<FederalApiConfiguration>(
    builder.Configuration.GetSection(FederalApiConfiguration.SectionName));

// Configure Eligibility settings
builder.Services.Configure<EligibilitySettings>(
    builder.Configuration.GetSection(EligibilitySettings.SectionName));

// Configure Azure OpenAI settings (required for AI form assistance)
builder.Services.Configure<AzureOpenAISettings>(
    builder.Configuration.GetSection(AzureOpenAISettings.SectionName));

// Add Federal API services
builder.Services.AddSingleton<IRateLimitingService, RateLimitingService>();

// Configure HttpClient with retry policies
builder.Services.AddHttpClient<IFederalApiClient, FederalApiClientService>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

// Add mock service for development
builder.Services.AddScoped<MockFederalApiService>();

// Add eligibility services
builder.Services.AddScoped<EFCCalculator>();
builder.Services.AddScoped<IEligibilityService, EligibilityCalculationService>();

// Add FAFSA services
builder.Services.AddScoped<FAFSAValidationService>();
builder.Services.AddScoped<IFAFSASubmissionService, FAFSASubmissionService>();

// Add background services
builder.Services.AddHostedService<SubmissionStatusUpdateService>();

// Add AI services (required for form assistance)
builder.Services.AddScoped<IAIAssistantService, AzureOpenAIService>();
builder.Services.AddScoped<IKnowledgeService, FinancialAidKnowledgeService>();

// Add Form services
builder.Services.AddScoped<IFormAssistanceService, FormAssistanceService>();

// Register appropriate API client based on configuration
builder.Services.AddScoped<IFederalApiClient>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<FederalApiConfiguration>>();
    
    if (configuration.Value.UseMockService)
    {
        return serviceProvider.GetRequiredService<MockFederalApiService>();
    }
    
    return serviceProvider.GetRequiredService<FederalApiClientService>();
});

// Add SignalR for real-time updates
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Initialize database and seed development data
await app.InitializeDatabaseAsync();

app.Run();

// Policy methods for HTTP client configuration
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => !msg.IsSuccessStatusCode)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Console.WriteLine($"Retry {retryCount} for {context.OperationKey} in {timespan.TotalMilliseconds}ms");
            });
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 3,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (exception, timespan) =>
            {
                // Log circuit breaker opening
            },
            onReset: () =>
            {
                // Log circuit breaker closing
            });
}
