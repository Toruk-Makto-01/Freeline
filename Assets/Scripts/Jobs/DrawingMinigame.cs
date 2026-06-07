using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

namespace Freeline
{
    /// <summary>
    /// Seviye 1 çizim mini-oyunu: kaydırma çubuğu (SlideBar) mekaniği.
    /// Oyuncu parmağını basılı tuttuğu sürece çubuk dolar; bırakınca yavaşça boşalır.
    /// Çubuk %100'e ulaştığında iş tamamlanır; İptal düğmesi işi terk eder.
    /// Yalnızca <see cref="JobType.SlideBar"/> türündeki işler için etkinleşir.
    /// </summary>
    public class DrawingMinigame : MonoBehaviour
    {
        public enum MinigameMode { Freelance, Webtoon, Exhibition }

        // Her çağrıda GameManager üzerinden erişilir; yerel referans saklanmaz.
        private static JobManager JM => GameManager.Instance.JobManager;

        [Header("Panel Root")]
        [SerializeField] private GameObject panel;

        [Header("Info Labels")]
        [SerializeField] private TextMeshProUGUI jobTitleText;
        [SerializeField] private TextMeshProUGUI clientNameText;
        [SerializeField] private TextMeshProUGUI payoutText;
        [SerializeField] private TextMeshProUGUI timeCostText;

        [Header("Slide Bar")]
        [SerializeField] private Image barFill;

        [Header("Buttons")]
        [SerializeField] private Button cancelButton;

        [Header("Settings")]
        [SerializeField] private float fillRate  = 0.25f; // saniyede dolma oranı (1× hızda)
        [SerializeField] private float drainRate = 0f;    // Awake'te fillRate / 3 olarak hesaplanır

        private float        _progress;                 // 0–1 arası dolum ilerlemesi
        private bool         _active;                  // mini-oyun şu an çalışıyor mu
        private JobData      _job;                     // aktif iş verisi; Webtoon/Exhibition modunda null kalır
        private MinigameMode _mode;                    // tamamlandığında hangi yöneticinin çağrılacağını belirler
        private System.Action _onExhibitionComplete;   // Exhibition tamamlandığında ExhibitionPanel.OnProductionComplete çağrılır

        void Awake()
        {
            // Boşalma hızı dolma hızının 1/3'ü: bırakınca çubuk yavaş geri çekilir.
            drainRate = fillRate / 3f;
            if (panel != null) panel.SetActive(false);
        }

        void OnEnable()
        {
        }

        void OnDisable()
        {
            if (GameManager.Instance == null) return;
            JM.OnJobStarted   -= HandleJobStarted;
            JM.OnJobCompleted -= HandleJobEnded;
            JM.OnJobAbandoned -= HandleJobAbandoned;
        }

        void Start()
        {
            cancelButton.onClick.AddListener(OnCancelPressed);
            if (GameManager.Instance?.JobManager == null)
            {
                Debug.LogError("[Minigame] JobManager is null in Start — events not subscribed.");
                return;
            }
            JM.OnJobStarted   += HandleJobStarted;
            JM.OnJobCompleted += HandleJobEnded;
            JM.OnJobAbandoned += HandleJobAbandoned;
        }

        /// <summary>
        /// Mini-oyun aktifken her kareyi işler.
        /// Gerçek zamanlı dokunma girişi gerektiğinden Update kullanımı kasıtlıdır.
        /// </summary>
        void Update()
        {
            if (!_active) return;

            bool held = Pointer.current != null && Pointer.current.press.isPressed;

            // Yiyecek buff'ları dolma hızını artırabilir; GetCurrentSpeedMultiplier bunu sağlar.
            float speed  = GameManager.Instance.EnergyManager.GetCurrentSpeedMultiplier();
            float delta  = held
                ? fillRate * speed * Time.deltaTime
                : -drainRate * Time.deltaTime;

            _progress = Mathf.Clamp01(_progress + delta);
            if (barFill != null) barFill.fillAmount = _progress;

            if (_progress >= 1f)
                Finish();
        }

        /// <summary>
        /// JobManager'dan iş başladığında tetiklenir.
        /// Yalnızca SlideBar türündeki işler için paneli açar.
        /// </summary>
        private void HandleJobStarted(JobData job)
        {
            if (job.jobType != JobType.SlideBar) return;
            _mode     = MinigameMode.Freelance;
            _job      = job;
            _progress = 0f;
            _active   = true;
            PopulateInfo(job);
            panel.SetActive(true);
        }

        // ExhibitionPanel.OnProduceClicked tarafından çağrılır; tamamlandığında onComplete callback'i tetiklenir.
        public void ShowForExhibition(ExhibitionProductData product, System.Action onComplete = null)
        {
            _mode                   = MinigameMode.Exhibition;
            _job                    = null;
            _onExhibitionComplete   = onComplete;
            _progress               = 0f;
            _active                 = true;
            jobTitleText.text       = product.productName;
            clientNameText.text     = product.productType.ToString();
            payoutText.text         = $"{product.basePrice} coin";
            timeCostText.text       = $"{product.productionHours:F1}h";
            panel.SetActive(true);
        }

