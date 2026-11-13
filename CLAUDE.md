# Harvest

Unity 6 (6000.4.6f1), URP, single `Assembly-CSharp` (no asmdefs, no namespaces). Realistic physics-sim ARPG where harvesting replaces combat. Physical grid-based tree chopping, draggable logs, itemised gear with parts and affixes are built.

## Read Notion first

Before doing anything, read the project tracker. It and the code are the source of truth.

- Tracker database: https://app.notion.com/p/2185e74d732c80679c54d763c6fde368 (data source `collection://2185e74d-732c-803e-b1af-000b0a701c67`)
- Design doc: "Harvest GDD" https://app.notion.com/p/2175e74d732c80838be4edf668bb50ce
- Query the tracker for rows in Slice `0 Foundation` that are not Done and every row in `1 Felling is fun`, and read their `Blocked by` relations. Each row's page holds its design spec and decisions.

## Workflow

- Do not commit and do not edit Notion unless asked. Never change a row's Status unless asked.
- Keep Notion row page content short: a few lines or bullets covering only what is vague, complex or a non-obvious decision. It is rarely read, so no tuning values, no restating the code, no long behaviour write-ups. Leave a page blank if the description says it all.
- Do not add systems beyond the row being worked on. Prefer procedural assets over new files.
- Tell the user before editing scenes or prefabs.
- After finishing work, say how to verify it in Play Mode.
- Before tuning feel or balance, confirm how the player will actually use it and test that pattern (for example moving round a tree, not hitting one spot).
- When a number or dimension could be read two ways (half or full width, which axis), use the literal or full reading, say which in one line, and ask if it matters.
- Apply feedback literally, one change at a time, and report the concrete numbers. Do not bundle redesigns with a small request.
- Say what was and was not verified. If one honest attempt at input injection or a screenshot fails, call it untested and stop.
- Use the vocabulary agreed with the user in code names and Notion, and rename once terms settle. After a rename or a removed setting, grep for the old names, then compile and read the console.

## Code style

- No namespaces. One main type per file; small helper types can live in the same file.
- Allman braces, 4 spaces. Single-line `if (x) return;` guards are fine.
- Comments are only for explaining non-obvious blocks inside larger functions: a short fragment above the block, no trailing period. No XML docs and no commented-out code.
- Private fields are camelCase with no underscore. Constants are `private static readonly float UPPER_SNAKE`, not `const`, placed at the very bottom of the class.
- Serialized config uses `[SerializeField] private` under `[Header("...")]` blocks.
- Events: `public event Action<T> OnX = delegate { };`, invoked with `?.Invoke(...)`. Handlers are named `OnPlayerItemAdded` style.
- Long descriptive names: `playerChopFromHit`, `swayShakeNoiseTime`, `posX` (never `px`). Animation transforms/points/mesh points are prefixed `at`/`ap`/`mp`.
- Member order: public properties, public methods, serialized fields, private fields, private methods and Unity messages, nested private types, then static constants.
- Language features in use: target-typed `new()`, expression-bodied properties, `?.`, `??=`, switch expressions, tuples, `Debug.Assert` for invariants, `var`.
- Asset naming: `Item_<Type>_<Sub>_<Name>`, `Affix_`, `AffixPool_`; mesh prefabs `X Mesh.prefab`. Items and affixes are looked up by `ID`; prefabs and materials by asset name.

## Architecture

