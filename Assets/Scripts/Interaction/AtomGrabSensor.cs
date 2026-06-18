using System.Collections.Generic;
using MolecularLab.Chemistry;
using MolecularLab.Managers;
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
        private readonly List<Atom> _draggedMoleculeAtoms = new List<Atom>();
        private readonly Dictionary<Atom, Vector3> _dragOffsets = new Dictionary<Atom, Vector3>();
        private Vector3 _dragAnchorStart;
        private Quaternion _dragAnchorRotationStart;
        private Quaternion _dragControllerRotationStart;
        private Transform _dragControllerTransform;
        private bool _draggingWholeMolecule;
        private bool _wasDraggingWholeMolecule;

        public bool IsDraggingWholeMolecule => _draggingWholeMolecule;

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

            _dragAnchorStart = transform.position;
            SetIgnoreCollisionWithOtherAtoms(true);
            BeginMoleculeDragIfNeeded(args);
            _wasDraggingWholeMolecule = _draggingWholeMolecule;
        }

        // ─── Release ──────────────────────────────────────────────────────────

        private void OnSelectExited(SelectExitEventArgs args)
        {
            if (debugLog) Debug.Log($"[AtomGrabSensor] {name}: ПУШТЕН");

            _atom.Freeze();
            EndMoleculeDrag();
            SetIgnoreCollisionWithOtherAtoms(false);

            if (_wasDraggingWholeMolecule)
            {
                var chamber = FindFirstObjectByType<ReactionChamber>();
                if (chamber != null)
                {
                    var result = chamber.TryAcceptReleasedMolecule(_atom);
                    if (result == ReactionChamber.ChamberAcceptResult.Accepted)
                    {
                        _draggedMoleculeAtoms.Clear();
                        _dragOffsets.Clear();
                        _wasDraggingWholeMolecule = false;
                        return;
                    }

                    if (result == ReactionChamber.ChamberAcceptResult.Rejected)
                    {
                        RestoreDraggedMoleculeToGrabStart();
                        _draggedMoleculeAtoms.Clear();
                        _dragOffsets.Clear();
                        _wasDraggingWholeMolecule = false;
                        return;
                    }
                }
            }

            if (TryAcceptReleasedTaggedMolecule())
                return;

            _draggedMoleculeAtoms.Clear();
            _dragOffsets.Clear();
            _wasDraggingWholeMolecule = false;

            var smallChamber = FindFirstObjectByType<SmallMoleculeChamber>();
            if (smallChamber != null && Molecule.BuildFrom(_atom).Atoms.Count == 1)
            {
                var smallResult = smallChamber.TryAcceptReleasedAtom(_atom);
                if (smallResult == SmallMoleculeChamber.SmallChamberAcceptResult.Accepted)
                {
                    return;
                }

                if (smallResult == SmallMoleculeChamber.SmallChamberAcceptResult.Rejected)
                {
                    SetAtomWorldPosition(_atom, _dragAnchorStart);
                    _atom.Freeze();
                    return;
                }
            }

            // If released inside the reaction chamber trigger bounds, play the spatialized place down sound
            var chamberInstance = FindFirstObjectByType<ReactionChamber>();
            if (chamberInstance != null)
            {
                var chamberCol = chamberInstance.GetComponent<Collider>();
                if (chamberCol != null && chamberCol.bounds.Contains(transform.position))
                {
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayPlaceDown(transform.position);
                    }
                }
            }

            _draggedMoleculeAtoms.Clear();
            _dragOffsets.Clear();
            _wasDraggingWholeMolecule = false;

            if (debugLog)
                Debug.Log($"[AtomGrabSensor] {name}: Пуштен надвор од chamber — нема автоматско формирање врски");
        }

        private bool TryAcceptReleasedTaggedMolecule()
        {
            if (ResolveTag(_atom) == null)
                return false;

            var chamber = FindFirstObjectByType<ReactionChamber>();
            if (chamber == null)
                return false;

            var result = chamber.TryAcceptReleasedMolecule(_atom);
            if (result == ReactionChamber.ChamberAcceptResult.Accepted)
            {
                _draggedMoleculeAtoms.Clear();
                _dragOffsets.Clear();
                _wasDraggingWholeMolecule = false;
                return true;
            }

            if (result == ReactionChamber.ChamberAcceptResult.Rejected)
            {
                SetAtomWorldPosition(_atom, _dragAnchorStart);
                _atom.Freeze();
                _draggedMoleculeAtoms.Clear();
                _dragOffsets.Clear();
                _wasDraggingWholeMolecule = false;
                return true;
            }

            return false;
        }

        private void LateUpdate()
        {
            if (!_draggingWholeMolecule) return;

            Quaternion rotationDelta = GetCurrentDragRotationDelta();
            Vector3 anchorPosition = transform.position;
            for (int i = 0; i < _draggedMoleculeAtoms.Count; i++)
            {
                var other = _draggedMoleculeAtoms[i];
                if (other == null || other == _atom) continue;
                SetAtomWorldPosition(other, anchorPosition + rotationDelta * _dragOffsets[other]);
            }
        }

        // ─── Молекуларно влечење ─────────────────────────────────────────────

        private void BeginMoleculeDragIfNeeded(SelectEnterEventArgs args)
        {
            _draggedMoleculeAtoms.Clear();
            _dragOffsets.Clear();
            _draggingWholeMolecule = false;
            _dragControllerTransform = args?.interactorObject?.transform;
            _dragAnchorRotationStart = transform.rotation;
            _dragControllerRotationStart = _dragControllerTransform != null ? _dragControllerTransform.rotation : transform.rotation;

            if (_atom.Bonds.Count == 0) return;

            var molecule = Molecule.BuildFrom(_atom);
            if (molecule.Atoms.Count <= 1) return;

            _dragAnchorStart = transform.position;
            for (int i = 0; i < molecule.Atoms.Count; i++)
            {
                var atom = molecule.Atoms[i];
                if (atom == null) continue;
                _draggedMoleculeAtoms.Add(atom);
                _dragOffsets[atom] = atom.transform.position - _dragAnchorStart;
                atom.Freeze();
            }

            _draggingWholeMolecule = true;

            if (debugLog)
                Debug.Log($"[AtomGrabSensor] {name}: Влечење на цела молекула ({_draggedMoleculeAtoms.Count} атоми)");
        }

        private void EndMoleculeDrag()
        {
            if (!_draggingWholeMolecule)
            {
                _draggedMoleculeAtoms.Clear();
                _dragOffsets.Clear();
                _dragControllerTransform = null;
                return;
            }

            for (int i = 0; i < _draggedMoleculeAtoms.Count; i++)
            {
                var atom = _draggedMoleculeAtoms[i];
                if (atom != null) atom.Freeze();
            }

            _draggingWholeMolecule = false;
            _dragControllerTransform = null;
        }

        private Quaternion GetCurrentDragRotationDelta()
        {
            Quaternion current = _dragControllerTransform != null ? _dragControllerTransform.rotation : transform.rotation;
            Quaternion start = _dragControllerTransform != null ? _dragControllerRotationStart : _dragAnchorRotationStart;
            return current * Quaternion.Inverse(start);
        }

        private static void SetAtomWorldPosition(Atom atom, Vector3 position)
        {
            atom.transform.position = position;
            if (atom.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.position = position;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        private void RestoreDraggedMoleculeToGrabStart()
        {
            for (int i = 0; i < _draggedMoleculeAtoms.Count; i++)
            {
                var atom = _draggedMoleculeAtoms[i];
                if (atom == null) continue;
                if (!_dragOffsets.TryGetValue(atom, out var offset)) continue;
                SetAtomWorldPosition(atom, _dragAnchorStart + offset);
                atom.Freeze();
            }
        }

        private static MoleculeTag ResolveTag(Atom atom)
        {
            if (atom == null)
                return null;

            var snap = Molecule.BuildFrom(atom);
            for (int i = 0; i < snap.Atoms.Count; i++)
            {
                var tag = snap.Atoms[i] != null ? snap.Atoms[i].GetComponent<MoleculeTag>() : null;
                if (tag != null && tag.Compound != null)
                    return tag;
            }

            return null;
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
