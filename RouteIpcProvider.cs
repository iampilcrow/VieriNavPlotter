using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace VieriNavPlotter;

internal sealed class RouteIpcProvider : IDisposable
{
    private readonly Configuration config;
    private readonly ICallGateProvider<int> version;
    private readonly ICallGateProvider<string, uint, uint, string?> resolve;
    private readonly ICallGateProvider<string> list;
    private readonly ICallGateProvider<string, string?> get;
    private readonly ICallGateProvider<string, string> run;
    private readonly NavmeshBridge navmesh;

    internal RouteIpcProvider(IDalamudPluginInterface pi, Configuration config, NavmeshBridge navmesh)
    {
        this.config = config;
        this.navmesh = navmesh;
        version = pi.GetIpcProvider<int>("VieriNavPlotter.GetApiVersion");
        resolve = pi.GetIpcProvider<string, uint, uint, string?>("VieriNavPlotter.ResolveOverride");
        list = pi.GetIpcProvider<string>("VieriNavPlotter.ListRoutes");
        get = pi.GetIpcProvider<string, string?>("VieriNavPlotter.GetRoute");
        run = pi.GetIpcProvider<string, string>("VieriNavPlotter.RunRoute");
        version.RegisterFunc(() => 1);
        resolve.RegisterFunc(Resolve);
        list.RegisterFunc(ListRoutes);
        get.RegisterFunc(GetRoute);
        run.RegisterFunc(RunRoute);
    }

    private string? Resolve(string kind, uint territoryId, uint targetDataId) =>
        RoutePolicy.SerializeOverride(config.Routes, kind, territoryId, targetDataId);

    private string ListRoutes() => System.Text.Json.JsonSerializer.Serialize(config.Routes.Select(route =>
        new RouteLibraryEntry(route.Id, route.Name, route.TerritoryId, route.Points.Count, route.Notes, route.Tags)));

    private string? GetRoute(string nameOrId)
    {
        PlottedRoute? route = RoutePolicy.FindByNameOrId(config.Routes, nameOrId);
        return route is null ? null : System.Text.Json.JsonSerializer.Serialize(route);
    }

    private string RunRoute(string nameOrId)
    {
        PlottedRoute? route = RoutePolicy.FindByNameOrId(config.Routes, nameOrId);
        if (route is null) return $"Route '{nameOrId}' was not found.";
        if (Plugin.ClientState.TerritoryType != route.TerritoryId) return $"Route '{route.Name}' belongs to territory {route.TerritoryId}.";
        if (Plugin.Objects.LocalPlayer is not { } player) return "The character is not ready.";
        navmesh.TryPlay(route, player.Position, out string message);
        return message;
    }

    public void Dispose()
    {
        run.UnregisterFunc();
        get.UnregisterFunc();
        list.UnregisterFunc();
        resolve.UnregisterFunc();
        version.UnregisterFunc();
    }
}
