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
- **`Bond.cs` updated** — `Claim()` calls `a.RegisterBond(this)` + `b.RegisterBond(this)` instead of mutating an int. `Release()` (in `OnDestroy`) unregisters. Net result: Atom↔Bond is now a true bidirectional graph traversable from either side. **Also adds a `FixedJoint` between the two atoms' Rigidbodies on Claim** (with `enableCollision = false`, `breakForce = Infinity`) so they're physically locked together once bonded; previously the bond was visual-only and atoms could drift apart freely. Joint is destroyed in `Release`. Bond auto-destroys via `LateUpdate` distance check (default 0.5 m) — if user yanks past that threshold, joint disappears with the bond. **On creation, atoms snap to a standardized equilibrium distance** = `(rA + rB) * bondLengthMultiplier` (default 1.5). Snap logic: if one atom is already in a molecule (Bonds.Count > 1 after registering this bond), it stays put and the other is pulled to the target distance; if both are free, both move symmetrically toward the midpoint; if both are anchors in existing molecules, no snap (would tear the geometry). **Order matters**: snap runs *before* the FixedJoint is added — otherwise the joint locks atoms at the release distance and fights the teleport, collapsing the cylinder length to ~0 and making the bond look invisible.
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

**Next code step:** wire visual/audio polish (ReactionSO effect prefabs + SFX), then move to MainMenu / MicroWorld scenes, and tutorial flow.

### Phase 6 — Periodic Table Wall (2026-05-14)

