using System.ComponentModel.DataAnnotations;

namespace finaid.Models.FAFSA;

public class StudentInformation : BaseEntity
{
    [Required]
    public Guid FAFSAApplicationId { get; set; }

    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? MiddleName { get; set; }

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(10)]
    public string? Suffix { get; set; } // Jr., Sr., III, etc.

    [Required]
    public DateTime DateOfBirth { get; set; }

    [Required]
    [MaxLength(256)] // Encrypted field
    public string EncryptedSSN { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string CitizenshipStatus { get; set; } = string.Empty; // US Citizen, Eligible Non-Citizen, etc.

    [MaxLength(100)]
    public string? AlienRegistrationNumber { get; set; } // For non-citizens

    [Required]
    [MaxLength(2)]
    public string StateOfLegalResidence { get; set; } = string.Empty;

    [Required]
    public DateTime ResidencyDate { get; set; } // When became legal resident

    [Required]
    [MaxLength(10)]
    public string Sex { get; set; } = string.Empty; // Male, Female, Not Reported

    [Required]
    [MaxLength(20)]
    public string MaritalStatus { get; set; } = string.Empty; // Single, Married, Divorced, Widowed, Separated

    public DateTime? MaritalStatusDate { get; set; }

    // Contact Information
    [MaxLength(200)]
    public string? PermanentAddress { get; set; }

    [MaxLength(100)]
    public string? PermanentCity { get; set; }

    [MaxLength(2)]
    public string? PermanentState { get; set; }

    [MaxLength(10)]
    public string? PermanentZipCode { get; set; }

    [MaxLength(200)]
    public string? MailingAddress { get; set; }

    [MaxLength(100)]
    public string? MailingCity { get; set; }

    [MaxLength(2)]
    public string? MailingState { get; set; }

    [MaxLength(10)]
    public string? MailingZipCode { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(256)]
    public string? AlternateEmail { get; set; }

    // Educational Information
    [Required]
    [MaxLength(20)]
    public string GradeLevel { get; set; } = string.Empty; // First-time college student, etc.

    [MaxLength(20)]
    public string DegreeType { get; set; } = string.Empty; // Bachelor's, Graduate, etc.

    public bool IsFirstGeneration { get; set; } = false;

    [MaxLength(100)]
    public string? HighSchoolName { get; set; }

    [MaxLength(2)]
    public string? HighSchoolState { get; set; }

    public int? HighSchoolGraduationYear { get; set; }

    public DateTime? GEDDate { get; set; }

    public bool HasHighSchoolDiploma { get; set; } = true;

    // Special Circumstances
    public bool IsOrphan { get; set; } = false;
    public bool IsWardOfCourt { get; set; } = false;
    public bool WasInFosterCare { get; set; } = false;
    public bool IsEmancipatedMinor { get; set; } = false;
    public bool IsHomeless { get; set; } = false;
    public bool AtRiskOfHomelessness { get; set; } = false;

    // Military Service
    public bool HasMilitaryService { get; set; } = false;
    public bool IsVeteran { get; set; } = false;
    public bool HasVeteranBenefits { get; set; } = false;

    // Dependency Override
    public bool HasDependencyOverride { get; set; } = false;
    
    [MaxLength(500)]
    public string? DependencyOverrideReason { get; set; }

    // Professional Judgment
    public bool HasProfessionalJudgment { get; set; } = false;
    
    [MaxLength(500)]
    public string? ProfessionalJudgmentReason { get; set; }

    // Navigation property
    public Models.Application.FAFSAApplication FAFSAApplication { get; set; } = null!;
}