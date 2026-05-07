using System.Collections.Generic;

namespace MolecularLab.Chemistry
{
    public static class Molecule
    {
        public struct Snapshot
        {
            public List<Atom> Atoms;
            public Dictionary<ElementSO, int> ElementCounts;
            public bool IsClosed;
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

                if (atom.RemainingValence > 0) snap.IsClosed = false;

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
