using System.ComponentModel.DataAnnotations;

namespace finaid.Models.OCR;

public class ExtractedField
{
    [Required]
    [MaxLength(100)]
    public string FieldName { get; set; } = string.Empty;
    
    public object? Value { get; set; }
    
    [Range(0.0, 1.0)]
    public decimal Confidence { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string DataType { get; set; } = string.Empty; // "currency", "date", "text", "number", "boolean"
    
    public bool RequiresValidation { get; set; }
    
    public string? ValidationError { get; set; }
    
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public static class DataTypes
{
    public const string Currency = "currency";
    public const string Date = "date"; 
    public const string Text = "text";
    public const string Number = "number";
    public const string Boolean = "boolean";
    public const string Address = "address";
    public const string Phone = "phone";
    public const string Email = "email";
    public const string SSN = "ssn";
}