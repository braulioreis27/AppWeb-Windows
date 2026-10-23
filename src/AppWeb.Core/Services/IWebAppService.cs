using AppWeb.Core.Models;

namespace AppWeb.Core.Services;

public interface IWebAppService
{
    Task<IReadOnlyList<WebAppDefinition>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<WebAppDefinition> CreateAsync(WebAppDraft draft, CancellationToken cancellationToken = default);

    Task<WebAppDefinition> UpdateAsync(
        Guid id,
        WebAppDraft draft,
        CancellationToken cancellationToken = default);

    Task SetIconAsync(
        Guid id,
        string? iconPath,
        bool isCustom = false,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task LaunchAsync(Guid id, CancellationToken cancellationToken = default);
}
