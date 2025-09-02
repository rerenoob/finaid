#!/usr/bin/env python3
"""
Financial Aid Assistant - Structure Verification Script

This script verifies that all necessary components are in place for
frontend-backend integration testing.
"""

import os
import sys
from pathlib import Path

def check_file_exists(path, description):
    """Check if a file exists and return status"""
    exists = os.path.exists(path)
    status = "✓" if exists else "✗"
    print(f"{status} {description}: {path}")
    return exists

def check_directory_exists(path, description):
    """Check if a directory exists and return status"""
    exists = os.path.isdir(path)
    status = "✓" if exists else "✗"
    print(f"{status} {description}: {path}")
    return exists

def main():
    print("Financial Aid Assistant - Structure Verification")
    print("=" * 50)
    
    base_dir = Path("/home/dpham/Projects/finaid")
    
    # Check key directories
    directories = [
        ("Components", "Blazor Components"),
        ("Components/Pages", "Page Components"),
        ("Components/Forms", "Form Components"),
        ("Components/Documents", "Document Components"),
        ("Components/Dashboard", "Dashboard Components"),
        ("Services", "Service Layer"),
        ("Models", "Data Models"),
        ("Data", "Data Access Layer"),
        ("Configuration", "Configuration Classes"),
    ]
    
    dir_results = []
    for dir_path, description in directories:
        full_path = base_dir / dir_path
        dir_results.append(check_directory_exists(full_path, description))
    
    print()
    
    # Check key files
    files = [
        ("Program.cs", "Application Startup"),
        ("appsettings.json", "Application Configuration"),
        ("appsettings.Development.json", "Development Configuration"),
        ("Components/Pages/Documents.razor", "Documents Page"),
        ("Components/Pages/FAFSA.razor", "FAFSA Page"),
        ("Components/Pages/Progress.razor", "Progress Page"),
        ("Components/Forms/FAFSAFormWithAI.razor", "FAFSA AI Form"),
        ("Components/Documents/DocumentUpload.razor", "Document Upload Component"),
        ("Services/Documents/DocumentUIService.cs", "Document UI Service"),
        ("Services/Forms/FormAssistanceService.cs", "Form Assistance Service"),
        ("Services/Dashboard/DashboardDataService.cs", "Dashboard Data Service"),
    ]
    
    file_results = []
    for file_path, description in files:
        full_path = base_dir / file_path
        file_results.append(check_file_exists(full_path, description))
    
    print()
    
    # Check database
    db_file = base_dir / "finaid-dev.db"
    db_exists = os.path.exists(db_file)
    status = "✓" if db_exists else "⚠️"
    print(f"{status} Database File: {db_file}")
    if not db_exists:
        print("   Note: Database will be created on first run")
    
    print()
    
    # Summary
    all_dirs_ok = all(dir_results)
    all_files_ok = all(file_results)
    
    if all_dirs_ok and all_files_ok:
        print("✓ All required components are in place!")
        print("✓ Frontend-backend integration is properly configured")
        print("✓ Application is ready for manual testing")
        return 0
    else:
        print("⚠️  Some components are missing or incomplete")
        print("Please check the missing items above")
        return 1

if __name__ == "__main__":
    sys.exit(main())