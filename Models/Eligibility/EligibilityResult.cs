using System.ComponentModel.DataAnnotations;

namespace finaid.Models.Eligibility;

/// <summary>
/// Results of eligibility calculations
/// </summary>
public class EligibilityResult
{
    public Guid ApplicationId { get; set; }
    
    public int AwardYear { get; set; }
    
    /// <summary>
    /// Expected Family Contribution (EFC) - for older FAFSA versions
    /// </summary>
    public decimal ExpectedFamilyContribution { get; set; }
    
    /// <summary>
    /// Student Aid Index (SAI) - for newer FAFSA versions (2024-25 and later)
    /// </summary>
    public decimal StudentAidIndex { get; set; }
    
    /// <summary>
    /// Estimated Pell Grant amount
    /// </summary>
    public decimal EstimatedPellGrant { get; set; }
    
    /// <summary>
    /// Maximum Pell Grant for the award year
    /// </summary>
    public decimal MaximumPellGrant { get; set; }
    
    /// <summary>
    /// Estimated Federal Direct Loan amounts
    /// </summary>
    public decimal EstimatedDirectLoan { get; set; }
    
    /// <summary>
    /// Federal Work-Study eligibility amount
    /// </summary>
    public decimal EstimatedWorkStudy { get; set; }
    
    /// <summary>
    /// Supplemental Educational Opportunity Grant (SEOG) estimate
    /// </summary>
    public decimal EstimatedSEOG { get; set; }
    
    /// <summary>
    /// Total estimated federal aid
    /// </summary>
    public decimal TotalEstimatedAid => EstimatedPellGrant + EstimatedDirectLoan + EstimatedWorkStudy + EstimatedSEOG;
    
    /// <summary>
    /// Whether the student is eligible for federal financial aid
    /// </summary>
    public bool IsEligibleForAid { get; set; }
    
    /// <summary>
    /// Whether the student is considered independent
    /// </summary>
    public bool IsIndependentStudent { get; set; }
    
    /// <summary>
    /// Issues that may affect eligibility
    /// </summary>
    public List<string> EligibilityIssues { get; set; } = new();
    
    /// <summary>
    /// Warnings about data quality or completeness
    /// </summary>
    public List<string> Warnings { get; set; } = new();
    
    /// <summary>
    /// When the calculation was performed
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Confidence level of the calculation (0.0 to 1.0)
    /// </summary>
    public double ConfidenceLevel { get; set; } = 1.0;
    
    /// <summary>
    /// Whether the calculation used federal APIs or fallback methods
    /// </summary>
    public bool UsedFederalAPI { get; set; } = false;
    
    /// <summary>
    /// Detailed breakdown of the calculation (optional)
    /// </summary>
    public EligibilityBreakdown? DetailedBreakdown { get; set; }
    
    /// <summary>
    /// School-specific aid estimates
    /// </summary>
    public List<SchoolAidEstimate> SchoolEstimates { get; set; } = new();
}

/// <summary>
/// Detailed breakdown of eligibility calculations
/// </summary>
public class EligibilityBreakdown
{
    /// <summary>
    /// Parent contribution to EFC/SAI
    /// </summary>
    public decimal ParentContribution { get; set; }
    
    /// <summary>
    /// Student contribution to EFC/SAI
    /// </summary>
    public decimal StudentContribution { get; set; }
    
    /// <summary>
    /// Allowances and adjustments applied
    /// </summary>
    public Dictionary<string, decimal> Allowances { get; set; } = new();
    
    /// <summary>
    /// Income assessment details
    /// </summary>
    public IncomeAssessment IncomeAssessment { get; set; } = new();
    
    /// <summary>
    /// Asset assessment details
    /// </summary>
    public AssetAssessment AssetAssessment { get; set; } = new();
    
    /// <summary>
    /// Dependency status factors
    /// </summary>
    public List<string> DependencyFactors { get; set; } = new();
}

/// <summary>
/// Income assessment breakdown
/// </summary>
public class IncomeAssessment
{
    public decimal AdjustedGrossIncome { get; set; }
    public decimal UntaxedIncome { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal IncomeProtectionAllowance { get; set; }
    public decimal EmploymentExpenseAllowance { get; set; }
    public decimal AvailableIncome { get; set; }
}

/// <summary>
/// Asset assessment breakdown
/// </summary>
public class AssetAssessment
{
    public decimal TotalAssets { get; set; }
    public decimal AssetProtectionAllowance { get; set; }
    public decimal DiscretionaryNetWorth { get; set; }
    public decimal AssetAssessmentRate { get; set; }
    public decimal AssetContribution { get; set; }
}

/// <summary>
/// School-specific aid estimate
/// </summary>
public class SchoolAidEstimate
{
    [Required]
    public string SchoolCode { get; set; } = string.Empty;
    
    public string SchoolName { get; set; } = string.Empty;
    
    public decimal CostOfAttendance { get; set; }
    
    public decimal EstimatedNeed { get; set; }
    
    public decimal EstimatedInstitutionalAid { get; set; }
    
    public decimal NetPrice { get; set; }
    
    public decimal ExpectedOutOfPocketCost { get; set; }
}