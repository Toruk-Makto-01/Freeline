#if UNITY_EDITOR
using UnityEditor;
#endif

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Freeline
{
    // Sergi günü oyun başında çıkan bildirim popup'ı
    // Oyuncu sergiyi açabilir veya geçebilir
    public class ExhibitionNotificationPanel : MonoBehaviour
    {
        [Header("Panel Root")]
        [SerializeField] private GameObject panel;

        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI subtitleText;

        [Header("Buttons")]
        [SerializeField] private Button openButton; // SERGİYİ AÇ
        [SerializeField] private Button skipButton; // GEÇ

        [Header("Wiring")]
        [SerializeField] private ExhibitionManager exhibitionManager;

        private static readonly Color PanelBg  = new Color(0.10f, 0.10f, 0.15f, 0.97f);
        private static readonly Color OpenColor = new Color(0.15f, 0.55f, 0.25f, 1.00f);
        private static readonly Color SkipColor = new Color(0.30f, 0.30f, 0.35f, 1.00f);

        void Awake()
        {
            openButton.onClick.AddListener(OnOpenClicked);
            skipButton.onClick.AddListener(OnSkipClicked);
            Hide(); // Artık hem paneli hem blocker'ı gizleyecek
        }

        void Start()
        {
            if (exhibitionManager == null)
                exhibitionManager = GameManager.Instance.ExhibitionManager;
            exhibitionManager.OnExhibitionDayReady += HandleExhibitionDayReady;
        }

        void OnDestroy()
        {
            if (exhibitionManager != null)
                exhibitionManager.OnExhibitionDayReady -= HandleExhibitionDayReady;
        }

        // OnExhibitionDayReady geldiğinde otomatik gösterilir; HUD'dan da çağrılabilir
        private void HandleExhibitionDayReady() => Show();

        public void Show()
        {
            int total = CountTotalStock();
            subtitleText.text = $"Stokta {total} ürün var. Sergiyi açmak ister misin?";
    
            panel.SetActive(true);
            Transform blocker = transform.Find("Blocker");
            if (blocker != null) blocker.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
            Transform blocker = transform.Find("Blocker");
            if (blocker != null) blocker.gameObject.SetActive(false);
        }

        private void OnOpenClicked()
        {
            Debug.Log("Sergi Başlatılıyor...");
            if (exhibitionManager == null)
                exhibitionManager.StartExhibition(); // Sergi başlatma işlemi ExhibitionManager üzerinden yapılır
            Hide();
        }

        private void OnSkipClicked()
        {
            exhibitionManager.SkipExhibition();
            Hide();
        }

        // Stoktaki tüm kalemlerin toplam miktarını hesaplar
        private int CountTotalStock()
        {
            int total = 0;
            foreach (var item in exhibitionManager.GetStock())
                total += item.quantity;
            return total;
        }

        // =========================================================================
        // EDITOR — Hierarchy builder
        // Right-click this component in the Inspector → "Build Exhibition Notification Hierarchy"
        // =========================================================================

#if UNITY_EDITOR
        [ContextMenu("Build Exhibition Notification Hierarchy")]
        private void BuildExhibitionNotificationHierarchy()
        {
            while (transform.childCount > 0)
                DestroyImmediate(transform.GetChild(0).gameObject);

            Stretch(GetComponent<RectTransform>());

            // Tam ekran karartma
            var blocker = NewUIObject("Blocker", transform);
            blocker.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.60f);
            Stretch(blocker.GetComponent<RectTransform>());

            // Merkezi panel — 800 × 500
            var panelGO = NewUIObject("Panel", transform);
            panelGO.AddComponent<Image>().color = PanelBg;
            var panelRT = panelGO.GetComponent<RectTransform>();
            panelRT.anchorMin        = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax        = new Vector2(0.5f, 0.5f);
            panelRT.pivot            = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta        = new Vector2(800f, 500f);
            panelRT.anchoredPosition = Vector2.zero;

            // "SERGİ" — büyük başlık alanı
            var badge = NewTMP("BadgeText", panelGO.transform, "SERGİ",
                80f, TextAlignmentOptions.Center);
            badge.fontStyle = FontStyles.Bold;
            badge.color     = new Color(1f, 0.85f, 0.2f, 1f);
            PositionFromTop(badge.rectTransform, 0f, 1f, 20f, 110f, 20f);

            // "BUGÜN SERGİ GÜNÜ!" — alt başlık
            var titleTMP = NewTMP("TitleText", panelGO.transform, "BUGÜN SERGİ GÜNÜ!",
                42f, TextAlignmentOptions.Center);
            titleTMP.fontStyle = FontStyles.Bold;
            PositionFromTop(titleTMP.rectTransform, 0f, 1f, 140f, 70f, 20f);

            // Alt metin — stok sayısı
            var subtitleTMP = NewTMP("SubtitleText", panelGO.transform,
                "Stokta 0 ürün var. Sergiyi açmak ister misin?",
                30f, TextAlignmentOptions.Center);
            subtitleTMP.color = new Color(0.80f, 0.80f, 0.85f, 1f);
            PositionFromTop(subtitleTMP.rectTransform, 0f, 1f, 220f, 80f, 20f);

            // Düğme satırı — iki düğme yan yana
            var openBtn = BuildButton("OpenButton", panelGO.transform,
                "SERGİYİ AÇ", OpenColor,
                new Vector2(0.05f, 0f), new Vector2(0.50f, 0f), 360f);
            var skipBtn = BuildButton("SkipButton", panelGO.transform,
                "GEÇ", SkipColor,
                new Vector2(0.52f, 0f), new Vector2(0.95f, 0f), 360f);

            // SerializeField referansları
            panel        = panelGO;
            subtitleText = subtitleTMP;
            openButton   = openBtn;
            skipButton   = skipBtn;

            panelGO.SetActive(false);

            EditorUtility.SetDirty(this);
            Debug.Log("[ExhibitionNotificationPanel] Hierarchy built.");
        }

        // Altta sabitlenmiş eylem düğmesi; xMin/xMax parent'ın anchor aralığını belirler
        private static Button BuildButton(string name, Transform parent,
            string label, Color bg,
            Vector2 anchorMin, Vector2 anchorMax, float bottomOffset)
        {
            var go  = NewUIObject(name, parent);
            var img = go.AddComponent<Image>();
            img.color = bg;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.offsetMin        = new Vector2(0f,  bottomOffset);
            rt.offsetMax        = new Vector2(0f,  bottomOffset + 90f);

            var tmp = NewTMP("Label", go.transform, label, 34f, TextAlignmentOptions.Center);
            tmp.fontStyle = FontStyles.Bold;
            Stretch(tmp.rectTransform);
            return btn;
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
            var go  = NewUIObject(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text          = text;
            tmp.fontSize      = fontSize;
            tmp.color         = Color.white;
            tmp.alignment     = align;
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
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2( padH, -topOffset - height);
            rt.offsetMax = new Vector2(-padH, -topOffset);
        }
#endif
    }
}
