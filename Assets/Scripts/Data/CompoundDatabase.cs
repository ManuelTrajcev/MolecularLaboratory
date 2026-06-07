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

        public CompoundSO FindMatchingCompound(Dictionary<ElementSO, int> elementCounts)
        {
            if (elementCounts == null || elementCounts.Count == 0)
                return null;

            for (int i = 0; i < compounds.Count; i++)
            {
                var compound = compounds[i];
                if (compound != null && compound.Matches(elementCounts))
                    return compound;
            }

            return null;
        }
    }
}