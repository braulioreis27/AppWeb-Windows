namespace AppWeb.Core.Models;

public sealed record WebAppDefinition
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string Url { get; init; } = string.Empty;

    public string BrowserId { get; init; } = BrowserIds.WebView2;

    public string? IconPath { get; init; }

    public bool IconIsCustom { get; init; }

    public string? ShortcutPath { get; init; }

    public bool ShowToolbar { get; init; }

    public bool PrivateWindow { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}
