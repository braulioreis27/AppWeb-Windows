using AppWeb.Core.Services;

namespace AppWeb.Infrastructure.Profiles;

public sealed class ProfileStore(string profilesDirectory) : IProfileStore
{
    public string GetProfileDirectory(Guid appId) =>
        Path.Combine(profilesDirectory, appId.ToString("N"));

    public async Task DeleteAsync(Guid appId, CancellationToken cancellationToken = default)
    {
        var path = GetProfileDirectory(appId);
        if (!Directory.Exists(path))
        {
            return;
        }

        const int attempts = 10;
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                Directory.Delete(path, recursive: true);
                return;
            }
            catch (DirectoryNotFoundException)
            {
                return;
            }
            catch (IOException) when (attempt < attempts)
            {
                await Task.Delay(250, cancellationToken);
            }
            catch (UnauthorizedAccessException) when (attempt < attempts)
            {
                await Task.Delay(250, cancellationToken);
            }
        }
    }
}
