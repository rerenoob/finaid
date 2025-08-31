using System.ComponentModel.DataAnnotations;
using finaid.Models.Application;
using finaid.Models.Document;

namespace finaid.Models.User;

public class User : BaseEntity
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    public DateTime DateOfBirth { get; set; }
    
    // Encrypted field - will be handled by EF Core value converters
    [MaxLength(256)]
    public string? EncryptedSSN { get; set; }
    
    // Navigation properties
    public UserProfile? Profile { get; set; }
    public ICollection<FAFSAApplication> Applications { get; set; } = new List<FAFSAApplication>();
    public ICollection<UserDocument> Documents { get; set; } = new List<UserDocument>();
    
    // Computed property for display name
    public string FullName => $"{FirstName} {LastName}";
}