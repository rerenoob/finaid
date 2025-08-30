# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is an ASP.NET Core 8.0 Blazor Server application named "finaid". It uses interactive server-side components with Razor syntax.

## Development Commands

### Running the application
```bash
dotnet run
# Or with specific profile:
dotnet run --launch-profile https
```
The app runs on https://localhost:7253 (HTTPS) and http://localhost:5033 (HTTP) by default.

### Build and restore
```bash
dotnet build
dotnet restore
```

### Watch mode for development
```bash
dotnet watch run
```

## Architecture

- **Framework**: ASP.NET Core 8.0 with Blazor Server
- **Rendering**: Interactive Server Components
- **Entry Point**: `Program.cs` configures services and middleware pipeline
- **Components**: Located in `/Components/` directory
  - `App.razor` - Root application component with HTML structure
  - `Routes.razor` - Routing configuration
  - `/Pages/` - Page components (Home, Counter, Weather, Error)
  - `/Layout/` - Layout components (MainLayout, NavMenu)
- **Static Files**: Served from `/wwwroot/`
- **Styling**: Uses Bootstrap CSS framework

## Key Configuration

- **Target Framework**: .NET 8.0
- **Nullable Reference Types**: Enabled
- **Implicit Usings**: Enabled
- **Antiforgery**: Configured for security
- **HTTPS Redirection**: Enabled in production
- **Exception Handling**: Custom error page at "/Error" for non-development environments

## Important Notes
- Update README.md and CLAUDE.md whenever applicable.
- Use the "~/Projects/dev-prompts/RULES.md" file for additional rules and
guidance for development.
- Use the "~/Projects/dev-prompts/[file-name].md" files for development tasks.
