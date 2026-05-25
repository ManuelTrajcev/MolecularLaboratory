using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

        private void Start()
        {
            instructionsPanel.SetActive(false);

            enterLabButton.onClick.AddListener(OnEnterLab);
            instructionsButton.onClick.AddListener(OnShowInstructions);
            exitButton.onClick.AddListener(OnExit);
            closeInstructionsButton.onClick.AddListener(OnCloseInstructions);
        }

        private void OnEnterLab()
        {
            SceneManager.LoadScene(laboratorySceneName);
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

        private void OnDestroy()
        {
            enterLabButton.onClick.RemoveAllListeners();
            instructionsButton.onClick.RemoveAllListeners();
            exitButton.onClick.RemoveAllListeners();
            closeInstructionsButton.onClick.RemoveAllListeners();
        }
    }
}
