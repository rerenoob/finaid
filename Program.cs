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
using finaid.Services.OCR;
using finaid.Services.Storage;
using finaid.BackgroundServices;
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
builder.Services.AddControllers();

// Add authentication services (placeholder for future implementation)
builder.Services.AddAuthentication()
    .AddCookie();
builder.Services.AddAuthorization();

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
builder.Services.AddScoped<IKnowledgeService, FinancialAidKnowledgeService>();

// Add mock AI service for development
builder.Services.AddScoped<finaid.Services.AI.MockAIAssistantService>();

// Register appropriate AI client based on configuration
builder.Services.AddScoped<IAIAssistantService>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AzureOpenAISettings>>();
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    
    // Check if Azure OpenAI is properly configured
    var settings = configuration.Value;
    if (string.IsNullOrWhiteSpace(settings.Endpoint) || 
        settings.Endpoint.Contains("your-openai-resource") ||
        string.IsNullOrWhiteSpace(settings.ApiKey) || 
        settings.ApiKey.Contains("your-api-key") ||
        string.IsNullOrWhiteSpace(settings.DeploymentName))
    {
        logger.LogWarning("Azure OpenAI not properly configured, using mock service for development");
        return serviceProvider.GetRequiredService<finaid.Services.AI.MockAIAssistantService>();
    }
    
    return new AzureOpenAIService(configuration, serviceProvider.GetRequiredService<ILogger<AzureOpenAIService>>());
});

// Add Form services
builder.Services.AddScoped<IFormAssistanceService, FormAssistanceService>();

// Configure Document Storage settings
builder.Services.Configure<finaid.Configuration.DocumentStorageSettings>(
    builder.Configuration.GetSection(finaid.Configuration.DocumentStorageSettings.SectionName));

// Add Azure Blob Storage client
builder.Services.AddSingleton(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("AzureStorage");
    
    if (string.IsNullOrEmpty(connectionString))
    {
        // Use development storage emulator if no connection string is configured
        connectionString = "UseDevelopmentStorage=true";
    }
    
    return new Azure.Storage.Blobs.BlobServiceClient(connectionString);
});

// Add Document Storage services
builder.Services.AddScoped<finaid.Services.Storage.IDocumentStorageService, finaid.Services.Storage.DocumentStorageService>();
builder.Services.AddScoped<finaid.Services.Security.IVirusScanningService, finaid.Services.Security.VirusScanningService>();

// Configure Form Recognizer settings
builder.Services.Configure<FormRecognizerSettings>(
    builder.Configuration.GetSection(FormRecognizerSettings.SectionName));

// Add Azure Form Recognizer client
builder.Services.AddSingleton(provider =>
{
    var configuration = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<FormRecognizerSettings>>();
    var settings = configuration.Value;
    
    if (string.IsNullOrEmpty(settings.Endpoint) || string.IsNullOrEmpty(settings.ApiKey))
    {
        throw new InvalidOperationException("Form Recognizer endpoint and API key must be configured");
    }
    
    return new Azure.AI.FormRecognizer.DocumentAnalysis.DocumentAnalysisClient(
        new Uri(settings.Endpoint), 
        new Azure.AzureKeyCredential(settings.ApiKey));
});

// Add OCR services
builder.Services.AddScoped<finaid.Services.OCR.IOCRService, finaid.Services.OCR.FormRecognizerService>();
builder.Services.AddScoped<finaid.Services.OCR.DocumentClassificationService>();

// Add background OCR processing service
builder.Services.AddHostedService<finaid.BackgroundServices.OCRProcessingService>();



// Add Dashboard services
builder.Services.AddScoped<finaid.Services.Dashboard.IDashboardDataService, finaid.Services.Dashboard.DashboardDataService>();
builder.Services.AddScoped<finaid.Services.Progress.ProgressCalculationService>();



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

// Add conversation context service with fallback implementation
// For development, we'll create a mock service that doesn't require Redis
builder.Services.AddScoped<finaid.Services.AI.IConversationContextService, finaid.Services.AI.MockConversationContextService>();

// Add CORS for React frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();



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
