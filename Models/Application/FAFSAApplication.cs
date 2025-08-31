using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using finaid.Models.User;
using finaid.Models.Document;

namespace finaid.Models.Application;

public class FAFSAApplication : BaseEntity
{
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public int AwardYear { get; set; }
    
    [Required]
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;
    
    // Completion tracking
    public decimal CompletionPercentage { get; set; } = 0;
    public DateTime? LastActivityAt { get; set; }
    
    // Form data stored as JSON for flexibility
    [Column(TypeName = "nvarchar(max)")]
    public string? FormDataJson { get; set; }
    
    // Calculated fields
    public decimal? EstimatedEFC { get; set; }
    public decimal? EstimatedPellGrant { get; set; }
    
    // Important dates
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime DeadlineDate { get; set; }
    
    // Federal Student Aid ID and confirmation
    [MaxLength(100)]
    public string? FSAIdUsername { get; set; }
    
    [MaxLength(100)]
    public string? ConfirmationNumber { get; set; }
    
    // Processing information
    [MaxLength(500)]
    public string? ProcessingNotes { get; set; }
    
    [MaxLength(1000)]
    public string? RejectionReason { get; set; }
    
    // Navigation properties
    public User.User User { get; set; } = null!;
    public ICollection<ApplicationDocument> SupportingDocuments { get; set; } = new List<ApplicationDocument>();
    public ICollection<ApplicationStep> Steps { get; set; } = new List<ApplicationStep>();
}