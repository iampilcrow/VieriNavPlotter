using System.Text.Json;
using System.Text.Json.Serialization;
using System.Numerics;

namespace VieriNavPlotter;

public enum RouteBindingKind
{
    None,
    GearVendor,
}

public sealed class RoutePoint
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    [JsonIgnore]
    public Vector3 Position => new(X, Y, Z);

    public static RoutePoint From(Vector3 value) => new() { X = value.X, Y = value.Y, Z = value.Z };
}

public sealed class PlottedRoute
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "New route";
    public uint TerritoryId { get; set; }
    public List<RoutePoint> Points { get; set; } = [];
    public string Notes { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public bool UseMesh { get; set; }
    public bool UseFlight { get; set; }
    public float Tolerance { get; set; } = 0.75f;
    public float LastPointTolerance { get; set; } = 3f;
    public RouteBindingKind BindingKind { get; set; }
    public uint TargetDataId { get; set; }
    public string TargetLabel { get; set; } = string.Empty;
    public bool OverrideEnabled { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed record RouteOverrideContract(
    int SchemaVersion,
    Guid RouteId,
    string Name,
    uint TerritoryId,
    uint TargetDataId,
    bool UseMesh,
    bool UseFlight,
    float Tolerance,
    float LastPointTolerance,
    IReadOnlyList<RoutePoint> Points);

public sealed record RouteLibraryEntry(Guid Id, string Name, uint TerritoryId, int PointCount, string Notes, string Tags);

internal static class RoutePolicy
{
    internal static bool IsValid(PlottedRoute route) =>
        route.TerritoryId != 0 && route.Points.Count >= 2 &&
        route.Points.All(point => float.IsFinite(point.X) && float.IsFinite(point.Y) && float.IsFinite(point.Z)) &&
        route.Tolerance is >= 0.1f and <= 20f && route.LastPointTolerance is >= 0.1f and <= 30f;

    internal static PlottedRoute? Resolve(IEnumerable<PlottedRoute> routes, RouteBindingKind kind, uint territoryId, uint targetDataId) =>
        routes.Where(route => route.OverrideEnabled && route.BindingKind == kind && route.TerritoryId == territoryId &&
                              route.TargetDataId == targetDataId && IsValid(route))
            .OrderByDescending(route => route.UpdatedAtUtc)
            .FirstOrDefault();

    internal static string? SerializeOverride(IEnumerable<PlottedRoute> routes, string kind, uint territoryId, uint targetDataId)
    {
        if (!Enum.TryParse(kind, true, out RouteBindingKind parsed) || parsed == RouteBindingKind.None)
            return null;
        PlottedRoute? route = Resolve(routes, parsed, territoryId, targetDataId);
        return route is null ? null : JsonSerializer.Serialize(new RouteOverrideContract(
            1, route.Id, route.Name, route.TerritoryId, route.TargetDataId, route.UseMesh, route.UseFlight,
            route.Tolerance, route.LastPointTolerance, route.Points));
    }

    internal static PlottedRoute? FindByNameOrId(IEnumerable<PlottedRoute> routes, string nameOrId)
    {
        if (Guid.TryParse(nameOrId, out Guid id))
            return routes.FirstOrDefault(route => route.Id == id);
        return routes.FirstOrDefault(route => string.Equals(route.Name, nameOrId, StringComparison.OrdinalIgnoreCase));
    }
}
