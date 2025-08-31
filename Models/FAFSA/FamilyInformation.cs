using System.ComponentModel.DataAnnotations;

namespace finaid.Models.FAFSA;

public class FamilyInformation : BaseEntity
{
    [Required]
    public Guid FAFSAApplicationId { get; set; }

    [Required]
    [MaxLength(20)]
    public string RelationshipType { get; set; } = string.Empty; // Parent1, Parent2, Spouse, Child, Other

    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? MiddleName { get; set; }

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(10)]
    public string? Suffix { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(256)] // Encrypted field
    public string? EncryptedSSN { get; set; }

    [MaxLength(20)]
    public string? Sex { get; set; }

    [MaxLength(20)]
    public string? MaritalStatus { get; set; }

    // Contact Information
    [MaxLength(200)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(2)]
    public string? State { get; set; }

    [MaxLength(10)]
    public string? ZipCode { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [EmailAddress]
    [MaxLength(256)]
    public string? Email { get; set; }

    // Educational Information (for family members in college)
    public bool IsInCollege { get; set; } = false;

    [MaxLength(200)]
    public string? CollegeName { get; set; }

    [MaxLength(10)]
    public string? CollegeCode { get; set; }

    [MaxLength(20)]
    public string? GradeLevel { get; set; }

    [MaxLength(20)]
    public string? DegreeType { get; set; }

    public DateTime? EnrollmentStartDate { get; set; }
    public DateTime? ExpectedGraduationDate { get; set; }

    // Parent-specific Information
    [MaxLength(20)]
    public string? EducationLevel { get; set; } // High school, College, Graduate, etc.

    public bool IsDeceased { get; set; } = false;
    public DateTime? DateOfDeath { get; set; }

    [MaxLength(20)]
    public string? EmploymentStatus { get; set; } // Employed, Unemployed, Retired, etc.

    [MaxLength(100)]
    public string? Employer { get; set; }

    [MaxLength(100)]
    public string? JobTitle { get; set; }

    // Dependency Information
    public bool IsLegalGuardian { get; set; } = false;
    public bool HasLegalCustody { get; set; } = false;
    public bool ProvidesSupportForStudent { get; set; } = false;

    // Special Circumstances
    public bool HasSpecialNeeds { get; set; } = false;
    
    [MaxLength(500)]
    public string? SpecialNeedsDescription { get; set; }

    public bool HasDisability { get; set; } = false;
    
    [MaxLength(500)]
    public string? DisabilityDescription { get; set; }

    // Tax Information (for parents/spouse)
    public bool WillClaimOnTaxes { get; set; } = false;
    public bool IsClaimedOnTaxes { get; set; } = false;

    // Citizenship Information
    [MaxLength(20)]
    public string? CitizenshipStatus { get; set; }

    [MaxLength(100)]
    public string? CountryOfOrigin { get; set; }

    // Additional Support Information
    public bool ReceivesChildSupport { get; set; } = false;
    public bool PaysChildSupport { get; set; } = false;

    // Verification Information
    public bool RequiresDocumentation { get; set; } = false;
    public bool DocumentationProvided { get; set; } = false;
    
    [MaxLength(500)]
    public string? DocumentationNotes { get; set; }

    // Data Quality
    public bool IsVerified { get; set; } = false;
    public DateTime? VerifiedDate { get; set; }
    public string? VerifiedBy { get; set; }

    [MaxLength(500)]
    public string? AdditionalNotes { get; set; }

    // Navigation property
    public Models.Application.FAFSAApplication FAFSAApplication { get; set; } = null!;
}