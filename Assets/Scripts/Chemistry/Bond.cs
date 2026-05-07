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
        [SerializeField, Min(0.5f)] private float bondLengthMultiplier = 1.5f;
        [SerializeField] private bool debugLog = true;

        private bool _claimed;
        private FixedJoint _joint;

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

            if (bond.debugLog)
            {
                var mf = bond.GetComponent<MeshFilter>();
                var mr = bond.GetComponent<MeshRenderer>();
                Debug.Log($"[Bond] Instantiated. MeshFilter={(mf != null)}, mesh={(mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : "NONE")}, MeshRenderer={(mr != null)}, material={(mr != null && mr.sharedMaterial != null ? mr.sharedMaterial.name : "NONE")}, parent={(parent != null ? parent.name : "world")}");
                Debug.Log($"[Bond] Pre-snap: A({a.name})={a.transform.position}, B({b.name})={b.transform.position}, dist={Vector3.Distance(a.transform.position, b.transform.position):F3}");
            }

            bond.Claim();

            if (bond.debugLog)
            {
                Debug.Log($"[Bond] Post-snap: A={a.transform.position}, B={b.transform.position}, dist={Vector3.Distance(a.transform.position, b.transform.position):F3}, target={bond.ComputeEquilibriumLength():F3}");
            }

            bond.UpdateTransform();

            if (bond.debugLog)
            {
                var p = bond.transform.parent;
                Debug.Log($"[Bond] Cylinder transform: pos={bond.transform.position}, scale={bond.transform.localScale}, activeSelf={bond.gameObject.activeSelf}, activeInHierarchy={bond.gameObject.activeInHierarchy}, parentActive={(p == null ? "no parent" : p.gameObject.activeInHierarchy.ToString())}");
            }

            return bond;
        }

        private void Claim()
        {
            if (_claimed) return;

            a.RegisterBond(this);
            b.RegisterBond(this);

            SnapToEquilibriumDistance();

            var rbA = a.GetComponent<Rigidbody>();
            var rbB = b.GetComponent<Rigidbody>();
            if (rbA != null && rbB != null)
            {
                _joint = rbA.gameObject.AddComponent<FixedJoint>();
                _joint.connectedBody = rbB;
                _joint.breakForce = Mathf.Infinity;
                _joint.breakTorque = Mathf.Infinity;
                _joint.enableCollision = false;
            }

            _claimed = true;
        }

        private void Release()
        {
            if (!_claimed) return;
            if (_joint != null)
            {
                Destroy(_joint);
                _joint = null;
            }
            if (a != null) a.UnregisterBond(this);
            if (b != null) b.UnregisterBond(this);
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

        private float ComputeEquilibriumLength()
        {
            float ra = a.Element != null ? a.Element.DisplayRadius : 0.05f;
            float rb = b.Element != null ? b.Element.DisplayRadius : 0.05f;
            return (ra + rb) * bondLengthMultiplier;
        }

        private void SnapToEquilibriumDistance()
        {
            float target = ComputeEquilibriumLength();
            Vector3 pa = a.transform.position;
            Vector3 pb = b.transform.position;

            Vector3 dir = pb - pa;
            if (dir.sqrMagnitude < 1e-6f) dir = Vector3.right;
            else dir.Normalize();

            // After Claim, each atom's bond list includes this bond. >1 means it has prior bonds.
            bool aIsAnchor = a.Bonds.Count > 1;
            bool bIsAnchor = b.Bonds.Count > 1;

            if (aIsAnchor && bIsAnchor)
            {
                return;
            }

            if (aIsAnchor)
            {
                SetAtomPosition(b, pa + dir * target);
            }
            else if (bIsAnchor)
            {
                SetAtomPosition(a, pb - dir * target);
            }
            else
            {
                Vector3 mid = (pa + pb) * 0.5f;
                float half = target * 0.5f;
                SetAtomPosition(a, mid - dir * half);
                SetAtomPosition(b, mid + dir * half);
            }
        }

        private static void SetAtomPosition(Atom atom, Vector3 pos)
        {
            atom.transform.position = pos;
            if (atom.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.position = pos;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
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
