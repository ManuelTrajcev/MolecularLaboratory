using System;
using UnityEngine;

namespace MolecularLab.Chemistry
{
    /// <summary>
    /// Attached at runtime by <see cref="MoleculeIdentifier"/> to the canonical
    /// atom (lowest instance id) of a closed molecule that matches a CompoundSO.
    /// The chamber and level system read .Compound to count molecule instances
    /// without re-running BFS.
    /// </summary>
    public class MoleculeTag : MonoBehaviour
    {
        public CompoundSO Compound { get; private set; }
        public Atom Owner { get; private set; }

        public event Action<MoleculeTag> Broken;

        public void Initialize(CompoundSO compound, Atom owner)
        {
            Compound = compound;
            Owner = owner;
        }

        /// <summary>Raised by MoleculeIdentifier when the molecule no longer matches.</summary>
        public void NotifyBroken()
        {
            Broken?.Invoke(this);
            Destroy(this);
        }
    }
}
