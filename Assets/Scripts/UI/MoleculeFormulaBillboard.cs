using System.Collections.Generic;
using MolecularLab.Chemistry;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MolecularLab.UI
{
    public class MoleculeFormulaBillboard : MonoBehaviour
    {
        private static Sprite _bubbleSprite;

        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.22f, 0f);
        [SerializeField, Min(0.0001f)] private float worldScale = 0.0016f;
        [SerializeField] private Color bubbleColor = new Color(1f, 1f, 1f, 0.72f);
        [SerializeField] private Color textColor = Color.black;
        [SerializeField, Min(1f)] private float fontSize = 64f;

        private readonly List<Atom> _atoms = new List<Atom>();
        private RectTransform _rectTransform;
        private TextMeshProUGUI _label;

        public static MoleculeFormulaBillboard Create(CompoundSO compound, IReadOnlyList<Atom> atoms)
        {
            if (compound == null || atoms == null || atoms.Count == 0)
                return null;

            var go = new GameObject($"MoleculeFormula_{compound.Formula}", typeof(RectTransform), typeof(Canvas), typeof(MoleculeFormulaBillboard));
            var billboard = go.GetComponent<MoleculeFormulaBillboard>();
            billboard.SetMolecule(compound, atoms);
            return billboard;
        }

        public void SetMolecule(CompoundSO compound, IReadOnlyList<Atom> atoms)
        {
            _atoms.Clear();
            if (atoms != null)
            {
                for (int i = 0; i < atoms.Count; i++)
                {
                    if (atoms[i] != null)
                        _atoms.Add(atoms[i]);
                }
            }

            Build();
            _label.text = compound != null ? FormatFormula(compound.Formula) : string.Empty;
            UpdatePosition();
        }

        private void Awake()
        {
            Build();
        }

        private void LateUpdate()
        {
            UpdatePosition();
            FaceCamera();
        }

        private void Build()
        {
            if (_label != null)
                return;

            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 305;

            _rectTransform = GetComponent<RectTransform>();
            _rectTransform.sizeDelta = new Vector2(240f, 104f);
            transform.localScale = Vector3.one * worldScale;

            var background = new GameObject("Bubble", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(transform, false);
            var bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var image = background.GetComponent<Image>();
            image.sprite = GetBubbleSprite();
            image.color = bubbleColor;
            image.raycastTarget = false;

            var label = new GameObject("Formula", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(background.transform, false);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(18f, 10f);
            labelRect.offsetMax = new Vector2(-18f, -10f);

            _label = label.GetComponent<TextMeshProUGUI>();
            _label.color = textColor;
            _label.fontSize = fontSize;
            _label.fontStyle = FontStyles.Bold;
            _label.alignment = TextAlignmentOptions.Center;
            _label.raycastTarget = false;
        }

        private void UpdatePosition()
        {
            if (_atoms.Count == 0)
                return;

            Bounds bounds = default;
            bool hasBounds = false;
            for (int i = _atoms.Count - 1; i >= 0; i--)
            {
                var atom = _atoms[i];
                if (atom == null)
                {
                    _atoms.RemoveAt(i);
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = new Bounds(atom.transform.position, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(atom.transform.position);
                }
            }

            if (!hasBounds)
                return;

            transform.position = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z) + worldOffset;
        }

        private void FaceCamera()
        {
            var cam = Camera.main;
            if (cam == null)
                return;

            Vector3 direction = transform.position - cam.transform.position;
            if (direction.sqrMagnitude < 0.0001f)
                return;

            transform.rotation = Quaternion.LookRotation(direction.normalized, cam.transform.up);
        }

        private static string FormatFormula(string formula)
        {
            if (string.IsNullOrEmpty(formula))
                return string.Empty;

            var sb = new System.Text.StringBuilder(formula.Length * 2);
            for (int i = 0; i < formula.Length; i++)
            {
                char c = formula[i];
                sb.Append(char.IsDigit(c) ? $"<sub>{c}</sub>" : c.ToString());
            }

            return sb.ToString();
        }

        private static Sprite GetBubbleSprite()
        {
            if (_bubbleSprite != null)
                return _bubbleSprite;

            _bubbleSprite = CreateRoundedSprite("RuntimeMoleculeFormulaBubble", 64, 14f);
            return _bubbleSprite;
        }

        private static Sprite CreateRoundedSprite(string textureName, int size, float radius)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = textureName,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(radius - x, 0f, x - (size - 1 - radius));
                    float dy = Mathf.Max(radius - y, 0f, y - (size - 1 - radius));
                    float distance = new Vector2(dx, dy).magnitude;
                    float alpha = Mathf.Clamp01(radius - distance + 1f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
