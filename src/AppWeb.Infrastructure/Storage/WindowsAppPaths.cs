using AppWeb.Core.Models;

namespace AppWeb.Infrastructure.Storage;

public static class WindowsAppPaths
{
    public static AppPaths CreateDefault()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var root = Path.Combine(localAppData, "AppWeb");

        return new AppPaths(
            RootDirectory: root,
            CatalogFile: Path.Combine(root, "catalog.json"),
            ProfilesDirectory: Path.Combine(root, "Profiles"),
            IconsDirectory: Path.Combine(root, "Icons"),
            LogsDirectory: Path.Combine(root, "Logs"),
            StartMenuDirectory: Path.Combine(
                appData,
                "Microsoft",
                "Windows",
                "Start Menu",
                "Programs",
                "AppWeb"));
    }
}
