namespace SuvidhaPOSInstaller.DynamicUI.Helpers;

public static class FormatHelper
{
    public static string Size(long bytes)
    {
        if (bytes <= 0) return "Pending";
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024L * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / 1024.0 / 1024:F1} MB";
        return $"{bytes / 1024.0 / 1024 / 1024:F2} GB";
    }

    public static string Time(TimeSpan value)
    {
        if (value <= TimeSpan.Zero) return "--:--";
        return value.TotalHours >= 1 ? value.ToString(@"hh\:mm\:ss") : value.ToString(@"mm\:ss");
    }
}