using System.ComponentModel.DataAnnotations;

namespace finaid.Models.Eligibility;

/// <summary>
/// Estimated financial aid amounts by type
/// </summary>
public class AidEstimate
{
    /// <summary>
    /// Type of financial aid
    /// </summary>
    [Required]
    public AidType AidType { get; set; }
    
    /// <summary>
    /// Estimated amount
    /// </summary>
    public decimal EstimatedAmount { get; set; }
    
    /// <summary>
    /// Minimum possible amount
    /// </summary>
    public decimal MinimumAmount { get; set; }
    
    /// <summary>
    /// Maximum possible amount
    /// </summary>
    public decimal MaximumAmount { get; set; }
    
    /// <summary>
    /// Whether this aid type requires repayment
    /// </summary>
    public bool RequiresRepayment { get; set; }
    
    /// <summary>
    /// Interest rate for loans (if applicable)
    /// </summary>
    public decimal? InterestRate { get; set; }
    
    /// <summary>
    /// Description of the aid type
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Eligibility requirements or notes
    /// </summary>
    public string? EligibilityNotes { get; set; }
    
    /// <summary>
    /// Award year this estimate applies to
    /// </summary>
    public int AwardYear { get; set; }
    
    /// <summary>
    /// Whether this is based on actual data or estimates
    /// </summary>
    public bool IsEstimate { get; set; } = true;
    
    /// <summary>
    /// Confidence level of this estimate (0.0 to 1.0)
    /// </summary>
    public double ConfidenceLevel { get; set; } = 1.0;
}

/// <summary>
/// Types of financial aid
/// </summary>
public enum AidType
{
    /// <summary>
    /// Federal Pell Grant
    /// </summary>
    PellGrant,
    
    /// <summary>
    /// Federal Supplemental Educational Opportunity Grant
    /// </summary>
    SEOG,
    
    /// <summary>
    /// Federal Work-Study Program
    /// </summary>
    WorkStudy,
    
    /// <summary>
    /// Federal Direct Subsidized Loan
    /// </summary>
    DirectSubsidizedLoan,
    
    /// <summary>
    /// Federal Direct Unsubsidized Loan
    /// </summary>
    DirectUnsubsidizedLoan,
    
    /// <summary>
    /// Federal Direct PLUS Loan (for parents)
    /// </summary>
    DirectPLUSLoan,
    
    /// <summary>
    /// Federal Direct Graduate PLUS Loan
    /// </summary>
    DirectGradPLUSLoan,
    
    /// <summary>
    /// Federal Perkins Loan (discontinued but still relevant for existing loans)
    /// </summary>
    PerkinsLoan,
    
    /// <summary>
    /// TEACH Grant
    /// </summary>
    TEACHGrant,
    
    /// <summary>
    /// Iraq and Afghanistan Service Grant
    /// </summary>
    IraqAfghanistanServiceGrant,
    
    /// <summary>
    /// State-based financial aid
    /// </summary>
    StateGrant,
    
    /// <summary>
    /// Institutional aid from the school
    /// </summary>
    InstitutionalAid,
    
    /// <summary>
    /// Private scholarships
    /// </summary>
    PrivateScholarship,
    
    /// <summary>
    /// Other forms of aid
    /// </summary>
    Other
}