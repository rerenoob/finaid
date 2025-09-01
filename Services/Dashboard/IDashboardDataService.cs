using finaid.Models.Dashboard;
using finaid.Models.User;

namespace finaid.Services.Dashboard;

public interface IDashboardDataService
{
    Task<DashboardViewModel> GetDashboardDataAsync(Guid userId);
    Task<List<ApplicationProgress>> GetApplicationProgressAsync(Guid userId);
    Task<List<UpcomingDeadline>> GetUpcomingDeadlinesAsync(Guid userId, int daysAhead = 30);
    Task<List<RecentActivity>> GetRecentActivitiesAsync(Guid userId, int count = 10);
    Task<NotificationSummary> GetNotificationSummaryAsync(Guid userId);
    Task<QuickActionSummary> GetQuickActionsAsync(Guid userId);
}