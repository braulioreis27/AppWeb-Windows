namespace AppWeb.Core.Models;

public sealed class WebAppCatalog
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public List<WebAppDefinition> Apps { get; init; } = [];
}
