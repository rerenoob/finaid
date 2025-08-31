using FluentValidation;
using finaid.Models.FAFSA;

namespace finaid.Validators.FAFSA;

public class FamilyInformationValidator : AbstractValidator<FamilyInformation>
{
    public FamilyInformationValidator()
    {
        // Required fields
        RuleFor(x => x.FAFSAApplicationId)
            .NotEmpty().WithMessage("FAFSA Application ID is required");

        RuleFor(x => x.RelationshipType)
            .NotEmpty().WithMessage("Relationship type is required")
            .MaximumLength(20).WithMessage("Relationship type cannot exceed 20 characters")
            .Must(BeValidRelationshipType).WithMessage("Invalid relationship type");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(50).WithMessage("First name cannot exceed 50 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters");

        RuleFor(x => x.MiddleName)
            .MaximumLength(50).WithMessage("Middle name cannot exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.MiddleName));

        RuleFor(x => x.Suffix)
            .MaximumLength(10).WithMessage("Suffix cannot exceed 10 characters")
            .When(x => !string.IsNullOrEmpty(x.Suffix));

        // Date validations
        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required")
            .Must(BeValidAge).WithMessage("Age must be between 0 and 120 years")
            .When(x => x.DateOfBirth.HasValue);

        // SSN validation for US citizens
        RuleFor(x => x.EncryptedSSN)
            .NotEmpty().WithMessage("SSN is required for US citizens")
            .When(x => x.CitizenshipStatus == "US Citizen" || x.CitizenshipStatus == "Permanent Resident");

        // Contact information
        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        // Address validation
        RuleFor(x => x.Address)
            .MaximumLength(200).WithMessage("Address cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Address));

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("City cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.City));

        RuleFor(x => x.State)
            .Length(2).WithMessage("State must be 2 characters")
            .Matches("^[A-Z]{2}$").WithMessage("State must be uppercase letters")
            .When(x => !string.IsNullOrEmpty(x.State));

        RuleFor(x => x.ZipCode)
            .Matches(@"^\d{5}(-\d{4})?$").WithMessage("Zip code must be in format 12345 or 12345-6789")
            .When(x => !string.IsNullOrEmpty(x.ZipCode));

        // Educational information validation
        RuleFor(x => x.CollegeName)
            .NotEmpty().WithMessage("College name is required when in college")
            .MaximumLength(200).WithMessage("College name cannot exceed 200 characters")
            .When(x => x.IsInCollege);

        RuleFor(x => x.CollegeCode)
            .Length(6, 8).WithMessage("College code must be 6-8 characters")
            .When(x => x.IsInCollege && !string.IsNullOrEmpty(x.CollegeCode));

        RuleFor(x => x.ExpectedGraduationDate)
            .GreaterThan(DateTime.Now).WithMessage("Expected graduation date must be in the future")
            .When(x => x.ExpectedGraduationDate.HasValue && x.IsInCollege);

        // Employment validation
        RuleFor(x => x.Employer)
            .MaximumLength(100).WithMessage("Employer name cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Employer));

        RuleFor(x => x.JobTitle)
            .MaximumLength(100).WithMessage("Job title cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.JobTitle));

        // Special circumstances validation
        RuleFor(x => x.SpecialNeedsDescription)
            .NotEmpty().WithMessage("Please describe the special needs")
            .MaximumLength(500).WithMessage("Special needs description cannot exceed 500 characters")
            .When(x => x.HasSpecialNeeds);

        RuleFor(x => x.DisabilityDescription)
            .NotEmpty().WithMessage("Please describe the disability")
            .MaximumLength(500).WithMessage("Disability description cannot exceed 500 characters")
            .When(x => x.HasDisability);

        RuleFor(x => x.DocumentationNotes)
            .MaximumLength(500).WithMessage("Documentation notes cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.DocumentationNotes));

        RuleFor(x => x.AdditionalNotes)
            .MaximumLength(500).WithMessage("Additional notes cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.AdditionalNotes));

        // Business rules
        RuleFor(x => x)
            .Must(HaveConsistentEducationInfo)
            .WithMessage("Education information is inconsistent")
            .WithName("Education Information");

        RuleFor(x => x)
            .Must(HaveConsistentEmploymentInfo)
            .WithMessage("Employment information is inconsistent")
            .WithName("Employment Information");
    }

    private static bool BeValidRelationshipType(string? relationshipType)
    {
        if (string.IsNullOrEmpty(relationshipType))
            return false;

        var validTypes = new[] { "Parent1", "Parent2", "Spouse", "Child", "Other", "Guardian", "Stepparent" };
        return validTypes.Contains(relationshipType);
    }

    private static bool BeValidAge(DateTime? dateOfBirth)
    {
        if (!dateOfBirth.HasValue) return true;
        
        var age = DateTime.Now.Year - dateOfBirth.Value.Year;
        if (dateOfBirth.Value.Date > DateTime.Now.AddYears(-age)) age--;
        
        return age >= 0 && age <= 120;
    }

    private static bool HaveConsistentEducationInfo(FamilyInformation family)
    {
        // If marked as in college, must have college information
        if (family.IsInCollege)
        {
            return !string.IsNullOrEmpty(family.CollegeName);
        }

        // If not in college, college-specific fields should be empty
        if (!family.IsInCollege)
        {
            return string.IsNullOrEmpty(family.CollegeName) && 
                   string.IsNullOrEmpty(family.CollegeCode);
        }

        return true;
    }

    private static bool HaveConsistentEmploymentInfo(FamilyInformation family)
    {
        // If employment status indicates employed, should have employer info
        if (family.EmploymentStatus == "Employed")
        {
            return !string.IsNullOrEmpty(family.Employer);
        }

        // If unemployed or retired, employer fields can be empty
        if (family.EmploymentStatus == "Unemployed" || family.EmploymentStatus == "Retired")
        {
            // This is valid - can have empty employer fields
            return true;
        }

        return true;
    }
}