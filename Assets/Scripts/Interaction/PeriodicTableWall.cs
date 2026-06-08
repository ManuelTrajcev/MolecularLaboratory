using System.Collections.Generic;
using MolecularLab.Chemistry;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MolecularLab.Interaction
{
    public class PeriodicTableWall : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private List<ElementSO> elements = new List<ElementSO>();
        [SerializeField] private GameObject atomPrefab;

        [Header("Layout (meters, wall-local)")]
        [SerializeField] private Vector2 cellSize = new Vector2(0.09f, 0.09f);
        [SerializeField] private Vector2 cellSpacing = new Vector2(0.012f, 0.012f);
        [SerializeField] private float panelPadding = 0.06f;
        [SerializeField] private float panelDepth = 0.03f;
        [SerializeField] private float cubeDepth = 0.04f;

        [Header("Spawn anchor (wall-local offset)")]
        [SerializeField] private Vector3 spawnAnchorLocalOffset = new Vector3(0f, -0.4f, 0.25f);

        [Header("Visuals")]
        [SerializeField] private Material periodicTableMaterial;
        [SerializeField] private Color panelColor = new Color(0.12f, 0.13f, 0.16f, 1f);
        [SerializeField] private bool showLabels = true;
        [SerializeField] private Color labelColor = Color.white;
        [SerializeField, Range(0.1f, 0.95f)] private float symbolHeightFactor = 0.5f;
        [SerializeField, Range(0.08f, 0.5f)] private float atomicNumberHeightFactor = 0.16f;
        [SerializeField, Range(0.08f, 0.35f)] private float footerHeightFactor = 0.16f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private MaterialPropertyBlock _mpb;
        private Material _runtimeMaterial;

        private void Start()
        {
            _mpb = new MaterialPropertyBlock();
            Build();
        }

        private void OnDestroy()
        {
            if (_runtimeMaterial != null)
            {
                Destroy(_runtimeMaterial);
                _runtimeMaterial = null;
            }
        }

        private void Build()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            const int groupCount = 18;
            int maxPeriod = 1;
            foreach (var el in elements)
            {
                if (el == null) continue;
                if (PeriodicTableUtils.TryGetPosition(el.AtomicNumber, out var p) && p.Period > maxPeriod)
                    maxPeriod = p.Period;
            }

            float gridW = groupCount * cellSize.x + (groupCount - 1) * cellSpacing.x;
            float gridH = maxPeriod * cellSize.y + (maxPeriod - 1) * cellSpacing.y;

            // Backing panel — behind the cubes (wall-local -Z)
            var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "Panel";
            panel.transform.SetParent(transform, false);
            panel.transform.localPosition = new Vector3(0f, 0f, -panelDepth * 0.5f);
            panel.transform.localScale = new Vector3(gridW + 2f * panelPadding, gridH + 2f * panelPadding, panelDepth);
            if (panel.TryGetComponent<Collider>(out var pc)) Destroy(pc);
            SetMprColor(panel, panelColor);

            // Spawn anchor
            var anchorGo = new GameObject("SpawnAnchor");
            anchorGo.transform.SetParent(transform, false);
            anchorGo.transform.localPosition = spawnAnchorLocalOffset;
            var spawnAnchor = anchorGo.transform;

            float halfW = gridW * 0.5f;
            float halfH = gridH * 0.5f;
            float colStride = cellSize.x + cellSpacing.x;
            float rowStride = cellSize.y + cellSpacing.y;

            bool labelFontAvailable = !showLabels || (TMP_Settings.defaultFontAsset != null);
            if (showLabels && !labelFontAvailable)
            {
                Debug.LogWarning("[PeriodicTableWall] TMP default font asset missing — labels disabled. Window > TextMeshPro > Import TMP Essential Resources.", this);
            }

            foreach (var el in elements)
            {
                if (el == null) continue;
                if (!PeriodicTableUtils.TryGetPosition(el.AtomicNumber, out var gp)) continue;

                float cx = halfW - cellSize.x * 0.5f - (gp.Group - 1) * colStride;
                float cy =  halfH - cellSize.y * 0.5f - (gp.Period - 1) * rowStride;

                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = $"Cell_{el.Symbol}";
                cube.transform.SetParent(transform, false);
                cube.transform.localPosition = new Vector3(cx, cy, cubeDepth * 0.5f);
                cube.transform.localScale = new Vector3(cellSize.x, cellSize.y, cubeDepth);
                SetMprColor(cube, el.CpkColor);

                cube.AddComponent<XRSimpleInteractable>();
                var button = cube.AddComponent<ElementSpawnButton>();
                button.Configure(el, atomPrefab, spawnAnchor);

                if (labelFontAvailable) BuildCellLabels(cx, cy, el);
            }
        }

        private void SetMprColor(GameObject go, Color c)
        {
            if (!go.TryGetComponent<MeshRenderer>(out var mr)) return;
            mr.sharedMaterial = GetPeriodicTableMaterial();
            mr.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, c);
            _mpb.SetColor(ColorId, c);
            mr.SetPropertyBlock(_mpb);
        }

        private Material GetPeriodicTableMaterial()
        {
            if (periodicTableMaterial != null)
                return periodicTableMaterial;

            if (_runtimeMaterial != null)
                return _runtimeMaterial;

            Shader shader =
                Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                ?? Shader.Find("Standard");

            _runtimeMaterial = new Material(shader)
            {
                name = "PeriodicTable_Runtime"
            };

            if (_runtimeMaterial.HasProperty("_Surface"))
                _runtimeMaterial.SetFloat("_Surface", 0f);
            if (_runtimeMaterial.HasProperty("_WorkflowMode"))
                _runtimeMaterial.SetFloat("_WorkflowMode", 1f);
            if (_runtimeMaterial.HasProperty("_Metallic"))
                _runtimeMaterial.SetFloat("_Metallic", 0f);
            if (_runtimeMaterial.HasProperty("_Smoothness"))
                _runtimeMaterial.SetFloat("_Smoothness", 0.15f);
            if (_runtimeMaterial.HasProperty("_BaseColor"))
                _runtimeMaterial.SetColor("_BaseColor", Color.white);
            if (_runtimeMaterial.HasProperty("_Color"))
                _runtimeMaterial.SetColor("_Color", Color.white);
            _runtimeMaterial.enableInstancing = true;
            return _runtimeMaterial;
        }

        private void BuildCellLabels(float cx, float cy, ElementSO element)
        {
            BuildText(
                $"AtomicNumber_{element.Symbol}",
                element.AtomicNumber.ToString(),
                cx - cellSize.x * 0.03f,
                cy + cellSize.y * 0.29f,
                cubeDepth + 0.005f,
                atomicNumberHeightFactor,
                TextAlignmentOptions.TopLeft,
                new Vector2(cellSize.x * 0.65f, cellSize.y * 0.25f));

            BuildText(
                $"Symbol_{element.Symbol}",
                element.Symbol,
                cx,
                cy + cellSize.y * 0.04f,
                cubeDepth + 0.0055f,
                symbolHeightFactor,
                TextAlignmentOptions.Center,
                new Vector2(cellSize.x * 0.92f, cellSize.y * 0.4f));

            string footer = $"{element.ElementName}\n{element.AtomicMass:0.###}";
            BuildText(
                $"Footer_{element.Symbol}",
                footer,
                cx,
                cy - cellSize.y * 0.22f,
                cubeDepth + 0.005f,
                footerHeightFactor,
                TextAlignmentOptions.Bottom,
                new Vector2(cellSize.x * 0.95f, cellSize.y * 0.34f));
        }

        private void BuildText(
            string objectName,
            string text,
            float cx,
            float cy,
            float z,
            float heightFactor,
            TextAlignmentOptions alignment,
            Vector2 size)
        {
            var labelGo = new GameObject(objectName);
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = new Vector3(cx, cy, z);
            labelGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            labelGo.transform.localScale = Vector3.one;

            var tmp = labelGo.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.alignment = alignment;
            tmp.color = labelColor;
            tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.lineSpacing = -18f;

            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 0.05f;
            tmp.fontSizeMax = Mathf.Max(0.2f, heightFactor * 1.2f);
            tmp.fontSize = tmp.fontSizeMax;

            var rt = tmp.rectTransform;
            rt.sizeDelta = size;
        }
    }
}