**Done (code + data + scene):**
- **All 118 ElementSO assets** in `Assets/ScriptableObjects/Elements/` covering the entire periodic table (H..Og). GUIDs follow pattern `e2e2e2e2e2e2e2e2e2e2e2e2e2e2e<Z-hex-3-digit>` (e.g. Potassium Z=19 → `…e013`, Oganesson Z=118 → `…e076`). Exceptions: the original 6 (H, C, N, O, Na, Cl) keep their legacy GUIDs (`111…`, `222…`, `333…`, `444…`, `555…`, `666…`).
- **Lanthanide / actinide layout**: in `PeriodicTableUtils`, Ce..Lu (58..71) map to *period 8*, groups 4..17; Th..Lr (90..103) map to *period 9*, groups 4..17. La (57) and Ac (89) sit at group 3 of periods 6 and 7 respectively. The wall renders this as two extra rows below the main grid.
- **Element data quality**: atomic mass, valence, covalent radius, category, state, electron config, oxidation states, electronegativity, density, mp/bp populated for all 118. CPK colors use the Jmol convention. Valence is the *common bonding count* (e.g. transition metals use representative value, noble gases 0). For superheavies (Z>=104) some properties are 0 / unknown.
- **`Assets/Scripts/Chemistry/PeriodicTableUtils.cs`** (guid `f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0`) — static `TryGetPosition(atomicNumber, out GridPosition)` returns `{Period, Group}` for Z=1..30 (lanthanides/actinides intentionally omitted; not needed in scope).
- **`Assets/Scripts/Interaction/ElementSpawnButton.cs`** (guid `f1f1f1f1f1f1f1f1f1f1f1f1f1f1f1f1`) — `RequireComponent(XRSimpleInteractable)`. On `selectEntered`, instantiates `atomPrefab` at the configured `spawnAnchor` and calls `atom.SetElement(element)`. Has cooldown (`spawnCooldown`, default 0.4s) to prevent spam, applies small `upwardImpulse` (default 0.4) on the spawned Rigidbody, and a `debugLog` toggle. `Configure(element, prefab, anchor)` lets `PeriodicTableWall` wire it at runtime.
- **`Assets/Scripts/Interaction/PeriodicTableWall.cs`** (guid `f2f2f2f2f2f2f2f2f2f2f2f2f2f2f2f2`) — scene component. At `Start`, procedurally builds:
  1. A dark backing panel sized to the grid + padding (`panelColor`, default near-black)
  2. A `SpawnAnchor` child Transform at `spawnAnchorLocalOffset` (default below front of wall) — atoms spawn here
  3. One colored Cube primitive per ElementSO at its (period, group), CPK color via `MaterialPropertyBlock`, sized by `cellSize` × `cubeDepth`. Each cube has an `XRSimpleInteractable` + `ElementSpawnButton`.
  4. A `TextMeshPro` (3D) label per cube showing the element symbol, in front of the cube face. Falls back gracefully (logs warning, skips labels) if `TMP_Settings.defaultFontAsset` is null (i.e. user hasn't imported TMP Essentials yet).
  - **Wall-local axis convention:** +Z is the front face (cubes, labels, spawn anchor all sit on +Z side of the wall). Place the wall in the scene so its forward axis points toward the user — at scene rotation `(0, 180, 0)` the wall's local +Z becomes world -Z, so a wall at world `(0, 1.6, 2.5)` faces a user standing near origin looking down +Z. This is exactly the placement used in `Laboratory.unity`.
- **`Assets/Scenes/Laboratory.unity` updated** — new `Periodic Table` GameObject (FIDs 7700000001-7700000003) at world `(0, 1.6, 2.5)` rot `(0, 180, 0)`, registered in SceneRoots. Wired in YAML: `atomPrefab` → Atom.prefab root GameObject (fileID `7401264195953927370`, NOT `100100000` — that latter is the PrefabImporter and casts to a non-GameObject at runtime, which crashed `Instantiate<GameObject>` in the first iteration), `elements` = all 18 SOs in ascending-Z order, default layout values (cellSize 0.09 m, spacing 0.012 m). Wall total width with periods 1–3 ≈ 1.8 m, height ≈ 0.29 m.

  **TMP label sizing (gotcha):** TextMeshPro 3D `fontSize` is in *world units when the transform's scale is 1*, NOT points. Setting `fontSize = 4` produced 4-meter-tall letters that swamped the wall in v1. Current code uses `enableAutoSizing` with `fontSizeMin/Max` derived from `cellSize.y * labelHeightFactor` (default 0.6) so labels always fit inside the cell rect regardless of cell size tuning. Don't replace this with a fixed fontSize unless you also rescale labelGo manually.

**Pending Editor / verification steps:**
1. Open `Laboratory.unity` and confirm the **Periodic Table** root is visible in the Hierarchy with the inspector showing all 18 element refs, atomPrefab, and layout values populated.
2. (One-time per fresh project) **Window → TextMeshPro → Import TMP Essential Resources** so labels render. If this hasn't been done, the wall will still build and be interactable — just unlabeled — with a one-line console warning.
3. Press Play with the XR Device Simulator. Walk/teleport to the wall (it's 2.5 m in front of the rig's default spot). Aim the near-far interactor at one of the cubes; on select an atom of that element drops out of the SpawnAnchor (~25 cm in front of the wall, 40 cm below center). Grab + bond as usual.
4. Tune in Inspector if needed: `cellSize`/`cellSpacing` to resize; `spawnAnchorLocalOffset` to relocate the drop point; `labelFontSize` if labels look too small/large.

**Known limitations / future work:**
- No "trash" / despawn zone yet — atoms accumulate; user has to leave them lying around or restart the scene.
- No hover info card (atomic number, mass, common compounds) — labels show only the symbol.
- Periods 4+ not in the wall but `PeriodicTableUtils` maps Z=19..30 already; add the SOs (K, Ca, Sc … Zn) to the `elements` list to populate row 4.
- Noble gases (He, Ne, Ar) are spawnable but have valence 0 so they refuse all bonds — by design; user feedback may need a hint UI later.

### Phase 7 — Level system with two-stage molecular reactions (2026-05-21)

**Goal**: Build CO₂ etc. via a two-stage gameplay loop. Stage 1 = construct intermediate compounds from raw atoms (e.g. 2× CO + 1× O₂). Stage 2 = drop those built molecules into a reaction chamber to combine into the final product (`2CO + O₂ → 2CO₂`). Levels are chained; UI shows the recipe with live checkboxes.

**Done (code):**

- **`Assets/Scripts/Chemistry/MoleculeTag.cs`** (guid `b0b0b0b0b0b0b0b0b0b0b0b0b0b0b003`) — runtime marker placed on the canonical atom (lowest instance id) of a closed molecule that matches a `CompoundSO`. Holds `Compound`, `Owner`, and a `Broken` event.
- **`Assets/Scripts/Chemistry/MoleculeIdentifier.cs`** (guid `b0b0b0b0b0b0b0b0b0b0b0b0b0b0b004`) — scene singleton. Subscribes to `BondManager.BondFormed`; on each bond, runs `Molecule.BuildFrom(bond.A)` and if `IsClosed` matches a `CompoundSO` in the assigned `CompoundDatabase`, adds a `MoleculeTag` to the canonical atom. `LateUpdate` re-validates all active tags every frame (a bond breaking via `Bond.OnDestroy` reduces the snapshot below `IsClosed`, so the tag dissolves and the `MoleculeDissolved` event fires). Exposes `MoleculeFormed(CompoundSO, MoleculeTag)` and `MoleculeDissolved(CompoundSO, MoleculeTag)`.
- **`Assets/Scripts/Chemistry/ReactionRecipeSO.cs`** (guid `b0b0b0b0b0b0b0b0b0b0b0b0b0b0b001`) — `[CreateAssetMenu] "MolecularLab/Reaction Recipe"`. Fields: `displayName`, `inputs` (List<CompoundCount>), `outputs` (List<CompoundCount>), `effectPrefab`, `sfx`. `Matches(Dictionary<CompoundSO,int>)` is exact multiset equality. Distinct from `ReactionSO` (which still handles single-molecule formation on bond closure) — `ReactionRecipeSO` handles multi-molecule chamber combinations.
- **`Assets/Scripts/Chemistry/LevelSO.cs`** (guid `b0b0b0b0b0b0b0b0b0b0b0b0b0b0b002`) — `[CreateAssetMenu] "MolecularLab/Level"`. Fields: `title`, `instructions`, `stage1` (List<CompoundCount> — intermediates to build), `stage2` (`ReactionRecipeSO` — final reaction), `nextLevel` (LevelSO — chain).
- **`Assets/Scripts/Interaction/ReactionChamber.cs`** (guid `b0b0b0b0b0b0b0b0b0b0b0b0b0b0b005`) — `RequireComponent(Collider)` (set `isTrigger=true` in `Awake`). Tracks `Dictionary<CompoundSO,int>` of molecules currently inside via `OnTriggerEnter/Exit` (resolves `MoleculeTag` by walking the atom's connected component). Also subscribes to `MoleculeIdentifier.MoleculeFormed` so molecules **built inside the chamber** are also counted (no enter event fires for in-place bonding; we test `_trigger.ClosestPoint(p) == p` per atom). On every contents change calls `recipe.Matches`; on hit consumes inputs (destroys atom + bond GameObjects) and spawns outputs at `outputAnchor`. Each `CompoundSO` can declare a `productPrefab` (pre-bonded molecule prefab); fallback spawns loose atoms via `atomPrefab` + `SetElement`. Fires `RecipeReacted(ReactionRecipeSO)`. `SetRecipe(recipe, armed)` and `SetArmed(bool)` are driven by `LevelManager`.
- **`Assets/Scripts/Managers/LevelManager.cs`** (guid `b0b0b0b0b0b0b0b0b0b0b0b0b0b0b006`) — scene singleton. Holds `startingLevel`, references `LevelObjectiveUI`, `ReactionChamber`, `MoleculeIdentifier` (auto-resolved if null). Tracks `Dictionary<CompoundSO,int> _built` of Stage 1 intermediates as they're formed/dissolved (only counts compounds listed in the current level's `Stage1`). When all Stage 1 targets are met → `chamber.SetArmed(true)`. When chamber fires the Stage 2 recipe → 2.5 s celebration → `SetLevel(nextLevel)`. `SetLevel(null)` shows an "All levels complete" panel.
- **`Assets/Scripts/UI/LevelObjectiveUI.cs`** (guid `b0b0b0b0b0b0b0b0b0b0b0b0b0b0b007`) — world-space TMP-3D panel built procedurally (like `PeriodicTableWall`). Renders: title at top, one row per Stage 1 intermediate with checkbox + progress `(have/target)`, and the Stage 2 equation at the bottom (dimmed until Stage 1 complete, green when armed). `ShowCompletion(completedTitle, nextTitle)` swaps to a "Next" panel for the celebration window. **Falls back gracefully** with a console warning if TMP Essential Resources aren't imported (same pattern as `PeriodicTableWall`).
- **`Assets/Scripts/Data/CompoundSO.cs`** — added `[SerializeField] private GameObject productPrefab;` + `ProductPrefab` property. Used by `ReactionChamber.SpawnCompound`.

**Done (data assets):**

- **6 new `CompoundSO` assets** in `Assets/Scripts/Data/Compound Data/`:
  - `CO.asset` ({C:1, O:1}, guid `a0…a001`)
  - `O2.asset` ({O:2}, guid `a0…a002`)
  - `H2.asset` ({H:2}, guid `a0…a003`)
  - `N2.asset` ({N:2}, guid `a0…a004`)
  - `Cl2.asset` ({Cl:2}, guid `a0…a005`)
  - `NaCl.asset` ({Na:1, Cl:1}, guid `a0…a006`)
  - All added to `CompoundDatabase.asset` (along with the original 5: H₂O, CO₂, CH₄, NH₃, HCl)
- **`ReactionRecipeSO` assets** in `Assets/ScriptableObjects/Recipes/` (NOTE: filenames don't all match content — `Recipe_2CO_O2_to_2CO2.asset` was repurposed on 2026-05-22 and now holds the HCl recipe):
  - `Recipe_2CO_O2_to_2CO2.asset` (guid `c0…c001`): **actually `H2 + Cl2 → 2HCl`** (despite the filename) — used by Level 1
  - `Recipe_2H2_O2_to_2H2O.asset` (guid `c0…c002`): 2 H₂ + 1 O₂ → 2 H₂O
  - `Recipe_N2_3H2_to_2NH3.asset` (guid `c0…c003`): 1 N₂ + 3 H₂ → 2 NH₃
  - `Recipe_CO_3H2_to_CH4_H2O.asset` (guid `c0…c004`, added 2026-06-03): CO + 3 H₂ → CH₄ + H₂O (methanation)
  - `Recipe_CH4_2O2_to_CO2_2H2O.asset` (guid `c0…c005`, added 2026-06-03): CH₄ + 2 O₂ → CO₂ + 2 H₂O (combustion)
  - `Recipe_CO2_synthesis.asset` (guid `c0…c006`, added 2026-06-03): 2 CO + O₂ → 2 CO₂ (the real CO₂ recipe; `c001` no longer does this)
- **Shared reaction VFX** (2026-06-29): `Assets/Prefabs/VFX/VFX_ReactionBurst.prefab` is the common chamber reaction `effectPrefab`. It is tuned as a compact, dense, soft light-grey smoke burst (2 s lifetime, reduced emission radius/spread, higher particle density, turbulence/noise) using `Assets/Materials/M_ReactionBurst.mat` on URP `ParticlesUnlit`; it should heavily veil chamber molecules while active without filling the full chamber for too long.
- **`LevelSO` assets** in `Assets/ScriptableObjects/Levels/` — chain is `startingLevel` (Level01) → … → null. Filenames are misleading; trust the `title`/content:
  - `Level01_CO2.asset` (guid `c0…c011`) — **content = "Level 1 — Make HCl"** (H₂ + Cl₂ → 2HCl) → next = Level02
  - `Level02_H2O.asset` (guid `c0…c012`) — "Level 2 — Make H2O" → next = Level03 (link restored 2026-06-03; was null/broken, which is why only 2 levels were reachable)
  - `Level03_NH3.asset` (guid `c0…c013`) — "Level 3 — Make NH3" → next = Level04 (was orphaned; re-linked 2026-06-03)
  - `Level04_CO2.asset` (guid `c0…c014`, added 2026-06-03) — "Level 4 — Make CO2": Stage1 2×CO + 1×O₂ → Stage2 `2CO + O₂ → 2CO₂` → next = Level05
  - `Level05_CH4.asset` (guid `c0…c015`, added 2026-06-03) — "Level 5 — Make Methane": Stage1 1×CO + 3×H₂ → Stage2 `CO + 3H₂ → CH₄ + H₂O` → next = Level06
  - `Level06_CombustCH4.asset` (guid `c0…c016`, added 2026-06-03) — "Level 6 — Burn Methane": Stage1 1×CH₄ + 2×O₂ → Stage2 `CH₄ + 2O₂ → CO₂ + 2H₂O` → next = null (end of chain)
  - Full reachable chain: **HCl → H₂O → NH₃ → CO₂ → Methane → Burn Methane**. All Stage-2 equations are balanced. New levels need no Editor steps — they reuse existing compounds (all in `CompoundDatabase`) and `LevelManager` walks `nextLevel` automatically.

**Pending Editor steps (must be done in Unity — NOT YAML, per `feedback_unity_prefab_fileid.md`):**

1. **`Window → TextMeshPro → Import TMP Essential Resources`** — one-time per project, or the UI panel will skip text and log a warning.
2. **Add three GameObjects to `Laboratory.unity`:**
   - `LevelManager` (empty GO) → add `LevelManager` component **and** `MoleculeIdentifier` component. Assign `startingLevel = Level01_CO2.asset`. Drag `CompoundDatabase.asset` into the `MoleculeIdentifier.database` field. Leave the other fields (chamber, ui, identifier) null — they auto-resolve in `Start()`.
   - `Reaction Chamber` (Cylinder primitive, scale ~ `(0.25, 0.3, 0.25)`) — place at e.g. `(0, 1.0, 1.0)` (reachable in front of the rig). Remove default `CapsuleCollider` (or change to SphereCollider). Add a wider **SphereCollider** with radius ~0.2, `Is Trigger ✓`. Add `ReactionChamber` component. Optionally add a translucent URP/Lit material. Assign `atomPrefab` (the existing Atom prefab) for fallback output spawning.
   - `Level Objective UI` (empty GO) — place at world `(1.0, 1.6, 2.5)` rot `(0, 180, 0)` so it faces the user, sibling to the periodic table wall. Add `LevelObjectiveUI` component. Defaults (`panelSize 0.7×0.9`, `rowSize 0.035`, etc.) are tuned for that distance.
3. **Smoke test (per plan verification section):**
   - Press Play → UI panel shows "Level 1 — Make CO₂" with two unchecked rows.
   - Spawn C from the wall, then 2× O, bond them into CO. Console: `[MoleculeIdentifier] Formed CO`. UI: `✓ 1 × CO (1/2)`.
   - Build a second CO and one O₂. All rows checked, Stage 2 line turns green.
   - Drop all three molecules into the chamber. Chamber logs `REACT: 2CO + O₂ → 2CO₂`, all input atoms vanish, 2 CO₂ molecules spawn at the chamber center, UI shows completion banner, advances to Level 2 after 2.5 s.
   - Edge case: yank a CO molecule apart while inside the chamber. Tag dissolves → chamber decrement → recipe no longer matches.
4. **Optional (visual polish, not required for the loop):**
   - Author `CO2.prefab`, `H2O.prefab`, `NH3.prefab` as pre-bonded molecule prefabs and assign each to its `CompoundSO.productPrefab` field — gives proper-looking products instead of loose atoms.
   - Author sound clips or recipe-specific alternate VFX if desired; the default shared chamber reaction smoke is already assigned via `ReactionRecipeSO.effectPrefab`.

**Known limitations / future work:**
- Recipe matching is by **composition multiset only** — no structural isomers (linear vs branched). For organic molecules this would need a graph-isomorphism check.
- `MoleculeIdentifier.LateUpdate` re-runs `Molecule.BuildFrom` for every tag every frame — O(N tags × atoms). Fine for current scope (≤ ~10 tags); revisit if molecule count balloons.
- `ReactionChamber` "built inside" detection uses `Collider.ClosestPoint(p) == p` which is approximate; works for convex triggers (Sphere, Box) but is inexact for concave meshes — keep the trigger a Sphere or Box.
- No haptic feedback on Stage 1 completion or Stage 2 firing — easy to add via `SimpleHapticFeedback`.

**Molecule completeness model — CO fix (2026-06-03):**
- The original recognition gate required `Molecule.Snapshot.IsClosed` (every atom at `RemainingValence == 0`). This **cannot ever be satisfied by CO**: the bond model consumes equal order from both atoms, so a diatomic A–B only closes when `valence(A) == valence(B)`. Carbon (4) ≠ Oxygen (2), so CO always left carbon with dangling valence → never tagged → `ReactionChamber` rejected it ("not a valid ingredient"). This blocked Level 4 (Make CO₂), which needs CO as a Stage-1 intermediate.
- Fix, two coordinated parts:
  1. `BondManager.GetTargetBondOrder` now returns **2 for C–O / O–C** (joining the existing O–O→2, N–N→3 special cases). This is the max oxygen's valence allows → O saturates, the user sees a double bond, and CO₂ becomes the chemically-correct O=C=O.
  2. `Molecule.Snapshot` gained `OpenAtomCount` + `IsSaturated` (`OpenAtomCount <= 1`). `MoleculeIdentifier` (both initial tag at line ~59 and per-frame re-validation at ~114) and `MoleculeInfoUI` now gate on `IsSaturated` instead of `IsClosed`. A molecule is "complete" when no two of its atoms could bond further (≤1 atom retains free valence). Exact composition match against `CompoundDatabase` remains the strong guard, so only CO is newly admitted — verified safe across the whole compound set (no single-element compounds exist, so lone atoms never match).
- `ReactionSystem.cs` (the older single-molecule `ReactionSO` path) intentionally still uses strict `IsClosed` — no CO recipe lives there, and the chamber/level loop is the only consumer that needed the relaxed gate.

**Level/recipe revert — undo broken commit 95df322 (2026-06-17):**
- Commit `95df322` ("Adding more levels and fixing display and UI", by Tanas10) added Levels 6/8/9/10 (Rust, Carbonic Acid, SO₂, Glucose) + their compounds (Fe₂O₃, SO₂, H₂CO₃, C₆H₁₂O₆) and recipes, **but also broke the original first 5 levels and their chamber recipes**:
  - Each of Level01–05 had its multi-ingredient Stage 1 collapsed to a single final-product compound (e.g. L2 `2×H2 + 1×O2` → `1×H2O`), and instructions rewritten to match.
  - Recipes `c001`/`c002`/`c003`/`c004`/`c006` were rewritten into self-referential nonsense (inputs == outputs == the product, e.g. `HCl → 2HCl`), so the chamber could never react.
  - The `CO` compound (guid `…a001`) was **deleted** — its file was repurposed into `C6H12O6` (Glucose) with a new guid `a4ace27f…`. This orphaned Levels 4 & 5, which need CO.
- Revert (working tree only, not yet committed): restored Stage 1 + instructions of **Level01_HCl, Level02_H2O, Level03_NH3, Level04_CO2, Level05_CH4** to their pre-commit values, and restored recipes **Recipe_HCl/H2O/NH3/CH4/CO2** to correct chemistry (`H2+Cl2→2HCl`, `2H2+O2→2H2O`, `N2+3H2→2NH3`, `CO+3H2→CH4+H2O`, `2CO+O2→2CO2`). Recreated `Assets/Scripts/Data/Compound Data/CO.asset` (guid `…a001`, Carbon Monoxide) and re-added it to `CompoundDatabase.asset`.
- **Kept** the newly-added Levels 6/8/9/10 and their compounds/recipes (per request), and **kept** the new level titles + the new `nextLevel` chain wiring (Level05 → Rust → CombustCH4 → …) so the new levels stay reachable. **Level07_CombustCH4** (the renamed original Level 6) and recipe `c005` were already correct and left untouched.
- New-level chain fixed too: the commit left it cyclic (Level08 → Rust, Level10 Glucose → Level01). Repointed **Level08 → Level09** and set **Level10 → null (end)**. Full reachable chain is now linear: **HCl → H₂O → NH₃ → CO₂ → CH₄ → Rust → CombustCH₄ → Carbonic Acid → SO₂ → Glucose → end**.

**Molecule-guidance info button restyle (2026-06-17):** in `LevelManager.cs`, the procedurally-built info ("i") button now lives on its **own world-space canvas** (`MoleculeInfoButton`, field `_guidanceInfoButton`, sortingOrder 251) parented to `Camera.main`, **independent of the top-centre `MoleculeGuidancePrompt`**. `SpawnGuidanceInfoButton()` (no longer takes the prompt root) creates the canvas; `PositionGuidanceInfoButtonBottomRight(cam)` pins it to the **bottom-right corner of the view frustum** using the same `tan(FOV/2)·z` math as `PositionGuidancePromptTopCenter` (horizontal extent via `cam.aspect`), inset by half the button + `infoButtonPadding·scale`. Button is enlarged (`infoButtonSize` default now 170, up from the original 70), **circular** via Unity's built-in `UI/Skin/Knob.psd` sprite (cached in static `_circleSprite`, fetched with `Resources.GetBuiltinResource`), with a **half-transparent light-blue** fill (`infoButtonColor` alpha 0.5) and a bold black "i" whose font size scales with the button (`infoButtonSize.y * 0.5`). The `InfoPanel` itself is 1700×700, at `anchoredPosition.y = -120` (pivots from top) so it clears the top hint prompt; **two-column layout** — centred "Instructions" title (`panelTextSize` ×1.5) + centred `InfoWelcomeText` subtitle at top, then `InfoGameplayText` (left half) and `InfoControlsText` (right half) top-left aligned at ±¼ panel width, Close button bottom-centre. Text size hardcoded `const panelTextSize = 18f`, all child rects derived from `panelSize`; the old single `InfoPanelText` was split into the three constants and the shared `infoTextSize` field removed. Clicking still opens the existing `InfoPanel` (which remains parented to the prompt, centre of view). `infoButtonSize`/`infoButtonPadding`/`infoButtonColor` are `[SerializeField]`-tunable. Note: in VR the on-device FOV/aspect may differ from `cam.fieldOfView`, so the corner inset is approximate — tune `infoButtonPadding` if needed.

**Small molecule-building chamber (2026-06-17):**
- Added `Assets/Scripts/Interaction/SmallMoleculeChamber.cs` and a **Small Molecule Chamber** object to `Assets/Scenes/Laboratory - Updated.unity`, positioned between the periodic table and the big reaction chamber. It accepts loose single atoms only, stages them as non-interactable, auto-builds the next needed Stage 1 molecule in UI order, then re-enables the built molecule for placement in the big reaction chamber.
- `LevelManager` now owns both chambers: the small chamber target is computed from current Stage 1 progress in the big chamber, while UI progress still updates only after completed molecules are placed in the big chamber. Atom spawn/release guidance reuses the yellow prompt/arrow for atom → small chamber, then molecule → big chamber after auto-build.
- `AtomGrabSensor` routes released single atoms through the small chamber before free-space bonding. Wrong atoms dropped into the small chamber are rejected, returned to their grab-start position, and now show `Wrong Atom` on the top-center hint label with a red background for a short duration.
- Added a circular **Atom Spawn Platform** beside the small chamber; periodic-table buttons spawn atoms on its `AtomSpawnAnchor` while the small chamber has an active target. Runtime atom labels are handled by `AtomSymbolBillboard`, created from `Atom.SetElement`, so spawned atoms and chamber-built/output atoms show a floating camera-facing chemical symbol bubble.
- Added `AtomDeleteController` on the LevelManager scene object. Pressing the left controller grip deletes the atom targeted by the left controller ray/near hand after breaking its bonds; in the XR Interaction Simulator this is **Left Shift + G** (`G` = Grip, `Left Shift` = left device actions).
- Added `CameraZoomController` on the LevelManager scene object. Holding the **right controller secondary button / B button** smoothly zooms the main camera FOV to 28 and releasing restores the normal FOV; in the XR Interaction Simulator this is **2** (`Left Shift + 2` would target the left controller, so use plain `2` for right).
- `LevelManager` now shows a yellow top-center hint after the configured inactivity delay (currently 5 seconds in `Laboratory - Updated.unity`) without selecting the next correct atom for the small chamber, e.g. `Pick H atom`; it shares the same yellow background as the atom/molecule placement guidance and hides as soon as the correct periodic-table atom is spawned.
- On Laboratory scene start, `LevelManager` shows a light-blue top-center hint for 30 seconds: `Look at table on your right to see the equation`.
- `LevelManager` now adds an XR-clickable info icon on the top hint-popup canvas (`MoleculeGuidancePrompt`); clicking it opens the same gameplay/control instructions used by the Main Menu, with a Close button.
- Main Menu instructions and the in-lab info panel now share `LaboratoryInstructionContent`, so gameplay text and XR/PC controls stay identical; the Main Menu panel rebuilds at runtime into the same dark title/welcome/two-column design as the lab info panel. The controls column is mode-aware: active `MouseControlCamera` shows desktop mouse/keyboard controls, otherwise it shows headset-only XR controls without simulator shortcuts. In mouse-camera mode, Main Menu XRI button events require a precise camera ray hit before invoking the button, preventing broad desktop sphere-cast near misses from accidentally triggering Exit. The Main Menu instructions background also gets a runtime `BoxCollider` + `XRSimpleInteractable` blocker in front of menu buttons, while the Close button collider is placed slightly closer so it remains selectable. `LevelManager` also clamps XR simulator/player camera height to Y=0.6-2.4 so Q/E vertical movement cannot leave the lab height range.
- `PeriodicTableWall` now generates larger cells/text and a segmented curved backing `Panel`; `Laboratory - Updated.unity` enables the curve so the full periodic table wraps slightly around the player instead of being fully flat.

**Laboratory camera scene state (2026-06-30):**
- Main Menu `Enter Laboratory` now loads `Assets/Scenes/Laboratory - new models.unity`; `ProjectSettings/EditorBuildSettings.asset` enables `MainMenu.unity` and `Laboratory - new models.unity` for player builds.
- `Assets/Scenes/Laboratory - new models.unity` currently has the XR Origin prefab instance enabled and the standalone `MouseControlCamera` GameObject disabled. The inactive mouse camera remains available/tagged `MainCamera` for desktop testing, with `MouseControlCamera` (`moveSpeed=2.5`, `lookSensitivity=0.2`, `reachDistance=20`, `heldKeyboardMoveSpeed=2.2`, vertical clamp Y=0.6-2.4), a Camera, AudioListener, and URP additional camera data, positioned at `(0, 1.3, -2.24)` with identity rotation.
- `MouseControlCamera.cs` now creates a hidden runtime `XRRayInteractor` and an `XR Interaction Manager` if needed, then uses XRI manual selection instead of invoking select events directly. PC controls: locked-FPS mouse look, `WASD` move, `Space`/`C` vertical clamped to Y=0.6-2.4, `Left Shift` sprint when not holding anything, `Left Ctrl` precision, `LMB` interact/grab, mouse wheel held-object depth, `Shift+WASD` moves the held atom/molecule without moving the camera, `X` delete aimed atom, `Z` zoom.
- The desktop green crosshair is generated by `MouseControlCamera` as a `ScreenSpaceOverlay` canvas with very high sorting order, so it renders on top of all 3D scene objects instead of being hidden by nearby geometry.

**TMP fallback atlas update (2026-06-29):**
- `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset` is no longer the untouched 1×1 fallback atlas: Unity populated it as a 512×512 dynamic atlas with the `→` character (Unicode 8594). This supports equation/UI text that uses reaction arrows; avoid reverting it as trivial churn unless replacing the UI text with ASCII arrows.

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
