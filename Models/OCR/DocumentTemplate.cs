using finaid.Models.Document;
using System.ComponentModel.DataAnnotations;

namespace finaid.Models.OCR;

public class DocumentTemplate
{
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public DocumentType DocumentType { get; set; }
    
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    
    public List<TemplateField> Fields { get; set; } = new();
    
    public List<string> Keywords { get; set; } = new();
    
    public decimal MinConfidenceThreshold { get; set; } = 0.7m;
    
    public bool IsActive { get; set; } = true;
    
    public int Version { get; set; } = 1;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class TemplateField
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;
    
    [Required]
    public string DataType { get; set; } = string.Empty;
    
    public bool IsRequired { get; set; }
    
    public string? ValidationPattern { get; set; }
    
    public List<string> AlternativeNames { get; set; } = new();
    
    public Dictionary<string, object> Metadata { get; set; } = new();
}