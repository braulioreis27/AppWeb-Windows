namespace AppWeb.Core.Models;

public sealed record WebAppDraft(
    string Name,
    string? Description,
    string Url,
    string BrowserId,
    bool ShowToolbar,
    bool PrivateWindow);