        // WebtoonPanel.OnProduceClicked tarafından çağrılır; JobManager event zincirini atlar.
        public void ShowForWebtoon()
        {
            _mode              = MinigameMode.Webtoon;
            _job               = null;
            _progress          = 0f;
            _active            = true;
            jobTitleText.text   = "Yeni Webtoon Bölümü";
            clientNameText.text = string.Empty;
            payoutText.text     = string.Empty;
            timeCostText.text   = string.Empty;
            panel.SetActive(true);
        }

        /// <summary>İş başarıyla tamamlandığında paneli gizler.</summary>
        private void HandleJobEnded(JobData job, float payout)
        {
            HidePanel();
        }

        /// <summary>İş terk edildiğinde paneli gizler.</summary>
        private void HandleJobAbandoned(JobData job)
        {
            HidePanel();
        }

        /// <summary>
        /// İptal düğmesine basıldığında işi terk eder.
        /// HidePanel, OnJobAbandoned event'i üzerinden çağrılır; burada çağrılmaz.
        /// </summary>
        private void OnCancelPressed()
        {
            if (!_active) return;
            JM.AbandonJob();
        }

        /// <summary>
        /// Çubuk dolduğunda çağrılır: aktif bayrağı sıfırlanır ve iş tamamlanır.
        /// HidePanel, OnJobCompleted event'i üzerinden çağrılır; burada çağrılmaz.
        /// </summary>
        private void Finish()
        {
            _active = false;
            if (_mode == MinigameMode.Webtoon)
            {
                GameManager.Instance.WebtoonManager.ProduceChapter();
                // Freelance modundaki gibi OnJobCompleted event zinciri yok; panel doğrudan gizlenir.
                HidePanel();
            }
            else if (_mode == MinigameMode.Exhibition)
            {
                // Stok artışı callback üzerinden ExhibitionPanel'e bildirilir; bu sınıf paneli tanımaz.
                _onExhibitionComplete?.Invoke();
                _onExhibitionComplete = null;
                HidePanel();
            }
            else
            {
                JM.CompleteJob();
                // HidePanel, OnJobCompleted event'i üzerinden HandleJobEnded'da çağrılır.
            }
        }

        /// <summary>İş bilgilerini panel etiketlerine yazar.</summary>
        private void PopulateInfo(JobData job)
        {
            jobTitleText.text   = job.jobTitle;
            clientNameText.text = job.clientName;
            payoutText.text     = $"{job.basePayout:N0} coins";
            timeCostText.text   = $"{job.durationHours:F1}h";
        }

        /// <summary>Mini-oyun durumunu sıfırlar ve paneli gizler.</summary>
        private void HidePanel()
        {
            _active = false;
            _job    = null;
            panel.SetActive(false);
        }

        // -------------------------------------------------------------------------
        // Editor helper — builds the full UI hierarchy under this GameObject.
        // Run once from the Inspector context menu, then wire the scene.
        // -------------------------------------------------------------------------

        /// <summary>
        /// Bu GameObject altında tam UI hiyerarşisini oluşturur ve tüm SerializeField
        /// referanslarını otomatik olarak doldurur.
        /// Inspector'da sağ tıklayıp bir kez çalıştırın; ardından sahneye wire edin.
        /// </summary>
        [ContextMenu("BuildMinigameHierarchy")]
        private void BuildMinigameHierarchy()
        {
#if UNITY_EDITOR
            // Kendi Canvas'ı oluşturulur; sortingOrder 20 ile HUD'ın (10) üzerinde kalır.
            // DrawingMinigame bu GO üzerinde durur; ayrı bir ebeveyn Canvas gerekmez.
            Canvas ownCanvas = gameObject.GetComponent<Canvas>();
            if (ownCanvas == null) ownCanvas = gameObject.AddComponent<Canvas>();
            ownCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            ownCanvas.sortingOrder = 20;

            UnityEngine.UI.GraphicRaycaster gr = gameObject.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (gr == null) gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            RectTransform selfRT = gameObject.GetComponent<RectTransform>();
            if (selfRT == null) selfRT = gameObject.AddComponent<RectTransform>();
            selfRT.anchorMin = Vector2.zero;
            selfRT.anchorMax = Vector2.one;
            selfRT.offsetMin = Vector2.zero;
            selfRT.offsetMax = Vector2.zero;

            // ── Panel root — tam ekran, koyu arka plan ────────────────────────
            GameObject panelGO = new GameObject("MinigamePanel");
            panelGO.transform.SetParent(transform, false);
            RectTransform panelRT = panelGO.AddComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;
            Image panelBg = panelGO.AddComponent<Image>();
            panelBg.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

            // ── İş başlığı ────────────────────────────────────────────────────
            GameObject titleGO = CreateLabel("JobTitleText", panelGO.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -160f), new Vector2(0f, -80f), 42, FontStyles.Bold, Color.white);

            // ── Müşteri adı ───────────────────────────────────────────────────
            GameObject clientGO = CreateLabel("ClientNameText", panelGO.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -240f), new Vector2(0f, -170f), 30, FontStyles.Normal, Color.white);

