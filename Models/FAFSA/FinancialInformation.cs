using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace finaid.Models.FAFSA;

public class FinancialInformation : BaseEntity
{
    [Required]
    public Guid FAFSAApplicationId { get; set; }

    [Required]
    public int TaxYear { get; set; }

    // Student Financial Information
    [Column(TypeName = "decimal(12,2)")]
    public decimal? StudentAdjustedGrossIncome { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? StudentIncomeTax { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? StudentUntaxedIncome { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? StudentAdditionalFinancialInfo { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? StudentCashSavingsChecking { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? StudentInvestmentValue { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? StudentBusinessFarmValue { get; set; }

    // Student Tax Filing Status
    public bool StudentFiledTaxReturn { get; set; } = false;
    public bool StudentWillFileTaxReturn { get; set; } = false;
    public bool StudentEligibleToFile1040EZ { get; set; } = false;
    public bool StudentReceivedW2 { get; set; } = false;

    // Student Benefits
    [Column(TypeName = "decimal(12,2)")]
    public decimal? StudentSocialSecurityBenefits { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? StudentWelfareTemporaryAssistance { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? StudentVeteransBenefits { get; set; }

    // Work-Study and Scholarships
    [Column(TypeName = "decimal(12,2)")]
    public decimal? StudentWorkStudy { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? StudentAssistantshipStipend { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? StudentOtherUntaxedIncome { get; set; }

    // Spouse Information (if married)
    [Column(TypeName = "decimal(12,2)")]
    public decimal? SpouseAdjustedGrossIncome { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? SpouseIncomeTax { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? SpouseUntaxedIncome { get; set; }

    public bool SpouseFiledTaxReturn { get; set; } = false;
    public bool SpouseWillFileTaxReturn { get; set; } = false;

    // Parent Information (for dependent students)
    [Column(TypeName = "decimal(12,2)")]
    public decimal? ParentAdjustedGrossIncome { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? ParentIncomeTax { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? ParentUntaxedIncome { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? ParentAdditionalFinancialInfo { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? ParentCashSavingsChecking { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? ParentInvestmentValue { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? ParentBusinessFarmValue { get; set; }

    // Parent Tax Information
    public bool ParentFiledTaxReturn { get; set; } = false;
    public bool ParentWillFileTaxReturn { get; set; } = false;
    public bool ParentEligibleToFile1040EZ { get; set; } = false;

    // Parent Benefits
    [Column(TypeName = "decimal(12,2)")]
    public decimal? ParentSocialSecurityBenefits { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? ParentWelfareTemporaryAssistance { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? ParentDisabilityBenefits { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? ParentOtherUntaxedIncome { get; set; }

    // Household Information
    public int HouseholdSize { get; set; } = 1;
    public int NumberInCollege { get; set; } = 1;

    // Special Circumstances
    public bool HasSpecialCircumstances { get; set; } = false;
    
    [MaxLength(1000)]
    public string? SpecialCircumstancesExplanation { get; set; }

    // Verification Flags
    public bool RequiresVerification { get; set; } = false;
    public bool TaxTranscriptRequested { get; set; } = false;
    public bool VerificationDocumentsSubmitted { get; set; } = false;
    public DateTime? VerificationDeadline { get; set; }

    // Calculated Fields (populated after submission)
    [Column(TypeName = "decimal(12,2)")]
    public decimal? ExpectedFamilyContribution { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? StudentAidIndex { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? ParentContribution { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? StudentContribution { get; set; }

    // FAFSA Formula Information
    [MaxLength(20)]
    public string? FormulaVersion { get; set; } // Federal Methodology, Institutional Methodology

    public DateTime? CalculatedDate { get; set; }

    [MaxLength(500)]
    public string? CalculationNotes { get; set; }

    // Data Collection Status
    public bool StudentSectionComplete { get; set; } = false;
    public bool SpouseSectionComplete { get; set; } = false;
    public bool ParentSectionComplete { get; set; } = false;
    public bool FinancialSectionComplete { get; set; } = false;

    public DateTime? StudentSectionCompletedAt { get; set; }
    public DateTime? SpouseSectionCompletedAt { get; set; }
    public DateTime? ParentSectionCompletedAt { get; set; }
    public DateTime? FinancialSectionCompletedAt { get; set; }

    // Navigation property
    public Models.Application.FAFSAApplication FAFSAApplication { get; set; } = null!;
}