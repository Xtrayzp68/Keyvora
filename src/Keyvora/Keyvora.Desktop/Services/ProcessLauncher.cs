namespace Keyvora.Desktop.Services;

using System.Diagnostics;

public static class ProcessLauncher
{
    public static Process? Launch(string path, string? arguments = null, string? workingDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        var startInfo = new ProcessStartInfo
        {
            FileName = path,
            Arguments = arguments ?? string.Empty,
            WorkingDirectory = workingDirectory ?? string.Empty,
            UseShellExecute = true
        };

        try
        {
            return Process.Start(startInfo);
        }
        catch
        {
            return null;
        }
    }

    public static Process? LaunchUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        return Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
}
