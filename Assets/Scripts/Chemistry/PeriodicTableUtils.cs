namespace MolecularLab.Chemistry
{
    public static class PeriodicTableUtils
    {
        public struct GridPosition
        {
            public int Period;
            public int Group;
        }

        public static bool TryGetPosition(int atomicNumber, out GridPosition pos)
        {
            pos = default;
            int z = atomicNumber;

            // Period 1
            if (z == 1) { pos = new GridPosition { Period = 1, Group = 1  }; return true; }
            if (z == 2) { pos = new GridPosition { Period = 1, Group = 18 }; return true; }

            // Period 2
            if (z == 3) { pos = new GridPosition { Period = 2, Group = 1  }; return true; }
            if (z == 4) { pos = new GridPosition { Period = 2, Group = 2  }; return true; }
            if (z >= 5 && z <= 10) { pos = new GridPosition { Period = 2, Group = 13 + (z - 5) }; return true; }

            // Period 3
            if (z == 11) { pos = new GridPosition { Period = 3, Group = 1  }; return true; }
            if (z == 12) { pos = new GridPosition { Period = 3, Group = 2  }; return true; }
            if (z >= 13 && z <= 18) { pos = new GridPosition { Period = 3, Group = 13 + (z - 13) }; return true; }

            // Period 4
            if (z == 19) { pos = new GridPosition { Period = 4, Group = 1 }; return true; }
            if (z == 20) { pos = new GridPosition { Period = 4, Group = 2 }; return true; }
            if (z >= 21 && z <= 30) { pos = new GridPosition { Period = 4, Group = 3 + (z - 21) }; return true; }
            if (z >= 31 && z <= 36) { pos = new GridPosition { Period = 4, Group = 13 + (z - 31) }; return true; }

            // Period 5
            if (z == 37) { pos = new GridPosition { Period = 5, Group = 1 }; return true; }
            if (z == 38) { pos = new GridPosition { Period = 5, Group = 2 }; return true; }
            if (z >= 39 && z <= 48) { pos = new GridPosition { Period = 5, Group = 3 + (z - 39) }; return true; }
            if (z >= 49 && z <= 54) { pos = new GridPosition { Period = 5, Group = 13 + (z - 49) }; return true; }

            // Period 6: Cs, Ba, La (group 3), Hf..Hg (groups 4-12), Tl..Rn (groups 13-18)
            if (z == 55) { pos = new GridPosition { Period = 6, Group = 1 }; return true; }
            if (z == 56) { pos = new GridPosition { Period = 6, Group = 2 }; return true; }
            if (z == 57) { pos = new GridPosition { Period = 6, Group = 3 }; return true; }
            if (z >= 58 && z <= 71) { pos = new GridPosition { Period = 8, Group = 4 + (z - 58) }; return true; } // Lanthanides Ce..Lu
            if (z >= 72 && z <= 80) { pos = new GridPosition { Period = 6, Group = 4 + (z - 72) }; return true; }
            if (z >= 81 && z <= 86) { pos = new GridPosition { Period = 6, Group = 13 + (z - 81) }; return true; }

            // Period 7: Fr, Ra, Ac (group 3), Rf..Cn (groups 4-12), Nh..Og (groups 13-18)
            if (z == 87) { pos = new GridPosition { Period = 7, Group = 1 }; return true; }
            if (z == 88) { pos = new GridPosition { Period = 7, Group = 2 }; return true; }
            if (z == 89) { pos = new GridPosition { Period = 7, Group = 3 }; return true; }
            if (z >= 90 && z <= 103) { pos = new GridPosition { Period = 9, Group = 4 + (z - 90) }; return true; } // Actinides Th..Lr
            if (z >= 104 && z <= 112) { pos = new GridPosition { Period = 7, Group = 4 + (z - 104) }; return true; }
            if (z >= 113 && z <= 118) { pos = new GridPosition { Period = 7, Group = 13 + (z - 113) }; return true; }

            return false;
        }
    }
}
