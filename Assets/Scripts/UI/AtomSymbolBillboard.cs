using MolecularLab.Chemistry;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MolecularLab.UI
{
    public class AtomSymbolBillboard : MonoBehaviour
    {
        private const string BubbleName = "AtomSymbolBubble";

        private static Sprite _circleSprite;

        [SerializeField] private Vector3 localOffset = new Vector3(0f, 1.05f, 0f);
        [SerializeField, Min(0.01f)] private float worldScale = 0.0012f;
        [SerializeField] private Color bubbleColor = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField] private Color textColor = Color.black;
        [SerializeField, Min(1f)] private float fontSize = 56f;

        private RectTransform _rectTransform;
        private TextMeshProUGUI _label;
        private Atom _atom;

        public static AtomSymbolBillboard Ensure(Atom atom)
        {
            if (atom == null)
                return null;

            var existing = atom.GetComponentInChildren<AtomSymbolBillboard>(true);
            if (existing != null)
            {
                existing._atom = atom;
                return existing;
            }

            var go = new GameObject(BubbleName, typeof(RectTransform), typeof(Canvas), typeof(AtomSymbolBillboard));
            go.transform.SetParent(atom.transform, false);
            var billboard = go.GetComponent<AtomSymbolBillboard>();
            billboard._atom = atom;
            billboard.Build();
            return billboard;
        }

        public void SetElement(ElementSO element)
        {
            if (_label == null)
                Build();

            _label.text = element != null ? element.Symbol : string.Empty;
            PositionAboveAtom();
        }

        private void Awake()
        {
            if (_atom == null)
                _atom = GetComponentInParent<Atom>();

            Build();
            if (_atom != null)
                SetElement(_atom.Element);
        }

        private void LateUpdate()
        {
            PositionAboveAtom();
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
            canvas.sortingOrder = 300;

            _rectTransform = GetComponent<RectTransform>();
            _rectTransform.sizeDelta = new Vector2(92f, 92f);
            ApplyWorldScale();

            var background = new GameObject("Bubble", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(transform, false);
            var bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var image = background.GetComponent<Image>();
            image.sprite = GetCircleSprite();
            image.color = bubbleColor;
            image.raycastTarget = false;

            var label = new GameObject("Symbol", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(background.transform, false);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 6f);
            labelRect.offsetMax = new Vector2(-8f, -6f);

            _label = label.GetComponent<TextMeshProUGUI>();
            _label.color = textColor;
            _label.fontSize = fontSize;
            _label.fontStyle = FontStyles.Bold;
            _label.alignment = TextAlignmentOptions.Center;
            _label.raycastTarget = false;

            PositionAboveAtom();
        }

        private void PositionAboveAtom()
        {
            if (_atom == null || _atom.Element == null)
            {
                transform.localPosition = localOffset;
                ApplyWorldScale();
                return;
            }

            float atomRadius = Mathf.Max(_atom.Element.DisplayRadius, 0.04f);
            float parentScaleY = Mathf.Max(Mathf.Abs(transform.parent != null ? transform.parent.lossyScale.y : 1f), 0.0001f);
            transform.localPosition = Vector3.up * ((atomRadius + 0.08f) / parentScaleY);
            ApplyWorldScale();
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

        private void ApplyWorldScale()
        {
            Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            transform.localScale = new Vector3(
                worldScale / Mathf.Max(Mathf.Abs(parentScale.x), 0.0001f),
                worldScale / Mathf.Max(Mathf.Abs(parentScale.y), 0.0001f),
                worldScale / Mathf.Max(Mathf.Abs(parentScale.z), 0.0001f));
        }

        private static Sprite GetCircleSprite()
        {
            if (_circleSprite != null)
                return _circleSprite;

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "RuntimeAtomSymbolBubble",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            float radius = size * 0.48f;
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(radius - distance + 1f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            _circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            return _circleSprite;
        }
    }
}
