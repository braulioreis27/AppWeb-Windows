namespace AppWeb.Core.Services;

public interface IProfileStore
{
    string GetProfileDirectory(Guid appId);

    Task DeleteAsync(Guid appId, CancellationToken cancellationToken = default);
}
