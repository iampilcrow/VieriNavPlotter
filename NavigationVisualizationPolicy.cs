namespace VieriNavPlotter;

internal static class NavigationVisualizationPolicy
{
    internal static bool ShouldDraw(bool enabled, bool navPlotterOwnsNavigation, bool suiteAuthorizesNavigation) =>
        enabled && (navPlotterOwnsNavigation || suiteAuthorizesNavigation);
}
