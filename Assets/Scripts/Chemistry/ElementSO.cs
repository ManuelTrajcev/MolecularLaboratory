using UnityEngine;

namespace MolecularLab.Chemistry
{
    public enum ElementCategory
    {
        Unknown,
        AlkaliMetal,
        AlkalineEarthMetal,
        Metalloid,
        PostTransitionMetal,
        Nonmetal,
        Halogen,
        NobleGas
    }

    public enum StandardState
    {
        Unknown,
        Solid,
        Liquid,
        Gas
    }

    [CreateAssetMenu(menuName = "MolecularLab/Element", fileName = "Element", order = 0)]
    public class ElementSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string symbol = "X";
        [SerializeField] private string elementName = "Unknown";
        [SerializeField] private int atomicNumber = 0;
        [SerializeField] private float atomicMass = 0f;

        [Header("Chemistry")]
        [SerializeField, Min(0)] private int valence = 0;
        [SerializeField, Min(0f)] private float covalentRadius = 0.077f;
        [SerializeField] private ElementCategory category = ElementCategory.Unknown;
        [SerializeField] private StandardState standardState = StandardState.Unknown;
        [SerializeField] private string electronConfiguration = "";
        [SerializeField] private string oxidationStates = "";
        [SerializeField, Min(0f)] private float electronegativity = 0f;
        [SerializeField, Min(0f)] private float density = 0f;
        [SerializeField] private float meltingPointCelsius = 0f;
        [SerializeField] private float boilingPointCelsius = 0f;

        [Header("Visualization")]
        [SerializeField, Min(0.001f)] private float displayRadius = 0.05f;
        [SerializeField] private Color cpkColor = Color.white;

        public string Symbol => symbol;
        public string ElementName => elementName;
        public int AtomicNumber => atomicNumber;
        public float AtomicMass => atomicMass;
        public int Valence => valence;
        public float CovalentRadius => covalentRadius;
        public ElementCategory Category => category;
        public StandardState State => standardState;
        public string ElectronConfiguration => electronConfiguration;
        public string OxidationStates => oxidationStates;
        public float Electronegativity => electronegativity;
        public float Density => density;
        public float MeltingPointCelsius => meltingPointCelsius;
        public float BoilingPointCelsius => boilingPointCelsius;
        public float DisplayRadius => displayRadius;
        public Color CpkColor => cpkColor;

        public int Period
        {
            get
            {
                return PeriodicTableUtils.TryGetPosition(atomicNumber, out var pos) ? pos.Period : 0;
            }
        }

        public int Group
        {
            get
            {
                return PeriodicTableUtils.TryGetPosition(atomicNumber, out var pos) ? pos.Group : 0;
            }
        }
    }
}
