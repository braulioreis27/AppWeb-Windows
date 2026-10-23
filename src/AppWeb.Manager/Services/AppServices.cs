using AppWeb.Core.Services;
using AppWeb.Infrastructure.Browsers;
using AppWeb.Infrastructure.Icons;
using AppWeb.Infrastructure.Launching;
using AppWeb.Infrastructure.Profiles;
using AppWeb.Infrastructure.Shortcuts;
using AppWeb.Infrastructure.Storage;

namespace AppWeb.Manager.Services;

public sealed class AppServices : IDisposable
{
    private AppServices(
        Core.Models.AppPaths paths,
        IWebAppService webApps,
        IBrowserDiscovery browsers,
        IIconService icons,
        IProfileStore profiles,
        IShortcutService shortcuts,
        IWebAppLauncher launcher)
    {
        Paths = paths;
        WebApps = webApps;
        Browsers = browsers;
        Icons = icons;
        Profiles = profiles;
        Shortcuts = shortcuts;
        Launcher = launcher;
    }

    public Core.Models.AppPaths Paths { get; }

    public IWebAppService WebApps { get; }

    public IBrowserDiscovery Browsers { get; }

    public IIconService Icons { get; }

    public IProfileStore Profiles { get; }

    public IShortcutService Shortcuts { get; }

    public IWebAppLauncher Launcher { get; }

    public static AppServices Create()
    {
        var paths = WindowsAppPaths.CreateDefault();
        Directory.CreateDirectory(paths.RootDirectory);

        var repository = new JsonWebAppRepository(paths.CatalogFile);
        var browsers = new WindowsBrowserDiscovery();
        var profiles = new ProfileStore(paths.ProfilesDirectory);
        var icons = new FaviconService(paths);
        var shortcuts = new ShellLinkService(paths);
        var launcher = new ProcessWebAppLauncher(profiles, browsers);
        var webApps = new WebAppService(
            repository,
            browsers,
            shortcuts,
            icons,
            profiles,
            launcher);

        return new AppServices(paths, webApps, browsers, icons, profiles, shortcuts, launcher);
    }

    public void Dispose()
    {
        if (WebApps is IDisposable disposable)
        {
            disposable.Dispose();
        }

        if (Icons is IDisposable disposableIcons)
        {
            disposableIcons.Dispose();
        }
    }
}
