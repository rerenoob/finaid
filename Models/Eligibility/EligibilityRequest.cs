using finaid.Models.FAFSA;
using System.ComponentModel.DataAnnotations;

namespace finaid.Models.Eligibility;

/// <summary>
/// Input parameters for eligibility calculations
/// </summary>
public class EligibilityRequest
{
    [Required]
    public Guid ApplicationId { get; set; }
    
    [Required]
    public int AwardYear { get; set; }
    
    [Required]
    public StudentInformation StudentInfo { get; set; } = null!;
    
    [Required]
    public FinancialInformation FinancialInfo { get; set; } = null!;
    
    public List<FamilyInformation> FamilyMembers { get; set; } = new();
    
    public List<SchoolSelection> SchoolSelections { get; set; } = new();
    
    /// <summary>
    /// Whether to use cached results if available
    /// </summary>
    public bool UseCachedResults { get; set; } = true;
    
    /// <summary>
    /// Force recalculation even if cached results exist
    /// </summary>
    public bool ForceRecalculation { get; set; } = false;
    
    /// <summary>
    /// Include detailed breakdown in results
    /// </summary>
    public bool IncludeDetailedBreakdown { get; set; } = false;
    
    /// <summary>
    /// Specific school to calculate aid for (optional)
    /// </summary>
    public string? SpecificSchoolCode { get; set; }
}