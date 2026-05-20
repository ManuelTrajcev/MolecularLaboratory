using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MolecularLab.Chemistry
{
    /// <summary>
    /// Гради молекуларна формула во Hill нотација од речник ElementSO → број на атоми.
    ///
    /// Hill нотација:
    ///   1. Доколку молекулата содржи Јаглерод (C) — прв C, втор H, потоа останатите азбучно.
    ///   2. Доколку нема Јаглерод — сите елементи азбучно.
    ///   3. Бројот се запишува само ако е > 1.
    ///
    /// Примери: H2O, CO2, NaCl, C2H5OH, H2
    /// </summary>
    public static class MoleculeFormulaBuilder
    {
        public static string Build(IReadOnlyDictionary<ElementSO, int> counts)
        {
            if (counts == null || counts.Count == 0) return "";

            var sb = new StringBuilder();

            ElementSO carbon   = null;
            ElementSO hydrogen = null;

            foreach (var kv in counts)
            {
                if (kv.Key == null) continue;
                if (kv.Key.Symbol == "C") carbon   = kv.Key;
                if (kv.Key.Symbol == "H") hydrogen = kv.Key;
            }

            // ── Hill со јаглерод ──────────────────────────────────────────────
            if (carbon != null)
            {
                AppendElement(sb, carbon, counts[carbon]);
                if (hydrogen != null)
                    AppendElement(sb, hydrogen, counts[hydrogen]);
            }

            // ── Останатите елементи — азбучно ─────────────────────────────────
            var rest = counts.Keys
                .Where(e => e != null && e != carbon && e != hydrogen)
                .OrderBy(e => e.Symbol);

            foreach (var el in rest)
                AppendElement(sb, el, counts[el]);

            // ── Ако нема јаглерод и водород — сите азбучно ───────────────────
            // (Веќе покриено: carbon==null значи горниот if-блок се прескока,
            //  hydrogen е во "rest" ако нема carbon.)

            return sb.ToString();
        }

        // ── Помошна метода ───────────────────────────────────────────────────
        private static void AppendElement(StringBuilder sb, ElementSO el, int count)
        {
            sb.Append(el.Symbol);
            if (count > 1) sb.Append(count);
        }
    }
}