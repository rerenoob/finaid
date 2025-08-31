using System.ComponentModel.DataAnnotations;

namespace finaid.Models.Application;

public class ApplicationStep : BaseEntity
{
    [Required]
    public Guid ApplicationId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string StepName { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string? StepTitle { get; set; }
    
    [MaxLength(500)]
    public string? StepDescription { get; set; }
    
    public int StepOrder { get; set; }
    
    public bool IsRequired { get; set; } = true;
    public bool IsCompleted { get; set; } = false;
    public DateTime? CompletedAt { get; set; }
    
    // Step-specific data as JSON
    public string? StepDataJson { get; set; }
    
    // Validation information
    public bool IsValid { get; set; } = false;
    public string? ValidationErrors { get; set; }
    
    // Navigation property
    public FAFSAApplication Application { get; set; } = null!;
}