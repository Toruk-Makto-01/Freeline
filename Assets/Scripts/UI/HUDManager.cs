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
        [SerializeField] private Button homeButton;

        [Header("Üst Bar Elementleri (Top Panel)")]
        [SerializeField] private Button settingsButton;
        [SerializeField] private RectTransform clockHandImg; // Dönen saat göstergesi ibresi (Saat Göstergesi_Ui)
        [SerializeField] private TextMeshProUGUI digitalClockText;
        [SerializeField] private Image energyBarFill; // Filled tipindeki enerji görseli
        [SerializeField] private TextMeshProUGUI hungerText; // Açlık yüzde metni
        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private TextMeshProUGUI gemText;
        [SerializeField] private Button addCoinButton;
        [SerializeField] private Button addGemButton;
        [SerializeField] private TextMeshProUGUI followersText; // Takipçi sayısı metni

        [Header("Açılacak Paneller / Sistem Referansları")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private MarketPanel marketPanel;
        [SerializeField] private GameObject tabletPanel; // Senin tasarımdaki ana tablet/telefon (PhonePanel'in root GO'su olabilir)
        [SerializeField] private DrawingDeskPanel drawingDeskPanel; // Yeni eklediğimiz çizim masası arayüzü

        [Header("Dinamik Renk Ayarları (Grisel Görsel İçin)")]
        [SerializeField] private Color normalEnergyColor = new Color(0f, 0.8f, 0.4f, 1f); // Yeşil / Mavi buff rengi
        [SerializeField] private Color lowEnergyColor = new Color(0.9f, 0.1f, 0.1f, 1f); // Kritik kırmızı

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
            if (sleepButton != null) sleepButton.onClick.AddListener(OnSleepClicked);
            if (tabletButton != null) tabletButton.onClick.AddListener(OnTabletClicked);
            if (homeButton != null) homeButton.onClick.AddListener(OnHomeClicked);

            // Üst Bar Buton Olayları
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
            if (addCoinButton != null) addCoinButton.onClick.AddListener(() => OpenMarketTab(MarketCategory.Yemek));
            if (addGemButton != null) addGemButton.onClick.AddListener(() => OpenMarketTab(MarketCategory.Upgrade));

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
        }

        // =========================================================================
        // Event Tetikleyicileri & UI Güncelleme (Tazeleme)
        // =========================================================================

        private void HandleTimeAdvanced(float previousHour, float newHour) => RefreshAllUI();
        private void HandleNewDay(int currentDay) => RefreshAllUI();
        private void HandleCoinsChanged(int currentCoins) => RefreshAllUI();

        public void RefreshAllUI()
        {
            if (GameManager.Instance == null || GameManager.Instance.SaveManager?.CurrentData == null) return;

            var data = GameManager.Instance.SaveManager.CurrentData;
            var timeManager = GameManager.Instance.TimeManager;

            // 1. Ekonomi Verileri (Kutuların içindeki yazılar)
            if (coinText != null) coinText.text = Mathf.FloorToInt(data.currentCoins).ToString();
            if (gemText != null) gemText.text = data.currentGems.ToString();

            // 2. Açlık Durumu (hoursSinceLastFood değişkeninden yüzde üretiyoruz)
            if (hungerText != null)
            {
                int hungerPercent = Mathf.Clamp(100 - Mathf.RoundToInt(data.hoursSinceLastFood * 5f), 0, 100);
                hungerText.text = $"Açlık: %{hungerPercent}";
            }

            // 3. Webtoon Takipçi Sayısı (SaveData -> webtoonData)
            if (followersText != null && data.webtoonData != null)
            {
                followersText.text = $"{data.webtoonData.totalFollowers} Takipçi";
            }

            // 4. Enerji Barı Doluluğu ve Renk Ayarı (Filled Image)
            if (energyBarFill != null)
            {
                float energyRatio = data.currentEnergy / 100f; // Maks enerji 100 varsayıldı
                energyBarFill.fillAmount = energyRatio;
                energyBarFill.color = (energyRatio <= 0.25f) ? lowEnergyColor : normalEnergyColor;
            }

            // 5. Saat Sistemleri (Dijital Metin ve Kadran Dönüşü)
            if (timeManager != null)
            {
                if (digitalClockText != null)
                    digitalClockText.text = timeManager.GetFormattedTime();

                if (clockHandImg != null)
                {
                    // Her saat 15 derece dönmeli (360 derece / 24 saat = 15 derece)
                    float targetRotation = -timeManager.CurrentHour * 15f;
                    clockHandImg.localRotation = Quaternion.Euler(0f, 0f, targetRotation);
                }
            }
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

        private void OnHomeClicked()
        {
            Debug.Log("<color=green>[HUD] Home (Ev) Butonuna Basildi! Tüm paneller kapaniyor.</color>");
            if (tabletPanel != null) tabletPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (drawingDeskPanel != null) drawingDeskPanel.gameObject.SetActive(false);
            if (marketPanel != null) marketPanel.Close();
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
    }
}