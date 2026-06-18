using System;
using System.Collections.Generic;
using UnityEngine;

namespace MolecularLab.Chemistry
{
    [CreateAssetMenu(menuName = "MolecularLab/Compound", fileName = "Compound", order = 2)]
    public class CompoundSO : ScriptableObject
    {
        [Serializable]
        public struct ElementCount
        {
            public ElementSO element;
            [Min(1)] public int count;
        }

        [Header("Identification")]
        [SerializeField] private string formula = "";

        [SerializeField] private string compoundName = "";

        [SerializeField] private string macedonianName = "";

        [Header("Composition")]
        [SerializeField] private List<ElementCount> inputs = new List<ElementCount>();  
        [Header("Physical Properties")]
        [SerializeField] private float molecularMass = 0f;

        [SerializeField] private AggregateState stateAtRoomTemp = AggregateState.Liquid;

        [Header("Description")]
        [SerializeField, TextArea(2, 5)] private string description = "";

        [Header("2D Structure")]
        [SerializeField, TextArea(3, 8)] private string structure2D = "";

        [Header("Category")]
        [SerializeField] private CompoundCategory category = CompoundCategory.Other;

        [Header("Visuals")]
        [SerializeField] private Color accentColor = new Color(0.2f, 0.6f, 1f, 1f);

        [Header("Product Prefab")]
        [Tooltip("Optional pre-bonded molecule prefab spawned as reaction output. If null, the chamber spawns loose atoms.")]
        [SerializeField] private GameObject productPrefab;

        // ─── Properties ─────────────────────────────────────────────

        public string Formula => formula;
        public string CompoundName => compoundName;
        public string MacedonianName => macedonianName;

        public IReadOnlyList<ElementCount> Inputs => inputs;

        public float MolecularMass => molecularMass;

        public AggregateState StateAtRoomTemp => stateAtRoomTemp;

        public string Description => description;

        public string Structure2D => structure2D;

        public CompoundCategory Category => category;

        public Color AccentColor => accentColor;

        public GameObject ProductPrefab => productPrefab;

        public string MassFormatted => molecularMass > 0f
            ? $"{molecularMass:F3} g/mol"
            : "—";

        public bool Matches(IReadOnlyDictionary<ElementSO, int> counts)
        {
            if (counts == null || counts.Count != inputs.Count)
                return false;

            for (int i = 0; i < inputs.Count; i++)
            {
                var ec = inputs[i];

                if (ec.element == null)
                    return false;

                if (!counts.TryGetValue(ec.element, out var c) || c != ec.count)
                    return false;
            }

            return true;
        }

        public float MatchProgress(IReadOnlyDictionary<ElementSO, int> counts)
        {
            if (counts == null)
                return 0f;

            int matched = 0;

            for (int i = 0; i < inputs.Count; i++)
            {
                var ec = inputs[i];

                if (ec.element == null)
                    continue;

                if (counts.TryGetValue(ec.element, out var c))
                    matched += Mathf.Min(c, ec.count);
            }

            int total = 0;

            for (int i = 0; i < inputs.Count; i++)
                total += inputs[i].count;

            return total > 0 ? (float)matched / total : 0f;
        }

        private void OnValidate()
{
    var merged = new Dictionary<ElementSO, int>();
    var cleaned = new List<ElementCount>();

    foreach (var ec in inputs)
    {
        // Задржи празни entries за Inspector да работи
        if (ec.element == null)
        {
            cleaned.Add(ec);
            continue;
        }

        merged.TryGetValue(ec.element, out var c);
        merged[ec.element] = c + Mathf.Max(1, ec.count);
    }

    inputs.Clear();

    // Додај merged валидни entries
    foreach (var kv in merged)
    {
        inputs.Add(new ElementCount
        {
            element = kv.Key,
            count = kv.Value
        });
    }

    // Додај празни entries назад
    inputs.AddRange(cleaned);
}
    }

    public enum AggregateState
    {
        Gas,
        Liquid,
        Solid,
        Plasma
    }

    public enum CompoundCategory
    {
        Oxide,
        Acid,
        Base,
        Salt,
        OrganicHydrocarbon,
        OrganicOther,
        ElementalMolecule,
        Other
    }
}