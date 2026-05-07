# TODO — Virtual Molecular Laboratory

Living list of work remaining. Group order = rough priority. Mark items `[x]` as they finish; move completed sections to a "Done" footer if it gets cluttered.

---

## Immediate (unblocks user testing)

- [ ] **Atom prefab finalized** — Sphere + Rigidbody + SphereCollider + XRGrabInteractable + `Atom` + `AtomGrabSensor`, using shared `M_Atom.mat`
- [ ] **`M_Atom.mat`: enable GPU Instancing** (currently OFF, `m_EnableInstancingVariants: 0`)
- [ ] **Bond prefab** — Cylinder, no collider, `Bond` component, light grey URP/Lit material with GPU Instancing
- [ ] **BondManager + ReactionSystem GameObject** in `Laboratory.unity`, wired to Bond prefab and Water/Salt reaction assets
- [ ] **Smoke test** end-to-end: 2H + O → log "Formed Water"; Na + Cl → log "Formed Sodium Chloride"

## Periodic Table UI (atom spawning in VR)

- [ ] World-space periodic table panel (TMP, readable at 0.5–1 m distance)
- [ ] One button per supported element (H, C, N, O, Na, Cl initially)
- [ ] Tap/poke interaction via XRI's poke interactor — spawns an Atom prefab in front of the panel with the chosen ElementSO assigned
- [ ] "Trash" zone — drop atoms into it to despawn (auto-destroys connected bonds via existing `breakDistance` logic)
- [ ] Element info card on hover (atomic number, mass, common compounds)
- [ ] Extend element library to ~20 common elements (add: He, Li, Be, B, F, Ne, Mg, Al, Si, P, S, K, Ca, Fe, Cu, Zn)

## Reaction & Molecule Polish

- [ ] **VFX**
  - [ ] Bond formation flash (small particle burst at midpoint)
  - [ ] Reaction completion particle: water = blue droplet sparkle, salt = white crystalline sparkle, generic = soft glow
  - [ ] Per-element subtle rim glow on the atom shader (URP Shader Graph)
- [ ] **SFX (spatial audio, AudioSource on atom/bond/molecule)**
  - [ ] Bond click on formation
  - [ ] Bond snap on break
  - [ ] Reaction chime per ReactionSO (water = soft drip, salt = crystal ting)
  - [ ] Ambient lab background loop (low volume)
- [ ] **Haptics** — `Simple Haptic Feedback` on:
  - [ ] Atom grab (light pulse)
  - [ ] Bond form (medium pulse, both controllers if user holds both atoms)
  - [ ] Reaction complete (longer pulse)
- [ ] **Bond order UI** — way to upgrade single→double→triple in VR (button or gesture)
- [ ] Visual distinction for bond order (1/2/3 cylinders side-by-side, or thicker)
- [ ] Add more reactions: CO₂, NH₃, CH₄, O₂, H₂, N₂, HCl
- [ ] Reaction "swap" mode — option in ReactionSO to replace constituent atoms with a stable molecule prefab on completion (vs. just visual feedback)
- [ ] **Structural validation** — currently only composition is checked. For isomers (organic chem), add graph-isomorphism matching to `ReactionSO`

## Scenes

- [ ] **MainMenu scene**
  - [ ] World-space main menu panel: Start, Tutorial, Settings, Quit
  - [ ] Async scene load with fade transition
  - [ ] Background ambient (microscope hum, soft sci-fi pad)
- [ ] **MicroWorld scene** — alternate "zoomed in" environment for free-form atom play (vs. structured Laboratory)
- [ ] **Tutorial flow** in Laboratory: scripted prompts walking the user through grab → bond → form water
- [ ] Scene manager singleton with async transitions and loading screen

## XR / Quest Polish

- [ ] Confirm Android XR loader (Oculus or OpenXR) is enabled in Editor — Project Settings → XR Plug-in Management → Android tab
- [ ] **Fixed Foveated Rendering** — verify FFR level High on Quest 2, Dynamic on Quest 3 (OpenXR Meta Quest feature group settings)
- [ ] **Stereo Rendering Mode** = Multiview / Single Pass Instanced
- [ ] **Application ID** — change `applicationIdentifier` from template default to `com.halicea.molecularlab`
- [ ] **Physics fixed timestep** — `0.01111` (1/90) for Quest 3 target
- [ ] Set GPU/CPU performance levels at startup (`Performance.TrySetPerformanceLevels` or OVRManager)
- [ ] Lightmap re-bake after final scene layout
- [ ] Profile draw calls (target < 100), confirm atoms batch via GPU instancing in Frame Debugger

## Lab Environment Art

- [ ] Workbench / table mesh (low-poly, baked lighting)
- [ ] Shelves / cabinets for visual context
- [ ] Periodic table wall poster (texture)
- [ ] Floor / wall / ceiling materials (currently flat grey plane)
- [ ] Skybox or window with outdoor view (optional ambience)
- [ ] Bake static lighting once final layout is set

## Code Quality / Architecture

- [ ] Move chemistry namespace into its own `MolecularLab.Chemistry.asmdef` for faster iteration compile times
- [ ] Same for `MolecularLab.Interaction.asmdef`
- [ ] Pool Bond prefabs (avoid `Instantiate` per bond on Quest)
- [ ] Pool Atom prefabs (when spawner / despawn UX is added)
- [ ] Replace `FindFirstObjectByType<BondManager>` fallback in `ReactionSystem.OnEnable` with explicit Inspector assignment (or DI bootstrap)
- [ ] Unit tests for `Molecule.BuildFrom` and `ReactionSO.Matches` (Unity Test Framework, edit mode)
- [ ] `IsClosed` reaction gating: dedupe so the same molecule doesn't re-trigger if a transient bond breaks/reforms within X seconds

## Accessibility / Comfort

- [ ] Snap turning on by default (already configured); add option for smooth turn
- [ ] Comfort vignette on locomotion (XRI Tunneling Vignette)
- [ ] Left-handed mode (swap dominant hand bindings)
- [ ] Subtitle / caption display for any voiceover or sound-cue-driven feedback
- [ ] Adjustable atom/UI scale for users uncomfortable with the default

## Build & Release

- [ ] Android build verified on physical Quest 2 (≥ 72 fps) and Quest 3 (≥ 90 fps)
- [ ] App icon + store presence (Meta Horizon Store metadata, screenshots, description)
- [ ] Privacy policy URL (required by Meta)
- [ ] Versioning + CI build script (optional)

## Documentation

- [ ] Short user-facing README section: "Controls in VR"
- [ ] Thesis writeup material: architecture diagram (Atom ↔ Bond ↔ Molecule ↔ Reaction), screenshots, performance numbers
- [ ] In-app credits / about panel
