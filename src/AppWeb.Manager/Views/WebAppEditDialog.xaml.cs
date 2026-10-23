using AppWeb.Core.Models;
using AppWeb.Core.Validation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Storage.Pickers;

namespace AppWeb.Manager.Views;

public sealed partial class WebAppEditDialog : ContentDialog
{
    private string? _selectedIconPath;

    public WebAppEditDialog(
        IReadOnlyList<BrowserDescriptor> browsers,
        WebAppDefinition? app = null)
    {
        InitializeComponent();

        Title = app is null ? "Novo Web App" : "Editar Web App";
        BrowserBox.ItemsSource = browsers;

        IconPreview.ImageFailed += (_, _) =>
        {
            IconPreview.Visibility = Visibility.Collapsed;
            IconPlaceholder.Visibility = Visibility.Visible;
        };

        if (app is null)
        {
            BrowserBox.SelectedItem = browsers.FirstOrDefault(browser =>
                string.Equals(browser.Id, BrowserIds.WebView2, StringComparison.OrdinalIgnoreCase))
                ?? browsers.FirstOrDefault();
        }
        else
        {
            NameBox.Text = app.Name;
            UrlBox.Text = app.Url;
            DescriptionBox.Text = app.Description ?? string.Empty;
            ToolbarSwitch.IsOn = app.ShowToolbar;
            PrivateSwitch.IsOn = app.PrivateWindow;
            BrowserBox.SelectedItem = browsers.FirstOrDefault(browser =>
                string.Equals(browser.Id, app.BrowserId, StringComparison.OrdinalIgnoreCase))
                ?? browsers.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(app.IconPath) && File.Exists(app.IconPath))
            {
                ShowIconPreview(app.IconPath, app.IconIsCustom ? "Ícone personalizado" : "Favicon do site");
            }
        }
    }

    public WebAppDraft? Draft { get; private set; }

    public string? SelectedIconPath => _selectedIconPath;

    public bool UseFavicon { get; private set; }

    private async void ChooseIconButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (App.MainWindow is null)
            {
                ShowValidationError("A janela principal não está disponível.");
                return;
            }

            var picker = new FileOpenPicker(App.MainWindow.AppWindow.Id)
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                ViewMode = PickerViewMode.Thumbnail
            };

            picker.FileTypeFilter.Add(".ico");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".bmp");
            picker.FileTypeFilter.Add(".gif");
            picker.FileTypeFilter.Add(".webp");

            var file = await picker.PickSingleFileAsync();
            if (file is null)
            {
                return;
            }

            _selectedIconPath = file.Path;
            UseFavicon = false;
            ShowIconPreview(file.Path, Path.GetFileName(file.Path));
        }
        catch (Exception exception)
        {
            ShowValidationError($"Não foi possível abrir o seletor de imagens: {exception.Message}");
        }
    }

    private void UseFaviconButton_Click(object sender, RoutedEventArgs e)
    {
        _selectedIconPath = null;
        UseFavicon = true;
        IconPreview.Source = null;
        IconPreview.Visibility = Visibility.Collapsed;
        IconPlaceholder.Visibility = Visibility.Visible;
        IconPathText.Text = "O favicon será obtido do site ao salvar.";
    }

    private void ShowIconPreview(string path, string label)
    {
        try
        {
            IconPreview.Source = new BitmapImage(new Uri(path));
            IconPreview.Visibility = Visibility.Visible;
            IconPlaceholder.Visibility = Visibility.Collapsed;
            IconPathText.Text = label;
        }
        catch (Exception)
        {
            IconPreview.Source = null;
            IconPreview.Visibility = Visibility.Collapsed;
            IconPlaceholder.Visibility = Visibility.Visible;
            IconPathText.Text = label;
        }
    }

    private void OnPrimaryButtonClick(
        ContentDialog sender,
        ContentDialogButtonClickEventArgs args)
    {
        ErrorText.Visibility = Visibility.Collapsed;

        if (BrowserBox.SelectedItem is not BrowserDescriptor browser)
        {
            args.Cancel = true;
            ShowValidationError("Selecione um navegador.");
            return;
        }

        try
        {
            Draft = WebAppValidator.Normalize(new WebAppDraft(
                NameBox.Text,
                DescriptionBox.Text,
                UrlBox.Text,
                browser.Id,
                ToolbarSwitch.IsOn,
                PrivateSwitch.IsOn));
        }
        catch (ValidationException exception)
        {
            args.Cancel = true;
            ShowValidationError(exception.Message);
        }
    }

    private void ShowValidationError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
