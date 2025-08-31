using finaid.Configuration;
using finaid.Models.Eligibility;
using finaid.Models.FAFSA;
using Microsoft.Extensions.Options;

namespace finaid.Services.Eligibility;

/// <summary>
/// Calculator for Expected Family Contribution (EFC) and Student Aid Index (SAI)
/// </summary>
public class EFCCalculator
{
    private readonly EligibilitySettings _settings;
    private readonly ILogger<EFCCalculator> _logger;

    public EFCCalculator(
        IOptions<EligibilitySettings> settings,
        ILogger<EFCCalculator> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Calculate Expected Family Contribution or Student Aid Index
    /// </summary>
    public async Task<EligibilityBreakdown> CalculateEFCAsync(
        StudentInformation student,
        FinancialInformation financial,
        bool isIndependent,
        int awardYear)
    {
        _logger.LogInformation("Starting EFC calculation for student {StudentId}, award year {AwardYear}", 
            student.Id, awardYear);

        var breakdown = new EligibilityBreakdown();

        try
        {
            // Calculate income assessment
            breakdown.IncomeAssessment = await CalculateIncomeAssessmentAsync(
                student, financial, isIndependent);

            // Calculate asset assessment
            breakdown.AssetAssessment = await CalculateAssetAssessmentAsync(
                student, financial, isIndependent);

            // Calculate parent contribution (for dependent students)
            if (!isIndependent)
            {
                breakdown.ParentContribution = await CalculateParentContributionAsync(
                    financial, breakdown.IncomeAssessment, breakdown.AssetAssessment);
            }

            // Calculate student contribution
            breakdown.StudentContribution = await CalculateStudentContributionAsync(
                financial, breakdown.IncomeAssessment, breakdown.AssetAssessment, isIndependent);

            // Set dependency factors
            breakdown.DependencyFactors = GetDependencyFactors(student);

            _logger.LogInformation("EFC calculation completed successfully for student {StudentId}", 
                student.Id);

            return breakdown;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating EFC for student {StudentId}", student.Id);
            throw;
        }
    }

    private async Task<IncomeAssessment> CalculateIncomeAssessmentAsync(
        StudentInformation student,
        FinancialInformation financial,
        bool isIndependent)
    {
        var assessment = new IncomeAssessment();

        // Total income (student + spouse or parent, depending on dependency)
        if (isIndependent)
        {
            assessment.AdjustedGrossIncome = financial.StudentAdjustedGrossIncome ?? 0;
            if (!string.IsNullOrEmpty(student.MaritalStatus) && student.MaritalStatus == "Married")
            {
                assessment.AdjustedGrossIncome += financial.SpouseAdjustedGrossIncome ?? 0;
            }
            assessment.UntaxedIncome = financial.StudentUntaxedIncome ?? 0;
        }
        else
        {
            assessment.AdjustedGrossIncome = financial.ParentAdjustedGrossIncome ?? 0;
            assessment.UntaxedIncome = financial.ParentUntaxedIncome ?? 0;
        }

        assessment.TotalIncome = assessment.AdjustedGrossIncome + assessment.UntaxedIncome;

        // Income protection allowance
        var householdSize = financial.HouseholdSize;
        assessment.IncomeProtectionAllowance = GetIncomeProtectionAllowance(householdSize, isIndependent);

        // Employment expense allowance (35% of earned income, max $4000)
        var earnedIncome = assessment.AdjustedGrossIncome; // Simplified - in reality would exclude investment income
        assessment.EmploymentExpenseAllowance = Math.Min(
            earnedIncome * _settings.EmploymentExpenseAllowanceRate,
            _settings.MaximumEmploymentExpenseAllowance);

        // Available income = Total income - allowances
        assessment.AvailableIncome = Math.Max(0, 
            assessment.TotalIncome - assessment.IncomeProtectionAllowance - assessment.EmploymentExpenseAllowance);

        return await Task.FromResult(assessment);
    }

    private async Task<AssetAssessment> CalculateAssetAssessmentAsync(
        StudentInformation student,
        FinancialInformation financial,
        bool isIndependent)
    {
        var assessment = new AssetAssessment();

        // Total reportable assets
        if (isIndependent)
        {
            assessment.TotalAssets = (financial.StudentCashSavingsChecking ?? 0) +
                                   (financial.StudentInvestmentValue ?? 0) +
                                   (financial.StudentBusinessFarmValue ?? 0);
        }
        else
        {
            assessment.TotalAssets = (financial.ParentCashSavingsChecking ?? 0) +
                                   (financial.ParentInvestmentValue ?? 0) +
                                   (financial.ParentBusinessFarmValue ?? 0);
        }

        // Asset protection allowance (based on older parent's age for married couples)
        var studentAge = CalculateAge(student.DateOfBirth);
        assessment.AssetProtectionAllowance = GetAssetProtectionAllowance(studentAge, isIndependent);

        // Discretionary net worth
        assessment.DiscretionaryNetWorth = Math.Max(0, 
            assessment.TotalAssets - assessment.AssetProtectionAllowance);

        // Asset assessment rate
        assessment.AssetAssessmentRate = isIndependent ? 
            _settings.StudentAssetAssessmentRate : 
            _settings.ParentAssetAssessmentRate;

        // Asset contribution
        assessment.AssetContribution = assessment.DiscretionaryNetWorth * assessment.AssetAssessmentRate;

        return await Task.FromResult(assessment);
    }

    private async Task<decimal> CalculateParentContributionAsync(
        FinancialInformation financial,
        IncomeAssessment incomeAssessment,
        AssetAssessment assetAssessment)
    {
        // Apply progressive income assessment rate
        var incomeContribution = ApplyProgressiveRate(
            incomeAssessment.AvailableIncome, 
            _settings.ParentIncomeAssessmentRates);

        // Add asset contribution
        var totalContribution = incomeContribution + assetAssessment.AssetContribution;

        // Adjust for number of family members in college
        var numberInCollege = Math.Max(1, financial.NumberInCollege);
        var adjustedContribution = totalContribution / numberInCollege;

        return await Task.FromResult(Math.Max(0, adjustedContribution));
    }

    private async Task<decimal> CalculateStudentContributionAsync(
        FinancialInformation financial,
        IncomeAssessment incomeAssessment,
        AssetAssessment assetAssessment,
        bool isIndependent)
    {
        if (!isIndependent)
        {
            // For dependent students, calculate separate student contribution
            var studentIncome = (financial.StudentAdjustedGrossIncome ?? 0) + 
                              (financial.StudentUntaxedIncome ?? 0);
            
            var availableStudentIncome = Math.Max(0, 
                studentIncome - _settings.StudentIncomeProtectionAllowance);
            
            var studentIncomeContribution = availableStudentIncome * _settings.StudentIncomeAssessmentRate;
            
            var studentAssets = (financial.StudentCashSavingsChecking ?? 0) +
                              (financial.StudentInvestmentValue ?? 0) +
                              (financial.StudentBusinessFarmValue ?? 0);
            
            var studentAssetContribution = studentAssets * _settings.StudentAssetAssessmentRate;

            return await Task.FromResult(studentIncomeContribution + studentAssetContribution);
        }

        // For independent students, this is included in the main calculation
        return await Task.FromResult(0m);
    }

    private decimal GetIncomeProtectionAllowance(int householdSize, bool isIndependent)
    {
        if (_settings.IncomeProtectionAllowances.TryGetValue(householdSize, out var allowance))
        {
            return allowance;
        }

        // For household sizes > 6, add additional amount per person
        var baseAllowance = _settings.IncomeProtectionAllowances[6];
        var additionalMembers = householdSize - 6;
        return baseAllowance + (additionalMembers * 4000m); // $4000 per additional member
    }

    private decimal GetAssetProtectionAllowance(int age, bool isIndependent)
    {
        // Students under 25 typically have no asset protection allowance
        if (!isIndependent && age < 25)
        {
            return 0m;
        }

        // Find the closest age bracket
        var applicableAge = _settings.AssetProtectionAllowances.Keys
            .Where(a => a <= age)
            .DefaultIfEmpty(25)
            .Max();

        return _settings.AssetProtectionAllowances.GetValueOrDefault(applicableAge, 0m);
    }

    private decimal ApplyProgressiveRate(decimal income, List<IncomeAssessmentBracket> brackets)
    {
        decimal totalAssessment = 0;
        decimal remainingIncome = income;

        foreach (var bracket in brackets.OrderBy(b => b.MinIncome))
        {
            if (remainingIncome <= 0) break;

            var bracketWidth = Math.Min(bracket.MaxIncome - bracket.MinIncome + 1, decimal.MaxValue);
            var incomeInBracket = Math.Min(remainingIncome, bracketWidth);

            totalAssessment += incomeInBracket * bracket.Rate;
            remainingIncome -= incomeInBracket;

            if (bracket.MaxIncome == decimal.MaxValue) break;
        }

        return totalAssessment;
    }

    private int CalculateAge(DateTime dateOfBirth)
    {
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;
        
        if (dateOfBirth.Date > today.AddYears(-age))
        {
            age--;
        }

        return age;
    }

    private List<string> GetDependencyFactors(StudentInformation student)
    {
        var factors = new List<string>();

        var age = CalculateAge(student.DateOfBirth);
        
        if (age >= _settings.IndependentStudentAgeThreshold)
            factors.Add($"Age {age} (24 or older)");
        
        if (student.IsVeteran)
            factors.Add("Veteran status");
            
        if (student.HasMilitaryService)
            factors.Add("Active military service");
            
        if (student.MaritalStatus == "Married")
            factors.Add("Married status");
            
        if (student.IsOrphan)
            factors.Add("Orphan or ward of court");
            
        if (student.IsWardOfCourt)
            factors.Add("Ward of court");
            
        if (student.WasInFosterCare)
            factors.Add("Foster care after age 13");
            
        if (student.IsEmancipatedMinor)
            factors.Add("Emancipated minor");
            
        if (student.IsHomeless || student.AtRiskOfHomelessness)
            factors.Add("Homeless or at risk of homelessness");
            
        if (student.HasDependencyOverride)
            factors.Add("Dependency override approved");

        if (!factors.Any())
            factors.Add("Dependent student (no independent factors)");

        return factors;
    }
}