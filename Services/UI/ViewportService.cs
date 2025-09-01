using Microsoft.JSInterop;
using finaid.Models.UI;

namespace finaid.Services.UI;

public class ViewportService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<ViewportService> _logger;
    
    private DeviceInfo? _deviceInfo;
    private bool _isInitialized = false;

    public ViewportService(IJSRuntime jsRuntime, ILogger<ViewportService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            var result = await _jsRuntime.InvokeAsync<object>("eval", @"
                (function() {
                    const ua = navigator.userAgent;
                    const viewport = {
                        width: window.innerWidth,
                        height: window.innerHeight,
                        devicePixelRatio: window.devicePixelRatio || 1,
                        orientation: screen.orientation ? screen.orientation.angle : 0
                    };
                    
                    return {
                        viewport: viewport,
                        isMobile: /Android|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(ua),
                        isTablet: /iPad|Android(?!.*Mobile)/i.test(ua) && window.innerWidth >= 768,
                        isTouch: 'ontouchstart' in window,
                        userAgent: ua,
                        platform: navigator.platform,
                        supportsServiceWorker: 'serviceWorker' in navigator,
                        connectionType: navigator.connection ? navigator.connection.effectiveType : 'unknown',
                        isOnline: navigator.onLine
                    };
                })()
            ");

            if (result != null)
            {
                var deviceData = System.Text.Json.JsonSerializer.Deserialize<dynamic>(result.ToString()!);
                _deviceInfo = ParseDeviceInfo(deviceData);
            }

            _isInitialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize viewport service. Using default values.");
            
            // Fallback device info
            _deviceInfo = new DeviceInfo
            {
                IsMobile = false,
                IsTablet = false,
                IsTouch = false,
                ViewportWidth = 1920,
                ViewportHeight = 1080,
                DevicePixelRatio = 1.0,
                Platform = "Unknown",
                UserAgent = "Unknown",
                IsOnline = true,
                ConnectionType = "unknown",
                SupportsServiceWorker = false,
                Orientation = 0
            };
            
            _isInitialized = true;
        }
    }

    public bool IsMobile()
    {
        return _deviceInfo?.IsMobile ?? false;
    }

    public bool IsTablet()
    {
        return _deviceInfo?.IsTablet ?? false;
    }

    public bool IsTouch()
    {
        return _deviceInfo?.IsTouch ?? false;
    }

    public bool IsDesktop()
    {
        return !IsMobile() && !IsTablet();
    }

    public DeviceInfo GetDeviceInfo()
    {
        return _deviceInfo ?? new DeviceInfo();
    }

    public async Task<DeviceInfo> GetCurrentDeviceInfoAsync()
    {
        if (!_isInitialized)
        {
            await InitializeAsync();
        }
        
        return GetDeviceInfo();
    }

    public string GetBreakpointClass()
    {
        if (!_isInitialized) return "desktop";
        
        var width = _deviceInfo?.ViewportWidth ?? 1920;
        
        return width switch
        {
            < 576 => "xs",
            < 768 => "sm", 
            < 992 => "md",
            < 1200 => "lg",
            < 1400 => "xl",
            _ => "xxl"
        };
    }

    public bool IsSmallScreen()
    {
        return (_deviceInfo?.ViewportWidth ?? 1920) < 768;
    }

    public bool IsLargeScreen()
    {
        return (_deviceInfo?.ViewportWidth ?? 1920) >= 1200;
    }

    private DeviceInfo ParseDeviceInfo(dynamic deviceData)
    {
        try
        {
            var jsonElement = (System.Text.Json.JsonElement)deviceData;
            
            return new DeviceInfo
            {
                IsMobile = jsonElement.GetProperty("isMobile").GetBoolean(),
                IsTablet = jsonElement.GetProperty("isTablet").GetBoolean(),
                IsTouch = jsonElement.GetProperty("isTouch").GetBoolean(),
                ViewportWidth = jsonElement.GetProperty("viewport").GetProperty("width").GetInt32(),
                ViewportHeight = jsonElement.GetProperty("viewport").GetProperty("height").GetInt32(),
                DevicePixelRatio = jsonElement.GetProperty("viewport").GetProperty("devicePixelRatio").GetDouble(),
                Platform = jsonElement.GetProperty("platform").GetString() ?? "Unknown",
                UserAgent = jsonElement.GetProperty("userAgent").GetString() ?? "Unknown",
                IsOnline = jsonElement.GetProperty("isOnline").GetBoolean(),
                ConnectionType = jsonElement.GetProperty("connectionType").GetString() ?? "unknown",
                SupportsServiceWorker = jsonElement.GetProperty("supportsServiceWorker").GetBoolean(),
                Orientation = jsonElement.GetProperty("viewport").GetProperty("orientation").GetInt32()
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse device info. Using defaults.");
            return new DeviceInfo();
        }
    }
}