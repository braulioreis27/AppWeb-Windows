using AppWeb.Core.Models;
using AppWeb.Core.Validation;
using AppWeb.Manager.Models;
using AppWeb.Manager.Services;
using AppWeb.Manager.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace AppWeb.Manager;

public sealed partial class MainWindow : Window
{
    private readonly AppServices _services;
    private List<WebAppListItem> _allItems = [];
    private bool _loaded;

    public MainWindow(AppServices services)
    {
        InitializeComponent();

        _services = services;
        Title = "AppWeb";

        try
        {
            SystemBackdrop = new MicaBackdrop();
        }
        catch (Exception)
        {
        }

        try
        {
            AppWindow.Resize(new SizeInt32(1000, 720));
        }
        catch (Exception)
        {
        }

        RootGrid.Loaded += (_, _) =>
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            _ = LoadAsync();
        };
    }

    private async Task LoadAsync()
    {
        SetLoading(true);

        try
        {
            var apps = await _services.WebApps.GetAllAsync();
            var browsers = _services.Browsers.GetAvailable()
                .ToDictionary(browser => browser.Id, StringComparer.OrdinalIgnoreCase);

            _allItems = apps.Select(app =>
            {
                var browserName = browsers.TryGetValue(app.BrowserId, out var browser)
                    ? browser.DisplayName
                    : app.BrowserId;

                return new WebAppListItem(app, browserName);
            }).ToList();

            ApplyFilter();
        }
        catch (Exception exception)
        {
            ShowError(exception.Message);
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void ApplyFilter()
    {
        var query = SearchBox.Text;
        var visible = _allItems.Where(item => item.Matches(query)).ToList();
        AppsList.ItemsSource = visible;

        AppsCountText.Text = _allItems.Count switch
        {
            0 => "Nenhum aplicativo criado",
            1 => "1 aplicativo",
            _ => $"{_allItems.Count} aplicativos"
        };

        var showEmptyState = visible.Count == 0;
        EmptyState.Visibility = showEmptyState ? Visibility.Visible : Visibility.Collapsed;

        if (showEmptyState)
        {
            EmptyStateTitle.Text = _allItems.Count == 0
                ? "Nenhum Web App ainda"
                : "Nenhum resultado";

            EmptyStateDescription.Text = _allItems.Count == 0
                ? "Crie um atalho no Menu Iniciar para usar qualquer site como um aplicativo."
                : "Tente pesquisar por outro nome ou endereço.";
        }
    }

    private void SetLoading(bool isLoading)
    {
        LoadingRing.IsActive = isLoading;
        LoadingRing.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
        AppsList.Visibility = isLoading ? Visibility.Collapsed : Visibility.Visible;

        if (isLoading)
        {
            EmptyState.Visibility = Visibility.Collapsed;
        }
    }

    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var browsers = _services.Browsers.GetAvailable();
        if (browsers.Count == 0)
        {
            ShowError("Nenhum navegador compatível foi encontrado.");
            return;
        }

        var dialog = new WebAppEditDialog(browsers)
        {
            XamlRoot = Content.XamlRoot
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary || dialog.Draft is null)
        {
            return;
        }

        try
        {
            var app = await _services.WebApps.CreateAsync(dialog.Draft);
            await TryApplySelectedIconAsync(app, dialog, refreshFavicon: true);
            await LoadAsync();
            ShowSuccess($"“{app.Name}” foi adicionado ao Menu Iniciar.");
        }
        catch (Exception exception)
        {
            ShowError(ToUserMessage(exception));
        }
    }

    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string id } || !Guid.TryParse(id, out var appId))
        {
            return;
        }

        var item = _allItems.FirstOrDefault(candidate => candidate.App.Id == appId);
        if (item is null)
        {
            return;
        }

        var browserList = _services.Browsers.GetAvailable().ToList();
        if (!browserList.Any(browser =>
                string.Equals(browser.Id, item.App.BrowserId, StringComparison.OrdinalIgnoreCase)))
        {
            browserList.Add(new BrowserDescriptor(
                item.App.BrowserId,
                $"{item.App.BrowserId} (não encontrado)",
                BrowserKind.Chromium,
                null));
        }

        var dialog = new WebAppEditDialog(browserList, item.App)
        {
            XamlRoot = Content.XamlRoot
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary || dialog.Draft is null)
        {
            return;
        }

        try
        {
            var urlChanged = !string.Equals(
                item.App.Url,
                dialog.Draft.Url,
                StringComparison.OrdinalIgnoreCase);

            var updated = await _services.WebApps.UpdateAsync(appId, dialog.Draft);
            await TryApplySelectedIconAsync(updated, dialog, refreshFavicon: urlChanged);
            await LoadAsync();
            ShowSuccess($"“{updated.Name}” foi atualizado.");
        }
        catch (Exception exception)
        {
            ShowError(ToUserMessage(exception));
        }
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string id } || !Guid.TryParse(id, out var appId))
        {
            return;
        }

        var item = _allItems.FirstOrDefault(candidate => candidate.App.Id == appId);
        if (item is null)
        {
            return;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = Content.XamlRoot,
            Title = "Excluir Web App?",
            Content = $"“{item.Name}” será removido do Menu Iniciar e seu perfil local será apagado.",
            PrimaryButtonText = "Excluir",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Close
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        try
        {
            await _services.WebApps.DeleteAsync(appId);
            await LoadAsync();
            ShowSuccess($"“{item.Name}” foi excluído.");
        }
        catch (Exception exception)
        {
            ShowError(ToUserMessage(exception));
        }
    }

    private async void LaunchButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string id } || !Guid.TryParse(id, out var appId))
        {
            return;
        }

        try
        {
            await _services.WebApps.LaunchAsync(appId);
        }
        catch (Exception exception)
        {
            ShowError(ToUserMessage(exception));
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loaded)
        {
            ApplyFilter();
        }
    }

    private async Task TryApplySelectedIconAsync(
        WebAppDefinition app,
        WebAppEditDialog dialog,
        bool refreshFavicon)
    {
        try
        {
            await ApplyIconAsync(app, dialog, refreshFavicon);
        }
        catch (Exception exception)
        {
            ShowWarning($"O Web App foi salvo, mas o ícone não pôde ser atualizado: {exception.Message}");
        }
    }

    private async Task ApplyIconAsync(
        WebAppDefinition app,
        WebAppEditDialog dialog,
        bool refreshFavicon)
    {
        if (dialog.UseFavicon)
        {
            await _services.WebApps.SetIconAsync(app.Id, null);
            await TryApplyFaviconAsync(
                app with { IconPath = null, IconIsCustom = false },
                force: true);
            return;
        }

        if (!string.IsNullOrWhiteSpace(dialog.SelectedIconPath))
        {
            var iconPath = await _services.Icons.ImportIconAsync(dialog.SelectedIconPath, app.Id);
            if (string.IsNullOrWhiteSpace(iconPath))
            {
                ShowWarning("Não foi possível importar a imagem. O favicon do site será usado.");
                await TryApplyFaviconAsync(app, force: true);
                return;
            }

            await _services.WebApps.SetIconAsync(app.Id, iconPath, isCustom: true);
            return;
        }

        if (app.IconIsCustom && !string.IsNullOrWhiteSpace(app.IconPath))
        {
            return;
        }

        await TryApplyFaviconAsync(app, refreshFavicon);
    }

    private async Task TryApplyFaviconAsync(WebAppDefinition app, bool force)
    {
        if (!force && !string.IsNullOrWhiteSpace(app.IconPath))
        {
            return;
        }

        try
        {
            var iconPath = await _services.Icons.DownloadFaviconAsync(app.Url, app.Id);
            if (!string.IsNullOrWhiteSpace(iconPath))
            {
                await _services.WebApps.SetIconAsync(app.Id, iconPath);
            }
        }
        catch (Exception)
        {
        }
    }

    private void ShowSuccess(string message)
    {
        StatusBar.Severity = InfoBarSeverity.Success;
        StatusBar.Message = message;
        StatusBar.IsOpen = true;
    }

    private void ShowWarning(string message)
    {
        StatusBar.Severity = InfoBarSeverity.Warning;
        StatusBar.Message = message;
        StatusBar.IsOpen = true;
    }

    private void ShowError(string message)
    {
        StatusBar.Severity = InfoBarSeverity.Error;
        StatusBar.Message = message;
        StatusBar.IsOpen = true;
    }

    private static string ToUserMessage(Exception exception) =>
        exception is ValidationException or InvalidOperationException or IOException or UnauthorizedAccessException
            ? exception.Message
            : $"Ocorreu um erro inesperado: {exception.Message}";
}
