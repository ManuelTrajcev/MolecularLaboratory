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
- **VR SDK:** Unity XR Interaction Toolkit 3.3.1 + Oculus XR Plugin 4.5.4 + OpenXR 1.16.1
- **XR Plugin:** Oculus XR Plugin (Android) + OpenXR (Standalone/PCVR) — pure Unity XR stack, no Meta XR All-in-One SDK from Asset Store
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

**Phase 1 complete** (2026-04-30). Phase 2–3 partial. **Phase 4 in progress** (Laboratory scene setup). Git initialized with Unity-flavored `.gitignore` at project root; `.claude/settings.local.json` is git-ignored, `.claude/settings.json` + `.claude/hooks/` are committed.

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
- `Assets/XRI/` (XR Interaction Toolkit Starter Assets) imported

### Phase 4 — Laboratory scene (in progress, 2026-04-30)
Current scene contents (`Assets/Scenes/Laboratory.unity`):
- Directional Light at `(0, 3, 0)`, rotation `(50, -30, 0)`, intensity 2
- Global Volume with shared profile
- XR Rig prefab instance at `(1.916, 0, 1.916)` with `UniversalAdditionalCameraData` added

**Pending manual Editor steps** — see *Phase 4 — Laboratory Scene Manual Setup* checklist below.

**Next after Phase 4:** Atom/Bond/Reaction system implementation, ScriptableObject element definitions, MicroWorld scene, MainMenu scene.

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
