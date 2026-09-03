#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Freeline
{
    public class HUDManager : MonoBehaviour
    {
        // ---- Canvas / UI Sabitleri ------------------------------------------
        private const float RefWidth = 1080f;
        private const float RefHeight = 1920f;
        private const float TopPanelH = 320f;
        private const float BottomNavBarH = 200f;

        // ---- SerializeField References (Mühendislik Slotları) ---------------
        [Header("Alt Bar Butonları (Bottom NavBar)")]
        [SerializeField] private Button sleepButton;
        [SerializeField] private Button tabletButton;
        [SerializeField] private Button exhibitionButton;
        [SerializeField] private Button detailsButton; // Home butonu 4. buton

        [Header("Üst Bar Elementleri (Top Panel)")]
        [SerializeField] private Button settingsButton;
        [SerializeField] private RectTransform clockHandImg; // Dönen saat göstergesi ibresi (Saat Göstergesi_Ui)
        [SerializeField] private TextMeshProUGUI digitalClockText;
        [SerializeField] private Image energyBarFill; // Filled tipindeki enerji görseli
        [SerializeField] private TextMeshProUGUI energyText; // YENİ: Enerji miktarını yazacak Text
        [SerializeField] private TextMeshProUGUI hungerText; // Açlık yüzde metni
        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private TextMeshProUGUI gemText;
        [SerializeField] private Button addCoinButton;
        [SerializeField] private Button addGemButton;
        [SerializeField] private TextMeshProUGUI followersText; // Takipçi sayısı metni

        [Header("Aktif Buff İkonları")]
        [SerializeField] private Transform buffIconsContainer;
        [SerializeField] private GameObject buffIconPrefab;
        [SerializeField] private Sprite energyCostSprite;  // Enerji tasarrufu ikonu (Örn: Yıldırım)
        [SerializeField] private Sprite energyRegenSprite; // Enerji yenileme ikonu (Örn: Artı işareti)

        [Header("Detaylar Paneli")]

        [SerializeField] private DetailsPanel detailsPanel; // Az önce yazdığımız panelin kodu

        [Header("Açılacak Paneller / Sistem Referansları")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private MarketPanel marketPanel;
        [SerializeField] private GameObject tabletPanel; // Senin tasarımdaki ana tablet/telefon (PhonePanel'in root GO'su olabilir)
        [SerializeField] private DrawingDeskPanel drawingDeskPanel; // Yeni eklediğimiz çizim masası arayüzü
        [Header("Gün Sonu Paneli")]
        [SerializeField] private DailyReportPanel dailyReportPanel;
        [Header("Bayılma Sistemi")]
        [SerializeField] private GameObject passOutPanel;
        [SerializeField] private Button passOutOkButton;

        [Header("Dinamik Renk Ayarları (Görsel İçin)")]
        [SerializeField] private Color normalEnergyColor = new Color(1f, 0.7f, 0f, 1f); // Turuncumsu Sarı
        [SerializeField] private Color lowEnergyColor = new Color(0.9f, 0.1f, 0.1f, 1f); // Kritik Kırmızı

        // Singleton Yapısı (ZenitoonPanel veya diğer panellerin HUDManager'a kolay erişebilmesi için)
        public static HUDManager Instance { get; private set; }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        void Start()
        {
            // Alt Bar Buton Olayları
            if (sleepButton != null)
            {
                sleepButton.onClick.RemoveAllListeners();
                sleepButton.onClick.AddListener(OnSleepButtonClicked);
            }
            if (tabletButton != null) tabletButton.onClick.AddListener(OnTabletClicked);
            if (exhibitionButton != null) exhibitionButton.onClick.AddListener(OnExhibitionClicked);
            if (detailsButton != null) detailsButton.onClick.AddListener(OnDetailsClicked);

            // Üst Bar Buton Olayları
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
            if (addCoinButton != null) addCoinButton.onClick.AddListener(() => OpenMarketTab(MarketCategory.Food));
            if (addGemButton != null) addGemButton.onClick.AddListener(() => OpenMarketTab(MarketCategory.Upgrade));

            GameManager.Instance.TimeManager.OnPassedOut += ShowPassOutPanel;
            if (passOutOkButton != null) passOutOkButton.onClick.AddListener(OnPassOutOkClicked);

            // İlk verileri ekrana yükle
            RefreshAllUI();
        }

        void OnEnable()
        {
            if (GameManager.Instance != null && GameManager.Instance.TimeManager != null)
            {
                GameManager.Instance.TimeManager.OnTimeAdvanced += HandleTimeAdvanced;
                GameManager.Instance.TimeManager.OnNewDayStarted += HandleNewDay;
            }
            if (GameManager.Instance?.SaveManager != null)
            {
                GameManager.Instance.SaveManager.OnCoinsChanged += HandleCoinsChanged;
            }

            // --- YENİ EKLENEN KISIM: AÇLIK VE ENERJİ TAKİBİ ---
            if (GameManager.Instance?.EnergyManager != null)
            {
                GameManager.Instance.EnergyManager.OnHungerChanged += HandleHungerOrEnergyChanged;
                GameManager.Instance.EnergyManager.OnEnergyChanged += HandleHungerOrEnergyChanged;
            }
        }

        void OnDisable()
        {
            if (GameManager.Instance != null && GameManager.Instance.TimeManager != null)
            {
                GameManager.Instance.TimeManager.OnTimeAdvanced -= HandleTimeAdvanced;
                GameManager.Instance.TimeManager.OnNewDayStarted -= HandleNewDay;
            }
            if (GameManager.Instance?.SaveManager != null)
            {
                GameManager.Instance.SaveManager.OnCoinsChanged -= HandleCoinsChanged;
            }

            // --- YENİ EKLENEN KISIM: AÇLIK VE ENERJİ TAKİBİ ---
            if (GameManager.Instance?.EnergyManager != null)
            {
                GameManager.Instance.EnergyManager.OnHungerChanged -= HandleHungerOrEnergyChanged;
                GameManager.Instance.EnergyManager.OnEnergyChanged -= HandleHungerOrEnergyChanged;
            }
        }

        // =========================================================================
        // Event Tetikleyicileri & UI Güncelleme (Tazeleme)
        // =========================================================================

        private void HandleTimeAdvanced(float previousHour, float newHour) => RefreshAllUI();
        private void HandleNewDay(int currentDay) => RefreshAllUI();
        private void HandleCoinsChanged(int currentCoins) => RefreshAllUI();

        // Bu yeni satır, enerji veya açlık değiştiğinde UI'ı anında yenileyecek!
        private void HandleHungerOrEnergyChanged(float current, float max) => RefreshAllUI();

        public void RefreshAllUI()
        {
            if (GameManager.Instance == null || GameManager.Instance.SaveManager?.CurrentData == null) return;

            var data = GameManager.Instance.SaveManager.CurrentData;
            var timeManager = GameManager.Instance.TimeManager;
            var energyManager = GameManager.Instance.EnergyManager; // Enerji yöneticimizi buraya ekledik

            // 1. Ekonomi Verileri (Kutuların içindeki yazılar)
            if (coinText != null) coinText.text = Mathf.FloorToInt(data.currentCoins).ToString();
            if (gemText != null) gemText.text = data.currentGems.ToString();

            // 2. Açlık Durumu (Artık matematiği EnergyManager'dan çekiyoruz!)
            if (hungerText != null)
            {
                int hungerPercent = energyManager.GetHungerPercentage();

                // Market panelindeki gibi %25 altına düşünce kırmızı yapma mantığını buraya da ekleyelim
                string colorHex = hungerPercent <= 25 ? "red" : "green";
                hungerText.text = $"<color={colorHex}>Açlık: %{hungerPercent}</color>";
            }

            // 3. Webtoon Takipçi Sayısı
            if (followersText != null && data.webtoonData != null)
            {
                followersText.text = $"{data.webtoonData.totalFollowers} Takipçi";
            }

            // 4. Enerji Barı Doluluğu, Renk Ayarı ve Metin Yazımı
            if (energyBarFill != null)
            {
                float energyRatio = energyManager.CurrentEnergy / energyManager.MaxEnergy;
                energyBarFill.fillAmount = energyRatio;

                // %25 ve altındaysa kırmızı, değilse turuncu/sarı yap
                energyBarFill.color = (energyRatio <= 0.25f) ? lowEnergyColor : normalEnergyColor;
            }

            // Enerji değerini tam sayıya yuvarlayarak yazdır (Örn: 75 / 100)
            if (energyText != null)
            {
                energyText.text = $"{Mathf.FloorToInt(energyManager.CurrentEnergy)} / {Mathf.FloorToInt(energyManager.MaxEnergy)}";
            }

            // 5. Saat Sistemleri (Dijital Metin ve Kadran Dönüşü)
            if (timeManager != null)
            {
                if (digitalClockText != null)
                    digitalClockText.text = timeManager.GetFormattedTime();

                if (clockHandImg != null)
                {
                    float targetRotation = -timeManager.CurrentHour * 15f;
                    clockHandImg.localRotation = Quaternion.Euler(0f, 0f, targetRotation);
                }
            }

            UpdateBuffIcons();
        }

        // =========================================================================
        // Dışarıdan Tetiklenebilir Köprü Fonksiyonu (Zenitoon Panel Geçişi İçin)
        // =========================================================================

        /// <summary>
        /// Zenitoon panelinden iş alındığında tableti pürüzsüzce kapatıp çizim masasını açar.
        /// </summary>
        public void TransitFromTabletToDrawingDesk(JobData acceptedJob)
        {
            // Ana tablet/telefonGO'sunu kapat
            if (tabletPanel != null) tabletPanel.SetActive(false);

            // Çizim Masası Panelini aç ve işi yükle
            if (drawingDeskPanel != null)
            {
                drawingDeskPanel.gameObject.SetActive(true);
                drawingDeskPanel.StartJob(acceptedJob);
                Debug.Log($"[HUD Bridge] {acceptedJob.jobTitle} isi cizim masasina aktarildi.");
            }
        }

        // =========================================================================
        // Buton Dinleyicileri (Ajan/Log Modu)
        // =========================================================================

        private void OnSleepClicked()
        {
            Debug.Log("<color=cyan>[HUD] Uyu Butonuna Basildi!</color>");
        }

        private void OnTabletClicked()
        {
            Debug.Log($"<color=yellow>[HUD] Tablet Butonuna Basildi! Panel Var Mi: {tabletPanel != null}</color>");
            if (tabletPanel != null) tabletPanel.SetActive(!tabletPanel.activeSelf);
        }

        private void OnExhibitionClicked()
        {
            Debug.Log("<color=green>[HUD] Home (Ev) Butonuna Basildi! Tüm paneller kapaniyor.</color>");
            if (tabletPanel != null) tabletPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (drawingDeskPanel != null) drawingDeskPanel.gameObject.SetActive(false);
            if (marketPanel != null) marketPanel.Close();
        }

        private void OnDetailsClicked()
        {
            Debug.Log("<color=magenta>[HUD] Detaylar Butonuna Basıldı!</color>");

            // Diğer açık panelleri kapatıp ortalığı temizleyelim
            if (tabletPanel != null) tabletPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (drawingDeskPanel != null) drawingDeskPanel.gameObject.SetActive(false);
            if (marketPanel != null) marketPanel.Close();

            // Kendi panelimizi açalım
            if (detailsPanel != null) detailsPanel.OpenPanel();
        }

        private void OnSettingsClicked()
        {
            if (settingsPanel != null) settingsPanel.SetActive(!settingsPanel.activeSelf);
        }

        private void OpenMarketTab(MarketCategory category)
        {
            if (marketPanel != null)
            {
                marketPanel.Open();
                marketPanel.ShowCategory(category);
            }
        }

        private void UpdateBuffIcons()
        {
            if (buffIconsContainer == null || buffIconPrefab == null) return;

            // Önce eski ikonları temizle
            foreach (Transform child in buffIconsContainer)
            {
                Destroy(child.gameObject);
            }

            var activeBuffs = GameManager.Instance.EnergyManager.GetActiveBuffs();

            // Aktif buff sayısınca ikon üret
            foreach (var buff in activeBuffs)
            {
                GameObject iconObj = Instantiate(buffIconPrefab, buffIconsContainer);
                Image img = iconObj.GetComponent<Image>();
                TooltipTrigger tooltip = iconObj.GetComponent<TooltipTrigger>();

                string buffTitle = "Güçlendirme";
                string buffDesc = "Aktif bir etki.";

                // --- YENİ GERÇEK ZAMAN (DATETIME) HESAPLAMASI ---
                string timeLeftText = "Bilinmiyor";
                if (System.DateTime.TryParse(buff.endTimeString, out System.DateTime endTime))
                {
                    System.TimeSpan timeLeft = endTime - System.DateTime.Now;

                    if (timeLeft.TotalSeconds > 0)
                    {
                        if (timeLeft.TotalHours >= 1)
                            timeLeftText = $"{(int)timeLeft.TotalHours} saat {(int)timeLeft.Minutes} dk";
                        else
                            timeLeftText = $"{(int)timeLeft.Minutes} dakika";
                    }
                    else
                    {
                        timeLeftText = "Süresi bitti";
                    }
                }

                if (img != null)
                {
                    // Efekt tipine göre doğru resmi (Sprite) ve Tooltip yazılarını ata
                    if (buff.effectType == ConsumableEffectType.EnergyCostReduction)
                    {
                        img.sprite = energyCostSprite;
                        buffTitle = "Odaklanma";
                        buffDesc = $"İş yaparken daha az enerji harcarsın.\n(Kalan: {timeLeftText})";
                    }
                    else if (buff.effectType == ConsumableEffectType.EnergyRegenOverTime)
                    {
                        img.sprite = energyRegenSprite;
                        buffTitle = "Enerji Patlaması";
                        buffDesc = $"Zamanla ekstra enerji kazanırsın.\n(Kalan: {timeLeftText})";
                    }
                }

                // TooltipTrigger bileşeni varsa, yazıları içine gönder
                if (tooltip != null)
                {
                    tooltip.Setup(buffTitle, buffDesc, true); // Pozitif buff olduğu için true (Yeşil)
                }
            }

            // ... (aktif buff foreach döngüsü bittikten sonra)
            var data = GameManager.Instance.SaveManager.CurrentData;

            // A. Bayılma Cezası İkonu (Sadece bayılarak uyandıysa görünür)
            if (data.hasPassOutPenalty)
            {
                GameObject penaltyIcon = Instantiate(buffIconPrefab, buffIconsContainer);
                penaltyIcon.GetComponent<Image>().color = Color.red; // Geçici olarak kırmızı renklendir (veya kendi sprite'ını ata)
                penaltyIcon.GetComponent<TooltipTrigger>()?.Setup(
                    "Aşırı Yorgunluk",
                    "Bayıldığın için maksimum enerjin bir sonraki uykuya kadar kısıtlandı!",
                    false);
            }

            // B. Kademeli Uykusuzluk İkonu (16 Saat Üstü)
            float awake = data.hoursAwake;
            if (awake >= 16f)
            {
                string debuffTitle = "Uykusuz";
                string debuffDesc = "İş yapma hızın %15 düştü.";

                if (awake >= 24f)
                {
                    debuffTitle = "Tükenmiş";
                    debuffDesc = "İş yapma hızın yarı yarıya (%50) düştü! Uyumazsan bayılacaksın.";
                }
                else if (awake >= 20f)
                {
                    debuffTitle = "Çok Yorgun";
                    debuffDesc = "İş yapma hızın %30 düştü.";
                }

                GameObject fatigueIcon = Instantiate(buffIconPrefab, buffIconsContainer);
                fatigueIcon.GetComponent<Image>().color = new Color(1f, 0.5f, 0f); // Turuncu uyarı rengi
                fatigueIcon.GetComponent<TooltipTrigger>()?.Setup(debuffTitle, debuffDesc, false);
            }
        }

        private void OnSleepButtonClicked()
        {
            Debug.Log("[HUD] Uyu butonuna basıldı, Gün Sonu Raporu açılıyor...");

            // Açık olan diğer panelleri (Tablet, Market vb.) kapat
            if (tabletPanel != null) tabletPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (marketPanel != null) marketPanel.Close();

            // Gün sonu panelini aç
            if (dailyReportPanel != null)
            {
                dailyReportPanel.OpenPanel();
            }
        }

        private void ShowPassOutPanel()
        {
            if (passOutPanel != null) passOutPanel.SetActive(true);
        }

        private void OnPassOutOkClicked()
        {
            passOutPanel.SetActive(false);
            // Uyku ekranına (Faturaya) zorla geçiş yapıyoruz!
            if (dailyReportPanel != null) dailyReportPanel.OpenPanel();
        }
    }
}