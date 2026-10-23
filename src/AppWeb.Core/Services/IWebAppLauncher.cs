using AppWeb.Core.Models;

namespace AppWeb.Core.Services;

public interface IWebAppLauncher
{
    Task LaunchAsync(WebAppDefinition app, CancellationToken cancellationToken = default);
}
