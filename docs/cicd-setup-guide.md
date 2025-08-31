# CI/CD Pipeline Setup Guide

This document provides comprehensive instructions for setting up automated CI/CD pipelines for the FinAid Assistant application using GitHub Actions.

## Overview

The CI/CD pipeline provides:
- Automated building and testing
- Security scanning and code quality analysis
- Automated deployment to staging and production
- Database migration management
- Infrastructure as Code deployment
- Rollback capabilities

## Prerequisites

- GitHub repository with Actions enabled
- Azure service principal with deployment permissions
- SonarCloud account for code quality scanning
- Completed Azure resources setup (see [Azure Setup Guide](./azure-setup-guide.md))

## Repository Structure

Ensure your repository has the following structure:

```
.github/
  workflows/
    ci.yml                 # Continuous Integration
    cd.yml                 # Continuous Deployment  
    infrastructure.yml     # Infrastructure Deployment
scripts/
  deploy.ps1              # Deployment scripts
  test.ps1               # Test scripts
Infrastructure/
  azure-resources.bicep   # Infrastructure as Code
tests/
  HealthChecks/          # Health check endpoints
```

## GitHub Secrets Configuration

### Required Secrets

Add the following secrets to your GitHub repository:

```bash
# Azure Credentials
AZURE_CLIENT_ID=<service-principal-client-id>
AZURE_CLIENT_SECRET=<service-principal-client-secret>
AZURE_SUBSCRIPTION_ID=<azure-subscription-id>
AZURE_TENANT_ID=<azure-tenant-id>

# Application Settings
SQL_CONNECTION_STRING=<production-sql-connection-string>
REDIS_CONNECTION_STRING=<production-redis-connection-string>
STORAGE_CONNECTION_STRING=<production-storage-connection-string>

# Azure AD B2C Settings
B2C_CLIENT_ID=<azure-ad-b2c-client-id>
B2C_CLIENT_SECRET=<azure-ad-b2c-client-secret>
B2C_TENANT_ID=<azure-ad-b2c-tenant-id>

# Code Quality
SONAR_TOKEN=<sonarcloud-token>

# Notification Settings
TEAMS_WEBHOOK_URL=<teams-notification-webhook>
SLACK_WEBHOOK_URL=<slack-notification-webhook>
```

## Continuous Integration Pipeline

Create `.github/workflows/ci.yml`:

```yaml
name: Continuous Integration

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

env:
  DOTNET_VERSION: '8.0.x'
  NODE_VERSION: '20.x'

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      with:
        fetch-depth: 0  # Shallow clones should be disabled for SonarCloud analysis

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}

    - name: Setup Node.js
      uses: actions/setup-node@v4
      with:
        node-version: ${{ env.NODE_VERSION }}

    - name: Cache dependencies
      uses: actions/cache@v3
      with:
        path: |
          ~/.nuget/packages
          ~/.npm
        key: ${{ runner.os }}-deps-${{ hashFiles('**/*.csproj', '**/package-lock.json') }}

    - name: Restore dependencies
      run: dotnet restore

    - name: Install Node dependencies
      run: npm install

    - name: Build application
      run: dotnet build --no-restore --configuration Release

    - name: Run unit tests
      run: dotnet test --no-build --configuration Release --collect:"XPlat Code Coverage"

    - name: Generate test report
      uses: dorny/test-reporter@v1
      if: success() || failure()
      with:
        name: .NET Tests
        path: '**/TestResults/*.trx'
        reporter: dotnet-trx

    - name: Upload coverage reports
      uses: codecov/codecov-action@v3
      with:
        files: '**/coverage.cobertura.xml'
        fail_ci_if_error: true

  security-scan:
    runs-on: ubuntu-latest
    needs: build-and-test
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4

    - name: Run OWASP Dependency Check
      uses: dependency-check/Dependency-Check_Action@main
      with:
        project: 'FinAid-Assistant'
        path: '.'
        format: 'SARIF'
        
    - name: Upload SARIF file
      uses: github/codeql-action/upload-sarif@v2
      with:
        sarif_file: reports/dependency-check-report.sarif

    - name: Run Snyk security scan
      uses: snyk/actions/dotnet@master
      env:
        SNYK_TOKEN: ${{ secrets.SNYK_TOKEN }}
      with:
        args: --severity-threshold=high

  code-quality:
    runs-on: ubuntu-latest
    needs: build-and-test
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      with:
        fetch-depth: 0

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}

    - name: Install SonarCloud scanner
      run: dotnet tool install --global dotnet-sonarscanner

    - name: Run SonarCloud analysis
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
      run: |
        dotnet sonarscanner begin \
          /k:"FinAid-Assistant" \
          /o:"your-organization" \
          /d:sonar.login="${{ secrets.SONAR_TOKEN }}" \
          /d:sonar.host.url="https://sonarcloud.io" \
          /d:sonar.cs.opencover.reportsPaths="**/coverage.opencover.xml"
        
        dotnet build --configuration Release
        dotnet test --configuration Release --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
        
        dotnet sonarscanner end /d:sonar.login="${{ secrets.SONAR_TOKEN }}"

  publish-artifacts:
    runs-on: ubuntu-latest
    needs: [build-and-test, security-scan, code-quality]
    if: github.ref == 'refs/heads/main'
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}

    - name: Publish application
      run: dotnet publish --configuration Release --output ./publish

    - name: Upload build artifacts
      uses: actions/upload-artifact@v3
      with:
        name: webapp-artifacts
        path: ./publish/
        retention-days: 30

    - name: Upload database scripts
      uses: actions/upload-artifact@v3
      with:
        name: database-scripts
        path: |
          ./scripts/
          ./Migrations/
        retention-days: 30
```

