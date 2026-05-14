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
            switch (atomicNumber)
            {
                case 1:  pos = new GridPosition { Period = 1, Group = 1  }; return true;
                case 2:  pos = new GridPosition { Period = 1, Group = 18 }; return true;
                case 3:  pos = new GridPosition { Period = 2, Group = 1  }; return true;
                case 4:  pos = new GridPosition { Period = 2, Group = 2  }; return true;
                case 5:  pos = new GridPosition { Period = 2, Group = 13 }; return true;
                case 6:  pos = new GridPosition { Period = 2, Group = 14 }; return true;
                case 7:  pos = new GridPosition { Period = 2, Group = 15 }; return true;
                case 8:  pos = new GridPosition { Period = 2, Group = 16 }; return true;
                case 9:  pos = new GridPosition { Period = 2, Group = 17 }; return true;
                case 10: pos = new GridPosition { Period = 2, Group = 18 }; return true;
                case 11: pos = new GridPosition { Period = 3, Group = 1  }; return true;
                case 12: pos = new GridPosition { Period = 3, Group = 2  }; return true;
                case 13: pos = new GridPosition { Period = 3, Group = 13 }; return true;
                case 14: pos = new GridPosition { Period = 3, Group = 14 }; return true;
                case 15: pos = new GridPosition { Period = 3, Group = 15 }; return true;
                case 16: pos = new GridPosition { Period = 3, Group = 16 }; return true;
                case 17: pos = new GridPosition { Period = 3, Group = 17 }; return true;
                case 18: pos = new GridPosition { Period = 3, Group = 18 }; return true;
                case 19: pos = new GridPosition { Period = 4, Group = 1  }; return true;
                case 20: pos = new GridPosition { Period = 4, Group = 2  }; return true;
                case 21: pos = new GridPosition { Period = 4, Group = 3  }; return true;
                case 22: pos = new GridPosition { Period = 4, Group = 4  }; return true;
                case 23: pos = new GridPosition { Period = 4, Group = 5  }; return true;
                case 24: pos = new GridPosition { Period = 4, Group = 6  }; return true;
                case 25: pos = new GridPosition { Period = 4, Group = 7  }; return true;
                case 26: pos = new GridPosition { Period = 4, Group = 8  }; return true;
                case 27: pos = new GridPosition { Period = 4, Group = 9  }; return true;
                case 28: pos = new GridPosition { Period = 4, Group = 10 }; return true;
                case 29: pos = new GridPosition { Period = 4, Group = 11 }; return true;
                case 30: pos = new GridPosition { Period = 4, Group = 12 }; return true;
                default: return false;
            }
        }
    }
}
