using AppWeb.Core.Models;
using AppWeb.Infrastructure.Shortcuts;

namespace AppWeb.Tests;

public sealed class ShellLinkServiceTests
{
    [Fact]
    public async Task SyncAsync_CreatesAndDeletesShortcut()
    {
        var directory = Path.Combine(Path.GetTempPath(), "AppWeb.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var paths = new AppPaths(
                RootDirectory: directory,
                CatalogFile: Path.Combine(directory, "catalog.json"),
                ProfilesDirectory: Path.Combine(directory, "Profiles"),
                IconsDirectory: Path.Combine(directory, "Icons"),
                LogsDirectory: Path.Combine(directory, "Logs"),
                StartMenuDirectory: Path.Combine(directory, "Start Menu", "AppWeb"));

            var service = new ShellLinkService(paths);
            var app = new WebAppDefinition
            {
                Id = Guid.NewGuid(),
                Name = "Notion",
                Url = "https://notion.so/",
                BrowserId = BrowserIds.WebView2,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            var shortcutPath = await service.SyncAsync(app, null);

            Assert.True(File.Exists(shortcutPath));
            Assert.True(new FileInfo(shortcutPath).Length > 0);

            service.Delete(shortcutPath);

            Assert.False(File.Exists(shortcutPath));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
