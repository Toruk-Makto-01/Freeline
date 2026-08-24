using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

namespace Freeline
{
    public enum MarketCategory { Decoration, Food, Energy, CoinGem, Upgrade }

    // Inspector'dan Kategori ile UI Content'ini eşleştirmek için yardımcı sınıf
    [System.Serializable]
    public class DecorationPageBinding
    {
        public DecorationCategory category; // Örn: Carpet (Halı)
        public Transform contentTransform;   // HalıList > Viewport > Content
    }

    public class MarketPanel : MonoBehaviour
    {
        [Header("Üst Bilgi Çubuğu (Top Bar)")]
        [SerializeField] private TextMeshProUGUI energyText;
        [SerializeField] private TextMeshProUGUI hungerText;
        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private TextMeshProUGUI gemText;

        [Header("Alt Sekmeler (Bottom Tabs)")]
        [SerializeField] private Button tabDecoration;
        [SerializeField] private Button tabFood;
        [SerializeField] private Button tabEnergy;
        [SerializeField] private Button tabCoinGem;
        [SerializeField] private Button closeBtn;

        [Header("Ana Paneller (Sayfalar)")]
        [SerializeField] private GameObject decorationPanel;
        [SerializeField] private GameObject foodPanel;
        [SerializeField] private GameObject energyPanel;
        [SerializeField] private GameObject coinGemPanel;

        [Header("Dekorasyon Ayarları")]
        [SerializeField] private DecorationCatalog decorationCatalog;
        [SerializeField] private MarketCardUI cardPrefab;
        [SerializeField] private Sprite boughtButtonSprite;

        [Header("Dekorasyon Sayfaları Listesi (Esnek Yapı)")]
        [SerializeField] private List<DecorationPageBinding> decorationPages = new();

        private RoomDecorationManager _roomDecoManager;
        private List<MarketCardUI> _spawnedCards = new();

        private void Awake()
        {
            if (tabDecoration != null) tabDecoration.onClick.AddListener(() => ShowCategory(MarketCategory.Decoration));
            if (tabFood != null) tabFood.onClick.AddListener(() => ShowCategory(MarketCategory.Food));
            if (tabEnergy != null) tabEnergy.onClick.AddListener(() => ShowCategory(MarketCategory.Energy));
            if (tabCoinGem != null) tabCoinGem.onClick.AddListener(() => ShowCategory(MarketCategory.CoinGem));
            if (closeBtn != null) closeBtn.onClick.AddListener(Close);
        }

