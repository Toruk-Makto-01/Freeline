using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Freeline
{
    public enum MarketCategory { Decoration, Food, Energy, CoinGem, Upgrade }

    [System.Serializable]
    public class DecorationPageBinding
    {
        public DecorationCategory category;
        public Transform contentTransform;
    }

    public class MarketPanel : MonoBehaviour
    {
        [Header("Üst Bilgi Çubuğu")]
        [SerializeField] private TextMeshProUGUI energyText;
        [SerializeField] private TextMeshProUGUI hungerText;
        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private TextMeshProUGUI gemText;

        [Header("Alt Sekmeler")]
        [SerializeField] private Button tabDecoration;
        [SerializeField] private Button tabFood;
        [SerializeField] private Button tabEnergy;
        [SerializeField] private Button tabCoinGem;
        [SerializeField] private Button closeBtn;

        [Header("Ana Paneller")]
        [SerializeField] private GameObject decorationPanel;
        [SerializeField] private GameObject foodPanel;
        [SerializeField] private GameObject energyPanel;
        [SerializeField] private GameObject coinGemPanel;

        [Header("Dekorasyon Ayarları")]
        [SerializeField] private DecorationCatalog decorationCatalog;
        [SerializeField] private MarketCardUI cardPrefab;
        [SerializeField] private Sprite boughtButtonSprite;
        [SerializeField] private List<DecorationPageBinding> decorationPages = new();

        [Header("Tüketilebilir Ürün (Yemek/Enerji) Ayarları")]
        [SerializeField] private ConsumableCatalog consumableCatalog;
        [SerializeField] private Transform foodContent;
        [SerializeField] private Transform energyContent;
        [SerializeField] private GameObject limitWarningPopup;

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
            if (_roomDecoManager == null) _roomDecoManager = FindAnyObjectByType<RoomDecorationManager>(FindObjectsInactive.Include);

            CheckDailyReset();
            UpdateTopBar();
            ShowCategory(MarketCategory.Decoration);
        }

        public void Close() => gameObject.SetActive(false);

        private void CheckDailyReset()
        {
            var saveData = GameManager.Instance.SaveManager.CurrentData;
            if (saveData.currentDay > saveData.lastMarketResetDay)
            {
                saveData.lastMarketResetDay = saveData.currentDay;
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

            // Eski kartları temizle
            foreach (var card in _spawnedCards)
            {
                if (card != null) Destroy(card.gameObject);
            }
            _spawnedCards.Clear();

            switch (cat)
            {
                case MarketCategory.Decoration:
                    if (decorationPanel != null) decorationPanel.SetActive(true);
                    LoadDecorations();
                    break;
                case MarketCategory.Food:
                    if (foodPanel != null) foodPanel.SetActive(true);
                    if (consumableCatalog != null) LoadConsumables(consumableCatalog.GetFoodItems(), foodContent);
                    break;
                case MarketCategory.Energy:
                    if (energyPanel != null) energyPanel.SetActive(true);
                    if (consumableCatalog != null) LoadConsumables(consumableCatalog.GetEnergyItems(), energyContent);
                    break;
                case MarketCategory.CoinGem:
                    if (coinGemPanel != null) coinGemPanel.SetActive(true);
                    break;
            }
        }

        private void UpdateTopBar()
        {
            var data = GameManager.Instance.SaveManager.CurrentData;
            var energyManager = GameManager.Instance.EnergyManager;

            if (energyText != null) energyText.text = Mathf.FloorToInt(energyManager.CurrentEnergy).ToString();
            if (coinText != null) coinText.text = Mathf.FloorToInt(data.currentCoins).ToString();
            if (gemText != null) gemText.text = data.currentGems.ToString();

            // --- YÜZDELİK AÇLIK SİSTEMİ ---
            if (hungerText != null)
            {
                // Artık matematiği biz yapmıyoruz, merkezden (EnergyManager) hazır alıyoruz!
                int hungerPercent = energyManager.GetHungerPercentage();

                string colorHex = hungerPercent <= 25 ? "red" : "white";
                hungerText.text = $"<color={colorHex}>%{hungerPercent}</color>";
            }
        }

        // ==================================================
        // --- DEKORASYON SİSTEMİ (ESKİ KUSURSUZ HALİ) ---
        // ==================================================
        private void LoadDecorations()
        {
            if (decorationCatalog == null) return;

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

            // Kartın görsel ayarlarını yapıyoruz
            card.SetCardState(isOwned, isEquipped, decItem.price, currentCoins >= decItem.price, boughtButtonSprite);

            // Önceki tıklama komutlarını temizliyoruz
            card.BuyButton.onClick.RemoveAllListeners();

            // 1. DURUM: Eşya takılıysa (Equipped)
            if (isEquipped)
            {
                // NOT: Enum isimlerini (Floor, Wall vb.) kendi sistemine göre düzenlemeyi unutma!
                if (decItem.category == DecorationCategory.Floor ||
                    decItem.category == DecorationCategory.Wall ||
                    decItem.category == DecorationCategory.Bed ||
                    decItem.category == DecorationCategory.Desk)
                {
                    card.BuyButton.interactable = false; // Zemin/Duvar çıkarılamaz
                }
                else
                {
                    card.BuyButton.interactable = true; // Aksesuar vb. çıkarılabilir
                    card.BuyButton.onClick.AddListener(() => UnequipDecoration(decItem));
                }
            }
            // 2. DURUM: Eşya alınmış ama TAKILI DEĞİLSE (Owned)
            else if (isOwned)
            {
                card.BuyButton.interactable = true;
                card.BuyButton.onClick.AddListener(() => EquipDecoration(decItem));
            }
            // 3. DURUM: Eşya henüz SATIN ALINMAMIŞSA (Buy)
            else
            {
                // Paramız yetmese bile buton tıklanabilir olsun ki "Yetersiz Bakiye" logu düşebilsin
                card.BuyButton.interactable = true;
                card.BuyButton.onClick.AddListener(() => BuyDecoration(decItem));
            }
        }
        private void BuyDecoration(DecorationItemData item)
        {
            var sm = GameManager.Instance.SaveManager;

            Debug.Log($"Satın alma denemesi! Ürün Fiyatı: {item.price}, Senin Paran: {sm.CurrentData.currentCoins}");

            if (sm.CurrentData.currentCoins >= item.price)
            {
                sm.AddCoins(-item.price);
                sm.CurrentData.ownedDecorations.Add(item.itemId);
                Debug.Log($"{item.displayName} başarıyla satın alındı! Şimdi takılıyor...");
                EquipDecoration(item);
            }
            else
            {
                Debug.LogWarning("YETERSİZ BAKİYE! Satın alma işlemi iptal edildi.");
            }
        }

        private void EquipDecoration(DecorationItemData item)
        {
            Debug.Log($"{item.displayName} isimli eşya Equip (Takma) işlemine girdi!");

            if (_roomDecoManager != null)
            {
                var sm = GameManager.Instance.SaveManager;
                var equippedList = sm.CurrentData.equippedDecorations;

                if (item.category != DecorationCategory.Accessory)
                {
                    equippedList.RemoveAll(e => e.category == item.category);
                }

                equippedList.Add(new EquippedDecoration { category = item.category, itemId = item.itemId });
                sm.SaveGame();

                Debug.Log("Kayıt dosyasına eklendi. Şimdi görsel güncellenecek...");
                _roomDecoManager.EquipItemVisual(item);

                UpdateTopBar();
                ShowCategory(MarketCategory.Decoration);
            }
            else
            {
                Debug.LogError("DİKKAT: _roomDecoManager bulunamadığı için işlem iptal oldu!");
            }
        }

        private void UnequipDecoration(DecorationItemData item)
        {
            if (_roomDecoManager != null)
            {
                var sm = GameManager.Instance.SaveManager;
                var equippedList = sm.CurrentData.equippedDecorations;

                equippedList.RemoveAll(e => e.itemId == item.itemId);
                sm.SaveGame();

                _roomDecoManager.UnequipItemVisual(item);

                ShowCategory(MarketCategory.Decoration);
            }
        }

        // ==================================================
        // --- TÜKETİLEBİLİR YEMEK/ENERJİ SİSTEMİ ---
        // ==================================================
        private void LoadConsumables(List<ConsumableItemData> items, Transform targetContent)
        {
            if (targetContent == null) return;
            var saveData = GameManager.Instance.SaveManager.CurrentData;

            foreach (var item in items)
            {
                var card = Instantiate(cardPrefab, targetContent);
                _spawnedCards.Add(card);

                var record = saveData.dailyPurchases.FirstOrDefault(p => p.itemId == item.itemId);
                int boughtCount = record != null ? record.boughtCount : 0;
                bool isLimitReached = boughtCount >= item.dailyLimit;

                int remainingCount = item.dailyLimit - boughtCount;
                string limitString = $"{remainingCount}/{item.dailyLimit}";

                card.Setup(item.shopIcon, Color.white, item.displayName, item.description, limitString);
                card.SetCardState(false, false, item.price, true, null);

                if (isLimitReached && card.BuyButton.GetComponent<Image>() != null)
                {
                    card.BuyButton.GetComponent<Image>().color = new Color(0.7f, 0.7f, 0.7f, 1f);
                }

                card.BuyButton.onClick.RemoveAllListeners();
                card.BuyButton.onClick.AddListener(() => TryBuyConsumable(item, isLimitReached));
            }
        }

        private void TryBuyConsumable(ConsumableItemData item, bool isLimitReached)
        {
            if (isLimitReached)
            {
                if (limitWarningPopup != null) limitWarningPopup.SetActive(true);
                return;
            }

            var sm = GameManager.Instance.SaveManager;
            bool canAfford = item.currencyType == CurrencyType.Coin
                ? sm.CurrentData.currentCoins >= item.price
                : sm.CurrentData.currentGems >= item.price;

            if (!canAfford)
            {
                Debug.Log("Yetersiz Bakiye! İleride Yetersiz Bakiye Popup eklenecek.");
                return;
            }

            if (item.currencyType == CurrencyType.Coin) sm.AddCoins(-item.price);
            else if (item.currencyType == CurrencyType.Gem) sm.CurrentData.currentGems -= item.price;

            var record = sm.CurrentData.dailyPurchases.FirstOrDefault(p => p.itemId == item.itemId);
            if (record == null)
            {
                record = new DailyPurchaseRecord { itemId = item.itemId, boughtCount = 0 };
                sm.CurrentData.dailyPurchases.Add(record);
            }
            record.boughtCount++;

            // Eşyanın etkisini (Enerji verme, açlık giderme, buff) karaktere uygula!
            GameManager.Instance.EnergyManager.ApplyConsumable(item);

            sm.SaveGame();
            UpdateTopBar();

            if (foodPanel.activeSelf) ShowCategory(MarketCategory.Food);
            else if (energyPanel.activeSelf) ShowCategory(MarketCategory.Energy);
        }
    }
}