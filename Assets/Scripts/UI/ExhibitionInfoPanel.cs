#if UNITY_EDITOR
using UnityEditor;
#endif

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Freeline
{
    // Sergi günü olmadığında sergi bilgilerini gösteren panel
    public class ExhibitionInfoPanel : MonoBehaviour
    {
        [Header("Panel Root")]
        [SerializeField] private GameObject panel;

        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI nextExhibitionText;
        [SerializeField] private TextMeshProUGUI stockCountText;

        [Header("Stock List")]
        [SerializeField] private RectTransform stockListContent; // ScrollRect içeriği

        [Header("Buttons")]
        [SerializeField] private Button backButton;

        [Header("Wiring")]
        [SerializeField] private ExhibitionManager exhibitionManager;

        private static readonly Color PanelBg = new Color(0.08f, 0.08f, 0.12f, 0.96f);
        private static readonly Color BackBtn = new Color(0.18f, 0.18f, 0.30f, 1.00f);
        private static readonly Color RowEven = new Color(0.14f, 0.14f, 0.20f, 1.00f);
        private static readonly Color RowOdd = new Color(0.10f, 0.10f, 0.16f, 1.00f);
        private static readonly Color StatColor = new Color(0.85f, 0.85f, 0.95f, 1.00f);

        void Awake()
        {
            backButton.onClick.AddListener(Hide);

            // Paneli gizle
            if (panel != null) panel.SetActive(false);

            // Blocker'ı da ayrı bir obje ise onu da gizle
            Transform blocker = transform.Find("Blocker");
            if (blocker != null) blocker.gameObject.SetActive(false);
        }

        void Start()
        {
            if (exhibitionManager == null)
                exhibitionManager = GameManager.Instance.ExhibitionManager;
        }

        // HUDManager'ın SERGI düğmesinden çağrılır (sergi günü değilse)
        public void Show()
        {
            panel.SetActive(true);
            RefreshInfo();
        }

        public void Hide() => panel.SetActive(false);

        // Yöneticilerden güncel verileri okur ve tüm alanları senkronize eder
        private void RefreshInfo()
        {
            int currentDay = GameManager.Instance.TimeManager.CurrentDay;
            int daysUntilNext = 7 - (currentDay % 7);

            // Eğer bugün 7. günse ve sergi açılmadıysa 0 gün yazsın
            if (daysUntilNext == 7) daysUntilNext = 0;

            nextExhibitionText.text = $"Sonraki Sergiye: {daysUntilNext} gün";

            var stock = exhibitionManager.GetStock();
            int totalItems = 0;
            foreach (var item in stock) totalItems += item.quantity;

            // İleride yetenek ağacından çekeceğimiz statlar için hazırlık
            int popBonus = 0; // Serginin popülerlik (müşteri çekme) bonusu
            int bargainBonus = 0; // Pazarlık şansı bonusu

            stockCountText.text = $"Stok: {totalItems} ürün\n" +
                                  $"<size=70%><color=#A0A0A0>Popülerlik: +%{popBonus} | Pazarlık Gücü: +%{bargainBonus}</color></size>";

            RebuildStockList(stock);
        }

        // Stok listesini yeniden inşa eder; mevcut satırları temizleyip yeniden oluşturur
        private void RebuildStockList(System.Collections.Generic.IReadOnlyList<ExhibitionStock> stock)
        {
            foreach (Transform child in stockListContent)
                Destroy(child.gameObject);

            for (int i = 0; i < stock.Count; i++)
            {
                var item = stock[i];

                // 1. Satırın kendisini oluştur (Sadece Arka Plan)
                var row = new GameObject($"Row_{i}", typeof(RectTransform));
                row.transform.SetParent(stockListContent, false);
                var rowImg = row.AddComponent<Image>();
                rowImg.color = (i % 2 == 0) ? RowEven : RowOdd;
                row.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 70f);

                // 2. Metni barındıracak ayrı bir çocuk obje oluştur
                var textObj = new GameObject("Text", typeof(RectTransform));
                textObj.transform.SetParent(row.transform, false);

                // Metin objesini satırı kaplayacak şekilde ayarla
                var textRT = textObj.GetComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = new Vector2(20f, 0f);
                textRT.offsetMax = new Vector2(-20f, 0f);

                var tmp = textObj.AddComponent<TextMeshProUGUI>(); // Burası artık hata vermeyecek
                tmp.text = $"{item.productName}  x{item.quantity}  —  {item.basePrice} coin";
                tmp.fontSize = 30f;
                tmp.color = StatColor;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
            }

            // Stok boşsa bilgi satırı (Burada da aynı hatayı almamak için aynı yöntemi kullan)
            if (stock.Count == 0)
            {
                var emptyRow = new GameObject("EmptyRow", typeof(RectTransform));
                emptyRow.transform.SetParent(stockListContent, false);

                var tmp = emptyRow.AddComponent<TextMeshProUGUI>(); // Buraya Image eklemedik, o yüzden Image hatası vermez
                tmp.text = "Henüz stok yok.";
                tmp.fontSize = 30f;
                tmp.color = new Color(0.5f, 0.5f, 0.6f, 1f);
                tmp.alignment = TextAlignmentOptions.Center;
            }
        }

        // =========================================================================
        // EDITOR — Hierarchy builder
        // Right-click this component in the Inspector → "Build Exhibition Info Hierarchy"
        //
        // Layout (reference resolution 1080 × 1920):
        //   ExhibitionInfoPanel (full-screen RectTransform)
        //     Blocker          — full-screen dark overlay
        //     Panel            — 900 × 1000, centered
        //       CloseButton    — 90 × 90, top-right
        //       HeaderText     — "SERGİ", 24 px from top, 100 px tall
        //       Divider        — 2 px at 130 px from top
        //       NextExhibText  — 150 px from top, 60 px tall
        //       StockCountText — 220 px from top, 60 px tall
        //       Divider2       — 2 px at 290 px from top
        //       ScrollRect     — 300 px to 860 px from top
        //         Viewport → Content (VerticalLayoutGroup, spacing 4)
        //       BackButton     — 700 × 80, 880 px from top
        // =========================================================================

#if UNITY_EDITOR
        [ContextMenu("Build Exhibition Info Hierarchy")]
        private void BuildExhibitionInfoHierarchy()
        {
            while (transform.childCount > 0)
                DestroyImmediate(transform.GetChild(0).gameObject);

            Stretch(GetComponent<RectTransform>());

            // Tam ekran karartma
            var blocker = NewUIObject("Blocker", transform);
            blocker.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            Stretch(blocker.GetComponent<RectTransform>());

            // Merkezi panel — 900 × 1000
            var panelGO = NewUIObject("Panel", transform);
            panelGO.AddComponent<Image>().color = PanelBg;
            var panelRT = panelGO.GetComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = new Vector2(900f, 1000f);
            panelRT.anchoredPosition = Vector2.zero;

            // Kapat düğmesi — sağ üst köşe
            var closeBtn = BuildIconButton("CloseButton", panelGO.transform, "X",
                new Color(0.45f, 0.12f, 0.12f, 1f));
            var closeBtnRT = closeBtn.GetComponent<RectTransform>();
            closeBtnRT.anchorMin = new Vector2(1f, 1f);
            closeBtnRT.anchorMax = new Vector2(1f, 1f);
            closeBtnRT.pivot = new Vector2(1f, 1f);
            closeBtnRT.sizeDelta = new Vector2(90f, 90f);
            closeBtnRT.anchoredPosition = new Vector2(-16f, -16f);
            // Kapat = geri; ayrı bir alan saklanmıyor
            closeBtn.onClick.AddListener(Hide);

            // "SERGİ" başlık
            var header = NewTMP("HeaderText", panelGO.transform, "SERGİ", 60f, TextAlignmentOptions.Center);
            header.fontStyle = FontStyles.Bold;
            PositionFromTop(header.rectTransform, 0f, 1f, 24f, 100f, 0f);

            // Ayırıcı 1
            BuildDivider("Divider", panelGO.transform, 130f);

            // Sonraki sergi
            var nextTMP = NewTMP("NextExhibitionText", panelGO.transform,
                "Sonraki Sergiye: — gün", 36f, TextAlignmentOptions.Center);
            nextTMP.color = StatColor;
            PositionFromTop(nextTMP.rectTransform, 0f, 1f, 150f, 60f, 20f);

            // Stok sayısı
            var stockTMP = NewTMP("StockCountText", panelGO.transform,
                "Stok: 0 ürün", 36f, TextAlignmentOptions.Center);
            stockTMP.color = StatColor;
            PositionFromTop(stockTMP.rectTransform, 0f, 1f, 220f, 60f, 20f);

            // Ayırıcı 2
            BuildDivider("Divider2", panelGO.transform, 290f);

            // ScrollRect — 300 px'den 860 px'e kadar
            var scrollGO = NewUIObject("StockScrollRect", panelGO.transform);
            var scrollRT = scrollGO.GetComponent<RectTransform>();
            scrollRT.anchorMin = new Vector2(0f, 1f);
            scrollRT.anchorMax = new Vector2(1f, 1f);
            scrollRT.pivot = new Vector2(0.5f, 1f);
            scrollRT.offsetMin = new Vector2(10f, -860f);
            scrollRT.offsetMax = new Vector2(-10f, -300f);

            var scroll = scrollGO.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 30f;

            // Viewport
            var viewportGO = NewUIObject("Viewport", scrollGO.transform);
            viewportGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            viewportGO.AddComponent<Mask>().showMaskGraphic = false;
            Stretch(viewportGO.GetComponent<RectTransform>());
            scroll.viewport = viewportGO.GetComponent<RectTransform>();

            // Content — VerticalLayoutGroup ile satırları otomatik dizer
            var contentGO = NewUIObject("Content", viewportGO.transform);
            var contentRT = contentGO.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.offsetMin = Vector2.zero;
            contentRT.offsetMax = Vector2.zero;
            contentRT.sizeDelta = new Vector2(0f, 0f);

            var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            var csf = contentGO.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scroll.content = contentRT;

            // GERİ düğmesi
            var backBtn = BuildActionButton("BackButton", panelGO.transform,
                "GERİ", BackBtn, 700f, 80f, 880f, 36f);

            // SerializeField referansları
            panel = panelGO;
            nextExhibitionText = nextTMP;
            stockCountText = stockTMP;
            stockListContent = contentRT;
            backButton = backBtn;

            panelGO.SetActive(false);

            EditorUtility.SetDirty(this);
            Debug.Log("[ExhibitionInfoPanel] Hierarchy built.");
        }

        private static Button BuildActionButton(string name, Transform parent,
            string label, Color bg, float width, float height,
            float topOffset, float fontSize)
        {
            var go = NewUIObject(name, parent);
            var img = go.AddComponent<Image>();
            img.color = bg;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.85f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.fadeDuration = 0.05f;
            btn.colors = colors;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(0f, -topOffset);

            var tmp = NewTMP("Label", go.transform, label, fontSize, TextAlignmentOptions.Center);
            tmp.fontStyle = FontStyles.Bold;
            Stretch(tmp.rectTransform);
            return btn;
        }

        private static Button BuildIconButton(string name, Transform parent,
            string label, Color bg)
        {
            var go = NewUIObject(name, parent);
            var img = go.AddComponent<Image>();
            img.color = bg;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var tmp = NewTMP("Label", go.transform, label, 36f, TextAlignmentOptions.Center);
            tmp.fontStyle = FontStyles.Bold;
            Stretch(tmp.rectTransform);
            return btn;
        }

        private static void BuildDivider(string name, Transform parent, float topOffset)
        {
            var go = NewUIObject(name, parent);
            go.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.04f, 1f);
            rt.anchorMax = new Vector2(0.96f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -(topOffset + 2f));
            rt.offsetMax = new Vector2(0f, -topOffset);
        }

        private static GameObject NewUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static TextMeshProUGUI NewTMP(string name, Transform parent,
            string text, float fontSize, TextAlignmentOptions align)
        {
            var go = NewUIObject(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void PositionFromTop(RectTransform rt,
            float xMin, float xMax, float topOffset, float height, float padH)
        {
            rt.anchorMin = new Vector2(xMin, 1f);
            rt.anchorMax = new Vector2(xMax, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(padH, -topOffset - height);
            rt.offsetMax = new Vector2(-padH, -topOffset);
        }
#endif
    }
}
