using UnityEngine;
using UnityEngine.InputSystem;

namespace MolecularLab.Managers
{
    public class CameraZoomController : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField, Range(10f, 120f)] private float normalFieldOfView = 60f;
        [SerializeField, Range(10f, 80f)] private float zoomFieldOfView = 24f;
        [SerializeField, Min(0.1f)] private float transitionSpeed = 10f;
        [SerializeField] private bool useCurrentCameraFovAsNormal = true;
        [SerializeField] private bool debugLog = false;

        private InputAction _zoomAction;
        private bool _wasZooming;

        private void Awake()
        {
            _zoomAction = new InputAction("Hold Right Secondary Zoom", InputActionType.Button);
            _zoomAction.AddBinding("<XRController>{RightHand}/secondaryButton");
        }

        private void Start()
        {
            ResolveCamera();
            if (targetCamera != null && useCurrentCameraFovAsNormal)
                normalFieldOfView = targetCamera.fieldOfView;
        }

        private void OnEnable()
        {
            _zoomAction?.Enable();
        }

        private void OnDisable()
        {
            _zoomAction?.Disable();
            if (targetCamera != null)
                targetCamera.fieldOfView = normalFieldOfView;
        }

        private void OnDestroy()
        {
            _zoomAction?.Dispose();
            _zoomAction = null;
        }

        private void Update()
        {
            ResolveCamera();
            if (targetCamera == null)
                return;

            bool zooming = IsZoomHeld();
            float targetFov = zooming ? zoomFieldOfView : normalFieldOfView;
            float t = 1f - Mathf.Exp(-transitionSpeed * Time.deltaTime);
            targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, targetFov, t);

            if (debugLog && zooming != _wasZooming)
                Debug.Log($"[CameraZoom] {(zooming ? "Zoom in" : "Zoom out")} FOV={targetFov}", this);

            _wasZooming = zooming;
        }

        private bool IsZoomHeld()
        {
            bool controllerHeld = _zoomAction != null && _zoomAction.ReadValue<float>() > 0.5f;
            var keyboard = Keyboard.current;
            bool simulatorHeld = keyboard != null
                && (keyboard.digit2Key.isPressed || keyboard.numpad2Key.isPressed)
                && !keyboard.leftShiftKey.isPressed
                && !keyboard.rightShiftKey.isPressed;

            return controllerHeld || simulatorHeld;
        }

        private void ResolveCamera()
        {
            if (targetCamera != null)
                return;

            targetCamera = Camera.main;
        }
    }
}
