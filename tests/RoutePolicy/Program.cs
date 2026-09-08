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
Check(domitien.Points[0].Position == new Vector3(164.4264f, 15.5000f, -75.7035f),
    "Domitien point 1 must retain the measured direct approach coordinate without an aisle overshoot.");
Check(domitien.Points[^1].Position == new Vector3(157.5930f, 15.7000f, -69.3316f),
    "Domitien's final movement point must retain the measured in-game standing coordinate.");
Check(domitien.ResolvedVendorPosition == new Vector3(152.8512f, 15.5f, -71.9293f),
    "Domitien playback must retain the real NPC coordinate separately from its safe standing point.");
Check(Vector3.Distance(domitien.Points[^1].Position, new Vector3(152.8512f, 15.5f, -71.9293f)) is > 5f and < 6f,
    "Domitien's final movement point must stand in front of the NPC instead of using his wall-side object coordinate.");
Check(domitien.LastPointTolerance == 0.75f,
    "Domitien playback must use a stable sub-yalm arrival radius instead of fighting navmesh over its final fraction of a yalm.");
Check(domitien.CreateEditableCopy().LastPointTolerance == domitien.LastPointTolerance,
    "Copying Domitien to My Routes must preserve its precise final-point tolerance.");
BuiltInRouteTemplate ironThunder = BuiltInRouteCatalog.All.Single(route => route.TargetDataId == 1001203);
Check(ironThunder.Points.Count == 1 && ironThunder.Points[0].Position == new Vector3(-155.3658f, 18.2000f, 23.3950f),
    "Iron Thunder must use the measured walkable standing point in front of his counter.");
Check(ironThunder.ResolvedVendorPosition == new Vector3(-156.6034f, 18.2f, 20.92f),
    "Iron Thunder must retain his real NPC coordinate separately for native interaction checks.");
Check(ironThunder.LastPointTolerance == 0.75f,
    "Iron Thunder playback must settle at the authored standing point without a wide early stop.");
BuiltInRouteTemplate sorcha = BuiltInRouteCatalog.All.Single(route => route.TargetDataId == 1001202);
Check(sorcha.Points.Count == 1 && sorcha.Points[0].Position == new Vector3(-135.1727f, 18.2000f, 14.8682f),
    "Sorcha must use the measured walkable standing point in front of her counter.");
Check(sorcha.ResolvedVendorPosition == new Vector3(-136.1650f, 18.1734f, 12.2894f),
    "Sorcha must retain her real NPC coordinate separately for native interaction checks.");
Check(sorcha.LastPointTolerance == 0.75f,
    "Sorcha playback must settle at the authored standing point without a wide early stop.");
BuiltInRouteTemplate geraint = BuiltInRouteCatalog.All.Single(route => route.TargetDataId == 1000217);
Check(geraint.Points.Count == 1 && geraint.Points[0].Position == new Vector3(168.4092f, 15.6999f, -73.9508f),
    "Geraint must use the measured walkable standing point in front of his counter.");
Check(geraint.ResolvedVendorPosition == new Vector3(167.8366f, 15.5f, -76.9244f),
    "Geraint must retain his real NPC coordinate separately for native interaction checks.");
Check(geraint.LastPointTolerance == 0.75f,
    "Geraint playback must settle at the authored standing point without a wide early stop.");
BuiltInRouteTemplate faezghim = BuiltInRouteCatalog.All.Single(route => route.TargetDataId == 1001205);
Check(faezghim.Points.Count == 1 && faezghim.Points[0].Position == new Vector3(-236.5439f, 16.2000f, 40.3006f),
    "Faezghim must use the measured walkable standing point in front of his counter.");
Check(faezghim.ResolvedVendorPosition == new Vector3(-236.1034f, 16f, 36.92f),
    "Faezghim must retain his real NPC coordinate separately for native interaction checks.");
Check(faezghim.LastPointTolerance == 0.75f,
    "Faezghim playback must settle at the authored standing point without a wide early stop.");
BuiltInRouteTemplate seghuie = BuiltInRouteCatalog.All.Single(route => route.TargetDataId == 1011200);
Check(seghuie.Points.Count == 1 && seghuie.Points[0].Position == new Vector3(-189.1842f, -12.6349f, -40.0551f),
    "Seghuie must use the measured walkable standing point in front of her counter.");