## Continuous Deployment Pipeline

Create `.github/workflows/cd.yml`:

```yaml
name: Continuous Deployment

on:
  workflow_run:
    workflows: ["Continuous Integration"]
    types:
      - completed
    branches: [main]

env:
  AZURE_WEBAPP_NAME: 'app-finaid-prod'
  AZURE_WEBAPP_STAGING_NAME: 'app-finaid-staging'
  RESOURCE_GROUP: 'rg-finaid-prod'

jobs:
  deploy-staging:
    runs-on: ubuntu-latest
    if: ${{ github.event.workflow_run.conclusion == 'success' }}
    environment:
      name: staging
      url: https://app-finaid-staging.azurewebsites.net
    
    steps:
    - name: Download artifacts
      uses: actions/download-artifact@v3
      with:
        name: webapp-artifacts
        path: ./artifacts

    - name: Azure Login
      uses: azure/login@v1
      with:
        creds: ${{ secrets.AZURE_CREDENTIALS }}

    - name: Deploy to staging slot
      uses: azure/webapps-deploy@v2
      with:
        app-name: ${{ env.AZURE_WEBAPP_NAME }}
        slot-name: 'staging'
        package: ./artifacts

    - name: Run database migrations (staging)
      run: |
        # Install Entity Framework tool
        dotnet tool install --global dotnet-ef
        
        # Run migrations
        dotnet ef database update --connection "${{ secrets.SQL_CONNECTION_STRING_STAGING }}"

    - name: Run smoke tests
      run: |
        # Wait for deployment to complete
        sleep 30
        
        # Run health checks
        curl -f https://app-finaid-staging.azurewebsites.net/health || exit 1
        
        # Run basic functionality tests
        npm run test:e2e:staging

    - name: Notify deployment status
      if: always()
      uses: 8398a7/action-slack@v3
      with:
        status: ${{ job.status }}
        channel: '#deployments'
        webhook_url: ${{ secrets.SLACK_WEBHOOK_URL }}

  deploy-production:
    runs-on: ubuntu-latest
    needs: deploy-staging
    environment:
      name: production
      url: https://app-finaid-prod.azurewebsites.net
    
    steps:
    - name: Download artifacts
      uses: actions/download-artifact@v3
      with:
        name: webapp-artifacts
        path: ./artifacts

    - name: Azure Login
      uses: azure/login@v1
      with:
        creds: ${{ secrets.AZURE_CREDENTIALS }}

    - name: Create production backup
      run: |
        # Backup current production database
        az sql db export \
          --resource-group ${{ env.RESOURCE_GROUP }} \
          --server sql-finaid-prod \
          --name sqldb-finaid-prod \
          --admin-user ${{ secrets.SQL_ADMIN_USER }} \
          --admin-password ${{ secrets.SQL_ADMIN_PASSWORD }} \
          --storage-key ${{ secrets.STORAGE_KEY }} \
          --storage-key-type StorageAccessKey \
          --storage-uri "https://stfinaidprod.blob.core.windows.net/backups/prod-backup-$(date +%Y%m%d%H%M%S).bacpac"

    - name: Deploy to production
      uses: azure/webapps-deploy@v2
      with:
        app-name: ${{ env.AZURE_WEBAPP_NAME }}
        package: ./artifacts

    - name: Run database migrations (production)
      run: |
        dotnet ef database update --connection "${{ secrets.SQL_CONNECTION_STRING }}"

    - name: Verify deployment
      run: |
        # Health check
        curl -f https://app-finaid-prod.azurewebsites.net/health || exit 1
        
        # Verify key endpoints
        curl -f https://app-finaid-prod.azurewebsites.net/ || exit 1

    - name: Create release
      uses: actions/create-release@v1
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
      with:
        tag_name: v${{ github.run_number }}
        release_name: Release v${{ github.run_number }}
        body: |
          Automated release from commit ${{ github.sha }}
          
          Changes: ${{ github.event.workflow_run.head_commit.message }}
        draft: false
        prerelease: false

    - name: Notify production deployment
      uses: 8398a7/action-slack@v3
      with:
        status: success
        channel: '#deployments'
        message: '🚀 Production deployment successful! Version: v${{ github.run_number }}'
        webhook_url: ${{ secrets.SLACK_WEBHOOK_URL }}

  rollback:
    runs-on: ubuntu-latest
    needs: deploy-production
    if: failure()
    
    steps:
    - name: Azure Login
      uses: azure/login@v1
      with:
        creds: ${{ secrets.AZURE_CREDENTIALS }}

    - name: Rollback deployment
      run: |
        # Swap staging slot back to production
        az webapp deployment slot swap \
          --resource-group ${{ env.RESOURCE_GROUP }} \
          --name ${{ env.AZURE_WEBAPP_NAME }} \
          --slot staging \
          --target-slot production

    - name: Notify rollback
      uses: 8398a7/action-slack@v3
      with:
        status: failure
        channel: '#deployments'
        message: '🔄 Production deployment failed - automatic rollback initiated'
        webhook_url: ${{ secrets.SLACK_WEBHOOK_URL }}
```

