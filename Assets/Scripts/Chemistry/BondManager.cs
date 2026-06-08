using System.Collections.Generic;
using UnityEngine;
using MolecularLab.Interaction;

namespace MolecularLab.Chemistry
{
    public class BondManager : MonoBehaviour
    {
        public static BondManager Instance { get; private set; }

        public event System.Action<Bond> BondFormed;

        [Header("Bond Prefab (опционално — Project Asset, НЕ сцена-објект)")]
        [SerializeField] private Bond bondPrefab;

        [Header("Процедурален fallback (користи се кога нема prefab)")]
        [SerializeField] private Material bondMaterial;
        [SerializeField] private Color bondColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        [Header("Поставки за формирање врски")]
        [SerializeField, Min(1f)] private float bondFormDistanceMultiplier = 1.5f;
        [SerializeField, Min(0f)] private float bondFormSlack = 0.05f;

        [Header("Родител за Bond-объекти")]
        [SerializeField] private Transform bondParent;

        private Bond _bondTemplate;
        private static Mesh _cylinderMesh;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;

            if (bondParent == null)
                bondParent = transform;

            CacheTemplate();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ─────────────────────────────────────────────────────────────
        // NEW: Composition Structure
        // ─────────────────────────────────────────────────────────────


        [System.Serializable]
        public struct ElementCount
        {
            public ElementSO element;
            public int count;
        }

        [Header("DEBUG Composition")]
        [SerializeField]
        private List<ElementCount> currentComposition = new List<ElementCount>();

        [ContextMenu("Refresh Composition")]
        public void RefreshComposition()
        {
            currentComposition.Clear();

            Bond[] bonds = FindObjectsByType<Bond>(FindObjectsSortMode.None);

            Dictionary<ElementSO, int> counts = new Dictionary<ElementSO, int>();

            HashSet<Atom> uniqueAtoms = new HashSet<Atom>();

            for (int i = 0; i < bonds.Length; i++)
            {
                Bond bond = bonds[i];

                if (bond == null)
                    continue;

                if (bond.A != null)
                    uniqueAtoms.Add(bond.A);

                if (bond.B != null)
                    uniqueAtoms.Add(bond.B);
            }

            foreach (Atom atom in uniqueAtoms)
            {
                if (atom == null || atom.Element == null)
                    continue;

                if (counts.ContainsKey(atom.Element))
                    counts[atom.Element]++;
                else
                    counts.Add(atom.Element, 1);
            }

            foreach (var kv in counts)
            {
                currentComposition.Add(new ElementCount
                {
                    element = kv.Key,
                    count = kv.Value
                });
            }
        }




        /// <summary>
        /// Ги враќа сите поврзани атоми со counter.
        /// Пример:
        /// H = 2
        /// O = 1
        /// </summary>
        public List<ElementCount> GetBondedComposition()
        {
            var result = new List<ElementCount>();

            Atom[] atoms = GetAllAtoms();

            Dictionary<ElementSO, int> counts = new Dictionary<ElementSO, int>();

            for (int i = 0; i < atoms.Length; i++)
            {
                Atom atom = atoms[i];

                if (atom == null || atom.Element == null)
                    continue;

                if (counts.ContainsKey(atom.Element))
                    counts[atom.Element]++;
                else
                    counts.Add(atom.Element, 1);
            }

            foreach (var kv in counts)
            {
                result.Add(new ElementCount
                {
                    element = kv.Key,
                    count = kv.Value
                });
            }

            return result;
        }

        // ─────────────────────────────────────────────────────────────

        private void CacheTemplate()
        {
            if (bondPrefab == null)
            {
                Debug.Log("[BondManager] bondPrefab не е поставен → процедурален режим.");
                return;
            }

            _bondTemplate = Instantiate(bondPrefab, bondParent);
            _bondTemplate.gameObject.name = "__BondTemplate__";
            _bondTemplate.gameObject.SetActive(false);

            Debug.Log("[BondManager] Bond шаблон кеширан успешно.");
        }

        public Atom[] GetAllAtoms()
            => FindObjectsByType<Atom>(FindObjectsSortMode.None);

        public List<Bond> TryFormBondsAround(Atom released)
        {
            var formed = new List<Bond>();

            if (released == null)
                return formed;

            while (released.CanBond())
            {
                Atom best = FindBestCandidate(released);

                if (best == null)
                    break;

                int targetOrder = GetTargetBondOrder(released, best);
                var existing = GetBondBetween(released, best);
                Bond bond = existing != null
                    ? UpgradeBond(existing, targetOrder)
                    : SpawnBond(released, best, Mathf.Max(1, targetOrder));

                if (bond == null)
                    break;

                if (!formed.Contains(bond))
                    formed.Add(bond);

                BondFormed?.Invoke(bond);
            }

            return formed;
        }

        public Bond TryCreateBond(Atom a, Atom b, int order = 1)
        {
            if (a == null || b == null || a == b)
                return null;

            int targetOrder = Mathf.Max(order, GetTargetBondOrder(a, b));
            var existing = GetBondBetween(a, b);
            if (existing != null)
            {
                var upgraded = UpgradeBond(existing, Mathf.Max(existing.Order, targetOrder));
                if (upgraded != null)
                    BondFormed?.Invoke(upgraded);
                return upgraded;
            }

            if (!a.CanBond(targetOrder) || !b.CanBond(targetOrder))
                return null;

            var bond = SpawnBond(a, b, targetOrder);
            if (bond != null)
                BondFormed?.Invoke(bond);

            return bond;
        }

