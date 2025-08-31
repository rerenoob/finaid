using FluentValidation;
using finaid.Models.FAFSA;

namespace finaid.Validators.FAFSA;

public class FinancialInformationValidator : AbstractValidator<FinancialInformation>
{
    public FinancialInformationValidator()
    {
        // Tax Year Validation
        RuleFor(x => x.TaxYear)
            .NotEmpty().WithMessage("Tax year is required")
            .InclusiveBetween(DateTime.Now.Year - 3, DateTime.Now.Year - 1)
            .WithMessage($"Tax year must be between {DateTime.Now.Year - 3} and {DateTime.Now.Year - 1}");

        // Student Financial Information
        RuleFor(x => x.StudentAdjustedGrossIncome)
            .GreaterThanOrEqualTo(0).WithMessage("Adjusted Gross Income cannot be negative")
            .LessThanOrEqualTo(9999999.99m).WithMessage("Adjusted Gross Income seems unusually high")
            .When(x => x.StudentAdjustedGrossIncome.HasValue);

        RuleFor(x => x.StudentIncomeTax)
            .GreaterThanOrEqualTo(0).WithMessage("Income tax cannot be negative")
            .LessThanOrEqualTo(x => x.StudentAdjustedGrossIncome ?? 0)
            .WithMessage("Income tax cannot exceed Adjusted Gross Income")
            .When(x => x.StudentIncomeTax.HasValue);

        RuleFor(x => x.StudentUntaxedIncome)
            .GreaterThanOrEqualTo(0).WithMessage("Untaxed income cannot be negative")
            .LessThanOrEqualTo(9999999.99m).WithMessage("Untaxed income seems unusually high")
            .When(x => x.StudentUntaxedIncome.HasValue);

        RuleFor(x => x.StudentCashSavingsChecking)
            .GreaterThanOrEqualTo(0).WithMessage("Cash, savings and checking cannot be negative")
            .LessThanOrEqualTo(99999999.99m).WithMessage("Cash, savings and checking seems unusually high")
            .When(x => x.StudentCashSavingsChecking.HasValue);

        RuleFor(x => x.StudentInvestmentValue)
            .GreaterThanOrEqualTo(0).WithMessage("Investment value cannot be negative")
            .LessThanOrEqualTo(99999999.99m).WithMessage("Investment value seems unusually high")
            .When(x => x.StudentInvestmentValue.HasValue);

        RuleFor(x => x.StudentBusinessFarmValue)
            .GreaterThanOrEqualTo(0).WithMessage("Business/farm value cannot be negative")
            .LessThanOrEqualTo(99999999.99m).WithMessage("Business/farm value seems unusually high")
            .When(x => x.StudentBusinessFarmValue.HasValue);

        // Tax Filing Consistency
        RuleFor(x => x)
            .Must(HaveConsistentStudentTaxInfo)
            .WithMessage("Tax filing information is inconsistent")
            .WithName("Student Tax Information");

        // Parent Financial Information (for dependent students)
        RuleFor(x => x.ParentAdjustedGrossIncome)
            .GreaterThanOrEqualTo(0).WithMessage("Parent Adjusted Gross Income cannot be negative")
            .LessThanOrEqualTo(9999999.99m).WithMessage("Parent Adjusted Gross Income seems unusually high")
            .When(x => x.ParentAdjustedGrossIncome.HasValue);

        RuleFor(x => x.ParentIncomeTax)
            .GreaterThanOrEqualTo(0).WithMessage("Parent income tax cannot be negative")
            .LessThanOrEqualTo(x => x.ParentAdjustedGrossIncome ?? 0)
            .WithMessage("Parent income tax cannot exceed Parent Adjusted Gross Income")
            .When(x => x.ParentIncomeTax.HasValue);

        RuleFor(x => x.ParentCashSavingsChecking)
            .GreaterThanOrEqualTo(0).WithMessage("Parent cash, savings and checking cannot be negative")
            .LessThanOrEqualTo(99999999.99m).WithMessage("Parent cash, savings and checking seems unusually high")
            .When(x => x.ParentCashSavingsChecking.HasValue);

        // Spouse Financial Information (for married students)
        RuleFor(x => x.SpouseAdjustedGrossIncome)
            .GreaterThanOrEqualTo(0).WithMessage("Spouse Adjusted Gross Income cannot be negative")
            .LessThanOrEqualTo(9999999.99m).WithMessage("Spouse Adjusted Gross Income seems unusually high")
            .When(x => x.SpouseAdjustedGrossIncome.HasValue);

        // Household Information
        RuleFor(x => x.HouseholdSize)
            .GreaterThanOrEqualTo(1).WithMessage("Household size must be at least 1")
            .LessThanOrEqualTo(15).WithMessage("Household size seems unusually large");

        RuleFor(x => x.NumberInCollege)
            .GreaterThanOrEqualTo(1).WithMessage("Number in college must be at least 1")
            .LessThanOrEqualTo(x => x.HouseholdSize)
            .WithMessage("Number in college cannot exceed household size");

        // Benefit Amounts
        RuleFor(x => x.StudentSocialSecurityBenefits)
            .GreaterThanOrEqualTo(0).WithMessage("Social Security benefits cannot be negative")
            .LessThanOrEqualTo(999999.99m).WithMessage("Social Security benefits seems unusually high")
            .When(x => x.StudentSocialSecurityBenefits.HasValue);

        RuleFor(x => x.StudentWelfareTemporaryAssistance)
            .GreaterThanOrEqualTo(0).WithMessage("Welfare/TANF benefits cannot be negative")
            .LessThanOrEqualTo(999999.99m).WithMessage("Welfare/TANF benefits seems unusually high")
            .When(x => x.StudentWelfareTemporaryAssistance.HasValue);

        RuleFor(x => x.StudentVeteransBenefits)
            .GreaterThanOrEqualTo(0).WithMessage("Veterans benefits cannot be negative")
            .LessThanOrEqualTo(999999.99m).WithMessage("Veterans benefits seems unusually high")
            .When(x => x.StudentVeteransBenefits.HasValue);

        // Work-Study and Aid
        RuleFor(x => x.StudentWorkStudy)
            .GreaterThanOrEqualTo(0).WithMessage("Work-study cannot be negative")
            .LessThanOrEqualTo(50000m).WithMessage("Work-study amount seems unusually high")
            .When(x => x.StudentWorkStudy.HasValue);

        RuleFor(x => x.StudentAssistantshipStipend)
            .GreaterThanOrEqualTo(0).WithMessage("Assistantship stipend cannot be negative")
            .LessThanOrEqualTo(100000m).WithMessage("Assistantship stipend seems unusually high")
            .When(x => x.StudentAssistantshipStipend.HasValue);

        // Special Circumstances
        RuleFor(x => x.SpecialCircumstancesExplanation)
            .NotEmpty().WithMessage("Please explain the special circumstances")
            .MinimumLength(10).WithMessage("Please provide more detail about the special circumstances")
            .MaximumLength(1000).WithMessage("Special circumstances explanation is too long")
            .When(x => x.HasSpecialCircumstances);

        // Calculated Fields Validation
        RuleFor(x => x.ExpectedFamilyContribution)
            .GreaterThanOrEqualTo(0).WithMessage("Expected Family Contribution cannot be negative")
            .LessThanOrEqualTo(999999m).WithMessage("Expected Family Contribution seems unusually high")
            .When(x => x.ExpectedFamilyContribution.HasValue);

        RuleFor(x => x.StudentAidIndex)
            .GreaterThanOrEqualTo(-1500).WithMessage("Student Aid Index cannot be less than -1500")
            .LessThanOrEqualTo(999999m).WithMessage("Student Aid Index seems unusually high")
            .When(x => x.StudentAidIndex.HasValue);

        // Verification Deadline
        RuleFor(x => x.VerificationDeadline)
            .GreaterThan(DateTime.Now).WithMessage("Verification deadline must be in the future")
            .When(x => x.VerificationDeadline.HasValue && x.RequiresVerification);
    }

