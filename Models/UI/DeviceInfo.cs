namespace finaid.Models.UI;

public class DeviceInfo
{
    public bool IsMobile { get; set; }
    public bool IsTablet { get; set; }
    public bool IsTouch { get; set; }
    public int ViewportWidth { get; set; } = 1920;
    public int ViewportHeight { get; set; } = 1080;
    public double DevicePixelRatio { get; set; } = 1.0;
    public string Platform { get; set; } = "Unknown";
    public string UserAgent { get; set; } = "Unknown";
    public bool IsOnline { get; set; } = true;
    public string ConnectionType { get; set; } = "unknown";
    public bool SupportsServiceWorker { get; set; } = false;
    public int Orientation { get; set; } = 0; // 0 = portrait, 90/-90 = landscape

    // Computed properties
    public bool IsDesktop => !IsMobile && !IsTablet;
    public bool IsPortrait => Orientation == 0 || Orientation == 180;
    public bool IsLandscape => Orientation == 90 || Orientation == -90;
    public bool IsSmallScreen => ViewportWidth < 768;
    public bool IsMediumScreen => ViewportWidth >= 768 && ViewportWidth < 1200;
    public bool IsLargeScreen => ViewportWidth >= 1200;
    public bool IsHighDensity => DevicePixelRatio > 1.5;
    public bool IsSlowConnection => ConnectionType == "slow-2g" || ConnectionType == "2g";
    
    public string GetBreakpointName()
    {
        return ViewportWidth switch
        {
            < 576 => "xs",
            < 768 => "sm",
            < 992 => "md", 
            < 1200 => "lg",
            < 1400 => "xl",
            _ => "xxl"
        };
    }

    public string GetDeviceTypeString()
    {
        if (IsMobile) return "Mobile";
        if (IsTablet) return "Tablet";
        return "Desktop";
    }

    public string GetOrientationString()
    {
        return IsPortrait ? "Portrait" : "Landscape";
    }

    public override string ToString()
    {
        return $"{GetDeviceTypeString()} - {ViewportWidth}x{ViewportHeight} ({GetBreakpointName()}) - {GetOrientationString()}";
    }
}