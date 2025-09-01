# Task: Implement Real-time Dashboard Updates with SignalR

## Overview
- **Parent Feature**: IMPL-005 - Unified Dashboard and Progress Tracking
- **Complexity**: High
- **Estimated Time**: 8 hours
- **Status**: Completed

## Dependencies
### Required Tasks
- [ ] 002-progress-tracking-components.md: Progress components implemented
- [ ] 003-deadline-management-system.md: Deadline system working
- [ ] 01-foundation-infrastructure/004-blazor-app-foundation.md: SignalR configured

### External Dependencies
- SignalR client libraries for Blazor
- Redis backplane for scaled deployments
- Connection management for mobile devices

## Implementation Details
### Files to Create/Modify
- `Hubs/DashboardHub.cs`: SignalR hub for dashboard updates
- `Services/RealTime/DashboardUpdateService.cs`: Update broadcasting logic
- `Services/RealTime/IDashboardUpdateService.cs`: Service interface
- `Models/RealTime/DashboardUpdate.cs`: Update message model
- `wwwroot/js/dashboard-signalr.js`: Client-side SignalR handling
- `Components/Dashboard/RealTimeConnectionStatus.razor`: Connection indicator
- `Services/RealTime/ConnectionManagerService.cs`: Connection lifecycle management
- `Configuration/SignalRSettings.cs`: SignalR configuration

### Code Patterns
- Use strongly-typed SignalR hubs for type safety
- Implement connection groups for user-specific updates
- Follow existing SignalR patterns in the application
- Use async patterns for all hub methods

### Real-time Updates Architecture
```csharp
public class DashboardHub : Hub
{
    private readonly IDashboardUpdateService _updateService;
    
    public async Task JoinUserGroup()
    {
        var userId = Context.UserIdentifier;
        if (userId != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        }
    }
    
    public async Task LeaveUserGroup()
    {
        var userId = Context.UserIdentifier;
        if (userId != null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
        }
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await LeaveUserGroup();
        await base.OnDisconnectedAsync(exception);
    }
}

public interface IDashboardUpdateService
{
    Task SendProgressUpdateAsync(Guid userId, ApplicationProgress progress);
    Task SendDeadlineUpdateAsync(Guid userId, ApplicationDeadline deadline);
    Task SendNotificationAsync(Guid userId, string message, NotificationType type);
    Task SendDocumentStatusUpdateAsync(Guid userId, DocumentMetadata document);
}

public class DashboardUpdate
{
    public string Type { get; set; } = null!; // "progress", "deadline", "notification", "document"
    public object Data { get; set; } = null!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Message { get; set; }
    public UpdatePriority Priority { get; set; }
}

// Client-side JavaScript integration
window.dashboardSignalR = {
    connection: null,
    
    start: function() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/dashboardHub")
            .build();
            
        this.connection.start().then(function() {
            console.log("Dashboard SignalR connected");
            this.connection.invoke("JoinUserGroup");
        });
        
        // Register update handlers
        this.connection.on("ProgressUpdate", this.handleProgressUpdate);
        this.connection.on("DeadlineUpdate", this.handleDeadlineUpdate);
    }
};
```

## Acceptance Criteria
- [ ] Real-time progress updates appear immediately across all user sessions
- [ ] Deadline notifications broadcast to connected clients instantly
- [ ] Document processing status updates displayed without page refresh
- [ ] Connection status indicator shows SignalR connection health
- [ ] Graceful reconnection handling for dropped connections
- [ ] Message queuing for offline clients (reconnection replay)
- [ ] User-specific update groups prevent cross-user data leaks
- [ ] Mobile-friendly connection management (handles sleep/wake cycles)
- [ ] Performance optimized for high-frequency updates
- [ ] Error handling and fallback for SignalR failures

## Testing Strategy
- Unit tests: Hub methods, update broadcasting, connection management
- Integration tests: Client-server communication, group management
- Manual validation:
  - Test real-time updates across multiple browser tabs
  - Verify updates work on mobile devices
  - Test connection recovery after network interruption
  - Confirm user isolation (no cross-user updates)
  - Test performance with multiple concurrent connections

## System Stability
- Connection pooling and management prevents resource exhaustion
- Message queuing ensures updates aren't lost during disconnections
- Circuit breaker pattern prevents cascading failures
- Fallback to polling when SignalR unavailable