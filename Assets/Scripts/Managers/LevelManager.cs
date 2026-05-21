using System.Collections;
using System.Collections.Generic;
using MolecularLab.Chemistry;
using MolecularLab.Interaction;
using MolecularLab.UI;
using UnityEngine;

namespace MolecularLab.Managers
{
    /// <summary>
    /// Orchestrates the two-stage level loop:
    ///   Stage 1: track which intermediate molecules have been built (via MoleculeIdentifier).
    ///   Stage 2: once Stage 1 targets are met, arm the ReactionChamber with the level's recipe.
    ///   On recipe react → advance to nextLevel.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [SerializeField] private LevelSO startingLevel;
        [SerializeField] private LevelObjectiveUI ui;
        [SerializeField] private ReactionChamber chamber;
        [SerializeField] private MoleculeIdentifier identifier;
        [SerializeField] private float completionDelay = 2.5f;
        [SerializeField] private bool debugLog = false;

        private LevelSO _current;
        private readonly Dictionary<CompoundSO, int> _built = new Dictionary<CompoundSO, int>();
        private bool _stage1Complete;

        public LevelSO CurrentLevel => _current;
        public IReadOnlyDictionary<CompoundSO, int> Built => _built;
        public bool Stage1Complete => _stage1Complete;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void Start()
        {
            if (identifier == null) identifier = MoleculeIdentifier.Instance ?? FindFirstObjectByType<MoleculeIdentifier>();
            if (chamber == null) chamber = FindFirstObjectByType<ReactionChamber>();
            if (ui == null) ui = FindFirstObjectByType<LevelObjectiveUI>();

            if (identifier != null)
            {
                identifier.MoleculeFormed += OnMoleculeFormed;
                identifier.MoleculeDissolved += OnMoleculeDissolved;
            }
            if (chamber != null)
            {
                chamber.RecipeReacted += OnRecipeReacted;
                chamber.ContentsChanged += OnChamberContentsChanged;
                chamber.MoleculeRejected += OnMoleculeRejected;
            }

            if (startingLevel != null) SetLevel(startingLevel);
        }

        private void OnDestroy()
        {
            if (identifier != null)
            {
                identifier.MoleculeFormed -= OnMoleculeFormed;
                identifier.MoleculeDissolved -= OnMoleculeDissolved;
            }
            if (chamber != null)
            {
                chamber.RecipeReacted -= OnRecipeReacted;
                chamber.ContentsChanged -= OnChamberContentsChanged;
                chamber.MoleculeRejected -= OnMoleculeRejected;
            }
            if (Instance == this) Instance = null;
        }

        public void SetLevel(LevelSO level)
        {
            _current = level;
            _built.Clear();
            _stage1Complete = false;

            if (ui != null) ui.SetLevel(level, _built, _stage1Complete);
            if (chamber != null) chamber.SetRecipe(level != null ? level.Stage2 : null, armed: false);
            if (debugLog) Debug.Log($"[Level] Set: {level?.Title}");
        }

        private void OnMoleculeFormed(CompoundSO compound, MoleculeTag tag)
        {
            if (_current == null || compound == null) return;
            if (!IsLevelIntermediate(compound)) return;
            _built.TryGetValue(compound, out var c);
            _built[compound] = c + 1;
            Refresh();
        }

        private void OnMoleculeDissolved(CompoundSO compound, MoleculeTag tag)
        {
            if (_current == null || compound == null) return;
            if (!_built.TryGetValue(compound, out var c)) return;
            int next = c - 1;
            if (next <= 0) _built.Remove(compound);
            else _built[compound] = next;
            Refresh();
        }

        private bool IsLevelIntermediate(CompoundSO compound)
        {
            var stage1 = _current.Stage1;
            for (int i = 0; i < stage1.Count; i++)
                if (stage1[i].compound == compound) return true;
            return false;
        }

        private void Refresh()
        {
            bool wasComplete = _stage1Complete;
            _stage1Complete = Stage1Met();

            if (_stage1Complete != wasComplete && chamber != null)
                chamber.SetArmed(_stage1Complete);

            if (ui != null) ui.UpdateStage1(_built, _stage1Complete);
            if (debugLog) Debug.Log($"[Level] Stage1 complete={_stage1Complete}");
        }

        private void OnChamberContentsChanged(IReadOnlyDictionary<CompoundSO, int> contents)
        {
            if (_current == null) return;

            _built.Clear();
            if (contents != null)
            {
                foreach (var kv in contents)
                {
                    if (kv.Key != null && IsLevelIntermediate(kv.Key))
                        _built[kv.Key] = kv.Value;
                }
            }

            Refresh();
        }

        private bool Stage1Met()
        {
            var stage1 = _current.Stage1;
            for (int i = 0; i < stage1.Count; i++)
            {
                var s = stage1[i];
                if (s.compound == null) return false;
                _built.TryGetValue(s.compound, out var c);
                if (c < s.count) return false;
            }
            return true;
        }

        private void OnRecipeReacted(ReactionRecipeSO recipe)
        {
            if (_current == null || _current.Stage2 != recipe) return;
            if (debugLog) Debug.Log($"[Level] Completed: {_current.Title}");
            StartCoroutine(CompleteAndPromptNext());
        }

        private IEnumerator CompleteAndPromptNext()
        {
            string nextTitle = _current.NextLevel != null ? _current.NextLevel.Title : "All levels complete!";
            yield return new WaitForSeconds(completionDelay);
            if (ui != null) ui.ShowCompletion(_current.Title, nextTitle, AdvanceToNextLevel);
        }

        private void AdvanceToNextLevel()
        {
            if (_current == null) return;
            SetLevel(_current.NextLevel);
        }

        private void OnMoleculeRejected(string message)
        {
            if (ui != null)
                ui.ShowStatus(message, new Color(1f, 0.45f, 0.45f, 1f), 2.2f);
        }
    }
}
