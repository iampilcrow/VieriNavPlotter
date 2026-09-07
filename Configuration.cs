using Dalamud.Configuration;
using Dalamud.Plugin;

namespace VieriNavPlotter;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public float RecordingIntervalSeconds { get; set; } = 1f;
    public float MinimumPointDistance { get; set; } = 0.75f;
    public bool ShowWorldPreview { get; set; } = true;
    public bool ShowPointNumbers { get; set; } = true;
    public List<PlottedRoute> Routes { get; set; } = [];
    public Guid? SelectedRouteId { get; set; }

    [NonSerialized] private IDalamudPluginInterface? pluginInterface;

    internal void Initialize(IDalamudPluginInterface pi)
    {
        pluginInterface = pi;
        Routes ??= [];
        foreach (PlottedRoute route in Routes)
        {
            route.Points ??= [];
            route.Name ??= "Unnamed route";
            route.TargetLabel ??= string.Empty;
            route.Notes ??= string.Empty;
            route.Tags ??= string.Empty;
        }
    }

    internal void Save() => pluginInterface?.SavePluginConfig(this);
}
