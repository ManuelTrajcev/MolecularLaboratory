using System.Collections;
using System.Collections.Generic;
using MolecularLab.Chemistry;
using MolecularLab.Interaction;
using MolecularLab.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MolecularLab.Managers
{
    /// <summary>
    /// Orchestrates the two-stage level loop:
    ///   Stage 1: track which intermediate molecules have been built (via MoleculeIdentifier).
    ///   Stage 2: once Stage 1 targets are met, arm the ReactionChamber with the level's recipe.
    ///   On recipe react → advance to nextLevel.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [SerializeField] private LevelSO startingLevel;
        [SerializeField] private LevelObjectiveUI ui;
        [SerializeField] private ReactionChamber chamber;
        [SerializeField] private MoleculeIdentifier identifier;
        [SerializeField] private float completionDelay = 2.5f;
        [SerializeField] private bool debugLog = false;

        [Header("Guidance")]
        [SerializeField] private string moleculeReadyMessage = "Pick up the molecule and move it to the reaction chamber";
        [SerializeField] private Vector3 guidancePromptLocalPosition = new Vector3(0f, 0f, 1.1f);
        [SerializeField] private float guidancePromptScale = 0.0012f;
        [SerializeField] private Color guidancePromptColor = new Color(1f, 0.9f, 0.25f, 0.92f);
        [SerializeField] private Color guidanceArrowColor = new Color(1f, 0.85f, 0.05f, 1f);
        [SerializeField, Min(0.05f)] private float guidanceArrowOrbitRadius = 0.28f;
        [SerializeField, Min(0.05f)] private float guidanceArrowTopHeight = 0.45f;
        [SerializeField, Min(0.05f)] private float guidanceArrowMaxLength = 0.7f;

        private LevelSO _current;
        private readonly Dictionary<CompoundSO, int> _built = new Dictionary<CompoundSO, int>();
        private bool _stage1Complete;
        private bool _levelCompleted;
        private Coroutine _completionRoutine;
        private MoleculeTag _guidanceTag;
        private GameObject _guidancePrompt;
        private GameObject _guidanceArrow;
        private LineRenderer _guidanceArrowShaft;
        private Transform _guidanceArrowHead;
        private Material _guidanceArrowMaterial;

        public LevelSO CurrentLevel => _current;
        public IReadOnlyDictionary<CompoundSO, int> Built => _built;
        public bool Stage1Complete => _stage1Complete;

        private void Awake()
        {
            MigrateGuidanceDefaults();
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void Start()
        {
            if (identifier == null) identifier = MoleculeIdentifier.Instance ?? FindFirstObjectByType<MoleculeIdentifier>();
            if (chamber == null) chamber = FindFirstObjectByType<ReactionChamber>();
            if (ui == null) ui = FindFirstObjectByType<LevelObjectiveUI>();

            if (chamber != null)
            {
                chamber.RecipeReacted += OnRecipeReacted;
                chamber.ContentsChanged += OnChamberContentsChanged;
                chamber.MoleculeRejected += OnMoleculeRejected;
            }
            if (identifier != null)
                identifier.MoleculeFormed += OnMoleculeFormed;
            if (ui != null)
                ui.SetResetAction(ResetCurrentLevel);

            if (startingLevel != null) SetLevel(startingLevel);
        }

        private void OnDestroy()
        {
            if (chamber != null)
            {
                chamber.RecipeReacted -= OnRecipeReacted;
                chamber.ContentsChanged -= OnChamberContentsChanged;
                chamber.MoleculeRejected -= OnMoleculeRejected;
            }
            if (identifier != null)
                identifier.MoleculeFormed -= OnMoleculeFormed;
            if (Instance == this) Instance = null;
        }

        private void LateUpdate()
        {
            UpdateGuidanceArrow();
        }

        public void SetLevel(LevelSO level)
        {
            _current = level;
            _built.Clear();
            _stage1Complete = false;
            _levelCompleted = false;
            HideMoleculeGuidance();
            if (_completionRoutine != null)
            {
                StopCoroutine(_completionRoutine);
                _completionRoutine = null;
            }

            if (ui != null)
            {
                ui.SetResetAction(ResetCurrentLevel);
                ui.SetLevel(level, _built, _stage1Complete);
            }
            if (chamber != null) chamber.SetRecipe(level != null ? level.Stage2 : null, armed: false);
            if (debugLog) Debug.Log($"[Level] Set: {level?.Title}");
        }

        private bool IsLevelIntermediate(CompoundSO compound)
        {
            var stage1 = _current.Stage1;
            for (int i = 0; i < stage1.Count; i++)
                if (stage1[i].compound == compound) return true;
            return false;
        }

        private void Refresh()
        {
            if (_levelCompleted)
                return;

            bool wasComplete = _stage1Complete;
            _stage1Complete = Stage1Met();

            if (_stage1Complete != wasComplete && chamber != null)
                chamber.SetArmed(_stage1Complete);

            if (ui != null) ui.UpdateStage1(_built, _stage1Complete);
            if (debugLog) Debug.Log($"[Level] Stage1 complete={_stage1Complete}");
        }

        private void OnChamberContentsChanged(IReadOnlyDictionary<CompoundSO, int> contents)
        {
            if (_current == null || _levelCompleted) return;

            HideMoleculeGuidance();

            _built.Clear();
            if (contents != null)
            {
                foreach (var kv in contents)
                {
                    if (kv.Key != null && IsLevelIntermediate(kv.Key))
                        _built[kv.Key] = kv.Value;
                }
            }

            Refresh();
        }

        private bool Stage1Met()
        {
            var stage1 = _current.Stage1;
            for (int i = 0; i < stage1.Count; i++)
            {
                var s = stage1[i];
                if (s.compound == null) return false;
                _built.TryGetValue(s.compound, out var c);
                if (c < s.count) return false;
            }
            return true;
        }

        private void OnRecipeReacted(ReactionRecipeSO recipe)
        {
            if (_current == null || _current.Stage2 != recipe) return;
            _levelCompleted = true;
            if (debugLog) Debug.Log($"[Level] Completed: {_current.Title}");
            if (_completionRoutine != null)
                StopCoroutine(_completionRoutine);
            _completionRoutine = StartCoroutine(CompleteAndPromptNext());
        }

        private IEnumerator CompleteAndPromptNext()
        {
            yield return new WaitForSeconds(completionDelay);
            if (_current != null && _current.NextLevel != null)
            {
                if (ui != null) ui.ShowNextButton(AdvanceToNextLevel);
            }
            else
            {
                var endScreen = EndScreenController.Instance ?? FindFirstObjectByType<EndScreenController>();
                if (endScreen != null) endScreen.Show();
            }
            _completionRoutine = null;
        }

        private void AdvanceToNextLevel()
        {
            if (_current == null) return;
            ClearRuntimeChemistry();
            SetLevel(_current.NextLevel);
        }

        public void ResetCurrentLevel()
        {
            if (_current == null)
                return;

            ClearRuntimeChemistry();
            SetLevel(_current);
        }

        private void ClearRuntimeChemistry()
        {
            if (_completionRoutine != null)
            {
                StopCoroutine(_completionRoutine);
                _completionRoutine = null;
            }

            if (chamber != null)
                chamber.ClearAllContents();

            var bondObjects = FindObjectsByType<Bond>(FindObjectsSortMode.None);
            for (int i = 0; i < bondObjects.Length; i++)
            {
                if (bondObjects[i] != null)
                    Destroy(bondObjects[i].gameObject);
            }

            var atomRoots = new HashSet<GameObject>();
            var atoms = FindObjectsByType<Atom>(FindObjectsSortMode.None);
            for (int i = 0; i < atoms.Length; i++)
            {
                var atom = atoms[i];
                if (atom == null) continue;
                atomRoots.Add(atom.transform.root.gameObject);
            }

            foreach (var root in atomRoots)
            {
                if (root != null)
                    Destroy(root);
            }

            _built.Clear();
            _stage1Complete = false;
            _levelCompleted = false;
        }

        private void OnMoleculeRejected(string message)
        {
            if (ui != null)
                ui.ShowStatus(message, new Color(1f, 0.45f, 0.45f, 1f), 2.2f);
        }

        private void OnMoleculeFormed(CompoundSO compound, MoleculeTag tag)
        {
            if (_current == null || _levelCompleted || compound == null || tag == null)
                return;

            if (!IsStillNeededForCurrentLevel(compound))
                return;

            if (chamber != null && tag.Owner != null && chamber.IsAtomStaged(tag.Owner))
                return;

            ShowMoleculeGuidance(tag);
        }

        private bool IsStillNeededForCurrentLevel(CompoundSO compound)
        {
            if (_current == null || compound == null)
                return false;

            var stage1 = _current.Stage1;
            for (int i = 0; i < stage1.Count; i++)
            {
                var target = stage1[i];
                if (target.compound != compound)
                    continue;

                _built.TryGetValue(compound, out int placed);
                return placed < target.count;
            }

            return false;
        }

        private void ShowMoleculeGuidance(MoleculeTag tag)
        {
            _guidanceTag = tag;
            EnsureGuidancePrompt();
            EnsureGuidanceArrow();

            if (_guidancePrompt != null)
                _guidancePrompt.SetActive(true);

            if (_guidanceArrow != null)
                _guidanceArrow.SetActive(true);
        }

        private void HideMoleculeGuidance()
        {
            _guidanceTag = null;

            if (_guidancePrompt != null)
                _guidancePrompt.SetActive(false);

            if (_guidanceArrow != null)
                _guidanceArrow.SetActive(false);
        }

        private void EnsureGuidancePrompt()
        {
            if (_guidancePrompt != null)
                return;

            var cameraTransform = Camera.main != null ? Camera.main.transform : transform;
            _guidancePrompt = new GameObject("MoleculeGuidancePrompt", typeof(RectTransform), typeof(Canvas));
            _guidancePrompt.transform.SetParent(cameraTransform, false);
            _guidancePrompt.transform.localRotation = Quaternion.identity;
            _guidancePrompt.transform.localScale = Vector3.one * guidancePromptScale;

            var canvas = _guidancePrompt.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 250;

            var root = _guidancePrompt.GetComponent<RectTransform>();
            root.sizeDelta = new Vector2(760f, 110f);
            PositionGuidancePromptTopCenter(root);

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(_guidancePrompt.transform, false);
            var bgRt = background.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var image = background.GetComponent<Image>();
            image.color = guidancePromptColor;
            image.raycastTarget = false;

            var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(background.transform, false);
            var labelRt = label.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(28f, 12f);
            labelRt.offsetMax = new Vector2(-28f, -12f);

            var tmp = label.GetComponent<TextMeshProUGUI>();
            tmp.text = moleculeReadyMessage;
            tmp.color = Color.black;
            tmp.fontSize = 38f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }

        private void PositionGuidancePromptTopCenter(RectTransform promptRoot)
        {
            if (promptRoot == null)
                return;

            Camera cam = Camera.main;
            float z = Mathf.Max(0.1f, guidancePromptLocalPosition.z);
            if (cam == null)
            {
                _guidancePrompt.transform.localPosition = new Vector3(0f, guidancePromptLocalPosition.y, z);
                return;
            }

            float topY = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * z;
            float halfPromptHeight = promptRoot.sizeDelta.y * guidancePromptScale * 0.5f;
            _guidancePrompt.transform.localPosition = new Vector3(0f, topY - halfPromptHeight, z);
        }

        private void EnsureGuidanceArrow()
        {
            if (_guidanceArrow != null)
                return;

            _guidanceArrowMaterial = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard"));
            _guidanceArrowMaterial.color = guidanceArrowColor;

            _guidanceArrow = new GameObject("MoleculeToChamberArrow");

            var shaft = new GameObject("ArrowShaft", typeof(LineRenderer));
            shaft.name = "ArrowShaft";
            shaft.transform.SetParent(_guidanceArrow.transform, false);
            _guidanceArrowShaft = shaft.GetComponent<LineRenderer>();
            _guidanceArrowShaft.sharedMaterial = _guidanceArrowMaterial;
            _guidanceArrowShaft.useWorldSpace = false;
            _guidanceArrowShaft.positionCount = 2;
            _guidanceArrowShaft.widthMultiplier = 0.025f;
            _guidanceArrowShaft.numCapVertices = 6;
            _guidanceArrowShaft.colorGradient = CreateArrowShaftGradient();

            var head = new GameObject("ArrowHead", typeof(MeshFilter), typeof(MeshRenderer));
            head.transform.SetParent(_guidanceArrow.transform, false);
            head.GetComponent<MeshFilter>().sharedMesh = CreateConeMesh(0.04f, 0.1f, 24);
            head.GetComponent<MeshRenderer>().sharedMaterial = _guidanceArrowMaterial;
            _guidanceArrowHead = head.transform;
        }

        private void UpdateGuidanceArrow()
        {
            if (_guidanceTag == null || _guidanceTag.Owner == null || chamber == null)
            {
                HideMoleculeGuidance();
                return;
            }

            if (chamber.IsAtomStaged(_guidanceTag.Owner))
            {
                HideMoleculeGuidance();
                return;
            }

            EnsureGuidanceArrow();

            Vector3 center = GetMoleculeCenter(_guidanceTag.Owner);
            bool isHeld = IsMoleculeBeingHeld(_guidanceTag.Owner);
            Vector3 start;
            Vector3 end;

            if (isHeld)
            {
                Vector3 toChamber = chamber.transform.position - center;
                toChamber.y = 0f;
                if (toChamber.sqrMagnitude < 0.0001f)
                    toChamber = transform.forward;
                else
                    toChamber.Normalize();

                start = center;
                end = chamber.transform.position + Vector3.up * 0.18f;
            }
            else
            {
                start = center + Vector3.up * guidanceArrowTopHeight;
                end = center + Vector3.up * 0.08f;
            }

            Vector3 direction = end - start;
            float distance = direction.magnitude;
            if (distance < 0.05f)
            {
                HideMoleculeGuidance();
                return;
            }

            direction /= distance;
            float arrowLength = Mathf.Clamp(distance * 0.65f, 0.18f, guidanceArrowMaxLength);
            float headLength = 0.1f;
            float shaftLength = Mathf.Max(0.08f, arrowLength - headLength);

            _guidanceArrow.transform.position = start;
            _guidanceArrow.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);

            if (_guidanceArrowShaft != null)
            {
                _guidanceArrowShaft.transform.localPosition = Vector3.zero;
                _guidanceArrowShaft.transform.localRotation = Quaternion.identity;
                _guidanceArrowShaft.transform.localScale = Vector3.one;
                _guidanceArrowShaft.SetPosition(0, Vector3.zero);
                _guidanceArrowShaft.SetPosition(1, new Vector3(0f, shaftLength, 0f));
            }

            if (_guidanceArrowHead != null)
            {
                _guidanceArrowHead.localPosition = new Vector3(0f, shaftLength + headLength * 0.5f, 0f);
                _guidanceArrowHead.localRotation = Quaternion.identity;
                _guidanceArrowHead.localScale = Vector3.one;
            }
        }

        private static bool IsMoleculeBeingHeld(Atom seed)
        {
            if (seed == null)
                return false;

            var snap = Molecule.BuildFrom(seed);
            for (int i = 0; i < snap.Atoms.Count; i++)
            {
                var atom = snap.Atoms[i];
                if (atom == null)
                    continue;

                var sensor = atom.GetComponent<AtomGrabSensor>();
                if (sensor != null && sensor.IsDraggingWholeMolecule)
                    return true;

                var grab = atom.GetComponent<XRGrabInteractable>();
                if (grab != null && grab.isSelected)
                    return true;
            }

            return false;
        }

        private Gradient CreateArrowShaftGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(guidanceArrowColor, 0f),
                    new GradientColorKey(guidanceArrowColor, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0.08f, 0f),
                    new GradientAlphaKey(0.35f, 0.45f),
                    new GradientAlphaKey(guidanceArrowColor.a, 1f),
                });
            return gradient;
        }

        private static Vector3 GetMoleculeCenter(Atom seed)
        {
            var snap = Molecule.BuildFrom(seed);
            if (snap.Atoms.Count == 0)
                return seed.transform.position;

            Vector3 center = Vector3.zero;
            int count = 0;
            for (int i = 0; i < snap.Atoms.Count; i++)
            {
                var atom = snap.Atoms[i];
                if (atom == null)
                    continue;

                center += atom.transform.position;
                count++;
            }

            return count > 0 ? center / count : seed.transform.position;
        }

        private static Mesh CreateConeMesh(float radius, float height, int segments)
        {
            var mesh = new Mesh { name = "GuidanceArrowCone" };
            var vertices = new Vector3[segments + 2];
            var triangles = new int[segments * 6];

            vertices[0] = new Vector3(0f, height * 0.5f, 0f);
            vertices[1] = new Vector3(0f, -height * 0.5f, 0f);

            for (int i = 0; i < segments; i++)
            {
                float angle = Mathf.PI * 2f * i / segments;
                vertices[i + 2] = new Vector3(Mathf.Cos(angle) * radius, -height * 0.5f, Mathf.Sin(angle) * radius);
            }

            int t = 0;
            for (int i = 0; i < segments; i++)
            {
                int current = i + 2;
                int next = i == segments - 1 ? 2 : current + 1;

                triangles[t++] = 0;
                triangles[t++] = current;
                triangles[t++] = next;

                triangles[t++] = 1;
                triangles[t++] = next;
                triangles[t++] = current;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            return mesh;
        }

        private void MigrateGuidanceDefaults()
        {
            if (guidancePromptLocalPosition == new Vector3(0.42f, 0.32f, 1.1f))
                guidancePromptLocalPosition = new Vector3(0f, 0f, 1.1f);
        }
    }
}
