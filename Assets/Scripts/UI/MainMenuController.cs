using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using MolecularLab.Managers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MolecularLab.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string laboratorySceneName = "Laboratory - Updated";
        [SerializeField] private GameObject instructionsPanel;
        [SerializeField] private Button enterLabButton;
        [SerializeField] private Button instructionsButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Button closeInstructionsButton;
        [SerializeField, Min(0.1f)] private float keyboardRayDistance = 20f;

        private void Start()
        {
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
        }

        private void OnEnterLab()
        {
            SceneFadeManager.LoadScene(laboratorySceneName);
        }

        private void OnShowInstructions()
        {
            instructionsPanel.SetActive(true);
        }

        private void OnCloseInstructions()
        {
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
                if (button != null && button.IsActive() && button.interactable)
                    button.onClick.Invoke();
            });
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
