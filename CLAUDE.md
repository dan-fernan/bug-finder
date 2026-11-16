# CLAUDE.md — Bug Defense: Don't Ship to Prod (Unity Tower Defense)

> Context file for Claude Code. Read this whole file before writing any code.
> Owner: Daniel Fernandez (GitHub: dan-fernan). Course: Video Game Design and Development, Fall 2026, Assignment 2.
> **DEADLINE: Tuesday, Nov 17, 2026, submitted as a GitHub repo.** Time is very short. Build in the phase order below, keep the game playable at the end of every phase, and commit after each phase.

---

## 0. TL;DR for Claude Code

- Build a **3D tower defense game in Unity** with a programming theme. **Bugs** crawl along a winding path through a codebase toward a **PROD server**. The player places **debugging-tool towers** that shoot physics projectiles at them. If a single bug reaches PROD, the game is over.
- **You cannot click around in the Unity Editor.** So the game is **code-first**: one static bootstrap builds the entire scene at runtime (map, path, camera, light, UI, managers). The user should only have to open the project and press Play in any scene, including the default `SampleScene`.
- Hit every grading requirement in §1. The README must map each requirement to the code that implements it.
- Robustness matters more than polish. Never leave the project in a non-compiling state.

---

## 1. Grading requirements (the target is A / A+)

The course offers CSCI 381 (undergrad) and CSCI 780 (grad) rubrics. **This design satisfies both.** Every item needs a concrete implementation and a README entry.

| Grade | Requirement | Where it's satisfied |
|---|---|---|
| C | Player can place towers | `TowerPlacer` (mouse raycast onto a `BuildTile`) |
| C | Towers fire continuously at a set rate | `Tower.fireRate` cooldown timer |
| C | Towers aim at an enemy | `Tower` rotates its head and barrel toward the target |
| C | Enemies travel predetermined paths | `PathDefinition` waypoints, `Enemy` follows them |
| C | Game ends when an enemy completes its path | `ProdServer` / `Enemy.ReachedEnd()` → `GameManager.GameOver()` |
| B | ≥2 tower types with different **rate of fire, projectile speed, and tower height** | 3 types in `TowerCatalog` (Linter / Unit Test / Debugger). **Height must visibly differ and the fire point must sit at that height.** |
| B | Text overlay clearly explains how to place towers | `HUD` instructions panel, always visible |
| B | Limited number of towers | "Sprint capacity" points plus a hard tower cap (§4.4) |
| B | Paths are not straight lines | Zig-zag path with 6+ turns (§4.1) |
| A | Multiple enemy types with different **speed and HP** | 4 types plus a boss in `EnemyCatalog` |
| A | Non-basic assets | Kenney Tower Defense Kit models (CC0), with primitive fallbacks (§7) |
| A (780: B) | Towers aim at the **predicted location** from target velocity and position | `AimPredictor.PredictInterceptPoint` (§5.6) |
| A+ | Above and beyond | Waves, boss that splits, Memory Leak growth, ballistic "Compiler" mortar with gravity-arc prediction, range rings, placement ghost, hit FX, speed toggle (§9) |

---

## 2. Environment & technical constraints (read carefully)

### 2.1 Detect the Unity version first
- Read `ProjectSettings/ProjectVersion.txt`. The project is probably **Unity 6 (6000.x)** but might be 2022.3 LTS. Adapt the APIs to it:
  - Unity 6: use `Rigidbody.linearVelocity` (`velocity` is obsolete). On 2022.3, use `velocity`. Prefer a small helper or `#if UNITY_6000_0_OR_NEWER`.
  - Unity 6: `FindObjectsByType<T>(FindObjectsSortMode.None)` / `FindFirstObjectByType<T>()`. Don't use the deprecated `FindObjectOfType`.
- The developer's machine is a Mac with Apple Silicon (M4).

### 2.2 Input system (common crash source)
- New Unity 6 projects default to **Input System Package (New)**. Under that setting, legacy `Input.GetKeyDown` / `Input.mousePosition` **throw exceptions**.
- Check `ProjectSettings/ProjectSettings.asset` for `activeInputHandler` (0 = old, 1 = new, 2 = both) and `Packages/manifest.json` for `com.unity.inputsystem`.
- Put **all** input behind one static wrapper `GameInput` that compiles under either setting:

