using AppWeb.Core.Models;
using AppWeb.Core.Validation;

namespace AppWeb.Core.Services;

public sealed class WebAppService(
    IWebAppRepository repository,
    IBrowserDiscovery browserDiscovery,
    IShortcutService shortcuts,
    IIconService icons,
    IProfileStore profiles,
    IWebAppLauncher launcher) : IWebAppService, IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<IReadOnlyList<WebAppDefinition>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var apps = await repository.GetAllAsync(cancellationToken);
        return apps
            .OrderBy(app => app.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public async Task<WebAppDefinition> CreateAsync(
        WebAppDraft draft,
        CancellationToken cancellationToken = default)
    {
        var normalized = WebAppValidator.Normalize(draft);
        await _gate.WaitAsync(cancellationToken);

        try
        {
            EnsureBrowserAvailable(normalized.BrowserId);
            var apps = (await repository.GetAllAsync(cancellationToken)).ToList();
            var now = DateTimeOffset.UtcNow;
            var app = new WebAppDefinition
            {
                Id = Guid.NewGuid(),
                Name = normalized.Name,
                Description = normalized.Description,
                Url = normalized.Url,
                BrowserId = normalized.BrowserId,
                ShowToolbar = normalized.ShowToolbar,
                PrivateWindow = normalized.PrivateWindow,
                CreatedAt = now,
                UpdatedAt = now
            };

            apps.Add(app);
            await repository.SaveAsync(apps, cancellationToken);

            string? createdShortcutPath = null;
            try
            {
                createdShortcutPath = await shortcuts.SyncAsync(app, null, cancellationToken);
                app = app with { ShortcutPath = createdShortcutPath };
                Replace(apps, app);
                await repository.SaveAsync(apps, cancellationToken);
                return app;
            }
            catch
            {
                if (createdShortcutPath is not null)
                {
                    shortcuts.Delete(createdShortcutPath);
                }

                apps.RemoveAll(candidate => candidate.Id == app.Id);
                await repository.SaveAsync(apps, CancellationToken.None);
                throw;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<WebAppDefinition> UpdateAsync(
        Guid id,
        WebAppDraft draft,
        CancellationToken cancellationToken = default)
    {
        var normalized = WebAppValidator.Normalize(draft);
        await _gate.WaitAsync(cancellationToken);

        try
        {
            EnsureBrowserAvailable(normalized.BrowserId);
            var apps = (await repository.GetAllAsync(cancellationToken)).ToList();
            var index = apps.FindIndex(app => app.Id == id);
            if (index < 0)
            {
                throw new ValidationException("Web App não encontrado.");
            }

            var previous = apps[index];
            var updated = previous with
            {
                Name = normalized.Name,
                Description = normalized.Description,
                Url = normalized.Url,
                BrowserId = normalized.BrowserId,
                ShowToolbar = normalized.ShowToolbar,
                PrivateWindow = normalized.PrivateWindow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            string? shortcutPath = null;
            try
            {
                shortcutPath = await shortcuts.SyncAsync(
                    updated,
                    previous.ShortcutPath,
                    cancellationToken);

                updated = updated with { ShortcutPath = shortcutPath };
                apps[index] = updated;
                await repository.SaveAsync(apps, cancellationToken);
                return updated;
            }
            catch
            {
                if (shortcutPath is not null)
                {
                    shortcuts.Delete(shortcutPath);
                }

                try
                {
                    await shortcuts.SyncAsync(previous, null, CancellationToken.None);
                }
                catch (Exception)
                {
                }

                throw;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SetIconAsync(
        Guid id,
        string? iconPath,
        bool isCustom = false,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var apps = (await repository.GetAllAsync(cancellationToken)).ToList();
            var index = apps.FindIndex(app => app.Id == id);
            if (index < 0)
            {
                throw new ValidationException("Web App não encontrado.");
            }

            var previous = apps[index];
            var normalizedIconPath = string.IsNullOrWhiteSpace(iconPath) ? null : iconPath;
            var custom = isCustom && normalizedIconPath is not null;
            var iconChanged =
                !string.Equals(previous.IconPath, normalizedIconPath, StringComparison.OrdinalIgnoreCase) ||
                previous.IconIsCustom != custom;

            if (!iconChanged)
            {
                return;
            }

            var updated = previous with
            {
                IconPath = normalizedIconPath,
                IconIsCustom = custom,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            apps[index] = updated;
            await repository.SaveAsync(apps, cancellationToken);

            try
            {
                var shortcutPath = await shortcuts.SyncAsync(
                    updated,
                    previous.ShortcutPath,
                    cancellationToken);

                if (!string.Equals(shortcutPath, updated.ShortcutPath, StringComparison.OrdinalIgnoreCase))
                {
                    updated = updated with { ShortcutPath = shortcutPath };
                    apps[index] = updated;
                    await repository.SaveAsync(apps, cancellationToken);
                }
            }
            catch
            {
                apps[index] = previous;
                await repository.SaveAsync(apps, CancellationToken.None);

                if (!string.Equals(previous.IconPath, normalizedIconPath, StringComparison.OrdinalIgnoreCase))
                {
                    icons.Delete(normalizedIconPath);
                }

                try
                {
                    await shortcuts.SyncAsync(previous, null, CancellationToken.None);
                }
                catch (Exception)
                {
                }

                throw;
            }

            if (!string.IsNullOrWhiteSpace(previous.IconPath) &&
                !string.Equals(previous.IconPath, normalizedIconPath, StringComparison.OrdinalIgnoreCase))
            {
                icons.Delete(previous.IconPath);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var apps = (await repository.GetAllAsync(cancellationToken)).ToList();
            var app = apps.FirstOrDefault(candidate => candidate.Id == id);
            if (app is null)
            {
                return;
            }

            apps.Remove(app);
            await repository.SaveAsync(apps, cancellationToken);

            shortcuts.Delete(app.ShortcutPath);
            icons.Delete(app.IconPath);

            try
            {
                await profiles.DeleteAsync(app.Id, cancellationToken);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task LaunchAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var apps = await repository.GetAllAsync(cancellationToken);
        var app = apps.FirstOrDefault(candidate => candidate.Id == id)
            ?? throw new ValidationException("Web App não encontrado.");

        await launcher.LaunchAsync(app, cancellationToken);
    }

    public void Dispose() => _gate.Dispose();

    private void EnsureBrowserAvailable(string browserId)
    {
        var available = browserDiscovery.GetAvailable();
        var browser = available.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, browserId, StringComparison.OrdinalIgnoreCase));

        if (browser is null || !browser.IsAvailable)
        {
            throw new ValidationException("O navegador selecionado não está disponível.");
        }
    }

    private static void Replace(List<WebAppDefinition> apps, WebAppDefinition app)
    {
        var index = apps.FindIndex(candidate => candidate.Id == app.Id);
        if (index >= 0)
        {
            apps[index] = app;
        }
    }
}
