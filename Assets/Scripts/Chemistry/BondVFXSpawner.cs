using UnityEngine;

namespace MolecularLab.Chemistry
{
    public class BondVFXSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject bondVFXPrefab;

        private BondManager _bondManager;

        private void Start()
        {
            _bondManager = GetComponent<BondManager>();
            if (_bondManager == null)
                _bondManager = FindFirstObjectByType<BondManager>();

            if (_bondManager != null)
                _bondManager.BondFormed += OnBondFormed;
        }

        private void OnDestroy()
        {
            if (_bondManager != null)
                _bondManager.BondFormed -= OnBondFormed;
        }

        private void OnBondFormed(Bond bond)
        {
            if (bondVFXPrefab == null || bond == null) return;
            if (bond.A == null || bond.B == null) return;

            Vector3 midpoint = (bond.A.transform.position + bond.B.transform.position) * 0.5f;
            Instantiate(bondVFXPrefab, midpoint, Quaternion.identity);
        }
    }
}
