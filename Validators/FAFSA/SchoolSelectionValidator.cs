using FluentValidation;
using finaid.Models.FAFSA;

namespace finaid.Validators.FAFSA;

public class SchoolSelectionValidator : AbstractValidator<SchoolSelection>
{
    public SchoolSelectionValidator()
    {
        // Required fields
        RuleFor(x => x.FAFSAApplicationId)
            .NotEmpty().WithMessage("FAFSA Application ID is required");

        // School Code Validation
        RuleFor(x => x.FederalSchoolCode)
            .NotEmpty().WithMessage("Federal school code is required")
            .MaximumLength(10).WithMessage("Federal school code cannot exceed 10 characters")
            .Matches("^[0-9A-Z]+$").WithMessage("Federal school code must contain only numbers and uppercase letters");

        // School Information
        RuleFor(x => x.SchoolName)
            .MaximumLength(200).WithMessage("School name cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.SchoolName));

        RuleFor(x => x.Address)
            .MaximumLength(200).WithMessage("School address cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Address));

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("School city cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.City));

        RuleFor(x => x.State)
            .Length(2).WithMessage("School state must be 2 characters")
            .Matches("^[A-Z]{2}$").WithMessage("School state must be uppercase letters")
            .When(x => !string.IsNullOrEmpty(x.State));

        RuleFor(x => x.ZipCode)
            .Matches(@"^\d{5}(-\d{4})?$").WithMessage("School zip code must be in format 12345 or 12345-6789")
            .When(x => !string.IsNullOrEmpty(x.ZipCode));

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        // Academic Information
        RuleFor(x => x.IntendedGradeLevel)
            .NotEmpty().WithMessage("Intended grade level is required")
            .MaximumLength(20).WithMessage("Intended grade level cannot exceed 20 characters")
            .Must(BeValidGradeLevel).WithMessage("Invalid grade level");

        RuleFor(x => x.IntendedDegreeType)
            .NotEmpty().WithMessage("Intended degree type is required")
            .MaximumLength(50).WithMessage("Intended degree type cannot exceed 50 characters")
            .Must(BeValidDegreeType).WithMessage("Invalid degree type");

        RuleFor(x => x.IntendedMajor)
            .MaximumLength(100).WithMessage("Intended major cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.IntendedMajor));

        RuleFor(x => x.IntendedMinor)
            .MaximumLength(100).WithMessage("Intended minor cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.IntendedMinor));

        // Enrollment Information
        RuleFor(x => x.EnrollmentStatus)
            .NotEmpty().WithMessage("Enrollment status is required")
            .MaximumLength(20).WithMessage("Enrollment status cannot exceed 20 characters")
            .Must(BeValidEnrollmentStatus).WithMessage("Invalid enrollment status");

        RuleFor(x => x.HousingPreference)
            .NotEmpty().WithMessage("Housing preference is required")
            .MaximumLength(20).WithMessage("Housing preference cannot exceed 20 characters")
            .Must(BeValidHousingPreference).WithMessage("Invalid housing preference");

        RuleFor(x => x.IntendedStartDate)
            .GreaterThan(DateTime.Now).WithMessage("Intended start date must be in the future")
            .When(x => x.IntendedStartDate.HasValue);

        RuleFor(x => x.SelectionOrder)
            .InclusiveBetween(1, 10).WithMessage("Selection order must be between 1 and 10");

        // School Characteristics
        RuleFor(x => x.SchoolType)
            .MaximumLength(20).WithMessage("School type cannot exceed 20 characters")
            .Must(BeValidSchoolType).WithMessage("Invalid school type")
            .When(x => !string.IsNullOrEmpty(x.SchoolType));

        RuleFor(x => x.ControlType)
            .MaximumLength(20).WithMessage("Control type cannot exceed 20 characters")
            .Must(BeValidControlType).WithMessage("Invalid control type")
            .When(x => !string.IsNullOrEmpty(x.ControlType));

        // Cost Information
        RuleFor(x => x.EstimatedCostOfAttendance)
            .GreaterThan(0).WithMessage("Estimated cost of attendance must be greater than 0")
            .LessThanOrEqualTo(200000m).WithMessage("Estimated cost of attendance seems unusually high")
            .When(x => x.EstimatedCostOfAttendance.HasValue);

        RuleFor(x => x.TuitionAndFees)
            .GreaterThanOrEqualTo(0).WithMessage("Tuition and fees cannot be negative")
            .LessThanOrEqualTo(x => x.EstimatedCostOfAttendance ?? decimal.MaxValue)
            .WithMessage("Tuition and fees cannot exceed estimated cost of attendance")
            .When(x => x.TuitionAndFees.HasValue);

        RuleFor(x => x.RoomAndBoard)
            .GreaterThanOrEqualTo(0).WithMessage("Room and board cannot be negative")
            .LessThanOrEqualTo(50000m).WithMessage("Room and board seems unusually high")
            .When(x => x.RoomAndBoard.HasValue);

        RuleFor(x => x.BooksAndSupplies)
            .GreaterThanOrEqualTo(0).WithMessage("Books and supplies cannot be negative")
            .LessThanOrEqualTo(10000m).WithMessage("Books and supplies seems unusually high")
            .When(x => x.BooksAndSupplies.HasValue);

        RuleFor(x => x.PersonalExpenses)
            .GreaterThanOrEqualTo(0).WithMessage("Personal expenses cannot be negative")
            .LessThanOrEqualTo(30000m).WithMessage("Personal expenses seem unusually high")
            .When(x => x.PersonalExpenses.HasValue);

        RuleFor(x => x.TransportationCosts)
            .GreaterThanOrEqualTo(0).WithMessage("Transportation costs cannot be negative")
            .LessThanOrEqualTo(20000m).WithMessage("Transportation costs seem unusually high")
            .When(x => x.TransportationCosts.HasValue);

        // Financial Aid Information
        RuleFor(x => x.InstitutionalAidAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Institutional aid amount cannot be negative")
            .LessThanOrEqualTo(x => x.EstimatedCostOfAttendance ?? decimal.MaxValue)
            .WithMessage("Institutional aid cannot exceed estimated cost of attendance")
            .When(x => x.InstitutionalAidAmount.HasValue);

        RuleFor(x => x.MeritScholarshipAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Merit scholarship amount cannot be negative")
            .LessThanOrEqualTo(x => x.EstimatedCostOfAttendance ?? decimal.MaxValue)
            .WithMessage("Merit scholarship cannot exceed estimated cost of attendance")
            .When(x => x.MeritScholarshipAmount.HasValue);

        RuleFor(x => x.NeedBasedAidAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Need-based aid amount cannot be negative")
            .LessThanOrEqualTo(x => x.EstimatedCostOfAttendance ?? decimal.MaxValue)
            .WithMessage("Need-based aid cannot exceed estimated cost of attendance")
            .When(x => x.NeedBasedAidAmount.HasValue);

        // Application Status
        RuleFor(x => x.ApplicationStatus)
            .MaximumLength(20).WithMessage("Application status cannot exceed 20 characters")
            .Must(BeValidApplicationStatus).WithMessage("Invalid application status")
            .When(x => !string.IsNullOrEmpty(x.ApplicationStatus));

        // Date validations
        RuleFor(x => x.ApplicationSubmittedDate)
            .LessThanOrEqualTo(DateTime.Now).WithMessage("Application submitted date cannot be in the future")
            .When(x => x.ApplicationSubmittedDate.HasValue);

        RuleFor(x => x.AcceptanceDate)
            .GreaterThanOrEqualTo(x => x.ApplicationSubmittedDate)
            .WithMessage("Acceptance date must be after application submitted date")
            .When(x => x.AcceptanceDate.HasValue && x.ApplicationSubmittedDate.HasValue);

        // Contact Information
        RuleFor(x => x.FinancialAidContactName)
            .MaximumLength(100).WithMessage("Financial aid contact name cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.FinancialAidContactName));

        RuleFor(x => x.FinancialAidContactEmail)
            .EmailAddress().WithMessage("Invalid financial aid contact email format")
            .MaximumLength(256).WithMessage("Financial aid contact email cannot exceed 256 characters")
            .When(x => !string.IsNullOrEmpty(x.FinancialAidContactEmail));

        // Business Rules
        RuleFor(x => x)
            .Must(HaveValidCostBreakdown)
            .WithMessage("Cost breakdown does not match estimated cost of attendance")
            .WithName("Cost Information");

        RuleFor(x => x)
            .Must(HaveConsistentSchoolType)
            .WithMessage("School type and characteristics are inconsistent")
            .WithName("School Information");

        RuleFor(x => x)
            .Must(HaveConsistentApplicationInfo)
            .WithMessage("Application information is inconsistent")
            .WithName("Application Information");
    }

    private static bool BeValidGradeLevel(string? gradeLevel)
    {
        if (string.IsNullOrEmpty(gradeLevel))
            return false;

        var validLevels = new[] { "Undergraduate", "Graduate", "Professional" };
        return validLevels.Contains(gradeLevel);
    }

    private static bool BeValidDegreeType(string? degreeType)
    {
        if (string.IsNullOrEmpty(degreeType))
            return false;

        var validTypes = new[] { "Bachelor's", "Master's", "Doctorate", "Certificate", "Associate", "Professional" };
        return validTypes.Contains(degreeType);
    }

    private static bool BeValidEnrollmentStatus(string? enrollmentStatus)
    {
        if (string.IsNullOrEmpty(enrollmentStatus))
            return false;

        var validStatuses = new[] { "Full-time", "Half-time", "Less than half-time", "Three-quarter time" };
        return validStatuses.Contains(enrollmentStatus);
    }

    private static bool BeValidHousingPreference(string? housingPreference)
    {
        if (string.IsNullOrEmpty(housingPreference))
            return false;

        var validPreferences = new[] { "On-campus", "Off-campus", "With parents" };
        return validPreferences.Contains(housingPreference);
    }

    private static bool BeValidSchoolType(string? schoolType)
    {
        if (string.IsNullOrEmpty(schoolType))
            return true; // Optional field

        var validTypes = new[] { "Public", "Private non-profit", "Private for-profit" };
        return validTypes.Contains(schoolType);
    }

    private static bool BeValidControlType(string? controlType)
    {
        if (string.IsNullOrEmpty(controlType))
            return true; // Optional field

        var validTypes = new[] { "Public", "Private" };
        return validTypes.Contains(controlType);
    }

    private static bool BeValidApplicationStatus(string? applicationStatus)
    {
        if (string.IsNullOrEmpty(applicationStatus))
            return true; // Optional field

        var validStatuses = new[] { "Applied", "Accepted", "Enrolled", "Waitlisted", "Rejected", "Withdrawn" };
        return validStatuses.Contains(applicationStatus);
    }

    private static bool HaveValidCostBreakdown(SchoolSelection school)
    {
        if (!school.EstimatedCostOfAttendance.HasValue)
            return true;

        var calculatedTotal = (school.TuitionAndFees ?? 0) +
                             (school.RoomAndBoard ?? 0) +
                             (school.BooksAndSupplies ?? 0) +
                             (school.TransportationCosts ?? 0) +
                             (school.PersonalExpenses ?? 0);

        // Allow some tolerance for rounding differences (within $100)
        return Math.Abs(calculatedTotal - school.EstimatedCostOfAttendance.Value) <= 100m;
    }

    private static bool HaveConsistentSchoolType(SchoolSelection school)
    {
        // Check consistency between school type flags
        var typeFlags = new[] { school.IsTwoYearCollege, school.IsFourYearCollege, school.IsGraduateSchool };
        var trueCount = typeFlags.Count(flag => flag);

        // Should have at least one type flag set
        if (trueCount == 0)
            return false;

        // Graduate school should be compatible with intended grade level
        if (school.IsGraduateSchool && school.IntendedGradeLevel == "Undergraduate")
            return false;

        // Two-year college should not offer graduate programs
        if (school.IsTwoYearCollege && school.IsGraduateSchool)
            return false;

        return true;
    }

    private static bool HaveConsistentApplicationInfo(SchoolSelection school)
    {
        // If accepted, should have application submitted
        if (school.ApplicationStatus == "Accepted" && !school.ApplicationSubmittedDate.HasValue)
            return false;

        // If enrolled, should have been accepted
        if (school.ApplicationStatus == "Enrolled" && school.AcceptanceDate == null)
            return false;

        // CSS Profile dates should be logical
        if (school.CSSProfileSubmitted && !school.CSSProfileSubmissionDate.HasValue)
            return false;

        // Verification completion should have date
        if (school.VerificationCompleted && !school.VerificationCompletedDate.HasValue)
            return false;

        return true;
    }
}