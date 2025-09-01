# Task: Implement Deadline Tracking and Notification System

## Overview
- **Parent Feature**: IMPL-005 - Unified Dashboard and Progress Tracking
- **Complexity**: High
- **Estimated Time**: 8 hours
- **Status**: Completed

## Dependencies
### Required Tasks
- [ ] 002-progress-tracking-components.md: Progress tracking established
- [ ] 01-foundation-infrastructure/002-database-schema-design.md: Database models available

### External Dependencies
- Background service for deadline monitoring
- Email/SMS notification services (SendGrid, Twilio)
- Calendar integration capabilities (iCal, Google Calendar)

## Implementation Details
### Files to Create/Modify
- `Services/Deadlines/DeadlineManagementService.cs`: Deadline tracking logic
- `Services/Deadlines/IDeadlineManagementService.cs`: Service interface
- `BackgroundServices/DeadlineMonitoringService.cs`: Background deadline checking
- `Components/Dashboard/DeadlineList.razor`: Upcoming deadlines display
- `Components/Dashboard/DeadlineCard.razor`: Individual deadline component
- `Models/Deadlines/ApplicationDeadline.cs`: Deadline data model
- `Services/Notifications/DeadlineNotificationService.cs`: Notification delivery
- `Data/Entities/UserNotificationPreference.cs`: User notification settings

### Code Patterns
- Use background services for periodic deadline checking
- Implement notification templates for different deadline types
- Follow existing notification service patterns
- Use priority queues for deadline urgency

### Deadline Management Architecture
```csharp
public interface IDeadlineManagementService
{
    Task<List<ApplicationDeadline>> GetUpcomingDeadlinesAsync(Guid userId, int daysAhead = 30);
    Task<ApplicationDeadline> CreateDeadlineAsync(Guid userId, string title, DateTime dueDate, DeadlineType type);
    Task<bool> MarkDeadlineCompletedAsync(Guid deadlineId);
    Task<List<ApplicationDeadline>> GetOverdueDeadlinesAsync(Guid userId);
    Task ScheduleDeadlineNotificationsAsync(Guid deadlineId);
}

public class ApplicationDeadline
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime DueDate { get; set; }
    public DeadlineType Type { get; set; }
    public DeadlineUrgency Urgency { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<DeadlineNotification> Notifications { get; set; } = new();
    public string? RelatedApplicationId { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public enum DeadlineUrgency
{
    Normal,    // > 14 days
    Warning,   // 7-14 days
    Critical,  // 1-7 days
    Overdue    // Past due date
}

@* Deadline display component *@
<div class="deadline-list">
    <h5>Upcoming Deadlines</h5>
    @if (!deadlines.Any())
    {
        <div class="no-deadlines">
            <p>No upcoming deadlines. Great job staying on track! 🎉</p>
        </div>
    }
    else
    {
        @foreach (var deadline in deadlines.OrderBy(d => d.DueDate))
        {
            <DeadlineCard Deadline="deadline" OnComplete="HandleDeadlineComplete" />
        }
    }
</div>
```

## Acceptance Criteria
- [ ] Automatic deadline creation based on application types and award years
- [ ] Deadline urgency calculation and color-coded visual indicators
- [ ] Email notifications sent at configurable intervals (30, 14, 7, 1 days before)
- [ ] SMS notifications for critical deadlines (user preference)
- [ ] Calendar export functionality (.ics files) for external calendar integration
- [ ] Deadline completion tracking with progress updates
- [ ] Overdue deadline identification and escalated notifications
- [ ] User notification preferences (frequency, channels, types)
- [ ] Institution-specific deadline integration
- [ ] Grace period handling for deadlines with extensions

## Testing Strategy
- Unit tests: Deadline urgency calculation, notification scheduling, completion tracking
- Integration tests: Background service execution, notification delivery
- Manual validation:
  - Create deadlines with various due dates and verify urgency calculation
  - Test notification delivery across email and SMS channels
  - Verify calendar export functionality
  - Test deadline completion and progress integration
  - Confirm background service processes deadlines correctly

## System Stability
- Background service resilient to temporary failures
- Notification queue prevents duplicate or missed notifications
- Graceful handling of external notification service failures
- Deadline data synchronized across all user sessions