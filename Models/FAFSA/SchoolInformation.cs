using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace finaid.Models.FAFSA;

public class SchoolSelection : BaseEntity
{
    [Required]
    public Guid FAFSAApplicationId { get; set; }

    [Required]
    [MaxLength(10)]
    public string FederalSchoolCode { get; set; } = string.Empty; // Title IV school code

    [MaxLength(200)]
    public string SchoolName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [MaxLength(2)]
    public string State { get; set; } = string.Empty;

    [MaxLength(10)]
    public string ZipCode { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [MaxLength(256)]
    public string? Website { get; set; }

    [MaxLength(256)]
    public string? FinancialAidEmail { get; set; }

    // Academic Information
    [MaxLength(20)]
    public string IntendedGradeLevel { get; set; } = string.Empty; // Undergraduate, Graduate

    [MaxLength(50)]
    public string IntendedDegreeType { get; set; } = string.Empty; // Bachelor's, Master's, Doctorate, Certificate

    [MaxLength(100)]
    public string? IntendedMajor { get; set; }

    [MaxLength(100)]
    public string? IntendedMinor { get; set; }

    // Enrollment Plans
    [MaxLength(20)]
    public string EnrollmentStatus { get; set; } = string.Empty; // Full-time, Half-time, Less than half-time

    [MaxLength(20)]
    public string HousingPreference { get; set; } = string.Empty; // On-campus, Off-campus, With parents

    public DateTime? IntendedStartDate { get; set; }

    public bool IsFirstChoice { get; set; } = false;

    public int SelectionOrder { get; set; } = 1; // Priority order (1-10)

    // School Characteristics
    [MaxLength(20)]
    public string SchoolType { get; set; } = string.Empty; // Public, Private non-profit, Private for-profit

    [MaxLength(20)]
    public string ControlType { get; set; } = string.Empty; // Public, Private

    public bool IsInState { get; set; } = false;

    public bool IsTwoYearCollege { get; set; } = false;

    public bool IsFourYearCollege { get; set; } = false;

    public bool IsGraduateSchool { get; set; } = false;

    // Cost Information
    [Column(TypeName = "decimal(10,2)")]
    public decimal? EstimatedCostOfAttendance { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? TuitionAndFees { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? RoomAndBoard { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? BooksAndSupplies { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? PersonalExpenses { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? TransportationCosts { get; set; }

    // Financial Aid Information
    public bool HasInstitutionalAid { get; set; } = false;

    [Column(TypeName = "decimal(10,2)")]
    public decimal? InstitutionalAidAmount { get; set; }

    public bool HasMeritScholarships { get; set; } = false;

    [Column(TypeName = "decimal(10,2)")]
    public decimal? MeritScholarshipAmount { get; set; }

    public bool HasNeedBasedAid { get; set; } = false;

    [Column(TypeName = "decimal(10,2)")]
    public decimal? NeedBasedAidAmount { get; set; }

    // Application Status
    [MaxLength(20)]
    public string ApplicationStatus { get; set; } = string.Empty; // Applied, Accepted, Enrolled, Waitlisted, Rejected

    public DateTime? ApplicationSubmittedDate { get; set; }

    public DateTime? AcceptanceDate { get; set; }

    public DateTime? EnrollmentConfirmationDate { get; set; }

    // Additional Requirements
    public bool RequiresCSSProfile { get; set; } = false;

    public bool CSSProfileSubmitted { get; set; } = false;

    public DateTime? CSSProfileSubmissionDate { get; set; }

    public bool RequiresInstitutionalForm { get; set; } = false;

    public bool InstitutionalFormSubmitted { get; set; } = false;

    // Special Programs
    public bool IsHonorsProgram { get; set; } = false;

    public bool IsStudyAbroadProgram { get; set; } = false;

    public bool IsCoopProgram { get; set; } = false;

    public bool IsDistanceLearning { get; set; } = false;

    // Contact Information
    [MaxLength(100)]
    public string? FinancialAidContactName { get; set; }

    [MaxLength(20)]
    public string? FinancialAidContactPhone { get; set; }

    [MaxLength(256)]
    public string? FinancialAidContactEmail { get; set; }

    [MaxLength(100)]
    public string? AdmissionsContactName { get; set; }

    [MaxLength(20)]
    public string? AdmissionsContactPhone { get; set; }

    [MaxLength(256)]
    public string? AdmissionsContactEmail { get; set; }

    // Processing Information
    public bool FAFSAProcessed { get; set; } = false;

    public DateTime? ProcessingDate { get; set; }

    [MaxLength(500)]
    public string? ProcessingNotes { get; set; }

    public bool RequiresVerification { get; set; } = false;

    public bool VerificationCompleted { get; set; } = false;

    public DateTime? VerificationCompletedDate { get; set; }

    // Award Information
    public bool AwardLetterReceived { get; set; } = false;

    public DateTime? AwardLetterDate { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? TotalAwardAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? PellGrantAward { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? DirectLoanAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? WorkStudyAward { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? SEOGAward { get; set; } // Supplemental Educational Opportunity Grant

    // Decision Information
    public bool StudentAcceptedOffer { get; set; } = false;

    public DateTime? OfferAcceptanceDate { get; set; }

    [MaxLength(500)]
    public string? DecisionNotes { get; set; }

    // Navigation property
    public Models.Application.FAFSAApplication FAFSAApplication { get; set; } = null!;
}