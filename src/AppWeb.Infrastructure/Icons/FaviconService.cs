using System.Drawing;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AppWeb.Core.Models;
using AppWeb.Core.Services;
using AppWeb.Core.Validation;

namespace AppWeb.Infrastructure.Icons;

public sealed class FaviconService : IIconService, IDisposable
{
    private const int MaxPageBytes = 2 * 1024 * 1024;
    private const int MaxIconBytes = 4 * 1024 * 1024;

    private readonly AppPaths _paths;
    private readonly HttpClient _httpClient;

    public FaviconService(AppPaths paths)
    {
        _paths = paths;
        _httpClient = new HttpClient(new SocketsHttpHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All,
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5
        })
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppWeb/1.0");
    }

    public async Task<string?> DownloadFaviconAsync(
        string pageUrl,
        Guid appId,
        CancellationToken cancellationToken = default)
    {
        if (!UrlPolicy.TryNormalize(pageUrl, out var normalized, out _))
        {
            return null;
        }

        var pageUri = new Uri(normalized);
        var candidates = new List<Uri>();

        try
        {
            var html = await GetStringAsync(pageUri, MaxPageBytes, cancellationToken);
            candidates.AddRange(FindIconCandidates(html, pageUri));
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
        }

        candidates.Add(new Uri(pageUri, "/favicon.ico"));

        foreach (var candidate in candidates.DistinctBy(uri => uri.AbsoluteUri))
        {
            try
            {
                var bytes = await GetBytesAsync(candidate, MaxIconBytes, cancellationToken);
                var path = await SaveIconAsync(bytes, appId, cancellationToken);
                if (path is not null)
                {
                    return path;
                }
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
            }
        }

        return null;
    }

    public async Task<string?> ImportIconAsync(
        string sourceFilePath,
        Guid appId,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourceFilePath))
        {
            return null;
        }

        var bytes = await File.ReadAllBytesAsync(sourceFilePath, cancellationToken);
        return await SaveIconAsync(bytes, appId, cancellationToken);
    }

    public void Delete(string? iconPath)
    {
        if (string.IsNullOrWhiteSpace(iconPath) || !File.Exists(iconPath))
        {
            return;
        }

        var fullPath = Path.GetFullPath(iconPath);
        var iconsRoot = Path.GetFullPath(_paths.IconsDirectory)
            .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (fullPath.StartsWith(iconsRoot, StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(fullPath);
        }
    }

    public void Dispose() => _httpClient.Dispose();

    private async Task<string?> SaveIconAsync(
        byte[] bytes,
        Guid appId,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_paths.IconsDirectory);

        byte[] iconBytes;
        try
        {
            if (IsIco(bytes))
            {
                iconBytes = bytes;
            }
            else
            {
                using var stream = new MemoryStream(bytes, writable: false);
                using var image = Image.FromStream(
                    stream,
                    useEmbeddedColorManagement: false,
                    validateImageData: true);

                iconBytes = IcoEncoder.Encode(image);
            }
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            ExternalException or
            NotSupportedException)
        {
            return null;
        }

        var hash = Convert.ToHexString(SHA256.HashData(iconBytes))[..16].ToLowerInvariant();
        var iconPath = Path.Combine(_paths.IconsDirectory, $"{appId:N}-{hash}.ico");
        var temporaryPath = iconPath + ".tmp";

        try
        {
            await File.WriteAllBytesAsync(temporaryPath, iconBytes, cancellationToken);
            File.Move(temporaryPath, iconPath, overwrite: true);
            return iconPath;
        }
        catch (IOException)
        {
            return null;
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private async Task<string> GetStringAsync(
        Uri uri,
        int maxBytes,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        await CopyLimitedAsync(stream, buffer, maxBytes, cancellationToken);
        return System.Text.Encoding.UTF8.GetString(buffer.ToArray());
    }

    private async Task<byte[]> GetBytesAsync(
        Uri uri,
        int maxBytes,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > maxBytes)
        {
            throw new HttpRequestException("O ícone é muito grande.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        await CopyLimitedAsync(stream, buffer, maxBytes, cancellationToken);
        return buffer.ToArray();
    }

    private static async Task CopyLimitedAsync(
        Stream source,
        Stream destination,
        int maxBytes,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[16 * 1024];
        var total = 0;

        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                return;
            }

            total += read;
            if (total > maxBytes)
            {
                throw new HttpRequestException("O conteúdo excede o limite permitido.");
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
    }

    private static IEnumerable<Uri> FindIconCandidates(string html, Uri pageUri)
    {
        var document = new HtmlParser().ParseDocument(html);
        var candidates = new List<(int Priority, Uri Uri)>();

        foreach (var element in document.QuerySelectorAll("link[rel][href]"))
        {
            var rel = element.GetAttribute("rel")?.ToLowerInvariant() ?? string.Empty;
            var href = element.GetAttribute("href");
            if (string.IsNullOrWhiteSpace(href) || href.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var priority = rel.Contains("apple-touch-icon", StringComparison.Ordinal)
                ? 0
                : rel.Contains("icon", StringComparison.Ordinal)
                    ? 1
                    : 3;

            if (priority == 3)
            {
                continue;
            }

            if (TryResolve(pageUri, href, out var iconUri))
            {
                candidates.Add((priority, iconUri));
            }
        }

        foreach (var element in document.QuerySelectorAll("meta[property='og:image'][content]"))
        {
            var content = element.GetAttribute("content");
            if (!string.IsNullOrWhiteSpace(content) && TryResolve(pageUri, content, out var imageUri))
            {
                candidates.Add((2, imageUri));
            }
        }

        return candidates
            .OrderBy(candidate => candidate.Priority)
            .Select(candidate => candidate.Uri);
    }

    private static bool TryResolve(Uri baseUri, string value, out Uri uri)
    {
        uri = null!;

        if (!Uri.TryCreate(baseUri, value.Trim(), out var resolved) ||
            (resolved.Scheme != Uri.UriSchemeHttp && resolved.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(resolved.UserInfo))
        {
            return false;
        }

        uri = resolved;
        return true;
    }

    private static bool IsIco(byte[] bytes) =>
        bytes.Length >= 4 &&
        bytes[0] == 0 &&
        bytes[1] == 0 &&
        bytes[2] == 1 &&
        bytes[3] == 0;
}
