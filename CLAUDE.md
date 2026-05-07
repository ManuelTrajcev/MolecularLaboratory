# CLAUDE.md

This file provides context for Claude Code when working on this project.

## Project Overview

**Name:** Virtual Laboratory for Material Structure at the Molecular Level
**Type:** VR Application (Unity)
**Target Platform:** Meta Quest (Quest 2, Quest 3, Quest Pro) — standalone Android build
**Secondary Platform:** Quest Link / Air Link (PCVR) for development

## Concept

A virtual reality application simulating a scientific laboratory where users can enter the microworld of materials and interact with their internal structure at the atomic and molecular level. Users do not work with substances as wholes — they manipulate atoms, molecules, and the bonds between them.

### Core User Capabilities
- Observe atoms and molecules at an enlarged scale
- Manipulate them: move, join, separate
- Visually track bonds between atoms and structural changes
- Trigger chemical reactions through valid atomic combinations
- Receive visual and audio feedback for interactions

## Technical Stack

- **Engine:** Unity 6000.3.9f1
- **Project Template:** `com.unity.template.urp-blank@17.0.14` (URP-blank)
- **Render Pipeline:** URP 17.3.0 (already installed and configured)
- **VR SDK:** Unity XR Interaction Toolkit 3.3.1 + OpenXR 1.16.1 (Oculus XR Plugin 4.5.4 still installed but unused)
- **XR Plugin:** OpenXR for both Android (with Meta Quest feature group + Meta Quest Touch Plus controller profile) and Standalone/PCVR — pure Unity XR stack, no Meta XR All-in-One SDK from Asset Store
- **Scripting Backend:** IL2CPP, ARM64 (already configured)
- **Color Space:** Linear (already set)
- **Input:** New Input System 1.18.0 only (`activeInputHandler: 1`)

## Performance Targets

- Quest 2: 72 FPS minimum
- Quest 3: 90 FPS minimum
- Draw calls: < 100 per frame
- Use GPU Instancing for atoms (many duplicate spheres)
- Single realtime directional light; bake everything else
- Mobile-friendly URP shaders only
- Fixed Foveated Rendering enabled

## Project Structure

```
Assets/
├── Scenes/              # MainMenu, Laboratory, MicroWorld
├── Prefabs/
│   ├── Atoms/           # Element prefabs (H, O, C, N, etc.)
│   ├── Molecules/       # Pre-built molecule prefabs
│   ├── Lab/             # Workbench, shelves, periodic table
│   └── UI/              # VR UI panels and buttons
├── Scripts/
│   ├── Interaction/     # XR grab, bond formation logic
│   ├── Chemistry/       # Atom, Bond, Molecule, ReactionSystem
│   ├── UI/              # Menu and in-world UI
│   └── Managers/        # Scene, audio, game state managers
├── Materials/           # URP materials, atom CPK coloring
├── Shaders/             # Custom shaders (atom glow, bond visuals)
├── Audio/               # SFX, ambient, haptic patterns
├── Models/              # 3D assets for lab environment
├── ScriptableObjects/
│   ├── Elements/        # Element definitions (valence, color, mass)
│   └── Reactions/       # Reaction recipes
└── Settings/            # XR, Input, URP render assets
```

## Architecture Notes

### Atom System
Each atom is a prefab with:
- Sphere mesh, color-coded using CPK convention (H=white, O=red, C=gray/black, N=blue, etc.)
- Rigidbody + SphereCollider
- XR Grab Interactable
- `Atom.cs` script: holds reference to ElementSO ScriptableObject, current bonds list, valence tracking

### Bond System
- Dynamically instantiated cylinder between two atoms
- `Bond.cs` script: tracks bond type (single/double/triple), endpoints
- Updates transform every frame to follow both atoms

### Reaction System
- ScriptableObject-based: `ReactionSO` defines input atoms → output molecule + effects
- Validated by `MoleculeManager` when atoms come into bonding proximity
- Chemistry rules enforced: valence limits, valid element combinations

