namespace AppWeb.Core.Models;

public sealed record AppPaths(
    string RootDirectory,
    string CatalogFile,
    string ProfilesDirectory,
    string IconsDirectory,
    string LogsDirectory,
    string StartMenuDirectory);
