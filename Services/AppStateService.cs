namespace finaid.Services;

public class AppStateService
{
    private readonly Dictionary<string, object> _state = new();
    private readonly List<Action> _stateChangeSubscribers = new();

    public event Action? OnStateChanged;

    /// <summary>
    /// Get a value from the application state
    /// </summary>
    /// <typeparam name="T">Type of the value to retrieve</typeparam>
    /// <param name="key">State key</param>
    /// <param name="defaultValue">Default value if key not found</param>
    /// <returns>The value or default value</returns>
    public T GetValue<T>(string key, T defaultValue = default!)
    {
        if (_state.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }

    /// <summary>
    /// Set a value in the application state
    /// </summary>
    /// <typeparam name="T">Type of the value to set</typeparam>
    /// <param name="key">State key</param>
    /// <param name="value">Value to set</param>
    public void SetValue<T>(string key, T value)
    {
        _state[key] = value!;
        NotifyStateChanged();
    }

    /// <summary>
    /// Remove a value from the application state
    /// </summary>
    /// <param name="key">State key to remove</param>
    /// <returns>True if the key was removed, false if it didn't exist</returns>
    public bool RemoveValue(string key)
    {
        var removed = _state.Remove(key);
        if (removed)
        {
            NotifyStateChanged();
        }
        return removed;
    }

    /// <summary>
    /// Check if a key exists in the application state
    /// </summary>
    /// <param name="key">State key</param>
    /// <returns>True if the key exists</returns>
    public bool HasValue(string key)
    {
        return _state.ContainsKey(key);
    }

    /// <summary>
    /// Clear all values from the application state
    /// </summary>
    public void Clear()
    {
        _state.Clear();
        NotifyStateChanged();
    }

    /// <summary>
    /// Get all keys in the application state
    /// </summary>
    /// <returns>Collection of all keys</returns>
    public IEnumerable<string> GetKeys()
    {
        return _state.Keys;
    }

    /// <summary>
    /// Subscribe to state change notifications
    /// </summary>
    /// <param name="callback">Callback to invoke when state changes</param>
    public void Subscribe(Action callback)
    {
        _stateChangeSubscribers.Add(callback);
    }

    /// <summary>
    /// Unsubscribe from state change notifications
    /// </summary>
    /// <param name="callback">Callback to remove</param>
    public void Unsubscribe(Action callback)
    {
        _stateChangeSubscribers.Remove(callback);
    }

    private void NotifyStateChanged()
    {
        OnStateChanged?.Invoke();
        
        // Notify all subscribers
        foreach (var subscriber in _stateChangeSubscribers)
        {
            try
            {
                subscriber.Invoke();
            }
            catch (Exception ex)
            {
                // Log the exception but continue with other subscribers
                // In a real application, you would use proper logging here
                Console.WriteLine($"Error in state change subscriber: {ex.Message}");
            }
        }
    }
}

/// <summary>
/// Common state keys used throughout the application
/// </summary>
public static class AppStateKeys
{
    public const string CurrentUser = "CurrentUser";
    public const string CurrentApplication = "CurrentApplication";
    public const string IsLoading = "IsLoading";
    public const string LoadingMessage = "LoadingMessage";
    public const string NotificationCount = "NotificationCount";
    public const string LastSyncTime = "LastSyncTime";
    public const string Theme = "Theme";
    public const string Language = "Language";
    public const string ShowIntroduction = "ShowIntroduction";
}

/// <summary>
/// Extension methods for common application state operations
/// </summary>
public static class AppStateExtensions
{
    /// <summary>
    /// Set loading state with optional message
    /// </summary>
    public static void SetLoading(this AppStateService appState, bool isLoading, string? message = null)
    {
        appState.SetValue(AppStateKeys.IsLoading, isLoading);
        if (message != null)
        {
            appState.SetValue(AppStateKeys.LoadingMessage, message);
        }
    }

    /// <summary>
    /// Get current loading state
    /// </summary>
    public static bool IsLoading(this AppStateService appState)
    {
        return appState.GetValue<bool>(AppStateKeys.IsLoading);
    }

    /// <summary>
    /// Get current loading message
    /// </summary>
    public static string GetLoadingMessage(this AppStateService appState)
    {
        return appState.GetValue<string>(AppStateKeys.LoadingMessage, "Loading...");
    }
}