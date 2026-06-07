#if UNITY_EDITOR
using UnityEditor;
#endif

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Freeline
{
    // Webtoon durumunu gösteren ve bölüm üretimini başlatan panel.
    public class WebtoonPanel : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Inspector references — populated by [Build Webtoon Panel Hierarchy] or manually
        // -------------------------------------------------------------------------

        [Header("Close")]
        [SerializeField] private Button closeButton;

        [Header("Stats")]
        [SerializeField] private TextMeshProUGUI followerText;
        [SerializeField] private TextMeshProUGUI dailyIncomeText;
        [SerializeField] private TextMeshProUGUI chaptersText;
        [SerializeField] private TextMeshProUGUI daysSinceText;

        [Header("Actions")]
        [SerializeField] private Button produceButton;
        [SerializeField] private Button backButton;

        [Header("Navigation")]
        [SerializeField] private DrawMenuPanel  drawMenuPanel;
        [SerializeField] private DrawingMinigame drawingMinigame;

        [Header("Status")]
        [SerializeField] private TextMeshProUGUI statusText;

        // -------------------------------------------------------------------------
        // Colors
        // -------------------------------------------------------------------------

        private static readonly Color PanelBg   = new Color(0.08f, 0.08f, 0.12f, 0.96f);
        private static readonly Color CloseBtn   = new Color(0.45f, 0.12f, 0.12f, 1.00f);
        private static readonly Color ProduceOn  = new Color(0.15f, 0.55f, 0.25f, 1.00f);
        private static readonly Color ProduceOff = new Color(0.22f, 0.22f, 0.28f, 1.00f);
        private static readonly Color BackBtn    = new Color(0.18f, 0.18f, 0.30f, 1.00f);
        private static readonly Color StatColor  = new Color(0.85f, 0.85f, 0.95f, 1.00f);
        private static readonly Color ViralColor = new Color(1.00f, 0.85f, 0.10f, 1.00f);
        private static readonly Color GainColor  = new Color(0.20f, 0.85f, 0.30f, 1.00f);
        private static readonly Color LossColor  = new Color(0.95f, 0.25f, 0.20f, 1.00f);

        // -------------------------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------------------------

        void Awake()
        {
            closeButton.onClick.AddListener(Hide);
            produceButton.onClick.AddListener(OnProduceClicked);
            backButton.onClick.AddListener(OnBackClicked);
        }

        void Start()
        {
            var gm = GameManager.Instance;
            gm.WebtoonManager.OnChapterPublished    += HandleChapterPublished;
            gm.WebtoonManager.OnFollowersChanged    += HandleFollowersChanged;
            gm.WebtoonManager.OnPassiveIncomeEarned += HandlePassiveIncome;
            gm.EnergyManager.OnEnergyChanged        += HandleEnergyChanged;
            Hide();
        }

        void OnDestroy()
        {
            if (GameManager.Instance == null) return;
            var gm = GameManager.Instance;
            gm.WebtoonManager.OnChapterPublished    -= HandleChapterPublished;
            gm.WebtoonManager.OnFollowersChanged    -= HandleFollowersChanged;
            gm.WebtoonManager.OnPassiveIncomeEarned -= HandlePassiveIncome;
            gm.EnergyManager.OnEnergyChanged        -= HandleEnergyChanged;
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        // DrawMenuPanel.OnWebtoonClicked tarafından çağrılır.
        public void Show()
        {
            gameObject.SetActive(true);
            statusText.text = string.Empty;
            RefreshStats();
        }

        public void Hide() => gameObject.SetActive(false);

        // -------------------------------------------------------------------------
        // Veri güncelleme
        // -------------------------------------------------------------------------

        // Tüm istatistik satırlarını ve buton durumunu yöneticilerden okuyarak senkronize eder.
        private void RefreshStats()
        {
            var wm = GameManager.Instance.WebtoonManager;
            var wt = GameManager.Instance.SaveManager.CurrentData.webtoonData;

            followerText.text    = $"Takipçi: {Mathf.FloorToInt(wt.followers)}";
            dailyIncomeText.text = $"Günlük Gelir: {Mathf.FloorToInt(wm.GetDailyPassiveIncome())} coin";
            chaptersText.text    = $"Yayınlanan Bölüm: {wt.totalChaptersPublished}";
            daysSinceText.text   = $"Son Bölümden Bu Yana: {Mathf.FloorToInt(wt.daysSinceLastChapter)} gün";

            RefreshProduceButton();
        }

        // Mevcut enerji ile bölüm maliyeti karşılaştırılarak buton etkinleştirilir ya da devre dışı bırakılır.
        private void RefreshProduceButton()
        {
            bool can = GameManager.Instance.EnergyManager.CurrentEnergy
                       >= GameManager.Instance.WebtoonManager.ChapterEnergyCost;
            produceButton.interactable                    = can;
            produceButton.GetComponent<Image>().color     = can ? ProduceOn : ProduceOff;
        }

        // -------------------------------------------------------------------------
        // Düğme işleyiciler
        // -------------------------------------------------------------------------

        private void OnProduceClicked()
        {
            // Doğrudan ProduceChapter() çağrılmaz; mini-oyun tamamlandığında çağrılır.
            Hide();
            drawingMinigame?.ShowForWebtoon();
        }

        private void OnBackClicked()
        {
            Hide();
            drawMenuPanel?.Show();
        }

        // -------------------------------------------------------------------------
        // Event işleyiciler
        // -------------------------------------------------------------------------

        private void HandleChapterPublished(int chapter, float gain, bool viral)
        {
            RefreshStats();
            statusText.color = viral ? ViralColor : GainColor;
            statusText.text  = viral
                ? $"Viral oldu! +{Mathf.FloorToInt(gain)} takipçi kazandın!"
                : $"Bölüm {chapter} yayınlandı. +{Mathf.FloorToInt(gain)} takipçi.";
        }

        // Çürüme de bu event'i tetikler; her iki durum için takipçi satırı güncellenir.
        private void HandleFollowersChanged(float followers, float delta)
        {
            if (!gameObject.activeSelf) return;
            followerText.text = $"Takipçi: {Mathf.FloorToInt(followers)}";
            if (delta < 0f)
            {
                statusText.text  = $"Takipçi kaybı: {Mathf.FloorToInt(delta)} (yeni bölüm yayınla!)";
                statusText.color = LossColor;
            }
        }

        private void HandlePassiveIncome(float amount)
        {
            if (!gameObject.activeSelf) return;
            // Günlük gelir takipçi sayısından türetilir; saklanan amount değil hesaplanan oran kullanılır.
            dailyIncomeText.text = $"Günlük Gelir: {Mathf.FloorToInt(GameManager.Instance.WebtoonManager.GetDailyPassiveIncome())} coin";
        }

        private void HandleEnergyChanged(float current, float max)
        {
            if (!gameObject.activeSelf) return;
            RefreshProduceButton();
        }

        // =========================================================================
        // EDITOR — Hierarchy builder
        // Right-click this component in the Inspector → "Build Webtoon Panel Hierarchy"
        // Destroys existing children, rebuilds the full hierarchy, and populates
        // every SerializeField reference automatically.
        //
        // Layout (reference resolution 1080 × 1920):
        //   WebtoonPanel (full-screen RectTransform)
        //     Blocker       — full-screen dark overlay
        //     Panel         — 900 × 1050, centered
        //       CloseButton   — 90 × 90, top-right
        //       HeaderText    — full width, 100 px, 24 px from top
        //       Divider       — 2 px at 134 px from top
        //       FollowerText  — stat row, 154 px from top
        //       DailyIncome   — stat row, 230 px from top
        //       ChaptersText  — stat row, 306 px from top
        //       DaysSinceText — stat row, 382 px from top
        //       Divider2      — 2 px at 458 px from top
        //       ProduceButton — 700 × 110, 478 px from top
        //       BackButton    — 700 × 80,  608 px from top
        //       StatusText    — full width, 120 px, 708 px from top
        // =========================================================================

#if UNITY_EDITOR
        [ContextMenu("Build Webtoon Panel Hierarchy")]
        private void BuildWebtoonPanelHierarchy()
        {
            // Eski çocuk nesneleri temizle; temiz yeniden inşa için gerekli.
            while (transform.childCount > 0)
                DestroyImmediate(transform.GetChild(0).gameObject);

            Stretch(GetComponent<RectTransform>());

            // Tam ekran karartma — panel arkasındaki dokunuşları yakalar.
            var blocker = NewUIObject("Blocker", transform);
            blocker.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            Stretch(blocker.GetComponent<RectTransform>());

            // Ortalanmış panel — 900 × 1050.
            var panel = NewUIObject("Panel", transform);
            panel.AddComponent<Image>().color = PanelBg;
            var panelRT = panel.GetComponent<RectTransform>();
            panelRT.anchorMin        = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax        = new Vector2(0.5f, 0.5f);
            panelRT.pivot            = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta        = new Vector2(900f, 1050f);
            panelRT.anchoredPosition = Vector2.zero;

            // Kapat düğmesi (X) — sağ üst köşe, 90 × 90.
            closeButton = BuildIconButton("CloseButton", panel.transform, "X", CloseBtn);
            var closeRT = closeButton.GetComponent<RectTransform>();
            closeRT.anchorMin        = new Vector2(1f, 1f);
            closeRT.anchorMax        = new Vector2(1f, 1f);
            closeRT.pivot            = new Vector2(1f, 1f);
            closeRT.sizeDelta        = new Vector2(90f, 90f);
            closeRT.anchoredPosition = new Vector2(-16f, -16f);

            // Başlık — "WEBTOON", kalın, panelin üstünden 24 px.
            var header = NewTMP("HeaderText", panel.transform, "WEBTOON", 60f, TextAlignmentOptions.Center);
            header.fontStyle = FontStyles.Bold;
            PositionFromTop(header.rectTransform, 0f, 1f, 24f, 100f, 0f);

            // Ayırıcı çizgi 1.
            BuildDivider("Divider", panel.transform, 134f);

            // İstatistik satırları — 56 px yükseklik, 20 px boşlukla.
            const float StatH = 56f;
            const float StatGap = 20f;
            float statY = 154f;

            followerText    = BuildStatRow("FollowerText",    panel.transform, "Takipçi: 0",                statY); statY += StatH + StatGap;
            dailyIncomeText = BuildStatRow("DailyIncomeText", panel.transform, "Günlük Gelir: 0 coin",      statY); statY += StatH + StatGap;
            chaptersText    = BuildStatRow("ChaptersText",    panel.transform, "Yayınlanan Bölüm: 0",       statY); statY += StatH + StatGap;
            daysSinceText   = BuildStatRow("DaysSinceText",   panel.transform, "Son Bölümden Bu Yana: 0 gün", statY);

            // Ayırıcı çizgi 2.
            BuildDivider("Divider2", panel.transform, 458f);

            // BÖLÜM ÇİZ düğmesi — 700 × 110, panelin üstünden 478 px.
            produceButton = BuildActionButton("ProduceButton", panel.transform,
                                              "BÖLÜM ÇİZ", ProduceOn, 700f, 110f, 478f, 50f);

            // GERİ düğmesi — 700 × 80, panelin üstünden 608 px.
            backButton = BuildActionButton("BackButton", panel.transform,
                                           "GERİ", BackBtn, 700f, 80f, 608f, 36f);

            // Durum mesajı — tam genişlik, 120 px, panelin üstünden 708 px.
            statusText = NewTMP("StatusText", panel.transform, string.Empty, 32f, TextAlignmentOptions.Center);
            statusText.color = GainColor;
            PositionFromTop(statusText.rectTransform, 0f, 1f, 708f, 120f, 20f);

            EditorUtility.SetDirty(gameObject);
            Debug.Log("[WebtoonPanel] Hierarchy built and SerializeField references populated.");
        }

        // İstatistik satırı — sola hizalı, 40 px yatay dolgu.
        private TextMeshProUGUI BuildStatRow(string name, Transform parent,
                                             string placeholder, float topOffset)
        {
            var tmp = NewTMP(name, parent, placeholder, 36f, TextAlignmentOptions.Left);
            tmp.color = StatColor;
            PositionFromTop(tmp.rectTransform, 0f, 1f, topOffset, 56f, 40f);
            return tmp;
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
