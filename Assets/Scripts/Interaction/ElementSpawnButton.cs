using MolecularLab.Chemistry;
using MolecularLab.Managers;
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

        [Tooltip("Оддалеченост пред копчето каде ќе се spawn-ира атомот (ако нема spawnAnchor)")]
        [SerializeField, Min(0f)] private float spawnForwardOffset = 0.15f;
        [SerializeField] private bool debugLog = false;

        private XRSimpleInteractable _interactable;
        private float _lastSpawnTime = -999f;

        public ElementSO Element => element;

        public void Configure(ElementSO el, GameObject prefab, Transform anchor)
        {
            element   = el;
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
                if (debugLog) Debug.LogWarning($"[PT] ElementSpawnButton: недостасуваат refs (element={element}, prefab={atomPrefab})", this);
                return;
            }

            Transform activeSpawnAnchor = GetActiveSpawnAnchor();
            Vector3 pos = activeSpawnAnchor != null
                ? activeSpawnAnchor.position
                : transform.position + transform.forward * spawnForwardOffset + Vector3.up * 0.02f;

            // Play element chosen teleport sound immediately before heavy instantiation cost to eliminate latency
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayElementChosen(pos);
                AudioManager.Instance.PlayPlaceDown(pos);
            }

            Quaternion rot = Quaternion.identity;

            var go   = Instantiate(atomPrefab, pos, rot);
            go.name  = $"Atom_{element.Symbol}";

            var atom = go.GetComponent<Atom>();
            if (atom != null)
            {
                atom.SetElement(element);
                LevelManager.Instance?.OnAtomSpawned(atom);
            }

            // НЕ додаваме никаков импулс — атомот останува точно каде spawn-ираме
            // Rigidbody е веќе конфигуриран во Atom.Awake (useGravity=false, висок drag)

            if (debugLog) Debug.Log($"[PT] Spawn-иран {element.Symbol} на {pos}", this);
        }

        private Transform GetActiveSpawnAnchor()
        {
            var smallChamber = FindFirstObjectByType<SmallMoleculeChamber>();
            if (smallChamber != null && smallChamber.HasActiveTarget && smallChamber.AtomSpawnAnchor != null)
                return smallChamber.AtomSpawnAnchor;

            return spawnAnchor;
        }
    }
}
