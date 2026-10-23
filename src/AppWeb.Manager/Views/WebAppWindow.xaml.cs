using System.Diagnostics;
using AppWeb.Core.Models;
using AppWeb.Manager.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Windows.Graphics;

namespace AppWeb.Manager.Views;

public sealed partial class WebAppWindow : Window
{
    private readonly WebAppDefinition _app;
    private readonly WebViewHostService _host;
    private CoreWebView2? _core;
    private bool _initialized;

    public WebAppWindow(AppServices services, WebAppDefinition app)
    {
        InitializeComponent();

        _app = app;
        _host = new WebViewHostService(services);
        Title = app.Name;
        WindowInterop.SetIcon(this, app.IconPath);

        if (!app.ShowToolbar)
        {
            Toolbar.Visibility = Visibility.Collapsed;
            ToolbarRow.Height = new GridLength(0);
        }

        try
        {
            AppWindow.Resize(new SizeInt32(1200, 800));
        }
        catch (Exception)
        {
        }

        RootGrid.Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            _core = await _host.InitializeAsync(WebView, _app);
            _core.NavigationStarting += OnNavigationStarting;
            _core.NavigationCompleted += OnNavigationCompleted;
            _core.SourceChanged += (_, _) => UpdateNavigationState();
            _core.DocumentTitleChanged += OnDocumentTitleChanged;
            _core.NewWindowRequested += OnNewWindowRequested;

            _core.Navigate(_app.Url);
            UpdateNavigationState();
        }
        catch (Exception exception)
        {
            await ShowErrorAsync(
                "Não foi possível abrir o Web App",
                exception.Message);
            Close();
        }
    }

    private void OnNavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
    {
        if (!IsWebUri(args.Uri))
        {
            args.Cancel = true;
            TryOpenExternal(args.Uri);
            return;
        }

        LoadingBar.Visibility = Visibility.Visible;
        AddressBox.Text = args.Uri;
    }

    private void OnNavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        LoadingBar.Visibility = Visibility.Collapsed;
        UpdateNavigationState();
    }

    private void OnDocumentTitleChanged(CoreWebView2 sender, object args)
    {
        Title = string.IsNullOrWhiteSpace(sender.DocumentTitle)
            ? _app.Name
            : sender.DocumentTitle;
    }

    private void OnNewWindowRequested(CoreWebView2 sender, CoreWebView2NewWindowRequestedEventArgs args)
    {
        args.Handled = true;
        TryOpenExternal(args.Uri);
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (_core?.CanGoBack == true)
        {
            _core.GoBack();
        }
    }

    private void ForwardButton_Click(object sender, RoutedEventArgs e)
    {
        if (_core?.CanGoForward == true)
        {
            _core.GoForward();
        }
    }

    private void ReloadButton_Click(object sender, RoutedEventArgs e)
    {
        _core?.Reload();
    }

    private void HomeButton_Click(object sender, RoutedEventArgs e)
    {
        _core?.Navigate(_app.Url);
    }

    private void OpenInBrowserButton_Click(object sender, RoutedEventArgs e)
    {
        TryOpenExternal(_core?.Source ?? _app.Url);
    }

    private void UpdateNavigationState()
    {
        BackButton.IsEnabled = _core?.CanGoBack == true;
        ForwardButton.IsEnabled = _core?.CanGoForward == true;

        if (!string.IsNullOrWhiteSpace(_core?.Source))
        {
            AddressBox.Text = _core.Source;
        }
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        _core?.Stop();
        _ = _host.CleanupAsync();
    }

    private static bool IsWebUri(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static void TryOpenExternal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            !Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return;
        }

        if (uri.Scheme != Uri.UriSchemeHttp &&
            uri.Scheme != Uri.UriSchemeHttps &&
            !string.Equals(uri.Scheme, "mailto", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Process.Start(new ProcessStartInfo(uri.AbsoluteUri)
        {
            UseShellExecute = true
        });
    }

    private async Task ShowErrorAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = RootGrid.XamlRoot,
            Title = title,
            Content = message,
            CloseButtonText = "Fechar"
        };

        await dialog.ShowAsync();
    }
}
