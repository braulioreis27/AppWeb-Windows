using AppWeb.Core.Models;
using AppWeb.Core.Services;
using AppWeb.Core.Validation;

namespace AppWeb.Tests;

public sealed class WebAppServiceTests
{
    [Fact]
    public async Task CreateAsync_SavesAppAndCreatesShortcut()
    {
        var context = new ServiceContext();
        using var service = context.CreateService();

        var app = await service.CreateAsync(CreateDraft("Notion"));

        var saved = Assert.Single(await context.Repository.GetAllAsync());
        Assert.Equal(app.Id, saved.Id);
        Assert.Equal(context.Shortcuts.NextPath, app.ShortcutPath);
        Assert.Equal(1, context.Shortcuts.SyncCount);
        Assert.Null(context.Shortcuts.LastPreviousPath);
    }

    [Fact]
    public async Task UpdateAsync_PreservesIdAndUpdatesShortcut()
    {
        var context = new ServiceContext();
        using var service = context.CreateService();
        var app = await service.CreateAsync(CreateDraft("Notion"));

        var updated = await service.UpdateAsync(
            app.Id,
            CreateDraft("Notion Workspace", "https://notion.so/workspace"));

        Assert.Equal(app.Id, updated.Id);
        Assert.Equal("Notion Workspace", updated.Name);
        Assert.Equal(app.ShortcutPath, context.Shortcuts.LastPreviousPath);
        Assert.Equal(updated.ShortcutPath, (Assert.Single(await context.Repository.GetAllAsync())).ShortcutPath);
    }

    [Fact]
    public async Task DeleteAsync_RemovesCatalogShortcutIconAndProfile()
    {
        var context = new ServiceContext();
        using var service = context.CreateService();
        var app = await service.CreateAsync(CreateDraft("Notion"));
        await service.SetIconAsync(app.Id, @"C:\icons\notion.ico");

        await service.DeleteAsync(app.Id);

        Assert.Empty(await context.Repository.GetAllAsync());
        Assert.Equal(app.ShortcutPath, Assert.Single(context.Shortcuts.Deleted));
        Assert.Equal(@"C:\icons\notion.ico", Assert.Single(context.Icons.Deleted));
        Assert.Equal(app.Id, Assert.Single(context.Profiles.Deleted));
    }

    [Fact]
    public async Task CreateAsync_RejectsUnavailableBrowser()
    {
        var context = new ServiceContext();
        using var service = context.CreateService();

        var draft = CreateDraft("Notion") with { BrowserId = BrowserIds.Chrome };

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(draft));
        Assert.Empty(await context.Repository.GetAllAsync());
    }

    [Fact]
    public async Task LaunchAsync_DelegatesToLauncher()
    {
        var context = new ServiceContext();
        using var service = context.CreateService();
        var app = await service.CreateAsync(CreateDraft("Notion"));

        await service.LaunchAsync(app.Id);

        Assert.Equal(app.Id, context.Launcher.Launched?.Id);
    }

    [Fact]
    public async Task SetIconAsync_UpdatesCatalogAndShortcut()
    {
        var context = new ServiceContext();
        using var service = context.CreateService();
        var app = await service.CreateAsync(CreateDraft("Notion"));

        await service.SetIconAsync(app.Id, @"C:\icons\notion.ico");

        var saved = Assert.Single(await context.Repository.GetAllAsync());
        Assert.Equal(@"C:\icons\notion.ico", saved.IconPath);
        Assert.Equal(2, context.Shortcuts.SyncCount);
    }

    [Fact]
    public async Task SetIconAsync_CanMarkIconAsCustomWithoutDeletingSamePath()
    {
        var context = new ServiceContext();
        using var service = context.CreateService();
        var app = await service.CreateAsync(CreateDraft("Notion"));
        var iconPath = @"C:\icons\notion.ico";

        await service.SetIconAsync(app.Id, iconPath, isCustom: true);
        await service.SetIconAsync(app.Id, iconPath, isCustom: false);

        var saved = Assert.Single(await context.Repository.GetAllAsync());
        Assert.False(saved.IconIsCustom);
        Assert.Equal(iconPath, saved.IconPath);
        Assert.Empty(context.Icons.Deleted);
    }

    private static WebAppDraft CreateDraft(string name, string url = "https://notion.so") =>
        new(name, "Notas", url, BrowserIds.WebView2, false, false);

    private sealed class ServiceContext
    {
        public InMemoryRepository Repository { get; } = new();

        public FakeBrowserDiscovery Browsers { get; } = new();

        public FakeShortcutService Shortcuts { get; } = new();

        public FakeIconService Icons { get; } = new();

        public FakeProfileStore Profiles { get; } = new();

        public FakeLauncher Launcher { get; } = new();

        public WebAppService CreateService() => new(
            Repository,
            Browsers,
            Shortcuts,
            Icons,
            Profiles,
            Launcher);
    }

    private sealed class InMemoryRepository : IWebAppRepository
    {
        private List<WebAppDefinition> _apps = [];

        public Task<IReadOnlyList<WebAppDefinition>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WebAppDefinition>>(_apps.ToList());

        public Task SaveAsync(
            IReadOnlyList<WebAppDefinition> apps,
            CancellationToken cancellationToken = default)
        {
            _apps = apps.ToList();
            return Task.CompletedTask;
        }
    }

    private sealed class FakeBrowserDiscovery : IBrowserDiscovery
    {
        public IReadOnlyList<BrowserDescriptor> GetAvailable() =>
        [
            new(BrowserIds.WebView2, "WebView2", BrowserKind.WebView2, null)
        ];
    }

    private sealed class FakeShortcutService : IShortcutService
    {
        public string NextPath { get; private set; } = @"C:\shortcuts\app.lnk";

        public int SyncCount { get; private set; }

        public string? LastPreviousPath { get; private set; }

        public List<string> Deleted { get; } = [];

        public Task<string> SyncAsync(
            WebAppDefinition app,
            string? previousShortcutPath,
            CancellationToken cancellationToken = default)
        {
            SyncCount++;
            LastPreviousPath = previousShortcutPath;
            NextPath = $@"C:\shortcuts\{app.Name}.lnk";
            return Task.FromResult(NextPath);
        }

        public void Delete(string? shortcutPath)
        {
            if (shortcutPath is not null)
            {
                Deleted.Add(shortcutPath);
            }
        }
    }

    private sealed class FakeIconService : IIconService
    {
        public List<string> Deleted { get; } = [];

        public Task<string?> DownloadFaviconAsync(
            string pageUrl,
            Guid appId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(@"C:\icons\favicon.ico");

        public Task<string?> ImportIconAsync(
            string sourceFilePath,
            Guid appId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(sourceFilePath);

        public void Delete(string? iconPath)
        {
            if (iconPath is not null)
            {
                Deleted.Add(iconPath);
            }
        }
    }

    private sealed class FakeProfileStore : IProfileStore
    {
        public List<Guid> Deleted { get; } = [];

        public string GetProfileDirectory(Guid appId) => Path.Combine(Path.GetTempPath(), appId.ToString("N"));

        public Task DeleteAsync(Guid appId, CancellationToken cancellationToken = default)
        {
            Deleted.Add(appId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeLauncher : IWebAppLauncher
    {
        public WebAppDefinition? Launched { get; private set; }

        public Task LaunchAsync(WebAppDefinition app, CancellationToken cancellationToken = default)
        {
            Launched = app;
            return Task.CompletedTask;
        }
    }
}
