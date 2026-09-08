using System.Numerics;

namespace VieriNavPlotter;

internal sealed record BuiltInRouteTemplate(
    string Id,
    string Name,
    string Category,
    uint TerritoryId,
    uint TargetDataId,
    string TargetLabel,
    string LevelBand,
    IReadOnlyList<RoutePoint> Points,
    bool UseMesh = true,
    bool UseFlight = true,
    string Notes = "",
    float Tolerance = 0.75f,
    float LastPointTolerance = 3f,
    RoutePoint? VendorPosition = null)
{
    internal bool IsCompletePath => Points.Count >= 2;

    internal Vector3 ResolvedVendorPosition => (VendorPosition ?? Points[^1]).Position;

    internal PlottedRoute CreateEditableCopy() => new()
    {
        Name = Name,
        TerritoryId = TerritoryId,
        Points = Points.Select(point => RoutePoint.From(point.Position)).ToList(),
        Notes = string.IsNullOrWhiteSpace(Notes)
            ? $"Copied from the built-in AutoDuty vendor catalog ({Id}). Review and test before enabling an override."
            : Notes + $" Copied from built-in template {Id}. Review and test before enabling an override.",
        Tags = $"built-in vendor gear {Category.ToLowerInvariant()} level {LevelBand}",
        UseMesh = UseMesh,
        UseFlight = UseFlight,
        Tolerance = Tolerance,
        LastPointTolerance = LastPointTolerance,
        BindingKind = RouteBindingKind.GearVendor,
        TargetDataId = TargetDataId,
        TargetLabel = TargetLabel,
        OverrideEnabled = false,
    };
}

