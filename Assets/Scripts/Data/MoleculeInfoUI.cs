using MolecularLab.Chemistry;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MolecularLab.UI
{
    /// <summary>
    /// Слуша за Bond настани и ажурира UI панел со:
    ///   – моменталната молекуларна формула
    ///   – информации за соединението (ако е познато)
    ///   – 2D текстуален приказ на структурата
    ///
    /// ПОСТАВУВАЊЕ:
    ///   1. Прикачи оваа скрипта на Canvas GameObject.
    ///   2. Повлечи ги TMP полињата во Inspector.
    ///   3. Повлечи го CompoundDatabase asset.
    ///   4. Поврзи ги BondManager и (опционално) атом-ивентите.
    /// </summary>
    public class MoleculeInfoUI : MonoBehaviour
    {
        // ── Singleton (опционален — за лесен пристап од AtomGrabSensor) ────
        public static MoleculeInfoUI Instance { get; private set; }

        [Header("Референци — задолжителни")]
        [SerializeField] private BondManager bondManager;
        [SerializeField] private CompoundDatabase database;

        [Header("UI Елементи")]
        [Tooltip("Панел кој се прикажува/крие")]
        [SerializeField] private GameObject infoPanel;

        [Tooltip("Голем текст: H2O")]
        [SerializeField] private TextMeshProUGUI formulaText;

        [Tooltip("Македонски назив: Вода")]
        [SerializeField] private TextMeshProUGUI compoundNameText;

        [Tooltip("Молекуларна маса: 18.015 g/mol")]
        [SerializeField] private TextMeshProUGUI molecularMassText;

        [Tooltip("Агрегатна состојба и категорија")]
        [SerializeField] private TextMeshProUGUI categoryText;

        [Tooltip("Опис на соединението")]
        [SerializeField] private TextMeshProUGUI descriptionText;

        [Tooltip("2D структура: H — O — H")]
        [SerializeField] private TextMeshProUGUI structure2DText;

        [Tooltip("Прикажано кога формулата не е во базата")]
        [SerializeField] private TextMeshProUGUI unknownLabel;

        [Tooltip("Акцентна линија/слика за боја на соединение")]
        [SerializeField] private Graphic accentGraphic;

        [Header("Поставки")]
        [Tooltip("Прикажи панел и за непознати соединенија (само формула)")]
        [SerializeField] private bool showForUnknown = true;

        [Tooltip("Задршка во секунди по кршење на врска пред панелот се крие")]
        [SerializeField, Min(0f)] private float hideDelay = 1.5f;

        // ── Инterna состојба ─────────────────────────────────────────────────
        private float _hideTimer = -1f;
        private bool  _panelVisible = false;
        private string _lastFormula = "";

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (bondManager == null)
                bondManager = FindFirstObjectByType<BondManager>();

            if (bondManager == null)
            {
                Debug.LogError("[MoleculeInfoUI] BondManager не е пронајден!", this);
                return;
            }

            bondManager.BondFormed += OnBondFormed;
            HidePanel(instant: true);
        }

        private void OnEnable()
        {
            // Поврзи BondManager ако Start уште не е повикан
            if (bondManager != null) bondManager.BondFormed += OnBondFormed;
        }

        private void OnDisable()
        {
            if (bondManager != null) bondManager.BondFormed -= OnBondFormed;
        }

        private void Update()
        {
            // Автоматско криење по одложување
            if (_hideTimer > 0f)
            {
                _hideTimer -= Time.deltaTime;
                if (_hideTimer <= 0f)
                {
                    _hideTimer = -1f;
                    HidePanel();
                }
            }
        }

        // ── Јавен API (AtomGrabSensor го повикува) ───────────────────────────

        /// <summary>
        /// Повикај го кога врски се скинуваат — панелот ќе се сокрие по hideDelay.
        /// </summary>
        public void NotifyBondsBreaking()
        {
            _hideTimer = hideDelay;
        }

        // ── Обработка на настани ─────────────────────────────────────────────

        private void OnBondFormed(Bond bond)
        {
            if (bond == null || bond.A == null) return;

            // Откажи одложено криење
            _hideTimer = -1f;

            // Изгради snapshot на молекулата
            var snap = Molecule.BuildFrom(bond.A);
            if (snap.Atoms.Count == 0) return;

            // Изгради формула
            string formula = MoleculeFormulaBuilder.Build(snap.ElementCounts);
            if (string.IsNullOrEmpty(formula)) return;

            // Побарај во базата
            CompoundSO compound = database != null
    ? database.FindMatchingCompound()
    : null;

            // Ажурирај UI
            UpdateUI(formula, compound, snap.IsClosed);
        }

        // ── UI ажурирање ─────────────────────────────────────────────────────

        private void UpdateUI(string formula, CompoundSO compound, bool isClosed)
        {
            bool isKnown = compound != null;

            // Не прикажувај ако е непознато и опцијата е исклучена
            if (!isKnown && !showForUnknown)
            {
                HidePanel();
                return;
            }

            _lastFormula = formula;
            ShowPanel();

            // ── Формула (секогаш) ─────────────────────────────────────────
            if (formulaText != null)
                formulaText.text = FormatFormula(formula);

            // ── Непознато соединение ──────────────────────────────────────
            if (unknownLabel != null)
            {
                bool showUnknown = !isKnown;
                unknownLabel.gameObject.SetActive(showUnknown);
                if (showUnknown)
                {
                    unknownLabel.text = isClosed
                        ? "Непознато соединение"
                        : "Нецелосна молекула...";
                }
            }

            // ── Информации за познато соединение ─────────────────────────
            if (compoundNameText != null)
            {
                compoundNameText.gameObject.SetActive(isKnown);
                if (isKnown) compoundNameText.text = compound.MacedonianName;
            }

            if (molecularMassText != null)
            {
                molecularMassText.gameObject.SetActive(isKnown);
                if (isKnown) molecularMassText.text = $"Молекуларна маса: {compound.MassFormatted}";
            }

            if (categoryText != null)
            {
                categoryText.gameObject.SetActive(isKnown);
                if (isKnown)
                    categoryText.text = $"{LocalizeState(compound.StateAtRoomTemp)}  ·  {LocalizeCategory(compound.Category)}";
            }

            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(isKnown && !string.IsNullOrEmpty(compound.Description));
                if (isKnown) descriptionText.text = compound.Description;
            }

            if (structure2DText != null)
            {
                bool hasStructure = isKnown && !string.IsNullOrEmpty(compound.Structure2D);
                structure2DText.gameObject.SetActive(hasStructure);
                if (hasStructure) structure2DText.text = compound.Structure2D;
            }

            // ── Акцентна боја ─────────────────────────────────────────────
            if (accentGraphic != null)
                accentGraphic.color = isKnown ? compound.AccentColor : Color.gray;
        }

        // ── Помошни методи ───────────────────────────────────────────────────

        private void ShowPanel()
        {
            if (!_panelVisible && infoPanel != null)
            {
                infoPanel.SetActive(true);
                _panelVisible = true;
            }
        }

        private void HidePanel(bool instant = false)
        {
            if (_panelVisible || instant)
            {
                if (infoPanel != null) infoPanel.SetActive(false);
                _panelVisible = false;
            }
        }

        /// <summary>
        /// Претвора "H2O" во богато форматиран TMP текст "H₂O".
        /// Бројките ги претвора во Unicode subscript знаци.
        /// </summary>
        private static string FormatFormula(string formula)
        {
            if (string.IsNullOrEmpty(formula)) return "";

            var sb = new System.Text.StringBuilder();
            foreach (char c in formula)
            {
                if (char.IsDigit(c))
                {
                    // Unicode subscript: ₀ = U+2080
                    sb.Append((char)(0x2080 + (c - '0')));
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static string LocalizeState(AggregateState state) => state switch
        {
            AggregateState.Gas    => "Гас",
            AggregateState.Liquid => "Течност",
            AggregateState.Solid  => "Цврста",
            AggregateState.Plasma => "Плазма",
            _                     => "Непознато"
        };

        private static string LocalizeCategory(CompoundCategory cat) => cat switch
        {
            CompoundCategory.Oxide               => "Оксид",
            CompoundCategory.Acid                => "Киселина",
            CompoundCategory.Base                => "База",
            CompoundCategory.Salt                => "Сол",
            CompoundCategory.OrganicHydrocarbon  => "Јаглеводород",
            CompoundCategory.OrganicOther        => "Органско",
            CompoundCategory.ElementalMolecule   => "Елементарна молекула",
            _                                    => "Друго"
        };
    }
}