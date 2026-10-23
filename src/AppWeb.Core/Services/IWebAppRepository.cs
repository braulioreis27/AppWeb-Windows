using AppWeb.Core.Models;

namespace AppWeb.Core.Services;

public interface IWebAppRepository
{
    Task<IReadOnlyList<WebAppDefinition>> GetAllAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(
        IReadOnlyList<WebAppDefinition> apps,
        CancellationToken cancellationToken = default);
}