Check(seghuie.ResolvedVendorPosition == new Vector3(-188.3116f, -12.5349f, -42.71f),
    "Seghuie must retain her real NPC coordinate separately for native interaction checks.");
Check(seghuie.LastPointTolerance == 0.75f,
    "Seghuie playback must settle at the authored standing point without a wide early stop.");
BuiltInRouteTemplate elbert = BuiltInRouteCatalog.All.Single(route => route.TargetDataId == 1011203);
Check(elbert.Points.Count == 1 && elbert.Points[0].Position == new Vector3(-216.0509f, -16.1262f, -60.4229f),
    "Elbert must use the measured walkable standing point in front of his counter.");
Check(elbert.ResolvedVendorPosition == new Vector3(-214.3844f, -16.0349f, -62.4175f),
    "Elbert must retain his real NPC coordinate separately for native interaction checks.");
Check(elbert.LastPointTolerance == 0.75f,
    "Elbert playback must settle at the authored standing point without a wide early stop.");
BuiltInRouteTemplate norlaise = BuiltInRouteCatalog.All.Single(route => route.TargetDataId == 1011204);
Check(norlaise.Points.Count == 1 && norlaise.Points[0].Position == new Vector3(-205.2957f, -16.1349f, -51.2569f),
    "Norlaise must use the measured walkable standing point in front of her counter.");
Check(norlaise.ResolvedVendorPosition == new Vector3(-203.5784f, -16.0349f, -53.2282f),
    "Norlaise must retain her real NPC coordinate separately for native interaction checks.");
Check(norlaise.LastPointTolerance == 0.75f,
    "Norlaise playback must settle at the authored standing point without a wide early stop.");
BuiltInRouteTemplate kuganeAccessories = BuiltInRouteCatalog.All.Single(route => route.TargetDataId == 1018988);
Check(kuganeAccessories.Points.Count == 1 && kuganeAccessories.Points[0].Position == new Vector3(29.9279f, 4.0000f, 52.4925f),
    "The Kugane accessories vendor must use the measured walkable standing point.");
Check(kuganeAccessories.ResolvedVendorPosition == new Vector3(29.8923f, 4.776f, 49.2442f),
    "The Kugane accessories vendor must retain the real NPC coordinate separately.");
Check(kuganeAccessories.LastPointTolerance == 0.75f,
    "Kugane accessories playback must settle precisely at the authored standing point.");
BuiltInRouteTemplate kuganeWeapons = BuiltInRouteCatalog.All.Single(route => route.TargetDataId == 1018989);
Check(kuganeWeapons.Points.Count == 1 && kuganeWeapons.Points[0].Position == new Vector3(35.2371f, 4.0000f, 52.5185f),
    "The Kugane weapons vendor must use the measured walkable standing point.");
Check(kuganeWeapons.ResolvedVendorPosition == new Vector3(35.1788f, 4.776f, 49.2578f),
    "The Kugane weapons vendor must retain the real NPC coordinate separately.");
Check(kuganeWeapons.LastPointTolerance == 0.75f,
    "Kugane weapons playback must settle precisely at the authored standing point.");
BuiltInRouteTemplate kuganeArmor = BuiltInRouteCatalog.All.Single(route => route.TargetDataId == 1018990);
Check(kuganeArmor.Points.Count == 1 && kuganeArmor.Points[0].Position == new Vector3(40.1606f, 4.0000f, 52.5056f),
    "The Kugane armor vendor must use the measured walkable standing point.");
Check(kuganeArmor.ResolvedVendorPosition == new Vector3(40.0461f, 4.8365f, 49.0844f),
    "The Kugane armor vendor must retain the real NPC coordinate separately.");
Check(kuganeArmor.LastPointTolerance == 0.75f,
    "Kugane armor playback must settle precisely at the authored standing point.");
BuiltInRouteTemplate level64Accessories = BuiltInRouteCatalog.All.Single(route => route.TargetDataId == 1019296);
Check(level64Accessories.Points.Count == 1 && level64Accessories.Points[0].Position == new Vector3(-283.8307f, 17.3200f, 492.3687f),
    "The level-64 accessories vendor must use the measured walkable standing point.");
