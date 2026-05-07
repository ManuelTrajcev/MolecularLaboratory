using UnityEngine;

namespace MolecularLab.Chemistry
{
    public class BondManager : MonoBehaviour
    {
        public static BondManager Instance { get; private set; }

        public event System.Action<Bond> BondFormed;

        [SerializeField] private Bond bondPrefab;
        [SerializeField, Min(1f)] private float bondFormDistanceMultiplier = 1.5f;
        [SerializeField, Min(0f)] private float bondFormSlack = 0.05f;
        [SerializeField] private Transform bondParent;

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

        public Atom[] GetAllAtoms()
        {
            return FindObjectsByType<Atom>(FindObjectsSortMode.None);
        }

        public Bond TryFormBondsAround(Atom released)
        {
            if (released == null || !released.CanBond() || bondPrefab == null) return null;

            Atom best = null;
            float bestDist = float.MaxValue;
            var all = GetAllAtoms();

            for (int i = 0; i < all.Length; i++)
            {
                var other = all[i];
                if (other == null || other == released) continue;
                if (!other.CanBond()) continue;
                if (AlreadyBonded(released, other)) continue;

                float threshold =
                    (released.Element.DisplayRadius + other.Element.DisplayRadius)
                    * bondFormDistanceMultiplier
                    + bondFormSlack;

                float d = Vector3.Distance(released.transform.position, other.transform.position);
                if (d <= threshold && d < bestDist)
                {
                    bestDist = d;
                    best = other;
                }
            }

            if (best == null) return null;
            var bond = Bond.Create(bondPrefab, released, best, 1, bondParent);
            if (bond != null) BondFormed?.Invoke(bond);
            return bond;
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
