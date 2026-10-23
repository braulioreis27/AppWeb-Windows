using System.Diagnostics;
using AppWeb.Core.Models;
using AppWeb.Core.Services;
using AppWeb.Core.Validation;

namespace AppWeb.Infrastructure.Launching;

public sealed class ProcessWebAppLauncher(
    IProfileStore profiles,
    IBrowserDiscovery browserDiscovery) : IWebAppLauncher
{
    public Task LaunchAsync(WebAppDefinition app, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var browser = browserDiscovery.GetAvailable().FirstOrDefault(candidate =>
            string.Equals(candidate.Id, app.BrowserId, StringComparison.OrdinalIgnoreCase));

        if (browser is null || !browser.IsAvailable)
        {
            throw new ValidationException("O navegador configurado para este Web App não está disponível.");
        }

        if (browser.Kind == BrowserKind.WebView2)
        {
            LaunchWebViewHost(app);
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(browser.ExecutablePath))
        {
            throw new ValidationException("O executável do navegador não foi encontrado.");
        }

        var profileDirectory = profiles.GetProfileDirectory(app.Id);
        Directory.CreateDirectory(profileDirectory);

        var startInfo = new ProcessStartInfo
        {
            FileName = browser.ExecutablePath,
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(browser.ExecutablePath) ?? AppContext.BaseDirectory
        };

        if (browser.Kind == BrowserKind.Chromium)
        {
            AddChromiumArguments(startInfo, app, browser, profileDirectory);
        }
        else
        {
            AddFirefoxArguments(startInfo, app, profileDirectory);
        }

        Process.Start(startInfo);
        return Task.CompletedTask;
    }

    private static void LaunchWebViewHost(WebAppDefinition app)
    {
        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new InvalidOperationException("Não foi possível localizar o executável do AppWeb.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = false,
            WorkingDirectory = AppContext.BaseDirectory
        };

        startInfo.ArgumentList.Add("--webapp");
        startInfo.ArgumentList.Add(app.Id.ToString("D"));

        Process.Start(startInfo);
    }

    private static void AddChromiumArguments(
        ProcessStartInfo startInfo,
        WebAppDefinition app,
        BrowserDescriptor browser,
        string profileDirectory)
    {
        startInfo.ArgumentList.Add($"--user-data-dir={profileDirectory}");
        startInfo.ArgumentList.Add("--no-first-run");
        startInfo.ArgumentList.Add("--no-default-browser-check");

        if (app.PrivateWindow)
        {
            startInfo.ArgumentList.Add(
                string.Equals(browser.Id, BrowserIds.Edge, StringComparison.OrdinalIgnoreCase)
                    ? "--inprivate"
                    : "--incognito");
        }

        if (app.ShowToolbar)
        {
            startInfo.ArgumentList.Add("--new-window");
            startInfo.ArgumentList.Add(app.Url);
        }
        else
        {
            startInfo.ArgumentList.Add($"--app={app.Url}");
        }
    }

    private static void AddFirefoxArguments(
        ProcessStartInfo startInfo,
        WebAppDefinition app,
        string profileDirectory)
    {
        startInfo.ArgumentList.Add("-profile");
        startInfo.ArgumentList.Add(profileDirectory);
        startInfo.ArgumentList.Add("-no-remote");
        startInfo.ArgumentList.Add("-new-instance");

        if (app.PrivateWindow)
        {
            startInfo.ArgumentList.Add("-private-window");
        }

        startInfo.ArgumentList.Add(app.Url);
    }
}
