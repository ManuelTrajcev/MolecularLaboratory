using System.Collections.Generic;
using UnityEngine;

namespace MolecularLab.Chemistry
{
    [CreateAssetMenu(menuName = "MolecularLab/Level", fileName = "Level", order = 5)]
    public class LevelSO : ScriptableObject
    {
        [SerializeField] private string title = "";
        [SerializeField, TextArea(2, 6)] private string instructions = "";
        [SerializeField] private List<ReactionRecipeSO.CompoundCount> stage1 = new List<ReactionRecipeSO.CompoundCount>();
        [SerializeField] private ReactionRecipeSO stage2;
        [SerializeField] private LevelSO nextLevel;

        public string Title => title;
        public string Instructions => instructions;
        public IReadOnlyList<ReactionRecipeSO.CompoundCount> Stage1 => stage1;
        public ReactionRecipeSO Stage2 => stage2;
        public LevelSO NextLevel => nextLevel;
    }
}
