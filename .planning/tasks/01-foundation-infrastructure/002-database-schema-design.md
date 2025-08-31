# Task: Design and Implement Core Database Schema

## Overview
- **Parent Feature**: IMPL-001 - Foundation and Infrastructure Setup
- **Complexity**: Medium
- **Estimated Time**: 8 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [ ] 001-azure-resources-setup.md: Azure SQL Database must be provisioned

### External Dependencies
- Entity Framework Core 8.0
- SQL Server connection from Azure resources

## Implementation Details
### Files to Create/Modify
- `Data/ApplicationDbContext.cs`: Main EF Core context
- `Models/User.cs`: User profile and authentication data
- `Models/Application/FAFSAApplication.cs`: FAFSA application data model
- `Models/Document/UserDocument.cs`: Document storage metadata
- `Migrations/`: EF Core database migrations
- `Data/Configurations/`: Entity configurations for complex relationships

### Code Patterns
- Follow existing Blazor Server Entity Framework patterns
- Use Data Annotations and Fluent API for entity configuration
- Implement soft delete pattern for sensitive data
- Use audit fields (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)

### Database Schema Design
```csharp
// Core entities to implement
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string SSN { get; set; } // Encrypted
    public UserProfile Profile { get; set; }
    // Navigation properties
}

public class FAFSAApplication
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int AwardYear { get; set; }
    public ApplicationStatus Status { get; set; }
    public string FormData { get; set; } // JSON blob
    public decimal EstimatedEFC { get; set; }
    // Relationships
}
```

## Acceptance Criteria
- [ ] Database schema supports all MVP user flows
- [ ] All sensitive fields properly encrypted at rest
- [ ] Entity relationships correctly defined with proper constraints
- [ ] Database migrations create schema without errors
- [ ] Audit fields implemented on all entities
- [ ] Soft delete functionality working for sensitive data
- [ ] Database indexes optimized for common queries
- [ ] Foreign key constraints properly configured

## Testing Strategy
- Unit tests: Entity validation and relationship tests
- Integration tests: Database operations through EF Core context
- Manual validation:
  - Run migrations against clean database
  - Verify all tables created with correct schema
  - Test CRUD operations for each entity
  - Validate encryption/decryption of sensitive fields

## System Stability
- Schema versioning through EF Core migrations
- Rollback capability through migration history
- No breaking changes to existing Blazor Server structure
- Database seeding for development and testing environments