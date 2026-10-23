using System.Drawing;
using System.Drawing.Imaging;
using AppWeb.Core.Models;
using AppWeb.Infrastructure.Icons;

namespace AppWeb.Tests;

public sealed class FaviconServiceTests
{
    [Fact]
    public async Task ImportIconAsync_CreatesHashedIconAndDeduplicatesContent()
    {
        var root = Path.Combine(Path.GetTempPath(), "AppWeb.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var paths = CreatePaths(root);
            var red = CreatePng(root, "red.png", Color.Red);
            var blue = CreatePng(root, "blue.png", Color.Blue);
            var appId = Guid.NewGuid();

            using var service = new FaviconService(paths);

            var first = await service.ImportIconAsync(red, appId);
            var duplicate = await service.ImportIconAsync(red, appId);
            var second = await service.ImportIconAsync(blue, appId);

            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.Equal(first, duplicate);
            Assert.NotEqual(first, second);
            Assert.True(File.Exists(first));
            Assert.True(File.Exists(second));

            service.Delete(first);

            Assert.False(File.Exists(first));
            Assert.True(File.Exists(second));
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
        StartMenuDirectory: Path.Combine(root, "Start Menu"));

    private static string CreatePng(string directory, string name, Color color)
    {
        var path = Path.Combine(directory, name);
        using var bitmap = new Bitmap(8, 8);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(color);
        bitmap.Save(path, ImageFormat.Png);
        return path;
    }
}
