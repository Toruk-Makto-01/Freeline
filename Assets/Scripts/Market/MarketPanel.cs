using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

namespace Freeline
{
    public enum MarketCategory { Decoration, Food, Energy, CoinGem, Upgrade }

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
        [SerializeField] private Sprite boughtButtonSprite; // İsteğe bağlı panelden atanacak Satın Alındı görseli

        [Header("Dekorasyon Content Alanları (Izgaralar)")]
        [SerializeField] private Transform floorContent;
        [SerializeField] private Transform wallContent;
        [SerializeField] private Transform curtainContent;
        [SerializeField] private Transform bedContent;

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
            foreach (var card in _spawnedCards)
            {
                if (card != null) Destroy(card.gameObject);
            }
            _spawnedCards.Clear();

            if (decorationCatalog == null) return;

            LoadCategoryToContent(DecorationCategory.Floor, floorContent);
            LoadCategoryToContent(DecorationCategory.Wall, wallContent);
            LoadCategoryToContent(DecorationCategory.Curtain, curtainContent);
            LoadCategoryToContent(DecorationCategory.Bed, bedContent);
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

            // Temel bilgi ataması
            card.Setup(decItem.shopIcon, Color.white, decItem.displayName, "");
            
            // Kart durumunu güncelle (Outline, Buton Görseli ve Fiyat)
            card.SetCardState(isOwned, isEquipped, decItem.price, currentCoins >= decItem.price, boughtButtonSprite);

            card.BuyButton.onClick.RemoveAllListeners();

            if (isOwned)
            {
                // Ürün zaten alınmışsa tıklayınca odada tak (Equip)
                card.BuyButton.onClick.AddListener(() => EquipDecoration(decItem));
            }
            else
            {
                // Henüz alınmamışsa tıklayınca Satın Al (Buy)
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
                
                // Satın alınınca otomatik olarak odada takalım
                EquipDecoration(item);
            }
        }

        private void EquipDecoration(DecorationItemData item)
        {
            if (_roomDecoManager != null)
            {
                _roomDecoManager.EquipItem(item);

                var sm = GameManager.Instance.SaveManager;
                var equippedList = sm.CurrentData.equippedDecorations;

                // O kategoride takılı olan eski eşyayı çıkar, yenisini ekle
                equippedList.RemoveAll(e => e.category == item.category);
                equippedList.Add(new EquippedDecoration { category = item.category, itemId = item.itemId });
                sm.SaveGame();

                UpdateTopBar();
                LoadDecorations(); // Listeleri yenile ki her kategorideki yeşil çerçeve doğru eşyada yansın!
            }
        }
    }
}