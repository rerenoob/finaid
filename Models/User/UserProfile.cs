using System.ComponentModel.DataAnnotations;

namespace finaid.Models.User;

public class UserProfile : BaseEntity
{
    [Required]
    public Guid UserId { get; set; }
    
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
    
    [MaxLength(200)]
    public string? AddressLine1 { get; set; }
    
    [MaxLength(200)]
    public string? AddressLine2 { get; set; }
    
    [MaxLength(100)]
    public string? City { get; set; }
    
    [MaxLength(50)]
    public string? State { get; set; }
    
    [MaxLength(20)]
    public string? ZipCode { get; set; }
    
    [MaxLength(100)]
    public string? Country { get; set; } = "United States";
    
    // Financial Information
    public decimal? AnnualIncome { get; set; }
    public int? HouseholdSize { get; set; }
    public bool IsMarried { get; set; }
    public bool HasDependents { get; set; }
    
    // Academic Information
    [MaxLength(200)]
    public string? HighSchool { get; set; }
    
    public int? GraduationYear { get; set; }
    
    [MaxLength(100)]
    public string? IntendedMajor { get; set; }
    
    public bool IsFirstGeneration { get; set; }
    
    // Communication Preferences
    public bool EmailNotifications { get; set; } = true;
    public bool SMSNotifications { get; set; } = false;
    public bool PushNotifications { get; set; } = true;
    
    // Onboarding Progress
    public bool OnboardingCompleted { get; set; } = false;
    public DateTime? OnboardingCompletedAt { get; set; }
    
    // Navigation property
    public User User { get; set; } = null!;
}