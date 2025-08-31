using FluentValidation;
using finaid.Models.Eligibility;

namespace finaid.Validators.Eligibility;

public class EligibilityRequestValidator : AbstractValidator<EligibilityRequest>
{
    public EligibilityRequestValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("Application ID is required");

        RuleFor(x => x.AwardYear)
            .InclusiveBetween(DateTime.Now.Year - 1, DateTime.Now.Year + 5)
            .WithMessage("Award year must be within valid range");

        RuleFor(x => x.StudentInfo)
            .NotNull().WithMessage("Student information is required");

        RuleFor(x => x.FinancialInfo)
            .NotNull().WithMessage("Financial information is required");

        RuleFor(x => x.FamilyMembers)
            .NotNull().WithMessage("Family members list cannot be null");

        RuleFor(x => x.SchoolSelections)
            .NotNull().WithMessage("School selections list cannot be null");

        When(x => !string.IsNullOrEmpty(x.SpecificSchoolCode), () => {
            RuleFor(x => x.SpecificSchoolCode)
                .Matches(@"^[0-9A-Z]{6,8}$")
                .WithMessage("School code must be 6-8 characters of letters and numbers");
        });
    }
}