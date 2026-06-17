using MolecularLab.Chemistry;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MolecularLab.Interaction
{
    public class AtomDeleteController : MonoBehaviour
    {
        [SerializeField] private Transform leftControllerTransform;
        [SerializeField, Min(0.1f)] private float maxRayDistance = 5f;
        [SerializeField, Min(0.001f)] private float rayRadius = 0.035f;
        [SerializeField, Min(0.01f)] private float nearDeleteRadius = 0.18f;
        [SerializeField] private LayerMask deleteMask = ~0;
        [SerializeField] private bool debugLog = false;

        private InputAction _leftGripDeleteAction;

        private void Awake()
        {
            _leftGripDeleteAction = new InputAction("Left Grip Delete Atom", InputActionType.Button);
            _leftGripDeleteAction.AddBinding("<XRController>{LeftHand}/{GripButton}");
        }

        private void OnEnable()
        {
            _leftGripDeleteAction?.Enable();
        }

        private void OnDisable()
        {
            _leftGripDeleteAction?.Disable();
        }

        private void OnDestroy()
        {
            _leftGripDeleteAction?.Dispose();
            _leftGripDeleteAction = null;
        }

        private void Update()
        {
            if (LeftGripWasPressed())
                TryDeleteTargetAtom();
        }

        private bool LeftGripWasPressed()
        {
            bool controllerPressed = _leftGripDeleteAction != null && _leftGripDeleteAction.WasPressedThisFrame();
            var keyboard = Keyboard.current;
            bool simulatorPressed = keyboard != null
                && keyboard.gKey.wasPressedThisFrame
                && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);

            return controllerPressed || simulatorPressed;
        }

        private void TryDeleteTargetAtom()
        {
            Transform source = GetLeftControllerTransform();
            if (source == null)
            {
                if (debugLog) Debug.LogWarning("[AtomDelete] No left controller transform found.", this);
                return;
            }

            Atom atom = FindRayTarget(source) ?? FindNearTarget(source.position);
            if (atom == null)
            {
                if (debugLog) Debug.Log("[AtomDelete] No atom target found.", this);
                return;
            }

            if (IsStagedInChamber(atom))
            {
                if (debugLog) Debug.Log($"[AtomDelete] Ignored staged atom {atom.name}; use Reset for chamber contents.", atom);
                return;
            }

            DeleteAtom(atom);
        }

        private Transform GetLeftControllerTransform()
        {
            if (leftControllerTransform != null)
                return leftControllerTransform;

            var transforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            for (int i = 0; i < transforms.Length; i++)
            {
                var candidate = transforms[i];
                if (candidate != null && candidate.name == "Left Controller")
                {
                    leftControllerTransform = candidate;
                    return leftControllerTransform;
                }
            }

            for (int i = 0; i < transforms.Length; i++)
            {
                var candidate = transforms[i];
                if (candidate == null)
                    continue;

                string lowerName = candidate.name.ToLowerInvariant();
                if (lowerName.Contains("left") && lowerName.Contains("controller"))
                {
                    leftControllerTransform = candidate;
                    return leftControllerTransform;
                }
            }

            return null;
        }

        private Atom FindRayTarget(Transform source)
        {
            var hits = Physics.SphereCastAll(
                source.position,
                rayRadius,
                source.forward,
                maxRayDistance,
                deleteMask,
                QueryTriggerInteraction.Ignore);

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

        private Atom FindNearTarget(Vector3 sourcePosition)
        {
            var colliders = Physics.OverlapSphere(sourcePosition, nearDeleteRadius, deleteMask, QueryTriggerInteraction.Ignore);
            Atom best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < colliders.Length; i++)
            {
                var atom = colliders[i] != null ? colliders[i].GetComponentInParent<Atom>() : null;
                if (atom == null)
                    continue;

                float distance = Vector3.Distance(sourcePosition, atom.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = atom;
                }
            }

            return best;
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
            if (atom == null)
                return;

            var bonds = new System.Collections.Generic.List<Bond>(atom.Bonds);
            for (int i = 0; i < bonds.Count; i++)
            {
                if (bonds[i] != null)
                    bonds[i].BreakImmediately();
            }

            if (debugLog) Debug.Log($"[AtomDelete] Deleted {atom.name}", atom);
            Destroy(atom.gameObject);
        }
    }
}
