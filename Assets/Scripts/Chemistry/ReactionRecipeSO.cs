using System;
using System.Collections.Generic;
using UnityEngine;

namespace MolecularLab.Chemistry
{
    /// <summary>
    /// Multi-molecule reaction: consumes whole CompoundSOs (already-built molecules)
    /// and produces other CompoundSOs. Triggered by ReactionChamber when its
    /// contents exactly match the input multiset.
    /// </summary>
    [CreateAssetMenu(menuName = "MolecularLab/Reaction Recipe", fileName = "ReactionRecipe", order = 4)]
    public class ReactionRecipeSO : ScriptableObject
    {
        [Serializable]
        public struct CompoundCount
        {
            public CompoundSO compound;
            [Min(1)] public int count;
        }

        [SerializeField] private string displayName = "";
        [SerializeField] private List<CompoundCount> inputs = new List<CompoundCount>();
        [SerializeField] private List<CompoundCount> outputs = new List<CompoundCount>();
        [SerializeField] private GameObject effectPrefab;
        [SerializeField] private AudioClip sfx;

        public string DisplayName => displayName;
        public IReadOnlyList<CompoundCount> Inputs => inputs;
        public IReadOnlyList<CompoundCount> Outputs => outputs;
        public GameObject EffectPrefab => effectPrefab;
        public AudioClip Sfx => sfx;

        /// <summary>Exact multiset match: chamber contents must equal inputs key-for-key.</summary>
        public bool Matches(IReadOnlyDictionary<CompoundSO, int> contents)
        {
            if (contents == null) return false;

            for (int i = 0; i < inputs.Count; i++)
            {
                var ic = inputs[i];
                if (ic.compound == null) return false;
                if (CountEquivalent(contents, ic.compound) != ic.count) return false;
            }

            foreach (var kv in contents)
            {
                if (kv.Value <= 0)
                    continue;

                bool expected = false;
                for (int i = 0; i < inputs.Count; i++)
                {
                    if (AreEquivalent(kv.Key, inputs[i].compound))
                    {
                        expected = true;
                        break;
                    }
                }

                if (!expected)
                    return false;
            }

            return true;
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
    }
}
