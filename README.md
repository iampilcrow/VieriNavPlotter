# VieriNavPlotter

VieriNavPlotter records and edits precise in-world routes for the Vieri plugin suite.

- Timed capture follows your movement at a configurable interval and distance.
- Manual capture adds the current position one point at a time.
- **Show Route** keeps the selected route's world-space line and numbered points visible for review; destination-only catalog entries show an honest generated-approach guide instead of pretending they contain authored segments.
- **Travel to Start** uses VieriAutoDuty's normal teleport, Aethernet, flight, navmesh, and stall-recovery flow to take you to the route.
- **Play Route** follows every saved point in order through that same travel flow, including cross-zone starts; **Stop Playback** stops both AutoDuty travel and local vnavmesh playback.
- Routes can be reversed, duplicated, played, and bound as opt-in automation overrides.
- Every route has a unique ID plus a searchable name, notes, and tags. Suite integrations can list, inspect, and run any route by name or ID; it does not need to override an existing destination.
- The built-in catalog exposes all 27 current AutoDuty gear-vendor destinations and clearly marks the two existing authored multi-point approaches. Copy any template into **My Routes** to refine it without changing the built-in reference.
- Gear-vendor bindings are consumed by VieriAutoDuty; unbound or disabled routes never alter automation.

Open it with `/vierinavplotter` or `/vnp`. Run a named route with `/vnp play "Route name"` and stop it with `/vnp stop`. Cross-zone travel uses the current VieriAutoDuty release; same-zone playback can fall back to vnavmesh. Target a vendor before using **Bind current target**.
