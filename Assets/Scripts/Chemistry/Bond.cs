using UnityEngine;
using MolecularLab.Interaction;
using System.Collections.Generic;

namespace MolecularLab.Chemistry
{
    public class Bond : MonoBehaviour
    {
        [SerializeField] private Atom a;
        [SerializeField] private Atom b;
        [SerializeField, Range(1, 3)] private int order = 1;

        [Header("Кршење на врски")]
        [SerializeField, Min(0f)] private float breakDistance = 0.5f;
        [Tooltip("Сила потребна за физичко кршење (Infinity = незршлива, 0 = користи само breakDistance)")]
        [SerializeField, Min(0f)] private float breakForce = 50f;

        [Header("Визуелно")]
        [SerializeField, Min(0.001f)] private float baseThickness = 0.015f;
        [SerializeField, Min(0.5f)] private float bondLengthMultiplier = 1.5f;
        [SerializeField] private bool debugLog = false;

        private bool _claimed;
        private FixedJoint _joint;
        private Transform _primaryVisual;
        private readonly List<Transform> _extraVisuals = new List<Transform>();
        private ReactionChamber _chamberCache;
        private bool _chamberCacheFetched;

        public Atom A => a;
        public Atom B => b;
        public int Order => order;

        // ─── Фабрички методи ──────────────────────────────────────────────────

        /// <summary>
        /// Создава Bond од кеширан шаблон (Bond со сите компоненти, но SetActive(false)).
        /// Ова е примарниот пат кога постои bondPrefab.
        /// </summary>
        public static Bond CreateFromTemplate(Bond template, Atom a, Atom b, int order = 1, Transform parent = null)
        {
            if (template == null || a == null || b == null || a == b) return null;
            if (!a.CanBond(order) || !b.CanBond(order)) return null;

            var bond = Instantiate(template, parent);
            bond.gameObject.SetActive(true);
            bond.gameObject.name = $"Bond_{a.Element?.Symbol}-{b.Element?.Symbol}";
            bond.Initialize(a, b, order);
            return bond;
        }

        /// <summary>
        /// Создава Bond процедурално — без потреба од prefab во сцената.
        /// BondManager го повикува кога нема валиден шаблон.
        /// </summary>
        public static Bond CreateProcedural(Atom a, Atom b, int order = 1,
                                            Transform parent = null,
                                            Material material = null,
                                            Mesh cylinderMesh = null)
        {
            if (a == null || b == null || a == b) return null;
            if (!a.CanBond(order) || !b.CanBond(order)) return null;

            var go = new GameObject($"Bond_{a.Element?.Symbol}-{b.Element?.Symbol}");
            go.transform.SetParent(parent);

            // Меш + рендерер
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = cylinderMesh;

            var mr = go.AddComponent<MeshRenderer>();
            if (material != null) mr.sharedMaterial = material;

            var bond = go.AddComponent<Bond>();
            bond.Initialize(a, b, order);
            return bond;
        }

        // ─── Стара статичка Create (одржана за компатибилност) ────────────────

        [System.Obsolete("Користи CreateFromTemplate или CreateProcedural преку BondManager")]
        public static Bond Create(Bond prefab, Atom a, Atom b, int order = 1, Transform parent = null)
        {
            if (prefab == null || a == null || b == null || a == b) return null;
            if (!a.CanBond(order) || !b.CanBond(order)) return null;

            var bond = Instantiate(prefab, parent);
            bond.Initialize(a, b, order);
            return bond;
        }

        // ─── Иницијализација ──────────────────────────────────────────────────

        /// <summary>
        /// Ги поставува атомите, регистрира врски, создава FixedJoint и позиционира.
        /// Се повикува и од фабричките методи и може директно после AddComponent.
        /// </summary>
        public void Initialize(Atom atomA, Atom atomB, int bondOrder = 1)
        {
            a = atomA;
            b = atomB;
            order = bondOrder;
            Claim();
            EnsureVisualCount();
            UpdateTransform();

            if (debugLog)
                Debug.Log($"[Bond] Иницијализиран: {a.Element?.Symbol}-{b.Element?.Symbol}, order={order}");
        }

