using System.Collections.Generic;
using UnityEngine;

namespace MolecularLab.Chemistry
{
    public class ReactionSystem : MonoBehaviour
    {
        [SerializeField] private BondManager bondManager;
        [SerializeField] private List<ReactionSO> reactions = new List<ReactionSO>();
        [SerializeField] private bool logToConsole = true;

        private void OnEnable()
        {
            if (bondManager == null) bondManager = FindFirstObjectByType<BondManager>();
            if (bondManager != null) bondManager.BondFormed += OnBondFormed;
        }

        private void OnDisable()
        {
            if (bondManager != null) bondManager.BondFormed -= OnBondFormed;
        }

        private void OnBondFormed(Bond bond)
        {
            if (bond == null || bond.A == null) return;
            var snap = Molecule.BuildFrom(bond.A);
            if (!snap.IsClosed) return;

            for (int i = 0; i < reactions.Count; i++)
            {
                var rxn = reactions[i];
                if (rxn != null && rxn.Matches(snap.ElementCounts))
                {
                    Trigger(rxn, snap);
                    return;
                }
            }
        }

        private void Trigger(ReactionSO rxn, Molecule.Snapshot snap)
        {
            Vector3 center = Vector3.zero;
            for (int i = 0; i < snap.Atoms.Count; i++) center += snap.Atoms[i].transform.position;
            if (snap.Atoms.Count > 0) center /= snap.Atoms.Count;

            if (rxn.EffectPrefab != null)
                Instantiate(rxn.EffectPrefab, center, Quaternion.identity);

            if (rxn.Sfx != null)
                AudioSource.PlayClipAtPoint(rxn.Sfx, center);

            if (logToConsole)
                Debug.Log($"[Reaction] Formed {rxn.DisplayName} ({snap.Atoms.Count} atoms)");
        }
    }
}
