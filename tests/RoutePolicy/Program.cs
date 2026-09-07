using System.Numerics;
using System.Text.Json;
using VieriNavPlotter;

int checks = 0;
void Check(bool condition, string message)
{
    checks++;
    if (!condition) throw new InvalidOperationException(message);
}

var valid = new PlottedRoute
{
    Name = "Hall to vendor",
    TerritoryId = 129,
    BindingKind = RouteBindingKind.GearVendor,
    TargetDataId = 1000215,
    TargetLabel = "Domitien",
    OverrideEnabled = true,
    Tags = "vendor indoor",
    Points = [RoutePoint.From(new Vector3(1, 2, 3)), RoutePoint.From(new Vector3(4, 5, 6))],
};
var older = new PlottedRoute
{
    Name = "Old route", TerritoryId = 129, BindingKind = RouteBindingKind.GearVendor,
    TargetDataId = 1000215, OverrideEnabled = true, UpdatedAtUtc = DateTime.UtcNow.AddDays(-1),
    Points = [RoutePoint.From(Vector3.Zero), RoutePoint.From(Vector3.One)],
};

Check(RoutePolicy.IsValid(valid), "A two-point route must be valid.");
Check(RoutePolicy.Resolve([older, valid], RouteBindingKind.GearVendor, 129, 1000215) == valid, "Newest exact binding must win.");
Check(RoutePolicy.Resolve([valid], RouteBindingKind.GearVendor, 130, 1000215) is null, "Territory mismatch must not resolve.");
Check(RoutePolicy.Resolve([valid], RouteBindingKind.GearVendor, 129, 7) is null, "Target mismatch must not resolve.");
valid.OverrideEnabled = false;
Check(RoutePolicy.Resolve([valid], RouteBindingKind.GearVendor, 129, 1000215) is null, "Disabled route must not override.");
valid.OverrideEnabled = true;
string json = RoutePolicy.SerializeOverride([valid], "GearVendor", 129, 1000215)!;
using JsonDocument document = JsonDocument.Parse(json);
Check(document.RootElement.GetProperty("SchemaVersion").GetInt32() == 1, "Contract schema must be versioned.");
Check(document.RootElement.GetProperty("Points").GetArrayLength() == 2, "All route points must be serialized.");
Check(RoutePolicy.FindByNameOrId([valid], "hall TO VENDOR") == valid, "Named lookup must be case-insensitive.");
Check(RoutePolicy.FindByNameOrId([valid], valid.Id.ToString()) == valid, "ID lookup must work.");
valid.Points[0].X = float.NaN;
Check(!RoutePolicy.IsValid(valid), "Non-finite coordinates must be rejected.");
Check(BuiltInRouteCatalog.All.Count == 27, "All 27 distinct AutoDuty gear vendors must be represented.");
Check(BuiltInRouteCatalog.All.Select(route => (route.TerritoryId, route.TargetDataId)).Distinct().Count() == 27,
    "Built-in vendor bindings must be unique.");
Check(BuiltInRouteCatalog.All.Count(route => route.IsCompletePath) == 2,
    "Only the two existing authored multi-point vendor approaches may be presented as complete paths.");
Check(BuiltInRouteCatalog.All.Where(route => route.IsCompletePath).All(route => RoutePolicy.IsValid(route.CreateEditableCopy())),
    "Every complete built-in template must produce a valid editable route.");
Check(BuiltInRouteCatalog.All.Where(route => !route.IsCompletePath).All(route => !RoutePolicy.IsValid(route.CreateEditableCopy())),
    "Destination-only templates must not silently become enabled route overrides.");
BuiltInRouteTemplate domitien = BuiltInRouteCatalog.All.Single(route => route.TargetDataId == 1000215);
Check(domitien.Points.Count == 2, "Domitien must use the clear center-aisle and safe interaction points only.");
Check(domitien.Points[^1].Position == new Vector3(157.5930f, 15.7000f, -69.3316f),
    "Domitien's final movement point must retain the measured in-game standing coordinate.");
Check(Vector3.Distance(domitien.Points[^1].Position, new Vector3(152.8512f, 15.5f, -71.9293f)) is > 5f and < 6f,
    "Domitien's final movement point must stand in front of the NPC instead of using his wall-side object coordinate.");
Check(domitien.LastPointTolerance is >= 0.1f and <= 0.5f,
    "Domitien playback must reach the measured standing point instead of completing several yalms early.");
Check(domitien.CreateEditableCopy().LastPointTolerance == domitien.LastPointTolerance,
    "Copying Domitien to My Routes must preserve its precise final-point tolerance.");

Console.WriteLine($"VieriNavPlotter RoutePolicy: {checks} checks passed.");
