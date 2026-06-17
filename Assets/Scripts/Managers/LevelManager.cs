using System.Collections;
using System.Collections.Generic;
using MolecularLab.Chemistry;
using MolecularLab.Interaction;
using MolecularLab.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
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
        [SerializeField] private SmallMoleculeChamber smallChamber;
        [SerializeField] private MoleculeIdentifier identifier;
        [SerializeField] private float completionDelay = 0.25f;
        [SerializeField] private bool debugLog = false;

        [Header("Guidance")]
        [SerializeField] private string moleculeReadyMessage = "Pick up the molecule and move it to the reaction chamber";
        [SerializeField] private string atomReadyMessage = "Pick up the atom and move it to the small chamber";
        [SerializeField] private Vector3 guidancePromptLocalPosition = new Vector3(0f, 0f, 1.1f);
        [SerializeField] private float guidancePromptScale = 0.0012f;
        [SerializeField] private Color guidancePromptColor = new Color(1f, 0.9f, 0.25f, 0.92f);
        [SerializeField, Min(1f)] private float guidancePromptFontSize = 38f;
        [SerializeField] private Color atomPickHintPromptColor = new Color(1f, 0.58f, 0.58f, 0.92f);
        [SerializeField, Min(1f)] private float atomPickHintPromptFontSize = 48f;
        [SerializeField, Min(1f)] private float atomPickHintDelay = 30f;
        [SerializeField] private string atomPickHintMessageFormat = "Pick {0} atom";
        [SerializeField] private string startHintMessage = "Look at table on your right to see the equation";
        [SerializeField] private Color startHintPromptColor = new Color(0.58f, 0.82f, 1f, 0.92f);
        [SerializeField, Min(1f)] private float startHintDuration = 30f;
        [SerializeField] private Color guidanceArrowColor = new Color(1f, 0.85f, 0.05f, 1f);
        [SerializeField, Min(0.05f)] private float guidanceArrowOrbitRadius = 0.28f;
        [SerializeField, Min(0.05f)] private float guidanceArrowTopHeight = 0.45f;
        [SerializeField, Min(0.05f)] private float guidanceArrowMaxLength = 0.7f;
        [SerializeField] private Vector2 infoButtonSize = new Vector2(250f, 250f);
        [SerializeField, Min(0f)] private float infoButtonPadding = 18f;
        [SerializeField] private Color infoButtonColor = new Color(0.58f, 0.82f, 1f, 0.5f);
        [SerializeField] private Color infoPanelColor = new Color(0.06f, 0.08f, 0.12f, 0.94f);
        [SerializeField] private Color infoTextColor = Color.white;

        private LevelSO _current;
        private readonly Dictionary<CompoundSO, int> _built = new Dictionary<CompoundSO, int>();
        private bool _stage1Complete;
        private bool _levelCompleted;
        private Coroutine _completionRoutine;
        private Coroutine _startHintRoutine;
        private GuidanceMode _guidanceMode = GuidanceMode.None;
        private MoleculeTag _guidanceTag;
        private Atom _guidanceAtom;
        private GameObject _guidancePrompt;
        private GameObject _guidanceInfoButton;
        private RectTransform _guidanceMessageRoot;
        private Image _guidancePromptImage;
        private TextMeshProUGUI _guidanceLabel;
        private RectTransform _guidanceInfoPanelRoot;
        private GameObject _guidanceArrow;
        private LineRenderer _guidanceArrowShaft;
        private Transform _guidanceArrowHead;
        private Material _guidanceArrowMaterial;
        private ElementSO _lastNeededHintElement;
        private float _lastCorrectAtomSelectionTime;

        public LevelSO CurrentLevel => _current;
        public IReadOnlyDictionary<CompoundSO, int> Built => _built;
        public bool Stage1Complete => _stage1Complete;

        private const string InfoWelcomeText = "Welcome to the Molecular Laboratory";

        private const string InfoGameplayText =
            "Gameplay\n\n"
            + "- Look at the table on your right for the equation\n"
            + "- Pick required atoms from the Periodic Table\n"
            + "- Drop single atoms into the Small Chamber\n"
            + "- The Small Chamber builds the needed molecule\n"
            + "- Move completed molecules to the Big Chamber\n"
            + "- Placed molecules become locked and non-interactable\n"
            + "- Wrong atoms/molecules are rejected\n"
            + "- Hints guide you after inactivity and on level start\n"
            + "- Reset clears the current level; Next/Retry appears after reactions";

        private const string InfoControlsText =
            "Controls\n\n"
            + "- Right controller: grab/select atoms, molecules, and buttons\n"
            + "- Simulator: G = right grab/select\n"
            + "- Left controller grip deletes a targeted atom\n"
            + "- Simulator delete: Left Shift + G\n"
            + "- Hold right B button to zoom\n"
            + "- Simulator zoom: 2";

        private enum GuidanceMode
        {
            None,
            StartHint,
            AtomPickHint,
            AtomToSmallChamber,
            MoleculeToBigChamber,
        }

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
            if (smallChamber == null) smallChamber = FindFirstObjectByType<SmallMoleculeChamber>();
            if (ui == null) ui = FindFirstObjectByType<LevelObjectiveUI>();

            if (chamber != null)
            {
                chamber.RecipeReacted += OnRecipeReacted;
                chamber.ContentsChanged += OnChamberContentsChanged;
                chamber.MoleculeRejected += OnMoleculeRejected;
            }
            if (identifier != null)
                identifier.MoleculeFormed += OnMoleculeFormed;
            if (smallChamber != null)
            {
                smallChamber.MoleculeBuilt += OnSmallChamberMoleculeBuilt;
                smallChamber.AtomRejected += OnSmallChamberAtomRejected;
            }
            if (ui != null)
                ui.SetResetAction(ResetCurrentLevel);

            if (startingLevel != null) SetLevel(startingLevel);
            ShowStartHint();
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
            if (smallChamber != null)
            {
                smallChamber.MoleculeBuilt -= OnSmallChamberMoleculeBuilt;
                smallChamber.AtomRejected -= OnSmallChamberAtomRejected;
            }
            if (Instance == this) Instance = null;
        }

        private void LateUpdate()
        {
            if (Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame)
                AdvanceToNextLevelForTesting();

            UpdateAtomPickHintTimer();
            UpdateGuidanceArrow();
        }

        public void SetLevel(LevelSO level)
        {
            _current = level;
            _built.Clear();
            _stage1Complete = false;
            _levelCompleted = false;
            RestartAtomPickHintTimer();
            HideMoleculeGuidance();
            if (_completionRoutine != null)
            {
                StopCoroutine(_completionRoutine);
                _completionRoutine = null;
            }
            if (_startHintRoutine != null)
            {
                StopCoroutine(_startHintRoutine);
                _startHintRoutine = null;
            }

            if (ui != null)
            {
                ui.SetResetAction(ResetCurrentLevel);
                ui.SetLevel(level, _built, _stage1Complete);
            }
            if (chamber != null) chamber.SetRecipe(level != null ? level.Stage2 : null, armed: false);
            UpdateSmallChamberTarget();
            if (debugLog) Debug.Log($"[Level] Set: {level?.Title}");
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
            UpdateSmallChamberTarget();
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
                    var target = FindStage1Compound(kv.Key);
                    if (target == null)
                        continue;

                    _built.TryGetValue(target, out int current);
                    _built[target] = current + kv.Value;
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
                if (ui != null) ui.ShowAllLevelsCompleted(RetryAllLevels);
            }
            _completionRoutine = null;
        }

        private void AdvanceToNextLevel()
        {
            if (_current == null) return;
            ClearRuntimeChemistry();
            SetLevel(_current.NextLevel);
        }

        private void AdvanceToNextLevelForTesting()
        {
            if (_current == null || _current.NextLevel == null)
                return;

            ClearRuntimeChemistry();
            SetLevel(_current.NextLevel);
        }

        private void RetryAllLevels()
        {
            ClearRuntimeChemistry();
            SetLevel(startingLevel);
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
            if (smallChamber != null)
                smallChamber.ClearAllContents();

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
            RestartAtomPickHintTimer();
            HideMoleculeGuidance();
        }

        private void OnMoleculeRejected(string message)
        {
            if (ui != null)
                ui.ShowStatus(message, new Color(1f, 0.45f, 0.45f, 1f), 2.2f);
        }

        private void OnSmallChamberAtomRejected(string message, Atom atom)
        {
            if (ui != null)
                ui.ShowStatus(message, new Color(1f, 0.45f, 0.45f, 1f), 2.2f);
        }

        private void OnSmallChamberMoleculeBuilt(CompoundSO compound, MoleculeTag tag)
        {
            if (_current == null || _levelCompleted || compound == null || tag == null)
                return;

            if (IsStillNeededForCurrentLevel(compound))
                ShowMoleculeGuidance(tag);
        }

        public void OnAtomSpawned(Atom atom)
        {
            if (atom == null || atom.Element == null || smallChamber == null || _levelCompleted)
                return;

            if (smallChamber.IsElementNeeded(atom.Element))
            {
                RestartAtomPickHintTimer(atom.Element);
                ShowAtomGuidance(atom);
            }
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
                if (!AreEquivalent(target.compound, compound))
                    continue;

                _built.TryGetValue(target.compound, out int placed);
                return placed < target.count;
            }

            return false;
        }

        private CompoundSO FindStage1Compound(CompoundSO compound)
        {
            if (_current == null || compound == null)
                return null;

            var stage1 = _current.Stage1;
            for (int i = 0; i < stage1.Count; i++)
            {
                var target = stage1[i].compound;
                if (AreEquivalent(target, compound))
                    return target;
            }

            return null;
        }

        private void UpdateSmallChamberTarget()
        {
            if (smallChamber == null)
                return;

            ElementSO previousNeeded = smallChamber.GetNextNeededElement();
            smallChamber.SetCurrentLevel(_current, _built);
            ElementSO currentNeeded = smallChamber.GetNextNeededElement();
            if (previousNeeded != currentNeeded)
                RestartAtomPickHintTimer(currentNeeded);
        }

        private static bool AreEquivalent(CompoundSO a, CompoundSO b)
        {
            if (a == null || b == null)
                return false;

            if (ReferenceEquals(a, b))
                return true;

            return !string.IsNullOrWhiteSpace(a.Formula)
                && string.Equals(a.Formula, b.Formula, System.StringComparison.OrdinalIgnoreCase);
        }

        private void ShowMoleculeGuidance(MoleculeTag tag)
        {
            _guidanceMode = GuidanceMode.MoleculeToBigChamber;
            _guidanceTag = tag;
            _guidanceAtom = null;
            EnsureGuidancePrompt();
            EnsureGuidanceArrow();
            SetGuidancePrompt(moleculeReadyMessage, guidancePromptColor, guidancePromptFontSize);

            if (_guidanceMessageRoot != null)
                _guidanceMessageRoot.gameObject.SetActive(true);

            if (_guidanceArrow != null)
                _guidanceArrow.SetActive(true);
        }

        private void ShowAtomGuidance(Atom atom)
        {
            _guidanceMode = GuidanceMode.AtomToSmallChamber;
            _guidanceAtom = atom;
            _guidanceTag = null;
            EnsureGuidancePrompt();
            EnsureGuidanceArrow();
            SetGuidancePrompt(atomReadyMessage, guidancePromptColor, guidancePromptFontSize);

            if (_guidanceMessageRoot != null)
                _guidanceMessageRoot.gameObject.SetActive(true);

            if (_guidanceArrow != null)
                _guidanceArrow.SetActive(true);
        }

        private void HideMoleculeGuidance()
        {
            _guidanceMode = GuidanceMode.None;
            _guidanceTag = null;
            _guidanceAtom = null;

            if (_guidanceMessageRoot != null)
                _guidanceMessageRoot.gameObject.SetActive(false);

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

            var messageRoot = new GameObject("Message", typeof(RectTransform));
            messageRoot.transform.SetParent(_guidancePrompt.transform, false);
            _guidanceMessageRoot = messageRoot.GetComponent<RectTransform>();
            _guidanceMessageRoot.anchorMin = Vector2.zero;
            _guidanceMessageRoot.anchorMax = Vector2.one;
            _guidanceMessageRoot.offsetMin = Vector2.zero;
            _guidanceMessageRoot.offsetMax = Vector2.zero;

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(_guidanceMessageRoot, false);
            var bgRt = background.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var image = background.GetComponent<Image>();
            _guidancePromptImage = image;
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
            _guidanceLabel = tmp;
            tmp.text = moleculeReadyMessage;
            tmp.color = Color.black;
            tmp.fontSize = guidancePromptFontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            SpawnGuidanceInfoButton();
        }

        private void SetGuidancePrompt(string message, Color backgroundColor, float fontSize)
        {
            if (_guidanceLabel != null)
            {
                _guidanceLabel.text = message;
                _guidanceLabel.fontSize = fontSize;
            }

            if (_guidancePromptImage != null)
                _guidancePromptImage.color = backgroundColor;

            if (_guidanceMessageRoot != null)
                _guidanceMessageRoot.gameObject.SetActive(true);
        }

        private void SpawnGuidanceInfoButton()
        {
            if (_guidanceInfoButton != null)
                return;

            Camera cam = Camera.main;
            Transform parent = cam != null ? cam.transform : transform;

            // Dedicated world-space canvas so the button can live in a display corner,
            // independent of the top-centre guidance prompt.
            _guidanceInfoButton = new GameObject("MoleculeInfoButton", typeof(RectTransform), typeof(Canvas));
            _guidanceInfoButton.transform.SetParent(parent, false);
            _guidanceInfoButton.transform.localRotation = Quaternion.identity;
            _guidanceInfoButton.transform.localScale = Vector3.one * guidancePromptScale;

            var canvas = _guidanceInfoButton.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = cam;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 251;

            var canvasRt = _guidanceInfoButton.GetComponent<RectTransform>();
            canvasRt.sizeDelta = infoButtonSize;

            var button = SpawnGuidanceButton("InfoButton", "i", canvasRt, Vector2.zero, infoButtonSize,
                ShowGuidanceInfoPanel, infoButtonColor, infoButtonSize.y * 0.5f);
            if (button != null)
            {
                // Circular, semi-transparent background.
                var image = button.GetComponent<Image>();
                if (image != null)
                    image.sprite = GetCircleSprite();
            }

            PositionGuidanceInfoButtonBottomRight(cam);
        }

        private void PositionGuidanceInfoButtonBottomRight(Camera cam)
        {
            if (_guidanceInfoButton == null)
                return;

            float z = Mathf.Max(0.1f, guidancePromptLocalPosition.z);
            if (cam == null)
            {
                _guidanceInfoButton.transform.localPosition = new Vector3(0f, 0f, z);
                return;
            }

            // Half-extents of the view frustum at distance z, then inset the button + its margin.
            float halfHeight = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * z;
            float halfWidth = halfHeight * cam.aspect;
            float halfButton = infoButtonSize.x * 0.5f * guidancePromptScale;
            float margin = infoButtonPadding * guidancePromptScale;

            float x = halfWidth - halfButton - margin;
            float y = -halfHeight + halfButton + margin;
            _guidanceInfoButton.transform.localPosition = new Vector3(x, y, z);
        }

        private static Sprite _circleSprite;

        private static Sprite GetCircleSprite()
        {
            // Unity's built-in "Knob" UI sprite is a filled soft circle.
            if (_circleSprite == null)
                _circleSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
            return _circleSprite;
        }

        private Button SpawnGuidanceButton(string objectName, string label, RectTransform parent, Vector2 anchoredPos,
                                           Vector2 size, System.Action onClick, Color fillColor, float fontSize)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.color = fillColor;
            image.raycastTarget = false;

            var button = go.GetComponent<Button>();
            if (onClick != null)
                button.onClick.AddListener(() => onClick());

            var collider = go.AddComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = new Vector3(size.x, size.y, 24f);

            var interactable = go.AddComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener(_ => button.onClick.Invoke());

            var labelTmp = SpawnGuidanceText("Label", label, rt, Vector2.zero, size, fontSize, Color.black, TextAlignmentOptions.Center);
            if (labelTmp != null)
            {
                labelTmp.rectTransform.anchorMin = Vector2.zero;
                labelTmp.rectTransform.anchorMax = Vector2.one;
                labelTmp.rectTransform.offsetMin = Vector2.zero;
                labelTmp.rectTransform.offsetMax = Vector2.zero;
                labelTmp.fontStyle = FontStyles.Bold;
            }

            return button;
        }

        private TextMeshProUGUI SpawnGuidanceText(string objectName, string text, RectTransform parent,
                                                  Vector2 anchoredPos, Vector2 size, float fontSize,
                                                  Color color, TextAlignmentOptions alignment)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.color = color;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Truncate;
            tmp.raycastTarget = false;
            return tmp;
        }

        private void ShowGuidanceInfoPanel()
        {
            EnsureGuidancePrompt();

            if (_guidanceInfoPanelRoot != null)
            {
                _guidanceInfoPanelRoot.gameObject.SetActive(true);
                return;
            }

            var root = _guidancePrompt != null ? _guidancePrompt.GetComponent<RectTransform>() : null;
            if (root == null)
                return;

            Vector2 panelSize = new Vector2(1700f, 700f);
            var go = new GameObject("InfoPanel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(root, false);
            _guidanceInfoPanelRoot = go.GetComponent<RectTransform>();
            _guidanceInfoPanelRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _guidanceInfoPanelRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _guidanceInfoPanelRoot.pivot = new Vector2(0.5f, 1f);
            // Sit below the top hint prompt without dropping too far.
            _guidanceInfoPanelRoot.anchoredPosition = new Vector2(0f, -120f);
            _guidanceInfoPanelRoot.sizeDelta = panelSize;

            var image = go.GetComponent<Image>();
            image.color = infoPanelColor;
            image.raycastTarget = false;

            const float panelTextSize = 24f;
            float halfH = panelSize.y * 0.5f;
            float halfW = panelSize.x * 0.5f;

            // Title + welcome subtitle, centred at the top.
            SpawnGuidanceText("InfoTitle", "Instructions", _guidanceInfoPanelRoot,
                new Vector2(0f, halfH - 50f), new Vector2(panelSize.x - 180f, 56f),
                panelTextSize * 1.5f, infoTextColor, TextAlignmentOptions.Center);

            SpawnGuidanceText("InfoWelcome", InfoWelcomeText, _guidanceInfoPanelRoot,
                new Vector2(0f, halfH - 104f), new Vector2(panelSize.x - 180f, 48f),
                panelTextSize, infoTextColor, TextAlignmentOptions.Center);

            // Two columns: Gameplay (left), Controls (right).
            float columnWidth = halfW - 120f;
            float columnX = panelSize.x * 0.25f;
            Vector2 columnSize = new Vector2(columnWidth, panelSize.y - 280f);
            float columnY = -40f;

            SpawnGuidanceText("InfoGameplay", InfoGameplayText, _guidanceInfoPanelRoot,
                new Vector2(-columnX, columnY), columnSize,
                panelTextSize, infoTextColor, TextAlignmentOptions.TopLeft);

            SpawnGuidanceText("InfoControls", InfoControlsText, _guidanceInfoPanelRoot,
                new Vector2(columnX, columnY), columnSize,
                panelTextSize, infoTextColor, TextAlignmentOptions.TopLeft);

            SpawnGuidanceButton("InfoCloseButton", "Close", _guidanceInfoPanelRoot,
                new Vector2(0f, -halfH + 52f), new Vector2(200f, 72f),
                HideGuidanceInfoPanel, new Color(0.88f, 0.48f, 0.38f, 0.96f), panelTextSize);
        }

        private void HideGuidanceInfoPanel()
        {
            if (_guidanceInfoPanelRoot != null)
                _guidanceInfoPanelRoot.gameObject.SetActive(false);
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
            if (_guidanceMode == GuidanceMode.StartHint || _guidanceMode == GuidanceMode.AtomPickHint)
                return;

            if (_guidanceMode == GuidanceMode.AtomToSmallChamber)
            {
                UpdateAtomGuidanceArrow();
                return;
            }

            if (_guidanceMode != GuidanceMode.MoleculeToBigChamber || _guidanceTag == null || _guidanceTag.Owner == null || chamber == null)
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
                start = center;
                end = chamber.transform.position + Vector3.up * 0.18f;
            }
            else
            {
                start = center + Vector3.up * guidanceArrowTopHeight;
                end = center + Vector3.up * 0.08f;
            }

            ApplyGuidanceArrow(start, end);
        }

        private void UpdateAtomGuidanceArrow()
        {
            if (_guidanceAtom == null || smallChamber == null)
            {
                HideMoleculeGuidance();
                return;
            }

            if (smallChamber.IsAtomStaged(_guidanceAtom))
            {
                RestartAtomPickHintTimer();
                HideMoleculeGuidance();
                return;
            }

            if (_guidanceAtom.Element == null || !smallChamber.IsElementNeeded(_guidanceAtom.Element))
            {
                RestartAtomPickHintTimer(smallChamber.GetNextNeededElement());
                HideMoleculeGuidance();
                return;
            }

            EnsureGuidanceArrow();

            Vector3 center = _guidanceAtom.transform.position;
            bool isHeld = IsAtomBeingHeld(_guidanceAtom);
            Vector3 start = isHeld ? center : center + Vector3.up * guidanceArrowTopHeight;
            Vector3 end = isHeld ? smallChamber.GuidanceTarget + Vector3.up * 0.12f : center + Vector3.up * 0.08f;
            ApplyGuidanceArrow(start, end);
        }

        private void UpdateAtomPickHintTimer()
        {
            if (_levelCompleted || _stage1Complete || smallChamber == null || !smallChamber.HasActiveTarget)
            {
                HideAtomPickHint();
                return;
            }

            ElementSO needed = smallChamber.GetNextNeededElement();
            if (needed == null)
            {
                HideAtomPickHint();
                return;
            }

            if (needed != _lastNeededHintElement)
                RestartAtomPickHintTimer(needed);

            if (_guidanceMode != GuidanceMode.None && _guidanceMode != GuidanceMode.AtomPickHint)
                return;

            if (Time.time - _lastCorrectAtomSelectionTime >= atomPickHintDelay)
                ShowAtomPickHint(needed);
        }

        private void ShowAtomPickHint(ElementSO element)
        {
            if (element == null)
                return;

            _guidanceMode = GuidanceMode.AtomPickHint;
            _guidanceAtom = null;
            _guidanceTag = null;

            EnsureGuidancePrompt();
            SetGuidancePrompt(string.Format(atomPickHintMessageFormat, element.Symbol), atomPickHintPromptColor, atomPickHintPromptFontSize);

            if (_guidanceMessageRoot != null)
                _guidanceMessageRoot.gameObject.SetActive(true);

            if (_guidanceArrow != null)
                _guidanceArrow.SetActive(false);
        }

        private void HideAtomPickHint()
        {
            if (_guidanceMode != GuidanceMode.AtomPickHint)
                return;

            HideMoleculeGuidance();
        }

        private void RestartAtomPickHintTimer(ElementSO neededElement = null)
        {
            _lastCorrectAtomSelectionTime = Time.time;
            _lastNeededHintElement = neededElement != null || smallChamber == null ? neededElement : smallChamber.GetNextNeededElement();
            HideAtomPickHint();
        }

        private void ShowStartHint()
        {
            if (string.IsNullOrWhiteSpace(startHintMessage) || startHintDuration <= 0f)
                return;

            if (_startHintRoutine != null)
                StopCoroutine(_startHintRoutine);

            _startHintRoutine = StartCoroutine(ShowStartHintRoutine());
        }

        private IEnumerator ShowStartHintRoutine()
        {
            _guidanceMode = GuidanceMode.StartHint;
            _guidanceAtom = null;
            _guidanceTag = null;

            EnsureGuidancePrompt();
            SetGuidancePrompt(startHintMessage, startHintPromptColor, guidancePromptFontSize);

            if (_guidanceMessageRoot != null)
                _guidanceMessageRoot.gameObject.SetActive(true);

            if (_guidanceArrow != null)
                _guidanceArrow.SetActive(false);

            yield return new WaitForSeconds(startHintDuration);

            if (_guidanceMode == GuidanceMode.StartHint)
                HideMoleculeGuidance();

            _startHintRoutine = null;
        }

        private void ApplyGuidanceArrow(Vector3 start, Vector3 end)
        {
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

        private static bool IsAtomBeingHeld(Atom atom)
        {
            if (atom == null)
                return false;

            var grab = atom.GetComponent<XRGrabInteractable>();
            return grab != null && grab.isSelected;
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
