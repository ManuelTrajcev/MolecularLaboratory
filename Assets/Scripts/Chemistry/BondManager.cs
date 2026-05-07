using System.Collections.Generic;
using UnityEngine;

namespace MolecularLab.Chemistry
{
    public class BondManager : MonoBehaviour
    {
        public static BondManager Instance { get; private set; }

        [SerializeField] private Bond bondPrefab;
        [SerializeField, Min(1f)] private float bondFormDistanceMultiplier = 1.4f;
        [SerializeField] private Transform bondParent;

        private readonly HashSet<Atom> _atoms = new HashSet<Atom>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            if (bondParent == null) bondParent = transform;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Register(Atom atom)
        {
            if (atom != null) _atoms.Add(atom);
        }

        public void Unregister(Atom atom)
        {
            if (atom != null) _atoms.Remove(atom);
        }

        public Bond TryFormBondsAround(Atom released)
        {
            if (released == null || !released.CanBond() || bondPrefab == null) return null;

            Atom best = null;
            float bestDist = float.MaxValue;

            foreach (var other in _atoms)
            {
                if (other == null || other == released) continue;
                if (!other.CanBond()) continue;
                if (AlreadyBonded(released, other)) continue;

                float threshold =
                    (released.Element.DisplayRadius + other.Element.DisplayRadius)
                    * bondFormDistanceMultiplier;

                float d = Vector3.Distance(released.transform.position, other.transform.position);
                if (d <= threshold && d < bestDist)
                {
                    bestDist = d;
                    best = other;
                }
            }

            return best != null ? Bond.Create(bondPrefab, released, best, 1, bondParent) : null;
        }

        private bool AlreadyBonded(Atom x, Atom y)
        {
            for (int i = 0; i < bondParent.childCount; i++)
            {
                var bond = bondParent.GetChild(i).GetComponent<Bond>();
                if (bond == null) continue;
                if ((bond.A == x && bond.B == y) || (bond.A == y && bond.B == x)) return true;
            }
            return false;
        }
    }
}
