#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Freeline
{
    // Telefon içi webtoon yönetim ekranı — ZenitoonPanel / MarketPanel ile aynı overlay deseni.
    public class WebtoonPanel : MonoBehaviour
    {
        // ---- Sabitler -------------------------------------------------------
        private const float HeaderH         = 80f;
        private const float StatsBlockH     = 200f;
        private const float BottomBarH      = 80f;
        private const float ProduceBtnH     = 140f;
        private const float FeedbackH       = 60f;
        private const float StatRowH        = 40f;
        private const float TitleFontSize   = 36f;
        private const float StatFontSize    = 26f;
        private const float ProduceFontSize = 32f;
        private const float CostFontSize    = 22f;
        private const float FeedbackFontSize = 26f;
        private const float BackFontSize    = 28f;

        // ProduceBtn pivot'u ActionBlock dikey merkezinin bu kadar üstünde
        private const float ProduceBtnOffsetY = 40f;

        // ---- SerializeField refs --------------------------------------------
        [SerializeField] private TextMeshProUGUI followerText;
        [SerializeField] private TextMeshProUGUI chapterText;
        [SerializeField] private TextMeshProUGUI dailyIncomeText;
        [SerializeField] private TextMeshProUGUI lastChapterText;
        [SerializeField] private Button          produceBtn;
        [SerializeField] private TextMeshProUGUI produceCostText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI cooldownText;
        [SerializeField] private Button          backBtn;

        private Coroutine _statusCoroutine;

        // =========================================================================
        // Runtime
        // =========================================================================

        void Awake()
        {
            if (backBtn    != null) backBtn.onClick.AddListener(Close);
            if (produceBtn != null) produceBtn.onClick.AddListener(OnProduceClicked);
        }

        void OnEnable()
        {
            var wm = GameManager.Instance?.WebtoonManager;
            if (wm != null) wm.OnChapterPublished += HandleChapterPublished;
        }

        void OnDisable()
        {
            var wm = GameManager.Instance?.WebtoonManager;
            if (wm != null) wm.OnChapterPublished -= HandleChapterPublished;
        }

        public void Open()
        {
            gameObject.SetActive(true);
            RefreshStats();
        }

        // Eski çağrı noktalarıyla (DrawMenuPanel, DrawingMinigame) uyumluluk için
        public void Show() => Open();

        public void Close() => gameObject.SetActive(false);

        public void RefreshStats()
        {
            var gm = GameManager.Instance;
            if (gm?.SaveManager?.CurrentData == null) return;

            var wt = gm.SaveManager.CurrentData.webtoonData;
            var wm = gm.WebtoonManager;

            if (followerText    != null)
                followerText.text    = $"Takipci: {Mathf.RoundToInt(wt.followers)}";
            if (chapterText     != null)
                chapterText.text     = $"Bolum: {wt.totalChaptersPublished}";
            if (dailyIncomeText != null)
                dailyIncomeText.text = $"Gunluk Gelir: {Mathf.RoundToInt(wm.GetDailyPassiveIncome())} coin";
            if (lastChapterText != null)
            {
                int days = Mathf.FloorToInt(wt.daysSinceLastChapter);
                lastChapterText.text = days == 0
                    ? "Son Bolum: bugun"
                    : $"Son Bolum: {days} gun once";
            }

            bool canAfford = gm.EnergyManager.CurrentEnergy >= wm.ChapterEnergyCost;

            if (produceBtn != null)
                produceBtn.interactable = canAfford;
            if (produceCostText != null)
                produceCostText.text =
                    $"Sure: {wm.ChapterProductionHours:0.#} saat  |  Enerji: {Mathf.RoundToInt(wm.ChapterEnergyCost)}";
            if (cooldownText != null)
            {
                cooldownText.gameObject.SetActive(!canAfford);
                if (!canAfford)
                    cooldownText.text = "Enerji yetersiz. Dinlen veya yemek ye.";
            }
        }

        private void OnProduceClicked()
        {
            var wm = GameManager.Instance?.WebtoonManager;
            if (wm == null) return;

            bool success = wm.ProduceChapter();
            if (!success)
                ShowStatus("Enerji yetersiz!", new Color(1f, 0.4f, 0.4f, 1f), 2f);
            // Başarılıysa HandleChapterPublished event aracılığıyla gelir
        }

        private void HandleChapterPublished(int chapterNum, float followerGain, bool wasViral)
        {
            string msg = wasViral
                ? $"Viral oldu! +{Mathf.RoundToInt(followerGain)} takipci!"
                : $"Bolum yayinlandi! +{Mathf.RoundToInt(followerGain)} takipci";
            Color col = wasViral
                ? new Color(1f, 0.85f, 0.2f, 1f)
                : new Color(0.4f, 1f, 0.5f, 1f);
            ShowStatus(msg, col, 2f);
            RefreshStats();
        }

        private void ShowStatus(string message, Color color, float duration)
        {
            if (statusText == null) return;
            if (_statusCoroutine != null) StopCoroutine(_statusCoroutine);
            _statusCoroutine = StartCoroutine(StatusFade(message, color, duration));
        }

        private IEnumerator StatusFade(string message, Color color, float duration)
        {
            statusText.text  = message;
            statusText.color = color;
            statusText.gameObject.SetActive(true);

            yield return new WaitForSeconds(duration);

            if (statusText != null)
                statusText.gameObject.SetActive(false);
            _statusCoroutine = null;
        }

        // =========================================================================
        // EDITOR — hiyerarşi kurucusu
        // =========================================================================

#if UNITY_EDITOR

        [ContextMenu("Build Webtoon Hierarchy")]
        private void BuildWebtoonHierarchy()
        {
            while (transform.childCount > 0)
                DestroyImmediate(transform.GetChild(0).gameObject);

            var selfRT       = GetComponent<RectTransform>();
            selfRT.anchorMin = Vector2.zero;
            selfRT.anchorMax = Vector2.one;
            selfRT.offsetMin = Vector2.zero;
            selfRT.offsetMax = Vector2.zero;

            var selfImg   = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            selfImg.color = new Color(0.10f, 0.10f, 0.14f, 1f);

            BuildHeader();
            BuildStatsBlock();
            BuildActionBlock();
            BuildBottomBar();

            gameObject.SetActive(false);

            EditorUtility.SetDirty(gameObject);
            Debug.Log("[WebtoonPanel] Hierarchy built.");
        }

        // ---- Header -----------------------------------------------------------

        private void BuildHeader()
        {
            var go = NewUIObj("Header", transform);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -HeaderH);
            rt.offsetMax = Vector2.zero;

            var img   = go.AddComponent<Image>();
            img.color = new Color(0.98f, 0.72f, 0.35f, 1f);

            var titleGO = NewUIObj("TitleText", go.transform);
            var titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = Vector2.zero;
            titleRT.anchorMax = Vector2.one;
            titleRT.offsetMin = new Vector2(20f, 0f);
            titleRT.offsetMax = Vector2.zero;

            var tmp = titleGO.AddComponent<TextMeshProUGUI>();
            tmp.text          = "Webtoon Studyo";
            tmp.fontSize      = TitleFontSize;
            tmp.fontStyle     = FontStyles.Bold;
            tmp.color         = new Color(0.20f, 0.10f, 0.02f, 1f);
            tmp.alignment     = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
        }

        // ---- StatsBlock -------------------------------------------------------

        private void BuildStatsBlock()
        {
            var go = NewUIObj("StatsBlock", transform);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -(HeaderH + StatsBlockH));
            rt.offsetMax = new Vector2(0f, -HeaderH);

            var img   = go.AddComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.16f, 1f);

            var vlg                    = go.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = false;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing                = 0f;
            vlg.padding                = new RectOffset(20, 20, 16, 16);
            vlg.childAlignment         = TextAnchor.UpperLeft;

            followerText    = BuildStatRow("FollowerRow",    go.transform, "Takipci: 0");
            chapterText     = BuildStatRow("ChapterRow",     go.transform, "Bolum: 0");
            dailyIncomeText = BuildStatRow("DailyIncomeRow", go.transform, "Gunluk Gelir: 0 coin");
            lastChapterText = BuildStatRow("LastChapterRow", go.transform, "Son Bolum: hic yayinlanmadi");
        }

        private TextMeshProUGUI BuildStatRow(string name, Transform parent, string text)
        {
            var go = NewUIObj(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, StatRowH);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text          = text;
            tmp.fontSize      = StatFontSize;
            tmp.color         = Color.white;
            tmp.alignment     = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            return tmp;
        }

        // ---- ActionBlock ------------------------------------------------------

        private void BuildActionBlock()
        {
            var go = NewUIObj("ActionBlock", transform);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(0f,  BottomBarH);
            rt.offsetMax = new Vector2(0f, -(HeaderH + StatsBlockH));

            var img   = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);

            BuildProduceBtn(go.transform);
            BuildStatusText(go.transform);
            BuildCooldownText(go.transform);
        }

        private void BuildProduceBtn(Transform parent)
        {
            var go = NewUIObj("ProduceBtn", parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.1f, 0.5f);
            rt.anchorMax        = new Vector2(0.9f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(0f, ProduceBtnH);
            rt.anchoredPosition = new Vector2(0f, ProduceBtnOffsetY);

            var img   = go.AddComponent<Image>();
            img.color = new Color(0.20f, 0.55f, 0.85f, 1f);

            produceBtn = go.AddComponent<Button>();

            // Ana etiket — üst %55
            var labelGO = NewUIObj("Label", go.transform);
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0f, 0.45f);
            labelRT.anchorMax = new Vector2(1f, 1f);
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;

            var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text          = "Yeni Bolum Uret";
            labelTMP.fontSize      = ProduceFontSize;
            labelTMP.fontStyle     = FontStyles.Bold;
            labelTMP.color         = Color.white;
            labelTMP.alignment     = TextAlignmentOptions.Bottom;
            labelTMP.raycastTarget = false;

            // Maliyet alt etiketi — alt %45
            var costGO = NewUIObj("CostText", go.transform);
            var costRT = costGO.GetComponent<RectTransform>();
            costRT.anchorMin = new Vector2(0f, 0f);
            costRT.anchorMax = new Vector2(1f, 0.45f);
            costRT.offsetMin = Vector2.zero;
            costRT.offsetMax = Vector2.zero;

            produceCostText           = costGO.AddComponent<TextMeshProUGUI>();
            produceCostText.text      = "Sure: 4 saat  |  Enerji: 30";
            produceCostText.fontSize  = CostFontSize;
            produceCostText.color     = new Color(0.85f, 0.92f, 1f, 1f);
            produceCostText.alignment = TextAlignmentOptions.Top;
            produceCostText.raycastTarget = false;
        }

        private void BuildStatusText(Transform parent)
        {
            float centerY = ProduceBtnOffsetY - ProduceBtnH * 0.5f - 12f - FeedbackH * 0.5f;

            var go = NewUIObj("StatusText", parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.05f, 0.5f);
            rt.anchorMax        = new Vector2(0.95f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(0f, FeedbackH);
            rt.anchoredPosition = new Vector2(0f, centerY);

            statusText           = go.AddComponent<TextMeshProUGUI>();
            statusText.text      = "";
            statusText.fontSize  = FeedbackFontSize;
            statusText.color     = new Color(0.4f, 1f, 0.5f, 1f);
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.raycastTarget = false;

            go.SetActive(false);
        }

        private void BuildCooldownText(Transform parent)
        {
            float statusCenterY  = ProduceBtnOffsetY - ProduceBtnH * 0.5f - 12f - FeedbackH * 0.5f;
            float cooldownCenterY = statusCenterY - FeedbackH - 8f;

            var go = NewUIObj("CooldownText", parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.05f, 0.5f);
            rt.anchorMax        = new Vector2(0.95f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(0f, FeedbackH);
            rt.anchoredPosition = new Vector2(0f, cooldownCenterY);

            cooldownText           = go.AddComponent<TextMeshProUGUI>();
            cooldownText.text      = "Enerji yetersiz. Dinlen veya yemek ye.";
            cooldownText.fontSize  = FeedbackFontSize - 2f;
            cooldownText.color     = new Color(1f, 0.5f, 0.4f, 1f);
            cooldownText.alignment = TextAlignmentOptions.Center;
            cooldownText.raycastTarget = false;

            go.SetActive(false);
        }

        // ---- BottomBar --------------------------------------------------------

        private void BuildBottomBar()
        {
            var go = NewUIObj("BottomBar", transform);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = new Vector2(0f, BottomBarH);

            var img   = go.AddComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.16f, 1f);

            var btnGO = NewUIObj("BackBtn", go.transform);
            var btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.anchorMin        = new Vector2(0.5f, 0.5f);
            btnRT.anchorMax        = new Vector2(0.5f, 0.5f);
            btnRT.pivot            = new Vector2(0.5f, 0.5f);
            btnRT.sizeDelta        = new Vector2(240f, BottomBarH - 20f);
            btnRT.anchoredPosition = Vector2.zero;

            var btnImg   = btnGO.AddComponent<Image>();
            btnImg.color = new Color(0.35f, 0.20f, 0.20f, 1f);

            backBtn = btnGO.AddComponent<Button>();

            var labelGO = NewUIObj("Label", btnGO.transform);
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;

            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text          = "<- Geri";
            tmp.fontSize      = BackFontSize;
            tmp.fontStyle     = FontStyles.Bold;
            tmp.color         = Color.white;
            tmp.alignment     = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }

        private static GameObject NewUIObj(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

#endif
    }
}
