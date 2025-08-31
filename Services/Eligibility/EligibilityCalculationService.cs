using finaid.Configuration;
using finaid.Models.Eligibility;
using finaid.Models.FAFSA;
using finaid.Services.Federal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using FluentValidation;

namespace finaid.Services.Eligibility;

/// <summary>
/// Main service for calculating federal financial aid eligibility
/// </summary>
public class EligibilityCalculationService : IEligibilityService
{
    private readonly EFCCalculator _efcCalculator;
    private readonly IFederalApiClient _federalApiClient;
    private readonly IMemoryCache _cache;
    private readonly EligibilitySettings _settings;
    private readonly IValidator<EligibilityRequest> _validator;
    private readonly ILogger<EligibilityCalculationService> _logger;

    public EligibilityCalculationService(
        EFCCalculator efcCalculator,
        IFederalApiClient federalApiClient,
        IMemoryCache memoryCache,
        IOptions<EligibilitySettings> settings,
        IValidator<EligibilityRequest> validator,
        ILogger<EligibilityCalculationService> logger)
    {
        _efcCalculator = efcCalculator;
        _federalApiClient = federalApiClient;
        _cache = memoryCache;
        _settings = settings.Value;
        _validator = validator;
        _logger = logger;
    }

    public async Task<EligibilityResult> CalculateEligibilityAsync(EligibilityRequest request)
    {
        _logger.LogInformation("Starting eligibility calculation for application {ApplicationId}", 
            request.ApplicationId);

        // Validate request
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new ArgumentException($"Invalid eligibility request: {errors}");
        }

        // Check cache first
        var cacheKey = GetCacheKey(request.ApplicationId);
        if (request.UseCachedResults && !request.ForceRecalculation 
            && _cache.TryGetValue(cacheKey, out EligibilityResult? cachedResult))
        {
            _logger.LogInformation("Returning cached eligibility result for application {ApplicationId}", 
                request.ApplicationId);
            return cachedResult!;
        }

