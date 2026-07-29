using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Freeline
{
    public enum MarketCategory { Yemek, Dekorasyon, Upgrade }

    [System.Serializable]
    public class MarketItem
    {
        public string itemName;
        public string description;
        public int price;
        public MarketCategory category;
        public Color iconColor;
    }

    public class MarketPanel : MonoBehaviour
    {
        // ---- Hazır ürün listesi -------------------
        private static readonly MarketItem[] AllItems =
        {
            new MarketItem { itemName="Kahve",              description="Hız buff + enerji",    price=30,  category=MarketCategory.Yemek,      iconColor=new Color(0.6f,0.4f,0.2f) },
            new MarketItem { itemName="Hamburger",          description="Yüksek enerji",        price=50,  category=MarketCategory.Yemek,      iconColor=new Color(0.8f,0.5f,0.2f) },
            new MarketItem { itemName="Tatli",              description="Enerji + viral sans",  price=40,  category=MarketCategory.Yemek,      iconColor=new Color(0.9f,0.6f,0.7f) },
            new MarketItem { itemName="Enerji Icecegi",     description="Güçlü buff",           price=60,  category=MarketCategory.Yemek,      iconColor=new Color(0.3f,0.7f,0.9f) },
            new MarketItem { itemName="Tablet Upgrade",     description="Görev ücreti +",       price=500, category=MarketCategory.Upgrade,    iconColor=new Color(0.4f,0.6f,0.9f) },
            new MarketItem { itemName="Ergonomik Sandalye", description="Enerji tüketimi -",    price=350, category=MarketCategory.Upgrade,    iconColor=new Color(0.5f,0.5f,0.6f) },
        };

        // ---- SerializeField refs --------------------------------------------
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI coinDisplayText;
        [SerializeField] private Transform contentRoot;
        [SerializeField] private Button tabYemek;
        [SerializeField] private Button tabDekorasyon;
        [SerializeField] private Button tabUpgrade;
        [SerializeField] private Button backBtn;

        [Header("Decoration Sub-Menu (Yatay Kaydirma)")]
        [SerializeField] private GameObject decorationSubCategoryBar; // Oluşturduğumuz ScrollView Kapsayıcısı
        [SerializeField] private Button btnFloor;
        [SerializeField] private Button btnWall;
        [SerializeField] private Button btnWindowCushion;
        [SerializeField] private Button btnCurtain;
        [SerializeField] private Button btnRug;
        [SerializeField] private Button btnDesk;
        [SerializeField] private Button btnBookshelf;
        [SerializeField] private Button btnSofa;
        [SerializeField] private Button btnBed;
        [SerializeField] private Button btnAccessory;

        [Header("Databases & Managers")]
        [SerializeField] private DecorationCatalog decorationCatalog;
        [SerializeField] private RoomDecorationManager roomDecorationManager;
        [SerializeField] private MarketCardUI marketCardPrefab;

        // ---- Runtime state --------------------------------------------------
        private MarketCategory _activeCategory = MarketCategory.Yemek;
        private readonly List<(Button btn, int price)> _cardButtons = new();

        // =========================================================================
        // Runtime
        // =========================================================================

        void Awake()
        {
            if (backBtn != null) backBtn.onClick.AddListener(Close);
            if (tabYemek != null) tabYemek.onClick.AddListener(() => ShowCategory(MarketCategory.Yemek));
            if (tabDekorasyon != null) tabDekorasyon.onClick.AddListener(() => ShowCategory(MarketCategory.Dekorasyon));
            if (tabUpgrade != null) tabUpgrade.onClick.AddListener(() => ShowCategory(MarketCategory.Upgrade));

            // Alt kategori butonlarının tıklama olaylarını bağlıyoruz
            if (btnFloor != null) btnFloor.onClick.AddListener(() => ShowDecorationCategory(DecorationCategory.Floor));
            if (btnWall != null) btnWall.onClick.AddListener(() => ShowDecorationCategory(DecorationCategory.Wall));
            if (btnWindowCushion != null) btnWindowCushion.onClick.AddListener(() => ShowDecorationCategory(DecorationCategory.WindowCushion));
            if (btnCurtain != null) btnCurtain.onClick.AddListener(() => ShowDecorationCategory(DecorationCategory.Curtain));
            if (btnRug != null) btnRug.onClick.AddListener(() => ShowDecorationCategory(DecorationCategory.Rug));
            if (btnDesk != null) btnDesk.onClick.AddListener(() => ShowDecorationCategory(DecorationCategory.Desk));
            if (btnBookshelf != null) btnBookshelf.onClick.AddListener(() => ShowDecorationCategory(DecorationCategory.Bookshelf));
            if (btnSofa != null) btnSofa.onClick.AddListener(() => ShowDecorationCategory(DecorationCategory.Sofa));
            if (btnBed != null) btnBed.onClick.AddListener(() => ShowDecorationCategory(DecorationCategory.Bed));
            if (btnAccessory != null) btnAccessory.onClick.AddListener(() => ShowDecorationCategory(DecorationCategory.Accessory));
        }

        void OnEnable()
        {
            var sm = GameManager.Instance?.SaveManager;
            if (sm != null) sm.OnCoinsChanged += HandleCoinsChanged;
        }

        void OnDisable()
        {
            var sm = GameManager.Instance?.SaveManager;
            if (sm != null) sm.OnCoinsChanged -= HandleCoinsChanged;
        }

        public void Open()
        {
            gameObject.SetActive(true);
            RefreshCoinDisplay();
            ShowCategory(MarketCategory.Yemek); // Panel açılınca varsayılan olarak Yemek sekmesi gelsin
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        public void ShowCategory(MarketCategory cat)
        {
            _activeCategory = cat;

            // Eğer seçilen kategori Dekorasyon ise yatay menüyü aktif et, değilse gizle
            if (decorationSubCategoryBar != null)
            {
                decorationSubCategoryBar.SetActive(cat == MarketCategory.Dekorasyon);
            }

            if (contentRoot == null) return;

            ClearContent();

            int coins = CurrentCoins();

            if (cat == MarketCategory.Dekorasyon)
            {
                // Dekorasyon sekmesi ilk açıldığında varsayılan olarak 'Zemin' (Floor) kategorisini yükle
                ShowDecorationCategory(DecorationCategory.Floor);
            }
            else
            {
                foreach (var item in AllItems)
                {
                    if (item.category == cat)
                    {
                        BuildItemCard(item, coins);
                    }
                }
            }
        }

        private void ShowDecorationCategory(DecorationCategory category)
        {
            ClearContent();

            if (decorationCatalog == null) return;

            foreach (var item in decorationCatalog.GetByCategory(category))
            {
                BuildDecorationCard(item, CurrentCoins());
            }
        }

        private void ClearContent()
        {
            for (int i = contentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(contentRoot.GetChild(i).gameObject);
            }

            _cardButtons.Clear();
        }

        // ---- Coin refresh ---------------------------------------------------

        private void HandleCoinsChanged(int coins)
        {
            if (coinDisplayText != null)
                coinDisplayText.text = $"Coin: {coins}";
            RefreshBuyButtons(coins);
        }

        private void RefreshCoinDisplay()
        {
            if (coinDisplayText == null) return;
            coinDisplayText.text = $"Coin: {CurrentCoins()}";
        }

        private void RefreshBuyButtons(int coins)
        {
            foreach (var (btn, price) in _cardButtons)
            {
                if (btn != null)
                    btn.interactable = coins >= price;
            }
        }

        private int CurrentCoins() =>
            Mathf.FloorToInt(GameManager.Instance?.SaveManager?.CurrentData?.currentCoins ?? 0f);

        // ---- Kart oluşturma (STANDART EŞYALAR) --------------------------------

        private void BuildItemCard(MarketItem item, int currentCoins)
        {
            MarketCardUI card = CreateCard(
                item.itemName,
                item.description,
                item.price,
                null,
                item.iconColor);

            card.BuyButton.onClick.AddListener(() =>
            {
                Debug.Log(item.itemName);
            });

            _cardButtons.Add((card.BuyButton, item.price));
        }

        // ---- Kart oluşturma (DEKORASYON EŞYALARI) -----------------------------

        private void BuildDecorationCard(DecorationItemData decItem, int currentCoins)
        {
            string customDescription = "";

            if (decItem.bonusType != PassiveBonusType.None)
                customDescription += $"Etki: {decItem.bonusType}\n";

            customDescription += $"Kargo: {decItem.deliveryDays} Gün";

            MarketCardUI card = CreateCard(
                decItem.displayName,
                customDescription,
                decItem.price,
                decItem.shopIcon,
                Color.white);

            var capturedItem = decItem;

            card.BuyButton.onClick.AddListener(() =>
            {
                OnBuyClicked(
                    capturedItem.price,
                    card.BuyButton,
                    card.BuyButton.GetComponentInChildren<TextMeshProUGUI>(),
                    card.BuyButton.GetComponent<Image>(),
                    () =>
                    {
                        if (roomDecorationManager != null)
                        {
                            roomDecorationManager.EquipItem(capturedItem);
                            Debug.Log($"{capturedItem.displayName} odaya yerleştirildi!");
                        }
                    });
            });

            _cardButtons.Add((card.BuyButton, capturedItem.price));
        }

        // ---- Ortak Kart Tasarım Metodu ---------------------------------------

        private MarketCardUI CreateCard(
            string name,
            string desc,
            int price,
            Sprite icon,
            Color fallbackColor)
        {
            MarketCardUI card = Instantiate(marketCardPrefab, contentRoot);

            card.Setup(
                icon,
                fallbackColor,
                name,
                desc,
                price,
                CurrentCoins() >= price);

            return card;
        }

        private void OnBuyClicked(int price, Button btn, TextMeshProUGUI label, Image btnImg, System.Action onSuccessAction)
        {
            var sm = GameManager.Instance?.SaveManager;
            if (sm == null) return;
            if (Mathf.FloorToInt(sm.CurrentData.currentCoins) < price) return;

            sm.AddCoins(-price);

            onSuccessAction?.Invoke();

            StartCoroutine(PurchaseFeedback(btn, label, btnImg, price));
        }

        private IEnumerator PurchaseFeedback(Button btn, TextMeshProUGUI label, Image btnImg, int itemPrice)
        {
            string origText = label.text;
            Color origColor = btnImg.color;

            btn.interactable = false;
            label.text = "Alindi!";
            btnImg.color = new Color(0.15f, 0.50f, 0.60f, 1f);

            yield return new WaitForSeconds(1.2f);

            if (btn == null) yield break;

            label.text = origText;
            btnImg.color = origColor;
            btn.interactable = CurrentCoins() >= itemPrice;
        }
    }
}