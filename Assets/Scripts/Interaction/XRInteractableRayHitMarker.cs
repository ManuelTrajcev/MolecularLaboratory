using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

namespace MolecularLab.Interaction
{
    public class XRInteractableRayHitMarker : MonoBehaviour
    {
        private const int MaxMarkers = 2;
        private const string ManagerObjectName = "XR Interactable Ray Hit Marker";
        private const string MarkerObjectNamePrefix = "XR Ray Hover Marker";
        private const string LegacyMarkerObjectName = "XR Interactable Ray Hit Marker Sphere";
        private const string MarkerMaterialName = "Runtime_GreenRayHitMarker";

        [SerializeField, Min(0.005f)] private float markerDiameter = 0.06f;
        [SerializeField] private Color markerColor = new Color(0.05f, 1f, 0.12f, 0.55f);
        [SerializeField, Min(0.1f)] private float interactorRefreshInterval = 1f;

        private static Material _markerMaterial;

        private readonly List<MarkerSlot> _markerSlots = new();
        private readonly List<Behaviour> _candidateInteractors = new();
        private bool _isDuplicateManager;
        private float _nextRefreshTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            CleanupGeneratedMarkers(includeCurrentMarkers: true);

            var existingManagers = FindObjectsByType<XRInteractableRayHitMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (existingManagers.Length > 0)
            {
                DestroyDuplicateManagers(existingManagers);
                return;
            }

            var go = new GameObject(ManagerObjectName);
            go.hideFlags = HideFlags.DontSave;
            go.AddComponent<XRInteractableRayHitMarker>();
        }

        private void Awake()
        {
            if (DestroyIfDuplicateManager())
                return;

            CleanupGeneratedMarkers(includeCurrentMarkers: true);
        }

        private void OnEnable()
        {
            if (_isDuplicateManager)
                return;

            CleanupGeneratedMarkers(includeCurrentMarkers: true);
            RefreshInteractors();
        }

