using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using System.Numerics;

namespace VieriNavPlotter;

internal readonly record struct RouteDispatchResult(bool Handled, bool Started, string Message);

internal sealed class SuiteTravelBridge
{
    private readonly ICallGateSubscriber<string, string> travelRoute;
    private readonly ICallGateSubscriber<bool> stopRoute;
    private readonly ICallGateSubscriber<bool> isVisualizationActive;
    private readonly IPluginLog log;

    internal SuiteTravelBridge(IDalamudPluginInterface pi, IPluginLog log)
    {
        this.log = log;
        travelRoute = pi.GetIpcSubscriber<string, string>("AutoDuty.TravelVieriRoute");
        stopRoute = pi.GetIpcSubscriber<bool>("AutoDuty.StopVieriRouteTravel");
        isVisualizationActive = pi.GetIpcSubscriber<bool>("AutoDuty.IsNavPlotterVisualizationActive");
    }

    internal RouteDispatchResult Dispatch(uint territoryId, IReadOnlyList<RoutePoint> points, bool useFlight,
        bool useMesh, float tolerance, float lastPointTolerance, bool travelOnly,
        uint vendorTargetDataId = 0, Vector3? vendorPosition = null)
    {
        if (territoryId == 0 || points.Count == 0)
            return new RouteDispatchResult(true, false, "This route has no usable destination.");

        string request = SuiteTravelRequestContract.Create(territoryId, points, useFlight, useMesh,
            tolerance, lastPointTolerance, travelOnly, vendorTargetDataId, vendorPosition);

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

    internal bool IsVisualizationAuthorized()
    {
        try { return isVisualizationActive.InvokeFunc(); }
        catch { return false; }
    }
}
