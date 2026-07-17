#if UNITY_EDITOR
using UnityEditor;
#endif

using TMPro;
using UnityEngine;
using UnityEngine.UI;



namespace Freeline
{
    
    // Sergi bitiminde oyuncuya günün özetini (satış adedi ve kazanç) gösteren panel
    public class ExhibitionSummaryPanel : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private ExhibitionSalesPanel salesPanel;

        [Header("Panel Root")]
        [SerializeField] private GameObject panel;

        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI summaryText;

        [Header("Buttons")]
        [SerializeField] private Button okButton; // TAMAM

        private static readonly Color PanelBg  = new Color(0.10f, 0.10f, 0.15f, 0.97f);
        private static readonly Color OkColor  = new Color(0.15f, 0.55f, 0.25f, 1.00f);

        void Awake()
        {
            if (okButton != null) okButton.onClick.AddListener(OnOkClicked);
            Hide(); // Başlangıçta paneli ve karartmayı kapatır
        }

        private void OnOkClicked()
        {
            Hide();
            if (salesPanel != null) salesPanel.Hide(); // Sergi satış panelini de kapat
        }

        void Start()
        {
            var em = GameManager.Instance?.ExhibitionManager;
            if (em != null)
            {
                em.OnExhibitionEnded += HandleExhibitionEnded;
            }
        }

        void OnDestroy()
        {
            if (GameManager.Instance == null) return;
            var em = GameManager.Instance.ExhibitionManager;
            if (em != null)
            {
                em.OnExhibitionEnded -= HandleExhibitionEnded;
            }
        }

        private void HandleExhibitionEnded(int itemsSold, int totalEarned)
        {
            if (summaryText != null)
            {
                if (itemsSold > 0)
                    summaryText.text = $"Harika bir gün! Toplam <color=#00FF00>{itemsSold}</color> ürün sattın ve <color=#FFD700>{totalEarned}</color> coin kazandın.";
                else
                    summaryText.text = "Bugün hiç ürün satamadın. Belki bir dahaki sefere daha şanslı olursun...";
            }
            
            Show();
        }

        public void Show()
        {
            if (panel != null) panel.SetActive(true);
            Transform blocker = transform.Find("Blocker");
            if (blocker != null) blocker.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
            Transform blocker = transform.Find("Blocker");
            if (blocker != null) blocker.gameObject.SetActive(false);
        }

        // =========================================================================
        // EDITOR — Hierarchy builder
        // Right-click this component in the Inspector → "Build Exhibition Summary Hierarchy"
        // =========================================================================

#if UNITY_EDITOR
        [ContextMenu("Build Exhibition Summary Hierarchy")]
        private void BuildExhibitionSummaryHierarchy()
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

            // "GÜN BİTTİ" — büyük başlık alanı
            var badge = NewTMP("BadgeText", panelGO.transform, "SERGİ BİTTİ",
                60f, TextAlignmentOptions.Center);
            badge.fontStyle = FontStyles.Bold;
            badge.color     = new Color(1f, 0.85f, 0.2f, 1f);
            PositionFromTop(badge.rectTransform, 0f, 1f, 40f, 80f, 20f);

            // Alt metin — Özet bilgisi
            var summaryTMP = NewTMP("SummaryText", panelGO.transform,
                "Harika bir gün! Toplam 5 ürün sattın ve 1200 coin kazandın.",
                32f, TextAlignmentOptions.Center);
            summaryTMP.color = new Color(0.80f, 0.80f, 0.85f, 1f);
            PositionFromTop(summaryTMP.rectTransform, 0f, 1f, 160f, 120f, 40f);
            summaryTMP.enableWordWrapping = true;

            // TAMAM düğmesi (Ortalanmış)
            var okBtn = BuildButton("OkButton", panelGO.transform,
                "TAMAM", OkColor,
                new Vector2(0.2f, 0f), new Vector2(0.8f, 0f), 40f);

            // SerializeField referansları
            panel       = panelGO;
            summaryText = summaryTMP;
            okButton    = okBtn;

            panelGO.SetActive(false);

            EditorUtility.SetDirty(this);
            Debug.Log("[ExhibitionSummaryPanel] Hierarchy built.");
        }

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