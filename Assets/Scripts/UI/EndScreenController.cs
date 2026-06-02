using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MolecularLab.UI
{
    public class EndScreenController : MonoBehaviour
    {
        [SerializeField] private GameObject endScreenCanvas;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private string laboratorySceneName = "Laboratory - Updated";

        public static EndScreenController Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            if (endScreenCanvas != null)
                endScreenCanvas.SetActive(false);
        }

        private void Start()
        {
            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestart);

            if (exitButton != null)
                exitButton.onClick.AddListener(OnExit);
        }

        private void OnDestroy()
        {
            if (restartButton != null) restartButton.onClick.RemoveAllListeners();
            if (exitButton != null) exitButton.onClick.RemoveAllListeners();
            if (Instance == this) Instance = null;
        }

        public void Show()
        {
            if (endScreenCanvas != null)
                endScreenCanvas.SetActive(true);
        }

        private void OnRestart()
        {
            SceneManager.LoadScene(laboratorySceneName);
        }

        private void OnExit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
