namespace AppWeb.Core.Services;

public interface IIconService
{
    Task<string?> DownloadFaviconAsync(
        string pageUrl,
        Guid appId,
        CancellationToken cancellationToken = default);

    Task<string?> ImportIconAsync(
        string sourceFilePath,
        Guid appId,
        CancellationToken cancellationToken = default);

    void Delete(string? iconPath);
}
