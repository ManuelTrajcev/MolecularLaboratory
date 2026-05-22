using System;
using System.Collections;
using System.Collections.Generic;
using MolecularLab.Chemistry;
using UnityEngine;

namespace MolecularLab.Interaction
{
    /// <summary>
    /// Trigger volume that tracks closed molecules (identified by MoleculeTag)
    /// currently inside. When contents match the active recipe, consumes the
    /// inputs and spawns outputs.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ReactionChamber : MonoBehaviour
    {
        public event Action<IReadOnlyDictionary<CompoundSO, int>> ContentsChanged;
        public event Action<string> MoleculeRejected;
        [SerializeField] private Transform outputAnchor;
        [SerializeField] private float outputSpread = 0.1f;
        [SerializeField] private GameObject atomPrefab; // fallback for compounds without productPrefab
        [SerializeField, Min(0f)] private float acceptHorizontalPadding = 0.2f;
        [SerializeField, Min(0f)] private float acceptVerticalPadding = 0.8f;
        [SerializeField, Min(0f)] private float combineDelay = 0.75f;
        [SerializeField] private bool debugLog = false;

        private ReactionRecipeSO _recipe;
        private readonly HashSet<MoleculeTag> _inside = new HashSet<MoleculeTag>();
        private readonly HashSet<Atom> _stagedAtoms = new HashSet<Atom>();
        private readonly Dictionary<CompoundSO, int> _contents = new Dictionary<CompoundSO, int>();
        private bool _armed;
        private bool _reacting;
        private bool _holdOutputs;

        public event Action<ReactionRecipeSO> RecipeReacted;
        public IReadOnlyDictionary<CompoundSO, int> Contents => _contents;
        public bool IsAtomStaged(Atom atom) => atom != null && _stagedAtoms.Contains(atom);

        public void ClearAllContents()
        {
            foreach (var tag in new List<MoleculeTag>(_inside))
            {
                if (tag != null)
                    DestroyMoleculeOf(tag);
            }

            var atoms = FindObjectsByType<Atom>(FindObjectsSortMode.None);
            for (int i = 0; i < atoms.Length; i++)
            {
                var atom = atoms[i];
                if (atom == null) continue;

                Vector3 p = atom.transform.position;
                if (_trigger != null && _trigger.ClosestPoint(p) == p)
                    Destroy(atom.gameObject);
            }

            _inside.Clear();
            _contents.Clear();
            _stagedAtoms.Clear();
            _holdOutputs = false;
        }

        public void SetRecipe(ReactionRecipeSO recipe, bool armed)
        {
            _recipe = recipe;
            _armed = armed;
            _holdOutputs = false;
            if (debugLog) Debug.Log($"[Chamber] recipe={(recipe != null ? recipe.DisplayName : "<none>")} armed={armed}");
            EvaluateRecipe();
        }

        public void SetArmed(bool armed)
        {
            _armed = armed;
            EvaluateRecipe();
        }

        private Collider _trigger;

        private void Awake()
        {
            if (outputAnchor == null) outputAnchor = transform;
            _trigger = GetComponent<Collider>();
            _trigger.isTrigger = true;
        }

        private void OnEnable()
        {
            var ident = MoleculeIdentifier.Instance ?? FindFirstObjectByType<MoleculeIdentifier>();
            if (ident != null) ident.MoleculeFormed += OnMoleculeFormed;
        }

        private void OnDisable()
        {
            var ident = MoleculeIdentifier.Instance;
            if (ident != null) ident.MoleculeFormed -= OnMoleculeFormed;
        }

        private void LateUpdate()
        {
            if (_holdOutputs)
                return;
            RefreshContentsFromScene();
        }

        /// <summary>
        /// Catches the case where a molecule is *built inside* the chamber — no
        /// OnTriggerEnter fires for the act of bonding, so we hook the formation
        /// event and check if any of the molecule's atoms sit inside our volume.
        /// </summary>
        private void OnMoleculeFormed(CompoundSO compound, MoleculeTag tag)
        {
            if (tag == null || tag.Owner == null || _trigger == null) return;
            RefreshContentsFromScene();
        }

        private void OnTriggerEnter(Collider other)
        {
            var tag = ResolveTag(other);
            if (tag == null) return;
            if (!_inside.Add(tag)) return;
            tag.Broken += OnTagBroken;
            Increment(tag.Compound, +1);
            if (debugLog) Debug.Log($"[Chamber] +{tag.Compound.Formula} → {Describe()}");
            EvaluateRecipe();
        }

        private void OnTriggerExit(Collider other)
        {
            var tag = ResolveTag(other);
            if (tag == null) return;
            if (!_inside.Remove(tag)) return;
            tag.Broken -= OnTagBroken;
            Increment(tag.Compound, -1);
            if (debugLog) Debug.Log($"[Chamber] -{tag.Compound.Formula} → {Describe()}");
            EvaluateRecipe();
        }

        private void OnTagBroken(MoleculeTag tag)
        {
            if (!_inside.Remove(tag)) return;
            tag.Broken -= OnTagBroken;
            if (tag.Compound != null) Increment(tag.Compound, -1);
            if (debugLog) Debug.Log($"[Chamber] broken {tag.Compound?.Formula} → {Describe()}");
            EvaluateRecipe();
        }

        public void RefreshContentsFromScene()
        {
            if (_trigger == null) return;

            var identifier = MoleculeIdentifier.Instance ?? FindFirstObjectByType<MoleculeIdentifier>();
            if (identifier == null) return;

            var nextInside = new HashSet<MoleculeTag>();
            var nextContents = new Dictionary<CompoundSO, int>();
            var active = identifier.ActiveTags;

            for (int i = 0; i < active.Count; i++)
            {
                var tag = active[i];
                if (tag == null || tag.Owner == null || tag.Compound == null) continue;
                if (!IsInside(tag)) continue;

                nextInside.Add(tag);
                if (nextContents.TryGetValue(tag.Compound, out var count))
                    nextContents[tag.Compound] = count + 1;
                else
                    nextContents[tag.Compound] = 1;
            }

            if (SetsEqual(_inside, nextInside) && DictionariesEqual(_contents, nextContents))
                return;

            foreach (var oldTag in _inside)
            {
                if (oldTag != null && !nextInside.Contains(oldTag))
                    oldTag.Broken -= OnTagBroken;
            }

            foreach (var newTag in nextInside)
            {
                if (newTag != null && !_inside.Contains(newTag))
                    newTag.Broken += OnTagBroken;
            }

            _inside.Clear();
            foreach (var tag in nextInside) _inside.Add(tag);

            _contents.Clear();
            foreach (var kv in nextContents) _contents[kv.Key] = kv.Value;

            _stagedAtoms.Clear();
            foreach (var tag in _inside)
            {
                if (tag == null || tag.Owner == null) continue;
                var snap = Molecule.BuildFrom(tag.Owner);
                for (int i = 0; i < snap.Atoms.Count; i++)
                {
                    if (snap.Atoms[i] != null) _stagedAtoms.Add(snap.Atoms[i]);
                }
            }

            if (debugLog) Debug.Log($"[Chamber] refresh → {Describe()}");
            ContentsChanged?.Invoke(_contents);
            EvaluateRecipe();
        }

        private bool IsInside(MoleculeTag tag)
        {
            var snap = Molecule.BuildFrom(tag.Owner);
            if (IsBeingDragged(snap))
                return false;

            for (int i = 0; i < snap.Atoms.Count; i++)
            {
                var atom = snap.Atoms[i];
                if (atom == null) continue;
                Vector3 p = atom.transform.position;
                if (_trigger.ClosestPoint(p) == p)
                    return true;
            }

            return false;
        }

        private static bool IsBeingDragged(Molecule.Snapshot snap)
        {
            for (int i = 0; i < snap.Atoms.Count; i++)
            {
                var atom = snap.Atoms[i];
                if (atom == null) continue;

                var sensor = atom.GetComponent<AtomGrabSensor>();
                if (sensor != null && sensor.IsDraggingWholeMolecule)
                    return true;
            }

            return false;
        }

        public ChamberAcceptResult TryAcceptReleasedMolecule(Atom seed)
        {
            if (seed == null || _trigger == null) return ChamberAcceptResult.TooFar;

            var snap = Molecule.BuildFrom(seed);
            if (snap.Atoms.Count == 0) return ChamberAcceptResult.TooFar;

            Vector3 centroid = Vector3.zero;
            int count = 0;
            for (int i = 0; i < snap.Atoms.Count; i++)
            {
                var atom = snap.Atoms[i];
                if (atom == null) continue;
                centroid += atom.transform.position;
                count++;
            }
            if (count == 0) return ChamberAcceptResult.TooFar;
            centroid /= count;

            Bounds bounds = _trigger.bounds;
            bounds.Expand(new Vector3(acceptHorizontalPadding * 2f, acceptVerticalPadding * 2f, acceptHorizontalPadding * 2f));
            if (!bounds.Contains(centroid))
                return ChamberAcceptResult.TooFar;

            var tag = ResolveTag(seed);
            if (tag == null || tag.Compound == null)
            {
                Reject("This molecule is not a valid ingredient for the chamber.");
                return ChamberAcceptResult.Rejected;
            }

            if (!CanStage(tag.Compound, out string reason))
            {
                Reject(reason);
                return ChamberAcceptResult.Rejected;
            }

            Vector3 target = _trigger.bounds.center;
            target.y = Mathf.Max(_trigger.bounds.center.y, _trigger.bounds.max.y - 0.05f);
            Vector3 delta = target - centroid;

            for (int i = 0; i < snap.Atoms.Count; i++)
            {
                var atom = snap.Atoms[i];
                if (atom == null) continue;
                Vector3 next = atom.transform.position + delta;
                atom.transform.position = next;
                if (atom.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.position = next;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                atom.Freeze();
            }

            if (debugLog) Debug.Log($"[Chamber] accepted molecule near chamber ({snap.Atoms.Count} atoms)");
            RefreshContentsFromScene();
            return ChamberAcceptResult.Accepted;
        }

        private static bool SetsEqual(HashSet<MoleculeTag> a, HashSet<MoleculeTag> b)
        {
            if (a.Count != b.Count) return false;
            foreach (var item in a)
            {
                if (!b.Contains(item)) return false;
            }
            return true;
        }

        private static bool DictionariesEqual(Dictionary<CompoundSO, int> a, Dictionary<CompoundSO, int> b)
        {
            if (a.Count != b.Count) return false;
            foreach (var kv in a)
            {
                if (!b.TryGetValue(kv.Key, out var other) || other != kv.Value)
                    return false;
            }
            return true;
        }

        private MoleculeTag ResolveTag(Collider other)
        {
            // The collider sits on an atom. The MoleculeTag lives on the canonical
            // atom of the connected component. Walk via the atom's bonds to find it.
            var atom = other.GetComponentInParent<Atom>();
            return ResolveTag(atom);
        }

        private MoleculeTag ResolveTag(Atom atom)
        {
            if (atom == null) return null;
            var snap = Molecule.BuildFrom(atom);
            for (int i = 0; i < snap.Atoms.Count; i++)
            {
                var t = snap.Atoms[i].GetComponent<MoleculeTag>();
                if (t != null) return t;
            }
            return null;
        }

        private void Increment(CompoundSO compound, int delta)
        {
            if (compound == null) return;
            _contents.TryGetValue(compound, out var c);
            int next = c + delta;
            if (next <= 0) _contents.Remove(compound);
            else _contents[compound] = next;
        }

        private void EvaluateRecipe()
        {
            if (_reacting || !_armed || _recipe == null) return;
            if (!_recipe.Matches(_contents)) return;

            if (debugLog) Debug.Log($"[Chamber] REACT: {_recipe.DisplayName}");
            StartCoroutine(ReactSequence(_recipe));
        }

        private IEnumerator ReactSequence(ReactionRecipeSO recipe)
        {
            _reacting = true;
            yield return new WaitForSeconds(combineDelay);

            List<Atom> consumedAtoms = CollectInsideAtoms();
            if (!TryReuseConsumedAtoms(recipe, consumedAtoms))
            {
                ConsumeInputs();
                SpawnOutputs(recipe);
            }
            PlayFeedback(recipe);

            var fired = recipe;
            _armed = false;
            _recipe = null;
            _reacting = false;
            RecipeReacted?.Invoke(fired);
        }

        private void ConsumeInputs()
        {
            // Destroy every atom of every molecule currently inside that's part of the recipe.
            // Bond.OnDestroy releases joints automatically.
            foreach (var tag in new List<MoleculeTag>(_inside))
            {
                if (tag == null) continue;
                DestroyMoleculeOf(tag);
            }
            _inside.Clear();
            _contents.Clear();
            _stagedAtoms.Clear();
            _holdOutputs = false;
        }

        private List<Atom> CollectInsideAtoms()
        {
            var collected = new List<Atom>();
            var seen = new HashSet<Atom>();

            foreach (var tag in _inside)
            {
                if (tag == null || tag.Owner == null)
                    continue;

                var snap = Molecule.BuildFrom(tag.Owner);
                for (int i = 0; i < snap.Atoms.Count; i++)
                {
                    var atom = snap.Atoms[i];
                    if (atom != null && seen.Add(atom))
                        collected.Add(atom);
                }
            }

            return collected;
        }

        private bool TryReuseConsumedAtoms(ReactionRecipeSO recipe, List<Atom> atoms)
        {
            if (recipe == null || atoms == null || atoms.Count == 0)
                return false;

            var required = BuildOutputElementList(recipe);
            if (required == null || required.Count != atoms.Count)
                return false;

            var pools = new Dictionary<ElementSO, Queue<Atom>>();
            for (int i = 0; i < atoms.Count; i++)
            {
                var atom = atoms[i];
                if (atom == null || atom.Element == null)
                    return false;

                if (!pools.TryGetValue(atom.Element, out var queue))
                {
                    queue = new Queue<Atom>();
                    pools[atom.Element] = queue;
                }
                queue.Enqueue(atom);
            }

            if (!MatchesElementPools(required, pools))
                return false;

            DisableAndDestroyBonds(atoms);
            _inside.Clear();
            _contents.Clear();
            _stagedAtoms.Clear();

            Vector3 anchor = GetOutputSpawnCenter();
            int moleculeIndex = 0;

            foreach (var outc in recipe.Outputs)
            {
                if (outc.compound == null)
                    return false;

                for (int countIndex = 0; countIndex < outc.count; countIndex++)
                {
                    var moleculeAtoms = new List<Atom>();
                    foreach (var ec in outc.compound.Inputs)
                    {
                        if (ec.element == null || !pools.TryGetValue(ec.element, out var queue))
                            return false;

                        for (int n = 0; n < ec.count; n++)
                        {
                            if (queue.Count == 0)
                                return false;
                            moleculeAtoms.Add(queue.Dequeue());
                        }
                    }

                    LayoutExplicitProduct(moleculeAtoms, anchor, moleculeIndex);
                    BondExplicitProduct(moleculeAtoms);
                    StageSpawnedAtoms(moleculeAtoms);
                    moleculeIndex++;
                }
            }

            _holdOutputs = true;
            return true;
        }

        private static List<ElementSO> BuildOutputElementList(ReactionRecipeSO recipe)
        {
            var required = new List<ElementSO>();
            if (recipe == null)
                return required;

            foreach (var outc in recipe.Outputs)
            {
                if (outc.compound == null)
                    return null;

                for (int produced = 0; produced < outc.count; produced++)
                {
                    foreach (var ec in outc.compound.Inputs)
                    {
                        if (ec.element == null)
                            return null;

                        for (int n = 0; n < ec.count; n++)
                            required.Add(ec.element);
                    }
                }
            }

            return required;
        }

        private static bool MatchesElementPools(List<ElementSO> required, Dictionary<ElementSO, Queue<Atom>> pools)
        {
            var needed = new Dictionary<ElementSO, int>();
            for (int i = 0; i < required.Count; i++)
            {
                var element = required[i];
                if (element == null)
                    return false;

                needed.TryGetValue(element, out int count);
                needed[element] = count + 1;
            }

            if (needed.Count != pools.Count)
                return false;

            foreach (var kv in needed)
            {
                if (!pools.TryGetValue(kv.Key, out var queue) || queue.Count != kv.Value)
                    return false;
            }

            return true;
        }

        private static void DisableAndDestroyBonds(IReadOnlyList<Atom> atoms)
        {
            if (atoms == null)
                return;

            var seen = new HashSet<Bond>();
            for (int i = 0; i < atoms.Count; i++)
            {
                var atom = atoms[i];
                if (atom == null)
                    continue;

                var bonds = atom.Bonds;
                for (int b = bonds.Count - 1; b >= 0; b--)
                {
                    var bond = bonds[b];
                    if (bond == null || !seen.Add(bond))
                        continue;

                    bond.BreakImmediately();
                }
            }
        }

        private void LayoutExplicitProduct(List<Atom> atoms, Vector3 anchor, int moleculeIndex)
        {
            if (atoms == null || atoms.Count == 0)
                return;

            Vector3 center = anchor + new Vector3((moleculeIndex % 3) * 0.22f, 0f, (moleculeIndex / 3) * 0.18f);
            if (atoms.Count == 2)
            {
                float spacing = ((atoms[0].Element != null ? atoms[0].Element.DisplayRadius : 0.05f)
                    + (atoms[1].Element != null ? atoms[1].Element.DisplayRadius : 0.05f)) * 1.5f;
                spacing = Mathf.Max(spacing, 0.12f);

                PositionAtom(atoms[0], center + new Vector3(-spacing * 0.5f, 0f, 0f));
                PositionAtom(atoms[1], center + new Vector3(spacing * 0.5f, 0f, 0f));
                return;
            }

            for (int i = 0; i < atoms.Count; i++)
            {
                if (atoms[i] != null)
                    PositionAtom(atoms[i], center + GetSpawnOffset(i));
            }
        }

        private static void BondExplicitProduct(List<Atom> atoms)
        {
            if (atoms == null || atoms.Count < 2)
                return;

            var bondManager = BondManager.Instance;
            if (bondManager == null)
                return;

            if (atoms.Count == 2)
            {
                bondManager.TryCreateBond(atoms[0], atoms[1], 1);
                return;
            }

            for (int i = 1; i < atoms.Count; i++)
                bondManager.TryCreateBond(atoms[0], atoms[i], 1);
        }

        private static void DestroyMoleculeOf(MoleculeTag tag)
        {
            if (tag == null || tag.Owner == null) return;
            var snap = Molecule.BuildFrom(tag.Owner);
            // Destroy bonds first so FixedJoints unhook cleanly.
            for (int i = 0; i < snap.Atoms.Count; i++)
            {
                var atom = snap.Atoms[i];
                if (atom == null) continue;
                var bonds = atom.Bonds;
                for (int b = bonds.Count - 1; b >= 0; b--)
                {
                    if (bonds[b] != null)
                        bonds[b].BreakImmediately();
                }
            }
            for (int i = 0; i < snap.Atoms.Count; i++)
            {
                if (snap.Atoms[i] != null)
                {
                    snap.Atoms[i].gameObject.SetActive(false);
                    Destroy(snap.Atoms[i].gameObject);
                }
            }
        }

        private void SpawnOutputs(ReactionRecipeSO recipe)
        {
            Vector3 anchor = GetOutputSpawnCenter();
            int spawnedIndex = 0;
            foreach (var outc in recipe.Outputs)
            {
                if (outc.compound == null) continue;
                for (int k = 0; k < outc.count; k++)
                {
                    Vector3 pos = anchor + new Vector3((spawnedIndex % 3) * 0.22f, 0f, (spawnedIndex / 3) * 0.18f);
                    SpawnCompound(outc.compound, pos);
                    spawnedIndex++;
                }
            }
        }

        private Vector3 GetOutputSpawnCenter()
        {
            if (_trigger != null)
            {
                var bounds = _trigger.bounds;
                return new Vector3(bounds.center.x, bounds.max.y - 0.04f, bounds.center.z);
            }

            return outputAnchor != null ? outputAnchor.position : transform.position;
        }

        private void SpawnCompound(CompoundSO compound, Vector3 pos)
        {
            if (compound.ProductPrefab != null)
            {
                var product = Instantiate(compound.ProductPrefab, pos, Quaternion.identity);
                StageSpawnedAtoms(product.GetComponentsInChildren<Atom>());
                ForceIdentifySpawnedAtoms(product.GetComponentsInChildren<Atom>());
                return;
            }

            if (atomPrefab == null) return;
            if (TrySpawnExplicitDiatomic(compound, pos))
                return;

            var spawnedAtoms = new List<Atom>();
            int idx = 0;
            foreach (var ec in compound.Inputs)
            {
                for (int n = 0; n < ec.count; n++)
                {
                    Vector3 localOffset = GetSpawnOffset(idx);
                    var go = Instantiate(atomPrefab, pos + localOffset, Quaternion.identity);
                    var atom = go.GetComponent<Atom>();
                    if (atom != null)
                    {
                        atom.SetElement(ec.element);
                        spawnedAtoms.Add(atom);
                    }
                    idx++;
                }
            }

            LayoutSpawnedCompound(spawnedAtoms);
            ConnectSpawnedCompound(spawnedAtoms);
            StageSpawnedAtoms(spawnedAtoms);
            ForceIdentifySpawnedAtoms(spawnedAtoms);
        }

        private bool TrySpawnExplicitDiatomic(CompoundSO compound, Vector3 center)
        {
            if (compound == null || compound.Inputs == null || compound.Inputs.Count == 0)
                return false;

            var sequence = new List<ElementSO>();
            for (int i = 0; i < compound.Inputs.Count; i++)
            {
                var ec = compound.Inputs[i];
                if (ec.element == null) return false;

                for (int n = 0; n < ec.count; n++)
                    sequence.Add(ec.element);
            }

            if (sequence.Count != 2)
                return false;

            var atomA = SpawnOutputAtom(sequence[0], center);
            var atomB = SpawnOutputAtom(sequence[1], center);
            if (atomA == null || atomB == null)
                return false;

            float spacing = ((sequence[0] != null ? sequence[0].DisplayRadius : 0.05f)
                + (sequence[1] != null ? sequence[1].DisplayRadius : 0.05f)) * 1.5f;
            spacing = Mathf.Max(spacing, 0.12f);

            PositionAtom(atomA, center + new Vector3(-spacing * 0.5f, 0f, 0f));
            PositionAtom(atomB, center + new Vector3(spacing * 0.5f, 0f, 0f));

            var pair = new List<Atom> { atomA, atomB };
            var bondManager = BondManager.Instance;
            if (bondManager != null)
                bondManager.TryCreateBond(atomA, atomB, 1);

            StageSpawnedAtoms(pair);
            ForceIdentifySpawnedAtoms(pair);
            return true;
        }

        private void LayoutSpawnedCompound(List<Atom> atoms)
        {
            if (atoms == null || atoms.Count == 0)
                return;

            if (atoms.Count == 2)
            {
                PositionAtom(atoms[0], atoms[0].transform.position);
                PositionAtom(atoms[1], atoms[0].transform.position + new Vector3(0.14f, 0f, 0f));
                return;
            }

            for (int i = 0; i < atoms.Count; i++)
            {
                if (atoms[i] == null) continue;
                PositionAtom(atoms[i], atoms[0].transform.position + GetSpawnOffset(i));
            }
        }

        private void ConnectSpawnedCompound(List<Atom> atoms)
        {
            if (atoms == null || atoms.Count < 2) return;

            var bondManager = BondManager.Instance;
            if (bondManager == null) return;

            var roots = new List<Atom>(atoms);
            roots.Sort(CompareBondPriority);

            var connected = new List<Atom> { roots[0] };
            for (int i = 1; i < roots.Count; i++)
            {
                var atom = roots[i];
                Atom parent = FindBestBondParent(atom, connected);
                if (parent == null)
                    continue;

                bondManager.TryCreateBond(parent, atom, 1);
                connected.Add(atom);
            }
        }

        private static Atom FindBestBondParent(Atom atom, List<Atom> connected)
        {
            Atom best = null;
            int bestValence = int.MinValue;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < connected.Count; i++)
            {
                var candidate = connected[i];
                if (candidate == null || !candidate.CanBond() || !atom.CanBond())
                    continue;

                int valence = candidate.RemainingValence;
                float distance = Vector3.Distance(candidate.transform.position, atom.transform.position);
                if (valence > bestValence || (valence == bestValence && distance < bestDistance))
                {
                    best = candidate;
                    bestValence = valence;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private static int CompareBondPriority(Atom a, Atom b)
        {
            int aValence = a != null ? a.RemainingValence : 0;
            int bValence = b != null ? b.RemainingValence : 0;
            int byValence = bValence.CompareTo(aValence);
            if (byValence != 0) return byValence;

            bool aIsHydrogen = a != null && a.Element != null && a.Element.Symbol == "H";
            bool bIsHydrogen = b != null && b.Element != null && b.Element.Symbol == "H";
            if (aIsHydrogen != bIsHydrogen)
                return aIsHydrogen ? 1 : -1;

            string aSymbol = a != null && a.Element != null ? a.Element.Symbol : string.Empty;
            string bSymbol = b != null && b.Element != null ? b.Element.Symbol : string.Empty;
            return string.CompareOrdinal(aSymbol, bSymbol);
        }

        private void StageSpawnedAtoms(IReadOnlyList<Atom> atoms)
        {
            if (atoms == null) return;

            for (int i = 0; i < atoms.Count; i++)
            {
                var atom = atoms[i];
                if (atom == null) continue;

                _stagedAtoms.Add(atom);
                atom.Freeze();
                if (atom.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }

        private void ForceIdentifySpawnedAtoms(IReadOnlyList<Atom> atoms)
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

        private Atom SpawnOutputAtom(ElementSO element, Vector3 position)
        {
            if (atomPrefab == null || element == null)
                return null;

            var go = Instantiate(atomPrefab, position, Quaternion.identity);
            var atom = go.GetComponent<Atom>();
            if (atom != null)
                atom.SetElement(element);
            return atom;
        }

        private static Vector3 GetSpawnOffset(int index)
        {
            return index switch
            {
                0 => new Vector3(-0.04f, 0f, 0f),
                1 => new Vector3(0.04f, 0f, 0f),
                2 => new Vector3(0f, 0.07f, 0f),
                3 => new Vector3(0f, -0.07f, 0f),
                _ => new Vector3(index * 0.05f, 0f, 0f),
            };
        }

        private void PlayFeedback(ReactionRecipeSO recipe)
        {
            if (recipe.EffectPrefab != null)
                Instantiate(recipe.EffectPrefab, transform.position, Quaternion.identity);
            if (recipe.Sfx != null)
                AudioSource.PlayClipAtPoint(recipe.Sfx, transform.position);
        }

        private bool CanStage(CompoundSO compound, out string reason)
        {
            reason = "";
            if (compound == null)
            {
                reason = "Unknown molecule.";
                return false;
            }

            if (_recipe == null)
            {
                reason = "No active chamber recipe.";
                return false;
            }

            int required = 0;
            for (int i = 0; i < _recipe.Inputs.Count; i++)
            {
                var input = _recipe.Inputs[i];
                if (input.compound == compound)
                {
                    required = input.count;
                    break;
                }
            }

            if (required <= 0)
            {
                reason = $"{compound.Formula} is not required for this level.";
                return false;
            }

            _contents.TryGetValue(compound, out int current);
            if (current >= required)
            {
                reason = $"You already placed enough {compound.Formula}.";
                return false;
            }

            return true;
        }

        private void Reject(string message)
        {
            if (debugLog) Debug.Log($"[Chamber] reject: {message}");
            MoleculeRejected?.Invoke(message);
        }

        public enum ChamberAcceptResult
        {
            TooFar,
            Accepted,
            Rejected,
        }

        private string Describe()
        {
            var sb = new System.Text.StringBuilder("{");
            bool first = true;
            foreach (var kv in _contents)
            {
                if (!first) sb.Append(", ");
                sb.Append(kv.Value).Append("×").Append(kv.Key.Formula);
                first = false;
            }
            sb.Append("}");
            return sb.ToString();
        }
    }
}
