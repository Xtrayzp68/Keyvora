namespace Keyvora.Desktop.Actions;

public sealed class ActionMetadata
{
    public string TypeId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string IconPath { get; init; } = string.Empty;
    public string Category { get; init; } = "General";
    public bool RequiresConfiguration { get; init; } = true;
}
