using System.Text.Json;
using AppWeb.Core.Models;
using AppWeb.Core.Services;

namespace AppWeb.Infrastructure.Storage;

public sealed class JsonWebAppRepository(string catalogPath) : IWebAppRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<IReadOnlyList<WebAppDefinition>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            if (!File.Exists(catalogPath))
            {
                return [];
            }

            await using var stream = new FileStream(
                catalogPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 16 * 1024,
                useAsync: true);

            var catalog = await JsonSerializer.DeserializeAsync<WebAppCatalog>(
                stream,
                SerializerOptions,
                cancellationToken);

            if (catalog is null)
            {
                return [];
            }

            if (catalog.SchemaVersion > WebAppCatalog.CurrentSchemaVersion)
            {
                throw new InvalidDataException(
                    $"O catálogo usa a versão {catalog.SchemaVersion}, mas este aplicativo suporta até {WebAppCatalog.CurrentSchemaVersion}.");
            }

            return catalog.Apps;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(
        IReadOnlyList<WebAppDefinition> apps,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(apps);
        await _gate.WaitAsync(cancellationToken);

        var temporaryPath = string.Empty;

        try
        {
            var directory = Path.GetDirectoryName(catalogPath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new InvalidOperationException("O caminho do catálogo é inválido.");
            }

            Directory.CreateDirectory(directory);
            temporaryPath = Path.Combine(directory, $".catalog.{Guid.NewGuid():N}.tmp");

            var catalog = new WebAppCatalog
            {
                Apps = apps.ToList()
            };

            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 16 * 1024,
                useAsync: true))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    catalog,
                    SerializerOptions,
                    cancellationToken);

                await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, catalogPath, overwrite: true);
        }
        finally
        {
            if (temporaryPath.Length > 0 && File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }

            _gate.Release();
        }
    }
}
