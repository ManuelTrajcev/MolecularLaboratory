using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MolecularLab.Managers
{
    public class SceneFadeManager : MonoBehaviour
    {
        public static SceneFadeManager Instance { get; private set; }

        [SerializeField, Min(0f)] private float fadeOutDuration = 0.35f;
        [SerializeField, Min(0f)] private float fadeInDuration = 0.35f;
        [SerializeField] private float fadeDistance = 0.25f;
        [SerializeField] private Color fadeColor = Color.black;

        private GameObject _fadePlane;
        private Renderer _fadeRenderer;
        private Material _fadeMaterial;
        private Coroutine _transitionRoutine;

        public static void LoadScene(string sceneName)
        {
            EnsureInstance().LoadSceneWithFade(sceneName);
        }

        private static SceneFadeManager EnsureInstance()
        {
            if (Instance != null)
                return Instance;

            var go = new GameObject("SceneFadeManager");
            return go.AddComponent<SceneFadeManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureFadePlane();
            SetFadeAlpha(0f);
            _fadePlane.SetActive(false);
        }

        private void LoadSceneWithFade(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return;

            if (_transitionRoutine != null)
                StopCoroutine(_transitionRoutine);

            _transitionRoutine = StartCoroutine(Transition(sceneName));
        }

        private IEnumerator Transition(string sceneName)
        {
            EnsureFadePlane();
            AttachToCamera();
            _fadePlane.SetActive(true);

            yield return Fade(0f, 1f, fadeOutDuration);

            KeepFadePlaneAliveDuringLoad();
            var load = SceneManager.LoadSceneAsync(sceneName);
            while (load != null && !load.isDone)
                yield return null;

            yield return WaitForSceneCamera();

            yield return Fade(1f, 0f, fadeInDuration);

            _fadePlane.SetActive(false);
            _transitionRoutine = null;
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                SetFadeAlpha(to);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetFadeAlpha(Mathf.Lerp(from, to, t));
                yield return null;
            }

            SetFadeAlpha(to);
        }

        private void EnsureFadePlane()
        {
            if (_fadePlane != null && _fadeRenderer != null)
                return;

            _fadePlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _fadePlane.name = "SceneFadePlane";
            DontDestroyOnLoad(_fadePlane);

            var collider = _fadePlane.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            _fadeRenderer = _fadePlane.GetComponent<Renderer>();
            _fadeMaterial = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard"));
            _fadeMaterial.name = "Runtime Scene Fade Material";
            _fadeMaterial.renderQueue = 5000;
            _fadeRenderer.sharedMaterial = _fadeMaterial;
            _fadeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _fadeRenderer.receiveShadows = false;
        }

        private bool AttachToCamera()
        {
            Camera cam = Camera.main;
            if (cam == null || _fadePlane == null)
                cam = FindFirstObjectByType<Camera>(FindObjectsInactive.Exclude);
            if (cam == null || _fadePlane == null)
                return false;

            _fadePlane.transform.SetParent(cam.transform, false);
            float distance = Mathf.Max(fadeDistance, cam.nearClipPlane + 0.01f);
            _fadePlane.transform.localPosition = new Vector3(0f, 0f, distance);
            _fadePlane.transform.localRotation = Quaternion.identity;

            float height = 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float width = height * cam.aspect;
            _fadePlane.transform.localScale = new Vector3(width, height, 1f);
            return true;
        }

        private void KeepFadePlaneAliveDuringLoad()
        {
            if (_fadePlane == null)
                return;

            _fadePlane.transform.SetParent(transform, true);
            DontDestroyOnLoad(_fadePlane);
        }

        private IEnumerator WaitForSceneCamera()
        {
            const int maxAttempts = 30;
            for (int i = 0; i < maxAttempts; i++)
            {
                if (AttachToCamera())
                    yield break;

                yield return null;
            }
        }

        private void SetFadeAlpha(float alpha)
        {
            if (_fadeMaterial == null)
                return;

            Color color = fadeColor;
            color.a = Mathf.Clamp01(alpha);
            if (_fadeMaterial.HasProperty("_BaseColor"))
                _fadeMaterial.SetColor("_BaseColor", color);

            if (_fadeMaterial.HasProperty("_Color"))
                _fadeMaterial.SetColor("_Color", color);
        }
    }
}
