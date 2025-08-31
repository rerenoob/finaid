using Microsoft.EntityFrameworkCore;
using finaid.Models;
using finaid.Models.User;
using finaid.Models.Application;
using finaid.Models.Document;
using System.Linq.Expressions;

namespace finaid.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<UserProfile> UserProfiles { get; set; } = null!;
    public DbSet<FAFSAApplication> FAFSAApplications { get; set; } = null!;
    public DbSet<ApplicationStep> ApplicationSteps { get; set; } = null!;
    public DbSet<UserDocument> UserDocuments { get; set; } = null!;
    public DbSet<ApplicationDocument> ApplicationDocuments { get; set; } = null!;
    public DbSet<finaid.Models.Background.BackgroundTaskStatus> BackgroundTasks { get; set; } = null!;
    public DbSet<finaid.Models.Audit.AuditEvent> AuditEvents { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Global query filters for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                var filter = Expression.Lambda(Expression.Equal(property, Expression.Constant(false)), parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EncryptedSSN).HasMaxLength(256);
        });

        // Configure UserProfile entity
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasOne(e => e.User)
                  .WithOne(u => u.Profile)
                  .HasForeignKey<UserProfile>(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Country).HasDefaultValue("United States");
            entity.Property(e => e.EmailNotifications).HasDefaultValue(true);
            entity.Property(e => e.PushNotifications).HasDefaultValue(true);
            entity.Property(e => e.AnnualIncome).HasPrecision(12, 2);
        });

        // Configure FAFSAApplication entity
        modelBuilder.Entity<FAFSAApplication>(entity =>
        {
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Applications)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.AwardYear }).IsUnique();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.CompletionPercentage).HasPrecision(5, 2);
            entity.Property(e => e.EstimatedEFC).HasPrecision(10, 2);
            entity.Property(e => e.EstimatedPellGrant).HasPrecision(10, 2);
        });

        // Configure ApplicationStep entity
        modelBuilder.Entity<ApplicationStep>(entity =>
        {
            entity.HasOne(e => e.Application)
                  .WithMany(a => a.Steps)
                  .HasForeignKey(e => e.ApplicationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.ApplicationId, e.StepOrder }).IsUnique();
        });

        // Configure UserDocument entity
        modelBuilder.Entity<UserDocument>(entity =>
        {
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Documents)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.DocumentType).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasIndex(e => e.FileHash);
        });

        // Configure ApplicationDocument entity (many-to-many relationship)
        modelBuilder.Entity<ApplicationDocument>(entity =>
        {
            entity.HasOne(e => e.Application)
                  .WithMany(a => a.SupportingDocuments)
                  .HasForeignKey(e => e.ApplicationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Document)
                  .WithMany(d => d.ApplicationDocuments)
                  .HasForeignKey(e => e.DocumentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.ApplicationId, e.DocumentId }).IsUnique();
        });

        // Seed data for development
        SeedData(modelBuilder);
    }

    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    // CreatedBy and UpdatedBy should be set by the service layer
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    // UpdatedBy should be set by the service layer
                    break;

                case EntityState.Deleted:
                    // Implement soft delete
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = DateTime.UtcNow;
                    // DeletedBy should be set by the service layer
                    break;
            }
        }
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // This would be populated with initial data for development/testing
        // For now, we'll leave it empty as seed data should come from migrations
    }
}