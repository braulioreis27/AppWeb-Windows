using System.Drawing;
using System.Drawing.Imaging;
using AppWeb.Core.Models;
using AppWeb.Core.Services;
using AppWeb.Infrastructure.Browsers;
using AppWeb.Infrastructure.Icons;
using AppWeb.Infrastructure.Profiles;
using AppWeb.Infrastructure.Shortcuts;
using AppWeb.Infrastructure.Storage;

namespace AppWeb.Tests;

public sealed class WebAppIntegrationTests
{
    [Fact]
    public async Task CustomIconFlow_PersistsShortcutAndRemovesOldIcon()
    {
        var root = Path.Combine(Path.GetTempPath(), "AppWeb.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var paths = CreatePaths(root);
            var repository = new JsonWebAppRepository(paths.CatalogFile);
            var browsers = new AvailableWebView2();
            var shortcuts = new ShellLinkService(paths);
            using var icons = new FaviconService(paths);
            var profiles = new ProfileStore(paths.ProfilesDirectory);
            var launcher = new NoopLauncher();
            using var service = new WebAppService(
                repository,
                browsers,
                shortcuts,
                icons,
                profiles,
                launcher);

            var app = await service.CreateAsync(new WebAppDraft(
                "Integration",
                "Teste",
                "https://example.com/",
                BrowserIds.WebView2,
                false,
                false));
            var shortcutPath = app.ShortcutPath;
            Assert.NotNull(shortcutPath);
            Assert.True(File.Exists(shortcutPath));

            var red = CreatePng(root, "red.png", Color.Red);
            var firstIcon = await icons.ImportIconAsync(red, app.Id);
            Assert.NotNull(firstIcon);
            await service.SetIconAsync(app.Id, firstIcon, isCustom: true);

            var firstSaved = Assert.Single(await repository.GetAllAsync());
            Assert.True(firstSaved.IconIsCustom);
            Assert.Equal(firstIcon, firstSaved.IconPath);

            var blue = CreatePng(root, "blue.png", Color.Blue);
            var secondIcon = await icons.ImportIconAsync(blue, app.Id);
            Assert.NotNull(secondIcon);
            Assert.NotEqual(firstIcon, secondIcon);

            await service.SetIconAsync(app.Id, secondIcon, isCustom: true);

            Assert.False(File.Exists(firstIcon));
            Assert.True(File.Exists(secondIcon));

            var secondSaved = Assert.Single(await repository.GetAllAsync());
            Assert.True(secondSaved.IconIsCustom);
            Assert.Equal(secondIcon, secondSaved.IconPath);

            await service.DeleteAsync(app.Id);

            Assert.Empty(await repository.GetAllAsync());
            Assert.False(File.Exists(secondIcon));
            Assert.False(File.Exists(shortcutPath));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static AppPaths CreatePaths(string root) => new(
        RootDirectory: root,
        CatalogFile: Path.Combine(root, "catalog.json"),
        ProfilesDirectory: Path.Combine(root, "Profiles"),
        IconsDirectory: Path.Combine(root, "Icons"),
        LogsDirectory: Path.Combine(root, "Logs"),
        StartMenuDirectory: Path.Combine(root, "Start Menu", "AppWeb"));

    private static string CreatePng(string directory, string name, Color color)
    {
        var path = Path.Combine(directory, name);
        using var bitmap = new Bitmap(8, 8);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(color);
        bitmap.Save(path, ImageFormat.Png);
        return path;
    }

    private sealed class AvailableWebView2 : IBrowserDiscovery
    {
        public IReadOnlyList<BrowserDescriptor> GetAvailable() =>
        [
            new(BrowserIds.WebView2, "WebView2", BrowserKind.WebView2, null)
        ];
    }

    private sealed class NoopLauncher : IWebAppLauncher
    {
        public Task LaunchAsync(WebAppDefinition app, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
