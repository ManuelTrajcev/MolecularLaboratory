using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class MouseInteractionController : MonoBehaviour
{
    [Header("Camera Movement")]
    public float moveSpeed = 5f;
    public float lookSensitivity = 0.15f;

    [Header("Interaction Settings")]
    public float reachDistance = 20f;

    private XRGrabInteractable _grabbedObject;
    private float _grabDistance;
    private Vector2 _rotation;

    void Start()
    {
        // Start with the cursor locked for a cleaner FPS feel, or toggle it later
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleCamera();
        HandleMouseInput();
    }

    void HandleCamera()
    {
        // WASD Movement
        Vector3 move = Vector3.zero;
        if (Keyboard.current.wKey.isPressed) move += transform.forward;
        if (Keyboard.current.sKey.isPressed) move -= transform.forward;
        if (Keyboard.current.aKey.isPressed) move -= transform.right;
        if (Keyboard.current.dKey.isPressed) move += transform.right;
        transform.position += move * moveSpeed * Time.deltaTime;

        // Right-Click to Look (only rotate when holding right mouse)
        if (Mouse.current.rightButton.isPressed)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Vector2 delta = Mouse.current.delta.ReadValue();
            _rotation.x += delta.x * lookSensitivity;
            _rotation.y -= delta.y * lookSensitivity;
            _rotation.y = Mathf.Clamp(_rotation.y, -90f, 90f);
            transform.localRotation = Quaternion.Euler(_rotation.y, _rotation.x, 0);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }

    void HandleMouseInput()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, reachDistance))
            {
                // 1. Periodic Table Buttons
                var simple = hit.collider.GetComponentInParent<XRSimpleInteractable>();
                if (simple != null)
                {
                    // Using default/empty args to prevent null refs in listeners
                    var args = new SelectEnterEventArgs();
                    simple.selectEntered.Invoke(args);
                }

                // 2. Atoms (Grab)
                var grab = hit.collider.GetComponentInParent<XRGrabInteractable>();
                if (grab != null)
                {
                    _grabbedObject = grab;
                    _grabDistance = hit.distance;

                    // Manually trigger AtomGrabSensor logic
                    var args = new SelectEnterEventArgs();
                    _grabbedObject.selectEntered.Invoke(args);

                    if (_grabbedObject.TryGetComponent<Rigidbody>(out var rb))
                        rb.isKinematic = true;
                }
            }
        }

        if (Mouse.current.leftButton.isPressed && _grabbedObject != null)
        {
            Vector3 mousePos = Mouse.current.position.ReadValue();
            mousePos.z = _grabDistance;
            _grabbedObject.transform.position = Camera.main.ScreenToWorldPoint(mousePos);
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && _grabbedObject != null)
        {
            // Crucial: Use valid Exit args
            var args = new SelectExitEventArgs();
            _grabbedObject.selectExited.Invoke(args);

            if (_grabbedObject.TryGetComponent<Rigidbody>(out var rb))
                rb.isKinematic = false;

            _grabbedObject = null;
        }
    }
}