# Task: Implement Real-time Eligibility Calculation Service

## Overview
- **Parent Feature**: IMPL-002 - Federal API Integration Layer
- **Complexity**: High
- **Estimated Time**: 8 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [ ] 002-api-client-foundation.md: Federal API client working
- [ ] 003-fafsa-data-models.md: FAFSA data models implemented

### External Dependencies
- Federal eligibility calculation APIs (when available)
- EFC (Expected Family Contribution) calculation algorithms
- Pell Grant eligibility determination rules

## Implementation Details
### Files to Create/Modify
- `Services/Eligibility/EligibilityCalculationService.cs`: Main eligibility service
- `Services/Eligibility/IEligibilityService.cs`: Service interface
- `Models/Eligibility/EligibilityRequest.cs`: Input parameters for calculations
- `Models/Eligibility/EligibilityResult.cs`: Calculation results and breakdown
- `Models/Eligibility/AidEstimate.cs`: Estimated financial aid amounts
- `Services/Eligibility/EFCCalculator.cs`: Expected Family Contribution logic
- `Configuration/EligibilitySettings.cs`: Calculation parameters and thresholds
- `Tests/Unit/Services/EligibilityTests.cs`: Comprehensive unit tests

### Code Patterns
- Use factory pattern for different calculation types
- Implement caching for frequently requested calculations
- Follow existing service registration patterns
- Use decimal type for all financial calculations

### Eligibility Service Architecture
```csharp
public interface IEligibilityService
{
    Task<EligibilityResult> CalculateEligibilityAsync(EligibilityRequest request);
    Task<decimal> EstimateExpectedFamilyContributionAsync(FinancialInformation financialInfo);
    Task<List<AidEstimate>> GetAidEstimatesAsync(EligibilityResult eligibility);
    Task<bool> ValidateEligibilityDataAsync(EligibilityRequest request);
}

public class EligibilityResult
{
    public decimal ExpectedFamilyContribution { get; set; }
    public decimal EstimatedPellGrant { get; set; }
    public decimal EstimatedFederalLoans { get; set; }
    public List<string> EligibilityIssues { get; set; } = new();
    public DateTime CalculatedAt { get; set; }
    public bool IsEligibleForAid { get; set; }
}
```

## Acceptance Criteria
- [ ] EFC calculation matches federal methodology
- [ ] Pell Grant eligibility determination accurate
- [ ] Federal loan eligibility properly calculated
- [ ] Service handles incomplete data gracefully
- [ ] Calculation results cached to improve performance
- [ ] Clear explanation of eligibility issues provided
- [ ] Service integrates with federal APIs when available
- [ ] Fallback calculations work when APIs unavailable
- [ ] All financial calculations use proper decimal precision
- [ ] Results include confidence levels and data freshness

## Testing Strategy
- Unit tests: EFC calculations with various income scenarios, edge cases
- Integration tests: Federal API integration, caching behavior
- Manual validation:
  - Test calculations against known FAFSA examples
  - Verify results match federal calculators
  - Test various family income and asset scenarios
  - Confirm caching improves performance
  - Validate error handling for incomplete data

## System Stability
- Graceful fallback when federal APIs unavailable
- Input validation prevents calculation errors
- Caching reduces API load and improves performance
- Comprehensive logging for troubleshooting calculation issues