Check(level64Accessories.ResolvedVendorPosition == new Vector3(-284.406f, 17.31996f, 490.3871f),
    "The level-64 accessories vendor must retain the real NPC coordinate separately.");
Check(level64Accessories.LastPointTolerance == 0.75f,
    "Level-64 accessories playback must settle precisely at the authored standing point.");
BuiltInRouteTemplate level66Accessories = BuiltInRouteCatalog.All.Single(route => route.TargetDataId == 1019269);
Check(level66Accessories.Points.Count == 1 && level66Accessories.Points[0].Position == new Vector3(171.1704f, 5.1697f, -421.6375f),
    "The level-66 accessories vendor must use the measured walkable standing point.");
Check(level66Accessories.ResolvedVendorPosition == new Vector3(169.5713f, 5.16971f, -421.7089f),
    "The level-66 accessories vendor must retain the real NPC coordinate separately.");
Check(level66Accessories.LastPointTolerance == 0.75f,
    "Level-66 accessories playback must settle precisely at the authored standing point.");
BuiltInRouteTemplate level68Accessories = BuiltInRouteCatalog.All.Single(route => route.TargetDataId == 1020866);
Check(level68Accessories.Points.Count == 1 && level68Accessories.Points[0].Position == new Vector3(-249.5169f, 257.5265f, 750.1727f),
    "The level-68 accessories vendor must use the measured walkable standing point.");
Check(level68Accessories.ResolvedVendorPosition == new Vector3(-247.1199f, 257.5265f, 751.4304f),
    "The level-68 accessories vendor must retain the real NPC coordinate separately.");
Check(level68Accessories.LastPointTolerance == 0.75f,
    "Level-68 accessories playback must settle precisely at the authored standing point.");
Check(BuiltInRouteCatalog.FindVendorPosition(faezghim.TerritoryId, faezghim.TargetDataId) == faezghim.ResolvedVendorPosition,
    "Copied vendor routes must recover their trusted catalog coordinate from the binding.");
using (JsonDocument vendorPlayback = JsonDocument.Parse(SuiteTravelRequestContract.Create(
           faezghim.TerritoryId, faezghim.Points, faezghim.UseFlight, faezghim.UseMesh,
           faezghim.Tolerance, faezghim.LastPointTolerance, false,
           faezghim.TargetDataId, faezghim.ResolvedVendorPosition)))
{
    Check(vendorPlayback.RootElement.GetProperty("VendorTargetDataId").GetUInt32() == faezghim.TargetDataId,
        "Built-in vendor playback must serialize the vendor target identity.");
    Check(vendorPlayback.RootElement.GetProperty("VendorPosition").GetProperty("X").GetSingle() == faezghim.ResolvedVendorPosition.X,
        "Built-in vendor playback must serialize the trusted vendor coordinate.");
}
using (JsonDocument startTravel = JsonDocument.Parse(SuiteTravelRequestContract.Create(
           domitien.TerritoryId, domitien.Points, domitien.UseFlight, domitien.UseMesh,
           domitien.Tolerance, domitien.LastPointTolerance, true,
           domitien.TargetDataId, domitien.ResolvedVendorPosition)))
{
    Check(startTravel.RootElement.GetProperty("VendorTargetDataId").GetUInt32() == 0,
        "Travel to Start on a multi-point path must not finish early against the vendor.");
    Check(startTravel.RootElement.GetProperty("VendorPosition").ValueKind == JsonValueKind.Null,
        "Travel to Start must not carry a final vendor coordinate.");
}
Check(!NavigationVisualizationPolicy.ShouldDraw(true, false, false),
    "Unrelated vnavmesh activity must never be drawn without an explicit owner.");
Check(NavigationVisualizationPolicy.ShouldDraw(true, true, false),
    "Locally initiated NavPlotter playback must be eligible for live waypoint drawing.");
Check(NavigationVisualizationPolicy.ShouldDraw(true, false, true),
    "Explicitly authorized gear-shopping and suite route travel must be eligible for live waypoint drawing.");
Check(!NavigationVisualizationPolicy.ShouldDraw(false, true, true),
    "The user's live-waypoint setting must remain authoritative for owned navigation.");

Console.WriteLine($"VieriNavPlotter RoutePolicy: {checks} checks passed.");
