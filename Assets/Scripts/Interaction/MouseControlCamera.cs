using System.Collections.Generic;
using MolecularLab.Chemistry;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace MolecularLab.Interaction
{
    [DisallowMultipleComponent]
    public class MouseControlCamera : MonoBehaviour
    {
        [Header("Camera Movement")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float lookSensitivity = 0.2f;
        [SerializeField, Min(1f)] private float sprintMultiplier = 2f;
        [SerializeField, Range(0.05f, 1f)] private float precisionMultiplier = 0.35f;

        [Header("Interaction Settings")]
        [SerializeField, Min(0.1f)] private float reachDistance = 20f;
        [SerializeField, Min(0.01f)] private float minHoldDistance = 0.15f;
        [SerializeField, Min(0.01f)] private float maxHoldDistance = 20f;
        [SerializeField, Min(0.01f)] private float scrollDistanceSpeed = 0.35f;
        [SerializeField, Min(0.01f)] private float heldKeyboardMoveSpeed = 3f;
        [SerializeField, Min(1f)] private float heldRotationSensitivity = 0.35f;
        [SerializeField, Min(0.001f)] private float deleteRayRadius = 0.035f;
        [SerializeField] private LayerMask interactionMask = ~0;

        [Header("Zoom")]
        [SerializeField, Range(10f, 120f)] private float normalFieldOfView = 60f;
        [SerializeField, Range(10f, 80f)] private float zoomFieldOfView = 28f;
        [SerializeField, Min(0.1f)] private float zoomTransitionSpeed = 10f;
        [SerializeField] private bool useCurrentCameraFovAsNormal = true;

        [Header("Crosshair")]
        [SerializeField] private bool showCrosshair = true;
        [SerializeField, Min(4f)] private float crosshairSize = 24f;
        [SerializeField, Min(1f)] private float crosshairThickness = 3f;
        [SerializeField] private Color crosshairColor = new Color(0f, 1f, 0.2f, 0.95f);
        [SerializeField] private int crosshairSortingOrder = 32000;

        [Header("Debug")]
        [SerializeField] private bool debugLog = false;

        private static Sprite _crosshairSprite;

        private Camera _camera;
        private XRInteractionManager _interactionManager;
        private XRRayInteractor _rayInteractor;
        private Canvas _crosshairCanvas;
        private Image _crosshairImage;
        private Transform _rayOrigin;
        private Transform _attachTransform;
        private XRBaseInteractable _selectedInteractable;
        private float _holdDistance;
        private Vector3 _holdLocalOffset;
        private Vector2 _rotation;
        private Quaternion _attachRotation;
        private bool _cursorLocked;
        private bool _ignoreSelectionThisFrame;

        private void Awake()
        {
            _camera = GetComponent<Camera>() ?? Camera.main;
            if (_camera != null && useCurrentCameraFovAsNormal)
                normalFieldOfView = _camera.fieldOfView;

            Vector3 euler = transform.eulerAngles;
            _rotation = new Vector2(euler.y, NormalizePitch(euler.x));
            _attachRotation = transform.rotation;
            _holdDistance = Mathf.Clamp(reachDistance * 0.25f, minHoldDistance, maxHoldDistance);
            _holdLocalOffset = Vector3.forward * _holdDistance;

            EnsureInteractionRig();
            EnsureCrosshair();
        }

        private void OnEnable()
        {
            LockCursor();
            SetCrosshairVisible(showCrosshair);
        }

        private void OnDisable()
        {
            EndSelection();
            UnlockCursor();
            SetCrosshairVisible(false);
        }

        private void Update()
        {
            ResolveDevices(out var keyboard, out var mouse);
            if (keyboard == null || mouse == null)
                return;

            _ignoreSelectionThisFrame = false;
            HandleCursorLock(keyboard, mouse);
            HandleLook(keyboard, mouse);
            HandleMovement(keyboard);
            HandleZoom(keyboard);
            UpdateInteractorPose(keyboard, mouse);
            HandleDelete(keyboard);
            HandleSelection(mouse);
        }

        private void EnsureInteractionRig()
        {
            _interactionManager = FindFirstObjectByType<XRInteractionManager>();
            if (_interactionManager == null)
            {
                var managerObject = new GameObject("XR Interaction Manager");
                _interactionManager = managerObject.AddComponent<XRInteractionManager>();
                if (debugLog) Debug.Log("[MousePC] Created XR Interaction Manager.", this);
            }

            var interactorObject = new GameObject("Desktop Mouse XR Ray Interactor");
            interactorObject.hideFlags = HideFlags.HideInHierarchy;
            interactorObject.transform.SetParent(transform, false);

            _rayOrigin = new GameObject("Ray Origin").transform;
            _rayOrigin.SetParent(interactorObject.transform, false);

            _attachTransform = new GameObject("Attach Transform").transform;
            _attachTransform.SetParent(interactorObject.transform, false);

            _rayInteractor = interactorObject.AddComponent<XRRayInteractor>();
            _rayInteractor.interactionManager = _interactionManager;
            _rayInteractor.handedness = InteractorHandedness.None;
            _rayInteractor.rayOriginTransform = _rayOrigin;
            _rayInteractor.attachTransform = _attachTransform;
            _rayInteractor.lineType = XRRayInteractor.LineType.StraightLine;
            _rayInteractor.hitDetectionType = XRRayInteractor.HitDetectionType.SphereCast;
            _rayInteractor.maxRaycastDistance = reachDistance;
            _rayInteractor.raycastMask = interactionMask;
            _rayInteractor.raycastTriggerInteraction = QueryTriggerInteraction.Collide;
            _rayInteractor.useForceGrab = true;
            _rayInteractor.manipulateAttachTransform = false;
            _rayInteractor.enableUIInteraction = true;
            _rayInteractor.blockInteractionsWithScreenSpaceUI = false;
            _rayInteractor.blockUIOnInteractableSelection = false;

            UpdateInteractorPose(null, null);
        }

        private void EnsureCrosshair()
        {
            if (!showCrosshair || _crosshairCanvas != null)
                return;

            var canvasObject = new GameObject("Desktop Mouse Crosshair", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.hideFlags = HideFlags.HideInHierarchy;
            canvasObject.transform.SetParent(transform, false);

            _crosshairCanvas = canvasObject.GetComponent<Canvas>();
            _crosshairCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _crosshairCanvas.overrideSorting = true;
            _crosshairCanvas.sortingOrder = crosshairSortingOrder;
            _crosshairCanvas.pixelPerfect = true;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            var imageObject = new GameObject("Crosshair Ring", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(canvasObject.transform, false);

            var rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(crosshairSize, crosshairSize);

            _crosshairImage = imageObject.GetComponent<Image>();
            _crosshairImage.sprite = GetCrosshairSprite();
            _crosshairImage.type = Image.Type.Simple;
            _crosshairImage.color = crosshairColor;
            _crosshairImage.raycastTarget = false;
        }

        private void SetCrosshairVisible(bool visible)
        {
            if (_crosshairCanvas != null)
                _crosshairCanvas.gameObject.SetActive(visible);
        }

        private static void ResolveDevices(out Keyboard keyboard, out Mouse mouse)
        {
            keyboard = Keyboard.current;
            mouse = Mouse.current;
        }

        private void HandleCursorLock(Keyboard keyboard, Mouse mouse)
        {
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                UnlockCursor();
                return;
            }

            if (!_cursorLocked && mouse.leftButton.wasPressedThisFrame)
            {
                LockCursor();
                _ignoreSelectionThisFrame = true;
            }
        }

        private void HandleLook(Keyboard keyboard, Mouse mouse)
        {
            if (!_cursorLocked)
                return;

            if (_selectedInteractable != null && keyboard.rKey.isPressed)
                return;

            Vector2 delta = mouse.delta.ReadValue();
            _rotation.x += delta.x * lookSensitivity;
            _rotation.y -= delta.y * lookSensitivity;
            _rotation.y = Mathf.Clamp(_rotation.y, -89f, 89f);
            transform.rotation = Quaternion.Euler(_rotation.y, _rotation.x, 0f);
        }

        private void HandleMovement(Keyboard keyboard)
        {
            if (HeldMoveModifierActive(keyboard))
            {
                HandleHeldKeyboardMove(keyboard);
                return;
            }

            Vector3 move = Vector3.zero;
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;

            if (keyboard.wKey.isPressed) move += forward;
            if (keyboard.sKey.isPressed) move -= forward;
            if (keyboard.aKey.isPressed) move -= right;
            if (keyboard.dKey.isPressed) move += right;
            if (keyboard.spaceKey.isPressed) move += Vector3.up;
            if (keyboard.cKey.isPressed) move -= Vector3.up;

            if (move.sqrMagnitude > 1f)
                move.Normalize();

            float speed = moveSpeed;
            if (keyboard.leftShiftKey.isPressed)
                speed *= sprintMultiplier;
            if (keyboard.leftCtrlKey.isPressed)
                speed *= precisionMultiplier;

            transform.position += move * speed * Time.deltaTime;
        }

        private bool HeldMoveModifierActive(Keyboard keyboard)
        {
            return _selectedInteractable != null
                && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
        }

        private void HandleHeldKeyboardMove(Keyboard keyboard)
        {
            Vector3 direction = Vector3.zero;
            if (keyboard.wKey.isPressed) direction += Vector3.forward;
            if (keyboard.sKey.isPressed) direction -= Vector3.forward;
            if (keyboard.dKey.isPressed) direction += Vector3.right;
            if (keyboard.aKey.isPressed) direction -= Vector3.right;

            if (direction.sqrMagnitude <= 0f)
                return;

            if (direction.sqrMagnitude > 1f)
                direction.Normalize();

            _holdLocalOffset += direction * heldKeyboardMoveSpeed * Time.deltaTime;
            _holdLocalOffset.z = Mathf.Clamp(_holdLocalOffset.z, minHoldDistance, maxHoldDistance);
        }

        private void HandleZoom(Keyboard keyboard)
        {
            if (_camera == null)
                return;

            bool zooming = keyboard.zKey.isPressed;
            float targetFov = zooming ? zoomFieldOfView : normalFieldOfView;
            float t = 1f - Mathf.Exp(-zoomTransitionSpeed * Time.deltaTime);
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, targetFov, t);
        }

        private void UpdateInteractorPose(Keyboard keyboard, Mouse mouse)
        {
            if (_rayOrigin == null || _attachTransform == null)
                return;

            _rayOrigin.position = transform.position;
            _rayOrigin.rotation = transform.rotation;

            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    _holdDistance = Mathf.Clamp(_holdDistance + scroll * scrollDistanceSpeed * 0.01f, minHoldDistance, maxHoldDistance);
                    _holdLocalOffset.z = _holdDistance;
                }
            }

            if (_selectedInteractable == null)
                _attachRotation = transform.rotation;

            if (keyboard != null && mouse != null && keyboard.rKey.isPressed && _selectedInteractable != null)
            {
                Vector2 delta = mouse.delta.ReadValue();
                Quaternion yaw = Quaternion.AngleAxis(delta.x * heldRotationSensitivity, transform.up);
                Quaternion pitch = Quaternion.AngleAxis(-delta.y * heldRotationSensitivity, transform.right);
                _attachRotation = yaw * pitch * _attachRotation;
            }

            _holdDistance = Mathf.Clamp(_holdLocalOffset.z, minHoldDistance, maxHoldDistance);
            _holdLocalOffset.z = _holdDistance;
            _attachTransform.position = transform.TransformPoint(_holdLocalOffset);
            _attachTransform.rotation = _attachRotation;
        }

        private void HandleDelete(Keyboard keyboard)
        {
            if (!keyboard.xKey.wasPressedThisFrame || _selectedInteractable != null)
                return;

            Atom atom = FindAimedAtom();
            if (atom == null || IsStagedInChamber(atom))
                return;

            DeleteAtom(atom);
        }

        private void HandleSelection(Mouse mouse)
        {
            if (!_ignoreSelectionThisFrame && mouse.leftButton.wasPressedThisFrame && _cursorLocked && _selectedInteractable == null)
                TryBeginSelection();

            if (mouse.leftButton.wasReleasedThisFrame && _selectedInteractable != null)
                EndSelection();
        }

        private void TryBeginSelection()
        {
            if (!TryFindInteractable(out var interactable, out var hit))
                return;

            if (interactable is not IXRSelectInteractable selectInteractable)
                return;

            _holdDistance = Mathf.Clamp(hit.distance, minHoldDistance, maxHoldDistance);
            _holdLocalOffset = Vector3.forward * _holdDistance;
            _attachRotation = transform.rotation;
            _attachTransform.position = transform.TransformPoint(_holdLocalOffset);
            _attachTransform.rotation = _attachRotation;

            _selectedInteractable = interactable;
            _rayInteractor.StartManualInteraction(selectInteractable);

            if (!_rayInteractor.IsSelecting(selectInteractable))
            {
                if (debugLog) Debug.LogWarning($"[MousePC] Selection prevented for {interactable.name}.", interactable);
                _selectedInteractable = null;
            }
        }

        private void EndSelection()
        {
            if (_selectedInteractable == null || _rayInteractor == null)
                return;

            _rayInteractor.EndManualInteraction();
            _selectedInteractable = null;
        }

        private bool TryFindInteractable(out XRBaseInteractable interactable, out RaycastHit bestHit)
        {
            interactable = null;
            bestHit = default;

            Ray ray = new Ray(transform.position, transform.forward);
            var hits = Physics.SphereCastAll(ray, deleteRayRadius, reachDistance, interactionMask, QueryTriggerInteraction.Collide);
            if (hits == null || hits.Length == 0)
                return false;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hitCollider = hits[i].collider;
                if (hitCollider == null)
                    continue;

                var candidate = hitCollider.GetComponentInParent<XRBaseInteractable>();
                if (candidate == null || !candidate.isActiveAndEnabled)
                    continue;

                interactable = candidate;
                bestHit = hits[i];
                return true;
            }

            return false;
        }

        private Atom FindAimedAtom()
        {
            Ray ray = new Ray(transform.position, transform.forward);
            var hits = Physics.SphereCastAll(ray, deleteRayRadius, reachDistance, interactionMask, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
                return null;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                var atom = hits[i].collider != null ? hits[i].collider.GetComponentInParent<Atom>() : null;
                if (atom != null)
                    return atom;
            }

            return null;
        }

        private static bool IsStagedInChamber(Atom atom)
        {
            var bigChamber = FindFirstObjectByType<ReactionChamber>();
            if (bigChamber != null && bigChamber.IsAtomStaged(atom))
                return true;

            var smallChamber = FindFirstObjectByType<SmallMoleculeChamber>();
            return smallChamber != null && smallChamber.IsAtomStaged(atom);
        }

        private void DeleteAtom(Atom atom)
        {
            var bonds = new List<Bond>(atom.Bonds);
            for (int i = 0; i < bonds.Count; i++)
            {
                if (bonds[i] != null)
                    bonds[i].BreakImmediately();
            }

            if (debugLog) Debug.Log($"[MousePC] Deleted {atom.name}.", atom);
            Destroy(atom.gameObject);
        }

        private void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _cursorLocked = true;
        }

        private void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _cursorLocked = false;
        }

        private static float NormalizePitch(float xEuler)
        {
            return xEuler > 180f ? xEuler - 360f : xEuler;
        }

        private Sprite GetCrosshairSprite()
        {
            if (_crosshairSprite != null)
                return _crosshairSprite;

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Runtime_DesktopMouseCrosshair",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float center = (size - 1) * 0.5f;
            float radius = size * 0.32f;
            float halfThickness = Mathf.Clamp(crosshairThickness / Mathf.Max(1f, crosshairSize) * size * 0.5f, 1.5f, 8f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float edgeAlpha = 1f - Mathf.Clamp01(Mathf.Abs(distance - radius) - halfThickness + 1f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, edgeAlpha));
                }
            }

            texture.Apply(false, true);
            _crosshairSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            return _crosshairSprite;
        }
    }
}
