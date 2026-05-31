# AGENTS.md

## Project
BattleShip is a Unity 6 naval combat prototype. The current gameplay direction is closer to World of Warships: large ships, engine telegraph, rudder control, turrets, shells, UI instruments.

Prefer gameplay-tunable kinematic systems over full Rigidbody naval physics unless explicitly asked otherwise.

## Where To Look First
- `Core/` - main ship movement, propulsion, rudder, maneuvering, visual heel, wave motion, config, combat profiles.
- `UI/` - Unity UI Toolkit screens and controls.
- `UI/speedo/` - engine telegraph and speedometer UI experiments.
- `UI/Rudder/` - rudder slider UI experiments.
- `Scenes/` - Unity scenes and scene-specific prototypes.
- `Docs/tracker/` - project tracker notes, with task files in `tasks/` and `todo_task_list.md` as the summary registry.
- `BattleShip_ShipMovement_Context.md` - long-form design context for ship movement decisions.

## Main Systems
- `ShipConfig` - ScriptableObject with ship tuning values.
- `ShipMovementController` - applies ship movement.
- `ShipPropulsionModel` - engine order, speed, acceleration, coasting, reverse thrust behavior.
- `ShipManeuveringModel` - turn radius / rudder maneuvering calculations.
- `RudderInputSystem` and `RudderInputController` - rudder input logic.
- `ShipHeelVisualController` - visual heel during turning.
- `ShipWaveMotionController` - wave motion separated from maneuvering.
- `ShipPropellerVisualController` and `ShipRudderVisualController` - visual-only rotating parts.

## Combat / Ballistics
- `Core/Scripts/Combat/Dispersion/` - dispersion profiles.
- `Core/Scripts/Combat/Projectile/` - shell ballistics profiles.
- Root turret scripts like `TurretController`, `TurretData`, `TurretWithCannons`, `Cannon`, `NavalGunFire` are current combat/turret entry points.

## Third-Party / Example Content
- `Plugins/` contains external packages, including Crest. Do not refactor plugin code unless the task explicitly requires it.
- `Example/`, `TutorialInfo/`, `TextMesh Pro/`, and imported asset folders are mostly reference/demo/vendor content.
- Avoid broad formatting or renaming in imported assets.

## Coding Rules
- Keep Unity `.meta` files with their assets.
- Do not manually create new Unity `.meta` files unless explicitly requested.
- Before any file modification, explicitly ask the user for permission.
- Always write plans in Russian.
- Prefer small, focused changes.
- When adding a new task file to `Docs/tracker/tasks/`, also update `Docs/tracker/todo_task_list.md`.
- Preserve existing serialized field names when possible to avoid breaking scene/prefab references.
- Use `[SerializeField] private` for inspector-configured fields unless the existing file uses another pattern.
- Do not introduce Rigidbody-based ship physics unless the task explicitly changes the architecture.
- If changing movement behavior, check `BattleShip_ShipMovement_Context.md` first.

## Validation
After code changes:
- Let Unity compile if possible.
- Check affected scene/prefab references when serialized fields are renamed.
- For movement changes, test engine telegraph, stop/coasting, reverse thrust, rudder turning, visual heel, propellers, and rudder visuals.
- For UI changes, test the relevant UXML/USS/controller together.
