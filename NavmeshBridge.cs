using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;

namespace VieriNavPlotter;

internal sealed class NavmeshBridge
{
    private readonly ICallGateSubscriber<bool> isReady;
    private readonly ICallGateSubscriber<bool> isRunning;
    private readonly ICallGateSubscriber<List<Vector3>> listWaypoints;
    private readonly ICallGateSubscriber<List<Vector3>, bool, object> moveTo;
    private readonly ICallGateSubscriber<float, object> setTolerance;
    private readonly ICallGateSubscriber<object> stop;
    private readonly IPluginLog log;
    private Vector3? ownedVisualizationDestination;

    internal NavmeshBridge(IDalamudPluginInterface pi, IPluginLog log)
    {
        this.log = log;
        isReady = pi.GetIpcSubscriber<bool>("vnavmesh.Nav.IsReady");
        isRunning = pi.GetIpcSubscriber<bool>("vnavmesh.Path.IsRunning");
        listWaypoints = pi.GetIpcSubscriber<List<Vector3>>("vnavmesh.Path.ListWaypoints");
        moveTo = pi.GetIpcSubscriber<List<Vector3>, bool, object>("vnavmesh.Path.MoveTo");
        setTolerance = pi.GetIpcSubscriber<float, object>("vnavmesh.Path.SetTolerance");
        stop = pi.GetIpcSubscriber<object>("vnavmesh.Path.Stop");
    }

    internal bool OwnsNavigationVisualization => ownedVisualizationDestination is not null;

    internal IReadOnlyList<Vector3> GetActiveWaypoints(bool suiteAuthorizesNavigation)
    {
        try
        {
            if (!isRunning.InvokeFunc())
            {
                ownedVisualizationDestination = null;
                return [];
            }

            Vector3[] waypoints = listWaypoints.InvokeFunc()
                .Where(point => float.IsFinite(point.X) && float.IsFinite(point.Y) && float.IsFinite(point.Z))
                .ToArray();

            if (suiteAuthorizesNavigation)
            {
                // AutoDuty explicitly owns this NavPlotter route or gear-shopping path.
                // It supersedes any earlier local playback marker.
                ownedVisualizationDestination = null;
                return waypoints;
            }

            if (ownedVisualizationDestination is not { } expectedDestination || waypoints.Length == 0)
                return [];

            // If another plugin replaces the active path before we observe an idle frame,
            // stop drawing immediately rather than leaking our overlay into its navigation.
            if (Vector3.Distance(waypoints[^1], expectedDestination) > 1.5f)
            {
                ownedVisualizationDestination = null;
                return [];
            }

            return waypoints;
        }
        catch
        {
            // vnavmesh is optional and can reload independently. A missing frame should simply
            // hide the live overlay rather than logging an exception every render tick.
            return [];
        }
    }

    internal bool TryPlay(PlottedRoute route, Vector3 playerPosition, out string message)
    {
        if (!RoutePolicy.IsValid(route)) { message = "The route needs at least two valid points."; return false; }
        if (Vector3.Distance(playerPosition, route.Points[0].Position) > 25f)
        {
            message = "Move within 25 yalms of the first point before testing this route.";
            return false;
        }
        try
        {
            if (!isReady.InvokeFunc()) { message = "vnavmesh is not ready."; return false; }
            setTolerance.InvokeAction(route.Tolerance);
            moveTo.InvokeAction(route.Points.Select(point => point.Position).ToList(), route.UseFlight);
            ownedVisualizationDestination = route.Points[^1].Position;
            message = $"Testing {route.Name}.";
            return true;
        }
        catch (Exception ex)
        {
            log.Warning(ex, "Could not start route playback.");
            message = "Could not start vnavmesh playback.";
            return false;
        }
    }

    internal bool TryMove(IReadOnlyList<RoutePoint> points, bool useFlight, float tolerance, out string message)
    {
        if (points.Count == 0)
        {
            message = "The route has no points.";
            return false;
        }

        try
        {
            if (!isReady.InvokeFunc()) { message = "vnavmesh is not ready."; return false; }
            setTolerance.InvokeAction(Math.Clamp(tolerance, 0.1f, 20f));
            moveTo.InvokeAction(points.Select(point => point.Position).ToList(), useFlight);
            ownedVisualizationDestination = points[^1].Position;
            message = $"Local route playback started ({points.Count} point{(points.Count == 1 ? string.Empty : "s")}).";
            return true;
        }
        catch (Exception ex)
        {
            log.Warning(ex, "Could not start local route playback.");
            message = "Could not start local vnavmesh playback.";
            return false;
        }
    }

    internal void Stop()
    {
        ownedVisualizationDestination = null;
        try { stop.InvokeAction(); }
        catch (Exception ex) { log.Debug(ex, "vnavmesh stop was unavailable."); }
    }
}
