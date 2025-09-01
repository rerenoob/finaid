#!/usr/bin/env pwsh

# Simple dashboard verification script
Write-Host "Testing Dashboard Implementation..."

# Check if DashboardLayout component exists
if (Test-Path "Components/Dashboard/DashboardLayout.razor") {
    Write-Host "✓ DashboardLayout component found" -ForegroundColor Green
} else {
    Write-Host "✗ DashboardLayout component missing" -ForegroundColor Red
    exit 1
}

# Check if Dashboard.razor page exists
if (Test-Path "Components/Pages/Dashboard.razor") {
    Write-Host "✓ Dashboard page found" -ForegroundColor Green
} else {
    Write-Host "✗ Dashboard page missing" -ForegroundColor Red
    exit 1
}

# Check if required dashboard components exist
$requiredComponents = @(
    "ProgressBar.razor",
    "StepIndicator.razor", 
    "DeadlineList.razor",
    "ActivityFeed.razor",
    "QuickActions.razor",
    "NotificationCenter.razor",
    "RealTimeConnectionStatus.razor"
)

foreach ($component in $requiredComponents) {
    if (Test-Path "Components/Dashboard/$component") {
        Write-Host "✓ $component found" -ForegroundColor Green
    } else {
        Write-Host "✗ $component missing" -ForegroundColor Red
        exit 1
    }
}

# Check if DashboardDataService exists
if (Test-Path "Services/Dashboard/DashboardDataService.cs") {
    Write-Host "✓ DashboardDataService found" -ForegroundColor Green
} else {
    Write-Host "✗ DashboardDataService missing" -ForegroundColor Red
    exit 1
}

# Check if IDashboardDataService interface exists
if (Test-Path "Services/Dashboard/IDashboardDataService.cs") {
    Write-Host "✓ IDashboardDataService interface found" -ForegroundColor Green
} else {
    Write-Host "✗ IDashboardDataService interface missing" -ForegroundColor Red
    exit 1
}

# Check if DashboardViewModel exists
if (Test-Path "Models/Dashboard/DashboardViewModel.cs") {
    Write-Host "✓ DashboardViewModel found" -ForegroundColor Green
} else {
    Write-Host "✗ DashboardViewModel missing" -ForegroundColor Red
    exit 1
}

# Check if dashboard CSS exists
if (Test-Path "wwwroot/css/dashboard.css") {
    Write-Host "✓ Dashboard CSS found" -ForegroundColor Green
} else {
    Write-Host "✗ Dashboard CSS missing" -ForegroundColor Red
    exit 1
}

# Build the project to check for compilation errors
Write-Host "Building project..." -ForegroundColor Yellow
$buildResult = dotnet build --no-restore

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Project builds successfully" -ForegroundColor Green
} else {
    Write-Host "✗ Project build failed" -ForegroundColor Red
    Write-Host $buildResult
    exit 1
}

Write-Host ""
Write-Host "Dashboard implementation verification completed successfully!" -ForegroundColor Green
Write-Host "All required components are in place and the project builds successfully." -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Run the application: dotnet run" -ForegroundColor Cyan
Write-Host "2. Navigate to http://localhost:5033/dashboard" -ForegroundColor Cyan
Write-Host "3. Test the dashboard functionality manually" -ForegroundColor Cyan