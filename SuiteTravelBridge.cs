using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using System.Text.Json;

namespace VieriNavPlotter;

internal readonly record struct RouteDispatchResult(bool Handled, bool Started, string Message);

internal sealed class SuiteTravelBridge
{
    private readonly ICallGateSubscriber<string, string> travelRoute;
    private readonly ICallGateSubscriber<bool> stopRoute;
    private readonly IPluginLog log;

    internal SuiteTravelBridge(IDalamudPluginInterface pi, IPluginLog log)
    {
        this.log = log;
        travelRoute = pi.GetIpcSubscriber<string, string>("AutoDuty.TravelVieriRoute");
        stopRoute = pi.GetIpcSubscriber<bool>("AutoDuty.StopVieriRouteTravel");
    }

    internal RouteDispatchResult Dispatch(uint territoryId, IReadOnlyList<RoutePoint> points, bool useFlight,
        bool useMesh, float tolerance, float lastPointTolerance, bool travelOnly)
    {
        if (territoryId == 0 || points.Count == 0)
            return new RouteDispatchResult(true, false, "This route has no usable destination.");

        IReadOnlyList<RoutePoint> requestedPoints = travelOnly ? [points[0]] : points;
        string request = JsonSerializer.Serialize(new
        {
            TerritoryId = territoryId,
            Points = requestedPoints,
            UseFlight = useFlight,
            UseMesh = useMesh,
            Tolerance = tolerance,
            LastPointTolerance = lastPointTolerance,
            Mode = travelOnly ? "travel" : "play",
        });

        try
        {
            string message = travelRoute.InvokeFunc(request);
            return new RouteDispatchResult(true,
                message.StartsWith("Started ", StringComparison.OrdinalIgnoreCase), message);
        }
        catch (Exception ex)
        {
            log.Debug(ex, "VieriAutoDuty route travel IPC is unavailable.");
            return new RouteDispatchResult(false, false,
                "VieriAutoDuty route travel is unavailable; using local vnavmesh when possible.");
        }
    }

    internal bool Stop()
    {
        try { return stopRoute.InvokeFunc(); }
        catch (Exception ex)
        {
            log.Debug(ex, "VieriAutoDuty route stop IPC is unavailable.");
            return false;
        }
    }
}
