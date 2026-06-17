using System.Collections.Generic;
using System.Text;
using System.Collections;
using MolecularLab.Chemistry;
using MolecularLab.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace MolecularLab.UI
{
    /// <summary>
    /// UI panel that shows the current level:
    ///   - Title
    ///   - Stage 1 checklist (one row per intermediate, with checkbox + progress)
    ///   - Stage 2 reaction equation (dimmed until Stage 1 complete)
    ///   - Optional completion banner
    /// Built procedurally so no prefab is required.
    /// </summary>
    public class LevelObjectiveUI : MonoBehaviour
    {
        [Header("Canvas Layout")]
        [SerializeField] private Vector2 panelSize = new Vector2(520f, 360f);
        [SerializeField] private Vector2 anchoredPosition = new Vector2(-30f, -30f);
        [SerializeField] private float padding = 24f;
        [SerializeField] private float rowHeight = 48f;

        [Header("VR Layout")]
        [SerializeField] private bool useWorldSpaceCanvasInXR = true;
        [SerializeField] private Vector3 vrPanelLocalPosition = new Vector3(0.28f, 0.12f, 0.8f);
        [SerializeField] private Vector3 vrPanelLocalEuler = new Vector3(0f, 28f, 0f);
        [SerializeField] private float vrCanvasScale = 0.0012f;

        [Header("Scene Layout")]
        [SerializeField] private bool anchorNearReactionChamberInXR = true;
        [SerializeField] private Transform sceneAnchor;
        [SerializeField] private string sceneAnchorName = "Display";
        [SerializeField] private Vector3 scenePanelLocalPosition = new Vector3(0f, 0f, -15f);
        [SerializeField] private Vector3 scenePanelLocalEuler = Vector3.zero;
        [SerializeField] private float sceneCanvasScale = 0.00135f;
        [SerializeField] private bool stretchToSceneAnchorFace = true;
        [SerializeField] private float scenePixelsPerUnit = 1000f;

        [Header("Visuals")]
        [SerializeField] private Color panelColor = new Color(0.07f, 0.08f, 0.12f, 0f);
        [SerializeField] private Color titleColor = Color.black;
        [SerializeField] private Color rowColor = Color.black;
        [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, 0.72f);
        [SerializeField] private Color stage2ActiveColor = Color.black;
        [SerializeField] private Color completionColor = Color.black;

        [Header("Font sizes")]
        [SerializeField] private float titleSize = 60f;
        [SerializeField] private float rowSize = 40f;
        [SerializeField] private float stage2Size = 60f;
        [SerializeField] private float completionSize = 64f;
        [SerializeField] private float buttonTextSize = 40f;
        [SerializeField] private Vector2 resetButtonSize = new Vector2(200f, 100f);
        [SerializeField] private Vector2 nextButtonSize = new Vector2(200f, 100f);

        private Canvas _canvas;
        private RectTransform _panelRoot;
        private Image _panelImage;
        private Vector2 _activePanelSize;

        private LevelSO _level;
        private System.Action _onReset;
        private TextMeshProUGUI _titleTmp;
        private readonly List<TextMeshProUGUI> _stage1Rows = new List<TextMeshProUGUI>();
        private readonly Dictionary<CompoundSO, int> _prevCounts = new Dictionary<CompoundSO, int>();
        private TextMeshProUGUI _stage2Tmp;
        private TextMeshProUGUI _completionTmp;
        private TextMeshProUGUI _statusTmp;
        private Button _nextButton;
        private Button _resetButton;
        private Coroutine _statusRoutine;

        private void Awake()
        {
            MigrateLegacyWorldSpaceValues();
        }

        public void SetResetAction(System.Action onReset)
        {
            _onReset = onReset;

            if (_resetButton == null)
                return;

            _resetButton.onClick.RemoveAllListeners();
            if (_onReset != null)
                _resetButton.onClick.AddListener(() => _onReset());
        }

        public void SetLevel(LevelSO level, IReadOnlyDictionary<CompoundSO, int> built, bool stage1Complete)
        {
            _prevCounts.Clear();
            if (built != null)
            {
                foreach (var kv in built)
                {
                    if (kv.Key != null)
                        _prevCounts[kv.Key] = kv.Value;
                }
            }

            _level = level;
            ClearChildren();
            if (level == null) return;

            if (TMP_Settings.defaultFontAsset == null)
            {
                Debug.LogWarning("[LevelObjectiveUI] TMP default font asset missing — Window > TextMeshPro > Import TMP Essential Resources", this);
                return;
            }

            BuildPanel();
            BuildResetButton();
            BuildTitle(level.Title);
            BuildStage1(level.Stage1, built);
            BuildStage2(level.Stage2, stage1Complete);
        }

        public void UpdateStage1(IReadOnlyDictionary<CompoundSO, int> built, bool stage1Complete)
        {
            if (_level == null) return;
            var stage1 = _level.Stage1;
            bool playedSfx = false;

            for (int i = 0; i < _stage1Rows.Count && i < stage1.Count; i++)
            {
                var s = stage1[i];
                int have = 0;
                if (s.compound != null) built.TryGetValue(s.compound, out have);
                _stage1Rows[i].text = FormatRow(s, have);
                _stage1Rows[i].color = have >= s.count ? stage2ActiveColor : rowColor;

                if (s.compound != null)
                {
                    _prevCounts.TryGetValue(s.compound, out int prevHave);
                    if (have > prevHave)
                    {
                        playedSfx = true;
                    }
                    _prevCounts[s.compound] = have;
                }
            }
            if (_stage2Tmp != null) _stage2Tmp.color = stage1Complete ? stage2ActiveColor : dimColor;

            if (playedSfx && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayDisplayUpdate(transform.position);
            }
        }

        public void ShowCompletion(string completedTitle, string nextTitle, System.Action onNext)
        {
            ClearChildren();
            BuildPanel();
            Vector2 panel = GetActivePanelSize();
            _completionTmp = SpawnText("Completion",
                $"✓ {completedTitle}\n\nNext:\n{nextTitle}",
                Vector2.zero,
                completionSize, completionColor, TextAlignmentOptions.Center,
                new Vector2(panel.x - 2f * padding - nextButtonSize.x - 18f, panel.y - 2f * padding - 70f),
                TextAnchor.MiddleCenter);
            _nextButton = SpawnButton("NextButton", "Next",
                new Vector2(600f, -600f),
                nextButtonSize,
                onNext,
                new Color(0.72f, 0.96f, 0.68f, 0.98f));
        }

        public void ShowNextButton(System.Action onNext)
        {
            if (_level == null)
                return;

            Vector2 panel = GetActivePanelSize();

            if (_nextButton != null)
            {
                _nextButton.onClick.RemoveAllListeners();
                if (onNext != null) _nextButton.onClick.AddListener(() => onNext());
                _nextButton.gameObject.SetActive(true);
                return;
            }

            _nextButton = SpawnButton("NextButton", "Next",
                new Vector2(600f, -600f),
                nextButtonSize,
                onNext,
                new Color(0.72f, 0.96f, 0.68f, 0.98f));
        }

        public void ShowAllLevelsCompleted(System.Action onRetry)
        {
            ClearChildren();
            BuildPanel();

            Vector2 panel = GetActivePanelSize();
            _completionTmp = SpawnText("AllLevelsCompleted",
                "ALL LEVELS COMPLETED!",
                Vector2.zero,
                completionSize,
                completionColor,
                TextAlignmentOptions.Center,
                new Vector2(panel.x - 2f * padding, panel.y * 0.45f),
                TextAnchor.MiddleCenter);

            _nextButton = SpawnButton("RetryButton", "RETRY",
                new Vector2(600f, -600f),
                nextButtonSize,
                onRetry,
                new Color(0.72f, 0.96f, 0.68f, 0.98f));
        }

        public void ShowStatus(string message, Color color, float duration = 2f)
        {
            if (_level == null) return;
            Vector2 panel = GetActivePanelSize();
            if (_statusTmp == null)
            {
                _statusTmp = SpawnText("Status", message,
                    new Vector2(0f, -(panel.y - padding * 2f - stage2Size * 4f)),
                    rowSize * 0.9f, color, TextAlignmentOptions.Center,
                    new Vector2(panel.x - 2f * padding, rowHeight * 1.4f),
                    TextAnchor.UpperCenter);
            }

            _statusTmp.text = message;
            _statusTmp.color = color;
            _statusTmp.gameObject.SetActive(true);

            if (_statusRoutine != null) StopCoroutine(_statusRoutine);
            _statusRoutine = StartCoroutine(ClearStatusAfter(duration));
        }

        // ─── builders ───────────────────────────────────────────────────────

        private void BuildPanel()
        {
            if (!EnsurePanelRoot()) return;
            _panelImage.color = panelColor;
            _panelImage.raycastTarget = false;
        }

        private void BuildTitle(string title)
        {
            Vector2 panel = GetActivePanelSize();
            float y = -padding - titleSize * 0.6f;
            _titleTmp = SpawnText("Title", title,
                new Vector2(-(panel.x * 0.5f - padding) + 28f, y),
                titleSize, titleColor, TextAlignmentOptions.TopLeft,
                new Vector2(panel.x - 2f * padding - resetButtonSize.x - 20f, titleSize * 1.5f),
                TextAnchor.UpperLeft);
        }

        private void BuildStage1(IReadOnlyList<ReactionRecipeSO.CompoundCount> stage1, IReadOnlyDictionary<CompoundSO, int> built)
        {
            Vector2 panel = GetActivePanelSize();
            // float topY = -padding - titleSize * 1.9f;
            float topY = -138f; 
            for (int i = 0; i < stage1.Count; i++)
            {
                var s = stage1[i];
                int have = 0;
                if (s.compound != null && built != null) built.TryGetValue(s.compound, out have);

                float y = topY - i * rowHeight;
                Vector2 anchoredPosition = new Vector2(-60f, y);
                Vector2 rowSizeDelta = new Vector2(panel.x - 2f * padding, rowHeight);
                if (i == 0)
                {
                    anchoredPosition = new Vector2(-680f, -110f);
                    rowSizeDelta = new Vector2(800f, 500f);
                }
                else if (i == 1)
                {
                    anchoredPosition = new Vector2(60f, -110f);
                    rowSizeDelta = new Vector2(800f, 500f);
                }

                var tmp = SpawnText($"Stage1_{i}", FormatRow(s, have),
                    anchoredPosition,
                    rowSize,
                    have >= s.count ? stage2ActiveColor : rowColor,
                    TextAlignmentOptions.Left,
                    rowSizeDelta,
                    TextAnchor.UpperLeft);
                _stage1Rows.Add(tmp);
            }
        }

        private void BuildStage2(ReactionRecipeSO stage2, bool stage1Complete)
        {
            if (stage2 == null) return;
            Vector2 panel = GetActivePanelSize();
            string equation = FormatRecipe(stage2);
            float y = -600f;
            _stage2Tmp = SpawnText("Stage2", equation,
                new Vector2(0f, y),
                stage2Size,
                stage1Complete ? stage2ActiveColor : dimColor,
                TextAlignmentOptions.Center,
                new Vector2(panel.x - 2f * padding, stage2Size * 2.2f),
                TextAnchor.UpperCenter);
        }

        private void BuildResetButton()
        {
            Vector2 panel = GetActivePanelSize();
            _resetButton = SpawnButton("ResetButton", "RESET",
                new Vector2(panel.x * 0.5f - padding - resetButtonSize.x * 0.5f - 24f, -padding - 12f),
                resetButtonSize,
                _onReset,
                new Color(0.88f, 0.48f, 0.38f, 0.96f));
        }

        // ─── helpers ────────────────────────────────────────────────────────

        private TextMeshProUGUI SpawnText(string name, string text, Vector2 anchoredPos,
                                          float fontSize, Color color,
                                          TextAlignmentOptions align, Vector2 size,
                                          TextAnchor anchor)
        {
            if (!EnsurePanelRoot()) return null;

            var go = new GameObject(name);
            go.transform.SetParent(_panelRoot, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = AnchorToPivot(anchor);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.alignment = align;
            tmp.color = color;
            tmp.textWrappingMode = TMPro.TextWrappingModes.Normal;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.outlineWidth = 0.18f;
            tmp.outlineColor = new Color(0f, 0f, 0f, 0.9f);
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;
            return tmp;
        }

        private void ClearChildren()
        {
            _stage1Rows.Clear();
            _titleTmp = null;
            _stage2Tmp = null;
            _completionTmp = null;
            _statusTmp = null;
            _nextButton = null;
            _resetButton = null;
            if (_panelRoot == null) return;
            for (int i = _panelRoot.childCount - 1; i >= 0; i--)
                Destroy(_panelRoot.GetChild(i).gameObject);
        }

        private Button SpawnButton(string objectName, string label, Vector2 anchoredPos, Vector2 size, System.Action onClick, Color? fillColor = null)
        {
            if (!EnsurePanelRoot()) return null;

            var go = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_panelRoot, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.color = fillColor ?? stage2ActiveColor;
            image.raycastTarget = false;

            var button = go.GetComponent<Button>();
            if (onClick != null) button.onClick.AddListener(() => onClick());

            if (_canvas != null && _canvas.renderMode == RenderMode.WorldSpace)
            {
                var collider = go.AddComponent<BoxCollider>();
                collider.center = Vector3.zero;
                collider.size = new Vector3(size.x, size.y, 24f);

                var interactable = go.AddComponent<XRSimpleInteractable>();
                interactable.selectEntered.AddListener(_ => button.onClick.Invoke());
            }

            Color labelColor = objectName == "NextButton" ? Color.black : Color.black;
            var labelTmp = SpawnText("Label", label, Vector2.zero, buttonTextSize, labelColor,
                TextAlignmentOptions.Center, size, TextAnchor.MiddleCenter);
            if (labelTmp != null)
            {
                labelTmp.transform.SetParent(go.transform, false);

                var labelRt = labelTmp.rectTransform;
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.pivot = new Vector2(0.5f, 0.5f);
                labelRt.anchoredPosition = Vector2.zero;
                labelRt.sizeDelta = Vector2.zero;
                labelRt.offsetMin = Vector2.zero;
                labelRt.offsetMax = Vector2.zero;
                labelTmp.verticalAlignment = VerticalAlignmentOptions.Middle;
                labelTmp.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            }

            return button;
        }

        private IEnumerator ClearStatusAfter(float duration)
        {
            yield return new WaitForSeconds(duration);
            if (_statusTmp != null) _statusTmp.gameObject.SetActive(false);
            _statusRoutine = null;
        }

        private bool EnsurePanelRoot()
        {
            if (_panelRoot != null && _panelImage != null) return true;

            _canvas = GetComponentInChildren<Canvas>(true);
            if (_canvas == null)
                _canvas = CreateOwnedCanvas();
            if (_canvas == null)
            {
                Debug.LogWarning("[LevelObjectiveUI] No Canvas found in scene for objective UI.", this);
                return false;
            }

            ConfigureCanvasForCurrentPlatform();

            var existing = _canvas.transform.Find("LevelObjectivePanel");
            if (existing != null)
            {
                _panelRoot = existing as RectTransform;
                _panelImage = existing.GetComponent<Image>();
            }

            if (_panelRoot == null)
            {
                var go = new GameObject("LevelObjectivePanel", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_canvas.transform, false);
                _panelRoot = go.GetComponent<RectTransform>();
                _panelImage = go.GetComponent<Image>();
            }

            if (_canvas.renderMode == RenderMode.WorldSpace)
            {
                _panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
                _panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
                _panelRoot.pivot = new Vector2(0.5f, 0.5f);
                _panelRoot.anchoredPosition = Vector2.zero;
                _panelRoot.localPosition = Vector3.zero;
                _panelRoot.localRotation = Quaternion.identity;
                _panelRoot.localScale = Vector3.one;
                _panelRoot.sizeDelta = GetActivePanelSize();
            }
            else
            {
                _panelRoot.anchorMin = new Vector2(1f, 1f);
                _panelRoot.anchorMax = new Vector2(1f, 1f);
                _panelRoot.pivot = new Vector2(1f, 1f);
                _panelRoot.anchoredPosition = anchoredPosition;
                _panelRoot.sizeDelta = GetActivePanelSize();
            }
            return true;
        }

        private void ConfigureCanvasForCurrentPlatform()
        {
            if (_canvas == null)
                return;

            RectTransform canvasRt = _canvas.transform as RectTransform;
            if (canvasRt == null)
                return;

            if (!ShouldUseWorldSpaceCanvas())
            {
                _activePanelSize = panelSize;
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.worldCamera = null;
                canvasRt.SetParent(transform, false);
                canvasRt.localPosition = Vector3.zero;
                canvasRt.localRotation = Quaternion.identity;
                canvasRt.localScale = Vector3.one;
                canvasRt.sizeDelta = _activePanelSize;
                return;
            }

            Transform target = ResolveWorldCanvasAnchor();
            if (target == null)
                return;

            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.worldCamera = Camera.main;
            DisableWorldSpaceRaycasters();

            if (anchorNearReactionChamberInXR)
            {
                AttachCanvasToAnchor(canvasRt, target, scenePanelLocalPosition, scenePanelLocalEuler, sceneCanvasScale);
            }
            else
            {
                AttachCanvasToAnchor(canvasRt, target, vrPanelLocalPosition, vrPanelLocalEuler, vrCanvasScale);
            }

            _activePanelSize = ResolveActivePanelSize(target);
            canvasRt.sizeDelta = _activePanelSize;
        }

        private Canvas CreateOwnedCanvas()
        {
            var existing = transform.Find("LevelObjectiveCanvas");
            if (existing != null)
                return existing.GetComponent<Canvas>();

            var go = new GameObject("LevelObjectiveCanvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
            return canvas;
        }

        private static void AttachCanvasToAnchor(RectTransform canvasRt, Transform target, Vector3 localPosition, Vector3 localEuler, float localScale)
        {
            if (canvasRt == null || target == null)
                return;

            canvasRt.SetParent(target, false);
            canvasRt.localPosition = localPosition;
            canvasRt.localRotation = Quaternion.Euler(localEuler);
            canvasRt.localScale = Vector3.one * localScale;
        }

        private Vector2 ResolveActivePanelSize(Transform target)
        {
            if (!stretchToSceneAnchorFace || target == null || !anchorNearReactionChamberInXR)
                return panelSize;

            Vector2 localFace = GetAnchorLocalFaceSize(target);
            if (localFace.x <= 0.001f || localFace.y <= 0.001f)
                return panelSize;

            return localFace * scenePixelsPerUnit;
        }

        private static Vector2 GetAnchorLocalFaceSize(Transform target)
        {
            if (target == null)
                return Vector2.zero;

            if (target.TryGetComponent<BoxCollider>(out var box))
            {
                Vector3 scale = target.localScale;
                return new Vector2(Mathf.Abs(box.size.x * scale.x), Mathf.Abs(box.size.y * scale.y));
            }

            Vector3 s = target.localScale;
            return new Vector2(Mathf.Abs(s.x), Mathf.Abs(s.y));
        }

        private Vector2 GetActivePanelSize()
        {
            return _activePanelSize == Vector2.zero ? panelSize : _activePanelSize;
        }

        private void DisableWorldSpaceRaycasters()
        {
            if (_canvas == null)
                return;

            var graphicRaycaster = _canvas.GetComponent<GraphicRaycaster>();
            if (graphicRaycaster != null)
                graphicRaycaster.enabled = false;

            var trackedRaycaster = _canvas.GetComponent<TrackedDeviceGraphicRaycaster>();
            if (trackedRaycaster != null)
                trackedRaycaster.enabled = false;
        }

        private Transform ResolveWorldCanvasAnchor()
        {
            if (sceneAnchor != null)
                return sceneAnchor;

            if (!string.IsNullOrWhiteSpace(sceneAnchorName))
            {
                var namedAnchor = FindSceneTransformByName(sceneAnchorName);
                if (namedAnchor != null)
                {
                    sceneAnchor = namedAnchor;
                    return sceneAnchor;
                }
            }

            if (anchorNearReactionChamberInXR)
            {
                var chamber = FindFirstObjectByType<MolecularLab.Interaction.ReactionChamber>();
                if (chamber != null)
                    return chamber.transform;
            }

            return transform;
        }

        private static Transform FindSceneTransformByName(string targetName)
        {
            var transforms = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < transforms.Length; i++)
            {
                var candidate = transforms[i];
                if (candidate != null && candidate.name == targetName)
                    return candidate;
            }

            return null;
        }

        private bool ShouldUseWorldSpaceCanvas()
        {
            if (!useWorldSpaceCanvasInXR)
                return false;

            if (XRSettings.isDeviceActive)
                return true;

            Camera cam = Camera.main;
            if (cam != null && cam.stereoTargetEye != StereoTargetEyeMask.None)
                return true;

            return false;
        }

        private void MigrateLegacyWorldSpaceValues()
        {
            // Older scene data stored meter-sized values for a 3D world panel.
            // If we detect that layout, swap to readable canvas-space defaults.
            if (panelSize.x <= 5f && panelSize.y <= 5f)
                panelSize = new Vector2(520f, 360f);

            if (padding <= 2f)
                padding = 24f;

            if (rowHeight <= 38f)
                rowHeight = 48f;

            if (titleSize <= 2f)
                titleSize = 34f;

            if (rowSize <= 2f)
                rowSize = 24f;

            if (stage2Size <= 2f)
                stage2Size = 28f;

            if (completionSize <= 2f)
                completionSize = 40f;

            if (anchoredPosition == Vector2.zero)
                anchoredPosition = new Vector2(-30f, -30f);

            if (Approximately(panelColor, new Color(0.08f, 0.09f, 0.12f, 1f))
                || Approximately(panelColor, new Color(0.92f, 0.94f, 0.97f, 0.16f))
                || Approximately(panelColor, new Color(0.92f, 0.94f, 0.97f, 0.56f))
                || Approximately(panelColor, new Color(0.07f, 0.08f, 0.12f, 0.74f)))
            {
                panelColor = new Color(0.07f, 0.08f, 0.12f, 0f);
            }

            if (Approximately(titleColor, new Color(1f, 0.95f, 0.6f, 1f))
                || Approximately(titleColor, new Color(0.12f, 0.16f, 0.22f, 1f))
                || Approximately(titleColor, new Color(0.98f, 0.99f, 1f, 1f)))
            {
                titleColor = Color.black;
            }

            if (Approximately(rowColor, Color.white)
                || Approximately(rowColor, new Color(0.16f, 0.19f, 0.23f, 1f))
                || Approximately(rowColor, new Color(0.93f, 0.96f, 1f, 1f)))
            {
                rowColor = Color.black;
            }

            if (Approximately(dimColor, new Color(1f, 1f, 1f, 0.35f))
                || Approximately(dimColor, new Color(0.16f, 0.19f, 0.23f, 0.42f))
                || Approximately(dimColor, new Color(0.78f, 0.83f, 0.92f, 0.72f)))
            {
                dimColor = new Color(0f, 0f, 0f, 0.72f);
            }

            if (Approximately(stage2ActiveColor, new Color(0.6f, 1f, 0.6f, 1f))
                || Approximately(stage2ActiveColor, new Color(0.12f, 0.55f, 0.24f, 1f))
                || Approximately(stage2ActiveColor, new Color(0.6f, 1f, 0.72f, 1f)))
            {
                stage2ActiveColor = Color.black;
            }

            if (Approximately(completionColor, new Color(1f, 0.7f, 0.2f, 1f))
                || Approximately(completionColor, new Color(0.7f, 0.45f, 0.08f, 1f))
                || Approximately(completionColor, new Color(1f, 0.86f, 0.42f, 1f)))
            {
                completionColor = Color.black;
            }

            if (titleSize <= 50f)
                titleSize = 60f;

            if (rowSize <= 36f)
                rowSize = 42f;

            if (stage2Size <= 60f)
                stage2Size = 60f;

            if (completionSize <= 54f)
                completionSize = 64f;

            if (buttonTextSize <= 34f)
                buttonTextSize = 40f;

            if (resetButtonSize == new Vector2(130f, 44f)
                || resetButtonSize == new Vector2(112f, 38f)
                || resetButtonSize == new Vector2(154f, 54f))
            {
                resetButtonSize = new Vector2(200f, 100f);
            }

            if (nextButtonSize == Vector2.zero
                || nextButtonSize == new Vector2(140f, 46f)
                || nextButtonSize == new Vector2(182f, 62f))
            {
                nextButtonSize = new Vector2(200f, 100f);
            }

            if (vrPanelLocalEuler == new Vector3(0f, 164f, 0f) || vrPanelLocalEuler == new Vector3(0f, -16f, 0f) || vrPanelLocalEuler == new Vector3(0f, 16f, 0f))
                vrPanelLocalEuler = new Vector3(0f, 28f, 0f);

            if (sceneCanvasScale <= 0f)
                sceneCanvasScale = 0.00135f;

            if (string.IsNullOrWhiteSpace(sceneAnchorName))
                sceneAnchorName = "Display";

            if (scenePanelLocalPosition == new Vector3(0.85f, 0.4f, 0f))
                scenePanelLocalPosition = new Vector3(-0.1f, -0.3f, -3f);

            if (scenePanelLocalEuler == new Vector3(0f, -90f, 0f)
                || scenePanelLocalEuler == new Vector3(0f, 90f, 0f)
                || scenePanelLocalEuler == new Vector3(0f, 180f, 0f))
            {
                scenePanelLocalEuler = Vector3.zero;
            }
        }

        private static bool Approximately(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.001f
                && Mathf.Abs(a.g - b.g) < 0.001f
                && Mathf.Abs(a.b - b.b) < 0.001f
                && Mathf.Abs(a.a - b.a) < 0.001f;
        }

        private static Vector2 AnchorToPivot(TextAnchor anchor)
        {
            return anchor switch
            {
                TextAnchor.UpperLeft => new Vector2(0f, 1f),
                TextAnchor.UpperCenter => new Vector2(0.5f, 1f),
                TextAnchor.UpperRight => new Vector2(1f, 1f),
                TextAnchor.MiddleLeft => new Vector2(0f, 0.5f),
                TextAnchor.MiddleCenter => new Vector2(0.5f, 0.5f),
                TextAnchor.MiddleRight => new Vector2(1f, 0.5f),
                TextAnchor.LowerLeft => new Vector2(0f, 0f),
                TextAnchor.LowerCenter => new Vector2(0.5f, 0f),
                TextAnchor.LowerRight => new Vector2(1f, 0f),
                _ => new Vector2(0.5f, 0.5f),
            };
        }

        private static string FormatRow(ReactionRecipeSO.CompoundCount target, int have)
        {
            // string check = have >= target.count ? "✓" : "□";
            // string label = target.compound != null ? target.compound.Formula : "?";
            // return $"  {check}  {target.count} × {label}   ({have}/{target.count})\n\n\n  {target.compound?.Description ?? string.Empty}";
            return $" {target.compound?.CompoundName ?? string.Empty} {target.compound?.Formula ?? string.Empty} \n Molecular Mass: {target.compound?.MolecularMass ?? 0} \n State At Room Temperature: {target.compound?.StateAtRoomTemp.ToString() ?? string.Empty} \n {target.compound?.Description ?? string.Empty}";
        }

        private static string FormatRecipe(ReactionRecipeSO recipe)
        {
            if (!string.IsNullOrEmpty(recipe.DisplayName)) return recipe.DisplayName;
            var sb = new StringBuilder();
            AppendSide(sb, recipe.Inputs);
            sb.Append(" → ");
            AppendSide(sb, recipe.Outputs);
            return sb.ToString();
        }

        private static void AppendSide(StringBuilder sb, IReadOnlyList<ReactionRecipeSO.CompoundCount> side)
        {
            for (int i = 0; i < side.Count; i++)
            {
                if (i > 0) sb.Append(" + ");
                var c = side[i];
                if (c.count > 1) sb.Append(c.count);
                sb.Append(c.compound != null ? c.compound.Formula : "?");
            }
        }
    }
}
