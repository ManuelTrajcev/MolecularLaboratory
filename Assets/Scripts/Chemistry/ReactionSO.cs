using System;
using System.Collections.Generic;
using UnityEngine;

namespace MolecularLab.Chemistry
{
    [CreateAssetMenu(menuName = "MolecularLab/Reaction", fileName = "Reaction", order = 1)]
    public class ReactionSO : ScriptableObject
    {
        [Serializable]
        public struct ElementCount
        {
            public ElementSO element;
            [Min(1)] public int count;
        }

        [SerializeField] private string displayName = "Reaction";
        [SerializeField] private List<ElementCount> inputs = new List<ElementCount>();
        [SerializeField] private GameObject effectPrefab;
        [SerializeField] private AudioClip sfx;

        public string DisplayName => displayName;
        public IReadOnlyList<ElementCount> Inputs => inputs;
        public GameObject EffectPrefab => effectPrefab;
        public AudioClip Sfx => sfx;

        public bool Matches(IReadOnlyDictionary<ElementSO, int> counts)
        {
            if (counts == null || counts.Count != inputs.Count) return false;
            for (int i = 0; i < inputs.Count; i++)
            {
                var ec = inputs[i];
                if (ec.element == null) return false;
                if (!counts.TryGetValue(ec.element, out var c) || c != ec.count) return false;
            }
            return true;
        }
        public float MatchProgress(IReadOnlyDictionary<ElementSO, int> counts)
        {
            if (counts == null) return 0f;
            int matched = 0;
            for (int i = 0; i < inputs.Count; i++)
            {
                var ec = inputs[i];
                if (ec.element == null) continue;
                if (counts.TryGetValue(ec.element, out var c))
                    matched += Mathf.Min(c, ec.count);
            }
            int total = 0;
            for (int i = 0; i < inputs.Count; i++) total += inputs[i].count;
            return total > 0 ? (float)matched / total : 0f;
        }
        private void OnValidate()
        {
            // Спој дупликати
            var merged = new Dictionary<ElementSO, int>();
            foreach (var ec in inputs)
            {
                if (ec.element == null) continue;
                merged.TryGetValue(ec.element, out var c);
                merged[ec.element] = c + ec.count;
            }
            inputs.Clear();
            foreach (var kv in merged)
                inputs.Add(new ElementCount { element = kv.Key, count = kv.Value });
        }
    }
}
