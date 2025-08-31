using finaid.Models.Eligibility;
using finaid.Models.FAFSA;

namespace finaid.Services.Eligibility;

/// <summary>
/// Service for calculating federal financial aid eligibility
/// </summary>
public interface IEligibilityService
{
    /// <summary>
    /// Calculate complete eligibility for a FAFSA application
    /// </summary>
    /// <param name="request">Eligibility calculation request</param>
    /// <returns>Complete eligibility results</returns>
    Task<EligibilityResult> CalculateEligibilityAsync(EligibilityRequest request);
    
    /// <summary>
    /// Estimate Expected Family Contribution (EFC) or Student Aid Index (SAI)
    /// </summary>
    /// <param name="financialInfo">Financial information from FAFSA</param>
    /// <param name="studentInfo">Student demographic information</param>
    /// <param name="awardYear">Award year for calculation</param>
    /// <returns>EFC/SAI amount</returns>
    Task<decimal> EstimateExpectedFamilyContributionAsync(
        FinancialInformation financialInfo, 
        StudentInformation studentInfo, 
        int awardYear);
    
    /// <summary>
    /// Get detailed aid estimates by type
    /// </summary>
    /// <param name="eligibility">Base eligibility results</param>
    /// <returns>List of aid estimates by type</returns>
    Task<List<AidEstimate>> GetAidEstimatesAsync(EligibilityResult eligibility);
    
    /// <summary>
    /// Validate that eligibility data is complete and consistent
    /// </summary>
    /// <param name="request">Eligibility calculation request</param>
    /// <returns>True if data is valid for calculation</returns>
    Task<bool> ValidateEligibilityDataAsync(EligibilityRequest request);
    
    /// <summary>
    /// Get validation errors for eligibility data
    /// </summary>
    /// <param name="request">Eligibility calculation request</param>
    /// <returns>List of validation error messages</returns>
    Task<List<string>> GetValidationErrorsAsync(EligibilityRequest request);
    
    /// <summary>
    /// Determine if a student is considered independent
    /// </summary>
    /// <param name="studentInfo">Student information</param>
    /// <param name="awardYear">Award year</param>
    /// <returns>True if student is independent</returns>
    Task<bool> IsIndependentStudentAsync(StudentInformation studentInfo, int awardYear);
    
    /// <summary>
    /// Calculate Cost of Attendance for specific schools
    /// </summary>
    /// <param name="schoolSelections">List of selected schools</param>
    /// <param name="studentInfo">Student information</param>
    /// <returns>Cost estimates by school</returns>
    Task<Dictionary<string, decimal>> CalculateCostOfAttendanceAsync(
        List<SchoolSelection> schoolSelections, 
        StudentInformation studentInfo);
    
    /// <summary>
    /// Clear cached eligibility results for an application
    /// </summary>
    /// <param name="applicationId">FAFSA application ID</param>
    /// <returns>Task completion</returns>
    Task ClearCacheAsync(Guid applicationId);
    
    /// <summary>
    /// Get eligibility calculation status
    /// </summary>
    /// <param name="applicationId">FAFSA application ID</param>
    /// <returns>Status information</returns>
    Task<EligibilityCalculationStatus> GetCalculationStatusAsync(Guid applicationId);
}

/// <summary>
/// Status of eligibility calculations
/// </summary>
public class EligibilityCalculationStatus
{
    public Guid ApplicationId { get; set; }
    public bool HasCachedResults { get; set; }
    public DateTime? LastCalculatedAt { get; set; }
    public bool IsCalculationInProgress { get; set; }
    public List<string> MissingDataElements { get; set; } = new();
    public double DataCompleteness { get; set; }
}