```csharp
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
public static class GameInput {
    public static Vector2 MousePosition {
        get {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
            return Input.mousePosition;
#endif
        }
    }
    public static bool LeftClickDown  { get { /* Mouse.current.leftButton.wasPressedThisFrame  / Input.GetMouseButtonDown(0) */ } }
    public static bool RightClickDown { get { /* Mouse.current.rightButton.wasPressedThisFrame / Input.GetMouseButtonDown(1) */ } }
    public static bool KeyDown(KeyCodeLite k) { /* map to Keyboard.current[Key.X].wasPressedThisFrame / Input.GetKeyDown */ }
}
```
- Keys used: `1`, `2`, `3`, `4` (select tower), `Esc` / right-click (cancel selection), `R` (restart), `Space` (start next wave early), `F` (toggle 2× speed), `H` (toggle help panel).
- UI clicks need an `EventSystem`. Create it at runtime with `InputSystemUIInputModule` when the new Input System is active, otherwise `StandaloneInputModule`.

### 2.3 Render pipeline / materials
- Unity 6's default 3D template is **URP**. Materials made with the built-in `Standard` shader render **pink** under URP.
- Create all materials through one `MaterialFactory` that picks the shader at runtime:
  - If `GraphicsSettings.currentRenderPipeline != null`, use `Shader.Find("Universal Render Pipeline/Lit")` (color property `_BaseColor`, emission `_EmissionColor` + `EnableKeyword("_EMISSION")`).
  - Otherwise use `Shader.Find("Standard")` (`_Color`, `_EmissionColor`).
  - For unlit glow use `"Universal Render Pipeline/Unlit"` or `"Unlit/Color"`.
- Cache materials by key so they aren't created every frame.

### 2.4 UI: no TextMeshPro
- **Don't use TextMeshPro.** It requires importing "TMP Essential Resources" through an editor dialog, which you can't click.
- Use **uGUI** (`UnityEngine.UI.Text`, `Image`, `Button`) built from code, with the font `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` (Unity 2022.2+). If that's null, fall back to `"Arial.ttf"`.
- The legacy font **can't render emoji**. Use plain ASCII and symbols like `>`, `*`, `!`, `[ ]`.
- Use `CanvasScaler` with Scale With Screen Size at a reference of 1920×1080.

### 2.5 No custom layers or tags
- Layers and tags normally get created through the editor UI. **Don't depend on them.** Identify objects by component instead (`GetComponent<BuildTile>()`, `GetComponentInParent<Enemy>()`).
- Find targets through a static `EnemyRegistry` list, not `Physics.OverlapSphere` with layer masks.

### 2.6 Verifying compilation
- After each phase, check for compile errors. If the Unity Editor is closed, you can run batch mode:
  `"/Applications/Unity/Hub/Editor/<VERSION>/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath "$(pwd)" -logFile -`
  then grep the output for `error CS`. If the editor is open with this project, batch mode will fail on the project lock. In that case, ask Daniel to check the Console, or run a careful self-review instead.
- Don't add packages unless necessary. If you do, edit `Packages/manifest.json` and say so.

---

## 3. Theme & game design

### 3.1 Pitch
**Bug Defense: Don't Ship to Prod.** You're the last engineer awake before a release. Bugs are crawling through the codebase toward production. Deploy debugging tools along the call path to squash them. If one bug reaches PROD, the pager goes off and you lose.

