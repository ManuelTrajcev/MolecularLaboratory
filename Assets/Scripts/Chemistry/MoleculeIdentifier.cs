using System;
using System.Collections.Generic;
using UnityEngine;

namespace MolecularLab.Chemistry
{
    /// <summary>
    /// Bridges BondManager.BondFormed → MoleculeTag placement, and validates
    /// existing tags each frame so broken molecules raise MoleculeDissolved.
    /// </summary>
    public class MoleculeIdentifier : MonoBehaviour
    {
        public static MoleculeIdentifier Instance { get; private set; }

        [SerializeField] private CompoundDatabase database;
        [SerializeField] private BondManager bondManager;
        [SerializeField] private bool debugLog = false;

        public event Action<CompoundSO, MoleculeTag> MoleculeFormed;
        public event Action<CompoundSO, MoleculeTag> MoleculeDissolved;

        private readonly List<MoleculeTag> _tags = new List<MoleculeTag>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            if (bondManager == null) bondManager = BondManager.Instance ?? FindFirstObjectByType<BondManager>();
            if (bondManager != null) bondManager.BondFormed += OnBondFormed;
        }

        private void OnDisable()
        {
            if (bondManager != null) bondManager.BondFormed -= OnBondFormed;
            if (Instance == this) Instance = null;
        }

        private void OnBondFormed(Bond bond)
        {
            if (bond == null || bond.A == null) return;
            TryTagMoleculeAt(bond.A);
        }

        public void IdentifyMoleculeAt(Atom seed)
        {
            if (seed == null)
                return;

            TryTagMoleculeAt(seed);
        }

        private void TryTagMoleculeAt(Atom seed)
        {
            var snap = Molecule.BuildFrom(seed);
            if (!snap.IsSaturated || snap.Atoms.Count == 0) return;

            CompoundSO match = FindMatch(snap.ElementCounts);
            if (match == null) return;

            Atom canonical = LowestId(snap.Atoms);
            if (canonical.GetComponent<MoleculeTag>() != null) return; // already tagged

            var tag = canonical.gameObject.AddComponent<MoleculeTag>();
            tag.Initialize(match, canonical);
            _tags.Add(tag);

            if (debugLog) Debug.Log($"[MoleculeIdentifier] Formed {match.Formula} at {canonical.name}");
            MoleculeFormed?.Invoke(match, tag);
        }

        private CompoundSO FindMatch(Dictionary<ElementSO, int> counts)
        {
            if (database == null) return null;
            var all = database.AllCompounds;
            for (int i = 0; i < all.Count; i++)
            {
                var c = all[i];
                if (c != null && c.Matches(counts)) return c;
            }
            return null;
        }

        private static Atom LowestId(List<Atom> atoms)
        {
            Atom best = atoms[0];
            int id = best.GetInstanceID();
            for (int i = 1; i < atoms.Count; i++)
            {
                int next = atoms[i].GetInstanceID();
                if (next < id) { id = next; best = atoms[i]; }
            }
            return best;
        }

        private void LateUpdate()
        {
            // Re-validate tags: a bond may have broken (Bond.OnDestroy fires after
            // the user yanks atoms past breakDistance). If the tagged molecule no
            // longer matches its CompoundSO, dissolve.
            for (int i = _tags.Count - 1; i >= 0; i--)
            {
                var tag = _tags[i];
                if (tag == null || tag.Owner == null)
                {
                    _tags.RemoveAt(i);
                    continue;
                }

                var snap = Molecule.BuildFrom(tag.Owner);
                bool stillValid = snap.IsSaturated && tag.Compound != null && tag.Compound.Matches(snap.ElementCounts);
                if (stillValid) continue;

                if (debugLog) Debug.Log($"[MoleculeIdentifier] Dissolved {tag.Compound?.Formula} at {tag.Owner.name}");
                var compound = tag.Compound;
                var seed = tag.Owner;
                _tags.RemoveAt(i);
                MoleculeDissolved?.Invoke(compound, tag);
                tag.NotifyBroken();

                // If the molecule re-arranged into a different valid compound (rare but possible),
                // try to tag it again from the seed atom.
                if (seed != null) TryTagMoleculeAt(seed);
            }
        }

        public IReadOnlyList<MoleculeTag> ActiveTags => _tags;
    }
}
