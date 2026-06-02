#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Freeline
{
    /// <summary>
    /// Pano üzerindeki her iş kartının UI referanslarını bir arada tutar.
    /// <see cref="JobBoardPanel"/> tarafından oluşturulur ve doldurulur.
    /// </summary>
    public class JobBoardPanel : MonoBehaviour
    {
        [System.Serializable]
        private class JobCardRefs
        {
            public GameObject      root;
            public TextMeshProUGUI titleText;
            public TextMeshProUGUI clientText;
            public TextMeshProUGUI timeText;
            public TextMeshProUGUI payoutText;
            public TextMeshProUGUI energyText;
            public TextMeshProUGUI diffText;
            public Button          selectButton;
            public TextMeshProUGUI selectLabel;
        }

        // -------------------------------------------------------------------------
        // Inspector references — populated by [Build Job Board Hierarchy] or manually
        // -------------------------------------------------------------------------

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI headerText;
        [SerializeField] private Button          closeButton;

        [Header("Job Cards")]
        [SerializeField] private JobCardRefs[] cards = new JobCardRefs[3];

        [Header("Footer")]
        [SerializeField] private Button          refreshButton;
        [SerializeField] private TextMeshProUGUI refreshLabel;

        // -------------------------------------------------------------------------
        // Colors
        // -------------------------------------------------------------------------

        private static readonly Color PanelBg     = new Color(0.08f, 0.08f, 0.12f, 0.96f);
        private static readonly Color CardBg       = new Color(0.13f, 0.13f, 0.20f, 1.00f);
        private static readonly Color CardSelected = new Color(0.18f, 0.38f, 0.65f, 1.00f);
        private static readonly Color SelectBtn    = new Color(0.20f, 0.65f, 0.30f, 1.00f);
        private static readonly Color BlockedBtn   = new Color(0.35f, 0.15f, 0.15f, 1.00f);
        private static readonly Color DiffBeginner = new Color(0.20f, 0.85f, 0.30f, 1.00f);
        private static readonly Color DiffInter    = new Color(0.95f, 0.75f, 0.10f, 1.00f);
        private static readonly Color DiffAdvanced = new Color(0.95f, 0.25f, 0.20f, 1.00f);
        private static readonly Color CloseBtn     = new Color(0.45f, 0.12f, 0.12f, 1.00f);
        private static readonly Color RefreshBtn   = new Color(0.18f, 0.18f, 0.30f, 1.00f);

        // Seçili kartın indeksi; hiçbir kart seçili değilse -1.
        private int _selectedIndex = -1;

        // -------------------------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------------------------

        void Awake()
        {
            closeButton.onClick.AddListener(Hide);
            refreshButton.onClick.AddListener(OnRefreshClicked);

            for (int i = 0; i < cards.Length; i++)
            {
                int idx = i;
                // Lambda'da döngü değişkenini kapamak yerine yerel kopya kullanılır;
                // aksi takdirde tüm düğmeler son indeksi yakalar.
                cards[i].selectButton.onClick.AddListener(() => OnSelectClicked(idx));
            }
        }

        void Start()
        {
            var gm = GameManager.Instance;
            var jm = gm.JobManager;

            jm.OnJobBoardRefreshed  += HandleBoardRefreshed;
            jm.OnJobSelected        += HandleJobSelected;
            jm.OnJobStarted         += HandleJobStarted;
            jm.OnJobBlockedByEnergy += HandleEnergyBlocked;
            gm.EnergyManager.OnEnergyChanged += HandleEnergyChanged;

            Hide();
        }

        void OnDestroy()
        {
            if (GameManager.Instance == null) return;
            var gm = GameManager.Instance;
            var jm = gm.JobManager;

            jm.OnJobBoardRefreshed  -= HandleBoardRefreshed;
            jm.OnJobSelected        -= HandleJobSelected;
            jm.OnJobStarted         -= HandleJobStarted;
            jm.OnJobBlockedByEnergy -= HandleEnergyBlocked;
            gm.EnergyManager.OnEnergyChanged -= HandleEnergyChanged;
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>
        /// Paneli görünür yapar, kart seçimini sıfırlar ve güncel pano içeriğini doldurur.
        /// HUDManager'daki Draw düğmesine basıldığında çağrılır.
        /// </summary>
        public void Show()
        {
            _selectedIndex = -1;
            gameObject.SetActive(true);
            PopulateCards(GameManager.Instance.JobManager.CurrentBoardJobs);
            UpdateRefreshButton();
        }

        /// <summary>Paneli gizler.</summary>
        public void Hide() => gameObject.SetActive(false);

        // -------------------------------------------------------------------------
        // Kart doldurma
        // -------------------------------------------------------------------------

        /// <summary>
        /// İş listesindeki verileri kart UI elemanlarına yazar.
        /// Panodaki iş sayısı kart sayısından azsa fazlalık kartlar gizlenir.
        /// </summary>
        private void PopulateCards(IReadOnlyList<JobData> jobs)
        {
            bool blocked = GameManager.Instance.EnergyManager.CurrentEnergy <= 0f;

            for (int i = 0; i < cards.Length; i++)
            {
                var card = cards[i];
                if (i < jobs.Count)
                {
                    JobData job = jobs[i];
                    card.root.SetActive(true);
                    card.titleText.text  = job.jobTitle;
                    card.clientText.text = job.clientName;
                    card.timeText.text   = $"TIME: {job.durationHours:0.#}h";
                    card.payoutText.text = $"COINS: {Mathf.FloorToInt(job.basePayout)}";
                    card.energyText.text = $"ENERGY: {Mathf.FloorToInt(job.energyCost)}";
                    card.diffText.text   = job.difficulty.ToString().ToUpper();
                    card.diffText.color  = DifficultyColor(job.difficulty);
                    RefreshCardVisuals(card, i == _selectedIndex, blocked);
                }
                else
                {
                    card.root.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Kartın seçili/engelli durumuna göre arka plan rengini ve düğme görselini günceller.
        /// </summary>
        private static void RefreshCardVisuals(JobCardRefs card, bool selected, bool blocked)
        {
            card.root.GetComponent<Image>().color             = selected ? CardSelected : CardBg;
            card.selectButton.interactable                    = !blocked;
            card.selectButton.GetComponent<Image>().color     = blocked ? BlockedBtn : SelectBtn;
            card.selectLabel.text                             = blocked ? "NO ENERGY" : "SELECT";
        }

        /// <summary>
        /// Yenileme düğmesinin metnini ve etkileşilebilirliğini kalan yenileme hakkına göre günceller.
        /// </summary>
        private void UpdateRefreshButton()
        {
            int left = GameManager.Instance.JobManager.RefreshesRemaining;
            refreshLabel.text          = $"REFRESH ({left} left)";
            refreshButton.interactable = left > 0;
        }

        /// <summary>Zorluk seviyesine karşılık gelen rengi döndürür.</summary>
        private static Color DifficultyColor(JobDifficulty d) => d switch
        {
            JobDifficulty.Beginner     => DiffBeginner,
            JobDifficulty.Intermediate => DiffInter,
            _                          => DiffAdvanced,
        };

        // -------------------------------------------------------------------------
        // Düğme işleyiciler
        // -------------------------------------------------------------------------

        /// <summary>
        /// Seçim düğmesine basıldığında ilgili işi seçip hemen başlatmayı dener.
        /// SelectJob başarılı olursa StartJob çağrılır; enerji yetersizse her ikisi de reddeder.
        /// </summary>
        private void OnSelectClicked(int index)
        {
            var jm = GameManager.Instance.JobManager;
            if (jm.SelectJob(index))
                jm.StartJob();
        }

        /// <summary>
        /// Pano yenileme isteğini JobManager'a iletir ve düğme etiketini günceller.
        /// </summary>
        private void OnRefreshClicked()
        {
            GameManager.Instance.JobManager.RefreshJobBoard();
            UpdateRefreshButton();
        }

        // -------------------------------------------------------------------------
        // Event işleyiciler
        // -------------------------------------------------------------------------

        /// <summary>
        /// Pano yenilendiğinde seçimi sıfırlar ve panel açıksa kartları yeniden doldurur.
        /// Panel kapalıysa doldurma atlanır; Show() açılışta zaten dolduracak.
        /// </summary>
        private void HandleBoardRefreshed(IReadOnlyList<JobData> jobs)
        {
            _selectedIndex = -1;
            if (gameObject.activeSelf) PopulateCards(jobs);
            UpdateRefreshButton();
        }

        /// <summary>
        /// Bir iş seçildiğinde ilgili kartı seçili olarak işaretler ve görselleri günceller.
        /// </summary>
        private void HandleJobSelected(JobData job)
        {
            var jobs = GameManager.Instance.JobManager.CurrentBoardJobs;
            _selectedIndex = -1;
            for (int i = 0; i < jobs.Count; i++)
                if (jobs[i] == job) { _selectedIndex = i; break; }
            if (gameObject.activeSelf) PopulateCards(jobs);
        }

        /// <summary>
        /// İş başladığında paneli gizler; mini-oyun devralır.
        /// </summary>
        private void HandleJobStarted(JobData job)
        {
            Debug.Log($"[JobBoard] Job started: {job.jobTitle}");
            Hide();
        }

        /// <summary>
        /// Enerji tükendiğinde panel açıksa kartları yeniden boyar (SELECT → NO ENERGY).
        /// </summary>
        private void HandleEnergyBlocked()
        {
            if (gameObject.activeSelf)
                PopulateCards(GameManager.Instance.JobManager.CurrentBoardJobs);
        }

        /// <summary>
        /// Enerji değiştiğinde panel açıksa kartları günceller.
        /// Enerji sıfırdan pozitife geçişte düğmeleri yeniden etkinleştirmek için gereklidir.
        /// </summary>
        private void HandleEnergyChanged(float current, float max)
        {
            if (gameObject.activeSelf)
                PopulateCards(GameManager.Instance.JobManager.CurrentBoardJobs);
        }

        // =========================================================================
        // EDITOR — Hierarchy builder
        // Right-click this component in the Inspector → "Build Job Board Hierarchy"
        // Destroys existing children, rebuilds the full hierarchy, and populates
        // every SerializeField reference automatically.
        //
        // Layout (reference resolution 1080 × 1920):
        //   JobBoardPanel (full-screen RectTransform)
        //     Blocker  — full-screen dark overlay, intercepts background taps
        //     Panel    — 900 × 1400, centered
        //       CloseButton   — 90 × 90, top-right
        //       HeaderText    — full width, 120 px tall, 24 px from top
        //       Divider       — 2 px separator at 150 px from top
        //       JobCard_0..2  — 840 × 340, stacked with 20 px gaps, start at 170 px
        //       RefreshButton — 700 × 88, 28 px from bottom
        // =========================================================================

#if UNITY_EDITOR
        [ContextMenu("Build Job Board Hierarchy")]
        private void BuildJobBoardHierarchy()
        {
            // Eski çocuk nesneleri temizle; temiz yeniden inşa için gerekli.
            while (transform.childCount > 0)
                DestroyImmediate(transform.GetChild(0).gameObject);

            // Bu nesne ebeveyn Canvas'ı tamamen kaplamalıdır.
            Stretch(GetComponent<RectTransform>());

            // Tam ekran karartma katmanı — panel arkasındaki dokunuşları yakalar.
            var blocker = NewUIObject("Blocker", transform);
            blocker.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            Stretch(blocker.GetComponent<RectTransform>());

            // Ortalanmış panel — 900 × 1400.
            var panel = NewUIObject("Panel", transform);
            panel.AddComponent<Image>().color = PanelBg;
            var panelRT = panel.GetComponent<RectTransform>();
            panelRT.anchorMin        = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax        = new Vector2(0.5f, 0.5f);
            panelRT.pivot            = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta        = new Vector2(900f, 1400f);
            panelRT.anchoredPosition = Vector2.zero;

            // Kapat düğmesi — sağ üst köşe, 90 × 90.
            closeButton = BuildIconButton("CloseButton", panel.transform, "X", CloseBtn);
            var closeRT = closeButton.GetComponent<RectTransform>();
            closeRT.anchorMin        = new Vector2(1f, 1f);
            closeRT.anchorMax        = new Vector2(1f, 1f);
            closeRT.pivot            = new Vector2(1f, 1f);
            closeRT.sizeDelta        = new Vector2(90f, 90f);
            closeRT.anchoredPosition = new Vector2(-16f, -16f);

            // Başlık metni — tam genişlik, 120 px, panelin üstünden 24 px aşağıda.
            headerText = NewTMP("HeaderText", panel.transform,
                                "AVAILABLE JOBS", 60f, TextAlignmentOptions.Center);
            headerText.fontStyle = FontStyles.Bold;
            PositionFromTop(headerText.rectTransform, 0f, 1f, 24f, 120f, 0f);

            // Ayırıcı çizgi — panelin üstünden 150 px, 2 px yükseklik.
            var divider = NewUIObject("Divider", panel.transform);
            divider.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
            var divRT = divider.GetComponent<RectTransform>();
            divRT.anchorMin = new Vector2(0.04f, 1f);
            divRT.anchorMax = new Vector2(0.96f, 1f);
            divRT.pivot     = new Vector2(0.5f,  1f);
            divRT.offsetMin = new Vector2(0f, -152f);
            divRT.offsetMax = new Vector2(0f, -150f);

            // 3 iş kartı — 840 × 340, 20 px boşlukla istiflenmiş, 170 px'den başlar.
            cards = new JobCardRefs[3];
            const float CardH  = 340f;
            const float GapV   =  20f;
            const float StartY = -170f;

            for (int i = 0; i < 3; i++)
                cards[i] = BuildJobCard($"JobCard_{i}", panel.transform,
                                        StartY - i * (CardH + GapV), CardH);

            // Yenileme düğmesi — 700 × 88, panelin altından 28 px yukarıda.
            var refreshGO  = NewUIObject("RefreshButton", panel.transform);
            var refreshImg = refreshGO.AddComponent<Image>();
            refreshImg.color    = RefreshBtn;
            refreshButton       = refreshGO.AddComponent<Button>();
            refreshButton.targetGraphic = refreshImg;
            var refreshRT = refreshGO.GetComponent<RectTransform>();
            refreshRT.anchorMin        = new Vector2(0.5f, 0f);
            refreshRT.anchorMax        = new Vector2(0.5f, 0f);
            refreshRT.pivot            = new Vector2(0.5f, 0f);
            refreshRT.sizeDelta        = new Vector2(700f, 88f);
            refreshRT.anchoredPosition = new Vector2(0f, 28f);

            refreshLabel = NewTMP("RefreshLabel", refreshGO.transform,
                                  "REFRESH (3 left)", 34f, TextAlignmentOptions.Center);
            Stretch(refreshLabel.rectTransform);

            EditorUtility.SetDirty(gameObject);
            Debug.Log("[JobBoard] Hierarchy built and SerializeField references populated.");
        }

        /// <summary>
        /// Tek bir iş kartı oluşturur: başlık, müşteri adı, istatistik satırı,
        /// zorluk rozeti ve seçim düğmesi.
        /// </summary>
        /// <param name="anchoredY">Panel üstünden kartın üst kenarına mesafe (negatif).</param>
        private static JobCardRefs BuildJobCard(string name, Transform parent,
                                                float anchoredY, float cardHeight)
        {
            var go = NewUIObject(name, parent);
            go.AddComponent<Image>().color = CardBg;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 1f);
            rt.anchorMax        = new Vector2(0.5f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.sizeDelta        = new Vector2(840f, cardHeight);
            rt.anchoredPosition = new Vector2(0f, anchoredY);

            // Başlık — kalın, tam genişlik, kartın üstünden 18 px.
            var title = NewTMP("TitleText", go.transform, "Job Title", 44f, TextAlignmentOptions.Left);
            title.fontStyle = FontStyles.Bold;
            PositionFromTop(title.rectTransform, 0f, 1f, 18f, 60f, 24f);

            // Müşteri adı — soluk renk, kartın üstünden 86 px.
            var client = NewTMP("ClientText", go.transform, "Client Name", 26f, TextAlignmentOptions.Left);
            client.color = new Color(0.65f, 0.65f, 0.75f, 1f);
            PositionFromTop(client.rectTransform, 0f, 1f, 86f, 40f, 24f);

            // İstatistik satırı — SÜRE (sol), COIN (orta), ENERJİ (sağ), kartın üstünden 138 px.
            var time = NewTMP("TimeText", go.transform, "TIME: 2h", 28f, TextAlignmentOptions.Left);
            time.rectTransform.anchorMin        = new Vector2(0f, 1f);
            time.rectTransform.anchorMax        = new Vector2(0f, 1f);
            time.rectTransform.pivot            = new Vector2(0f, 1f);
            time.rectTransform.sizeDelta        = new Vector2(250f, 40f);
            time.rectTransform.anchoredPosition = new Vector2(24f, -138f);

            var payout = NewTMP("PayoutText", go.transform, "COINS: 500", 28f, TextAlignmentOptions.Center);
            payout.rectTransform.anchorMin        = new Vector2(0.5f, 1f);
            payout.rectTransform.anchorMax        = new Vector2(0.5f, 1f);
            payout.rectTransform.pivot            = new Vector2(0.5f, 1f);
            payout.rectTransform.sizeDelta        = new Vector2(300f, 40f);
            payout.rectTransform.anchoredPosition = new Vector2(0f, -138f);

            var energy = NewTMP("EnergyText", go.transform, "ENERGY: 20", 28f, TextAlignmentOptions.Right);
            energy.rectTransform.anchorMin        = new Vector2(1f, 1f);
            energy.rectTransform.anchorMax        = new Vector2(1f, 1f);
            energy.rectTransform.pivot            = new Vector2(1f, 1f);
            energy.rectTransform.sizeDelta        = new Vector2(250f, 40f);
            energy.rectTransform.anchoredPosition = new Vector2(-24f, -138f);

            // Zorluk rozeti — kalın, renkli, kartın üstünden 192 px.
            var diff = NewTMP("DiffText", go.transform, "BEGINNER", 30f, TextAlignmentOptions.Left);
            diff.fontStyle = FontStyles.Bold;
            diff.color     = DiffBeginner;
            diff.rectTransform.anchorMin        = new Vector2(0f, 1f);
            diff.rectTransform.anchorMax        = new Vector2(0f, 1f);
            diff.rectTransform.pivot            = new Vector2(0f, 1f);
            diff.rectTransform.sizeDelta        = new Vector2(380f, 44f);
            diff.rectTransform.anchoredPosition = new Vector2(24f, -192f);

            // SELECT düğmesi — 600 × 72, kartın altından 20 px, ortalanmış.
            var btnGO  = NewUIObject("SelectButton", go.transform);
            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = SelectBtn;
            var btn    = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            var btnRT  = btnGO.GetComponent<RectTransform>();
            btnRT.anchorMin        = new Vector2(0.5f, 0f);
            btnRT.anchorMax        = new Vector2(0.5f, 0f);
            btnRT.pivot            = new Vector2(0.5f, 0f);
            btnRT.sizeDelta        = new Vector2(600f, 72f);
            btnRT.anchoredPosition = new Vector2(0f, 20f);

            var selectLabel = NewTMP("SelectLabel", btnGO.transform,
                                     "SELECT", 36f, TextAlignmentOptions.Center);
            selectLabel.fontStyle = FontStyles.Bold;
            Stretch(selectLabel.rectTransform);

            return new JobCardRefs
            {
                root         = go,
                titleText    = title,
                clientText   = client,
                timeText     = time,
                payoutText   = payout,
                energyText   = energy,
                diffText     = diff,
                selectButton = btn,
                selectLabel  = selectLabel,
            };
        }

        /// <summary>
        /// Elemanı üst kenara sabitler; yatayda xMin–xMax arasında, padH piksel iç dolguyla yerleştirir.
        /// </summary>
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

        /// <summary>Metin etiketli küçük simge düğmesi oluşturur (örn. kapat düğmesi).</summary>
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

        /// <summary>RectTransform bileşeni olan boş bir UI nesnesi oluşturur.</summary>
        private static GameObject NewUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        /// <summary>Belirtilen metin ve boyutla yapılandırılmış bir TextMeshProUGUI bileşeni oluşturur.</summary>
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

        /// <summary>RectTransform'u ebeveynine tam germe (stretch-stretch) olarak ayarlar.</summary>
        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
#endif
    }
}
