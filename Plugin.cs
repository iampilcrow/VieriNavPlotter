using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace VieriNavPlotter;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface Pi { get; private set; } = null!;
    [PluginService] internal static ICommandManager Commands { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IObjectTable Objects { get; private set; } = null!;
    [PluginService] internal static ITargetManager Targets { get; private set; } = null!;
    [PluginService] internal static ICondition Condition { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IChatGui Chat { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private readonly WindowSystem windows = new("VieriNavPlotter");
    private readonly PlotterWindow window;
    private readonly RouteIpcProvider ipc;
    private readonly GameplayReadyGate readyGate = new();
    private bool ready;

    public Plugin()
    {
        var config = Pi.GetPluginConfig() as Configuration ?? new Configuration();
        config.Initialize(Pi);
        var navmesh = new NavmeshBridge(Pi, Log);
        var suiteTravel = new SuiteTravelBridge(Pi, Log);
        window = new PlotterWindow(config, navmesh, suiteTravel);
        windows.AddWindow(window);
        ipc = new RouteIpcProvider(Pi, config, navmesh);
        Commands.AddHandler("/vierinavplotter", new CommandInfo(OnCommand) { HelpMessage = "Open VieriNavPlotter. Alias: /vnp" });
        Commands.AddHandler("/vnp", new CommandInfo(OnCommand) { HelpMessage = "Open VieriNavPlotter." });
        Pi.UiBuilder.Draw += Draw;
        Pi.UiBuilder.OpenMainUi += Open;
        Pi.UiBuilder.OpenConfigUi += Open;
    }

    public void Dispose()
    {
        Pi.UiBuilder.Draw -= Draw;
        Pi.UiBuilder.OpenMainUi -= Open;
        Pi.UiBuilder.OpenConfigUi -= Open;
        Commands.RemoveHandler("/vierinavplotter");
        Commands.RemoveHandler("/vnp");
        window.StopAll();
        ipc.Dispose();
        windows.RemoveAllWindows();
    }

    private void OnCommand(string _, string arguments)
    {
        string trimmed = arguments.Trim();
        if (trimmed.StartsWith("play ", StringComparison.OrdinalIgnoreCase))
            window.PlayNamedRoute(trimmed[5..].Trim().Trim('"'));
        else if (trimmed.Equals("stop", StringComparison.OrdinalIgnoreCase))
            window.StopPlayback();
        else
            Open();
    }
    private void Open() => window.IsOpen = true;

    private void Draw()
    {
        ready = readyGate.Evaluate(ClientState.IsLoggedIn, Objects.LocalPlayer is { IsTargetable: true },
            ClientState.TerritoryType != 0,
            Condition[ConditionFlag.BetweenAreas] || Condition[ConditionFlag.BetweenAreas51], Environment.TickCount64);
        if (!ready) { window.Suspend(); return; }
        window.UpdateRecording();
        windows.Draw();
        window.DrawWorldPreview();
    }
}