## Infrastructure Deployment Pipeline

Create `.github/workflows/infrastructure.yml`:

```yaml
name: Infrastructure Deployment

on:
  push:
    paths:
      - 'Infrastructure/**'
      - '.github/workflows/infrastructure.yml'
    branches: [main]
  workflow_dispatch:

env:
  LOCATION: 'eastus'
  RESOURCE_GROUP: 'rg-finaid-prod'

jobs:
  validate-infrastructure:
    runs-on: ubuntu-latest
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4

    - name: Azure Login
      uses: azure/login@v1
      with:
        creds: ${{ secrets.AZURE_CREDENTIALS }}

    - name: Validate Bicep templates
      run: |
        az bicep build --file Infrastructure/azure-resources.bicep
        
    - name: Run What-If analysis
      run: |
        az deployment group what-if \
          --resource-group ${{ env.RESOURCE_GROUP }} \
          --template-file Infrastructure/azure-resources.bicep \
          --parameters @Infrastructure/parameters/prod.json

  deploy-infrastructure:
    runs-on: ubuntu-latest
    needs: validate-infrastructure
    environment:
      name: production-infrastructure
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4

    - name: Azure Login
      uses: azure/login@v1
      with:
        creds: ${{ secrets.AZURE_CREDENTIALS }}

    - name: Deploy infrastructure
      run: |
        az deployment group create \
          --resource-group ${{ env.RESOURCE_GROUP }} \
          --template-file Infrastructure/azure-resources.bicep \
          --parameters @Infrastructure/parameters/prod.json \
          --mode Incremental

    - name: Update Key Vault secrets
      run: |
        # Update connection strings and secrets
        ./scripts/update-keyvault-secrets.sh

    - name: Restart web applications
      run: |
        az webapp restart \
          --name ${{ env.AZURE_WEBAPP_NAME }} \
          --resource-group ${{ env.RESOURCE_GROUP }}
```

## Deployment Scripts

Create `scripts/deploy.ps1`:

