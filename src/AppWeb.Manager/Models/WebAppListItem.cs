using AppWeb.Core.Models;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace AppWeb.Manager.Models;

public sealed class WebAppListItem
{
    public WebAppListItem(WebAppDefinition app, string browserDisplayName)
    {
        App = app;
        Id = app.Id.ToString("D");
        Name = app.Name;
        Url = app.Url;
        Description = app.Description ?? string.Empty;
        BrowserDisplayName = browserDisplayName;
        Icon = LoadIcon(app.IconPath);
    }

    public WebAppDefinition App { get; }

    public string Id { get; }

    public string Name { get; }

    public string Url { get; }

    public string Description { get; }

    public string BrowserDisplayName { get; }

    public ImageSource? Icon { get; }

    public string Details =>
        string.IsNullOrWhiteSpace(Description)
            ? $"{BrowserDisplayName} · perfil isolado"
            : $"{BrowserDisplayName} · {Description}";

    public bool Matches(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
               Url.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
               Description.Contains(query, StringComparison.CurrentCultureIgnoreCase);
    }

    private static ImageSource? LoadIcon(string? iconPath)
    {
        if (string.IsNullOrWhiteSpace(iconPath) || !File.Exists(iconPath))
        {
            return null;
        }

        try
        {
            return new BitmapImage(new Uri(iconPath));
        }
        catch (Exception)
        {
            return null;
        }
    }
}
