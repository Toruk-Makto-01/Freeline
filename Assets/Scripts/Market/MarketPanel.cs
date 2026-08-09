using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq; // List işlemleri için eklendi

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
        // ---- Hazır ürün listesi (Yemek/Upgrade) -------------------
        private static readonly MarketItem[] AllItems =
        {
            new MarketItem { itemName="Kahve",              description="Hız buff + enerji",    price=30,  category=MarketCategory.Yemek,      iconColor=new Color(0.6f,0.4f,0.2f) },
            new MarketItem { itemName="Hamburger",          description="Yüksek enerji",        price=50,  category=MarketCategory.Yemek,      iconColor=new Color(0.8f,0.5f,0.2f) },
            new MarketItem { itemName="Tatli",              description="Enerji + viral sans",  price=40,  category=MarketCategory.Yemek,      iconColor=new Color(0.9f,0.6f,0.7f) },
            new MarketItem { itemName="Enerji Icecegi",     description="Güçlü buff",           price=60,  category=MarketCategory.Yemek,      iconColor=new Color(0.3f,0.7f,0.9f) },
            new MarketItem { itemName="Tablet Upgrade",     description="Görev ücreti +",       price=500, category=MarketCategory.Upgrade,    iconColor=new Color(0.4f,0.6f,0.9f) },
            new MarketItem { itemName="Ergonomik Sandalye", description="Enerji tüketimi -",    price=350, category=MarketCategory.Upgrade,    iconColor=new Color(0.5f,0.5f,0.6f) },
        };

        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI coinDisplayText;
        [SerializeField] private Transform contentRoot;
        [SerializeField] private Button tabYemek;
        [SerializeField] private Button tabDekorasyon;
        [SerializeField] private Button tabUpgrade;
        [SerializeField] private Button backBtn;

        [Header("Decoration Sub-Menu (Yatay Kaydirma)")]
        [SerializeField] private GameObject decorationSubCategoryBar;
        [SerializeField] private Button btnFloor;
        [SerializeField] private Button btnWall;
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

        private MarketCategory _activeCategory = MarketCategory.Yemek;
        private DecorationCategory _activeSubCategory = DecorationCategory.Floor;

        // Butonların durumunu güncelleyecek aksiyonları tuttuğumuz liste
        private readonly List<System.Action<int>> _coinRefreshActions = new();

        void Awake()
        {
            // 1. Püf Noktası: MarketPanel uyanır uyanmaz yöneticiyi sahnede kendi bulsun!
            if (roomDecorationManager == null)
            {
                // DEĞİŞEN KISIM: Kapalı/Gizli objeleri de bulması için parametre ekledik.
                roomDecorationManager = UnityEngine.Object.FindAnyObjectByType<RoomDecorationManager>(FindObjectsInactive.Include);
                
                if (roomDecorationManager == null)
                {
                    Debug.LogWarning("MarketPanel: Sahnede RoomDecorationManager hiçbir şekilde bulunamadı! Sahneye (Hierarchy) eklendiğinden emin ol.");
                }
            }

            // 2. Mevcut buton dinleyicileri (Zaten olan kısımlar)
            if (backBtn != null) backBtn.onClick.AddListener(Close);
            if (tabYemek != null) tabYemek.onClick.AddListener(() => ShowCategory(MarketCategory.Yemek));
            if (tabDekorasyon != null) tabDekorasyon.onClick.AddListener(() => ShowCategory(MarketCategory.Dekorasyon));
            if (tabUpgrade != null) tabUpgrade.onClick.AddListener(() => ShowCategory(MarketCategory.Upgrade));

            if (btnFloor != null) btnFloor.onClick.AddListener(() => ShowDecorationCategory(DecorationCategory.Floor));
            if (btnWall != null) btnWall.onClick.AddListener(() => ShowDecorationCategory(DecorationCategory.Wall));
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
            ShowCategory(MarketCategory.Yemek);
        }

        public void Close() => gameObject.SetActive(false);

        public void ShowCategory(MarketCategory cat)
        {
            _activeCategory = cat;
            if (decorationSubCategoryBar != null)
                decorationSubCategoryBar.SetActive(cat == MarketCategory.Dekorasyon);

            if (cat == MarketCategory.Dekorasyon)
            {
                ShowDecorationCategory(_activeSubCategory);
            }
            else
            {
                ClearContent();
                int coins = CurrentCoins();
                foreach (var item in AllItems.Where(i => i.category == cat))
                {
                    BuildItemCard(item, coins);
                }
            }
        }

        private void ShowDecorationCategory(DecorationCategory category)
        {
            _activeSubCategory = category;
            ClearContent();

            if (decorationCatalog == null) return;

            int coins = CurrentCoins();
            foreach (var item in decorationCatalog.GetByCategory(category))
            {
                BuildDecorationCard(item, coins);
            }
        }

        private void ClearContent()
        {
            for (int i = contentRoot.childCount - 1; i >= 0; i--)
                Destroy(contentRoot.GetChild(i).gameObject);

            _coinRefreshActions.Clear();
        }

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
            foreach (var action in _coinRefreshActions)
            {
                action?.Invoke(coins);
            }
        }

        private int CurrentCoins() =>
            Mathf.FloorToInt(GameManager.Instance?.SaveManager?.CurrentData?.currentCoins ?? 0f);

        // ---- YEMEK & UPGRADE KARTLARI (Tüketilebilir) ----
        private void BuildItemCard(MarketItem item, int currentCoins)
        {
            MarketCardUI card = Instantiate(marketCardPrefab, contentRoot);
            card.Setup(null, item.iconColor, item.itemName, item.description, item.price);

            // Tüketilebilir eşyalar için buton durumu sadece paraya bağlıdır
            _coinRefreshActions.Add((coins) =>
            {
                card.BuyButton.interactable = coins >= item.price;
                card.BuyButton.GetComponentInChildren<TextMeshProUGUI>().text = "Satin Al";
            });

            // İlk durumu ayarla
            card.BuyButton.interactable = currentCoins >= item.price;

            card.BuyButton.onClick.AddListener(() =>
            {
                if (CurrentCoins() >= item.price)
                {
                    GameManager.Instance.SaveManager.AddCoins(-item.price);
                    Debug.Log($"{item.itemName} Satin Alindi!");
                    StartCoroutine(PurchaseFeedback(card.BuyButton, card.BuyButton.GetComponentInChildren<TextMeshProUGUI>(), card.BuyButton.GetComponent<Image>(), item.price));
                }
            });
        }

        // ---- DEKORASYON KARTLARI (Kalıcı Envanter) ----
        private void BuildDecorationCard(DecorationItemData decItem, int currentCoins)
        {
            string customDescription = "";
            if (decItem.bonusType != PassiveBonusType.None)
                customDescription += $"Etki: {decItem.bonusType}\n";
            customDescription += $"Kargo: {decItem.deliveryDays} Gün";

            MarketCardUI card = Instantiate(marketCardPrefab, contentRoot);
            card.Setup(decItem.shopIcon, Color.white, decItem.displayName, customDescription, decItem.price);

            // Kartın görsel durumunu güncelleyen bir Action ekliyoruz
            System.Action<int> updateCardVisuals = (coins) =>
            {
                var data = GameManager.Instance.SaveManager.CurrentData;
                bool isOwned = data.ownedDecorations.Contains(decItem.itemId);
                bool isEquipped = data.equippedDecorations.Any(eq => eq.itemId == decItem.itemId);

                card.SetButtonState(isOwned, isEquipped, coins, decItem.price);
            };

            // Listeye ekle ve hemen ilk durumu çalıştır
            _coinRefreshActions.Add(updateCardVisuals);
            updateCardVisuals(currentCoins);

            card.BuyButton.onClick.AddListener(() =>
            {
                var data = GameManager.Instance.SaveManager.CurrentData;
                bool isOwned = data.ownedDecorations.Contains(decItem.itemId);

                if (!isOwned)
                {
                    // 1. SATIN ALMA İŞLEMİ
                    if (CurrentCoins() >= decItem.price)
                    {
                        GameManager.Instance.SaveManager.AddCoins(-decItem.price);
                        data.ownedDecorations.Add(decItem.itemId);

                        // Ekranı tazele ki buton "Kullan"a dönüşsün
                        RefreshBuyButtons(CurrentCoins());
                    }
                }
                else
                {
                    // 2. KULLANMA / ODAYA YERLEŞTİRME İŞLEMİ
                    Debug.Log($"---> [{decItem.displayName}] icin Kullan butonuna tiklandi!");

                    if (roomDecorationManager != null)
                    {
                        Debug.Log("---> RoomDecorationManager bulundu, esya odaya gonderiliyor...");
                        roomDecorationManager.EquipItem(decItem);

                        Debug.Log("---> Esya SaveData listesine ekleniyor...");
                        data.equippedDecorations.RemoveAll(x => x.category == decItem.category);
                        data.equippedDecorations.Add(new EquippedDecoration { category = decItem.category, itemId = decItem.itemId });

                        Debug.Log("---> UI listesi yenileniyor...");
                        ShowDecorationCategory(_activeSubCategory);
                    }
                    else
                    {
                        Debug.LogError("---> HATA: roomDecorationManager referansi NULL! (Inspector'da bos kalmis veya silinmis)");
                    }
                }
            });
        }

        // Tüketilebilir eşyalar (Yemek vs.) için 1 saniyelik görsel feedback coroutine'i
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