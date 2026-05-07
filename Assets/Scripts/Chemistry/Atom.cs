using System.Collections.Generic;
using UnityEngine;

namespace MolecularLab.Chemistry
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class Atom : MonoBehaviour
    {
        [SerializeField] private ElementSO element;
        [SerializeField] private MeshRenderer meshRenderer;

        private static MaterialPropertyBlock _propertyBlock;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private readonly List<Bond> _bonds = new List<Bond>();

        public ElementSO Element => element;
        public IReadOnlyList<Bond> Bonds => _bonds;

        public int UsedValence
        {
            get
            {
                int sum = 0;
                for (int i = 0; i < _bonds.Count; i++) sum += _bonds[i].Order;
                return sum;
            }
        }

        public int RemainingValence => element != null ? element.Valence - UsedValence : 0;

        public bool CanBond(int order = 1) => element != null && RemainingValence >= order;

        public void RegisterBond(Bond bond)
        {
            if (bond != null && !_bonds.Contains(bond)) _bonds.Add(bond);
        }

        public void UnregisterBond(Bond bond)
        {
            _bonds.Remove(bond);
        }

        public void SetElement(ElementSO newElement)
        {
            element = newElement;
            ApplyElement();
        }

        private void Awake()
        {
            ApplyElement();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying) ApplyElement();
        }

        private void ApplyElement()
        {
            if (element == null) return;

            float diameter = element.DisplayRadius * 2f;
            transform.localScale = new Vector3(diameter, diameter, diameter);

            if (meshRenderer == null) meshRenderer = GetComponentInChildren<MeshRenderer>();
            if (meshRenderer != null)
            {
                _propertyBlock ??= new MaterialPropertyBlock();
                meshRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(BaseColorId, element.CpkColor);
                meshRenderer.SetPropertyBlock(_propertyBlock);
            }

            if (TryGetComponent<SphereCollider>(out var sc)) sc.radius = 0.5f;

            if (!Application.isPlaying) gameObject.name = $"Atom_{element.Symbol}";
        }
    }
}