        // ─── Животен циклус ───────────────────────────────────────────────────

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

                // breakForce = 0 значи само breakDistance режим (без физичко кршење)
                _joint.breakForce  = breakForce > 0f ? breakForce : Mathf.Infinity;
                _joint.breakTorque = breakForce > 0f ? breakForce : Mathf.Infinity;
                _joint.enableCollision = false;
            }

            _claimed = true;
        }

        private void Release()
        {
            if (!_claimed) return;
            if (_joint != null) { Destroy(_joint); _joint = null; }
            if (a != null) a.UnregisterBond(this);
            if (b != null) b.UnregisterBond(this);
            _claimed = false;
        }

        public void BreakImmediately()
        {
            Release();
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        public bool TrySetOrder(int newOrder)
        {
            if (newOrder < 1 || newOrder > 3)
                return false;

            if (a == null || b == null)
                return false;

            if (newOrder == order)
                return true;

            int delta = newOrder - order;
            if (delta > 0 && (!a.CanBond(delta) || !b.CanBond(delta)))
                return false;

            order = newOrder;
            EnsureVisualCount();
            SnapToEquilibriumDistance();
            UpdateTransform();
            return true;
        }

        private void OnDestroy() => Release();

        private void OnJointBreak(float breakForceUsed)
        {
            // FixedJoint го кршеше физиката — уништи го Bond-от
            if (debugLog) Debug.Log($"[Bond] Joint скршен со сила {breakForceUsed:F1}");
            Destroy(gameObject);
        }

        private void LateUpdate()
        {
            if (a == null || b == null) { Destroy(gameObject); return; }

            EnforceEquilibriumDistance();
            UpdateTransform();

            if (IsWholeMoleculeDragActive() || IsChamberStaged())
                return;

            // breakDistance режим — растојанска проверка
            if (breakDistance > 0f &&
                Vector3.Distance(a.transform.position, b.transform.position) > breakDistance)
            {
                if (debugLog) Debug.Log($"[Bond] Скршен по растојание > {breakDistance}");
                Destroy(gameObject);
            }
        }

        private bool IsWholeMoleculeDragActive()
        {
            var sensorA = a != null ? a.GetComponent<AtomGrabSensor>() : null;
            if (sensorA != null && sensorA.IsDraggingWholeMolecule) return true;

            var sensorB = b != null ? b.GetComponent<AtomGrabSensor>() : null;
            return sensorB != null && sensorB.IsDraggingWholeMolecule;
        }

        private bool IsChamberStaged()
        {
            if (!_chamberCacheFetched)
            {
                _chamberCache = FindFirstObjectByType<ReactionChamber>();
                _chamberCacheFetched = true;
            }

            if (_chamberCache == null)
                return false;

            return (a != null && _chamberCache.IsAtomStaged(a)) || (b != null && _chamberCache.IsAtomStaged(b));
        }

        // ─── Позиционирање и трансформација ──────────────────────────────────

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

            bool aIsAnchor = a.Bonds.Count > 1;
            bool bIsAnchor = b.Bonds.Count > 1;

            if (aIsAnchor && bIsAnchor) return;

            if (aIsAnchor)        SetAtomPosition(b, pa + dir * target);
            else if (bIsAnchor)   SetAtomPosition(a, pb - dir * target);
            else
            {
                Vector3 mid = (pa + pb) * 0.5f;
                SetAtomPosition(a, mid - dir * (target * 0.5f));
                SetAtomPosition(b, mid + dir * (target * 0.5f));
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

        private void EnforceEquilibriumDistance()
        {
            if (a == null || b == null)
                return;

            if (IsWholeMoleculeDragActive())
                return;

            float target = ComputeEquilibriumLength();
            Vector3 pa = a.transform.position;
            Vector3 pb = b.transform.position;
            Vector3 dir = pb - pa;

            if (dir.sqrMagnitude < 1e-6f)
                dir = Vector3.right;
            else
                dir.Normalize();

            Vector3 mid = (pa + pb) * 0.5f;
            SetAtomPosition(a, mid - dir * (target * 0.5f));
            SetAtomPosition(b, mid + dir * (target * 0.5f));
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
            transform.localScale = Vector3.one;

            EnsureVisualRoot();
            float t = baseThickness;
            float visualSpacing = baseThickness * 1.8f;
            float yScale = len * 0.5f;

            if (order <= 1)
            {
                if (_primaryVisual != null)
                {
                    _primaryVisual.localPosition = Vector3.zero;
                    _primaryVisual.localRotation = Quaternion.identity;
                    _primaryVisual.localScale = new Vector3(t, yScale, t);
                }
            }
            else if (order == 2)
            {
                if (_primaryVisual != null)
                {
                    _primaryVisual.localPosition = new Vector3(-visualSpacing * 0.5f, 0f, 0f);
                    _primaryVisual.localRotation = Quaternion.identity;
                    _primaryVisual.localScale = new Vector3(t, yScale, t);
                }

                if (_extraVisuals.Count >= 1)
                {
                    _extraVisuals[0].localPosition = new Vector3(visualSpacing * 0.5f, 0f, 0f);
                    _extraVisuals[0].localRotation = Quaternion.identity;
                    _extraVisuals[0].localScale = new Vector3(t, yScale, t);
                }
            }
            else
            {
                if (_primaryVisual != null)
                {
                    _primaryVisual.localPosition = Vector3.zero;
                    _primaryVisual.localRotation = Quaternion.identity;
                    _primaryVisual.localScale = new Vector3(t, yScale, t);
                }

                if (_extraVisuals.Count >= 1)
                {
                    _extraVisuals[0].localPosition = new Vector3(-visualSpacing, 0f, 0f);
                    _extraVisuals[0].localRotation = Quaternion.identity;
                    _extraVisuals[0].localScale = new Vector3(t, yScale, t);
                }

                if (_extraVisuals.Count >= 2)
                {
                    _extraVisuals[1].localPosition = new Vector3(visualSpacing, 0f, 0f);
                    _extraVisuals[1].localRotation = Quaternion.identity;
                    _extraVisuals[1].localScale = new Vector3(t, yScale, t);
                }
            }
        }

        private void EnsureVisualCount()
        {
            EnsureVisualRoot();
            int neededExtras = Mathf.Max(0, order - 1);

            while (_extraVisuals.Count < neededExtras)
            {
                var go = new GameObject($"BondVisual_{_extraVisuals.Count + 1}");
                go.transform.SetParent(transform, false);

                var mf = go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();

                if (TryGetComponent<MeshFilter>(out var rootMf))
                    mf.sharedMesh = rootMf.sharedMesh;

                if (TryGetComponent<MeshRenderer>(out var rootMr))
                    mr.sharedMaterials = rootMr.sharedMaterials;

                _extraVisuals.Add(go.transform);
            }

            while (_extraVisuals.Count > neededExtras)
            {
                int last = _extraVisuals.Count - 1;
                if (_extraVisuals[last] != null)
                    Destroy(_extraVisuals[last].gameObject);
                _extraVisuals.RemoveAt(last);
            }
        }

        private void EnsureVisualRoot()
        {
            if (_primaryVisual != null)
                return;

            if (!TryGetComponent<MeshFilter>(out var rootMf) || !TryGetComponent<MeshRenderer>(out var rootMr))
                return;

            var go = new GameObject("BondVisual_0");
            go.transform.SetParent(transform, false);

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = rootMf.sharedMesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = rootMr.sharedMaterials;

            rootMr.enabled = false;
            _primaryVisual = go.transform;
        }
    }
}
