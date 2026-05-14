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
        [SerializeField] private Color panelColor = new Color(0.12f, 0.13f, 0.16f, 1f);
        [SerializeField] private bool showLabels = true;
        [SerializeField] private Color labelColor = Color.white;
        [SerializeField, Range(0.05f, 0.95f)] private float labelHeightFactor = 0.6f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock _mpb;

        private void Start()
        {
            _mpb = new MaterialPropertyBlock();
            Build();
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

                float cx = -halfW + cellSize.x * 0.5f + (gp.Group - 1) * colStride;
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

                if (labelFontAvailable) BuildLabel(cx, cy, el.Symbol);
            }
        }

        private void SetMprColor(GameObject go, Color c)
        {
            if (!go.TryGetComponent<MeshRenderer>(out var mr)) return;
            mr.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, c);
            mr.SetPropertyBlock(_mpb);
        }

        private void BuildLabel(float cx, float cy, string symbol)
        {
            var labelGo = new GameObject($"Label_{symbol}");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = new Vector3(cx, cy, cubeDepth + 0.005f);
            labelGo.transform.localRotation = Quaternion.identity;
            labelGo.transform.localScale = Vector3.one;

            var tmp = labelGo.AddComponent<TextMeshPro>();
            tmp.text = symbol;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = labelColor;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Truncate;

            float targetHeight = cellSize.y * labelHeightFactor;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = targetHeight * 0.25f;
            tmp.fontSizeMax = targetHeight;
            tmp.fontSize = targetHeight;

            var rt = tmp.rectTransform;
            rt.sizeDelta = new Vector2(cellSize.x, cellSize.y);
        }
    }
}
