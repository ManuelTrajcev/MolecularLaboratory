using UnityEngine;

namespace MolecularLab.Chemistry
{
    public class Bond : MonoBehaviour
    {
        [SerializeField] private Atom a;
        [SerializeField] private Atom b;
        [SerializeField, Range(1, 3)] private int order = 1;
        [SerializeField, Min(0.05f)] private float breakDistance = 0.5f;
        [SerializeField, Min(0.001f)] private float baseThickness = 0.015f;

        private bool _claimed;

        public Atom A => a;
        public Atom B => b;
        public int Order => order;

        public static Bond Create(Bond prefab, Atom a, Atom b, int order = 1, Transform parent = null)
        {
            if (prefab == null || a == null || b == null || a == b) return null;
            if (!a.CanBond(order) || !b.CanBond(order)) return null;

            var bond = Instantiate(prefab, parent);
            bond.a = a;
            bond.b = b;
            bond.order = order;

            if (!bond.Claim())
            {
                Destroy(bond.gameObject);
                return null;
            }

            bond.UpdateTransform();
            return bond;
        }

        private bool Claim()
        {
            if (_claimed) return true;
            if (!a.ConsumeValence(order)) return false;
            if (!b.ConsumeValence(order))
            {
                a.ReleaseValence(order);
                return false;
            }
            _claimed = true;
            return true;
        }

        private void Release()
        {
            if (!_claimed) return;
            if (a != null) a.ReleaseValence(order);
            if (b != null) b.ReleaseValence(order);
            _claimed = false;
        }

        private void OnDestroy() => Release();

        private void LateUpdate()
        {
            if (a == null || b == null) { Destroy(gameObject); return; }
            UpdateTransform();
            if (Vector3.Distance(a.transform.position, b.transform.position) > breakDistance)
                Destroy(gameObject);
        }

        private void UpdateTransform()
        {
            Vector3 pa = a.transform.position;
            Vector3 pb = b.transform.position;
            Vector3 dir = pb - pa;
            float len = dir.magnitude;

            transform.position = (pa + pb) * 0.5f;
            if (len > 0.0001f)
                transform.rotation = Quaternion.FromToRotation(Vector3.up, dir / len);

            float t = baseThickness * (1f + 0.4f * (order - 1));
            transform.localScale = new Vector3(t, len * 0.5f, t);
        }
    }
}