### Scene Flow
1. **MainMenu** — entry, settings, tutorial launcher
2. **Laboratory** — main lab room with workbenches and tools
3. **MicroWorld** — zoomed-in molecular manipulation space

## Coding Conventions

- C# naming: PascalCase for classes/methods, camelCase for fields, _camelCase for private
- Use `[SerializeField] private` over `public` for inspector exposure
- Prefer ScriptableObjects for data (elements, reactions, settings)
- Use Unity events / UnityActions for decoupled communication
- Namespace all scripts: `MolecularLab.Chemistry`, `MolecularLab.Interaction`, etc.

## VR-Specific Constraints

- No HTML `<form>` style coupling — all UI must be world-space and laser-pointer/hand interactable
- Comfort: provide both continuous and teleport locomotion; snap turning by default
- All text must be TextMeshPro at readable VR sizes (avoid sub-1cm world-space text)
- Haptic feedback on every meaningful interaction (grab, snap, reaction)
- Spatial audio for all 3D sound sources

## Current Status

**Phase 1 complete** (2026-04-30). Phase 2–3 partial. Phase 4 mostly complete (2026-05-03). **Phase 5 started** (2026-05-07): chemistry data layer + Atom script.

**Repo plumbing** (2026-04-30):
- Git initialized with Unity-flavored `.gitignore` (Library/, Temp/, Logs/, UserSettings/, obj/, Bee/, BuildReports/, IDE files, Mac noise, archives all ignored)
- Git LFS configured via `.gitattributes` for binary assets (images, audio, video, 3D models, fonts, native plugins, .unitypackage, .apk/.aab) — Unity is in *Force Text* serialization mode so .unity/.prefab/.asset stay as YAML text with `unityyamlmerge` driver
- `README.md` at project root: setup instructions for new clones (prerequisites, `git lfs pull`, Unity Hub install, VR Editor setup pointers, Quest deployment, troubleshooting)
- `.claude/settings.json` + `.claude/hooks/` committed (Stop hook uses `$CLAUDE_PROJECT_DIR` so it works on any clone); `.claude/settings.local.json` git-ignored

### Phase 1 — Project bootstrap (done)
- Unity 6000.3.9f1, URP-blank template, URP 17.3.0
- URP render assets at `Assets/Settings/`: `Mobile_RPAsset`, `PC_RPAsset`, `Mobile_Renderer`, `PC_Renderer`, Global Settings, Volume Profiles
- XR packages: `com.unity.xr.management@4.5.4`, `com.unity.xr.oculus@4.5.4`, `com.unity.xr.openxr@1.16.1`, `com.unity.xr.interaction.toolkit@3.3.1`, `com.unity.xr.hands@1.7.3`
- XR Loaders at `Assets/XR/Loaders/`: `OculusLoader.asset`, `OpenXRLoader.asset`
- `Assets/XR/XRGeneralSettingsPerBuildTarget.asset`: Standalone (OpenXR) wired; **Android (Oculus) loader still needs Editor toggle**
- `Assets/XR Rig.prefab` at root
- Android build: IL2CPP, ARM64, Vulkan+OpenGLES3, MinSDK 29
- TMP comes via `com.unity.ugui@2.0.0` in Unity 6 — no separate package

### Phase 2–3 — Project structure (partial)
- `Assets/Scripts/` subfolders created: `Chemistry/`, `Interaction/`, `Managers/`, `UI/` (all empty — no gameplay code yet)
- `Assets/Scenes/Laboratory.unity` created
- `Assets/XRI/` (XRI default settings) and `Assets/Samples/XR Interaction Toolkit/3.3.1/{Starter Assets, XR Interaction Simulator}` samples imported
- Active XR rig: `Assets/Samples/XR Interaction Toolkit/3.3.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab` (guid `f6336ac4ac8b4d34bc5072418cdc62a0`) — comes pre-wired with Locomotion Mediator, Continuous Move + Snap Turn + Teleportation Providers, Near-Far Interactors, ray UI interactors, hand-pose visuals
- Old `Assets/XR Rig.prefab` (guid `b10fd511f03efb04db1d0f4452cbe655`) is no longer referenced by any scene; safe to delete when convenient

