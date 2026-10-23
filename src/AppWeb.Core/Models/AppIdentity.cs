namespace AppWeb.Core.Models;

public static class AppIdentity
{
    public const string ManagerAumid = "BraulioReis.AppWeb.Manager";

    public static string GetWebAppAumid(Guid id) => $"BraulioReis.AppWeb.{id:N}";
}
