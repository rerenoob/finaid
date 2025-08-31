using finaid.Data;
using finaid.Models.Application;
using finaid.Models.FAFSA;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace finaid.Services.FAFSA;

/// <summary>
/// Service for validating FAFSA applications before submission
/// </summary>
public class FAFSAValidationService
{
    private readonly ApplicationDbContext _context;
    private readonly IValidator<StudentInformation> _studentValidator;
    private readonly IValidator<FinancialInformation> _financialValidator;
    private readonly IValidator<FamilyInformation> _familyValidator;
    private readonly IValidator<SchoolSelection> _schoolValidator;
    private readonly ILogger<FAFSAValidationService> _logger;

    public FAFSAValidationService(
        ApplicationDbContext context,
        IValidator<StudentInformation> studentValidator,
        IValidator<FinancialInformation> financialValidator,
        IValidator<FamilyInformation> familyValidator,
        IValidator<SchoolSelection> schoolValidator,
        ILogger<FAFSAValidationService> logger)
    {
        _context = context;
        _studentValidator = studentValidator;
        _financialValidator = financialValidator;
        _familyValidator = familyValidator;
        _schoolValidator = schoolValidator;
        _logger = logger;
    }

    /// <summary>
    /// Validate a FAFSA application for submission readiness
    /// </summary>
    public async Task<ValidationResult> ValidateForSubmissionAsync(Guid applicationId)
    {
        _logger.LogInformation("Validating FAFSA application {ApplicationId} for submission", applicationId);

        var validationResult = new ValidationResult();

        try
        {
            // Load the application
            var application = await _context.Set<FAFSAApplication>()
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null)
            {
                validationResult.Errors.Add(new ValidationFailure("Application", "Application not found"));
                return validationResult;
            }

            // Parse form data
            var formData = ParseFormData(application.FormDataJson);
            if (formData == null)
            {
                validationResult.Errors.Add(new ValidationFailure("FormData", "Invalid or missing form data"));
                return validationResult;
            }

            // Validate basic application requirements
            await ValidateBasicRequirements(application, validationResult);

            // Validate student information
            if (formData.StudentInfo != null)
            {
                var studentValidation = await _studentValidator.ValidateAsync(formData.StudentInfo);
                if (!studentValidation.IsValid)
                {
                    foreach (var error in studentValidation.Errors)
                    {
                        validationResult.Errors.Add(new ValidationFailure($"Student.{error.PropertyName}", error.ErrorMessage));
                    }
                }
            }
            else
            {
                validationResult.Errors.Add(new ValidationFailure("Student", "Student information is required"));
            }

            // Validate financial information
            if (formData.FinancialInfo != null)
            {
                var financialValidation = await _financialValidator.ValidateAsync(formData.FinancialInfo);
                if (!financialValidation.IsValid)
                {
                    foreach (var error in financialValidation.Errors)
                    {
                        validationResult.Errors.Add(new ValidationFailure($"Financial.{error.PropertyName}", error.ErrorMessage));
                    }
                }
            }
            else
            {
                validationResult.Errors.Add(new ValidationFailure("Financial", "Financial information is required"));
            }

            // Validate family members (for dependent students)
            if (formData.StudentInfo != null && !IsIndependentStudent(formData.StudentInfo))
            {
                await ValidateFamilyInformation(formData.FamilyMembers, validationResult);
            }

            // Validate school selections
            if (formData.SchoolSelections.Any())
            {
                await ValidateSchoolSelections(formData.SchoolSelections, validationResult);
            }
            else
            {
                validationResult.Errors.Add(new ValidationFailure("Schools", "At least one school selection is required"));
            }

            // Validate business rules
            await ValidateBusinessRules(formData, validationResult);

            // Validate signatures and authentication requirements
            await ValidateSignatureRequirements(application, formData, validationResult);

            _logger.LogInformation("FAFSA validation completed for application {ApplicationId}. " +
                                 "Valid: {IsValid}, Errors: {ErrorCount}",
                applicationId, validationResult.IsValid, validationResult.Errors.Count);

            return validationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating FAFSA application {ApplicationId}", applicationId);
            validationResult.Errors.Add(new ValidationFailure("System", "An error occurred during validation"));
            return validationResult;
        }
    }

    /// <summary>
    /// Quick validation check to see if application is potentially submittable
    /// </summary>
    public async Task<bool> IsReadyForSubmissionAsync(Guid applicationId)
    {
        var result = await ValidateForSubmissionAsync(applicationId);
        return result.IsValid;
    }

    /// <summary>
    /// Get a summary of validation issues
    /// </summary>
    public async Task<ValidationSummary> GetValidationSummaryAsync(Guid applicationId)
    {
        var result = await ValidateForSubmissionAsync(applicationId);
        
        return new ValidationSummary
        {
            ApplicationId = applicationId,
            IsValid = result.IsValid,
            TotalErrors = result.Errors.Count,
            ErrorsBySection = result.Errors
                .GroupBy(e => GetSectionFromPropertyName(e.PropertyName))
                .ToDictionary(g => g.Key, g => g.Count()),
            CriticalErrors = result.Errors
                .Where(e => IsCriticalError(e.ErrorMessage))
                .Select(e => e.ErrorMessage)
                .ToList(),
            Warnings = result.Errors
                .Where(e => !IsCriticalError(e.ErrorMessage))
                .Select(e => e.ErrorMessage)
                .ToList(),
            CompletionPercentage = CalculateCompletionPercentage(result)
        };
    }

    // Private helper methods

    private FAFSAFormData? ParseFormData(string? formDataJson)
    {
        if (string.IsNullOrEmpty(formDataJson))
            return null;

        try
        {
            // This would parse the JSON into structured FAFSA data
            // For now, return a mock structure
            return JsonSerializer.Deserialize<FAFSAFormData>(formDataJson);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing form data JSON");
            return null;
        }
    }

    private async Task ValidateBasicRequirements(FAFSAApplication application, ValidationResult validationResult)
    {
        // Check award year
        var currentYear = DateTime.Now.Year;
        if (application.AwardYear < currentYear || application.AwardYear > currentYear + 2)
        {
            validationResult.Errors.Add(new ValidationFailure("AwardYear", 
                $"Award year {application.AwardYear} is not valid"));
        }

        // Check application status
        if (application.Status == ApplicationStatus.Submitted)
        {
            validationResult.Errors.Add(new ValidationFailure("Status", 
                "Application has already been submitted"));
        }

        // Check user association
        if (application.UserId == Guid.Empty)
        {
            validationResult.Errors.Add(new ValidationFailure("UserId", 
                "Application must be associated with a user"));
        }

        await Task.CompletedTask;
    }

    private bool IsIndependentStudent(StudentInformation student)
    {
        var age = DateTime.Now.Year - student.DateOfBirth.Year;
        if (student.DateOfBirth > DateTime.Now.AddYears(-age)) age--;

        return age >= 24 ||
               student.IsVeteran ||
               student.HasMilitaryService ||
               student.MaritalStatus == "Married" ||
               student.IsOrphan ||
               student.IsWardOfCourt ||
               student.WasInFosterCare ||
               student.IsEmancipatedMinor ||
               student.IsHomeless ||
               student.AtRiskOfHomelessness ||
               student.HasDependencyOverride;
    }

    private async Task ValidateFamilyInformation(List<FamilyInformation> familyMembers, ValidationResult validationResult)
    {
        var hasParent1 = familyMembers.Any(f => f.RelationshipType == "Parent1");
        var hasParent2 = familyMembers.Any(f => f.RelationshipType == "Parent2");

        if (!hasParent1)
        {
            validationResult.Errors.Add(new ValidationFailure("Family", 
                "At least one parent's information is required for dependent students"));
        }

        foreach (var family in familyMembers)
        {
            var familyValidation = await _familyValidator.ValidateAsync(family);
            if (!familyValidation.IsValid)
            {
                foreach (var error in familyValidation.Errors)
                {
                    validationResult.Errors.Add(new ValidationFailure($"Family.{error.PropertyName}", 
                        error.ErrorMessage));
                }
            }
        }
    }

    private async Task ValidateSchoolSelections(List<SchoolSelection> schoolSelections, ValidationResult validationResult)
    {
        if (schoolSelections.Count > 10)
        {
            validationResult.Errors.Add(new ValidationFailure("Schools", 
                "Maximum of 10 schools can be selected"));
        }

        var duplicateCodes = schoolSelections
            .GroupBy(s => s.FederalSchoolCode)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var duplicateCode in duplicateCodes)
        {
            validationResult.Errors.Add(new ValidationFailure("Schools", 
                $"Duplicate school code: {duplicateCode}"));
        }

        foreach (var school in schoolSelections)
        {
            var schoolValidation = await _schoolValidator.ValidateAsync(school);
            if (!schoolValidation.IsValid)
            {
                foreach (var error in schoolValidation.Errors)
                {
                    validationResult.Errors.Add(new ValidationFailure($"School.{error.PropertyName}", 
                        error.ErrorMessage));
                }
            }
        }
    }

    private Task ValidateBusinessRules(FAFSAFormData formData, ValidationResult validationResult)
    {
        // Validate consistency between sections
        if (formData.StudentInfo != null && formData.FinancialInfo != null)
        {
            // Check if financial information is consistent with student status
            var isIndependent = IsIndependentStudent(formData.StudentInfo);
            
            if (!isIndependent && !formData.FinancialInfo.ParentAdjustedGrossIncome.HasValue)
            {
                validationResult.Errors.Add(new ValidationFailure("Financial", 
                    "Parent financial information is required for dependent students"));
            }

            if (formData.StudentInfo.MaritalStatus == "Married" && 
                !formData.FinancialInfo.SpouseAdjustedGrossIncome.HasValue)
            {
                validationResult.Errors.Add(new ValidationFailure("Financial", 
                    "Spouse financial information is required for married students"));
            }

            // Validate household size consistency
            if (formData.FinancialInfo.NumberInCollege > formData.FinancialInfo.HouseholdSize)
            {
                validationResult.Errors.Add(new ValidationFailure("Financial", 
                    "Number in college cannot exceed household size"));
            }
        }
        
        return Task.CompletedTask;
    }

    private async Task ValidateSignatureRequirements(
        FAFSAApplication application, 
        FAFSAFormData formData, 
        ValidationResult validationResult)
    {
        // Check if FSA ID is provided
        if (string.IsNullOrEmpty(application.FSAIdUsername))
        {
            validationResult.Errors.Add(new ValidationFailure("Signature", 
                "FSA ID username is required for submission"));
        }

        // For dependent students, parent signature is required
        if (formData.StudentInfo != null && !IsIndependentStudent(formData.StudentInfo))
        {
            // In a real implementation, you would check for stored parent signatures
            // For now, just add a placeholder validation
            validationResult.Errors.Add(new ValidationFailure("Signature", 
                "Parent signature is required for dependent students"));
        }

        await Task.CompletedTask;
    }

    private string GetSectionFromPropertyName(string propertyName)
    {
        if (propertyName.StartsWith("Student."))
            return "Student Information";
        if (propertyName.StartsWith("Financial."))
            return "Financial Information";
        if (propertyName.StartsWith("Family."))
            return "Family Information";
        if (propertyName.StartsWith("School."))
            return "School Selections";
        
        return "General";
    }

    private bool IsCriticalError(string errorMessage)
    {
        var criticalKeywords = new[] 
        { 
            "required", "missing", "invalid", "not found", "empty", "null" 
        };
        
        return criticalKeywords.Any(keyword => 
            errorMessage.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private double CalculateCompletionPercentage(ValidationResult result)
    {
        // Simple calculation based on error count
        // In a real implementation, this would be more sophisticated
        if (result.Errors.Count == 0)
            return 100.0;

        var maxExpectedErrors = 50; // Estimated maximum number of validation rules
        var errorCount = Math.Min(result.Errors.Count, maxExpectedErrors);
        
        return Math.Max(0, (maxExpectedErrors - errorCount) * 100.0 / maxExpectedErrors);
    }
}

/// <summary>
/// Structured FAFSA form data for validation
/// </summary>
public class FAFSAFormData
{
    public StudentInformation? StudentInfo { get; set; }
    public FinancialInformation? FinancialInfo { get; set; }
    public List<FamilyInformation> FamilyMembers { get; set; } = new();
    public List<SchoolSelection> SchoolSelections { get; set; } = new();
}

/// <summary>
/// Summary of validation results
/// </summary>
public class ValidationSummary
{
    public Guid ApplicationId { get; set; }
    public bool IsValid { get; set; }
    public int TotalErrors { get; set; }
    public Dictionary<string, int> ErrorsBySection { get; set; } = new();
    public List<string> CriticalErrors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public double CompletionPercentage { get; set; }
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
}