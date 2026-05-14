using MolecularLab.Chemistry;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MolecularLab.Interaction
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class ElementSpawnButton : MonoBehaviour
    {
        [SerializeField] private ElementSO element;
        [SerializeField] private GameObject atomPrefab;
        [SerializeField] private Transform spawnAnchor;
        [SerializeField, Min(0f)] private float spawnCooldown = 0.4f;
        [SerializeField] private float upwardImpulse = 0.4f;
        [SerializeField] private bool debugLog = false;

        private XRSimpleInteractable _interactable;
        private float _lastSpawnTime = -999f;

        public ElementSO Element => element;

        public void Configure(ElementSO el, GameObject prefab, Transform anchor)
        {
            element = el;
            atomPrefab = prefab;
            spawnAnchor = anchor;
        }

        private void Awake()
        {
            _interactable = GetComponent<XRSimpleInteractable>();
            _interactable.selectEntered.AddListener(OnSelected);
        }

        private void OnDestroy()
        {
            if (_interactable != null) _interactable.selectEntered.RemoveListener(OnSelected);
        }

        private void OnSelected(SelectEnterEventArgs args)
        {
            if (Time.time - _lastSpawnTime < spawnCooldown) return;
            _lastSpawnTime = Time.time;
            Spawn();
        }

        private void Spawn()
        {
            if (element == null || atomPrefab == null)
            {
                if (debugLog) Debug.LogWarning($"[PT] ElementSpawnButton missing refs (element={element}, prefab={atomPrefab})", this);
                return;
            }

            Vector3 pos = spawnAnchor != null ? spawnAnchor.position : transform.position + transform.forward * 0.2f;
            Quaternion rot = spawnAnchor != null ? spawnAnchor.rotation : Quaternion.identity;

            GameObject go = Instantiate(atomPrefab, pos, rot);
            go.name = $"Atom_{element.Symbol}";

            Atom atom = go.GetComponent<Atom>();
            if (atom != null) atom.SetElement(element);

            if (go.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = Vector3.up * upwardImpulse;
            }

            if (debugLog) Debug.Log($"[PT] Spawned {element.Symbol} at {pos}", this);
        }
    }
}
