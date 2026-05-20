using System.Collections.Generic;
using MolecularLab.Chemistry;
using MolecularLab.UI;
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

            if (debugLog) Debug.Log($"[AtomGrabSensor] {name}: {_myColliders.Length} колидери кеширани");
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

        // ─── Grab ─────────────────────────────────────────────────────────────

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (debugLog) Debug.Log($"[AtomGrabSensor] {name}: ЗГРАБЕН");

            SetIgnoreCollisionWithOtherAtoms(true);

            // Скини ги сите врски, замрзни ги партнерите и извести го UI
            BreakBondsAndFreezePartners();
        }

        // ─── Release ──────────────────────────────────────────────────────────

        private void OnSelectExited(SelectExitEventArgs args)
        {
            if (debugLog) Debug.Log($"[AtomGrabSensor] {name}: ПУШТЕН");

            _atom.Freeze();
            SetIgnoreCollisionWithOtherAtoms(false);

            if (BondManager.Instance == null)
            {
                Debug.LogWarning($"[AtomGrabSensor] {name}: BondManager.Instance е null — нема BondManager во сцената?");
                return;
            }

            var bonds = BondManager.Instance.TryFormBondsAround(_atom);

            if (debugLog)
                Debug.Log($"[AtomGrabSensor] {name}: Формирани {bonds.Count} врски");
        }

        // ─── Кинење врски + UI известување ───────────────────────────────────

        private void BreakBondsAndFreezePartners()
        {
            var snapshot = new List<Bond>(_atom.Bonds);

            if (snapshot.Count == 0) return;

            // Извести го UI дека молекулата се менува
            MoleculeInfoUI.Instance?.NotifyBondsBreaking();

            foreach (var bond in snapshot)
            {
                if (bond == null) continue;

                var partner = (bond.A == _atom) ? bond.B : bond.A;
                if (partner != null) partner.Freeze();

                Destroy(bond.gameObject);
            }

            if (debugLog)
                Debug.Log($"[AtomGrabSensor] {name}: Скинати {snapshot.Count} врски, партнерите замрзнати");
        }

        // ─── Колидери ─────────────────────────────────────────────────────────

        private void SetIgnoreCollisionWithOtherAtoms(bool ignore)
        {
            if (_myColliders == null) return;

            var allAtoms = Atom.AllAtoms;
            int pairs = 0;

            for (int k = 0; k < allAtoms.Count; k++)
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

            if (debugLog)
                Debug.Log($"[AtomGrabSensor] {name}: {(ignore ? "Игнорирани" : "Вратени")} {pairs} пар(ови) колидери");
        }
    }
}