### 3.2 Look & feel
- **Dark IDE / circuit-board aesthetic.** Camera background is near-black navy (`#0B0F1A`).
- **Buildable tiles:** dark slate (`#1A2233`) with a thin lighter border (a slightly smaller inset quad, or an emission edge if that's easy).
- **Path tiles:** emissive green, terminal-style (`#1FD17A`, low emission), like a glowing trace on a circuit board.
- **Spawn point:** labelled `git push` (a world-space text label, e.g. `TextMesh` or a world-space canvas) on a small portal or pad.
- **PROD server:** a tall rack of stacked dark cubes with small blinking green emissive "LED" cubes. It turns red and flashes on game over. Label: `PROD`.
- Towers keep the Kenney models (or primitives), tinted per type.
- **Bugs:** tinted per type (§4.3). For a bug look with primitives: flattened sphere body, small sphere head, 6 thin cylinder legs, with a small bob animation while moving.

### 3.3 Core loop
1. Game starts in a **Build phase** (~10 s countdown, or press Space to start immediately).
2. Wave N spawns bugs at `git push`. They follow the path to `PROD`.
3. Towers auto-target and fire physics projectiles. Bugs take damage and die, spawning a death FX plus floating text.
4. When a wave is cleared, the player gets **+2 sprint capacity**, then the next build phase starts.
5. Survive **wave 6** (boss wave) → **Victory** screen ("Release shipped. Zero bugs in prod.").
6. Any bug reaching PROD → **Game Over** immediately ("Bug shipped to production. You're on call now."). Press R to restart.

---

## 4. Detailed design specs

### 4.1 Map & path (concrete layout)
- Grid: **18 columns (x) × 12 rows (z)**, tile size **2 world units**, origin at world (0,0,0). Tile center = `(x*2 + 1, 0, z*2 + 1)`.
- Ground tiles are thin cubes (scale `2, 0.2, 2`, top surface at y = 0). Each tile gets a `BoxCollider` (from the cube) and a `BuildTile` component (`isPath`, `occupant`).
- **Path waypoints in grid coordinates** (enemies walk tile centers, slightly above ground at y = 0.5):
  ```
  (0,2) -> (5,2) -> (5,8) -> (10,8) -> (10,3) -> (14,3) -> (14,9) -> (17,9)
  ```
  Spawn is just off-grid at (-1,2). PROD sits at (18,9), just past the last waypoint.
- Mark every tile along each segment as `isPath = true` (no building there, green material).
- Keep waypoint data in one place, `PathDefinition.GridWaypoints`, so the path is easy to edit. Draw `Gizmos` lines in `OnDrawGizmos` for debugging.

### 4.2 Towers (`TowerCatalog`)
Store stats in a plain serializable C# class `TowerType` (not ScriptableObject assets, which are harder to author from code). All values are tunable.

| Key | Name | Cost (capacity) | Fire rate (shots/s) | Projectile speed | **Tower height** (fire point Y) | Damage | Range | Color | Flavor text |
|---|---|---|---|---|---|---|---|---|---|
| 1 | **Linter** | 1 | 4.0 | 30 | 1.0 | 4 | 7 | Cyan `#38BDF8` | "Catches small stuff, constantly." |
| 2 | **Unit Test** | 2 | 1.5 | 20 | 2.2 | 14 | 9 | Yellow `#FACC15` | "Reliable. Boring. Essential." |
| 3 | **Debugger** | 3 | 0.5 | 14 | 3.8 | 55 | 13 | Red `#F87171` | "Sets a breakpoint. Hard." |
| 4 | **Compiler** (A+) | 3 | 0.4 | 16 launch | 1.5 | 30 splash, r = 2.5 | 12 | Purple `#A78BFA` | "Lobs builds. Errors everywhere." |

- **Height must be physically obvious.** The tower body gets taller and the `firePoint` transform sits at the tower height. The B rubric explicitly lists tower height as a stat. Taller towers get more range, which explains why the Debugger reaches farthest.
- Straight-shot towers (Linter / Unit Test / Debugger) use projectiles with gravity **off**. The Compiler uses gravity **on** with a ballistic solution (§9).
- Each tower has a rotating **head** (yaw) and **barrel** (pitch) aimed at the predicted intercept point.
- Projectile visual: small emissive sphere in the tower color. Linter: 0.15 scale; Unit Test: 0.25; Debugger: 0.4 elongated capsule.

### 4.3 Enemies (`EnemyCatalog`)

| Name | HP | Speed (u/s) | Scale | Color | Special |
|---|---|---|---|---|---|
| **Syntax Error** | 20 | 4.5 | 0.6 | Red `#EF4444` | None. Fast and fragile. |
| **Logic Bug** | 60 | 2.6 | 0.9 | Orange `#F97316` | None |
| **Memory Leak** | 220 | 1.3 | 1.0 → grows to 1.6 | Purple `#A855F7` | Scale (and visible size) grows over its lifetime; HP +2 per second (A+ flavor) |
| **Race Condition** | 12 | 7.5 | 0.5 | Yellow `#EAB308` | Very fast; small speed jitter ±15% |
| **Production Outage** (boss) | 1500 | 0.9 | 2.2 | Dark red + emissive | On death spawns 6 Syntax Errors at its position that continue along the path from its current waypoint index |

- Each enemy shows a **world-space HP bar** that faces the camera, or a simple scaled quad pair.
- `Enemy.Velocity` (public, read-only) = `currentDirection * currentSpeed`. **Required for predictive aiming.** Compute it every frame from actual movement (`(pos - lastPos) / Time.deltaTime`), or from direction × speed. Direction × speed is more stable; use that.

### 4.4 Tower limit / economy ("sprint capacity")
- Start with **8 capacity points**. Each tower costs its `Cost`.
- Clearing a wave grants **+2** points.
- **Hard cap: 14 towers total**, regardless of points. Clearly satisfies "limited number of towers."
- **Right-click a placed tower to refund it.** (Optional; if implemented, refund the full cost during build phase and 50% during a wave.)
- HUD always shows: `Capacity: 5 / 8 pts   Towers: 3 / 14`.
- Placement fails with a red flash message when the tile is a path tile, already occupied, unaffordable, or at the cap. Show the reason, e.g. "Not enough sprint capacity."

### 4.5 Waves (`WaveSpawner`)
Waves are data: a list of `(enemyType, count, spawnInterval)` groups.

| Wave | Contents |
|---|---|
| 1 | 8 × Syntax Error @ 1.0 s |
| 2 | 6 × Syntax Error @ 0.8 s, then 4 × Logic Bug @ 1.5 s |
| 3 | 8 × Logic Bug @ 1.2 s, 6 × Race Condition @ 0.6 s |
| 4 | 3 × Memory Leak @ 3 s, 10 × Syntax Error @ 0.5 s |
| 5 | Mixed: 6 Logic, 4 Race, 3 Memory Leak, interleaved |
| 6 | **Boss:** 1 × Production Outage, plus 8 × Race Condition @ 1.0 s after a 5 s delay |

- Build phase: 10 s countdown (Space skips it). The HUD shows `Wave 2/6 starts in 7s - press SPACE to start now`.
- A wave is cleared when everything has spawned and `EnemyRegistry.Count == 0`.

### 4.6 Placement UX (`TowerPlacer`)
1. Press 1–4 (or click a tower button in the HUD) to select a tower type.
2. A semi-transparent **ghost tower** follows the mouse, snapped to the hovered tile. It's green if valid and red if invalid. A **range ring** (`LineRenderer` circle, 64 segments) shows its range.
3. Left-click places it (subtract cost, set `tile.occupant`). The selection **stays active** so the player can place several in a row. Esc or right-click on empty ground deselects.
4. Hovering an existing tower with nothing selected shows its range ring and a tooltip with its stats.
- Raycast: `Camera.main.ScreenPointToRay(GameInput.MousePosition)` → `Physics.Raycast` → `hit.collider.GetComponentInParent<BuildTile>()`.
- Ignore clicks over UI: `EventSystem.current.IsPointerOverGameObject()`.

### 4.7 HUD (`HUD`)
- **Top-left status panel:** `Wave 3 / 6`, `Bugs remaining: 7`, `Capacity: 5 / 10 pts`, `Towers: 4 / 14`, `Speed: 1x`.
- **Bottom bar:** 4 tower buttons. Each shows `[1] Linter - 1 pt` and small stat lines (`Rate 4.0/s | Proj 30 | Height 1.0 | Dmg 4`). Highlight the selected one; grey it out if unaffordable.
- **Always-visible instructions panel (top-right; B requirement, must be clear):**
  ```
  HOW TO DEPLOY TOOLS
  1. Press 1-4 (or click a button below) to pick a tower
  2. Move the mouse over a dark tile (not the green path)
  3. Left-click to place. Right-click / Esc to cancel
  Each tower costs sprint capacity. Earn more by clearing waves.
  SPACE: start wave now   F: 2x speed   H: hide help   R: restart
  Don't let a single bug reach PROD!
  ```
- **Center toast messages** (fade after 2 s): "Wave 3 cleared! +2 capacity", "Tile is on the path", and so on.
- **Game Over panel:** big red `PRODUCTION IS DOWN`, subtitle, waves survived, `Press R to restart`.
- **Victory panel:** green `RELEASE SHIPPED`, `Press R to play again`.

### 4.8 Camera & lighting
- Fixed perspective camera looking down at about 55°, framing the whole 36×24-unit board with margin. The PROD rack and spawn must both be visible. Compute the position from the grid bounds so it doesn't depend on aspect ratio guesses. Starting point: position `(18, 30, -8)`, looking at `(18, 0, 12)`; tune it so the board fills the screen at 16:9.
- Reuse `Camera.main` if the scene has one, otherwise create one. Set `clearFlags = SolidColor` and the background color from §3.2.
- One directional light (reuse an existing one if present) at rotation `(50, -30, 0)`, intensity ~1, plus a dim ambient. Emissive path and projectiles provide the glow.
- Optional (A+): WASD/arrow pan within bounds, scroll-wheel zoom clamped.

---

## 5. Architecture

### 5.1 Folder layout
```
Assets/
  Scripts/
    Core/       GameBootstrap.cs, GameManager.cs, GameInput.cs, GameState.cs
    Map/        GridMap.cs, BuildTile.cs, PathDefinition.cs, ProdServer.cs, SpawnPortal.cs
    Enemies/    EnemyType.cs, EnemyCatalog.cs, Enemy.cs, EnemyRegistry.cs, WaveSpawner.cs, WaveDefinition.cs, HealthBar.cs
    Towers/     TowerType.cs, TowerCatalog.cs, Tower.cs, Projectile.cs, AimPredictor.cs, TowerPlacer.cs
    UI/         HUD.cs, UIFactory.cs, Toast.cs
    Visuals/    MaterialFactory.cs, ModelLibrary.cs, RangeRing.cs, FloatingText.cs, HitFx.cs, Blinker.cs
    Audio/      Sfx.cs (optional)
  Resources/
    Models/     (Kenney FBX models copied here so Resources.Load works, §7)
    Audio/      (optional CC0 SFX)
  ThirdParty/   (original asset pack + its License.txt)
```
Use a namespace like `BugDefense` for every script.

### 5.2 Bootstrap: the scene is built from code
```csharp
public static class GameBootstrap {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() {
        if (Object.FindFirstObjectByType<GameManager>() != null) return; // already built (e.g., after restart)
        var root = new GameObject("BugDefense");
        root.AddComponent<GameManager>(); // GameManager.Awake builds everything else in order
    }
}
```
`GameManager.Awake()` / `Start()` build in this order: materials → camera/light → GridMap (tiles + path) → SpawnPortal + ProdServer → EnemyRegistry reset → WaveSpawner → TowerPlacer → EventSystem + HUD.

- **Restart:** `SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex)`. On reload the bootstrap runs again. **Reset all static state** (the `EnemyRegistry` list, `Time.timeScale = 1`) on boot and on restart, or you'll keep stale references.
- Make sure the active scene is in Build Settings. The default `SampleScene` usually is; if not, mention it in the README.

### 5.3 GameManager
- Singleton (`GameManager.Instance`).
- `GameState { Build, Wave, GameOver, Victory }`.
- Owns `capacity`, `towersPlaced`, `waveIndex`.
- `GameOver(Enemy culprit)`: set state, `Time.timeScale = 0` (or stop the spawner and enemies), flash PROD red, show the panel, and focus the camera on nothing special. Make sure R still works while `timeScale = 0` (input polling in `Update` still runs; just don't rely on scaled time in that code path).
- `F` toggles `Time.timeScale` between 1 and 2 (only in Build/Wave).

### 5.4 Enemy
```text
fields: EnemyType type; float hp, maxHp, speed; int waypointIndex; Vector3 Velocity {get;}
components: kinematic Rigidbody (isKinematic = true, interpolation on), CapsuleCollider or SphereCollider (not trigger)
Update/FixedUpdate:
  target = waypoints[waypointIndex]
  dir = (target - pos).normalized
  newPos = Vector3.MoveTowards(pos, target, speed * dt)
  rb.MovePosition(newPos); face dir (smooth Slerp)
  Velocity = dir * speed
  if reached target: waypointIndex++; if past last -> ReachedEnd()
TakeDamage(float dmg): hp -= dmg; flash white 0.05s; spawn floating damage text (optional); if hp <= 0 -> Die()
Die(): EnemyRegistry.Remove(this); HitFx burst; FloatingText "fixed" in green; special (boss split); Destroy
ReachedEnd(): GameManager.Instance.GameOver(this)
OnEnable/OnDisable: register/unregister in EnemyRegistry
```
- The boss split spawns children at the boss position with `waypointIndex` copied, so they continue along the path rather than restarting.

### 5.5 Tower
```text
fields: TowerType type; Transform head, barrel, firePoint; float cooldown; Enemy target
Update:
  if state not Wave/Build -> return
  retarget every 0.2s: choose enemy in range with the FURTHEST path progress (waypointIndex + fraction) -- "first" targeting is better than nearest
  if target: aimPoint = AimPredictor.PredictInterceptPoint(firePoint.position, target.AimPosition, target.Velocity, type.projectileSpeed)
             rotate head (yaw) + barrel (pitch) toward aimPoint (smooth, but fast: ~720 deg/s)
  cooldown -= dt; if cooldown <= 0 && target != null -> Fire(aimPoint); cooldown = 1/fireRate
Fire: spawn Projectile at firePoint, set velocity = (aimPoint - firePoint).normalized * projectileSpeed
```
- "Fire continuously at a set rate" means a steady cadence whenever a target is in range.
- Debug option: `Tower.showPredictionGizmo` draws a small marker at `aimPoint`. Good for demoing the A requirement in a GIF or video.

### 5.6 Predictive aiming (A requirement; get this exactly right)
Solve for time `t > 0` such that `|D + V·t| = s·t`, where `D = target - shooter`, `V` = target velocity, `s` = projectile speed. That gives the quadratic `(V·V - s²)t² + 2(D·V)t + D·D = 0`.

```csharp
public static class AimPredictor {
    /// Returns the point to aim at so a constant-speed (no gravity) projectile hits a target
    /// moving with constant velocity. Falls back to the target's current position if no solution.
    public static Vector3 PredictInterceptPoint(Vector3 shooter, Vector3 target, Vector3 targetVel, float projSpeed) {
        Vector3 d = target - shooter;
        float a = Vector3.Dot(targetVel, targetVel) - projSpeed * projSpeed;
        float b = 2f * Vector3.Dot(d, targetVel);
        float c = Vector3.Dot(d, d);
        float t;
        if (Mathf.Abs(a) < 1e-4f) {               // speeds nearly equal -> linear equation
            if (Mathf.Abs(b) < 1e-4f) return target;
            t = -c / b;
        } else {
            float disc = b * b - 4f * a * c;
            if (disc < 0f) return target;
            float sq = Mathf.Sqrt(disc);
            float t1 = (-b - sq) / (2f * a);
            float t2 = (-b + sq) / (2f * a);
            t = (t1 > 0f && t2 > 0f) ? Mathf.Min(t1, t2) : Mathf.Max(t1, t2);
        }
        return t > 0f ? target + targetVel * t : target;
    }
}
```
- Write a quick sanity test: an editor-only or `[ContextMenu]` method that logs results for a known case. Example: shooter at the origin, target at (10,0,0) moving (0,0,5), speed 20 → the intercept lies on the target's line ahead of it, and `|aim - shooter| / 20 ≈ t`.
- Known limitation, documented in the README: enemies turn at waypoints, so linear prediction can miss right at corners. For A+, optionally predict along the path: walk the remaining waypoints to find where the enemy will be at time `t` and iterate 3–4 times. If you implement it, make it a toggle (`P` key) so the difference can be demoed.

### 5.7 Projectile (3D physics; the assignment says "3D physics")
- `Rigidbody`: `useGravity = false` for straight shots (`true` for the Compiler), `collisionDetectionMode = ContinuousDynamic`, `interpolation = Interpolate`. `SphereCollider`, **not** trigger.
- `Physics.IgnoreCollision(projectileCollider, eachColliderOfOwnerTower)` on spawn so shots don't hit their own tower.
- `OnCollisionEnter`: if `collision.collider.GetComponentInParent<Enemy>()` exists, apply damage. In every case, spawn a small hit FX and destroy the projectile.
- Lifetime 4 s safety destroy, and destroy if `y < -5`.
- Enemies are kinematic Rigidbodies moved with `MovePosition`, so dynamic projectiles collide with them properly.

---

## 6. Build phases (commit after each one, keeping it playable)

> After each phase: make sure it compiles, play-test mentally or ask Daniel to press Play, then `git add -A && git commit -m "Phase N: ..."`.

**Phase 0: Repo hygiene (5 min)**
- Add the standard **Unity `.gitignore`** (`[Ll]ibrary/`, `[Tt]emp/`, `[Oo]bj/`, `[Bb]uild/`, `[Bb]uilds/`, `[Ll]ogs/`, `[Uu]ser[Ss]ettings/`, `*.csproj`, `*.sln`, `.vs/`, `.idea/`, `*.DS_Store`, etc.). Make sure `Assets/`, `Packages/`, and `ProjectSettings/` **are** committed, `.meta` files included.
- Check `ProjectVersion.txt`, the input handling setting, and the render pipeline, and note them in the README.

**Phase 1: Core loop (C grade)**
- `GameBootstrap`, `GameManager`, `GameInput`, `MaterialFactory`, camera and light.
- `GridMap` with tiles and the path, `PathDefinition`, `SpawnPortal`, `ProdServer`.
- `Enemy` (one type) following waypoints; `EnemyRegistry`; simple spawner (one enemy per second).
- `Tower` (one type) with direct aim and `Projectile` physics.
- `TowerPlacer` (click a tile to place).
- Game over when an enemy reaches PROD. Minimal on-screen text: "GAME OVER - R to restart".
- ✅ Acceptance: press Play, bugs walk the zig-zag path, click to place towers that shoot and kill bugs, and a leaked bug ends the game.

**Phase 2: B grade**
- `TowerType` / `TowerCatalog` with Linter, Unit Test, and Debugger. Visibly different heights; fire point at height.
- Keys 1–3 for selection, ghost preview, and range ring.
- Capacity and tower cap.
- Full HUD: status panel, tower buttons, instructions panel, toasts.
- ✅ Acceptance: all three towers are placeable and visibly different, the limit is enforced with a message, and the instructions are readable.

**Phase 3: A grade**
- `EnemyType` / `EnemyCatalog` (all four types plus boss data), HP bars, `WaveSpawner` with waves 1–6, build-phase countdown, victory screen.
- `AimPredictor` wired into all straight-shot towers.
- Kenney assets imported and used (§7), with primitive fallbacks.
- ✅ Acceptance: a full run from wave 1 to 6 is winnable with a reasonable layout. Towers visibly lead moving targets, which is most obvious on Race Conditions with the slow Debugger.

**Phase 4: A+ polish (do as many as time allows, in this order)**
1. Boss split plus Memory Leak growth.
2. Hit FX (a tiny particle burst via `ParticleSystem` built in code, or scaling spheres that fade), death "fixed" floating text, damage flash.
3. 2× speed toggle (F) and Space to start the wave early.
4. Compiler mortar tower with a ballistic arc and splash damage (§9).
5. PROD rack blinking LEDs, plus red alarm flashing on game over.
6. Path-aware prediction toggle (P).
7. Optional CC0 SFX (shoot, hit, death, game over).
8. Camera pan and zoom.

**Phase 5: Submission (don't skip)**
- README.md (§8), and a screenshot or GIF placeholder section.
- Final commit and push to GitHub. Daniel will create or confirm the remote. If no remote exists, run `git remote -v` and ask him; don't create repos on his behalf without asking.

---

## 7. Assets (the "non-basic assets" requirement)

- **Primary pack:** Kenney **Tower Defense Kit** (CC0), https://kenney.nl/assets/tower-defense-kit. It contains towers, weapons/turret heads, path and ground tiles, and enemy models.
  - Daniel may need to download and unzip it himself (browser download). If you can't fetch it, **ask Daniel to place the unzipped folder at `Assets/ThirdParty/KenneyTowerDefense/`.**
  - Use the **FBX** versions. Unity imports FBX natively; GLB needs the glTFast package.
  - **Don't guess file names.** List the folder contents, then pick suitable models: a tower base, turret heads for each tower type, an enemy model, and optionally decorative pieces (rocks, trees, crystals). Copy the chosen FBXs to `Assets/Resources/Models/` with clear names (`tower_linter.fbx`, etc.) so `Resources.Load<GameObject>("Models/tower_linter")` works.
  - Kenney models often use a shared colormap texture/material. If the materials appear pink under URP, assign `MaterialFactory` materials at runtime instead, or keep the imported material if it renders correctly.
  - Keep the pack's `License.txt` in `Assets/ThirdParty/` and credit "Kenney (www.kenney.nl), CC0" in the README.
- **`ModelLibrary` must fall back gracefully:** if `Resources.Load` returns null, build the primitive version (cylinder base + cube head + cylinder barrel for towers; the primitive bug from §3.2 for enemies). **The game must run with zero assets imported.**
- When instantiating a model, normalize its size: compute renderer bounds, scale to the target footprint (~1.6 units wide for towers), and place its base at y = 0. Strip or disable any imported colliders, then add your own simple colliders.
- The tower's `head` / `barrel` / `firePoint` transforms must be your own empty GameObjects. Parent the model meshes under them, so aiming works regardless of the model's pivot.
- Optional SFX: Kenney "Impact Sounds" / "Sci-fi Sounds" (CC0) → `Assets/Resources/Audio/`.

---

## 8. README.md contents (graders read this)

1. Title, one-paragraph pitch, screenshot/GIF placeholder.
2. **How to run:** Unity version, open the project, open any scene (e.g. `Assets/Scenes/SampleScene`), press Play. The game builds itself at runtime (`GameBootstrap`).
3. **Controls** table.
4. **Requirements checklist**: the table from §1 with the file and class for each item, and a short note on how to observe it in-game (e.g. "watch the Debugger lead Race Conditions").
5. **Tower and enemy stat tables** (from the catalogs).
6. **Predictive aiming**: two or three sentences on the quadratic intercept, plus the corner limitation and the path-aware toggle if built.
7. **Above & beyond** list.
8. **Credits:** Kenney assets (CC0), plus a note on AI assistance as the course permits ("work must be your own or the result of your own AI prompts").
9. Course: CSCI 381, Fall 2026, Assignment 2. Author: Daniel Fernandez.

---

## 9. A+ details

### Compiler (ballistic mortar with gravity, using the physics engine)
- Projectile `useGravity = true`. Given launch speed `v`, horizontal distance `x`, height difference `y`, and gravity `g`, the launch angle is:
  `θ = atan( (v² + sqrt(v⁴ - g(gx² + 2yv²))) / (gx) )`. Use the **high arc** (`+`) for a mortar feel, or the low arc if the high one is out of range.
- Prediction: iterate. Guess `t` from the current distance, predict the target position `P(t) = pos + vel·t`, solve the angle to `P(t)`, compute the actual flight time `t' = x / (v·cosθ)`, set `t = t'`, and repeat 4 times.
- On impact (any collision, or the ground): splash damage to all enemies within radius 2.5 (iterate `EnemyRegistry`), and an expanding ring FX.
- In the README this shows physics-aware prediction, beyond the base requirement.

### Other ideas if time remains
- **Heisenbug** enemy: becomes untargetable (semi-transparent) for 1 s whenever a tower targets it, then visible for 2 s.
- Tower upgrade ("Refactor") by clicking a placed tower during build phase: +25% damage, costs 1 pt.
- End-of-game stats: bugs squashed, shots fired, accuracy %.

---

## 10. Code quality rules

- C# with namespace `BugDefense`; one class per file, with the file name matching the class.
- No `FindObjectOfType` in `Update`. Cache references; use the `GameManager.Instance` singleton and `EnemyRegistry`.
- Serialize tunables as `public` fields or `[SerializeField]` so they can be tweaked in the Inspector during Play, but give everything sensible code defaults because nothing is set up in the editor.
- Null-check `Camera.main`, `Mouse.current`, `Keyboard.current`, and loaded resources.
- Clean up static lists on scene load (`RuntimeInitializeLoadType.SubsystemRegistration` reset for statics, since Domain Reload might be disabled).
- Short comments on non-obvious math (aim prediction, ballistic angle) and on each grading requirement, e.g. `// [Req B] Different tower heights`. This helps the grader and the README.
- Never commit with compile errors. If unsure, keep changes small and verify.

---

## 11. Working agreement with Daniel

- Daniel is a strong full-stack dev (JS/TS, Python, Java) but newer to Unity. When something needs to happen **in the Unity Editor** (importing assets, pressing Play, checking the Console), tell him exactly what to click, as a short numbered list.
- Ask before: creating or pushing to a GitHub remote, adding packages, or deleting files you didn't create.
- At the end of each phase, give a 3–5 line summary: what was added, how to test it in Play mode, and any known issues.
- If running out of time, **stop adding features and go straight to Phase 5.** A submitted A beats an unsubmitted A+.