        try
        {
            var result = new EligibilityResult
            {
                ApplicationId = request.ApplicationId,
                AwardYear = request.AwardYear,
                CalculatedAt = DateTime.UtcNow
            };

            // Determine if student is independent
            result.IsIndependentStudent = await IsIndependentStudentAsync(request.StudentInfo, request.AwardYear);

            // Validate data completeness
            var dataIssues = await GetValidationErrorsAsync(request);
            result.EligibilityIssues.AddRange(dataIssues);

            // Calculate confidence level based on data completeness
            result.ConfidenceLevel = CalculateConfidenceLevel(request, dataIssues);

            // Try federal API first if enabled and data is complete
            var usedFederalApi = false;
            if (_settings.UseFederalAPIs && dataIssues.Count == 0)
            {
                try
                {
                    var apiResult = await TryFederalApiCalculationAsync(request);
                    if (apiResult != null)
                    {
                        ApplyFederalApiResults(result, apiResult);
                        usedFederalApi = true;
                        result.ConfidenceLevel = 1.0;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Federal API calculation failed, falling back to local calculation");
                    result.Warnings.Add("Federal API unavailable, using estimated calculations");
                }
            }

            result.UsedFederalAPI = usedFederalApi;

            // Perform local calculation if federal API wasn't used
            if (!usedFederalApi)
            {
                await PerformLocalCalculationAsync(request, result);
            }

            // Calculate detailed aid estimates
            var aidEstimates = await GetAidEstimatesAsync(result);
            PopulateAidEstimates(result, aidEstimates);

            // Calculate school-specific estimates
            if (request.SchoolSelections.Any())
            {
                result.SchoolEstimates = await CalculateSchoolEstimatesAsync(request, result);
            }

            // Add detailed breakdown if requested
            if (request.IncludeDetailedBreakdown && !usedFederalApi)
            {
                result.DetailedBreakdown = await _efcCalculator.CalculateEFCAsync(
                    request.StudentInfo, 
                    request.FinancialInfo, 
                    result.IsIndependentStudent, 
                    request.AwardYear);
            }

            // Cache the result
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(_settings.CacheDuration)
                .SetPriority(CacheItemPriority.Normal);

            _cache.Set(cacheKey, result, cacheEntryOptions);

            _logger.LogInformation("Eligibility calculation completed for application {ApplicationId}. " +
                                 "EFC/SAI: {EFC}, Estimated Pell: {Pell}, Total Aid: {TotalAid}",
                request.ApplicationId, result.StudentAidIndex, result.EstimatedPellGrant, result.TotalEstimatedAid);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating eligibility for application {ApplicationId}", 
                request.ApplicationId);
            throw;
        }
    }

    public async Task<decimal> EstimateExpectedFamilyContributionAsync(
        FinancialInformation financialInfo, 
        StudentInformation studentInfo, 
        int awardYear)
    {
        var isIndependent = await IsIndependentStudentAsync(studentInfo, awardYear);
        var breakdown = await _efcCalculator.CalculateEFCAsync(
            studentInfo, financialInfo, isIndependent, awardYear);

        return breakdown.ParentContribution + breakdown.StudentContribution;
    }

    public async Task<List<AidEstimate>> GetAidEstimatesAsync(EligibilityResult eligibility)
    {
        var estimates = new List<AidEstimate>();

        // Pell Grant
        if (eligibility.EstimatedPellGrant > 0)
        {
            estimates.Add(new AidEstimate
            {
                AidType = AidType.PellGrant,
                EstimatedAmount = eligibility.EstimatedPellGrant,
                MinimumAmount = Math.Max(0, eligibility.EstimatedPellGrant * 0.8m),
                MaximumAmount = _settings.MaximumPellGrant,
                RequiresRepayment = false,
                Description = "Federal Pell Grant",
                EligibilityNotes = "Based on Expected Family Contribution/Student Aid Index",
                AwardYear = eligibility.AwardYear,
                ConfidenceLevel = eligibility.ConfidenceLevel
            });
        }

        // Federal Direct Loans
        if (eligibility.EstimatedDirectLoan > 0)
        {
            var loanType = eligibility.StudentAidIndex <= 5000 ? 
                AidType.DirectSubsidizedLoan : 
                AidType.DirectUnsubsidizedLoan;

            estimates.Add(new AidEstimate
            {
                AidType = loanType,
                EstimatedAmount = eligibility.EstimatedDirectLoan,
                MinimumAmount = eligibility.EstimatedDirectLoan,
                MaximumAmount = GetMaximumLoanAmount(eligibility.IsIndependentStudent, 1), // Assume first year
                RequiresRepayment = true,
                InterestRate = loanType == AidType.DirectSubsidizedLoan ? 0.0536m : 0.0536m, // 2024-25 rates
                Description = loanType == AidType.DirectSubsidizedLoan ? 
                    "Federal Direct Subsidized Loan" : 
                    "Federal Direct Unsubsidized Loan",
                AwardYear = eligibility.AwardYear,
                ConfidenceLevel = eligibility.ConfidenceLevel
            });
        }

        // SEOG
        if (eligibility.EstimatedSEOG > 0)
        {
            estimates.Add(new AidEstimate
            {
                AidType = AidType.SEOG,
                EstimatedAmount = eligibility.EstimatedSEOG,
                MinimumAmount = 100m,
                MaximumAmount = 4000m,
                RequiresRepayment = false,
                Description = "Federal Supplemental Educational Opportunity Grant",
                EligibilityNotes = "Limited funds available, priority to Pell Grant recipients",
                AwardYear = eligibility.AwardYear,
                ConfidenceLevel = eligibility.ConfidenceLevel * 0.7 // Lower confidence due to limited funds
            });
        }

        // Work-Study
        if (eligibility.EstimatedWorkStudy > 0)
        {
            estimates.Add(new AidEstimate
            {
                AidType = AidType.WorkStudy,
                EstimatedAmount = eligibility.EstimatedWorkStudy,
                MinimumAmount = 1000m,
                MaximumAmount = 5000m,
                RequiresRepayment = false,
                Description = "Federal Work-Study Program",
                EligibilityNotes = "Part-time employment opportunity, actual amount depends on hours worked",
                AwardYear = eligibility.AwardYear,
                ConfidenceLevel = eligibility.ConfidenceLevel * 0.6 // Lower confidence due to employment requirement
            });
        }

        return await Task.FromResult(estimates);
    }

    public async Task<bool> ValidateEligibilityDataAsync(EligibilityRequest request)
    {
        var errors = await GetValidationErrorsAsync(request);
        return errors.Count == 0;
    }

    public async Task<List<string>> GetValidationErrorsAsync(EligibilityRequest request)
    {
        var errors = new List<string>();

        // Validate required student information
        if (string.IsNullOrEmpty(request.StudentInfo.FirstName))
            errors.Add("Student first name is required");

        if (string.IsNullOrEmpty(request.StudentInfo.LastName))
            errors.Add("Student last name is required");

        if (request.StudentInfo.DateOfBirth == default)
            errors.Add("Student date of birth is required");

        if (string.IsNullOrEmpty(request.StudentInfo.EncryptedSSN))
            errors.Add("Student SSN is required");

        // Validate financial information
        if (request.FinancialInfo.TaxYear < DateTime.Now.Year - 5)
            errors.Add("Tax year is too old");

        if (request.FinancialInfo.HouseholdSize < 1)
            errors.Add("Household size must be at least 1");

        if (request.FinancialInfo.NumberInCollege < 1)
            errors.Add("Number in college must be at least 1");

        if (request.FinancialInfo.NumberInCollege > request.FinancialInfo.HouseholdSize)
            errors.Add("Number in college cannot exceed household size");

        // Check for parent information if student is dependent
        var isIndependent = await IsIndependentStudentAsync(request.StudentInfo, request.AwardYear);
        if (!isIndependent)
        {
            if (!request.FinancialInfo.ParentAdjustedGrossIncome.HasValue)
                errors.Add("Parent income information is required for dependent students");
        }

        return errors;
    }

    public async Task<bool> IsIndependentStudentAsync(StudentInformation studentInfo, int awardYear)
    {
        var age = CalculateAge(studentInfo.DateOfBirth);

        // Age 24 or older
        if (age >= _settings.IndependentStudentAgeThreshold)
            return true;

        // Married
        if (studentInfo.MaritalStatus == "Married")
            return true;

        // Veteran or active military
        if (studentInfo.IsVeteran || studentInfo.HasMilitaryService)
            return true;

        // Orphan or ward of court
        if (studentInfo.IsOrphan || studentInfo.IsWardOfCourt)
            return true;

        // Foster care after age 13
        if (studentInfo.WasInFosterCare)
            return true;

        // Emancipated minor
        if (studentInfo.IsEmancipatedMinor)
            return true;

        // Homeless or at risk
        if (studentInfo.IsHomeless || studentInfo.AtRiskOfHomelessness)
            return true;

        // Dependency override
        if (studentInfo.HasDependencyOverride)
            return true;

        // Graduate student (would need additional logic to determine)
        // For now, assume undergraduate unless specified

        return await Task.FromResult(false);
    }

    public async Task<Dictionary<string, decimal>> CalculateCostOfAttendanceAsync(
        List<SchoolSelection> schoolSelections, 
        StudentInformation studentInfo)
    {
        var costs = new Dictionary<string, decimal>();

        foreach (var school in schoolSelections)
        {
            var totalCost = (school.TuitionAndFees ?? 0) +
                           (school.RoomAndBoard ?? 0) +
                           (school.BooksAndSupplies ?? 0) +
                           (school.PersonalExpenses ?? 0) +
                           (school.TransportationCosts ?? 0);

            costs[school.FederalSchoolCode] = totalCost;
        }

        return await Task.FromResult(costs);
    }

    public async Task ClearCacheAsync(Guid applicationId)
    {
        var cacheKey = GetCacheKey(applicationId);
        _cache.Remove(cacheKey);
        await Task.CompletedTask;
    }

    public async Task<EligibilityCalculationStatus> GetCalculationStatusAsync(Guid applicationId)
    {
        var cacheKey = GetCacheKey(applicationId);
        var hasCachedResults = _cache.TryGetValue(cacheKey, out _);

        // In a real implementation, you might check a database for calculation status
        return await Task.FromResult(new EligibilityCalculationStatus
        {
            ApplicationId = applicationId,
            HasCachedResults = hasCachedResults,
            LastCalculatedAt = hasCachedResults ? DateTime.UtcNow : null, // Simplified
            IsCalculationInProgress = false, // Would track actual calculation state
            MissingDataElements = new List<string>(),
            DataCompleteness = 1.0 // Would calculate based on actual data
        });
    }

    // Private helper methods

    private async Task<object?> TryFederalApiCalculationAsync(EligibilityRequest request)
    {
        try
        {
            // This would integrate with actual federal APIs when available
            // For now, return null to indicate API is not available
            return await Task.FromResult<object?>(null);
        }
        catch
        {
            return null;
        }
    }

    private void ApplyFederalApiResults(EligibilityResult result, object apiResult)
    {
        // This would parse and apply results from federal APIs
        // Implementation depends on actual API response format
    }

    private async Task PerformLocalCalculationAsync(EligibilityRequest request, EligibilityResult result)
    {
        // Calculate EFC/SAI
        var breakdown = await _efcCalculator.CalculateEFCAsync(
            request.StudentInfo,
            request.FinancialInfo,
            result.IsIndependentStudent,
            request.AwardYear);

        var totalContribution = breakdown.ParentContribution + breakdown.StudentContribution;
        
        // For 2024-25 and later, use Student Aid Index (SAI)
        result.StudentAidIndex = totalContribution;
        result.ExpectedFamilyContribution = totalContribution; // Keep for backward compatibility

        // Calculate Pell Grant eligibility
        if (result.StudentAidIndex <= _settings.MaximumSAIForPellGrant)
        {
            var pellFormula = _settings.MaximumPellGrant - (result.StudentAidIndex * 0.33m);
            result.EstimatedPellGrant = Math.Max(_settings.MinimumPellGrant, 
                Math.Min(_settings.MaximumPellGrant, pellFormula));
        }

        // Calculate loan eligibility
        var maxLoan = GetMaximumLoanAmount(result.IsIndependentStudent, 1); // Assume first year
        result.EstimatedDirectLoan = maxLoan;

        // Calculate SEOG (limited funds, Pell recipients get priority)
        if (result.EstimatedPellGrant > 0 && result.StudentAidIndex <= 3000)
        {
            result.EstimatedSEOG = Math.Min(2000m, _settings.MaximumPellGrant - result.EstimatedPellGrant);
        }

        // Calculate Work-Study eligibility
        if (result.StudentAidIndex <= 10000)
        {
            result.EstimatedWorkStudy = 2500m; // Typical work-study award
        }

        result.IsEligibleForAid = result.TotalEstimatedAid > 0;
        result.MaximumPellGrant = _settings.MaximumPellGrant;
    }

    private void PopulateAidEstimates(EligibilityResult result, List<AidEstimate> estimates)
    {
        // Estimates are already calculated in GetAidEstimatesAsync
        // This method could be used to add the estimates to the result object
        // if we wanted to include them directly in EligibilityResult
    }

    private async Task<List<SchoolAidEstimate>> CalculateSchoolEstimatesAsync(
        EligibilityRequest request, 
        EligibilityResult result)
    {
        var estimates = new List<SchoolAidEstimate>();

        foreach (var school in request.SchoolSelections)
        {
            var costOfAttendance = school.EstimatedCostOfAttendance ?? 0;
            var estimatedNeed = Math.Max(0, costOfAttendance - result.StudentAidIndex);
            var institutionalAid = school.InstitutionalAidAmount ?? 0;
            var totalFederalAid = result.TotalEstimatedAid;

            estimates.Add(new SchoolAidEstimate
            {
                SchoolCode = school.FederalSchoolCode,
                SchoolName = school.SchoolName,
                CostOfAttendance = costOfAttendance,
                EstimatedNeed = estimatedNeed,
                EstimatedInstitutionalAid = institutionalAid,
                NetPrice = Math.Max(0, costOfAttendance - totalFederalAid - institutionalAid),
                ExpectedOutOfPocketCost = Math.Max(0, costOfAttendance - totalFederalAid - institutionalAid - result.StudentAidIndex)
            });
        }

        return await Task.FromResult(estimates);
    }

    private double CalculateConfidenceLevel(EligibilityRequest request, List<string> issues)
    {
        if (issues.Count == 0)
            return 1.0;

        // Reduce confidence based on number of issues
        var reduction = Math.Min(0.8, issues.Count * 0.1);
        return Math.Max(0.2, 1.0 - reduction);
    }

    private decimal GetMaximumLoanAmount(bool isIndependent, int yearInSchool)
    {
        var limits = isIndependent ? 
            _settings.DirectLoanLimits.IndependentUndergraduate : 
            _settings.DirectLoanLimits.DependentUndergraduate;

        return limits.GetValueOrDefault(yearInSchool, limits.Values.LastOrDefault());
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

    private string GetCacheKey(Guid applicationId)
    {
        return $"eligibility_{applicationId}";
    }
}