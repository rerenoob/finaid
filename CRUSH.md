## Important Notes
- Update README.md and CLAUDE.md whenever applicable.
- Use the "~/Projects/dev-prompts/RULES.md" file for additional rules and
guidance for development.
- Use the "~/Projects/dev-prompts/[file-name].md" files for development tasks.
- Do not make any code changes before reading the RULES.md file mentioned above.
- Don't make any code attribution for git commit

# CRUSH.md - Development Guide

## Build & Test Commands
- `dotnet build` - Build solution
- `dotnet run` - Run application (HTTPS:7253, HTTP:5033)
- `dotnet watch run` - Run with hot reload
- `dotnet test` - Run all tests
- `dotnet test --filter "FullyQualifiedName~TestClassName"` - Run single test
- `dotnet ef migrations add` - Add EF migration
- `dotnet ef database update` - Update database

## Code Style Guidelines
- **Namespaces**: `finaid.{Area}.{SubArea}` (e.g., `finaid.Services.AI`)
- **Imports**: Group system, third-party, then local namespaces
- **Naming**: PascalCase for classes/methods, camelCase for parameters
- **Entities**: Inherit from `BaseEntity` with GUID Id and audit fields
- **Services**: Interface-first design (`I{ServiceName}` pattern)
- **Validation**: Use FluentValidation with separate validator classes
- **Error Handling**: Use ILogger with structured logging
- **Async/Await**: Use `async`/`await` pattern consistently

## Architecture Patterns
- Dependency Injection for all services
- Repository pattern with Entity Framework
- CQRS-inspired service layer
- Azure integration patterns for cloud services
- XUnit for testing with Moq and FluentAssertions

## Key Conventions
- Nullable reference types enabled
- Implicit usings enabled
- Use `DateTime.UtcNow` for timestamps
- Follow Blazor component naming (.razor files)
- Separate models by domain area (FAFSA, Eligibility, AI, etc.)
