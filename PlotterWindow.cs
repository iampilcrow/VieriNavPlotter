using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using System.Text.Json;

namespace VieriNavPlotter;

internal sealed class PlotterWindow : Window
{
    private readonly Configuration config;
    private readonly NavmeshBridge navmesh;
    private bool recording;
    private long nextCaptureAt;
    private string status = "Create a route, then record or add points manually.";
    private Guid? pendingDelete;
    private string search = string.Empty;
    private int selectedPoint = -1;
    private bool showBuiltIns = true;
    private string selectedTemplateId = "arr-domitien";

    internal PlotterWindow(Configuration config, NavmeshBridge navmesh)
        : base("VieriNavPlotter###VieriNavPlotter", ImGuiWindowFlags.NoScrollbar)
    {
        this.config = config;
        this.navmesh = navmesh;
        Size = new Vector2(900, 600);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(720, 460), MaximumSize = new Vector2(1800, 1200) };
    }

    private PlottedRoute? Selected => config.Routes.FirstOrDefault(route => route.Id == config.SelectedRouteId);

    public override void Draw()
    {
        DrawHeader();
        if (ImGui.BeginChild("RouteLibrary", new Vector2(245, 0), true)) DrawLibrary();
        ImGui.EndChild();
        ImGui.SameLine();
        if (ImGui.BeginChild("RouteEditor", Vector2.Zero, true)) DrawEditor();
        ImGui.EndChild();
        DrawDeleteConfirmation();
    }

    private void DrawHeader()
    {
        ImGui.TextColored(new Vector4(0.95f, 0.25f, 0.25f, 1), "VIERI NAV PLOTTER");
        ImGui.SameLine();
        ImGui.TextDisabled("Record • refine • preview • override");
        ImGui.SameLine(ImGui.GetWindowWidth() - 290);
        ImGui.TextColored(recording ? ImGuiColors.HealerGreen : ImGuiColors.DalamudGrey, recording ? "● RECORDING" : "● IDLE");
        ImGui.Separator();
    }

    private void DrawLibrary()
    {
        if (ImGui.Button("+ New route", new Vector2(-1, 0))) CreateRoute();
        if (ImGui.Button("Built-in vendor paths", new Vector2(118, 0))) showBuiltIns = true;
        ImGui.SameLine();
        if (ImGui.Button("My routes", new Vector2(-1, 0))) showBuiltIns = false;
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##RouteSearch", "Search names, tags, notes...", ref search, 100);
        ImGui.Spacing();
        if (showBuiltIns)
        {
            DrawBuiltInLibrary();
            return;
        }
        foreach (PlottedRoute route in config.Routes.Where(MatchesSearch).OrderBy(route => route.Name))
        {
            bool selected = route.Id == config.SelectedRouteId;
            string label = $"{route.Name}##{route.Id}";
            if (ImGui.Selectable(label, selected)) { config.SelectedRouteId = route.Id; selectedPoint = -1; config.Save(); }
            ImGui.TextDisabled($"  Territory {route.TerritoryId} • {route.Points.Count} points");
            if (route.OverrideEnabled)
                ImGui.TextColored(ImGuiColors.HealerGreen, $"  ↳ {route.BindingKind}: {route.TargetLabel}");
        }
        if (config.Routes.Count == 0) ImGui.TextWrapped("Your personal library is empty. Use New route or copy a built-in vendor template.");
    }

    private void DrawEditor()
    {
        if (showBuiltIns)
        {
            DrawBuiltInDetails();
            return;
        }
        PlottedRoute? route = Selected;
        if (route is null) { ImGui.TextDisabled("Select or create a route."); return; }

        string name = route.Name;
        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputText("##RouteName", ref name, 100)) { route.Name = name; Changed(route); }
        ImGui.TextDisabled($"Territory {route.TerritoryId}   •   {route.Points.Count} points   •   {RouteLength(route):N1} yalms");
        string tags = route.Tags;
        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputTextWithHint("##Tags", "Tags, e.g. vendor, quest, farming, indoor", ref tags, 200)) { route.Tags = tags; Changed(route); }
        string notes = route.Notes;
        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputTextMultiline("##Notes", ref notes, 500, new Vector2(-1, 48))) { route.Notes = notes; Changed(route); }

        ImGui.Spacing();
        if (!recording)
        {
            if (ImGui.Button("Start timed recording")) StartRecording(route);
        }
        else if (ImGui.Button("Stop recording")) StopRecording();
        ImGui.SameLine();
        if (ImGui.Button("Add current location")) AddCurrent(route, false);
        ImGui.SameLine();
        if (ImGui.Button("Undo last") && route.Points.Count > 0) { route.Points.RemoveAt(route.Points.Count - 1); Changed(route); }
        ImGui.SameLine();
        if (ImGui.Button("Reverse") && route.Points.Count > 1) { route.Points.Reverse(); Changed(route); }

        float interval = config.RecordingIntervalSeconds;
        ImGui.SetNextItemWidth(140);
        if (ImGui.SliderFloat("Capture interval", ref interval, 0.2f, 5f, "%.1f sec")) { config.RecordingIntervalSeconds = interval; config.Save(); }
        float distance = config.MinimumPointDistance;
        ImGui.SetNextItemWidth(140);
        if (ImGui.SliderFloat("Minimum spacing", ref distance, 0.1f, 10f, "%.1f y")) { config.MinimumPointDistance = distance; config.Save(); }

        bool preview = config.ShowWorldPreview;
        if (ImGui.Checkbox("Show connected route in the world", ref preview)) { config.ShowWorldPreview = preview; config.Save(); }
        ImGui.SameLine();
        bool numbers = config.ShowPointNumbers;
        if (ImGui.Checkbox("Point numbers", ref numbers)) { config.ShowPointNumbers = numbers; config.Save(); }

        Section("Playback");
        bool mesh = route.UseMesh;
        if (ImGui.Checkbox("Mesh-assisted between points", ref mesh)) { route.UseMesh = mesh; Changed(route); }
        ImGui.SameLine();
        bool flight = route.UseFlight;
        if (ImGui.Checkbox("Allow flight", ref flight)) { route.UseFlight = flight; Changed(route); }
        ImGui.TextDisabled("For exact halls and vendor approaches, leave mesh-assisted off. Every saved point is then followed in order.");
        if (ImGui.Button("Test route"))
        {
            if (Plugin.ClientState.TerritoryType != route.TerritoryId) status = "Travel to the route's territory before testing it.";
            else if (Plugin.Objects.LocalPlayer is { } player) navmesh.TryPlay(route, player.Position, out status);
        }
        ImGui.SameLine();
        if (ImGui.Button("Stop playback")) { navmesh.Stop(); status = "Playback stopped."; }

        Section("Optional automation assignment");
        ImGui.TextWrapped("Named routes are always available to VieriCodex, VieriNexus, and future Vieri modules through the shared route library. An assignment additionally replaces one exact built-in destination.");
        int binding = (int)route.BindingKind;
        ImGui.SetNextItemWidth(220);
        if (ImGui.Combo("Use this route for", ref binding, "No override\0Gear vendor\0")) { route.BindingKind = (RouteBindingKind)binding; Changed(route); }
        if (route.BindingKind != RouteBindingKind.None)
        {
            if (ImGui.Button("Bind current target")) BindCurrentTarget(route);
            ImGui.SameLine();
            ImGui.TextUnformatted(route.TargetDataId == 0 ? "No target bound" : $"{route.TargetLabel} ({route.TargetDataId})");
            bool enabled = route.OverrideEnabled;
            if (ImGui.Checkbox("Enable this override", ref enabled))
            {
                if (enabled && (!RoutePolicy.IsValid(route) || route.TargetDataId == 0)) status = "Add at least two valid points and bind a target first.";
                else { route.OverrideEnabled = enabled; DisableDuplicateBindings(route); Changed(route); }
            }
        }

        Section("Points");
        if (ImGui.BeginChild("PointList", new Vector2(0, 130), true))
        {
            for (int i = 0; i < route.Points.Count; i++)
            {
                RoutePoint point = route.Points[i];
                if (ImGui.Selectable($"{i + 1,3}.  X {point.X,8:F2}   Y {point.Y,8:F2}   Z {point.Z,8:F2}##point{i}", selectedPoint == i))
                    selectedPoint = i;
            }
        }
        ImGui.EndChild();
        if (selectedPoint >= 0 && selectedPoint < route.Points.Count)
        {
            if (ImGui.Button("Replace selected with current") && Plugin.Objects.LocalPlayer is { } player)
            {
                route.Points[selectedPoint] = RoutePoint.From(player.Position); Changed(route);
            }
            ImGui.SameLine();
            if (ImGui.Button("Move up") && selectedPoint > 0)
            {
                (route.Points[selectedPoint - 1], route.Points[selectedPoint]) = (route.Points[selectedPoint], route.Points[selectedPoint - 1]); selectedPoint--; Changed(route);
            }
            ImGui.SameLine();
            if (ImGui.Button("Move down") && selectedPoint < route.Points.Count - 1)
            {
                (route.Points[selectedPoint + 1], route.Points[selectedPoint]) = (route.Points[selectedPoint], route.Points[selectedPoint + 1]); selectedPoint++; Changed(route);
            }
            ImGui.SameLine();
            if (ImGui.Button("Remove point")) { route.Points.RemoveAt(selectedPoint); selectedPoint = Math.Min(selectedPoint, route.Points.Count - 1); Changed(route); }
        }
        ImGui.TextWrapped(status);
        if (ImGui.Button("Duplicate route")) Duplicate(route);
        ImGui.SameLine();
        if (ImGui.Button("Copy route")) { ImGui.SetClipboardText(JsonSerializer.Serialize(route, new JsonSerializerOptions { WriteIndented = true })); status = "Route copied to the clipboard."; }
        ImGui.SameLine();
        if (ImGui.Button("Import clipboard")) ImportClipboard();
        ImGui.SameLine();
        if (ImGui.Button("Clear points")) { route.Points.Clear(); route.OverrideEnabled = false; Changed(route); }
        ImGui.SameLine();
        if (ImGui.Button("Delete route")) { pendingDelete = route.Id; ImGui.OpenPopup("Delete route?"); }
    }

    internal void UpdateRecording()
    {
        if (!recording || Environment.TickCount64 < nextCaptureAt || Selected is not { } route) return;
        if (Plugin.ClientState.TerritoryType != route.TerritoryId) { StopRecording(); status = "Recording stopped because the territory changed."; return; }
        AddCurrent(route, true);
        nextCaptureAt = Environment.TickCount64 + (long)(config.RecordingIntervalSeconds * 1000);
    }

    internal void DrawWorldPreview()
    {
        if (!config.ShowWorldPreview) return;
        IReadOnlyList<RoutePoint> points;
        uint territoryId;
        if (showBuiltIns)
        {
            BuiltInRouteTemplate? template = BuiltInRouteCatalog.All.FirstOrDefault(item => item.Id == selectedTemplateId);
            if (template is null) return;
            points = template.Points;
            territoryId = template.TerritoryId;
        }
        else
        {
            if (Selected is not { } route) return;
            points = route.Points;
            territoryId = route.TerritoryId;
        }
        if (territoryId != Plugin.ClientState.TerritoryType) return;
        var draw = ImGui.GetForegroundDrawList();
        uint lineColor = ImGui.GetColorU32(new Vector4(0.95f, 0.18f, 0.18f, 0.9f));
        uint pointColor = ImGui.GetColorU32(new Vector4(1f, 0.75f, 0.15f, 1f));
        Vector2? previous = null;
        for (int i = 0; i < points.Count; i++)
        {
            if (!Plugin.GameGui.WorldToScreen(points[i].Position, out Vector2 screen)) { previous = null; continue; }
            if (previous is { } prior) draw.AddLine(prior, screen, lineColor, 3f);
            draw.AddCircleFilled(screen, i == 0 ? 7f : 5f, pointColor);
            if (config.ShowPointNumbers) draw.AddText(screen + new Vector2(7, -8), pointColor, (i + 1).ToString());
            previous = screen;
        }
    }

    internal void Suspend() { if (recording) StopRecording(); }
    internal void StopAll() { StopRecording(); navmesh.Stop(); config.Save(); }
    internal void StopPlayback() { navmesh.Stop(); status = "Playback stopped."; }
    internal void PlayNamedRoute(string nameOrId)
    {
        PlottedRoute? route = RoutePolicy.FindByNameOrId(config.Routes, nameOrId);
        if (route is null) { Plugin.Chat.PrintError($"[VieriNavPlotter] Route '{nameOrId}' was not found."); return; }
        config.SelectedRouteId = route.Id;
        if (Plugin.ClientState.TerritoryType != route.TerritoryId) status = $"Route '{route.Name}' belongs to territory {route.TerritoryId}.";
        else if (Plugin.Objects.LocalPlayer is { } player) navmesh.TryPlay(route, player.Position, out status);
        Plugin.Chat.Print($"[VieriNavPlotter] {status}");
    }

    private void CreateRoute()
    {
        var route = new PlottedRoute { Name = $"Route {config.Routes.Count + 1}", TerritoryId = Plugin.ClientState.TerritoryType };
        config.Routes.Add(route); config.SelectedRouteId = route.Id; selectedPoint = -1; showBuiltIns = false; config.Save(); status = "Route created. Add the first point or start recording.";
    }

    private void StartRecording(PlottedRoute route)
    {
        if (Plugin.ClientState.TerritoryType != route.TerritoryId) { status = "This route belongs to another territory."; return; }
        recording = true; AddCurrent(route, false); nextCaptureAt = Environment.TickCount64 + (long)(config.RecordingIntervalSeconds * 1000); status = "Recording your movement.";
    }

    private void StopRecording() { recording = false; status = "Recording stopped."; config.Save(); }

    private void AddCurrent(PlottedRoute route, bool respectSpacing)
    {
        if (Plugin.Objects.LocalPlayer is not { } player || Plugin.ClientState.TerritoryType != route.TerritoryId) return;
        if (respectSpacing && route.Points.LastOrDefault() is { } last && Vector3.Distance(last.Position, player.Position) < config.MinimumPointDistance) return;
        route.Points.Add(RoutePoint.From(player.Position)); Changed(route);
    }

    private void BindCurrentTarget(PlottedRoute route)
    {
        if (Plugin.Targets.Target is not { } target) { status = "Target the vendor or object first."; return; }
        route.TargetDataId = target.BaseId; route.TargetLabel = target.Name.ToString(); route.TerritoryId = Plugin.ClientState.TerritoryType; Changed(route);
        status = $"Bound to {route.TargetLabel}.";
    }

    private void DisableDuplicateBindings(PlottedRoute route)
    {
        if (!route.OverrideEnabled) return;
        foreach (PlottedRoute other in config.Routes.Where(other => other.Id != route.Id && other.BindingKind == route.BindingKind &&
                     other.TerritoryId == route.TerritoryId && other.TargetDataId == route.TargetDataId)) other.OverrideEnabled = false;
    }

    private void Duplicate(PlottedRoute source)
    {
        var copy = new PlottedRoute { Name = source.Name + " copy", TerritoryId = source.TerritoryId, Points = source.Points.Select(p => RoutePoint.From(p.Position)).ToList(), UseMesh = source.UseMesh, UseFlight = source.UseFlight, Tolerance = source.Tolerance, LastPointTolerance = source.LastPointTolerance };
        config.Routes.Add(copy); config.SelectedRouteId = copy.Id; config.Save();
    }

    private void DrawBuiltInLibrary()
    {
        string? category = null;
        foreach (BuiltInRouteTemplate template in BuiltInRouteCatalog.All.Where(MatchesSearch))
        {
            if (category != template.Category)
            {
                category = template.Category;
                ImGui.TextColored(new Vector4(0.95f, 0.65f, 0.2f, 1), category);
            }
            if (ImGui.Selectable($"{template.Name}##template-{template.Id}", selectedTemplateId == template.Id))
                selectedTemplateId = template.Id;
            ImGui.TextDisabled(template.IsCompletePath
                ? $"  {template.Points.Count} points • Lv. {template.LevelBand}"
                : $"  destination • Lv. {template.LevelBand}");
        }
    }

    private void DrawBuiltInDetails()
    {
        BuiltInRouteTemplate template = BuiltInRouteCatalog.All.FirstOrDefault(item => item.Id == selectedTemplateId)
                                           ?? BuiltInRouteCatalog.All[0];
        ImGui.TextColored(new Vector4(1f, 0.86f, 0.86f, 1), template.Name);
        ImGui.TextDisabled($"Built-in AutoDuty catalog • {template.Category} • Level {template.LevelBand}");
        ImGui.Spacing();
        ImGui.TextUnformatted($"Territory: {template.TerritoryId}");
        ImGui.TextUnformatted($"Vendor: {template.TargetLabel} ({template.TargetDataId})");
        ImGui.TextUnformatted(template.IsCompletePath ? $"Authored path: {template.Points.Count} points" : "Stored behavior: destination plus generated navmesh approach");
        ImGui.TextWrapped(template.Notes);
        ImGui.Spacing();
        if (!template.IsCompletePath)
            ImGui.TextColored(new Vector4(0.95f, 0.72f, 0.2f, 1), "This is a destination template, not a proven multi-point path. Copy it, then record or add the safe approach you want.");
        else
            ImGui.TextColored(ImGuiColors.HealerGreen, "This entry contains AutoDuty's current authored multi-point coordinates.");

        if (ImGui.Button("Copy to My Routes", new Vector2(190, 34)))
        {
            PlottedRoute route = template.CreateEditableCopy();
            config.Routes.Add(route); config.SelectedRouteId = route.Id; showBuiltIns = false; selectedPoint = -1; config.Save();
            status = template.IsCompletePath ? "Built-in path copied. Test it before enabling the override." : "Destination copied. Add the safe approach points before playback or override use.";
        }

        Section("Stored points");
        for (int i = 0; i < template.Points.Count; i++)
        {
            RoutePoint point = template.Points[i];
            ImGui.TextUnformatted($"{i + 1,3}.  X {point.X,8:F2}   Y {point.Y,8:F2}   Z {point.Z,8:F2}");
        }
    }

    private void ImportClipboard()
    {
        try
        {
            PlottedRoute? route = JsonSerializer.Deserialize<PlottedRoute>(ImGui.GetClipboardText());
            if (route is null || !RoutePolicy.IsValid(route)) { status = "Clipboard does not contain a valid route with at least two points."; return; }
            route.Id = Guid.NewGuid(); route.Name = string.IsNullOrWhiteSpace(route.Name) ? "Imported route" : route.Name + " (imported)";
            route.OverrideEnabled = false; route.UpdatedAtUtc = DateTime.UtcNow;
            config.Routes.Add(route); config.SelectedRouteId = route.Id; selectedPoint = -1; config.Save(); status = "Route imported. Its automation override is disabled until you review it.";
        }
        catch (JsonException) { status = "Clipboard route JSON could not be read."; }
    }

    private void DrawDeleteConfirmation()
    {
        if (!ImGui.BeginPopupModal("Delete route?", ImGuiWindowFlags.AlwaysAutoResize)) return;
        ImGui.TextUnformatted("Delete this route permanently?");
        if (ImGui.Button("Delete") && pendingDelete is { } id)
        {
            config.Routes.RemoveAll(route => route.Id == id); config.SelectedRouteId = config.Routes.FirstOrDefault()?.Id; config.Save(); pendingDelete = null; recording = false; ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel")) { pendingDelete = null; ImGui.CloseCurrentPopup(); }
        ImGui.EndPopup();
    }

    private void Changed(PlottedRoute route) { route.UpdatedAtUtc = DateTime.UtcNow; config.Save(); }
    private static float RouteLength(PlottedRoute route) => route.Points.Zip(route.Points.Skip(1), (a, b) => Vector3.Distance(a.Position, b.Position)).Sum();
    private bool MatchesSearch(PlottedRoute route) => string.IsNullOrWhiteSpace(search) ||
        route.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || route.Tags.Contains(search, StringComparison.OrdinalIgnoreCase) ||
        route.Notes.Contains(search, StringComparison.OrdinalIgnoreCase);
    private bool MatchesSearch(BuiltInRouteTemplate route) => string.IsNullOrWhiteSpace(search) ||
        route.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || route.Category.Contains(search, StringComparison.OrdinalIgnoreCase) ||
        route.TargetLabel.Contains(search, StringComparison.OrdinalIgnoreCase) || route.LevelBand.Contains(search, StringComparison.OrdinalIgnoreCase) ||
        route.TargetDataId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase);
    private static void Section(string title) { ImGui.Spacing(); ImGui.Separator(); ImGui.TextColored(new Vector4(0.95f, 0.65f, 0.2f, 1), title); }
}
