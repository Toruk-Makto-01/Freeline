#if UNITY_EDITOR
using UnityEditor;
#endif

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Freeline
{
    // Tablet ikonuna basınca açılan 3 seçenekli çizim menüsü.
    // Freelance, Webtoon ve Sergi Ürünü seçeneklerini sunar.
    public class DrawMenuPanel : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Inspector references — populated by [Build Draw Menu Hierarchy] or manually
        // -------------------------------------------------------------------------

        [Header("Buttons")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button freelanceButton;
        [SerializeField] private Button webtoonButton;

        [Header("Panels")]
        [SerializeField] private JobBoardPanel  jobBoardPanel;
        [SerializeField] private WebtoonPanel   webtoonPanel;

        // Blocker ve Panel doğrudan çocukları; builder yeniden çalıştırılmasa da
        // isimle bulunur. Root GO asla SetActive(false) yapılmaz; böylece bu nesnenin
        // altına sahne içinde yerleştirilen başka paneller etkilenmez.
        private Transform _blocker;
        private Transform _panelRoot;

        // -------------------------------------------------------------------------
        // Colors
        // -------------------------------------------------------------------------

        private static readonly Color OverlayBg  = new Color(0.00f, 0.00f, 0.00f, 0.55f);
        private static readonly Color PanelBg    = new Color(0.08f, 0.08f, 0.12f, 0.96f);
        private static readonly Color CloseBtn   = new Color(0.45f, 0.12f, 0.12f, 1.00f);
        private static readonly Color MenuBtn    = new Color(0.13f, 0.22f, 0.40f, 1.00f);
        private static readonly Color MenuBtnHov = new Color(0.20f, 0.35f, 0.60f, 1.00f);

        // -------------------------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------------------------

        void Awake()
        {
            _blocker   = transform.Find("Blocker");
            _panelRoot = transform.Find("Panel");

            closeButton.onClick.AddListener(Hide);
            freelanceButton.onClick.AddListener(OnFreelanceClicked);
            webtoonButton.onClick.AddListener(OnWebtoonClicked);
        }

        void Start()
        {
            Hide();
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        // HUDManager'daki Draw düğmesine basıldığında çağrılır.
        public void Show()
        {
            _blocker?.gameObject.SetActive(true);
            _panelRoot?.gameObject.SetActive(true);
        }

        public void Hide()
        {
            // Root GO aktif kalır; yalnızca görsel çocuklar gizlenir.
            // Root devre dışı bırakılsaydı, sahne içinde bu nesnenin altına
            // konumlandırılmış JobBoardPanel gibi çocuklar da gizlenirdi.
            _blocker?.gameObject.SetActive(false);
            _panelRoot?.gameObject.SetActive(false);
        }

        // -------------------------------------------------------------------------
        // Düğme işleyiciler
        // -------------------------------------------------------------------------

        private void OnFreelanceClicked()
        {
            // JobBoardPanel önce açılır; sonra bu menü gizlenir.
            // Ters sırada, JobBoardPanel bu nesnenin çocuğu olduğu durumda
            // Hide() onu da etkilerdi.
            jobBoardPanel?.Show();
            Hide();
        }

        private void OnWebtoonClicked()
        {
            webtoonPanel?.Show();
            Hide();
        }


        // =========================================================================
        // EDITOR — Hierarchy builder
        // Right-click this component in the Inspector → "Build Draw Menu Hierarchy"
        // Destroys existing children, rebuilds the full hierarchy, and populates
        // every SerializeField reference automatically.
        //
        // Layout (reference resolution 1080 × 1920):
        //   DrawMenuPanel (full-screen RectTransform)
        //     Blocker      — full-screen dark overlay, intercepts background taps
        //     Panel        — 700 × 640, centered
        //       CloseButton  — 80 × 80, top-right
        //       HeaderText   — full width, 100 px, 20 px from top
        //       Divider      — 2 px separator at 130 px from top
        //       FreelanceButton  — 600 × 140, starts at 160 px from top
        //       WebtoonButton    — 600 × 140, 20 px below Freelance
        // =========================================================================

#if UNITY_EDITOR
        [ContextMenu("Build Draw Menu Hierarchy")]
        private void BuildDrawMenuHierarchy()
        {
            // Eski çocuk nesneleri temizle; temiz yeniden inşa için gerekli.
            while (transform.childCount > 0)
                DestroyImmediate(transform.GetChild(0).gameObject);

            Stretch(GetComponent<RectTransform>());

            // Tam ekran karartma — panel arkasındaki dokunuşları yakalar.
            var blocker = NewUIObject("Blocker", transform);
            blocker.AddComponent<Image>().color = OverlayBg;
            Stretch(blocker.GetComponent<RectTransform>());

            // Ortalanmış panel — 700 × 640.
            var panel = NewUIObject("Panel", transform);
            panel.AddComponent<Image>().color = PanelBg;
            var panelRT = panel.GetComponent<RectTransform>();
            panelRT.anchorMin        = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax        = new Vector2(0.5f, 0.5f);
            panelRT.pivot            = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta        = new Vector2(700f, 640f);
            panelRT.anchoredPosition = Vector2.zero;

            // Kapat düğmesi (✕) — sağ üst köşe, 80 × 80.
            closeButton = BuildIconButton("CloseButton", panel.transform, "X", CloseBtn);
            var closeRT = closeButton.GetComponent<RectTransform>();
            closeRT.anchorMin        = new Vector2(1f, 1f);
            closeRT.anchorMax        = new Vector2(1f, 1f);
            closeRT.pivot            = new Vector2(1f, 1f);
            closeRT.sizeDelta        = new Vector2(80f, 80f);
            closeRT.anchoredPosition = new Vector2(-16f, -16f);

            // Başlık — "ÇİZ", kalın, panelin üstünden 20 px.
            var header = NewTMP("HeaderText", panel.transform, "ÇİZ", 72f, TextAlignmentOptions.Center);
            header.fontStyle = FontStyles.Bold;
            PositionFromTop(header.rectTransform, 0f, 1f, 20f, 100f, 0f);

            // Ayırıcı çizgi — 130 px, 2 px yükseklik.
            var divider = NewUIObject("Divider", panel.transform);
            divider.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
            var divRT = divider.GetComponent<RectTransform>();
            divRT.anchorMin = new Vector2(0.04f, 1f);
            divRT.anchorMax = new Vector2(0.96f, 1f);
            divRT.pivot     = new Vector2(0.5f,  1f);
            divRT.offsetMin = new Vector2(0f, -132f);
            divRT.offsetMax = new Vector2(0f, -130f);

            // 3 büyük menü düğmesi — 600 × 140, 20 px dikey boşlukla.
            const float BtnH  = 140f;
            const float GapV  =  20f;
            const float StartY = -160f;

            freelanceButton  = BuildMenuButton("FreelanceButton",  panel.transform, "FREELANCE",   StartY);
            webtoonButton    = BuildMenuButton("WebtoonButton",    panel.transform, "WEBTOON",     StartY - (BtnH + GapV));

            EditorUtility.SetDirty(gameObject);
            Debug.Log("[DrawMenu] Hierarchy built and SerializeField references populated.");
        }

        // Büyük menü seçenek düğmesi — 600 × 140, panelin üst kenarından anchoredY.
        private static Button BuildMenuButton(string name, Transform parent,
                                              string label, float anchoredY)
        {
            var go  = NewUIObject(name, parent);
            var img = go.AddComponent<Image>();
            img.color = MenuBtn;

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
            rt.sizeDelta        = new Vector2(600f, 140f);
            rt.anchoredPosition = new Vector2(0f, anchoredY);

            var tmp = NewTMP("Label", go.transform, label, 44f, TextAlignmentOptions.Center);
            tmp.fontStyle = FontStyles.Bold;
            Stretch(tmp.rectTransform);

            return btn;
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
