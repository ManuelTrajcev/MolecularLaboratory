using System.Collections.Generic;
using System.Text;
using System.Collections;
using MolecularLab.Chemistry;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private float rowHeight = 38f;

        [Header("Visuals")]
        [SerializeField] private Color panelColor = new Color(0.08f, 0.09f, 0.12f, 1f);
        [SerializeField] private Color titleColor = new Color(1f, 0.95f, 0.6f);
        [SerializeField] private Color rowColor = Color.white;
        [SerializeField] private Color dimColor = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] private Color stage2ActiveColor = new Color(0.6f, 1f, 0.6f);
        [SerializeField] private Color completionColor = new Color(1f, 0.7f, 0.2f);

        [Header("Font sizes")]
        [SerializeField] private float titleSize = 34f;
        [SerializeField] private float rowSize = 24f;
        [SerializeField] private float stage2Size = 28f;
        [SerializeField] private float completionSize = 40f;
        [SerializeField] private float buttonTextSize = 24f;
        [SerializeField] private Vector2 resetButtonSize = new Vector2(130f, 44f);

        private Canvas _canvas;
        private RectTransform _panelRoot;
        private Image _panelImage;

        private LevelSO _level;
        private System.Action _onReset;
        private TextMeshProUGUI _titleTmp;
        private readonly List<TextMeshProUGUI> _stage1Rows = new List<TextMeshProUGUI>();
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
            for (int i = 0; i < _stage1Rows.Count && i < stage1.Count; i++)
            {
                var s = stage1[i];
                int have = 0;
                if (s.compound != null) built.TryGetValue(s.compound, out have);
                _stage1Rows[i].text = FormatRow(s, have);
                _stage1Rows[i].color = have >= s.count ? stage2ActiveColor : rowColor;
            }
            if (_stage2Tmp != null) _stage2Tmp.color = stage1Complete ? stage2ActiveColor : dimColor;
        }

        public void ShowCompletion(string completedTitle, string nextTitle, System.Action onNext)
        {
            ClearChildren();
            BuildPanel();
            _completionTmp = SpawnText("Completion",
                $"✓ {completedTitle}\n\nNext:\n{nextTitle}",
                Vector2.zero,
                completionSize, completionColor, TextAlignmentOptions.Center,
                new Vector2(panelSize.x - 2f * padding, panelSize.y - 2f * padding - 70f),
                TextAnchor.MiddleCenter);
            _nextButton = SpawnButton("NextButton", "Next",
                new Vector2(0f, -(panelSize.y - 72f)),
                new Vector2(180f, 52f),
                onNext);
        }

        public void ShowNextButton(System.Action onNext)
        {
            if (_level == null)
                return;

            if (_nextButton != null)
            {
                _nextButton.onClick.RemoveAllListeners();
                if (onNext != null) _nextButton.onClick.AddListener(() => onNext());
                _nextButton.gameObject.SetActive(true);
                return;
            }

            _nextButton = SpawnButton("NextButton", "Next",
                new Vector2(0f, -(panelSize.y - 72f)),
                new Vector2(180f, 52f),
                onNext);
        }

        public void ShowStatus(string message, Color color, float duration = 2f)
        {
            if (_level == null) return;
            if (_statusTmp == null)
            {
                _statusTmp = SpawnText("Status", message,
                    new Vector2(0f, -(panelSize.y - padding * 2f - stage2Size * 4f)),
                    rowSize * 0.9f, color, TextAlignmentOptions.Center,
                    new Vector2(panelSize.x - 2f * padding, rowHeight * 1.4f),
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
        }

        private void BuildTitle(string title)
        {
            float y = -padding - titleSize * 0.6f;
            _titleTmp = SpawnText("Title", title,
                new Vector2(10f, y),
                titleSize, titleColor, TextAlignmentOptions.Center,
                new Vector2(panelSize.x - 2f * padding - resetButtonSize.x - 18f, titleSize * 1.5f),
                TextAnchor.UpperCenter);
        }

        private void BuildStage1(IReadOnlyList<ReactionRecipeSO.CompoundCount> stage1, IReadOnlyDictionary<CompoundSO, int> built)
        {
            float topY = -padding - titleSize * 1.9f;
            for (int i = 0; i < stage1.Count; i++)
            {
                var s = stage1[i];
                int have = 0;
                if (s.compound != null && built != null) built.TryGetValue(s.compound, out have);

                float y = topY - i * rowHeight;
                var tmp = SpawnText($"Stage1_{i}", FormatRow(s, have),
                    new Vector2(0f, y),
                    rowSize,
                    have >= s.count ? stage2ActiveColor : rowColor,
                    TextAlignmentOptions.Left,
                    new Vector2(panelSize.x - 2f * padding, rowHeight),
                    TextAnchor.UpperLeft);
                _stage1Rows.Add(tmp);
            }
        }

        private void BuildStage2(ReactionRecipeSO stage2, bool stage1Complete)
        {
            if (stage2 == null) return;
            string equation = FormatRecipe(stage2);
            float y = -(panelSize.y - padding * 2f - stage2Size * 1.8f);
            _stage2Tmp = SpawnText("Stage2", equation,
                new Vector2(0f, y),
                stage2Size,
                stage1Complete ? stage2ActiveColor : dimColor,
                TextAlignmentOptions.Center,
                new Vector2(panelSize.x - 2f * padding, stage2Size * 2.2f),
                TextAnchor.UpperCenter);
        }

        private void BuildResetButton()
        {
            _resetButton = SpawnButton("ResetButton", "RESET",
                new Vector2(panelSize.x * 0.5f - padding - resetButtonSize.x * 0.5f, -padding),
                resetButtonSize,
                _onReset,
                new Color(0.95f, 0.45f, 0.35f, 1f));
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
            tmp.enableWordWrapping = true;
            tmp.fontSize = fontSize;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
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

            var button = go.GetComponent<Button>();
            if (onClick != null) button.onClick.AddListener(() => onClick());

            var labelTmp = SpawnText("Label", label, Vector2.zero, buttonTextSize, Color.black,
                TextAlignmentOptions.Center, size, TextAnchor.MiddleCenter);
            if (labelTmp != null) labelTmp.transform.SetParent(go.transform, false);

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

            _canvas = GetComponentInParent<Canvas>();
            if (_canvas == null) _canvas = FindFirstObjectByType<Canvas>();
            if (_canvas == null)
            {
                Debug.LogWarning("[LevelObjectiveUI] No Canvas found in scene for objective UI.", this);
                return false;
            }

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

            _panelRoot.anchorMin = new Vector2(1f, 1f);
            _panelRoot.anchorMax = new Vector2(1f, 1f);
            _panelRoot.pivot = new Vector2(1f, 1f);
            _panelRoot.anchoredPosition = anchoredPosition;
            _panelRoot.sizeDelta = panelSize;
            return true;
        }

        private void MigrateLegacyWorldSpaceValues()
        {
            // Older scene data stored meter-sized values for a 3D world panel.
            // If we detect that layout, swap to readable canvas-space defaults.
            if (panelSize.x <= 5f && panelSize.y <= 5f)
                panelSize = new Vector2(520f, 360f);

            if (padding <= 2f)
                padding = 24f;

            if (rowHeight <= 2f)
                rowHeight = 38f;

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
            string check = have >= target.count ? "✓" : "□";
            string label = target.compound != null ? target.compound.Formula : "?";
            return $"  {check}  {target.count} × {label}   ({have}/{target.count})";
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
