namespace finaid.Configuration;

/// <summary>
/// Configuration settings for eligibility calculations
/// </summary>
public class EligibilitySettings
{
    public const string SectionName = "Eligibility";
    
    /// <summary>
    /// Current award year for calculations
    /// </summary>
    public int CurrentAwardYear { get; set; } = 2024;
    
    /// <summary>
    /// Maximum Pell Grant amount for the current award year
    /// </summary>
    public decimal MaximumPellGrant { get; set; } = 7395m; // 2024-25 amount
    
    /// <summary>
    /// Minimum Pell Grant amount (typically $600)
    /// </summary>
    public decimal MinimumPellGrant { get; set; } = 600m;
    
    /// <summary>
    /// Maximum Student Aid Index (SAI) for Pell Grant eligibility
    /// </summary>
    public decimal MaximumSAIForPellGrant { get; set; } = 7000m;
    
    /// <summary>
    /// Federal loan limits by grade level and dependency status
    /// </summary>
    public LoanLimits DirectLoanLimits { get; set; } = new();
    
    /// <summary>
    /// Income protection allowances by household size
    /// </summary>
    public Dictionary<int, decimal> IncomeProtectionAllowances { get; set; } = new()
    {
        { 1, 10000m },
        { 2, 16500m },
        { 3, 20500m },
        { 4, 25300m },
        { 5, 29800m },
        { 6, 34800m }
    };
    
    /// <summary>
    /// Asset protection allowance by age (simplified - actual is by age and marital status)
    /// </summary>
    public Dictionary<int, decimal> AssetProtectionAllowances { get; set; } = new()
    {
        { 25, 0m },
        { 30, 2500m },
        { 35, 5000m },
        { 40, 8000m },
        { 45, 11000m },
        { 50, 14500m },
        { 55, 18500m },
        { 60, 23000m },
        { 65, 28000m }
    };
    
    /// <summary>
    /// Parent asset assessment rate (typically 5.64%)
    /// </summary>
    public decimal ParentAssetAssessmentRate { get; set; } = 0.0564m;
    
    /// <summary>
    /// Student asset assessment rate (typically 20%)
    /// </summary>
    public decimal StudentAssetAssessmentRate { get; set; } = 0.20m;
    
    /// <summary>
    /// Parent income assessment rates by income bracket
    /// </summary>
    public List<IncomeAssessmentBracket> ParentIncomeAssessmentRates { get; set; } = new()
    {
        new() { MinIncome = 0, MaxIncome = 19000, Rate = 0.22m },
        new() { MinIncome = 19001, MaxIncome = 23900, Rate = 0.25m },
        new() { MinIncome = 23901, MaxIncome = 29700, Rate = 0.29m },
        new() { MinIncome = 29701, MaxIncome = 35500, Rate = 0.34m },
        new() { MinIncome = 35501, MaxIncome = 41200, Rate = 0.40m },
        new() { MinIncome = 41201, MaxIncome = decimal.MaxValue, Rate = 0.47m }
    };
    
    /// <summary>
    /// Student income assessment rate (typically 50% after allowances)
    /// </summary>
    public decimal StudentIncomeAssessmentRate { get; set; } = 0.50m;
    
    /// <summary>
    /// Student income protection allowance
    /// </summary>
    public decimal StudentIncomeProtectionAllowance { get; set; } = 7600m; // 2024-25 amount
    
    /// <summary>
    /// Employment expense allowance percentage
    /// </summary>
    public decimal EmploymentExpenseAllowanceRate { get; set; } = 0.35m; // 35% of earned income
    
    /// <summary>
    /// Maximum employment expense allowance
    /// </summary>
    public decimal MaximumEmploymentExpenseAllowance { get; set; } = 4000m;
    
    /// <summary>
    /// Age threshold for independent student status
    /// </summary>
    public int IndependentStudentAgeThreshold { get; set; } = 24;
    
    /// <summary>
    /// Cache duration for eligibility results
    /// </summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromHours(24);
    
    /// <summary>
    /// Whether to use federal APIs when available
    /// </summary>
    public bool UseFederalAPIs { get; set; } = true;
    
    /// <summary>
    /// Timeout for federal API calls
    /// </summary>
    public TimeSpan FederalAPITimeout { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Federal Direct Loan limits by grade level and dependency status
/// </summary>
public class LoanLimits
{
    /// <summary>
    /// Dependent undergraduate loan limits by year in school
    /// </summary>
    public Dictionary<int, decimal> DependentUndergraduate { get; set; } = new()
    {
        { 1, 5500m }, // First year
        { 2, 6500m }, // Second year
        { 3, 7500m }, // Third year
        { 4, 7500m }, // Fourth year
        { 5, 7500m }  // Fifth year+
    };
    
    /// <summary>
    /// Independent undergraduate loan limits by year in school
    /// </summary>
    public Dictionary<int, decimal> IndependentUndergraduate { get; set; } = new()
    {
        { 1, 9500m }, // First year
        { 2, 10500m }, // Second year
        { 3, 12500m }, // Third year
        { 4, 12500m }, // Fourth year
        { 5, 12500m }  // Fifth year+
    };
    
    /// <summary>
    /// Graduate/Professional loan limits
    /// </summary>
    public decimal Graduate { get; set; } = 20500m;
    
    /// <summary>
    /// Aggregate loan limits
    /// </summary>
    public AggregateLoansLimits Aggregate { get; set; } = new();
}

/// <summary>
/// Aggregate loan limits
/// </summary>
public class AggregateLoansLimits
{
    public decimal DependentUndergraduate { get; set; } = 31000m;
    public decimal IndependentUndergraduate { get; set; } = 57500m;
    public decimal Graduate { get; set; } = 138500m; // Including undergraduate debt
}

/// <summary>
/// Income assessment bracket for progressive tax-like calculation
/// </summary>
public class IncomeAssessmentBracket
{
    public decimal MinIncome { get; set; }
    public decimal MaxIncome { get; set; }
    public decimal Rate { get; set; }
}