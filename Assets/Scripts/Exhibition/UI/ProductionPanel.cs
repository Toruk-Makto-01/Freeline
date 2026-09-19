using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Freeline
{
    public class ProductionPanel : MonoBehaviour
    {
        public static ProductionPanel Instance { get; private set; }

        [Header("Genel Arayüz")]
        [SerializeField] private Button backButton;
        [SerializeField] private Transform gridContent;
        [SerializeField] private ProductionItemUI itemPrefab;
        [SerializeField] private List<ExhibitionProductData> allProducts = new();

        [Header("Mini Detay Ekranı")]
        [SerializeField] private GameObject detailPopup;
        [SerializeField] private Image popupProductIcon;
        [SerializeField] private TextMeshProUGUI popupTitleText;
        [SerializeField] private TextMeshProUGUI popupEnergyCostText;
        [SerializeField] private TextMeshProUGUI popupTimeCostText;
        [SerializeField] private TextMeshProUGUI popupPotentialPriceText;
        [SerializeField] private TextMeshProUGUI popupDifficultyText;
        [SerializeField] private Button popupProduceButton;
        [SerializeField] private Button popupCloseButton;

        [Header("Sahne ve Mini Oyun Entegrasyonu")]
        [Tooltip("SlideBarGame'i içinde barındıran masa objesi (DrawingDeskPanel)")]
        [SerializeField] private GameObject drawingDeskPanel;
        [Tooltip("Telefon arayüzünün ana objesi (Masaya geçerken gizlenecek)")]
        [SerializeField] private GameObject phonePanel;
        [SerializeField] private SlideBarGame slideBarGame;

        private ExhibitionProductData _selectedProduct;
        private List<ProductionItemUI> _spawnedItems = new();

        private void Awake()
        {
            Instance = this;

            if (backButton != null) backButton.onClick.AddListener(Close);
            if (popupCloseButton != null) popupCloseButton.onClick.AddListener(CancelDetailPopup);
            if (popupProduceButton != null) popupProduceButton.onClick.AddListener(StartProductionGame);

            // Telefon kapansa bile dinlemenin kopmaması için Awake'te abone oluyoruz
            if (slideBarGame != null)
            {
                slideBarGame.OnGameFinished += HandleSlideBarResult;
            }
        }

        private void OnDestroy()
        {
            if (slideBarGame != null)
            {
                slideBarGame.OnGameFinished -= HandleSlideBarResult;
            }
        }

        public void Open()
        {
            gameObject.SetActive(true);
            detailPopup.SetActive(false);
            _selectedProduct = null;
            RefreshList();
        }

        public void Close()
        {
            detailPopup.SetActive(false);
            _selectedProduct = null;
            gameObject.SetActive(false);
        }

        public void RefreshList()
        {
            foreach (var item in _spawnedItems)
            {
                if (item != null) Destroy(item.gameObject);
            }
            _spawnedItems.Clear();

            var saveData = GameManager.Instance.SaveManager.CurrentData;
            int unlockedLimit = Mathf.Max(1, saveData.unlockedProductionCount);

            for (int i = 0; i < allProducts.Count; i++)
            {
                var product = allProducts[i];
                bool isUnlocked = i < unlockedLimit;

                int currentStock = 0;
                var stockItem = saveData.exhibitionStock.Find(s => s.product == product);
                if (stockItem != null) currentStock = stockItem.quantity;

                ProductionItemUI uiCard = Instantiate(itemPrefab, gridContent);
                uiCard.Setup(product, isUnlocked, currentStock);
                uiCard.OnCardClicked += OpenDetailPopup;

                _spawnedItems.Add(uiCard);
            }
        }

        private void OpenDetailPopup(ExhibitionProductData product)
        {
            _selectedProduct = product;

            if (popupProductIcon != null) popupProductIcon.sprite = product.icon;
            if (popupTitleText != null) popupTitleText.text = product.productName;
            if (popupEnergyCostText != null) popupEnergyCostText.text = $"Enerji: {product.energyCost}";
            if (popupTimeCostText != null) popupTimeCostText.text = $"Süre: {product.productionHours} Saat";
            if (popupPotentialPriceText != null) popupPotentialPriceText.text = $"~{product.basePrice} Coin";
            if (popupDifficultyText != null) popupDifficultyText.text = $"{product.difficultyLabel}";

            detailPopup.SetActive(true);
        }

        // Oyuncu X veya Kapat butonuna bastığında çağrılır
        private void CancelDetailPopup()
        {
            if (detailPopup != null) detailPopup.SetActive(false);
            _selectedProduct = null;
        }

        public void StartProductionGame()
        {
            if (_selectedProduct == null) return;

            var energyManager = GameManager.Instance.EnergyManager;
            if (energyManager.CurrentEnergy < _selectedProduct.energyCost)
            {
                Debug.LogWarning("[Üretim] Yeterli enerji yok!");
                return;
            }

            // 1. Detay penceresini kapat (Veriyi null yapmadan sadece pencereyi gizliyoruz)
            if (detailPopup != null) detailPopup.SetActive(false);

            // 2. Telefon ekranını gizle ki masa görünsün
            if (phonePanel != null) phonePanel.SetActive(false);
            else gameObject.SetActive(false);

            // 3. Masayı aç (Böylece içindeki SlideBarGame hiyerarşide aktifleşebilir)
            if (drawingDeskPanel != null) drawingDeskPanel.SetActive(true);

            // 4. Mini oyunu poster görseli ve zorluk hızıyla başlat
            if (slideBarGame != null)
            {
                slideBarGame.gameObject.SetActive(true);
                slideBarGame.Setup(_selectedProduct.slideSpeedMultiplier, _selectedProduct.icon);
            }
        }

        private void HandleSlideBarResult(bool isSuccess)
        {
            // 1. Masayı ve mini oyunu kapat
            if (slideBarGame != null) slideBarGame.gameObject.SetActive(false);
            if (drawingDeskPanel != null) drawingDeskPanel.SetActive(false);

            // 2. Telefon ekranını tekrar aç
            if (phonePanel != null) phonePanel.SetActive(true);
            gameObject.SetActive(true);

            if (_selectedProduct == null) return;

            if (isSuccess)
            {
                GameManager.Instance.EnergyManager.ConsumeEnergy(_selectedProduct.energyCost);
                GameManager.Instance.TimeManager.AdvanceTime(_selectedProduct.productionHours);

                var stock = GameManager.Instance.SaveManager.CurrentData.exhibitionStock;
                var existing = stock.Find(item => item.product == _selectedProduct);

                if (existing != null)
                {
                    existing.quantity++;
                }
                else
                {
                    stock.Add(new ExhibitionStockItem
                    {
                        product = _selectedProduct,
                        quantity = 1
                    });
                }

                GameManager.Instance.SaveManager.SaveGame();
                HUDManager.Instance?.RefreshAllUI();

                Debug.Log($"[Üretim] {_selectedProduct.productName} başarıyla üretildi!");
            }
            else
            {
                Debug.LogWarning("[Üretim] Çizim başarısız oldu.");
            }

            // İşlem bittiğinde seçimi temizle ve kartlardaki stok sayılarını yenile
            _selectedProduct = null;
            RefreshList();
        }
    }
}