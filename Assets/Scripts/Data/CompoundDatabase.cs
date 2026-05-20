using System.Collections.Generic;
using UnityEngine;

namespace MolecularLab.Chemistry
{
    [CreateAssetMenu(
        menuName = "MolecularLab/CompoundDatabase",
        fileName = "CompoundDatabase",
        order = 3)]
    public class CompoundDatabase : ScriptableObject
    {
        [SerializeField]
        private List<CompoundSO> compounds = new List<CompoundSO>();

        public IReadOnlyList<CompoundSO> AllCompounds => compounds;

        public CompoundSO FindMatchingCompound()
        {
            if (BondManager.Instance == null)
                return null;

            // Земи composition од BondManager
            Dictionary<string, int> bonded =
                BondManager.Instance.GetCompositionDictionary();

            if (bonded == null || bonded.Count == 0)
                return null;

            // Провери compounds
            for (int i = 0; i < compounds.Count; i++)
            {
                CompoundSO compound = compounds[i];

                if (compound == null)
                    continue;

                if (Matches(compound, bonded))
                    return compound;
            }

            return null;
        }

        private bool Matches(
            CompoundSO compound,
            Dictionary<string, int> bonded)
        {
            var required = compound.Inputs;

            // Ист број различни елементи
            if (required.Count != bonded.Count)
                return false;

            for (int i = 0; i < required.Count; i++)
            {
                var req = required[i];

                if (req.element == null)
                    return false;

                string symbol = req.element.Symbol;

                // Дали постои елементот
                if (!bonded.TryGetValue(symbol, out int count))
                    return false;

                // Дали count е точен
                if (count != req.count)
                    return false;
            }

            return true;
        }
    }
}