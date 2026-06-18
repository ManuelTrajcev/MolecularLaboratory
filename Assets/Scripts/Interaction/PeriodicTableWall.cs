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
        [SerializeField] private Vector2 cellSize = new Vector2(0.15f, 0.15f);
        [SerializeField] private Vector2 cellSpacing = new Vector2(0.019f, 0.019f);
        [SerializeField] private float panelPadding = 0.1f;
        [SerializeField] private float panelDepth = 0.03f;
        [SerializeField] private float cubeDepth = 0.065f;

        [Header("Curvature")]
        [SerializeField] private bool curveHorizontally = true;
        [SerializeField, Min(0f)] private float curveDepth = 0.35f;
        [SerializeField, Range(0f, 35f)] private float maxCurveYaw = 18f;
        [SerializeField, Range(4, 48)] private int panelCurveSegments = 28;

        [Header("Spawn anchor (wall-local offset)")]
        [SerializeField] private Vector3 spawnAnchorLocalOffset = new Vector3(0f, -0.4f, 0.25f);

        [Header("Visuals")]
        [SerializeField] private Material periodicTableMaterial;
        [SerializeField] private Color panelColor = new Color(0.12f, 0.13f, 0.16f, 1f);
        [SerializeField] private bool showLabels = true;
        [SerializeField] private Color labelColor = Color.white;
        [SerializeField, Range(0.1f, 1.2f)] private float symbolHeightFactor = 0.85f;
        [SerializeField, Range(0.08f, 0.7f)] private float atomicNumberHeightFactor = 0.3f;
        [SerializeField, Range(0.08f, 0.55f)] private float footerHeightFactor = 0.28f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private MaterialPropertyBlock _mpb;
        private Material _runtimeMaterial;
        private float _currentGridWidth;

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
            _currentGridWidth = gridW;

            BuildCurvedPanel(gridW + 2f * panelPadding, gridH + 2f * panelPadding);

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
                cube.transform.localPosition = GetCurvedLocalPosition(cx, cy, cubeDepth * 0.5f, out float yaw);
                cube.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
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

        private void BuildCurvedPanel(float width, float height)
        {
            var panelRoot = new GameObject("Panel");
            panelRoot.transform.SetParent(transform, false);

            int segmentCount = curveHorizontally ? Mathf.Max(4, panelCurveSegments) : 1;
            float segmentWidth = width / segmentCount;
            float startX = -width * 0.5f + segmentWidth * 0.5f;

            for (int i = 0; i < segmentCount; i++)
            {
                float x = startX + segmentWidth * i;
                var segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                segment.name = segmentCount == 1 ? "PanelSegment" : $"PanelSegment_{i + 1:00}";
                segment.transform.SetParent(panelRoot.transform, false);
                segment.transform.localPosition = GetCurvedLocalPosition(x, 0f, -panelDepth * 0.5f, out float yaw);
                segment.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                segment.transform.localScale = new Vector3(segmentWidth * 1.08f, height, panelDepth);
                if (segment.TryGetComponent<Collider>(out var collider)) Destroy(collider);
                SetMprColor(segment, panelColor);
            }
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
            labelGo.transform.localPosition = GetCurvedLocalPosition(cx, cy, z, out float yaw);
            labelGo.transform.localRotation = Quaternion.Euler(0f, 180f + yaw, 0f);
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

        private Vector3 GetCurvedLocalPosition(float x, float y, float z, out float yaw)
        {
            yaw = 0f;
            if (!curveHorizontally || _currentGridWidth <= 0.0001f || curveDepth <= 0f)
                return new Vector3(x, y, z);

            float normalizedX = Mathf.Clamp(x / (_currentGridWidth * 0.5f), -1f, 1f);
            float curvedZ = z + curveDepth * normalizedX * normalizedX;
            yaw = -normalizedX * maxCurveYaw;
            return new Vector3(x, y, curvedZ);
        }
    }
}
