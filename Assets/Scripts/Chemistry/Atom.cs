using System.Collections.Generic;
using UnityEngine;

namespace MolecularLab.Chemistry
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class Atom : MonoBehaviour
    {
        // ─── Статички регистар (наместо FindObjectsByType на секој grab) ──────
        /// <summary>
        /// Сите активни атоми во сцената. Автоматски се одржува преку OnEnable/OnDisable.
        /// </summary>
        public static readonly List<Atom> AllAtoms = new List<Atom>();

        // ─── Инстанцни полиња ─────────────────────────────────────────────────
        [SerializeField] private ElementSO element;
        [SerializeField] private MeshRenderer meshRenderer;

        private static MaterialPropertyBlock _propertyBlock;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private readonly List<Bond> _bonds = new List<Bond>();

        // ─── Јавни пристапи ───────────────────────────────────────────────────
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

        // ─── Bond регистрација ────────────────────────────────────────────────
        public void RegisterBond(Bond bond)
        {
            if (bond != null && !_bonds.Contains(bond)) _bonds.Add(bond);
        }

        public void UnregisterBond(Bond bond)
        {
            _bonds.Remove(bond);
        }

        // ─── Поставување елемент ──────────────────────────────────────────────
        public void SetElement(ElementSO newElement)
        {
            element = newElement;
            ApplyElement();
        }

        // ─── Unity callbacks ──────────────────────────────────────────────────
        private void Awake()
        {
            ConfigureRigidbody();
            ApplyElement();
        }

        private void OnEnable()
        {
            if (!AllAtoms.Contains(this))
                AllAtoms.Add(this);
        }

        private void OnDisable()
        {
            AllAtoms.Remove(this);
        }

        private void OnValidate()
        {
            if (!Application.isPlaying) ApplyElement();
        }

        // ─── Приватни методи ──────────────────────────────────────────────────

        /// <summary>
        /// Поставува Rigidbody за „нула гравитација" — атомот лебди точно
        /// каде ќе го оставиш, без никакво движење.
        /// </summary>
        private void ConfigureRigidbody()
        {
            if (!TryGetComponent<Rigidbody>(out var rb)) return;

            rb.useGravity       = false;   // без гравитација
            rb.linearDamping    = 10f;     // брзо запирање на транслација
            rb.angularDamping   = 10f;     // брзо запирање на ротација
            rb.interpolation    = RigidbodyInterpolation.Interpolate;
            rb.constraints      = RigidbodyConstraints.FreezeRotation; // не се врти
        }

        /// <summary>
        /// Целосно запира физичкото движење — повикај по release на grab.
        /// </summary>
        public void Freeze()
        {
            if (!TryGetComponent<Rigidbody>(out var rb)) return;
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
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