### Phase 4 — Laboratory scene (mostly complete, 2026-05-03)

Current scene contents (`Assets/Scenes/Laboratory.unity`):
- Directional Light, Global Volume, XR Rig (prefab instance)
- **Floor** (Plane) with `Assets/Materials/Floor.mat` — checklist step 4 done
- **Cube** (test grabbable) with `Assets/Materials/TestCube.mat` — checklist step 6 done (named `Cube`, not `Test Grab Cube` — cosmetic)
- **Event System** — checklist step 3 done
- Lightmap baked: `Assets/Scenes/Laboratory/LightingData.asset` present — checklist step 8 done

URP / XR config:
- `Mobile_RPAsset` tuned: MSAA=4, RenderScale=1.0, HDR off, ShadowDistance=15 — checklist step 9 done
- `Assets/XR/Settings/OculusSettings.asset` and `OpenXR Package Settings.asset` configured
- `XRGeneralSettingsPerBuildTarget.asset` has Android Settings populated — Oculus Android loader appears wired (verify in Editor)

**Editor testing:** XR Device Simulator is set to auto-instantiate in Editor only (`Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset`: `m_AutomaticallyInstantiateSimulatorPrefab: 1`, `m_AutomaticallyInstantiateInEditorOnly: 1`). Pressing Play renders headset view in Game View with mouse+keyboard control.

**Open items to verify in Editor:**
- *XR Interaction Manager* GameObject — not visible at scene top level via grep. Confirm it exists (checklist step 2). Without it, XR interactors silently no-op and the Console logs "no XR Interaction Manager found".
- *Android XR plugin chosen: **OpenXR + Meta Quest feature group*** (resolved 2026-05-03). `MetaQuestFeature Android` and `MetaQuestTouchPlusControllerProfile Android` both enabled in OpenXR Package Settings; Android Providers list has OpenXRLoader. Original CLAUDE.md plan said "Oculus XR Plugin (Android)" — superseded by OpenXR. Quest-specific tuning (FFR, AppSW, dynamic resolution) goes through OpenXR's Meta Quest feature group settings, not OculusSettings.asset.
- Confirm cube is reachable / grabbable when pressing Play with the simulator.

**Next after Phase 4:** Atom/Bond/Reaction system implementation, ScriptableObject element definitions, MicroWorld scene, MainMenu scene.

### Phase 5 — Chemistry core (in progress, 2026-05-07)

**Done (code + data):**
- `Assets/Scripts/Chemistry/ElementSO.cs` (guid `e1e2e3e4e5e6e7e8e9eaebecedeeefe0`) — `[CreateAssetMenu]` "MolecularLab/Element". Fields: symbol, elementName, atomicNumber, atomicMass, valence, covalentRadius, displayRadius, cpkColor. All read-only via properties.
- `Assets/Scripts/Chemistry/Atom.cs` (guid `a1a2a3a4a5a6a7a8a9aaabacadaeafa0`) — `RequireComponent(Rigidbody, SphereCollider)`. Holds `ElementSO`, tracks `_usedValence`, exposes `RemainingValence` / `CanBond` / `ConsumeValence` / `ReleaseValence` for the upcoming Bond class. `ApplyElement()` (called in `Awake` and `OnValidate`) sets `transform.localScale` from `displayRadius`, sets `_BaseColor` via shared `MaterialPropertyBlock` (GPU-instancing-friendly), and renames the GameObject `Atom_<symbol>` in edit mode.
- Six element ScriptableObjects in `Assets/ScriptableObjects/Elements/`: Hydrogen (H, val 1, white), Carbon (C, val 4, dark grey), Nitrogen (N, val 3, blue), Oxygen (O, val 2, light blue — customized away from CPK red), Sodium (Na, val 1, violet), Chlorine (Cl, val 1, green). CPK colors written as sRGB 0–1 (Inspector-displayed values); project is Linear color space, so the Lit shader will convert at sample time.

**Pending — must be done in Editor (cannot be safely written as YAML):**

