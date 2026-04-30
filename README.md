# Virtual Laboratory for Material Structure at the Molecular Level

A VR application for Meta Quest in which users enter the microworld of materials and interact with their structure at the atomic and molecular level — observing atoms, manipulating molecules, forming and breaking bonds, and triggering chemical reactions.

- **Engine:** Unity `6000.3.9f1` (URP-blank template, URP `17.3.0`)
- **Target devices:** Meta Quest 2, Quest 3, Quest Pro (standalone Android build)
- **Secondary platform:** Quest Link / Air Link (PCVR) for development
- **VR stack:** Unity XR Interaction Toolkit `3.3.1` + Oculus XR Plugin `4.5.4` + OpenXR `1.16.1`

## Prerequisites

Install before cloning:

| Tool | Version | Notes |
|------|---------|-------|
| [Unity Hub](https://unity.com/download) | latest | |
| Unity Editor | **6000.3.9f1** | Install via Unity Hub. Add modules: **Android Build Support** (with Android SDK & NDK Tools, OpenJDK) |
| [git](https://git-scm.com/) | 2.30+ | |
| [git-lfs](https://git-lfs.com/) | 3.x | **Required** — binary assets (textures, audio, models, fonts) live in LFS |
| [Meta Quest Developer Hub](https://developer.oculus.com/meta-quest-developer-hub/) | latest | For deploying to device, log capture, and Air Link |

On macOS:

```bash
brew install git git-lfs
git lfs install
```

On Windows: install the official installers, then run `git lfs install` once in any terminal.

## Getting the project

```bash
git clone <repo-url>
cd "Molecular Laboratory"
git lfs pull           # downloads binary assets — required even if clone reports success
```

> If `git lfs pull` fails with "filter not found", you forgot the one-time `git lfs install`.

## Opening in Unity

1. Open **Unity Hub** → **Add** → select the cloned project root.
2. Hub will prompt to install **Unity 6000.3.9f1** if you don't have it. Install it (with Android Build Support + Android SDK/NDK + OpenJDK).
3. Open the project. First import takes 5–15 minutes — Unity is rebuilding the `Library/` cache (which is git-ignored).
4. Open `Assets/Scenes/Laboratory.unity`.

## VR setup (one-time, in Unity Editor)

The XR build target needs the Android Oculus loader enabled. This setting is per-machine and not committed:

1. **Edit → Project Settings → XR Plug-in Management → Android tab** → check **Oculus**.
2. **Standalone tab** → confirm **OpenXR** is checked (used for PCVR / Quest Link development).

Full Laboratory-scene wiring (XR Interaction Manager, Event System, floor, test grab cube, lighting, URP per-platform renderer) is documented in **`CLAUDE.md`** under *Phase 4 — Laboratory Scene Manual Setup*. Re-run that checklist after a fresh clone.

## Running on Quest

### Quest Link / Air Link (fastest dev loop)

1. Connect Quest via USB-C (Link cable) or pair via Air Link.
2. Put on the headset → enable **Quest Link** from the Universal Menu.
3. In Unity, press **Play**. The scene runs in-headset via Link with full Editor debugging.

### Standalone Android build (deploy to device)

1. **File → Build Profiles** → switch active platform to **Android**.
2. Connect Quest via USB-C, put it on, accept the **Allow USB Debugging** dialog (one time per machine; check **Always allow**).
3. **Build And Run**. First build takes 5–10 minutes. Subsequent builds ~1–2 min.
4. App launches on-device under **Apps → Unknown Sources** in the Quest library.

### Without USB

Use **Meta Quest Developer Hub → Apps → Add Build** to drag-and-drop the `.apk` over Wi-Fi.

## Performance targets

- Quest 2: 72 FPS minimum
- Quest 3: 90 FPS minimum
- Draw calls: < 100 per frame
- Single realtime directional light; bake everything else
- Mobile-friendly URP shaders only

See `CLAUDE.md` → *Oculus / Quest Optimization Settings* for the full tuning checklist.

## Project layout

```
Assets/
├── Scenes/              # MainMenu, Laboratory, MicroWorld
├── Prefabs/             # Atoms, Molecules, Lab equipment, UI
├── Scripts/
│   ├── Chemistry/       # Atom, Bond, Molecule, ReactionSystem
│   ├── Interaction/     # XR grab, bond formation logic
│   ├── Managers/        # Scene, audio, game state
│   └── UI/              # Menu and in-world UI
├── Materials/           # URP materials (CPK atom coloring)
├── Shaders/             # Custom shaders (atom glow, bond visuals)
├── Audio/               # SFX, ambient, haptic patterns
├── Models/              # 3D assets
├── ScriptableObjects/   # Element & Reaction definitions
└── Settings/            # XR, Input, URP render assets
```

## Coding conventions

- C# naming: `PascalCase` for classes/methods, `camelCase` for fields, `_camelCase` for private
- Prefer `[SerializeField] private` over `public` for inspector exposure
- Prefer ScriptableObjects for data (elements, reactions, settings)
- Namespace all scripts: `MolecularLab.Chemistry`, `MolecularLab.Interaction`, etc.

## Setting up Unity YAML merge (optional, per-machine)

`.gitattributes` declares `merge=unityyamlmerge` for Unity scene/prefab/asset files, but git only honors that if you've registered the driver. Without it, git falls back to a default 3-way merge — fine for small files, but can corrupt large scenes. Configure once per machine:

**macOS** — locate the tool inside your Unity install:

```bash
# Adjust the version segment to match your installed Editor
TOOL="/Applications/Unity/Hub/Editor/6000.3.9f1/Unity.app/Contents/Tools/UnityYAMLMerge"

git config --global merge.unityyamlmerge.name "Unity SmartMerge"
git config --global merge.unityyamlmerge.driver "$TOOL merge -p %O %B %A %A"
git config --global merge.unityyamlmerge.recursive binary
```

**Windows (PowerShell):**

```powershell
$Tool = "C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Data\Tools\UnityYAMLMerge.exe"
git config --global merge.unityyamlmerge.name "Unity SmartMerge"
git config --global merge.unityyamlmerge.driver "`"$Tool`" merge -p %O %B %A %A"
git config --global merge.unityyamlmerge.recursive binary
```

**Linux:**

```bash
TOOL="$HOME/Unity/Hub/Editor/6000.3.9f1/Editor/Data/Tools/UnityYAMLMerge"
git config --global merge.unityyamlmerge.name "Unity SmartMerge"
git config --global merge.unityyamlmerge.driver "$TOOL merge -p %O %B %A %A"
git config --global merge.unityyamlmerge.recursive binary
```

Verify it took:

```bash
git config --global --get merge.unityyamlmerge.driver
```

Now merging branches that touch the same scene falls back to Unity's structural YAML merge first, which understands GameObject hierarchy and component overrides. If that fails (genuinely conflicting changes), you'll be dropped into a normal text conflict to resolve manually.

## Troubleshooting

**"This project requires Unity 6000.3.9f1"** — install the exact version via Unity Hub. Other 6000.x versions may upgrade the project format and break for collaborators.

**Missing binary assets / pink materials** — you skipped `git lfs pull`. Run it now.

**Build target won't switch to Android** — install Android Build Support module via Unity Hub → Installs → ⋮ → Add Modules.

**Quest doesn't appear in adb / Build And Run** — enable Developer Mode for your headset in the Meta Quest mobile app, plug in via USB-C, accept the on-device debugging prompt.

**Long Editor freeze on first open** — normal. Unity is rebuilding the `Library/` cache (~2 GB). Don't kill the process.

## Contributing

This project uses Claude Code conventions:

- `CLAUDE.md` is the source of truth for project state, architecture, and Editor checklists. Keep it current when you change scenes/scripts/prefabs.
- `.claude/settings.json` (committed) wires a Stop hook that reminds Claude when `CLAUDE.md` is stale relative to `Assets/`.
- `.claude/settings.local.json` is git-ignored — your personal Claude permissions stay local.