        public void Open()
        {
            gameObject.SetActive(true);

            if (_roomDecoManager == null)
                _roomDecoManager = FindAnyObjectByType<RoomDecorationManager>(FindObjectsInactive.Include);

            CheckDailyReset();
            UpdateTopBar();
            ShowCategory(MarketCategory.Decoration);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        private void CheckDailyReset()
        {
            var saveData = GameManager.Instance.SaveManager.CurrentData;
            string today = DateTime.Now.ToString("dd-MM-yyyy");

            if (saveData.lastRealTimeDate != today)
            {
                saveData.lastRealTimeDate = today;
                saveData.dailyPurchases.Clear();
                GameManager.Instance.SaveManager.SaveGame();
            }
        }

        public void ShowCategory(MarketCategory cat)
        {
            if (decorationPanel != null) decorationPanel.SetActive(false);
            if (foodPanel != null) foodPanel.SetActive(false);
            if (energyPanel != null) energyPanel.SetActive(false);
            if (coinGemPanel != null) coinGemPanel.SetActive(false);

            switch (cat)
            {
                case MarketCategory.Decoration:
                    if (decorationPanel != null) decorationPanel.SetActive(true);
                    LoadDecorations();
                    break;
                case MarketCategory.Food:
                    if (foodPanel != null) foodPanel.SetActive(true);
                    break;
                case MarketCategory.Energy:
                    if (energyPanel != null) energyPanel.SetActive(true);
                    break;
                case MarketCategory.CoinGem:
                    if (coinGemPanel != null) coinGemPanel.SetActive(true);
                    break;
            }
        }

        private void UpdateTopBar()
        {
            var data = GameManager.Instance.SaveManager.CurrentData;
            if (energyText != null) energyText.text = Mathf.FloorToInt(data.currentEnergy).ToString();
            if (coinText != null) coinText.text = Mathf.FloorToInt(data.currentCoins).ToString();
            if (gemText != null) gemText.text = data.currentGems.ToString();
        }

        private void LoadDecorations()
        {
            // Eski kartları temizle
            foreach (var card in _spawnedCards)
            {
                if (card != null) Destroy(card.gameObject);
            }
            _spawnedCards.Clear();

            if (decorationCatalog == null) return;

            // Bütün tanımlı sayfaları dinamik olarak döngüyle yükle!
            foreach (var pageBinding in decorationPages)
            {
                if (pageBinding.contentTransform != null)
                {
                    LoadCategoryToContent(pageBinding.category, pageBinding.contentTransform);
                }
            }
        }

        private void LoadCategoryToContent(DecorationCategory category, Transform targetContent)
        {
            if (targetContent == null) return;

            foreach (var item in decorationCatalog.GetByCategory(category))
            {
                BuildDecorationCard(item, targetContent);
            }
        }

        private void BuildDecorationCard(DecorationItemData decItem, Transform parentContent)
        {
            var card = Instantiate(cardPrefab, parentContent);
            _spawnedCards.Add(card);

            var saveData = GameManager.Instance.SaveManager.CurrentData;
            bool isOwned = saveData.ownedDecorations.Contains(decItem.itemId);
            bool isEquipped = saveData.equippedDecorations.Exists(e => e.itemId == decItem.itemId);
            int currentCoins = Mathf.FloorToInt(saveData.currentCoins);

            card.Setup(decItem.shopIcon, Color.white, decItem.displayName, "");
            card.SetCardState(isOwned, isEquipped, decItem.price, currentCoins >= decItem.price, boughtButtonSprite);

            card.BuyButton.onClick.RemoveAllListeners();

            if (isEquipped)
            {
                // NOT: Enum adın "Accessory" veya "Aksesuar" ise burayı ona göre değiştir!
                if (decItem.category == DecorationCategory.Accessory)
                {
                    // Takılı bir aksesuar ise tıklayıp çıkarabilmeli
                    card.BuyButton.interactable = true;
                    card.BuyButton.onClick.AddListener(() => UnequipDecoration(decItem));
                }
                else
                {
                    // Diğer kategorilerde takılıysa buton pasif kalsın
                    card.BuyButton.interactable = false;
                }
            }
            else if (isOwned)
            {
                card.BuyButton.onClick.AddListener(() => EquipDecoration(decItem));
            }
            else
            {
                card.BuyButton.onClick.AddListener(() => BuyDecoration(decItem));
            }
        }

        private void BuyDecoration(DecorationItemData item)
        {
            var sm = GameManager.Instance.SaveManager;
            if (sm.CurrentData.currentCoins >= item.price)
            {
                sm.AddCoins(-item.price);
                sm.CurrentData.ownedDecorations.Add(item.itemId);
                EquipDecoration(item);
            }
        }

        private void EquipDecoration(DecorationItemData item)
        {
            if (_roomDecoManager != null)
            {
                var sm = GameManager.Instance.SaveManager;
                var equippedList = sm.CurrentData.equippedDecorations;

                // EĞER AKSESUAR DEĞİLSE eski eşyayı listeden sil (Aksesuar ise karışma!)
                if (item.category != DecorationCategory.Accessory)
                {
                    equippedList.RemoveAll(e => e.category == item.category);
                }

                // Yeni eşyayı listeye ekle
                equippedList.Add(new EquippedDecoration { category = item.category, itemId = item.itemId });
                sm.SaveGame();

                _roomDecoManager.EquipItem(item); // Sahneyi güncelle
                UpdateTopBar();
                LoadDecorations();
            }
        }

        private void UnequipDecoration(DecorationItemData item)
        {
            if (_roomDecoManager != null)
            {
                var sm = GameManager.Instance.SaveManager;
                var equippedList = sm.CurrentData.equippedDecorations;

                // Seçilen aksesuarı takılılar listesinden sil
                equippedList.RemoveAll(e => e.itemId == item.itemId);
                sm.SaveGame();

                // İLERİDE EKLENECEK: Sahnede görseli gizleme komutu
                // _roomDecoManager.UnequipItem(item); 

                LoadDecorations(); // Arayüzü yenile (Yeşil çerçeve kalksın)
            }
        }
    }
}