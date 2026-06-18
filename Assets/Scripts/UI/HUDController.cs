using TMPro;
using UnityEngine;
using MolecularLab.Chemistry;
using MolecularLab.Managers;

namespace MolecularLab.UI
{
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI moleculeNameText;
        [SerializeField] private TextMeshProUGUI bondsText;
        [SerializeField] private TextMeshProUGUI levelText;

        private BondManager _bondManager;
        private LevelManager _levelManager;
        private MoleculeIdentifier _identifier;
        private int _bondCount;

        private void Start()
        {
            _bondManager = FindFirstObjectByType<BondManager>();
            _levelManager = FindFirstObjectByType<LevelManager>();
            _identifier = FindFirstObjectByType<MoleculeIdentifier>();

            if (_bondManager != null)
                _bondManager.BondFormed += OnBondFormed;

            if (_identifier != null)
            {
                _identifier.MoleculeFormed += OnMoleculeFormed;
                _identifier.MoleculeDissolved += OnMoleculeDissolved;
            }

            RefreshLevel();
            SetMoleculeName("—");
            SetBonds(0);
        }

        private void OnBondFormed(Bond bond)
        {
            _bondCount++;
            SetBonds(_bondCount);
        }

        private void OnMoleculeFormed(CompoundSO compound, MoleculeTag tag)
        {
            SetMoleculeName(compound.CompoundName);
        }

        private void OnMoleculeDissolved(CompoundSO compound, MoleculeTag tag)
        {
            SetMoleculeName("—");
        }

        private void Update()
        {
            RefreshLevel();
        }

        private void RefreshLevel()
        {
            if (_levelManager == null || _levelManager.CurrentLevel == null)
            {
                levelText.text = "Level: —";
                return;
            }
            levelText.text = $"Level: {_levelManager.CurrentLevel.Title}";
        }

        private void SetMoleculeName(string name)
        {
            moleculeNameText.text = name;
        }

        private void SetBonds(int count)
        {
            bondsText.text = $"Bonds: {count}";
        }

        private void OnDestroy()
        {
            if (_bondManager != null)
                _bondManager.BondFormed -= OnBondFormed;

            if (_identifier != null)
            {
                _identifier.MoleculeFormed -= OnMoleculeFormed;
                _identifier.MoleculeDissolved -= OnMoleculeDissolved;
            }
        }
    }
}