        private Atom FindBestCandidate(Atom released)
        {
            Atom best = null;
            float bestDist = float.MaxValue;

            var all = GetAllAtoms();
            var chamber = FindFirstObjectByType<ReactionChamber>();

            if (chamber != null && chamber.IsAtomStaged(released))
                return null;

            for (int i = 0; i < all.Length; i++)
            {
                var other = all[i];

                if (other == null || other == released)
                    continue;

                if (chamber != null && chamber.IsAtomStaged(other))
                    continue;

                if (other.Element == null || released.Element == null)
                    continue;

                int targetOrder = GetTargetBondOrder(released, other);
                if (targetOrder <= 0)
                    continue;

                var existing = GetBondBetween(released, other);
                if (existing != null)
                {
                    if (existing.Order >= targetOrder)
                        continue;

                    int delta = targetOrder - existing.Order;
                    if (!released.CanBond(delta) || !other.CanBond(delta))
                        continue;
                }
                else if (!released.CanBond(targetOrder) || !other.CanBond(targetOrder))
                {
                    continue;
                }

                float threshold =
                    (released.Element.DisplayRadius + other.Element.DisplayRadius)
                    * bondFormDistanceMultiplier + bondFormSlack;

                float d = Vector3.Distance(
                    released.transform.position,
                    other.transform.position);

                if (d <= threshold && d < bestDist)
                {
                    bestDist = d;
                    best = other;
                }
            }

            return best;
        }

        private Bond UpgradeBond(Bond existing, int targetOrder)
        {
            if (existing == null)
                return null;

            int desired = Mathf.Clamp(targetOrder, existing.Order, 3);
            return existing.TrySetOrder(desired) ? existing : null;
        }

        private int GetTargetBondOrder(Atom a, Atom b)
        {
            if (a == null || b == null || a.Element == null || b.Element == null)
                return 0;

            string aSymbol = a.Element.Symbol;
            string bSymbol = b.Element.Symbol;

            if (aSymbol == "O" && bSymbol == "O")
                return 2;

            if (aSymbol == "N" && bSymbol == "N")
                return 3;

            // Carbon–Oxygen forms a multiple bond (double, the most the symmetric
            // valence model allows since O has valence 2). This saturates oxygen so
            // a lone C+O pair is recognized as CO, and gives O=C=O for CO2.
            if ((aSymbol == "C" && bSymbol == "O") || (aSymbol == "O" && bSymbol == "C"))
                return 2;

            return 1;
        }

        private Bond SpawnBond(Atom a, Atom b, int order)
        {
            if (_bondTemplate != null)
            {
                return Bond.CreateFromTemplate(
                    _bondTemplate,
                    a,
                    b,
                    order,
                    bondParent);
            }

            return Bond.CreateProcedural(
                a,
                b,
                order,
                bondParent,
                GetOrCreateMaterial(),
                GetOrCreateCylinderMesh());
        }

        private Bond GetBondBetween(Atom x, Atom y)
        {
            for (int i = 0; i < bondParent.childCount; i++)
            {
                var bond = bondParent.GetChild(i).GetComponent<Bond>();

                if (bond == null)
                    continue;

                if ((bond.A == x && bond.B == y) ||
                    (bond.A == y && bond.B == x))
                    return bond;
            }

            return null;
        }

        private bool AlreadyBonded(Atom x, Atom y)
        {
            return GetBondBetween(x, y) != null;
        }

        private Material GetOrCreateMaterial()
        {
            if (bondMaterial != null)
                return bondMaterial;

            var shader =
                Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard");

            bondMaterial = new Material(shader)
            {
                name = "BondMaterial_Auto"
            };

            bondMaterial.color = bondColor;

            return bondMaterial;
        }

        private static Mesh GetOrCreateCylinderMesh()
        {
            if (_cylinderMesh != null)
                return _cylinderMesh;

            var tmp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

            _cylinderMesh =
                tmp.GetComponent<MeshFilter>().sharedMesh;

            Destroy(tmp);

            return _cylinderMesh;
        }






        public Dictionary<string, int> GetCompositionDictionary()
        {
            Dictionary<string, int> result =
                new Dictionary<string, int>();

            Bond[] bonds =
                FindObjectsByType<Bond>(FindObjectsSortMode.None);

            HashSet<Atom> uniqueAtoms =
                new HashSet<Atom>();

            foreach (var bond in bonds)
            {
                if (bond == null)
                    continue;

                if (bond.A != null)
                    uniqueAtoms.Add(bond.A);

                if (bond.B != null)
                    uniqueAtoms.Add(bond.B);
            }

            foreach (var atom in uniqueAtoms)
            {
                if (atom == null || atom.Element == null)
                    continue;

                string symbol = atom.Element.Symbol;

                if (result.ContainsKey(symbol))
                    result[symbol]++;
                else
                    result.Add(symbol, 1);
            }

            return result;
        }
    }
}
