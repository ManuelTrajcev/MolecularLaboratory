using System.Collections.Generic;

namespace MolecularLab.Chemistry
{
    public static class Molecule
    {
        public struct Snapshot
        {
            public List<Atom> Atoms;
            public Dictionary<ElementSO, int> ElementCounts;

            /// <summary>True only when every atom has zero remaining valence.</summary>
            public bool IsClosed;

            /// <summary>Number of atoms in the component that still have free valence.</summary>
            public int OpenAtomCount;

            /// <summary>
            /// A molecule is "saturated" when no two of its atoms could bond any
            /// further — i.e. at most one atom still carries free valence. This is
            /// the correct completeness test for species like CO (C≡O), where the
            /// symmetric valence model can never drive carbon's count to zero, so
            /// IsClosed alone would wrongly reject a perfectly complete molecule.
            /// </summary>
            public bool IsSaturated => OpenAtomCount <= 1;
        }

        public static Snapshot BuildFrom(Atom seed)
        {
            var snap = new Snapshot
            {
                Atoms = new List<Atom>(),
                ElementCounts = new Dictionary<ElementSO, int>(),
                IsClosed = true,
            };

            if (seed == null) return snap;

            var visited = new HashSet<Atom>();
            var queue = new Queue<Atom>();
            queue.Enqueue(seed);
            visited.Add(seed);

            while (queue.Count > 0)
            {
                var atom = queue.Dequeue();
                snap.Atoms.Add(atom);

                if (atom.Element != null)
                {
                    snap.ElementCounts.TryGetValue(atom.Element, out var c);
                    snap.ElementCounts[atom.Element] = c + 1;
                }

                if (atom.RemainingValence > 0)
                {
                    snap.IsClosed = false;
                    snap.OpenAtomCount++;
                }

                foreach (var bond in atom.Bonds)
                {
                    if (bond == null) continue;
                    var other = bond.A == atom ? bond.B : bond.A;
                    if (other == null || !visited.Add(other)) continue;
                    queue.Enqueue(other);
                }
            }

            return snap;
        }
    }
}
