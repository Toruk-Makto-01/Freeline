#if UNITY_EDITOR
using UnityEditor;
#endif

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Freeline
{
    // Sergi için ürün üretim paneli.
    // Oyuncu burada sergi ürünü üretir ve stoğa ekler.
    public class ExhibitionPanel : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Inspector references — populated by [Build Exhibition Hierarchy] or manually
        // -------------------------------------------------------------------------

        [Header("Close")]
        [SerializeField] private Button closeButton;

        [Header("Stock")]
        [SerializeField] private TextMeshProUGUI stockText;

        [Header("Type Selector")]
        [SerializeField] private Button[] typeButtons = new Button[3]; // 0=Poster 1=Tablo 2=Manga

        [Header("Product Info")]
        [SerializeField] private TextMeshProUGUI productionTimeText;
        [SerializeField] private TextMeshProUGUI energyCostText;
        [SerializeField] private TextMeshProUGUI basePriceText;

        [Header("Actions")]
        [SerializeField] private Button          produceButton;
        [SerializeField] private Button          backButton;

        [Header("Status")]
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Data")]
        [SerializeField] private ExhibitionProductData[] products;

        [Header("Navigation")]
        [SerializeField] private DrawMenuPanel   drawMenuPanel;
        [SerializeField] private DrawingMinigame drawingMinigame;

        // -------------------------------------------------------------------------
        // Colors
        // -------------------------------------------------------------------------

        private static readonly Color PanelBg      = new Color(0.08f, 0.08f, 0.12f, 0.96f);
        private static readonly Color CloseBtn      = new Color(0.45f, 0.12f, 0.12f, 1.00f);
        private static readonly Color TypeDefault   = new Color(0.13f, 0.22f, 0.40f, 1.00f);
        private static readonly Color TypeSelected  = new Color(0.22f, 0.48f, 0.80f, 1.00f);
        private static readonly Color ProduceOn     = new Color(0.15f, 0.55f, 0.25f, 1.00f);
        private static readonly Color ProduceOff    = new Color(0.22f, 0.22f, 0.28f, 1.00f);
        private static readonly Color BackBtn       = new Color(0.18f, 0.18f, 0.30f, 1.00f);
        private static readonly Color StatColor     = new Color(0.85f, 0.85f, 0.95f, 1.00f);
        private static readonly Color ErrorColor    = new Color(0.95f, 0.25f, 0.20f, 1.00f);

        // -------------------------------------------------------------------------
        // Runtime state
        // -------------------------------------------------------------------------

        private int _currentStock;
        private int _selectedIndex;

        // -------------------------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------------------------

        void Awake()
        {
            closeButton.onClick.AddListener(OnBackClicked);

            for (int i = 0; i < typeButtons.Length; i++)
            {
                int idx = i;
                // Döngü değişkeni kapatma hatasını önlemek için yerel kopya.
                typeButtons[i].onClick.AddListener(() => SelectProduct(idx));
            }

            produceButton.onClick.AddListener(OnProduceClicked);
            backButton.onClick.AddListener(OnBackClicked);
        }

        void Start()
        {
            GameManager.Instance.EnergyManager.OnEnergyChanged += HandleEnergyChanged;
            Hide();
        }

        void OnDestroy()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.EnergyManager.OnEnergyChanged -= HandleEnergyChanged;
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        // DrawMenuPanel.OnExhibitionClicked tarafından çağrılır.
        public void Show()
        {
            gameObject.SetActive(true);
            statusText.text = string.Empty;
            SelectProduct(0);
            RefreshStock();
        }

        public void Hide() => gameObject.SetActive(false);

        // DrawingMinigame üretim tamamlandığında bu callback'i çağırır.
        public void OnProductionComplete()
        {
            _currentStock++;
            // Panel bu noktada kapalı; stok Show() sırasında yansıtılır.
        }

        // -------------------------------------------------------------------------
        // Veri güncelleme
        // -------------------------------------------------------------------------

        private void RefreshStock()
        {
            stockText.text = $"Stok: {_currentStock} ürün";
        }

        private void SelectProduct(int index)
        {
            _selectedIndex = index;

            // Seçili düğmeyi vurgula, diğerlerini varsayılan renge döndür.
            for (int i = 0; i < typeButtons.Length; i++)
                typeButtons[i].GetComponent<Image>().color = i == index ? TypeSelected : TypeDefault;

            RefreshProductInfo();
            RefreshProduceButton();
        }

        private void RefreshProductInfo()
        {
            if (!HasValidSelection()) return;
            var p = products[_selectedIndex];
            productionTimeText.text = $"Üretim Süresi: {p.productionHours:F1} saat";
            energyCostText.text     = $"Enerji Maliyeti: {Mathf.FloorToInt(p.energyCost)}";
            basePriceText.text      = $"Tahmini Satış Fiyatı: {p.basePrice} coin";
        }

        private void RefreshProduceButton()
        {
            bool can = HasValidSelection()
                       && GameManager.Instance.EnergyManager.CurrentEnergy
                          >= products[_selectedIndex].energyCost;
            produceButton.interactable                = can;
            produceButton.GetComponent<Image>().color = can ? ProduceOn : ProduceOff;
        }

        // products dizisi atanmamış veya seçim geçersizse erken çıkış.
        private bool HasValidSelection()
            => products != null
               && _selectedIndex < products.Length
               && products[_selectedIndex] != null;

        // -------------------------------------------------------------------------
        // Düğme işleyiciler
        // -------------------------------------------------------------------------

        private void OnProduceClicked()
        {
            if (!HasValidSelection()) return;
            // Minigame tamamlandığında OnProductionComplete çağrılır; stok orada artırılır.
            Hide();
            drawingMinigame?.ShowForExhibition(products[_selectedIndex], OnProductionComplete);
        }

        private void OnBackClicked()
        {
            Hide();
            drawMenuPanel?.Show();
        }

        // -------------------------------------------------------------------------
        // Event işleyiciler
        // -------------------------------------------------------------------------

        private void HandleEnergyChanged(float current, float max)
        {
            if (!gameObject.activeSelf) return;
            RefreshProduceButton();
        }

        // =========================================================================
        // EDITOR — Hierarchy builder
        // Right-click this component in the Inspector → "Build Exhibition Hierarchy"
        // Destroys existing children, rebuilds the full hierarchy, and populates
        // every SerializeField reference automatically.
        //
        // Layout (reference resolution 1080 × 1920):
        //   ExhibitionPanel (full-screen RectTransform)
        //     Blocker             — full-screen dark overlay
        //     Panel               — 900 × 1100, centered
        //       CloseButton       — 90 × 90, top-right
        //       HeaderText        — full width, 100 px, 24 px from top
        //       Divider           — 2 px at 134 px from top
        //       StockText         — stat row, 154 px from top
        //       Divider2          — 2 px at 230 px from top
        //       PosterButton      — 260 × 80, x=−290, 250 px from top
        //       TabloButton       — 260 × 80, x=0,    250 px from top
        //       MangaButton       — 260 × 80, x=+290, 250 px from top
        //       Divider3          — 2 px at 350 px from top
        //       ProductionTime    — info row, 370 px from top
        //       EnergyCost        — info row, 438 px from top
        //       BasePrice         — info row, 506 px from top
        //       Divider4          — 2 px at 578 px from top
        //       ProduceButton     — 700 × 110, 598 px from top
        //       BackButton        — 700 × 80,  728 px from top
        //       StatusText        — full width, 80 px, 828 px from top
        // =========================================================================

#if UNITY_EDITOR
        [ContextMenu("Build Exhibition Hierarchy")]
        private void BuildExhibitionHierarchy()
        {
            // Eski çocuk nesneleri temizle; temiz yeniden inşa için gerekli.
            while (transform.childCount > 0)
                DestroyImmediate(transform.GetChild(0).gameObject);

            Stretch(GetComponent<RectTransform>());

            // Tam ekran karartma — panel arkasındaki dokunuşları yakalar.
            var blocker = NewUIObject("Blocker", transform);
            blocker.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            Stretch(blocker.GetComponent<RectTransform>());

            // Ortalanmış panel — 900 × 1100.
            var panel = NewUIObject("Panel", transform);
            panel.AddComponent<Image>().color = PanelBg;
            var panelRT = panel.GetComponent<RectTransform>();
            panelRT.anchorMin        = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax        = new Vector2(0.5f, 0.5f);
            panelRT.pivot            = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta        = new Vector2(900f, 1100f);
            panelRT.anchoredPosition = Vector2.zero;

            // Kapat düğmesi (X) — sağ üst köşe, 90 × 90.
            closeButton = BuildIconButton("CloseButton", panel.transform, "X", CloseBtn);
            var closeRT = closeButton.GetComponent<RectTransform>();
            closeRT.anchorMin        = new Vector2(1f, 1f);
            closeRT.anchorMax        = new Vector2(1f, 1f);
            closeRT.pivot            = new Vector2(1f, 1f);
            closeRT.sizeDelta        = new Vector2(90f, 90f);
            closeRT.anchoredPosition = new Vector2(-16f, -16f);

            // Başlık — "SERGİ ÜRÜNÜ", kalın, panelin üstünden 24 px.
            var header = NewTMP("HeaderText", panel.transform, "SERGİ ÜRÜNÜ", 56f, TextAlignmentOptions.Center);
            header.fontStyle = FontStyles.Bold;
            PositionFromTop(header.rectTransform, 0f, 1f, 24f, 100f, 0f);

            BuildDivider("Divider", panel.transform, 134f);

            // Stok satırı.
            stockText = BuildStatRow("StockText", panel.transform, "Stok: 0 ürün", 154f);

            BuildDivider("Divider2", panel.transform, 230f);

            // Ürün tipi seçici — 3 yan yana düğme, panelin üstünden 250 px.
            typeButtons    = new Button[3];
            typeButtons[0] = BuildTypeButton("PosterButton", panel.transform, "POSTER", -290f, 250f);
            typeButtons[1] = BuildTypeButton("TabloButton",  panel.transform, "TABLO",      0f, 250f);
            typeButtons[2] = BuildTypeButton("MangaButton",  panel.transform, "MANGA",   290f, 250f);

            // İlk düğme seçili görünümde başlar.
            typeButtons[0].GetComponent<Image>().color = TypeSelected;

            BuildDivider("Divider3", panel.transform, 350f);

            // Ürün bilgi satırları.
            productionTimeText = BuildStatRow("ProductionTimeText", panel.transform, "Üretim Süresi: —",          370f);
            energyCostText     = BuildStatRow("EnergyCostText",     panel.transform, "Enerji Maliyeti: —",         438f);
            basePriceText      = BuildStatRow("BasePriceText",      panel.transform, "Tahmini Satış Fiyatı: — coin", 506f);

            BuildDivider("Divider4", panel.transform, 578f);

            // ÜRET düğmesi — 700 × 110, panelin üstünden 598 px.
            produceButton = BuildActionButton("ProduceButton", panel.transform,
                                              "ÜRET", ProduceOn, 700f, 110f, 598f, 50f);

            // GERİ düğmesi — 700 × 80, panelin üstünden 728 px.
            backButton = BuildActionButton("BackButton", panel.transform,
                                           "GERİ", BackBtn, 700f, 80f, 728f, 36f);

            // Durum mesajı — tam genişlik, 80 px, panelin üstünden 828 px.
            statusText = NewTMP("StatusText", panel.transform, string.Empty, 30f, TextAlignmentOptions.Center);
            statusText.color = ErrorColor;
            PositionFromTop(statusText.rectTransform, 0f, 1f, 828f, 80f, 20f);

            EditorUtility.SetDirty(gameObject);
            Debug.Log("[ExhibitionPanel] Hierarchy built and SerializeField references populated.");
        }

        // Ürün tipi seçici düğmesi — sabit genişlik, yan yana hizalama için xOffset parametresi.
        private static Button BuildTypeButton(string name, Transform parent,
                                              string label, float xOffset, float topOffset)
        {
            var go  = NewUIObject(name, parent);
            var img = go.AddComponent<Image>();
            img.color = TypeDefault;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var colors              = btn.colors;
            colors.normalColor      = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.85f);
            colors.pressedColor     = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.fadeDuration     = 0.05f;
            btn.colors              = colors;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 1f);
            rt.anchorMax        = new Vector2(0.5f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.sizeDelta        = new Vector2(260f, 80f);
            rt.anchoredPosition = new Vector2(xOffset, -topOffset);

            var tmp = NewTMP("Label", go.transform, label, 34f, TextAlignmentOptions.Center);
            tmp.fontStyle = FontStyles.Bold;
            Stretch(tmp.rectTransform);

            return btn;
        }

        // Eylem düğmesi — ortalanmış, sabit genişlik/yükseklik, panelin üstünden konumlandırılmış.
        private static Button BuildActionButton(string name, Transform parent,
                                                string label, Color bg,
                                                float width, float height,
                                                float topOffset, float fontSize)
        {
            var go  = NewUIObject(name, parent);
            var img = go.AddComponent<Image>();
            img.color = bg;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var colors              = btn.colors;
            colors.normalColor      = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.85f);
            colors.pressedColor     = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.fadeDuration     = 0.05f;
            btn.colors              = colors;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 1f);
            rt.anchorMax        = new Vector2(0.5f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.sizeDelta        = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(0f, -topOffset);

            var tmp = NewTMP("Label", go.transform, label, fontSize, TextAlignmentOptions.Center);
            tmp.fontStyle = FontStyles.Bold;
            Stretch(tmp.rectTransform);

            return btn;
        }

        // İstatistik / bilgi satırı — sola hizalı, 40 px yatay dolgu.
        private TextMeshProUGUI BuildStatRow(string name, Transform parent,
                                             string placeholder, float topOffset)
        {
            var tmp = NewTMP(name, parent, placeholder, 34f, TextAlignmentOptions.Left);
            tmp.color = StatColor;
            PositionFromTop(tmp.rectTransform, 0f, 1f, topOffset, 52f, 40f);
            return tmp;
        }

        // İnce yatay ayırıcı — panelin üst kenarından topOffset px aşağıda.
        private static void BuildDivider(string name, Transform parent, float topOffset)
        {
            var go = NewUIObject(name, parent);
            go.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.04f, 1f);
            rt.anchorMax = new Vector2(0.96f, 1f);
            rt.pivot     = new Vector2(0.5f,  1f);
            rt.offsetMin = new Vector2(0f, -(topOffset + 2f));
            rt.offsetMax = new Vector2(0f, -topOffset);
        }

        // Metin etiketli simge düğmesi (kapat düğmesi için).
        private static Button BuildIconButton(string name, Transform parent,
                                              string label, Color bg)
        {
            var go  = NewUIObject(name, parent);
            var img = go.AddComponent<Image>();
            img.color = bg;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var tmp = NewTMP("Label", go.transform, label, 36f, TextAlignmentOptions.Center);
            tmp.fontStyle = FontStyles.Bold;
            Stretch(tmp.rectTransform);
            return btn;
        }

        // ---- UI yardımcıları (yalnızca Editor zamanı) ----

        private static GameObject NewUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static TextMeshProUGUI NewTMP(string name, Transform parent,
                                              string text, float fontSize,
                                              TextAlignmentOptions align)
        {
            var go  = NewUIObject(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text          = text;
            tmp.fontSize      = fontSize;
            tmp.color         = Color.white;
            tmp.alignment     = align;
            tmp.raycastTarget = false; // Metin elementleri tıklama olaylarını engellemez.
            return tmp;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // Üst kenara sabitlenmiş eleman konumlandırma; padH = yatay iç dolgu.
        private static void PositionFromTop(RectTransform rt,
                                            float xMin, float xMax,
                                            float topOffset, float height, float padH)
        {
            rt.anchorMin = new Vector2(xMin, 1f);
            rt.anchorMax = new Vector2(xMax, 1f);
            rt.pivot     = new Vector2(0.5f,  1f);
            rt.offsetMin = new Vector2( padH, -topOffset - height);
            rt.offsetMax = new Vector2(-padH, -topOffset);
        }
#endif
    }
}
