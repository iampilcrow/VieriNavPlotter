using System.Numerics;
using System.Text.Json;

namespace VieriNavPlotter;

internal static class SuiteTravelRequestContract
{
    internal static string Create(uint territoryId, IReadOnlyList<RoutePoint> points, bool useFlight,
        bool useMesh, float tolerance, float lastPointTolerance, bool travelOnly,
        uint vendorTargetDataId = 0, Vector3? vendorPosition = null)
    {
        IReadOnlyList<RoutePoint> requestedPoints = travelOnly ? [points[0]] : points;
        bool includeVendorArrival = vendorTargetDataId != 0 && (!travelOnly || points.Count == 1);
        RoutePoint? serializedVendorPosition = includeVendorArrival && vendorPosition is { } position
            ? RoutePoint.From(position)
            : null;
        return JsonSerializer.Serialize(new
        {
            TerritoryId = territoryId,
            Points = requestedPoints,
            UseFlight = useFlight,
            UseMesh = useMesh,
            Tolerance = tolerance,
            LastPointTolerance = lastPointTolerance,
            Mode = travelOnly ? "travel" : "play",
            VendorTargetDataId = includeVendorArrival ? vendorTargetDataId : 0,
            VendorPosition = serializedVendorPosition,
        });
    }
}
