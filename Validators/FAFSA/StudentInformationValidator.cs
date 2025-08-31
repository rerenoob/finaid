using FluentValidation;
using finaid.Models.FAFSA;

namespace finaid.Validators.FAFSA;

public class StudentInformationValidator : AbstractValidator<StudentInformation>
{
    public StudentInformationValidator()
    {
        // Basic Required Fields
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(50).WithMessage("First name must not exceed 50 characters")
            .Matches(@"^[a-zA-Z\s\-'\.]+$").WithMessage("First name contains invalid characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(50).WithMessage("Last name must not exceed 50 characters")
            .Matches(@"^[a-zA-Z\s\-'\.]+$").WithMessage("Last name contains invalid characters");

        RuleFor(x => x.MiddleName)
            .MaximumLength(50).WithMessage("Middle name must not exceed 50 characters")
            .Matches(@"^[a-zA-Z\s\-'\.]*$").WithMessage("Middle name contains invalid characters")
            .When(x => !string.IsNullOrEmpty(x.MiddleName));

        // Date of Birth Validation
        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required")
            .LessThan(DateTime.Now.AddYears(-13)).WithMessage("Student must be at least 13 years old")
            .GreaterThan(DateTime.Now.AddYears(-100)).WithMessage("Please enter a valid date of birth");

        // SSN Validation
        RuleFor(x => x.EncryptedSSN)
            .NotEmpty().WithMessage("Social Security Number is required");

        // Citizenship Status
        RuleFor(x => x.CitizenshipStatus)
            .NotEmpty().WithMessage("Citizenship status is required")
            .Must(BeValidCitizenshipStatus).WithMessage("Please select a valid citizenship status");

        // State of Legal Residence
        RuleFor(x => x.StateOfLegalResidence)
            .NotEmpty().WithMessage("State of legal residence is required")
            .Length(2).WithMessage("State must be a valid 2-character state code")
            .Must(BeValidStateCode).WithMessage("Please enter a valid state code");

        // Residency Date
        RuleFor(x => x.ResidencyDate)
            .NotEmpty().WithMessage("Residency date is required")
            .LessThanOrEqualTo(DateTime.Now).WithMessage("Residency date cannot be in the future");

        // Sex
        RuleFor(x => x.Sex)
            .NotEmpty().WithMessage("Sex is required")
            .Must(BeValidSex).WithMessage("Please select a valid option");

        // Marital Status
        RuleFor(x => x.MaritalStatus)
            .NotEmpty().WithMessage("Marital status is required")
            .Must(BeValidMaritalStatus).WithMessage("Please select a valid marital status");

        // Marital Status Date validation
        RuleFor(x => x.MaritalStatusDate)
            .NotEmpty().WithMessage("Marital status date is required")
            .LessThanOrEqualTo(DateTime.Now).WithMessage("Marital status date cannot be in the future")
            .When(x => x.MaritalStatus != "Single");

        // Contact Information
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required")
            .EmailAddress().WithMessage("Please enter a valid email address")
            .MaximumLength(256).WithMessage("Email address must not exceed 256 characters");

        RuleFor(x => x.AlternateEmail)
            .EmailAddress().WithMessage("Please enter a valid alternate email address")
            .MaximumLength(256).WithMessage("Alternate email address must not exceed 256 characters")
            .When(x => !string.IsNullOrEmpty(x.AlternateEmail));

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\(\d{3}\) \d{3}-\d{4}$|^\d{3}-\d{3}-\d{4}$|^\d{10}$")
            .WithMessage("Please enter a valid phone number")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        // Address Validation
        When(x => !string.IsNullOrEmpty(x.PermanentAddress), () =>
        {
            RuleFor(x => x.PermanentCity)
                .NotEmpty().WithMessage("City is required when address is provided")
                .MaximumLength(100).WithMessage("City must not exceed 100 characters");

            RuleFor(x => x.PermanentState)
                .NotEmpty().WithMessage("State is required when address is provided")
                .Length(2).WithMessage("State must be a valid 2-character state code")
                .Must(BeValidStateCode).WithMessage("Please enter a valid state code");

            RuleFor(x => x.PermanentZipCode)
                .NotEmpty().WithMessage("ZIP code is required when address is provided")
                .Matches(@"^\d{5}(-\d{4})?$").WithMessage("Please enter a valid ZIP code");
        });

        // Educational Information
        RuleFor(x => x.GradeLevel)
            .NotEmpty().WithMessage("Grade level is required")
            .Must(BeValidGradeLevel).WithMessage("Please select a valid grade level");

        RuleFor(x => x.DegreeType)
            .Must(BeValidDegreeType).WithMessage("Please select a valid degree type")
            .When(x => !string.IsNullOrEmpty(x.DegreeType));

        // High School Information
        RuleFor(x => x.HighSchoolGraduationYear)
            .InclusiveBetween(1950, DateTime.Now.Year + 2)
            .WithMessage($"High school graduation year must be between 1950 and {DateTime.Now.Year + 2}")
            .When(x => x.HighSchoolGraduationYear.HasValue);

        // Special Circumstances Logic Validation
        RuleFor(x => x)
            .Must(HaveValidDependencyStatus)
            .WithMessage("Based on your answers, your dependency status may need review")
            .WithName("Dependency Status");

        // Alien Registration Number for non-citizens
        RuleFor(x => x.AlienRegistrationNumber)
            .NotEmpty().WithMessage("Alien Registration Number is required for non-citizens")
            .Matches(@"^[A-Z]?\d{8,9}$").WithMessage("Please enter a valid Alien Registration Number")
            .When(x => x.CitizenshipStatus == "Eligible Non-Citizen");
    }

    private static bool BeValidCitizenshipStatus(string? status)
    {
        var validStatuses = new[] { "US Citizen", "Eligible Non-Citizen", "Neither" };
        return !string.IsNullOrEmpty(status) && validStatuses.Contains(status);
    }

    private static bool BeValidStateCode(string? stateCode)
    {
        var validStates = new[]
        {
            "AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "FL", "GA", "HI", "ID", "IL", "IN", "IA",
            "KS", "KY", "LA", "ME", "MD", "MA", "MI", "MN", "MS", "MO", "MT", "NE", "NV", "NH", "NJ",
            "NM", "NY", "NC", "ND", "OH", "OK", "OR", "PA", "RI", "SC", "SD", "TN", "TX", "UT", "VT",
            "VA", "WA", "WV", "WI", "WY", "DC", "PR", "VI", "GU", "AS", "MP"
        };
        return !string.IsNullOrEmpty(stateCode) && validStates.Contains(stateCode.ToUpper());
    }

    private static bool BeValidSex(string? sex)
    {
        var validOptions = new[] { "Male", "Female", "Not Reported" };
        return !string.IsNullOrEmpty(sex) && validOptions.Contains(sex);
    }

    private static bool BeValidMaritalStatus(string? status)
    {
        var validStatuses = new[] { "Single", "Married", "Divorced", "Widowed", "Separated" };
        return !string.IsNullOrEmpty(status) && validStatuses.Contains(status);
    }

    private static bool BeValidGradeLevel(string? level)
    {
        var validLevels = new[] 
        { 
            "Never attended college", 
            "First year undergraduate", 
            "Second year undergraduate", 
            "Third year undergraduate", 
            "Fourth year undergraduate", 
            "Fifth year/other undergraduate", 
            "First year graduate/professional", 
            "Continuing graduate/professional" 
        };
        return !string.IsNullOrEmpty(level) && validLevels.Contains(level);
    }

    private static bool BeValidDegreeType(string? degreeType)
    {
        var validTypes = new[] 
        { 
            "Certificate", 
            "Associate", 
            "Bachelor's", 
            "Master's", 
            "Doctorate", 
            "Professional" 
        };
        return string.IsNullOrEmpty(degreeType) || validTypes.Contains(degreeType);
    }

    private static bool HaveValidDependencyStatus(StudentInformation student)
    {
        // Check for independent student indicators
        var hasIndependentIndicators = student.IsOrphan ||
                                       student.IsWardOfCourt ||
                                       student.WasInFosterCare ||
                                       student.IsEmancipatedMinor ||
                                       student.IsHomeless ||
                                       student.HasMilitaryService ||
                                       student.IsVeteran;

        var isMarried = student.MaritalStatus == "Married";
        
        // Calculate age
        var age = DateTime.Now.Year - student.DateOfBirth.Year;
        if (DateTime.Now.DayOfYear < student.DateOfBirth.DayOfYear)
        {
            age--;
        }
        
        var isOver24 = age >= 24;

        // Independent students should have certain characteristics
        if (hasIndependentIndicators || isMarried || isOver24)
        {
            // Additional validation could be added here
            return true;
        }

        // Dependent students under 24 and not married
        return true;
    }
}