    private static bool HaveConsistentStudentTaxInfo(FinancialInformation financial)
    {
        // If they filed a tax return, they should have AGI
        if (financial.StudentFiledTaxReturn && !financial.StudentAdjustedGrossIncome.HasValue)
        {
            return false;
        }

        // If they didn't file and won't file, but have significant income, that's inconsistent
        if (!financial.StudentFiledTaxReturn && !financial.StudentWillFileTaxReturn)
        {
            var totalIncome = (financial.StudentAdjustedGrossIncome ?? 0) + 
                             (financial.StudentUntaxedIncome ?? 0);
            
            // If they have significant income but didn't file, might be inconsistent
            // (threshold varies by age and dependency status)
            if (totalIncome > 12950) // Approximate filing threshold
            {
                // This might need additional context about the student's age and dependency
                // For now, we'll allow it but it could trigger a review
            }
        }

        // If they're eligible for 1040EZ, they should have filed
        if (financial.StudentEligibleToFile1040EZ && !financial.StudentFiledTaxReturn)
        {
            return false;
        }

        // If they received a W-2, they likely should have filed
        if (financial.StudentReceivedW2 && !financial.StudentFiledTaxReturn && !financial.StudentWillFileTaxReturn)
        {
            var agi = financial.StudentAdjustedGrossIncome ?? 0;
            if (agi > 5000) // Threshold where filing would likely be required
            {
                return false;
            }
        }

        return true;
    }
}