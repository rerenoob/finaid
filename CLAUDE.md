## Important Notes
- Update README.md and CLAUDE.md whenever applicable.
- Use the "~/Projects/dev-prompts/RULES.md" file for additional rules and
guidance for development.
- Use the "~/Projects/dev-prompts/[file-name].md" files for development tasks.
- Do not make any code changes before reading the RULES.md file mentioned above.

# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is an ASP.NET Core 8.0 Web API backend named "finaid" with a React frontend. The backend provides REST APIs and the frontend is built with React, TypeScript, and Vite.

## Development Commands

### Running the Backend API
```bash
cd /home/dpham/Projects/finaid
dotnet run
# Or with hot reload:
dotnet watch run
```
The API runs on https://localhost:7253 (HTTPS) and http://localhost:5033 (HTTP) by default.

### Running the React Frontend
```bash
cd /home/dpham/Projects/finaid/finaid-react
npm run dev
```
The React app runs on http://localhost:5173 by default.

### Backend Build and Restore
```bash
dotnet build
dotnet restore
```

### Frontend Build
```bash
cd /home/dpham/Projects/finaid/finaid-react
npm run build
```

## Architecture

### Backend (ASP.NET Core 8.0 Web API)
- **Framework**: ASP.NET Core 8.0 Web API
- **Entry Point**: `Program.cs` configures services and middleware pipeline
- **Controllers**: Located in `/Controllers/` directory
- **Services**: Business logic in `/Services/` directory
- **Models**: Data models in `/Models/` directory
- **Database**: Entity Framework with SQLite (dev) / SQL Server (prod)

### Frontend (React + TypeScript + Vite)
- **Framework**: React 19 with TypeScript
- **Build Tool**: Vite
- **Styling**: Tailwind CSS
- **Location**: `/finaid-react/` directory
- **Entry Point**: `src/main.tsx`
- **Pages**: Located in `src/pages/`
- **Components**: Located in `src/components/`

## Key Configuration

- **Target Framework**: .NET 8.0
- **Nullable Reference Types**: Enabled
- **Implicit Usings**: Enabled
- **Antiforgery**: Configured for security
- **HTTPS Redirection**: Enabled in production
- **Exception Handling**: Custom error page at "/Error" for non-development environments