1. **Atom prefab** (`Assets/Prefabs/Atoms/Atom.prefab`):
   - GameObject → 3D Object → Sphere (deletes default MeshCollider; we use SphereCollider)
   - Reset transform; remove the auto-added MeshCollider, add **SphereCollider** (radius set automatically by `Atom.ApplyElement()`)
   - Add **Rigidbody**: Mass 0.05, Use Gravity ✓, Interpolate, Collision Detection Continuous Dynamic
   - Add **XR Grab Interactable** (auto-adds XR General Grab Transformer); Movement Type Instantaneous, Throw On Detach ✓
   - Add **Atom** component; assign the inner sphere MeshRenderer to the `meshRenderer` field (or leave null — `ApplyElement` falls back to `GetComponentInChildren`)
   - Material: `Assets/Materials/M_Atom.mat` — URP/Lit, Smoothness ~0.4, Metallic 0, **Enable GPU Instancing ✓**. Color comes from `MaterialPropertyBlock`, so the material's base color is irrelevant.
   - Drag prefab into `Assets/Prefabs/Atoms/`. Create per-element variants OR leave a single base prefab and assign `ElementSO` per scene instance.
2. **Smoke test in Laboratory scene:** drag Atom prefab into scene, assign `Hydrogen.asset`, press Play with XR Device Simulator. Atom should be white-ish, scaled to 8 cm diameter, grabbable. Repeat with Oxygen (red, 12 cm).
3. **`Assets/Materials/M_Atom.mat` created** (2026-05-07) but **GPU Instancing is currently OFF** (`m_EnableInstancingVariants: 0`). Tick *Enable GPU Instancing* at the bottom of the material Inspector — without it, every atom becomes a separate draw call on Quest.

**Step 3 done (code, 2026-05-07):**
- `Assets/Scripts/Chemistry/Bond.cs` (guid `b1b2b3b4b5b6b7b8b9babbbcbdbebfb0`) — MonoBehaviour for a cylinder. Static `Bond.Create(prefab, a, b, order, parent)` consumes valence on both atoms (rolls back if either fails) and returns the Bond instance. `LateUpdate` re-positions/rotates/scales the cylinder between the two atoms each frame; auto-destroys if either atom is null or distance exceeds `breakDistance` (default 0.5 m). `OnDestroy` releases valence. Default cylinder mesh is 2 units tall, so y-scale = `len * 0.5`. Thickness scales with bond order (single/double/triple).
- `Assets/Scripts/Chemistry/BondManager.cs` (guid `c1c2c3c4c5c6c7c8c9cacbcccdcecfc0`) — singleton scene component. Holds `bondPrefab`, `bondParent` (defaults to its own transform). **Atom discovery is via `FindObjectsByType<Atom>(FindObjectsSortMode.None)`** rather than a manual registry — earlier registry-based design had silent Awake/OnEnable order failures (atoms registered before BondManager.Instance was set, leading to `RegisteredAtoms` being empty at grab time). `TryFormBondsAround(atom)` finds the nearest other atom within `(rA + rB) * bondFormDistanceMultiplier + bondFormSlack` (defaults 1.5 and 0.05 m) that has free valence and isn't already bonded, then calls `Bond.Create`. Currently always creates order=1 bonds.
- `Assets/Scripts/Interaction/AtomGrabSensor.cs` (guid `d1d2d3d4d5d6d7d8d9dadbdcdddedfd0`) — XRI bridge. `RequireComponent(Atom, XRGrabInteractable)`. **Atom discovery is via `FindObjectsByType<Atom>` on grab/release**, same rationale as BondManager (registry-based design failed silently). **On grab (`selectEntered`)** — iterates every collider in the held atom's hierarchy against every collider in every other atom in the scene and calls `Physics.IgnoreCollision(true)` so the held atom can freely pass through others while placing (was knocking other atoms aside before). **On release (`selectExited`)** — re-enables collisions and calls `BondManager.Instance.TryFormBondsAround`. Has a `debugLog` toggle that prints GRABBED / RELEASED / scan-and-pair counts / bond-result lines for diagnostics. Uses XRI 3.x namespaces (`UnityEngine.XR.Interaction.Toolkit.Interactables` for `XRGrabInteractable`).

