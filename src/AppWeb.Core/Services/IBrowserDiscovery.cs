using AppWeb.Core.Models;

namespace AppWeb.Core.Services;

public interface IBrowserDiscovery
{
    IReadOnlyList<BrowserDescriptor> GetAvailable();
}
