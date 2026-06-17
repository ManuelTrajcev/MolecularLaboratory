using System;
using System.Collections;
using System.Collections.Generic;
using MolecularLab.Chemistry;
using MolecularLab.Managers;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MolecularLab.Interaction
{
    [RequireComponent(typeof(Collider))]
    public class SmallMoleculeChamber : MonoBehaviour
    {
        public event Action<CompoundSO, MoleculeTag> MoleculeBuilt;
        public event Action<string, Atom> AtomRejected;

        [SerializeField] private Transform outputAnchor;
        [SerializeField] private Transform atomSpawnAnchor;
        [SerializeField, Min(0f)] private float acceptHorizontalPadding = 0.14f;
        [SerializeField, Min(0f)] private float acceptVerticalPadding = 0.4f;
        [SerializeField, Min(0f)] private float buildDelay = 0.15f;
        [SerializeField] private string wrongAtomMessage = "{0} is not needed for {1}.";
        [SerializeField] private bool debugLog = false;

        private readonly Dictionary<ElementSO, int> _acceptedCounts = new Dictionary<ElementSO, int>();
        private readonly List<Atom> _stagedAtoms = new List<Atom>();
        private CompoundSO _targetCompound;
        private Coroutine _buildRoutine;
        private Collider _trigger;

        public CompoundSO TargetCompound => _targetCompound;
        public Transform AtomSpawnAnchor => atomSpawnAnchor;
        public bool HasActiveTarget => _targetCompound != null;
        public Vector3 GuidanceTarget => _trigger != null ? _trigger.bounds.center : transform.position;

        private void Awake()
        {
            _trigger = GetComponent<Collider>();
            _trigger.isTrigger = true;
        }

        public void SetCurrentLevel(LevelSO level, IReadOnlyDictionary<CompoundSO, int> built)
        {
            var nextTarget = FindNextTarget(level, built);
            if (AreEquivalent(_targetCompound, nextTarget))
                return;

            _targetCompound = nextTarget;
            ClearStagedAtoms();
            if (debugLog) Debug.Log($"[SmallChamber] target={_targetCompound?.Formula ?? "<none>"}");
        }

        public void ClearAllContents()
        {
            if (_buildRoutine != null)
            {
                StopCoroutine(_buildRoutine);
                _buildRoutine = null;
            }

            ClearStagedAtoms();
        }

        public bool IsAtomStaged(Atom atom)
        {
            return atom != null && _stagedAtoms.Contains(atom);
        }

        public bool IsElementNeeded(ElementSO element)
        {
            if (_targetCompound == null || element == null)
                return false;

            int required = GetRequiredCount(_targetCompound, element);
            if (required <= 0)
                return false;

            _acceptedCounts.TryGetValue(element, out int accepted);
            return accepted < required;
        }

        public ElementSO GetNextNeededElement()
        {
            if (_targetCompound == null)
                return null;

            var inputs = _targetCompound.Inputs;
            for (int i = 0; i < inputs.Count; i++)
            {
                var input = inputs[i];
                if (input.element == null)
                    continue;

                _acceptedCounts.TryGetValue(input.element, out int accepted);
                if (accepted < input.count)
                    return input.element;
            }

            return null;
        }

        public SmallChamberAcceptResult TryAcceptReleasedAtom(Atom atom)
        {
            if (atom == null || atom.Element == null || _trigger == null)
                return SmallChamberAcceptResult.TooFar;

            if (Molecule.BuildFrom(atom).Atoms.Count > 1)
                return SmallChamberAcceptResult.TooFar;

            Bounds bounds = _trigger.bounds;
            bounds.Expand(new Vector3(acceptHorizontalPadding * 2f, acceptVerticalPadding * 2f, acceptHorizontalPadding * 2f));
            if (!bounds.Contains(atom.transform.position))
                return SmallChamberAcceptResult.TooFar;

            if (_targetCompound == null)
            {
                Reject("No molecule target is active.", atom);
                return SmallChamberAcceptResult.Rejected;
            }

            if (!IsElementNeeded(atom.Element))
            {
                Reject(string.Format(wrongAtomMessage, atom.Element.Symbol, _targetCompound.Formula), atom);
                return SmallChamberAcceptResult.Rejected;
            }

            AcceptAtom(atom);
            return SmallChamberAcceptResult.Accepted;
        }

        private void AcceptAtom(Atom atom)
        {
            _stagedAtoms.Add(atom);
            _acceptedCounts.TryGetValue(atom.Element, out int count);
            _acceptedCounts[atom.Element] = count + 1;

            PositionAtom(atom, GetStagingPosition(_stagedAtoms.Count - 1));
            atom.Freeze();
            SetAtomInteractable(atom, false);

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayPlaceDown(atom.transform.position);

            if (HasAllRequiredAtoms() && _buildRoutine == null)
                _buildRoutine = StartCoroutine(BuildTargetMolecule());
        }

        private IEnumerator BuildTargetMolecule()
        {
            yield return new WaitForSeconds(buildDelay);
            var target = _targetCompound;
            var atoms = new List<Atom>(_stagedAtoms);

            LayoutMolecule(target, atoms, GetOutputPosition());
            BondMolecule(target, atoms);

            for (int i = 0; i < atoms.Count; i++)
            {
                if (atoms[i] == null) continue;
                atoms[i].Freeze();
                SetAtomInteractable(atoms[i], true);
            }

            ForceIdentify(atoms);
            var tag = ResolveTag(atoms);
            if (tag != null)
                MoleculeBuilt?.Invoke(target, tag);

            _stagedAtoms.Clear();
            _acceptedCounts.Clear();
            _buildRoutine = null;
        }

        private bool HasAllRequiredAtoms()
        {
            if (_targetCompound == null)
                return false;

            var inputs = _targetCompound.Inputs;
            for (int i = 0; i < inputs.Count; i++)
            {
                var input = inputs[i];
                if (input.element == null)
                    return false;

                _acceptedCounts.TryGetValue(input.element, out int accepted);
                if (accepted < input.count)
                    return false;
            }

            return true;
        }

        private Vector3 GetStagingPosition(int index)
        {
            Vector3 center = _trigger != null ? _trigger.bounds.center : transform.position;
            center.y = _trigger != null ? _trigger.bounds.max.y + 0.04f : center.y;
            return center + GetSpawnOffset(index) * 1.4f;
        }

        private Vector3 GetOutputPosition()
        {
            if (outputAnchor != null)
                return outputAnchor.position;

            if (_trigger != null)
            {
                var bounds = _trigger.bounds;
                return new Vector3(bounds.center.x, bounds.max.y + 0.07f, bounds.center.z);
            }

            return transform.position + Vector3.up * 0.2f;
        }

        private void Reject(string message, Atom atom)
        {
            if (debugLog) Debug.Log($"[SmallChamber] reject: {message}", this);
            AtomRejected?.Invoke(message, atom);
        }

        private void ClearStagedAtoms()
        {
            for (int i = 0; i < _stagedAtoms.Count; i++)
            {
                if (_stagedAtoms[i] != null)
                    Destroy(_stagedAtoms[i].gameObject);
            }

            _stagedAtoms.Clear();
            _acceptedCounts.Clear();
        }

        private static CompoundSO FindNextTarget(LevelSO level, IReadOnlyDictionary<CompoundSO, int> built)
        {
            if (level == null)
                return null;

            var stage = level.Stage1;
            for (int i = 0; i < stage.Count; i++)
            {
                var target = stage[i];
                if (target.compound == null)
                    continue;

                int placed = CountEquivalent(built, target.compound);
                if (placed < target.count)
                    return target.compound;
            }

            return null;
        }

        private static int CountEquivalent(IReadOnlyDictionary<CompoundSO, int> contents, CompoundSO target)
        {
            if (contents == null || target == null)
                return 0;

            int count = 0;
            foreach (var kv in contents)
            {
                if (AreEquivalent(kv.Key, target))
                    count += kv.Value;
            }

            return count;
        }

        private static bool AreEquivalent(CompoundSO a, CompoundSO b)
        {
            if (a == null || b == null)
                return false;

            if (ReferenceEquals(a, b))
                return true;

            return !string.IsNullOrWhiteSpace(a.Formula)
                && string.Equals(a.Formula, b.Formula, StringComparison.OrdinalIgnoreCase);
        }

        private static int GetRequiredCount(CompoundSO compound, ElementSO element)
        {
            if (compound == null || element == null)
                return 0;

            var inputs = compound.Inputs;
            for (int i = 0; i < inputs.Count; i++)
            {
                if (inputs[i].element == element)
                    return inputs[i].count;
            }

            return 0;
        }

        private static void LayoutMolecule(CompoundSO compound, List<Atom> atoms, Vector3 center)
        {
            string formula = NormalizeFormula(compound);

            if (formula == "CO2")
            {
                var carbon = FirstAtom(atoms, "C");
                var oxygens = AtomsBySymbol(atoms, "O");
                if (carbon != null && oxygens.Count >= 2)
                {
                    float co = ProductBondLength(carbon, oxygens[0]);
                    PositionAtom(carbon, center);
                    PositionAtom(oxygens[0], center + new Vector3(-co, 0f, 0f));
                    PositionAtom(oxygens[1], center + new Vector3(co, 0f, 0f));
                    return;
                }
            }

            if (formula == "H2O" && TryLayoutRadial(atoms, center, "O", "H", 2, 0.72f)) return;
            if (formula == "CH4" && TryLayoutRadial(atoms, center, "C", "H", 4, 1f)) return;
            if (formula == "NH3" && TryLayoutRadial(atoms, center, "N", "H", 3, 0.9f)) return;

            if (atoms.Count == 1)
            {
                PositionAtom(atoms[0], center);
                return;
            }

            if (atoms.Count == 2)
            {
                float spacing = ProductBondLength(atoms[0], atoms[1]);
                PositionAtom(atoms[0], center + new Vector3(-spacing * 0.5f, 0f, 0f));
                PositionAtom(atoms[1], center + new Vector3(spacing * 0.5f, 0f, 0f));
                return;
            }

            for (int i = 0; i < atoms.Count; i++)
                PositionAtom(atoms[i], center + GetSpawnOffset(i));
        }

        private static void BondMolecule(CompoundSO compound, List<Atom> atoms)
        {
            if (atoms == null || atoms.Count < 2 || BondManager.Instance == null)
                return;

            string formula = NormalizeFormula(compound);
            var manager = BondManager.Instance;

            if (formula == "CO2")
            {
                var carbon = FirstAtom(atoms, "C");
                var oxygens = AtomsBySymbol(atoms, "O");
                if (carbon != null && oxygens.Count >= 2)
                {
                    manager.TryCreateBondExact(carbon, oxygens[0], 2);
                    manager.TryCreateBondExact(carbon, oxygens[1], 2);
                    return;
                }
            }

            if (formula == "H2O" && TryBondRadial(atoms, manager, "O", "H", 2)) return;
            if (formula == "CH4" && TryBondRadial(atoms, manager, "C", "H", 4)) return;
            if (formula == "NH3" && TryBondRadial(atoms, manager, "N", "H", 3)) return;

            if (atoms.Count == 2)
            {
                manager.TryCreateBond(atoms[0], atoms[1], 1);
                return;
            }

            for (int i = 1; i < atoms.Count; i++)
                manager.TryCreateBond(atoms[0], atoms[i], 1);
        }

        private static bool TryLayoutRadial(List<Atom> atoms, Vector3 center, string centerSymbol, string outerSymbol, int outerCount, float arcScale)
        {
            var centerAtom = FirstAtom(atoms, centerSymbol);
            var outerAtoms = AtomsBySymbol(atoms, outerSymbol);
            if (centerAtom == null || outerAtoms.Count < outerCount)
                return false;

            PositionAtom(centerAtom, center);
            float radius = ProductBondLength(centerAtom, outerAtoms[0]);
            for (int i = 0; i < outerCount; i++)
            {
                float angleDegrees = outerCount == 2 ? 140f - 100f * i : 90f + 360f / outerCount * i;
                float angle = angleDegrees * Mathf.Deg2Rad;
                PositionAtom(outerAtoms[i], center + new Vector3(Mathf.Cos(angle) * radius * arcScale, 0f, Mathf.Sin(angle) * radius));
            }

            return true;
        }

        private static bool TryBondRadial(List<Atom> atoms, BondManager manager, string centerSymbol, string outerSymbol, int outerCount)
        {
            var centerAtom = FirstAtom(atoms, centerSymbol);
            var outerAtoms = AtomsBySymbol(atoms, outerSymbol);
            if (centerAtom == null || outerAtoms.Count < outerCount)
                return false;

            for (int i = 0; i < outerCount; i++)
                manager.TryCreateBondExact(centerAtom, outerAtoms[i], 1);

            return true;
        }

        private static void ForceIdentify(IReadOnlyList<Atom> atoms)
        {
            var identifier = MoleculeIdentifier.Instance ?? FindFirstObjectByType<MoleculeIdentifier>();
            if (identifier == null || atoms == null)
                return;

            for (int i = 0; i < atoms.Count; i++)
            {
                if (atoms[i] != null)
                    identifier.IdentifyMoleculeAt(atoms[i]);
            }
        }

        private static MoleculeTag ResolveTag(IReadOnlyList<Atom> atoms)
        {
            if (atoms == null)
                return null;

            for (int i = 0; i < atoms.Count; i++)
            {
                if (atoms[i] == null)
                    continue;

                var snap = Molecule.BuildFrom(atoms[i]);
                for (int j = 0; j < snap.Atoms.Count; j++)
                {
                    var tag = snap.Atoms[j] != null ? snap.Atoms[j].GetComponent<MoleculeTag>() : null;
                    if (tag != null)
                        return tag;
                }
            }

            return null;
        }

        private static void SetAtomInteractable(Atom atom, bool interactable)
        {
            if (atom == null)
                return;

            var grab = atom.GetComponent<XRGrabInteractable>();
            if (grab != null)
                grab.enabled = interactable;

            var sensor = atom.GetComponent<AtomGrabSensor>();
            if (sensor != null)
                sensor.enabled = interactable;
        }

        private static void PositionAtom(Atom atom, Vector3 position)
        {
            if (atom == null)
                return;

            atom.transform.position = position;
            if (atom.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.position = position;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        private static string NormalizeFormula(CompoundSO compound)
        {
            return compound != null && !string.IsNullOrWhiteSpace(compound.Formula)
                ? compound.Formula.Replace(" ", string.Empty).ToUpperInvariant()
                : string.Empty;
        }

        private static Atom FirstAtom(List<Atom> atoms, string symbol)
        {
            if (atoms == null)
                return null;

            for (int i = 0; i < atoms.Count; i++)
            {
                var atom = atoms[i];
                if (atom != null && atom.Element != null && atom.Element.Symbol == symbol)
                    return atom;
            }

            return null;
        }

        private static List<Atom> AtomsBySymbol(List<Atom> atoms, string symbol)
        {
            var matches = new List<Atom>();
            if (atoms == null)
                return matches;

            for (int i = 0; i < atoms.Count; i++)
            {
                var atom = atoms[i];
                if (atom != null && atom.Element != null && atom.Element.Symbol == symbol)
                    matches.Add(atom);
            }

            return matches;
        }

        private static float ProductBondLength(Atom a, Atom b)
        {
            float radiusA = a != null && a.Element != null ? a.Element.DisplayRadius : 0.05f;
            float radiusB = b != null && b.Element != null ? b.Element.DisplayRadius : 0.05f;
            return Mathf.Max((radiusA + radiusB) * 1.5f, 0.14f);
        }

        private static Vector3 GetSpawnOffset(int index)
        {
            return index switch
            {
                0 => new Vector3(-0.04f, 0f, 0f),
                1 => new Vector3(0.04f, 0f, 0f),
                2 => new Vector3(0f, 0f, 0.07f),
                3 => new Vector3(0f, 0f, -0.07f),
                _ => new Vector3(index * 0.05f, 0f, 0f),
            };
        }

        public enum SmallChamberAcceptResult
        {
            TooFar,
            Accepted,
            Rejected,
        }
    }
}
