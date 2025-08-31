# Task: Implement FAFSA Data Models and Validation

## Overview
- **Parent Feature**: IMPL-002 - Federal API Integration Layer
- **Complexity**: Medium
- **Estimated Time**: 7 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [ ] 001-federal-api-research.md: Federal API data structures documented
- [ ] 01-foundation-infrastructure/002-database-schema-design.md: Database models established

### External Dependencies
- FluentValidation library for model validation
- System.Text.Json for JSON serialization
- FAFSA form specifications from Federal Student Aid

## Implementation Details
### Files to Create/Modify
- `Models/FAFSA/FAFSAApplication.cs`: Core FAFSA application model
- `Models/FAFSA/StudentInformation.cs`: Student demographic data
- `Models/FAFSA/FinancialInformation.cs`: Income and asset data
- `Models/FAFSA/FamilyInformation.cs`: Parent/spouse information
- `Models/FAFSA/SchoolInformation.cs`: Educational institution data
- `Validators/FAFSA/`: FluentValidation validators for each model
- `Extensions/ModelExtensions.cs`: Model conversion and mapping utilities
- `Tests/Unit/Models/FAFSA/`: Unit tests for models and validation

### Code Patterns
- Follow existing entity model patterns in the codebase
- Use DataAnnotations for basic validation
- Implement FluentValidation for complex business rules
- Use nullable reference types for optional fields

### FAFSA Data Model Structure
```csharp
public class FAFSAApplication : BaseEntity
{
    public Guid UserId { get; set; }
    public int AwardYear { get; set; }
    public ApplicationStatus Status { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public string? ConfirmationNumber { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public StudentInformation StudentInfo { get; set; } = null!;
    public FinancialInformation FinancialInfo { get; set; } = null!;
    public List<FamilyInformation> FamilyMembers { get; set; } = new();
    public List<SchoolSelection> SchoolSelections { get; set; } = new();
}

public class StudentInformation : BaseEntity
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateTime DateOfBirth { get; set; }
    public string SocialSecurityNumber { get; set; } = null!; // Encrypted
    public string CitizenshipStatus { get; set; } = null!;
    public string StateOfLegalResidence { get; set; } = null!;
    // Additional FAFSA-specific fields
}
```

## Acceptance Criteria
- [ ] All FAFSA data models implemented with proper relationships
- [ ] Model validation rules match federal FAFSA requirements
- [ ] Data models support all required FAFSA form sections
- [ ] Proper handling of optional vs required fields
- [ ] Model-to-API mapping utilities implemented
- [ ] Database migration creates all necessary tables
- [ ] Entity Framework relationships configured correctly
- [ ] Validation provides clear, user-friendly error messages
- [ ] Models support partial completion and resume functionality
- [ ] Sensitive data fields properly encrypted

## Testing Strategy
- Unit tests: Model validation rules, property mappings, business logic
- Integration tests: Database operations, Entity Framework relationships
- Manual validation:
  - Create and validate FAFSA application with complete data
  - Test partial completion scenarios
  - Verify validation rules work correctly
  - Test model-to-API data conversion
  - Confirm encrypted fields are handled properly

## System Stability
- Backward compatible with existing User entity
- Database migrations handle schema changes gracefully
- Model validation prevents invalid data from entering system
- Proper error handling for validation failures