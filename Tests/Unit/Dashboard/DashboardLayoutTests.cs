using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Bunit;
using finaid.Components.Dashboard;
using finaid.Models.Dashboard;
using finaid.Models.User;
using finaid.Services.Dashboard;
using Moq;
using Xunit;

namespace finaid.Tests.Unit.Dashboard;

public class DashboardLayoutTests : TestContext
{
    private readonly Mock<IDashboardDataService> _mockDashboardService;
    
    public DashboardLayoutTests()
    {
        _mockDashboardService = new Mock<IDashboardDataService>();
        Services.AddSingleton(_mockDashboardService.Object);
    }
    
    [Fact]
    public void DashboardLayout_Renders_Correctly_With_Data()
    {
        // Arrange
        var dashboardData = new DashboardViewModel
        {
            Applications = new List<ApplicationProgress>
            {
                new ApplicationProgress
                {
                    ApplicationId = Guid.NewGuid(),
                    ApplicationType = "FAFSA",
                    Title = "Federal Student Aid Application",
                    OverallCompletion = 65.0m,
                    Status = ApplicationStatus.InProgress,
                    LastUpdated = DateTime.UtcNow.AddDays(-1),
                    NextDeadline = DateTime.UtcNow.AddDays(30),
                    NextStepDescription = "Complete financial information",
                    HasBlockingIssues = false,
                    BlockingIssueCount = 0
                }
            },
            UpcomingDeadlines = new List<UpcomingDeadline>
            {
                new UpcomingDeadline
                {
                    Id = Guid.NewGuid(),
                    Title = "FAFSA Submission",
                    Description = "Complete and submit your FAFSA application",
                    DueDate = DateTime.UtcNow.AddDays(15),
                    ApplicationType = "FAFSA",
                    Priority = DeadlinePriority.High,
                    DaysUntilDue = 15,
                    ActionUrl = "/fafsa"
                }
            },
            RecentActivities = new List<RecentActivity>
            {
                new RecentActivity
                {
                    Id = Guid.NewGuid(),
                    Title = "FAFSA Section Updated",
                    Description = "Student demographic information completed",
                    Type = ActivityType.FormSubmitted,
                    Timestamp = DateTime.UtcNow.AddHours(-2),
                    Icon = "bi-file-earmark-check",
                    ActionUrl = "/fafsa"
                }
            },
            Notifications = new NotificationSummary
            {
                UnreadCount = 1,
                TotalCount = 2,
                RecentNotifications = new List<NotificationItem>
                {
                    new NotificationItem
                    {
                        Id = Guid.NewGuid(),
                        Title = "Deadline Reminder",
                        Message = "FAFSA application due in 15 days",
                        Type = NotificationType.Warning,
                        Timestamp = DateTime.UtcNow.AddHours(-1),
                        IsRead = false,
                        ActionUrl = "/fafsa"
                    }
                }
            },
            QuickActions = new QuickActionSummary
            {
                Actions = new List<QuickAction>
                {
                    new QuickAction
                    {
                        Title = "Continue FAFSA",
                        Description = "Resume your FAFSA application",
                        Icon = "bi-file-earmark-text",
                        Url = "/fafsa",
                        CssClass = "btn-primary",
                        Priority = 1
                    }
                },
                ShowNewUserActions = false
            }
        };
        
        var currentUser = new User
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com"
        };
        
        // Act
        var cut = RenderComponent<DashboardLayout>(parameters => parameters
            .Add(p => p.DashboardData, dashboardData)
            .Add(p => p.CurrentUser, currentUser)
        );
        
        // Assert
        Assert.Contains("Welcome, John!", cut.Markup);
        Assert.Contains("Federal Student Aid Application", cut.Markup);
        Assert.Contains("65%", cut.Markup);
        Assert.Contains("FAFSA Submission", cut.Markup);
        Assert.Contains("Continue FAFSA", cut.Markup);
    }
    
    [Fact]
    public void DashboardLayout_Shows_Loading_States()
    {
        // Arrange
        var currentUser = new User
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@example.com"
        };
        
        // Act
        var cut = RenderComponent<DashboardLayout>(parameters => parameters
            .Add(p => p.CurrentUser, currentUser)
            .Add(p => p.IsLoadingApplications, true)
            .Add(p => p.IsLoadingDeadlines, true)
            .Add(p => p.IsLoadingActivities, true)
            .Add(p => p.IsLoadingNotifications, true)
            .Add(p => p.IsLoadingQuickActions, true)
        );
        
        // Assert
        Assert.Contains("Welcome, Jane!", cut.Markup);
        Assert.Contains("Loading...", cut.Markup);
    }
    
    [Fact]
    public void DashboardLayout_Shows_Empty_States()
    {
        // Arrange
        var dashboardData = new DashboardViewModel();
        var currentUser = new User
        {
            FirstName = "Bob",
            LastName = "Wilson",
            Email = "bob.wilson@example.com"
        };
        
        // Act
        var cut = RenderComponent<DashboardLayout>(parameters => parameters
            .Add(p => p.DashboardData, dashboardData)
            .Add(p => p.CurrentUser, currentUser)
        );
        
        // Assert
        Assert.Contains("Welcome, Bob!", cut.Markup);
        Assert.Contains("No Applications Started", cut.Markup);
        Assert.Contains("No Upcoming Deadlines", cut.Markup);
        Assert.Contains("No Recent Activity", cut.Markup);
    }
}