**Pending Editor steps for step 3:**
1. **Bond prefab** (`Assets/Prefabs/Atoms/Bond.prefab` or under `Prefabs/Lab/`):
   - GameObject → 3D Object → Cylinder; remove the auto-added CapsuleCollider (bonds shouldn't physically collide)
   - Add **Bond** component (atom refs are assigned at runtime by `Bond.Create`)
   - Material: any URP/Lit, light grey, GPU Instancing ✓ recommended
2. **BondManager scene object:** add empty GameObject `BondManager` to Laboratory scene. Add `BondManager` component, drag the Bond prefab into the `bondPrefab` slot.
3. **Atom prefab update:** add `AtomGrabSensor` component to the existing Atom prefab. (Requires the prefab to already have `Atom` + `XRGrabInteractable`, which step 2 set up.)
4. **Smoke test:** put two Hydrogen atoms in the scene, grab one with the simulator, drag it close to the other and release. A cylinder bond should appear connecting them. Pulling them apart > 0.5 m destroys the bond and frees their valence (test by re-bonding).

**Step 4 done (code + data, 2026-05-07):**
- **`Atom.cs` refactored** — now holds `List<Bond> _bonds` (was just `_usedValence` int). `UsedValence` is computed from `Σ bond.Order`. New API: `RegisterBond(Bond)` / `UnregisterBond(Bond)`. The old `ConsumeValence`/`ReleaseValence` methods were removed (Bond now manages adjacency directly).
- **`Bond.cs` updated** — `Claim()` calls `a.RegisterBond(this)` + `b.RegisterBond(this)` instead of mutating an int. `Release()` (in `OnDestroy`) unregisters. Net result: Atom↔Bond is now a true bidirectional graph traversable from either side.
- **`Assets/Scripts/Chemistry/Molecule.cs`** (Unity-assigned guid `7cea08a496bf9405986c4d741a2ac159`) — static `Molecule.BuildFrom(Atom seed)` returns a `Snapshot { List<Atom> Atoms; Dictionary<ElementSO,int> ElementCounts; bool IsClosed }` via BFS over Atom.Bonds. `IsClosed` is true when every atom in the connected component has `RemainingValence == 0`.
- **`Assets/Scripts/Chemistry/ReactionSO.cs`** (Unity-assigned guid `5b667864b4f4443c59f3ba35549d554b`) — `[CreateAssetMenu]` "MolecularLab/Reaction". Fields: `displayName`, `inputs` (list of `{ElementSO element; int count}`), `effectPrefab`, `sfx`. `Matches(IReadOnlyDictionary<ElementSO,int>)` requires exact multiset equality (same element keys, same counts).
- **`Assets/Scripts/Chemistry/ReactionSystem.cs`** — scene component. Subscribes to `BondManager.BondFormed`; on each new bond, builds the molecule from `bond.A`, gates on `IsClosed`, then linear-scans `reactions` for the first matching `ReactionSO`. On hit: spawns `effectPrefab` and plays `sfx` at the molecule centroid, logs to console.
- **`BondManager.cs` updated** — added `public event Action<Bond> BondFormed`, raised after a successful `Bond.Create` inside `TryFormBondsAround`. ReactionSystem subscribes via `FindFirstObjectByType<BondManager>()` if not assigned in Inspector.
- **Two seed reactions** in `Assets/ScriptableObjects/Reactions/`: `Water.asset` (2× Hydrogen + 1× Oxygen → "Water (H2O)") and `Salt.asset` (1× Sodium + 1× Chlorine → "Sodium Chloride (NaCl)"). Both have null `effectPrefab` and `sfx` — assign in Inspector once you have a particle system / audio clip.

**Pending Editor steps for step 4:**
1. Add **ReactionSystem** component to the BondManager GameObject (or a sibling). Drag `BondManager` into its `bondManager` slot (or leave null — auto-resolved). Drag `Water.asset` and `Salt.asset` into the `reactions` list.
2. **Smoke test water:** drop two H atoms + one O in the scene. Bond H–O, then second H–O. Console should log `[Reaction] Formed Water (H2O) (3 atoms)`. Break a bond → free valence → no longer closed → next bond re-triggers if multiset still matches.
3. **Optional polish:** create a small particle prefab (sparkle / glow burst) and an SFX clip; assign to each ReactionSO. Wire haptic feedback on `BondManager.BondFormed` if desired.

**Known limitations / future work:**
- Multiset match doesn't validate *structure*, only *composition*. H–H–H–O–O (linear) would match the same multiset as the standard H₂O if such valences allowed it — they don't here, but worth keeping in mind for organic molecules where isomers share formulas.
- Only order-1 bonds are formed by `BondManager` (the data model supports 2 and 3 — UI for upgrading bond order is future work).
- ReactionSystem fires every time a closing bond forms; no deduplication if the same molecule survives multiple frames.

**Next code step:** wire visual/audio polish (ReactionSO effect prefabs + SFX), then move to MainMenu / MicroWorld scenes, periodic table UI for element spawning, and tutorial flow.

## Phase 4 — Laboratory Scene Manual Setup

These steps must be performed in the Unity Editor (cannot be set via YAML). Re-execute after IDE/scene reset if needed.

### 1. Scene baseline
- Open `Assets/Scenes/Laboratory.unity`
- Confirm in Hierarchy: Directional Light, Global Volume, XR Rig
- File → Build Profiles → add `Laboratory` to Scenes In Build

### 2. XR Interaction Manager
- Hierarchy → right-click → **XR → Interaction Manager**
- Rename `XR Interaction Manager`, leave at world origin
- Only one per scene

### 3. Event System (UI clicks)
- Hierarchy → right-click → **UI → Event System**
- Remove `Standalone Input Module`
- Add Component → **XR UI Input Module**

### 4. Floor
- Hierarchy → right-click → **3D Object → Plane**, rename `Floor`
- Position `(0, 0, 0)`, Scale `(2, 1, 2)` → 20×20 m
- Mark Static (Contribute GI, Occluder Static)
- Material `Assets/Materials/M_Floor.mat`: URP/Lit, mid-grey base, Smoothness 0.2, Metallic 0
- Default Mesh Collider, Convex unchecked

### 5. XR Rig placement
- Position `(0, 0, 0)` (override prefab modification)
- On `XR Origin (XR Rig)`: Tracking Origin Mode = `Floor`, Camera Y Offset = `0`
- Verify children: Camera Offset → Main Camera, LeftHand Controller, RightHand Controller (each with Near-Far Interactor in XRI 3.3.x)
- Main Camera: Tag `MainCamera`, Near clip `0.01`, single Audio Listener in scene

### 6. Test grab cube
- Hierarchy → right-click → **3D Object → Cube**, rename `Test Grab Cube`
- Position `(0, 1, 0.6)`, Scale `(0.15, 0.15, 0.15)`
- Add **Rigidbody**: Mass 0.5, Use Gravity ✓, Interpolate `Interpolate`, Collision Detection `Continuous Dynamic`
- Add **XR Grab Interactable** (auto-adds XR General Grab Transformer): Movement Type `Instantaneous`, Throw On Detach ✓, Smooth Position/Rotation ✓, Track Position/Rotation ✓
- Material `Assets/Materials/M_TestCube.mat` (URP/Lit, any color)
- Box Collider — Is Trigger off

### 7. Haptic feedback (optional)
- On cube: Add Component → **Simple Haptic Feedback**
- Select Entered: amp 0.5, dur 0.1; Hover Entered: amp 0.2, dur 0.05

### 8. Lighting tune
- Directional Light: Mode `Mixed`, Intensity ~1.2, Soft Shadows, Strength 0.7
- Window → Rendering → Lighting:
  - Environment Source: `Skybox` (or `Color`)
  - Mixed Lighting Mode: `Subtractive` (cheapest on Quest)
  - Generate Lighting (one bake)

### 9. URP per-platform renderer
- Project Settings → Quality:
  - Android tier RP Asset = `Mobile_RPAsset`
  - Standalone tier RP Asset = `PC_RPAsset`
- `Mobile_RPAsset`: MSAA 4x, Render Scale 1.0, HDR off (Quest 2), Shadow Distance 15, Cascades 1

### 10. XR runtime
- Project Settings → XR Plug-in Management:
  - Android tab: ✓ **Oculus** (the still-pending Phase 1 item)
  - Standalone tab: ✓ **OpenXR** (already set)
- Oculus sub-page (Android): Stereo Rendering = `Multiview`, Low Overhead Mode ✓, Optimize Buffer Discards ✓, Symmetric Projection ✓, Subsampled Layout ✓

### 11. Editor test
- Connect Quest via Link/Air Link, or Window → XR → **XR Device Simulator**
- Press Play: spawn at floor, see controllers/hands, ray-grab or hand-grab the cube, throw it
- Console must be free of "no XR Interaction Manager found" warnings

### 12. Build verification
- File → Build Profiles → Android → Switch Platform
- Connect Quest via USB, **Build And Run**
- On-device: floor visible, cube grabbable, ≥72 fps Quest 2 / ≥90 fps Quest 3

## Oculus / Quest Optimization Settings

Settings to verify or apply in the Unity Editor (cannot be set via YAML directly):

### In-Editor Checklist
- **XR > Android loader**: Edit > Project Settings > XR Plug-in Management > Android tab → enable Oculus
- **Fixed Foveated Rendering (FFR)**: Project Settings > XR Plug-in Management > Oculus → set FFR level to High (Quest 2) or use EyeTracked FFR on Quest Pro; Quest 3 uses Dynamic FFR via eye tracking — leave at `0` (auto) if using the Oculus XR Plugin's runtime API
- **Multiview / Single Pass Instanced**: XR Interaction Toolkit XR Camera should use Single Pass Instanced rendering; confirm in OculusSettings.asset or via: Project Settings > XR > Oculus > Stereo Rendering Mode = Multiview
- **Vulkan first, OpenGLES3 fallback**: Already correctly set (confirmed in `m_BuildTargetGraphicsAPIs`)
- **Application ID**: Change `applicationIdentifier` for Android from the template default (`com.UnityTechnologies.com.unity.template.urpblank`) to your own bundle ID (e.g., `com.halicea.molecularlab`)
- **Physics Fixed Timestep**: Edit > Project Settings > Time → Fixed Timestep = `0.01111` (1/90) for Quest 3, or `0.01389` (1/72) for Quest 2 minimum target
- **Depth Submission / Spacewarp**: For AppSW (Application SpaceWarp on Quest 2+), ensure depth texture submission is enabled in OculusSettings — skip until needed as it has CPU cost
- **Quest 3 Dynamic Resolution**: The Oculus XR Plugin exposes `OculusSettings.m_UseHWOcclusionMeshes = true` — verify in Editor, not settable via raw YAML

### Runtime Recommendations (code-level)
- Call `OVRManager.gpuLevel` and `cpuLevel` (or the `Performance.TrySetPerformanceLevels` XR API) at startup: GPU=3, CPU=3 for Quest 2; GPU=4, CPU=4 for Quest 3
- Enable `OVRManager.tiledMultiResLevel = OVRManager.TiledMultiResLevel.LevelTop` (FFR) if using the Meta OVR utilities; or use `XRSettings.eyeTextureResolutionScale` for simpler resolution scaling
- For atoms (many identical sphere instances): use `Graphics.DrawMeshInstanced` or ensure GPU Instancing is enabled on all atom materials

## Things to Avoid

- Built-in Render Pipeline (won't perform on Quest)
- Realtime shadows on dynamic objects (bake them)
- High-poly meshes (keep atoms ~ 200–500 tris)
- Standard Shader (use URP Lit / Simple Lit)
- Synchronous scene loads (use async with loading transition)
- Heavy post-processing (bloom OK, motion blur NEVER in VR)