            // ── Ödeme / süre satırı ───────────────────────────────────────────
            Color lightGray = new Color(0.8f, 0.8f, 0.8f, 1f);
            GameObject payoutGO = CreateLabel("PayoutText", panelGO.transform,
                new Vector2(0f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -320f), new Vector2(0f, -250f), 28, FontStyles.Normal, lightGray);

            GameObject timeGO = CreateLabel("TimeCostText", panelGO.transform,
                new Vector2(0.5f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -320f), new Vector2(0f, -250f), 28, FontStyles.Normal, lightGray);

            // ── Çubuk arka planı — 800×60, panelin %40 yüksekliğinde ortalanmış ─
            GameObject barBgGO = new GameObject("BarBackground");
            barBgGO.transform.SetParent(panelGO.transform, false);
            RectTransform barBgRT = barBgGO.AddComponent<RectTransform>();
            barBgRT.anchorMin        = new Vector2(0.5f, 0.4f);
            barBgRT.anchorMax        = new Vector2(0.5f, 0.4f);
            barBgRT.sizeDelta        = new Vector2(800f, 60f);
            barBgRT.anchoredPosition = Vector2.zero;
            Image barBgImg = barBgGO.AddComponent<Image>();
            barBgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // ── Çubuk dolgusu — Image.Type.Filled, yatay, soldan sağa ──────────
            GameObject barFillGO = new GameObject("BarFill");
            barFillGO.transform.SetParent(barBgGO.transform, false);
            RectTransform barFillRT = barFillGO.AddComponent<RectTransform>();
            barFillRT.anchorMin = Vector2.zero;
            barFillRT.anchorMax = Vector2.one;
            barFillRT.offsetMin = Vector2.zero;
            barFillRT.offsetMax = Vector2.zero;
            Image barFillImg = barFillGO.AddComponent<Image>();
            barFillImg.color      = new Color(0.3f, 0.8f, 0.3f, 1f);
            barFillImg.type       = Image.Type.Filled;       // fillAmount animasyonu için zorunlu
            barFillImg.fillMethod = Image.FillMethod.Horizontal;
            barFillImg.fillOrigin = 0;                       // soldan dolmaya başlar
            barFillImg.fillAmount = 0f;

            // ── İptal düğmesi ─────────────────────────────────────────────────
            GameObject btnGO = new GameObject("CancelButton");
            btnGO.transform.SetParent(panelGO.transform, false);
            RectTransform btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.anchorMin        = new Vector2(0.3f, 0f);
            btnRT.anchorMax        = new Vector2(0.7f, 0f);
            btnRT.anchoredPosition = new Vector2(0f, 120f);
            btnRT.sizeDelta        = new Vector2(0f, 80f);
            Image btnImg = btnGO.AddComponent<Image>();
            btnImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);
            Button btn = btnGO.AddComponent<Button>();

            GameObject btnLabelGO = new GameObject("Label");
            btnLabelGO.transform.SetParent(btnGO.transform, false);
            RectTransform btnLabelRT = btnLabelGO.AddComponent<RectTransform>();
            btnLabelRT.anchorMin = Vector2.zero;
            btnLabelRT.anchorMax = Vector2.one;
            btnLabelRT.offsetMin = Vector2.zero;
            btnLabelRT.offsetMax = Vector2.zero;
            TextMeshProUGUI btnLabel = btnLabelGO.AddComponent<TextMeshProUGUI>();
            btnLabel.text      = "CANCEL";
            btnLabel.fontSize  = 32;
            btnLabel.fontStyle = FontStyles.Bold;
            btnLabel.alignment = TextAlignmentOptions.Center;
            btnLabel.color     = Color.white;

            // ── SerializeField referanslarını ata ─────────────────────────────
            panel          = panelGO;
            jobTitleText   = titleGO.GetComponent<TextMeshProUGUI>();
            clientNameText = clientGO.GetComponent<TextMeshProUGUI>();
            payoutText     = payoutGO.GetComponent<TextMeshProUGUI>();
            timeCostText   = timeGO.GetComponent<TextMeshProUGUI>();
            barFill        = barFillImg; // arka plan değil, dolgu Image'ı atanır
            cancelButton   = btn;

            // Kök aktif kalır; OnEnable tetiklenir ve event abonelikleri kurulur.
            // Yalnızca panel çocuğu gizli başlar.
            panelGO.SetActive(false);

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("DrawingMinigame hierarchy built.");
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Metin etiketi oluşturur; anchor, offset, yazı tipi ve renk parametrelerini alır.
        /// BuildMinigameHierarchy içindeki tekrar eden etiket inşasını soyutlar.
        /// </summary>
        private static GameObject CreateLabel(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax,
            float fontSize, FontStyles style, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin  = anchorMin;
            rt.anchorMax  = anchorMax;
            rt.offsetMin  = offsetMin;
            rt.offsetMax  = offsetMax;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = name;
            tmp.fontSize  = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = color;
            return go;
        }
#endif
    }
}
