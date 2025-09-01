#!/bin/bash

# Simple dashboard verification script
echo "Testing Dashboard Implementation..."

# Check if DashboardLayout component exists
if [ -f "Components/Dashboard/DashboardLayout.razor" ]; then
    echo "✓ DashboardLayout component found"
else
    echo "✗ DashboardLayout component missing"
    exit 1
fi

# Check if Dashboard.razor page exists
if [ -f "Components/Pages/Dashboard.razor" ]; then
    echo "✓ Dashboard page found"
else
    echo "✗ Dashboard page missing"
    exit 1
fi

# Check if required dashboard components exist
required_components=(
    "ProgressBar.razor"
    "StepIndicator.razor" 
    "DeadlineList.razor"
    "ActivityFeed.razor"
    "QuickActions.razor"
    "NotificationCenter.razor"
    "RealTimeConnectionStatus.razor"
)

for component in "${required_components[@]}"; do
    if [ -f "Components/Dashboard/$component" ]; then
        echo "✓ $component found"
    else
        echo "✗ $component missing"
        exit 1
    fi
done

# Check if DashboardDataService exists
if [ -f "Services/Dashboard/DashboardDataService.cs" ]; then
    echo "✓ DashboardDataService found"
else
    echo "✗ DashboardDataService missing"
    exit 1
fi

# Check if IDashboardDataService interface exists
if [ -f "Services/Dashboard/IDashboardDataService.cs" ]; then
    echo "✓ IDashboardDataService interface found"
else
    echo "✗ IDashboardDataService interface missing"
    exit 1
fi

# Check if DashboardViewModel exists
if [ -f "Models/Dashboard/DashboardViewModel.cs" ]; then
    echo "✓ DashboardViewModel found"
else
    echo "✗ DashboardViewModel missing"
    exit 1
fi

# Check if dashboard CSS exists
if [ -f "wwwroot/css/dashboard.css" ]; then
    echo "✓ Dashboard CSS found"
else
    echo "✗ Dashboard CSS missing"
    exit 1
fi

# Build the project to check for compilation errors
echo "Building project..."
build_result=$(dotnet build --no-restore 2>&1)

if [ $? -eq 0 ]; then
    echo "✓ Project builds successfully"
else
    echo "✗ Project build failed"
    echo "$build_result"
    exit 1
fi

echo ""
echo "Dashboard implementation verification completed successfully!"
echo "All required components are in place and the project builds successfully."
echo ""
echo "Next steps:"
echo "1. Run the application: dotnet run"
echo "2. Navigate to http://localhost:5033/dashboard"
echo "3. Test the dashboard functionality manually"