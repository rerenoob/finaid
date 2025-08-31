using System.ComponentModel.DataAnnotations;
using finaid.Models.Application;

namespace finaid.Models.Document;

public class ApplicationDocument : BaseEntity
{
    [Required]
    public Guid ApplicationId { get; set; }
    
    [Required]
    public Guid DocumentId { get; set; }
    
    public bool IsRequired { get; set; } = false;
    
    [MaxLength(200)]
    public string? Purpose { get; set; }
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    // Navigation properties
    public FAFSAApplication Application { get; set; } = null!;
    public UserDocument Document { get; set; } = null!;
}