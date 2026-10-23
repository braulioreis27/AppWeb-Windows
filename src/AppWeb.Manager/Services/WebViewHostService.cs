using AppWeb.Core.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace AppWeb.Manager.Services;

public sealed class WebViewHostService(AppServices services)
{
    private string? _privateProfileDirectory;

    public async Task<CoreWebView2> InitializeAsync(
        WebView2 webView,
        WebAppDefinition app,
        CancellationToken cancellationToken = default)
    {
        var profileRoot = services.Profiles.GetProfileDirectory(app.Id);
        string dataFolder;

        if (app.PrivateWindow)
        {
            _privateProfileDirectory = Path.Combine(
                profileRoot,
                $"webview2-private-{Environment.ProcessId}");

            dataFolder = _privateProfileDirectory;
        }
        else
        {
            dataFolder = Path.Combine(profileRoot, "webview2");
        }

        Directory.CreateDirectory(dataFolder);
        cancellationToken.ThrowIfCancellationRequested();

        var environment = await CoreWebView2Environment.CreateWithOptionsAsync(
            null,
            dataFolder,
            null);

        cancellationToken.ThrowIfCancellationRequested();
        await webView.EnsureCoreWebView2Async(environment);

        var core = webView.CoreWebView2;
        core.Settings.AreDevToolsEnabled = false;
        core.Settings.AreDefaultContextMenusEnabled = true;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.IsZoomControlEnabled = true;
        core.Settings.IsPasswordAutosaveEnabled = false;
        core.Settings.IsGeneralAutofillEnabled = false;

        return core;
    }

    public async Task CleanupAsync()
    {
        if (string.IsNullOrWhiteSpace(_privateProfileDirectory))
        {
            return;
        }

        const int attempts = 10;
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                if (Directory.Exists(_privateProfileDirectory))
                {
                    Directory.Delete(_privateProfileDirectory, recursive: true);
                }

                return;
            }
            catch (DirectoryNotFoundException)
            {
                return;
            }
            catch (IOException) when (attempt < attempts)
            {
                await Task.Delay(200);
            }
            catch (UnauthorizedAccessException) when (attempt < attempts)
            {
                await Task.Delay(200);
            }
        }
    }
}