```powershell
param(
    [Parameter(Mandatory=$true)]
    [string]$Environment,
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroup,
    [Parameter(Mandatory=$true)]
    [string]$WebAppName,
    [string]$ConnectionString
)

# Function to check deployment health
function Test-DeploymentHealth {
    param($Url)
    
    $maxRetries = 10
    $retryCount = 0
    
    do {
        try {
            $response = Invoke-WebRequest -Uri "$Url/health" -TimeoutSec 30
            if ($response.StatusCode -eq 200) {
                Write-Host "Health check passed" -ForegroundColor Green
                return $true
            }
        }
        catch {
            Write-Host "Health check failed (attempt $($retryCount + 1)): $($_.Exception.Message)" -ForegroundColor Yellow
        }
        
        $retryCount++
        Start-Sleep -Seconds 30
    } while ($retryCount -lt $maxRetries)
    
    return $false
}

# Main deployment logic
Write-Host "Starting deployment to $Environment environment" -ForegroundColor Blue

try {
    # Run database migrations
    Write-Host "Running database migrations..." -ForegroundColor Yellow
    dotnet ef database update --connection $ConnectionString
    
    # Deploy application
    Write-Host "Deploying application..." -ForegroundColor Yellow
    az webapp deployment source config-zip --resource-group $ResourceGroup --name $WebAppName --src ./publish.zip
    
    # Verify deployment
    $siteUrl = "https://$WebAppName.azurewebsites.net"
    Write-Host "Verifying deployment at $siteUrl" -ForegroundColor Yellow
    
    if (Test-DeploymentHealth -Url $siteUrl) {
        Write-Host "Deployment successful!" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "Deployment verification failed!" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "Deployment failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
```

## Health Check Implementation

Create `HealthChecks/DatabaseHealthCheck.cs`:

```csharp
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;

    public DatabaseHealthCheck(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Database.CanConnectAsync(cancellationToken);
            
            var userCount = await _context.Users.CountAsync(cancellationToken);
            
            return HealthCheckResult.Healthy($"Database is healthy. Users: {userCount}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Database health check failed: {ex.Message}");
        }
    }
}
```

Register health checks in `Program.cs`:

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddApplicationInsightsPublisher();

// Add health check endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

## Monitoring and Alerting

### Application Insights Integration

```csharp
// In Program.cs
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("ApplicationInsights");
});

// Custom telemetry
public class DeploymentTelemetry
{
    private readonly TelemetryClient _telemetryClient;

    public void TrackDeployment(string version, bool success)
    {
        _telemetryClient.TrackEvent("Deployment", new Dictionary<string, string>
        {
            ["Version"] = version,
            ["Success"] = success.ToString(),
            ["Environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            ["Timestamp"] = DateTime.UtcNow.ToString()
        });
    }
}
```

### Azure Monitor Alerts

Set up alerts for:
- Failed deployments
- Application errors
- Performance degradation
- Health check failures

## Security Scanning

### GitHub Security

Enable the following GitHub security features:
- Dependabot alerts
- Code scanning alerts
- Secret scanning
- Dependency review

### Custom Security Checks

```yaml
# Add to CI pipeline
- name: Run custom security checks
  run: |
    # Check for hardcoded secrets
    git log --all --grep="password\|secret\|key" --oneline | head -20
    
    # Scan for common vulnerabilities
    npm audit --audit-level moderate
    
    # Check SSL configuration
    nmap --script ssl-enum-ciphers -p 443 ${{ env.AZURE_WEBAPP_NAME }}.azurewebsites.net
```

## Performance Testing

Integrate performance testing into the pipeline:

```yaml
performance-test:
  runs-on: ubuntu-latest
  needs: deploy-staging
  
  steps:
  - name: Run load tests
    run: |
      # Install k6
      sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
      echo "deb https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
      sudo apt-get update
      sudo apt-get install k6
      
      # Run load tests
      k6 run tests/performance/load-test.js
```

## Troubleshooting

### Common Issues

1. **Deployment Failures**
   - Check application logs
   - Verify connection strings
   - Test health endpoints

2. **Database Migration Issues**
   - Review migration scripts
   - Check database permissions
   - Verify connection strings

3. **Authentication Problems**
   - Validate Azure AD B2C configuration
   - Check redirect URIs
   - Verify secrets in Key Vault

### Debugging Pipeline Issues

```bash
# Enable debug logging
az config set core.collect_telemetry=no
az config set core.output=table
az config set logging.enable_log_file=yes
```

## Maintenance

### Regular Tasks

1. **Weekly**
   - Review security alerts
   - Update dependencies
   - Check performance metrics

2. **Monthly**
   - Rotate secrets and certificates
   - Review and optimize resource costs
   - Update infrastructure templates

3. **Quarterly**
   - Security assessment
   - Disaster recovery testing
   - Performance baseline review

This CI/CD pipeline provides a robust foundation for automated deployment while maintaining security, quality, and reliability standards for the FinAid Assistant application.