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

Console.WriteLine($"VieriNavPlotter RoutePolicy: {checks} checks passed.");
