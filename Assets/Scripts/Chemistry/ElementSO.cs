using UnityEngine;

namespace MolecularLab.Chemistry
{
    [CreateAssetMenu(menuName = "MolecularLab/Element", fileName = "Element", order = 0)]
    public class ElementSO : ScriptableObject
    {
        [SerializeField] private string symbol = "X";
        [SerializeField] private string elementName = "Unknown";
        [SerializeField] private int atomicNumber = 0;
        [SerializeField] private float atomicMass = 0f;
        [SerializeField, Min(0)] private int valence = 0;
        [SerializeField, Min(0f)] private float covalentRadius = 0.077f;
        [SerializeField, Min(0.001f)] private float displayRadius = 0.05f;
        [SerializeField] private Color cpkColor = Color.white;

        public string Symbol => symbol;
        public string ElementName => elementName;
        public int AtomicNumber => atomicNumber;
        public float AtomicMass => atomicMass;
        public int Valence => valence;
        public float CovalentRadius => covalentRadius;
        public float DisplayRadius => displayRadius;
        public Color CpkColor => cpkColor;
    }
}
