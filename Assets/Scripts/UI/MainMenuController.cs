using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using MolecularLab.Managers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MolecularLab.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string laboratorySceneName = "Laboratory - new models";
        [SerializeField] private GameObject instructionsPanel;
        [SerializeField] private Button enterLabButton;
        [SerializeField] private Button instructionsButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Button closeInstructionsButton;
        [SerializeField, Min(0.1f)] private float keyboardRayDistance = 20f;
        [SerializeField] private Color instructionsPanelColor = new Color(0.06f, 0.08f, 0.12f, 0.94f);
        [SerializeField] private Color instructionsTextColor = Color.white;
        [SerializeField] private Color closeButtonColor = new Color(0.58f, 0.82f, 1f, 0.6f);

        private TextMeshProUGUI _instructionsControlsLabel;
        private bool _instructionsPanelBuilt;

        private void Start()
        {
            EnsureInstructionsPanelLayout();
            if (instructionsPanel != null)
                instructionsPanel.SetActive(false);

            enterLabButton.onClick.AddListener(OnEnterLab);
            instructionsButton.onClick.AddListener(OnShowInstructions);
            exitButton.onClick.AddListener(OnExit);
            closeInstructionsButton.onClick.AddListener(OnCloseInstructions);

            MakeXRSelectable(enterLabButton);
            MakeXRSelectable(instructionsButton);
            MakeXRSelectable(exitButton);
            MakeXRSelectable(closeInstructionsButton);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
                TryPressButtonInView();

            if (instructionsPanel != null && instructionsPanel.activeSelf)
                UpdateInstructionsControlsText();
        }

        private void OnEnterLab()
        {
            SceneFadeManager.LoadScene(laboratorySceneName);
        }

        private void OnShowInstructions()
        {
            EnsureInstructionsPanelLayout();
            UpdateInstructionsControlsText();
            if (instructionsPanel != null)
                instructionsPanel.SetActive(true);
        }

        private void OnCloseInstructions()
        {
            if (instructionsPanel != null)
                instructionsPanel.SetActive(false);
        }

        private void OnExit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void EnsureInstructionsPanelLayout()
        {
            if (_instructionsPanelBuilt || instructionsPanel == null)
                return;

            var root = instructionsPanel.GetComponent<RectTransform>();
            if (root == null)
                return;

            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = Vector2.zero;
            Vector2 panelSize = new Vector2(1700f, 700f);
            root.sizeDelta = panelSize;

            var image = instructionsPanel.GetComponent<Image>();
            if (image != null)
            {
                image.color = instructionsPanelColor;
                image.raycastTarget = true;
            }

            RemoveLegacyInstructionChildren(root);

            const float panelTextSize = 24f;
            float halfH = panelSize.y * 0.5f;
            float halfW = panelSize.x * 0.5f;

            SpawnInstructionsText("InfoTitle", "Instructions", root,
                new Vector2(0f, halfH - 50f), new Vector2(panelSize.x - 180f, 56f),
                panelTextSize * 1.5f, TextAlignmentOptions.Center);

            SpawnInstructionsText("InfoWelcome", LaboratoryInstructionContent.WelcomeText, root,
                new Vector2(0f, halfH - 104f), new Vector2(panelSize.x - 180f, 48f),
                panelTextSize, TextAlignmentOptions.Center);

            float columnWidth = halfW - 120f;
            float columnX = panelSize.x * 0.25f;
            Vector2 columnSize = new Vector2(columnWidth, panelSize.y - 220f);
            float columnY = -10f;

            SpawnInstructionsText("InfoGameplay", LaboratoryInstructionContent.GameplayText, root,
                new Vector2(-columnX, columnY), columnSize,
                panelTextSize, TextAlignmentOptions.TopLeft);

            _instructionsControlsLabel = SpawnInstructionsText("InfoControls", LaboratoryInstructionContent.GetActiveControlsText(), root,
                new Vector2(columnX, columnY), columnSize,
                panelTextSize, TextAlignmentOptions.TopLeft);

            RestyleCloseInstructionsButton(root, panelSize);
            _instructionsPanelBuilt = true;
        }

        private void RemoveLegacyInstructionChildren(RectTransform root)
        {
            var childrenToRemove = new List<GameObject>();
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (closeInstructionsButton != null && child == closeInstructionsButton.transform)
                    continue;

                childrenToRemove.Add(child.gameObject);
            }

            foreach (GameObject child in childrenToRemove)
                Destroy(child);
        }

        private TextMeshProUGUI SpawnInstructionsText(string objectName, string text, RectTransform parent,
                                                      Vector2 anchoredPos, Vector2 size, float fontSize,
                                                      TextAlignmentOptions alignment)
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
            tmp.color = instructionsTextColor;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Truncate;
            tmp.raycastTarget = false;
            return tmp;
        }

        private void RestyleCloseInstructionsButton(RectTransform panelRoot, Vector2 panelSize)
        {
            if (closeInstructionsButton == null)
                return;

            Transform closeTransform = closeInstructionsButton.transform;
            closeTransform.SetParent(panelRoot, false);

            var closeRt = closeInstructionsButton.GetComponent<RectTransform>();
            if (closeRt != null)
            {
                closeRt.anchorMin = new Vector2(0.5f, 0.5f);
                closeRt.anchorMax = new Vector2(0.5f, 0.5f);
                closeRt.pivot = new Vector2(0.5f, 0.5f);
                closeRt.anchoredPosition = new Vector2(0f, -panelSize.y * 0.5f + 52f);
                closeRt.sizeDelta = new Vector2(220f, 72f);
            }

            var image = closeInstructionsButton.GetComponent<Image>();
            if (image != null)
                image.color = closeButtonColor;

            var label = closeInstructionsButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = "Close";
                label.color = Color.black;
                label.fontSize = 30f;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
            }
        }

        private void UpdateInstructionsControlsText()
        {
            if (_instructionsControlsLabel != null)
                _instructionsControlsLabel.text = LaboratoryInstructionContent.GetActiveControlsText();
        }

        private static void MakeXRSelectable(Button button)
        {
            if (button == null)
                return;

            var rect = button.GetComponent<RectTransform>();
            if (rect == null)
                return;

            var collider = button.GetComponent<BoxCollider>();
            if (collider == null)
                collider = button.gameObject.AddComponent<BoxCollider>();

            collider.center = Vector3.zero;
            collider.size = new Vector3(rect.sizeDelta.x, rect.sizeDelta.y, 24f);
            collider.isTrigger = false;

            var interactable = button.GetComponent<XRSimpleInteractable>();
            if (interactable == null)
                interactable = button.gameObject.AddComponent<XRSimpleInteractable>();

            interactable.selectEntered.AddListener(_ =>
            {
                if (ShouldInvokeSelectableButton(button))
                    button.onClick.Invoke();
            });
        }

        private static bool ShouldInvokeSelectableButton(Button button)
        {
            if (button == null || !button.IsActive() || !button.interactable)
                return false;

            if (!LaboratoryInstructionContent.IsMouseControlCameraActive())
                return true;

            Camera cam = Camera.main;
            if (cam == null)
                return false;

            var ray = new Ray(cam.transform.position, cam.transform.forward);
            if (!Physics.Raycast(ray, out var hit, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
                return false;

            return hit.collider != null && hit.collider.GetComponentInParent<Button>() == button;
        }

        private void TryPressButtonInView()
        {
            Camera cam = Camera.main;
            if (cam == null)
                return;

            var ray = new Ray(cam.transform.position, cam.transform.forward);
            if (!Physics.Raycast(ray, out var hit, keyboardRayDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
                return;

            var button = hit.collider.GetComponentInParent<Button>();
            if (button == null || !button.IsActive() || !button.interactable)
                return;

            button.onClick.Invoke();
        }

        private void OnDestroy()
        {
            if (enterLabButton != null) enterLabButton.onClick.RemoveAllListeners();
            if (instructionsButton != null) instructionsButton.onClick.RemoveAllListeners();
            if (exitButton != null) exitButton.onClick.RemoveAllListeners();
            if (closeInstructionsButton != null) closeInstructionsButton.onClick.RemoveAllListeners();
        }
    }
}
