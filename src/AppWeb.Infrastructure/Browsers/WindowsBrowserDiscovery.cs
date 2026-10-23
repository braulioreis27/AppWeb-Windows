using AppWeb.Core.Models;
using AppWeb.Core.Services;
using Microsoft.Win32;

namespace AppWeb.Infrastructure.Browsers;

public sealed class WindowsBrowserDiscovery : IBrowserDiscovery
{
    private readonly Lazy<IReadOnlyList<BrowserDescriptor>> _browsers = new(Discover);

    public IReadOnlyList<BrowserDescriptor> GetAvailable() => _browsers.Value;

    private static IReadOnlyList<BrowserDescriptor> Discover()
    {
        var browsers = new List<BrowserDescriptor>
        {
            new(BrowserIds.WebView2, "WebView2 (Edge)", BrowserKind.WebView2, null)
        };

        var knownPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddChromium(
            browsers,
            knownPaths,
            BrowserIds.Edge,
            "Microsoft Edge",
            "msedge.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe"));

        AddChromium(
            browsers,
            knownPaths,
            BrowserIds.Chrome,
            "Google Chrome",
            "chrome.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "Application", "chrome.exe"));

        AddChromium(
            browsers,
            knownPaths,
            BrowserIds.Brave,
            "Brave",
            "brave.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "BraveSoftware", "Brave-Browser", "Application", "brave.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "BraveSoftware", "Brave-Browser", "Application", "brave.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BraveSoftware", "Brave-Browser", "Application", "brave.exe"));

        AddChromium(
            browsers,
            knownPaths,
            BrowserIds.Vivaldi,
            "Vivaldi",
            "vivaldi.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Vivaldi", "Application", "vivaldi.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Vivaldi", "Application", "vivaldi.exe"));

        AddFirefox(browsers, knownPaths);

        return browsers;
    }

    private static void AddChromium(
        List<BrowserDescriptor> browsers,
        HashSet<string> knownPaths,
        string id,
        string displayName,
        string executableName,
        params string[] candidates)
    {
        var path = FindExecutable(executableName, candidates, knownPaths);
        if (path is not null)
        {
            browsers.Add(new BrowserDescriptor(id, displayName, BrowserKind.Chromium, path));
        }
    }

    private static void AddFirefox(List<BrowserDescriptor> browsers, HashSet<string> knownPaths)
    {
        var path = FindExecutable(
            "firefox.exe",
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Firefox", "firefox.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Mozilla Firefox", "firefox.exe")
            ],
            knownPaths);

        if (path is not null)
        {
            browsers.Add(new BrowserDescriptor(BrowserIds.Firefox, "Mozilla Firefox", BrowserKind.Firefox, path));
        }
    }

    private static string? FindExecutable(
        string executableName,
        IEnumerable<string> candidates,
        HashSet<string> knownPaths)
    {
        var registryPath = ReadAppPath(executableName);
        if (IsUsable(registryPath, knownPaths))
        {
            knownPaths.Add(registryPath!);
            return registryPath;
        }

        foreach (var candidate in candidates)
        {
            var expanded = Environment.ExpandEnvironmentVariables(candidate);
            if (IsUsable(expanded, knownPaths))
            {
                knownPaths.Add(expanded);
                return expanded;
            }
        }

        return null;
    }

    private static bool IsUsable(string? path, HashSet<string> knownPaths) =>
        !string.IsNullOrWhiteSpace(path) &&
        File.Exists(path) &&
        !knownPaths.Contains(path);

    private static string? ReadAppPath(string executableName)
    {
        var subKey = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{executableName}";

        foreach (var hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
        {
            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                    using var key = baseKey.OpenSubKey(subKey);
                    if (key?.GetValue(null) is not string rawValue)
                    {
                        continue;
                    }

                    var path = ExtractExecutablePath(rawValue);
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        return path;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                }
                catch (IOException)
                {
                }
            }
        }

        return null;
    }

    private static string? ExtractExecutablePath(string rawValue)
    {
        var value = Environment.ExpandEnvironmentVariables(rawValue.Trim());
        if (value.Length == 0)
        {
            return null;
        }

        if (value[0] == '"')
        {
            var closingQuote = value.IndexOf('"', 1);
            return closingQuote > 1 ? value[1..closingQuote] : null;
        }

        var exeIndex = value.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        return exeIndex >= 0 ? value[..(exeIndex + 4)].Trim() : value;
    }
}
