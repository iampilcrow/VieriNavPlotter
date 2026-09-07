# VieriNavPlotter

VieriNavPlotter records and edits precise in-world routes for the Vieri plugin suite.

- Timed capture follows your movement at a configurable interval and distance.
- Manual capture adds the current position one point at a time.
- Live world-space lines and numbered points make the route visible while editing.
- Routes can be reversed, duplicated, tested with vnavmesh, and bound as opt-in automation overrides.
- Every route has a unique ID plus a searchable name, notes, and tags. Suite integrations can list, inspect, and run any route by name or ID; it does not need to override an existing destination.
- The built-in catalog exposes all 27 current AutoDuty gear-vendor destinations and clearly marks the two existing authored multi-point approaches. Copy any template into **My Routes** to refine it without changing the built-in reference.
- Gear-vendor bindings are consumed by VieriAutoDuty; unbound or disabled routes never alter automation.

Open it with `/vierinavplotter` or `/vnp`. Run a named route with `/vnp play "Route name"` and stop it with `/vnp stop`. Target a vendor before using **Bind current target**.
