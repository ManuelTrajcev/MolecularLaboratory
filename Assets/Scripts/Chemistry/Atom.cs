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

        private int _usedValence;

        public ElementSO Element => element;
        public int UsedValence => _usedValence;
        public int RemainingValence => element != null ? element.Valence - _usedValence : 0;

        public bool CanBond(int order = 1) => element != null && RemainingValence >= order;

        public bool ConsumeValence(int order)
        {
            if (order <= 0 || RemainingValence < order) return false;
            _usedValence += order;
            return true;
        }

        public void ReleaseValence(int order)
        {
            _usedValence = Mathf.Max(0, _usedValence - order);
        }

        public void SetElement(ElementSO newElement)
        {
            element = newElement;
            _usedValence = 0;
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
