using AppWeb.Core.Models;

namespace AppWeb.Core.Services;

public interface IShortcutService
{
    Task<string> SyncAsync(
        WebAppDefinition app,
        string? previousShortcutPath,
        CancellationToken cancellationToken = default);

    void Delete(string? shortcutPath);
}