- `PlayerPersistent` (DontDestroyOnLoad) owns the `GridInventory` and `GearInventory`, and respawns the Player prefab at the `Spawnpoint` tag on every scene load. It also draws the debug scene buttons in `OnGUI`.
- `Player` is the composition root. Subsystems are plain `[Serializable]` classes with `Init(Player)`: `PlayerInput`, `PlayerCamera`, `PlayerMovement`, `PlayerInteractor`, `PlayerGear`, `PlayerActionHandler`, `PlayerAnimator`, `PlayerBuffs`. `Player.Update/FixedUpdate/LateUpdate` call them in an explicit order.
- Actions: `PlayerAction` subclasses (`PlayerAxeChopAction`, `PlayerDragAction`, `PlayerPickupItemAction`) are `async Task Start(ct)` with cancel conditions. The handler runs the first runnable action when the Primary button is pressed. An action can take over the movement keys (`OverridesMovementInput`) and choose its own cancel conditions (for example `CancelOnBackwardInput`).
- Tools: equipping a tool item creates a `PlayerTool` (only `Axe` exists) which registers its action. Gear body visuals attach to `MP - Gear - {type}` mesh points.
- Items: `ItemData`, `AffixData`, `AffixPool` are ScriptableObjects. `ItemInstance`, `AffixInstance` and `PartSlotInstance` are runtime classes. `PartSlotInstance.item` is `[SerializeReference]` so null stays null. Parts nest recursively and recursive serialization is deliberate.
- `AssetDatabase` is a scene MonoBehaviour that holds static dictionaries. In the Editor its `OnValidate` fills the item and affix lists automatically from the project.
- Stats: `Stat` enum, `StatResolver` accumulates from player base stats, equipped items (recursing into parts) and buffs. `PlayerStatProvider` caches and is dirtied by gear, part and buff changes only. `BitWidth` is the axe head's full width in centimetres. In chopping, sharpness sets how well the edge bites and strength sets depth and wood damage.
- Trees: `ChoppableTree` plus `TreeGrid` (columns are angle, rows are height, one `Radius` per cell is the truth, `BaseRadius` is the uncut trunk, `Damage` is per cell). `TreeHitMath` holds the hit formulas: sharpness against depth-derived hardness gives bite quality, strength gives dent depth. The mesh is rebuilt from the grid on every hit. A row splits when the best arc of a third of the trunk's angle is cut about 80% of the way to the core, and the top half becomes a draggable rigidbody log (`CanChop = false`). Visuals read the surface from the grid (`TrySampleSurface`), not the collider, which lags behind cuts. The `Choppable` tag is on Hub trees and kept on purpose.
- UI is built in code and prefabs: `PlayerUI` (canvas root, static `Instance`), `GridInventoryUI`, `GearInventoryUI`, `ItemUI`, `ItemInspectorUI`, `TextPopupManager`.
- Singletons and statics in use: `PlayerUI.Instance`, `ParticleManager.Instance`, `TextPopupManager` static facade, `PlayerPersistent.instance`, `PlayerInput.PointerPosition`.
- Scenes: `HubScene` (main test scene), `AreaScene`, `TreeChoppingTest` (debug, not in build settings). Build settings contain Hub and Area.

## Input

- Input System package only. `Assets/Harvest/Settings/InputActions.inputactions` has a single `Player` map: Move, Look, Point, Primary, Zoom. Add actions as features need them (a Secondary action is planned for the cut direction toggle).
- `PlayerInput` reads the actions and exposes `InputMovement`, `InputMovementAxes` (raw A/D and W/S), `InputScroll` (-1, 0 or 1), `MouseDelta` (scaled by 0.1), `IsMousePressed/Released`, `RaycastHits`. UI scripts without a player reference use the static `PlayerInput.PointerPosition`.
- The asset is referenced on the Player prefab's `PlayerInput`. The UI uses the Input System UI module with the package default actions.

## Gotchas

- Scenes and prefabs are text YAML and can be edited, but the editor may rewrite them. Check `git diff` afterwards.
- Unity MCP is connected. Check the console after every change and compile. The editor only advances frames reliably when the Game view is focused.
- Leave the editor and repo as found: delete temporary files made in the project, restore any changed editor or Input System settings, and run `git status` after Play Mode runs for stray scene or asset changes.
- Bound anything that can block the editor. Thousand-iteration loops through the full mesh rebuild path froze Unity for minutes; test the pure logic (for example `TreeGrid` and `TreeHitMath`) instead.
- Positioned camera screenshots (`manage_camera` with a view position) left the Scene view broken. Avoid them unless needed, and close and reopen the Scene tab if its errors appear.
- `execute_code` compiles as C# 6 (no local functions or tuples). Write multi-line edit scripts to a file instead of long shell heredocs.

## Testing input through Unity MCP

Real mouse and keyboard cannot be driven from outside. In the Editor, input can be injected by queueing state events on `Mouse.current` and `Keyboard.current`, but:

- The Editor ignores input when unfocused, so use manual update mode, ignore focus, and send all device input to the game view.
- A small pump must be added to the player loop to queue the events and call `InputSystem.Update()` every frame.
- Reading device state from `execute_code` returns the editor-update buffer, not the game's. Read game state (for example `Player.Input.RaycastHits`) instead.
- These settings and the player loop persist after Play Mode stops and will break the user's real input. Always reset them when done: `PlayerLoop.SetPlayerLoop(PlayerLoop.GetDefaultPlayerLoop())`, update mode `ProcessEventsInDynamicUpdate`, background behaviour `ResetAndDisableNonBackgroundDevices`, editor behaviour `PointersAndKeyboardsRespectGameViewFocus`.
