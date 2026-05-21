using System;
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
        [SerializeField] private Transform outputAnchor;
        [SerializeField] private float outputSpread = 0.1f;
        [SerializeField] private GameObject atomPrefab; // fallback for compounds without productPrefab
        [SerializeField, Min(0f)] private float acceptHorizontalPadding = 0.2f;
        [SerializeField, Min(0f)] private float acceptVerticalPadding = 0.8f;
        [SerializeField] private bool debugLog = false;

        private ReactionRecipeSO _recipe;
        private readonly HashSet<MoleculeTag> _inside = new HashSet<MoleculeTag>();
        private readonly HashSet<Atom> _stagedAtoms = new HashSet<Atom>();
        private readonly Dictionary<CompoundSO, int> _contents = new Dictionary<CompoundSO, int>();
        private bool _armed;

        public event Action<ReactionRecipeSO> RecipeReacted;
        public IReadOnlyDictionary<CompoundSO, int> Contents => _contents;
        public bool IsAtomStaged(Atom atom) => atom != null && _stagedAtoms.Contains(atom);

        public void SetRecipe(ReactionRecipeSO recipe, bool armed)
        {
            _recipe = recipe;
            _armed = armed;
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

        public bool TryAcceptReleasedMolecule(Atom seed)
        {
            if (seed == null || _trigger == null) return false;

            var snap = Molecule.BuildFrom(seed);
            if (snap.Atoms.Count == 0) return false;

            Vector3 centroid = Vector3.zero;
            int count = 0;
            for (int i = 0; i < snap.Atoms.Count; i++)
            {
                var atom = snap.Atoms[i];
                if (atom == null) continue;
                centroid += atom.transform.position;
                count++;
            }
            if (count == 0) return false;
            centroid /= count;

            Bounds bounds = _trigger.bounds;
            bounds.Expand(new Vector3(acceptHorizontalPadding * 2f, acceptVerticalPadding * 2f, acceptHorizontalPadding * 2f));
            if (!bounds.Contains(centroid))
                return false;

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
            return true;
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
            if (!_armed || _recipe == null) return;
            if (!_recipe.Matches(_contents)) return;

            if (debugLog) Debug.Log($"[Chamber] REACT: {_recipe.DisplayName}");

            ConsumeInputs();
            SpawnOutputs();
            PlayFeedback();

            var fired = _recipe;
            _armed = false;
            _recipe = null;
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
                    if (bonds[b] != null) Destroy(bonds[b].gameObject);
                }
            }
            for (int i = 0; i < snap.Atoms.Count; i++)
            {
                if (snap.Atoms[i] != null) Destroy(snap.Atoms[i].gameObject);
            }
        }

        private void SpawnOutputs()
        {
            Vector3 anchor = outputAnchor.position;
            int spawnedIndex = 0;
            foreach (var outc in _recipe.Outputs)
            {
                if (outc.compound == null) continue;
                for (int k = 0; k < outc.count; k++)
                {
                    Vector3 pos = anchor + UnityEngine.Random.insideUnitSphere * outputSpread;
                    SpawnCompound(outc.compound, pos);
                    spawnedIndex++;
                }
            }
        }

        private void SpawnCompound(CompoundSO compound, Vector3 pos)
        {
            if (compound.ProductPrefab != null)
            {
                Instantiate(compound.ProductPrefab, pos, Quaternion.identity);
                return;
            }
            // Fallback: spawn loose atoms matching composition; they will bond
            // naturally if proximity allows (or the user can bond them by hand).
            if (atomPrefab == null) return;
            int idx = 0;
            foreach (var ec in compound.Inputs)
            {
                for (int n = 0; n < ec.count; n++)
                {
                    var go = Instantiate(atomPrefab, pos + new Vector3(idx * 0.06f, 0f, 0f), Quaternion.identity);
                    var atom = go.GetComponent<Atom>();
                    if (atom != null) atom.SetElement(ec.element);
                    idx++;
                }
            }
        }

        private void PlayFeedback()
        {
            if (_recipe.EffectPrefab != null)
                Instantiate(_recipe.EffectPrefab, transform.position, Quaternion.identity);
            if (_recipe.Sfx != null)
                AudioSource.PlayClipAtPoint(_recipe.Sfx, transform.position);
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
