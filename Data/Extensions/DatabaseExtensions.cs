using finaid.Models.User;
using finaid.Models.Application;
using Microsoft.EntityFrameworkCore;

namespace finaid.Data.Extensions;

public static class DatabaseExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Apply any pending migrations
        if (context.Database.GetPendingMigrations().Any())
        {
            await context.Database.MigrateAsync();
        }

        // Seed development data if in development environment
        if (app.Environment.IsDevelopment())
        {
            await SeedDevelopmentDataAsync(context);
        }
    }

    private static async Task SeedDevelopmentDataAsync(ApplicationDbContext context)
    {
        // Check if we already have data
        if (await context.Users.AnyAsync())
        {
            return; // Data already seeded
        }

        // Create sample users for development
        var sampleUsers = new List<User>
        {
            new User
            {
                Email = "emily.carter@example.com",
                FirstName = "Emily",
                LastName = "Carter",
                DateOfBirth = new DateTime(2006, 5, 15),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Profile = new UserProfile
                {
                    PhoneNumber = "555-0123",
                    AddressLine1 = "123 Rural Road",
                    City = "Springfield",
                    State = "Missouri",
                    ZipCode = "65802",
                    AnnualIncome = 35000m,
                    HouseholdSize = 4,
                    IsFirstGeneration = true,
                    IntendedMajor = "Computer Science",
                    HighSchool = "Springfield High School",
                    GraduationYear = 2024,
                    OnboardingCompleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            },
            new User
            {
                Email = "marcus.reed@example.com", 
                FirstName = "Marcus",
                LastName = "Reed",
                DateOfBirth = new DateTime(1989, 8, 22),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Profile = new UserProfile
                {
                    PhoneNumber = "555-0456",
                    AddressLine1 = "456 City Avenue",
                    AddressLine2 = "Apt 2B",
                    City = "Chicago",
                    State = "Illinois", 
                    ZipCode = "60601",
                    AnnualIncome = 48000m,
                    HouseholdSize = 3,
                    IsMarried = false,
                    HasDependents = true,
                    IsFirstGeneration = false,
                    IntendedMajor = "Business Administration",
                    OnboardingCompleted = true,
                    OnboardingCompletedAt = DateTime.UtcNow.AddDays(-30),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            }
        };

        context.Users.AddRange(sampleUsers);
        await context.SaveChangesAsync();

        // Create sample FAFSA applications
        var currentYear = DateTime.Now.Year;
        var emilyUser = sampleUsers.First(u => u.FirstName == "Emily");
        var marcusUser = sampleUsers.First(u => u.FirstName == "Marcus");

        var sampleApplications = new List<FAFSAApplication>
        {
            new FAFSAApplication
            {
                UserId = emilyUser.Id,
                AwardYear = currentYear + 1,
                Status = ApplicationStatus.Draft,
                CompletionPercentage = 25.0m,
                DeadlineDate = new DateTime(currentYear + 1, 6, 30),
                LastActivityAt = DateTime.UtcNow.AddDays(-2),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new FAFSAApplication
            {
                UserId = marcusUser.Id,
                AwardYear = currentYear + 1,
                Status = ApplicationStatus.InProgress,
                CompletionPercentage = 65.0m,
                DeadlineDate = new DateTime(currentYear + 1, 6, 30),
                LastActivityAt = DateTime.UtcNow.AddHours(-6),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.FAFSAApplications.AddRange(sampleApplications);
        await context.SaveChangesAsync();
    }
}