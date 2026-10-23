using System.Text.Json;
using AppWeb.Core.Models;
using AppWeb.Infrastructure.Storage;

namespace AppWeb.Tests;

public sealed class JsonWebAppRepositoryTests
{
    [Fact]
    public async Task SaveAndLoad_RoundTripsCatalog()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var path = Path.Combine(directory, "catalog.json");
            var repository = new JsonWebAppRepository(path);
            var app = CreateApp("Notion");

            await repository.SaveAsync([app]);
            var loaded = await repository.GetAllAsync();

            var result = Assert.Single(loaded);
            Assert.Equal(app.Id, result.Id);
            Assert.Equal(app.Name, result.Name);
            Assert.Equal(app.Url, result.Url);
            Assert.True(File.Exists(path));
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyWhenFileDoesNotExist()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var repository = new JsonWebAppRepository(Path.Combine(directory, "missing.json"));

            Assert.Empty(await repository.GetAllAsync());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task GetAll_RejectsFutureSchemaVersion()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var path = Path.Combine(directory, "catalog.json");
            await File.WriteAllTextAsync(path, """{"schemaVersion":99,"apps":[]}""");
            var repository = new JsonWebAppRepository(path);

            await Assert.ThrowsAsync<InvalidDataException>(() => repository.GetAllAsync());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static WebAppDefinition CreateApp(string name) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Url = "https://example.com/",
        BrowserId = BrowserIds.WebView2,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "AppWeb.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