internal static class BuiltInRouteCatalog
{
    internal static IReadOnlyList<BuiltInRouteTemplate> All { get; } =
    [
        new("arr-iron-thunder", "Iron Thunder — armor", "ARR cities", 129, 1001203, "Iron Thunder", "1–49",
        [
            Point(-155.3658f, 18.2000f, 23.3950f),
        ], true, false, "Uses the measured walkable standing point directly in front of Iron Thunder. The NPC object coordinate is retained separately for native interaction checks and is not a movement point.", LastPointTolerance: 0.75f,
            VendorPosition: Point(-156.6034f, 18.2f, 20.92f)),
        new("arr-faezghim", "Faezghim — weapons", "ARR cities", 129, 1001205, "Faezghim", "1–49",
        [
            Point(-236.5439f, 16.2000f, 40.3006f),
        ], true, false, "Uses the measured walkable standing point directly in front of Faezghim. The NPC object coordinate is retained separately for native interaction checks and is not a movement point.", LastPointTolerance: 0.75f,
            VendorPosition: Point(-236.1034f, 16f, 36.92f)),
        new("arr-sorcha", "Sorcha — accessories", "ARR cities", 129, 1001202, "Sorcha", "1–49",
        [
            Point(-135.1727f, 18.2000f, 14.8682f),
        ], true, false, "Uses the measured walkable standing point directly in front of Sorcha. The NPC object coordinate is retained separately for native interaction checks and is not a movement point.", LastPointTolerance: 0.75f,
            VendorPosition: Point(-136.1650f, 18.1734f, 12.2894f)),
        new("arr-domitien", "Domitien — Ebony Stalls hall", "ARR cities", 133, 1000215, "Domitien", "1–49",
        [
            Point(164.4264f, 15.5000f, -75.7035f),
            Point(157.5930f, 15.7000f, -69.3316f),
        ], true, false, "The route approaches Domitien directly, then settles at the measured standing point in front of him. The NPC's wall-side object coordinate is intentionally not a movement point.", LastPointTolerance: 0.75f,
            VendorPosition: Point(152.8512f, 15.5f, -71.9293f)),
        new("arr-geraint", "Geraint — weapons", "ARR cities", 133, 1000217, "Geraint", "1–49",
        [
            Point(168.4092f, 15.6999f, -73.9508f),
        ], true, false, "Uses the measured walkable standing point directly in front of Geraint's counter. The NPC's behind-counter object coordinate is retained for native interaction checks and is not a movement point.", LastPointTolerance: 0.75f,
            VendorPosition: Point(167.8366f, 15.5f, -76.9244f)),

        new("hw-seghuie", "Seghuie — accessories", "Heavensward", 419, 1011200, "Seghuie", "50–60",
        [
            Point(-189.1842f, -12.6349f, -40.0551f),
        ], true, false, "Uses the measured walkable standing point directly in front of Seghuie. Her NPC object coordinate is retained separately for native interaction checks and is not a movement point.", LastPointTolerance: 0.75f,
            VendorPosition: Point(-188.3116f, -12.5349f, -42.71f)),
        new("hw-elbert", "Elbert — weapons", "Heavensward", 419, 1011203, "Elbert", "50–60",
        [
            Point(-216.0509f, -16.1262f, -60.4229f),
        ], true, false, "Uses the measured walkable standing point directly in front of Elbert. His NPC object coordinate is retained separately for native interaction checks and is not a movement point.", LastPointTolerance: 0.75f,
            VendorPosition: Point(-214.3844f, -16.0349f, -62.4175f)),
        new("hw-norlaise", "Norlaise — armor", "Heavensward", 419, 1011204, "Norlaise", "50–60",
        [
            Point(-205.2957f, -16.1349f, -51.2569f),
        ], true, false, "Uses the measured walkable standing point directly in front of Norlaise. Her NPC object coordinate is retained separately for native interaction checks and is not a movement point.", LastPointTolerance: 0.75f,
            VendorPosition: Point(-203.5784f, -16.0349f, -53.2282f)),

        Destination("sb-1018988", "Kugane accessories vendor", "Stormblood", 628, 1018988, "Gear vendor 1018988", "62–70", 29.8923f, 4.776f, 49.2442f),
        Destination("sb-1018989", "Kugane weapons vendor", "Stormblood", 628, 1018989, "Gear vendor 1018989", "62–70", 35.1788f, 4.776f, 49.2578f),
        Destination("sb-1018990", "Kugane armor vendor", "Stormblood", 628, 1018990, "Gear vendor 1018990", "62–70", 40.0461f, 4.8365f, 49.0844f),
        Destination("sb-1019296", "Level 64 accessories vendor", "Stormblood", 614, 1019296, "Gear vendor 1019296", "64", -284.406f, 17.31996f, 490.3871f),
        Destination("sb-1019269", "Level 66 accessories vendor", "Stormblood", 614, 1019269, "Gear vendor 1019269", "66", 169.5713f, 5.16971f, -421.7089f),
        Destination("sb-1020866", "Level 68 accessories vendor", "Stormblood", 620, 1020866, "Gear vendor 1020866", "68", -247.1199f, 257.5265f, 751.4304f),

        Destination("shb-1027242", "Crystarium accessories vendor", "Shadowbringers", 819, 1027242, "Gear vendor 1027242", "72–80", -121.5391f, -1.1096f, 129.6337f),
        Destination("shb-1027243", "Crystarium gear vendor", "Shadowbringers", 819, 1027243, "Gear vendor 1027243", "72–80", -132.8298f, -1.0798f, 112.6268f),
        Destination("shb-1027991", "Crystarium gear vendor 2", "Shadowbringers", 819, 1027991, "Gear vendor 1027991", "72–80", -126.2379f, -1.0834f, 96.3301f),

        new("ew-1037049", "Old Sharlayan gear vendor — staged stairs", "Endwalker", 962, 1037049, "Gear vendor 1037049", "80",
        [
            Point(81f, 4.75f, -82f),
            Point(80f, 4.75f, -80.5f),
            Point(78.5f, 4.9f, -77.5f),
            Point(77.75f, 5.25f, -74f),
            Point(56.78f, 5.15f, -73.87f),
            Point(42.9011f, 5.15f, -77.0043f),
        ], false, false, "Reference copy of AutoDuty's staged lower-plaza, wall-corner, stair, upper-plaza, and vendor coordinates.",
            VendorPosition: Point(42.9011f, 5.15f, -77.0043f)),
        Destination("ew-1037720", "Level 82 gear vendor", "Endwalker", 958, 1037720, "Gear vendor 1037720", "82", -429.1346f, 22.4812f, 450.393f),
        Destination("ew-1037791", "Level 84 gear vendor", "Endwalker", 959, 1037791, "Gear vendor 1037791", "84", -19.8631f, -132.9519f, -461.3871f),
        Destination("ew-1037907", "Level 86 gear vendor", "Endwalker", 961, 1037907, "Gear vendor 1037907", "86", 140.5236f, 10.3859f, 164.8957f),
        Destination("ew-1038003", "Level 88 gear vendor", "Endwalker", 960, 1038003, "Gear vendor 1038003", "88", 467.0165f, 437.0017f, 327.212f),

        Destination("dt-1048377", "Level 90 gear vendor", "Dawntrail", 1185, 1048377, "Gear vendor 1048377", "90", -33.0111f, -10f, 79.7725f),
        Destination("dt-1048851", "Level 92 gear vendor", "Dawntrail", 1188, 1048851, "Gear vendor 1048851", "92", -449.6504f, 122.1928f, 274.0082f),
        Destination("dt-1048971", "Level 94 gear vendor", "Dawntrail", 1189, 1048971, "Gear vendor 1048971", "94", 626.9987f, -137.1328f, 517.8016f),
        Destination("dt-1049371", "Level 96 gear vendor", "Dawntrail", 1190, 1049371, "Gear vendor 1049371", "96", -282.598f, 18.9704f, -96.587f),
        Destination("dt-1049486", "Level 98 gear vendor", "Dawntrail", 1191, 1049486, "Gear vendor 1049486", "98", -209.3354f, 31f, 129.8653f),
    ];

    private static BuiltInRouteTemplate Destination(string id, string name, string category, uint territoryId,
        uint targetDataId, string targetLabel, string levelBand, float x, float y, float z) =>
        new(id, name, category, territoryId, targetDataId, targetLabel, levelBand, [Point(x, y, z)], true, true,
            "AutoDuty currently stores this as a vendor destination and lets travel/navmesh build the approach. Add or record approach points after copying it before enabling it as a custom override.",
            VendorPosition: Point(x, y, z));

    internal static Vector3? FindVendorPosition(uint territoryId, uint targetDataId) =>
        All.FirstOrDefault(route => route.TerritoryId == territoryId && route.TargetDataId == targetDataId)
            ?.ResolvedVendorPosition;

    private static RoutePoint Point(float x, float y, float z) => new() { X = x, Y = y, Z = z };
}
