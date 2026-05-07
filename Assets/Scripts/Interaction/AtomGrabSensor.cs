using MolecularLab.Chemistry;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MolecularLab.Interaction
{
    [RequireComponent(typeof(Atom))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class AtomGrabSensor : MonoBehaviour
    {
        [SerializeField] private bool debugLog = false;

        private Atom _atom;
        private XRGrabInteractable _grab;
        private Collider[] _myColliders;

        private void Awake()
        {
            _atom = GetComponent<Atom>();
            _grab = GetComponent<XRGrabInteractable>();
            _myColliders = GetComponentsInChildren<Collider>(true);
            if (debugLog) Debug.Log($"[AtomGrabSensor] {name}: cached {_myColliders.Length} colliders");
        }

        private void OnEnable()
        {
            _grab.selectEntered.AddListener(OnSelectEntered);
            _grab.selectExited.AddListener(OnSelectExited);
        }

        private void OnDisable()
        {
            _grab.selectEntered.RemoveListener(OnSelectEntered);
            _grab.selectExited.RemoveListener(OnSelectExited);
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (debugLog) Debug.Log($"[AtomGrabSensor] {name}: GRABBED");
            SetIgnoreCollisionWithOtherAtoms(true);
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            if (debugLog) Debug.Log($"[AtomGrabSensor] {name}: RELEASED");
            SetIgnoreCollisionWithOtherAtoms(false);

            if (BondManager.Instance == null)
            {
                Debug.LogWarning($"[AtomGrabSensor] {name}: BondManager.Instance is null at release — no BondManager in scene?");
                return;
            }
            var bond = BondManager.Instance.TryFormBondsAround(_atom);
            if (debugLog) Debug.Log($"[AtomGrabSensor] {name}: TryFormBondsAround result = {(bond != null ? "BOND" : "no bond")}");
        }

        private void SetIgnoreCollisionWithOtherAtoms(bool ignore)
        {
            if (_myColliders == null) return;

            var allAtoms = FindObjectsByType<Atom>(FindObjectsSortMode.None);
            int pairs = 0;
            for (int k = 0; k < allAtoms.Length; k++)
            {
                var other = allAtoms[k];
                if (other == null || other == _atom) continue;

                var otherColliders = other.GetComponentsInChildren<Collider>(true);
                for (int i = 0; i < _myColliders.Length; i++)
                {
                    var mine = _myColliders[i];
                    if (mine == null || !mine.enabled) continue;
                    for (int j = 0; j < otherColliders.Length; j++)
                    {
                        var oc = otherColliders[j];
                        if (oc == null || !oc.enabled) continue;
                        Physics.IgnoreCollision(mine, oc, ignore);
                        pairs++;
                    }
                }
            }
            if (debugLog) Debug.Log($"[AtomGrabSensor] {name}: scanned {allAtoms.Length} atom(s), {(ignore ? "ignored" : "restored")} {pairs} collider pair(s)");
        }
    }
}