        private void LateUpdate()
        {
            if (_isDuplicateManager)
                return;

            if (Time.unscaledTime >= _nextRefreshTime)
            {
                RefreshInteractors();
                _nextRefreshTime = Time.unscaledTime + interactorRefreshInterval;
            }

            for (int i = 0; i < _markerSlots.Count; i++)
                _markerSlots[i].Update(markerDiameter, transform, GetMarkerMaterial(markerColor));
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _markerSlots.Count; i++)
                _markerSlots[i].DestroyMarker();
        }

        private bool DestroyIfDuplicateManager()
        {
            var managers = FindObjectsByType<XRInteractableRayHitMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (managers.Length <= 1)
                return false;

            var keeper = GetKeeper(managers);
            if (keeper == this)
            {
                DestroyDuplicateManagers(managers);
                return false;
            }

            _isDuplicateManager = true;
            DestroyGeneratedObject(gameObject);
            return true;
        }

        private void RefreshInteractors()
        {
            CleanupGeneratedMarkers(includeCurrentMarkers: false);

            _candidateInteractors.Clear();

            var nearFarInteractors = FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < nearFarInteractors.Length && _candidateInteractors.Count < MaxMarkers; i++)
            {
                if (IsUsable(nearFarInteractors[i]))
                    _candidateInteractors.Add(nearFarInteractors[i]);
            }

            var rayInteractors = FindObjectsByType<XRRayInteractor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < rayInteractors.Length && _candidateInteractors.Count < MaxMarkers; i++)
            {
                if (IsUsable(rayInteractors[i]) && !ContainsInteractor(_candidateInteractors, rayInteractors[i]))
                    _candidateInteractors.Add(rayInteractors[i]);
            }

            for (int i = _markerSlots.Count - 1; i >= 0; i--)
            {
                if (!ContainsInteractor(_candidateInteractors, _markerSlots[i].Interactor))
                {
                    _markerSlots[i].DestroyMarker();
                    _markerSlots.RemoveAt(i);
                }
            }

            for (int i = 0; i < _candidateInteractors.Count && _markerSlots.Count < MaxMarkers; i++)
            {
                if (!HasSlot(_candidateInteractors[i]))
                    _markerSlots.Add(new MarkerSlot(_candidateInteractors[i], _markerSlots.Count));
            }
        }

        private bool HasSlot(Behaviour interactor)
        {
            for (int i = 0; i < _markerSlots.Count; i++)
            {
                if (_markerSlots[i].Interactor == interactor)
                    return true;
            }

            return false;
        }

        private static bool ContainsInteractor(List<Behaviour> interactors, Behaviour interactor)
        {
            for (int i = 0; i < interactors.Count; i++)
            {
                if (interactors[i] == interactor)
                    return true;
            }

            return false;
        }

        private static bool IsUsable(Behaviour interactor)
        {
            return interactor != null && interactor.isActiveAndEnabled && interactor.gameObject.activeInHierarchy;
        }

        private static void DestroyDuplicateManagers(XRInteractableRayHitMarker[] managers)
        {
            var keeper = GetKeeper(managers);
            for (int i = 0; i < managers.Length; i++)
            {
                if (managers[i] != null && managers[i] != keeper)
                    DestroyGeneratedObject(managers[i].gameObject);
            }
        }

        private static XRInteractableRayHitMarker GetKeeper(XRInteractableRayHitMarker[] managers)
        {
            XRInteractableRayHitMarker keeper = null;
            for (int i = 0; i < managers.Length; i++)
            {
                if (managers[i] == null)
                    continue;

                if (keeper == null || managers[i].GetInstanceID() < keeper.GetInstanceID())
                    keeper = managers[i];
            }

            return keeper;
        }

        private static Material GetMarkerMaterial(Color color)
        {
            if (_markerMaterial != null)
                return _markerMaterial;

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            _markerMaterial = new Material(shader)
            {
                name = MarkerMaterialName,
                color = color,
                hideFlags = HideFlags.DontSave
            };

            if (_markerMaterial.HasProperty("_BaseColor"))
                _markerMaterial.SetColor("_BaseColor", color);
            if (_markerMaterial.HasProperty("_Color"))
                _markerMaterial.SetColor("_Color", color);

            ConfigureTransparency(_markerMaterial);
            return _markerMaterial;
        }

        private static void ConfigureTransparency(Material material)
        {
            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_SrcBlend"))
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend"))
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 0f);

            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        private static void CleanupGeneratedMarkers(bool includeCurrentMarkers)
        {
            var transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < transforms.Length; i++)
            {
                var go = transforms[i].gameObject;
                if (go == null)
                    continue;
                if (go.GetComponent<XRInteractableRayHitMarker>() != null)
                    continue;

                bool isCurrentMarker = go.name.StartsWith(MarkerObjectNamePrefix);
                bool isLegacyMarker = go.name == LegacyMarkerObjectName ||
                    (go.name.EndsWith(" Hit Marker") && go.GetComponent<MeshRenderer>() != null);
                if ((includeCurrentMarkers && isCurrentMarker) || isLegacyMarker)
                    DestroyGeneratedObject(go);
            }
        }

        private static void DestroyGeneratedObject(Object obj)
        {
            if (obj == null)
                return;

#if UNITY_EDITOR
            DestroyImmediate(obj);
#else
            Destroy(obj);
#endif
        }

        private sealed class MarkerSlot
        {
            private readonly XRRayInteractor _rayInteractor;
            private readonly IXRHoverInteractor _hoverInteractor;
            private readonly ICurveInteractionDataProvider _curveProvider;
            private readonly int _index;
            private GameObject _marker;

            public MarkerSlot(Behaviour interactor, int index)
            {
                Interactor = interactor;
                _rayInteractor = interactor as XRRayInteractor;
                _hoverInteractor = interactor as IXRHoverInteractor;
                _curveProvider = interactor as ICurveInteractionDataProvider;
                _index = index;
            }

            public Behaviour Interactor { get; }

            public void Update(float diameter, Transform parent, Material material)
            {
                if (!IsUsable(Interactor) || !HasHover || !TryGetHitPoint(out var point))
                {
                    HideMarker();
                    return;
                }

                EnsureMarker(parent, material);
                _marker.transform.position = point;
                _marker.transform.localScale = Vector3.one * diameter;
                if (!_marker.activeSelf)
                    _marker.SetActive(true);
            }

            public void DestroyMarker()
            {
                if (_marker != null)
                    DestroyGeneratedObject(_marker);
            }

            private bool HasHover => _hoverInteractor != null && _hoverInteractor.hasHover;

            private bool TryGetHitPoint(out Vector3 point)
            {
                if (_curveProvider != null && _curveProvider.isActive &&
                    _curveProvider.TryGetCurveEndPoint(out point) == EndPointType.ValidCastHit)
                    return true;

                if (_rayInteractor != null && _rayInteractor.TryGetCurrent3DRaycastHit(out var hit) &&
                    hit.collider != null && hit.collider.GetComponentInParent<XRBaseInteractable>() != null)
                {
                    point = hit.point;
                    return true;
                }

                point = default;
                return false;
            }

            private void EnsureMarker(Transform parent, Material material)
            {
                if (_marker != null)
                    return;

                _marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _marker.name = $"{MarkerObjectNamePrefix} {_index + 1}";
                _marker.hideFlags = HideFlags.DontSave;
                _marker.layer = LayerMask.NameToLayer("Ignore Raycast");
                _marker.transform.SetParent(parent, false);
                _marker.SetActive(false);

                if (_marker.TryGetComponent<Collider>(out var collider))
                    DestroyGeneratedObject(collider);

                var renderer = _marker.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
            }

            private void HideMarker()
            {
                if (_marker != null)
                    _marker.SetActive(false);
            }
        }
    }
}
