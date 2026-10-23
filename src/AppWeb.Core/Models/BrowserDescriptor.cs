namespace AppWeb.Core.Models;

public enum BrowserKind
{
    WebView2,
    Chromium,
    Firefox
}

public sealed record BrowserDescriptor(
    string Id,
    string DisplayName,
    BrowserKind Kind,
    string? ExecutablePath)
{
    public bool IsAvailable => Kind == BrowserKind.WebView2 || !string.IsNullOrWhiteSpace(ExecutablePath);
}
