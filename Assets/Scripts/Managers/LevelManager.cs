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
        private bool _levelCompleted;
        private Coroutine _completionRoutine;

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

            if (chamber != null)
            {
                chamber.RecipeReacted += OnRecipeReacted;
                chamber.ContentsChanged += OnChamberContentsChanged;
                chamber.MoleculeRejected += OnMoleculeRejected;
            }
            if (ui != null)
                ui.SetResetAction(ResetCurrentLevel);

            if (startingLevel != null) SetLevel(startingLevel);
        }

        private void OnDestroy()
        {
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
            _levelCompleted = false;
            if (_completionRoutine != null)
            {
                StopCoroutine(_completionRoutine);
                _completionRoutine = null;
            }

            if (ui != null)
            {
                ui.SetResetAction(ResetCurrentLevel);
                ui.SetLevel(level, _built, _stage1Complete);
            }
            if (chamber != null) chamber.SetRecipe(level != null ? level.Stage2 : null, armed: false);
            if (debugLog) Debug.Log($"[Level] Set: {level?.Title}");
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
            if (_levelCompleted)
                return;

            bool wasComplete = _stage1Complete;
            _stage1Complete = Stage1Met();

            if (_stage1Complete != wasComplete && chamber != null)
                chamber.SetArmed(_stage1Complete);

            if (ui != null) ui.UpdateStage1(_built, _stage1Complete);
            if (debugLog) Debug.Log($"[Level] Stage1 complete={_stage1Complete}");
        }

        private void OnChamberContentsChanged(IReadOnlyDictionary<CompoundSO, int> contents)
        {
            if (_current == null || _levelCompleted) return;

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
            _levelCompleted = true;
            if (debugLog) Debug.Log($"[Level] Completed: {_current.Title}");
            if (_completionRoutine != null)
                StopCoroutine(_completionRoutine);
            _completionRoutine = StartCoroutine(CompleteAndPromptNext());
        }

        private IEnumerator CompleteAndPromptNext()
        {
            yield return new WaitForSeconds(completionDelay);
            if (_current != null && _current.NextLevel != null)
            {
                if (ui != null) ui.ShowNextButton(AdvanceToNextLevel);
            }
            else
            {
                var endScreen = EndScreenController.Instance ?? FindFirstObjectByType<EndScreenController>();
                if (endScreen != null) endScreen.Show();
            }
            _completionRoutine = null;
        }

        private void AdvanceToNextLevel()
        {
            if (_current == null) return;
            ClearRuntimeChemistry();
            SetLevel(_current.NextLevel);
        }

        public void ResetCurrentLevel()
        {
            if (_current == null)
                return;

            ClearRuntimeChemistry();
            SetLevel(_current);
        }

        private void ClearRuntimeChemistry()
        {
            if (_completionRoutine != null)
            {
                StopCoroutine(_completionRoutine);
                _completionRoutine = null;
            }

            if (chamber != null)
                chamber.ClearAllContents();

            var bondObjects = FindObjectsByType<Bond>(FindObjectsSortMode.None);
            for (int i = 0; i < bondObjects.Length; i++)
            {
                if (bondObjects[i] != null)
                    Destroy(bondObjects[i].gameObject);
            }

            var atomRoots = new HashSet<GameObject>();
            var atoms = FindObjectsByType<Atom>(FindObjectsSortMode.None);
            for (int i = 0; i < atoms.Length; i++)
            {
                var atom = atoms[i];
                if (atom == null) continue;
                atomRoots.Add(atom.transform.root.gameObject);
            }

            foreach (var root in atomRoots)
            {
                if (root != null)
                    Destroy(root);
            }

            _built.Clear();
            _stage1Complete = false;
            _levelCompleted = false;
        }

        private void OnMoleculeRejected(string message)
        {
            if (ui != null)
                ui.ShowStatus(message, new Color(1f, 0.45f, 0.45f, 1f), 2.2f);
        }
    }
}
