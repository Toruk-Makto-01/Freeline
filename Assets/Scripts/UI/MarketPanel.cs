#if UNITY_EDITOR
using UnityEditor;
#endif

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
        public string         itemName;
        public string         description;
        public int            price;
        public MarketCategory category;
        public Color          iconColor;
    }

    public class MarketPanel : MonoBehaviour
    {
        // ---- Sabitler -------------------------------------------------------
        private const float HeaderH       = 80f;
        private const float CategoryBarH  = 60f;
        private const float BottomBarH    = 80f;
        private const float CardH         = 140f;
        private const float IconBoxW      = 120f;
        private const float BuyBtnW       = 120f;
        private const float TitleFontSize = 36f;
        private const float CoinFontSize  = 28f;
        private const float TabFontSize   = 24f;
        private const float NameFontSize  = 24f;
        private const float DescFontSize  = 20f;
        private const float PriceFontSize = 22f;
        private const float BuyFontSize   = 22f;
        private const float BackFontSize  = 28f;

        // ---- Hazır ürün listesi (Dekorasyonlar çıkarıldı) -------------------
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
        [SerializeField] private Transform       contentRoot;
        [SerializeField] private Button          tabYemek;
        [SerializeField] private Button          tabDekorasyon;
        [SerializeField] private Button          tabUpgrade;
        [SerializeField] private Button          backBtn;

        [Header("Databases & Managers")]
        [SerializeField] private DecorationCatalog decorationCatalog;
        [SerializeField] private RoomDecorationManager roomDecorationManager;

        // ---- Runtime state --------------------------------------------------
        private MarketCategory _activeCategory = MarketCategory.Yemek;
        private readonly List<(Button btn, int price)> _cardButtons = new();

        // =========================================================================
        // Runtime
        // =========================================================================

        void Awake()
        {
            if (backBtn        != null) backBtn.onClick.AddListener(Close);
            if (tabYemek       != null) tabYemek.onClick.AddListener(() => ShowCategory(MarketCategory.Yemek));
            if (tabDekorasyon  != null) tabDekorasyon.onClick.AddListener(() => ShowCategory(MarketCategory.Dekorasyon));
            if (tabUpgrade     != null) tabUpgrade.onClick.AddListener(() => ShowCategory(MarketCategory.Upgrade));
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
            if (contentRoot == null) return;

            for (int i = contentRoot.childCount - 1; i >= 0; i--)
            {
                var child = contentRoot.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
            _cardButtons.Clear();

            int coins = CurrentCoins();

            if (cat == MarketCategory.Dekorasyon)
            {
                if (decorationCatalog != null && decorationCatalog.allItems != null)
                {
                    foreach (var decItem in decorationCatalog.allItems)
                    {
                        BuildDecorationCard(decItem, coins);
                    }
                }
                else
                {
                    Debug.LogWarning("Decoration Catalog atanmamış veya boş!");
                }
            }
            else 
            {
                foreach (var item in AllItems)
                {
                    if (item.category == cat)
                        BuildItemCard(item, coins);
                }
            }
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
            var (buyBtn, buyTMP, buyImg) = CreateCardBase(item.itemName, item.description, item.price, currentCoins, item.iconColor, null);
            
            var capturedItem = item;
            buyBtn.onClick.AddListener(() => OnBuyClicked(capturedItem.price, buyBtn, buyTMP, buyImg, () => {
                Debug.Log($"{capturedItem.itemName} kullanıldı!");
            }));

            _cardButtons.Add((buyBtn, item.price));
        }

        // ---- Kart oluşturma (DEKORASYON EŞYALARI) -----------------------------

        private void BuildDecorationCard(DecorationItemData decItem, int currentCoins)
        {
            // ScriptableObject içerisinde description olmadığı için otomatik bir metin üretiyoruz
            string customDescription = "";
            if (decItem.bonusType != PassiveBonusType.None)
                customDescription += $"Etki: {decItem.bonusType}\n";
            customDescription += $"Kargo: {decItem.deliveryDays} Gün";

            var (buyBtn, buyTMP, buyImg) = CreateCardBase(decItem.displayName, customDescription, decItem.price, currentCoins, Color.white, decItem.shopIcon);
            
            var capturedItem = decItem;
            buyBtn.onClick.AddListener(() => OnBuyClicked(capturedItem.price, buyBtn, buyTMP, buyImg, () => {
                if (roomDecorationManager != null)
                {
                    roomDecorationManager.EquipItem(capturedItem);
                    Debug.Log($"{capturedItem.displayName} odaya yerleştirildi!");
                }
            }));

            _cardButtons.Add((buyBtn, decItem.price));
        }

        // ---- Ortak Kart Tasarım Metodu ---------------------------------------

        private (Button, TextMeshProUGUI, Image) CreateCardBase(string name, string desc, int price, int currentCoins, Color color, Sprite sprite)
        {
            bool canAfford = currentCoins >= price;

            var cardGO = NewUIObj("ItemCard_" + name, contentRoot);
            var cardRT = cardGO.GetComponent<RectTransform>();
            cardRT.sizeDelta = new Vector2(0f, CardH);
            cardGO.AddComponent<Image>().color = new Color(0.14f, 0.14f, 0.18f, 1f);

            var iconGO = NewUIObj("IconBox", cardGO.transform);
            var iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0f, 0f);
            iconRT.anchorMax = new Vector2(0f, 1f);
            iconRT.offsetMin = new Vector2(8f, 8f);
            iconRT.offsetMax = new Vector2(IconBoxW + 8f, -8f);
            var img = iconGO.AddComponent<Image>();
            
            if (sprite != null) img.sprite = sprite;
            else img.color = color;

            var infoGO = NewUIObj("InfoBlock", cardGO.transform);
            var infoRT = infoGO.GetComponent<RectTransform>();
            infoRT.anchorMin = new Vector2(0f, 0f);
            infoRT.anchorMax = new Vector2(1f, 1f);
            infoRT.offsetMin = new Vector2(IconBoxW + 16f, 4f);
            infoRT.offsetMax = new Vector2(-(BuyBtnW + 16f), -4f);
            infoGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0f);

            MakeInfoText("ItemNameText", infoGO.transform, new Vector2(0f, 0.55f), new Vector2(1f, 1f), name, NameFontSize, FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft);
            MakeInfoText("DescText", infoGO.transform, new Vector2(0f, 0.22f), new Vector2(1f, 0.55f), desc, DescFontSize, FontStyles.Normal, new Color(0.68f, 0.68f, 0.68f, 1f), TextAlignmentOptions.MidlineLeft);
            MakeInfoText("PriceText", infoGO.transform, new Vector2(0f, 0f), new Vector2(1f, 0.22f), $"{price} coin", PriceFontSize, FontStyles.Normal, new Color(1f, 0.85f, 0.2f, 1f), TextAlignmentOptions.MidlineLeft);

            var buyGO = NewUIObj("BuyBtn", cardGO.transform);
            var buyRT = buyGO.GetComponent<RectTransform>();
            buyRT.anchorMin = new Vector2(1f, 0f);
            buyRT.anchorMax = new Vector2(1f, 1f);
            buyRT.offsetMin = new Vector2(-(BuyBtnW + 8f), 8f);
            buyRT.offsetMax = new Vector2(-8f, -8f);

            var buyImg = buyGO.AddComponent<Image>();
            buyImg.color = canAfford ? new Color(0.25f, 0.65f, 0.30f, 1f) : new Color(0.30f, 0.30f, 0.30f, 1f);

            var buyBtn = buyGO.AddComponent<Button>();
            buyBtn.interactable = canAfford;

            var buyLabelGO = NewUIObj("Label", buyGO.transform);
            var buyLabelRT = buyLabelGO.GetComponent<RectTransform>();
            buyLabelRT.anchorMin = Vector2.zero;
            buyLabelRT.anchorMax = Vector2.one;
            buyLabelRT.offsetMin = Vector2.zero;
            buyLabelRT.offsetMax = Vector2.zero;

            var buyTMP = buyLabelGO.AddComponent<TextMeshProUGUI>();
            buyTMP.text = "Satin Al";
            buyTMP.fontSize = BuyFontSize;
            buyTMP.fontStyle = FontStyles.Bold;
            buyTMP.color = Color.white;
            buyTMP.alignment = TextAlignmentOptions.Center;
            buyTMP.raycastTarget = false;

            return (buyBtn, buyTMP, buyImg);
        }

        private static TextMeshProUGUI MakeInfoText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, string text, float fontSize, FontStyles style, Color color, TextAlignmentOptions align)
        {
            var go = NewUIObj(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
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
            string origText  = label.text;
            Color  origColor = btnImg.color;

            btn.interactable = false;
            label.text       = "Alindi!";
            btnImg.color     = new Color(0.15f, 0.50f, 0.60f, 1f);

            yield return new WaitForSeconds(1.2f);

            if (btn == null) yield break;

            label.text   = origText;
            btnImg.color = origColor;
            btn.interactable = CurrentCoins() >= itemPrice;
        }

        private static GameObject NewUIObj(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

#if UNITY_EDITOR

        [ContextMenu("Build Market Hierarchy")]
        private void BuildMarketHierarchy()
        {
            while (transform.childCount > 0)
                DestroyImmediate(transform.GetChild(0).gameObject);

            var selfRT       = GetComponent<RectTransform>();
            selfRT.anchorMin = Vector2.zero;
            selfRT.anchorMax = Vector2.one;
            selfRT.offsetMin = Vector2.zero;
            selfRT.offsetMax = Vector2.zero;

            var selfImg   = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            selfImg.color = new Color(0.10f, 0.10f, 0.14f, 1f);

            BuildHeader();
            BuildCategoryBar();
            BuildScrollView();
            BuildBottomBar();

            gameObject.SetActive(false);

            EditorUtility.SetDirty(gameObject);
            Debug.Log("[MarketPanel] Hierarchy built.");
        }

        private void BuildHeader()
        {
            var go = NewUIObj("Header", transform);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -HeaderH);
            rt.offsetMax = Vector2.zero;

            var img   = go.AddComponent<Image>();
            img.color = new Color(0.96f, 0.76f, 0.80f, 1f);

            var titleGO = NewUIObj("TitleText", go.transform);
            var titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0f,   0f);
            titleRT.anchorMax = new Vector2(0.55f, 1f);
            titleRT.offsetMin = new Vector2(20f, 0f);
            titleRT.offsetMax = Vector2.zero;

            var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            titleTMP.text          = "Market";
            titleTMP.fontSize      = TitleFontSize;
            titleTMP.fontStyle     = FontStyles.Bold;
            titleTMP.color         = new Color(0.20f, 0.08f, 0.12f, 1f);
            titleTMP.alignment     = TextAlignmentOptions.MidlineLeft;
            titleTMP.raycastTarget = false;

            var coinGO = NewUIObj("CoinDisplay", go.transform);
            var coinRT = coinGO.GetComponent<RectTransform>();
            coinRT.anchorMin = new Vector2(0.55f, 0f);
            coinRT.anchorMax = new Vector2(1f,    1f);
            coinRT.offsetMin = Vector2.zero;
            coinRT.offsetMax = new Vector2(-16f,  0f);

            coinDisplayText           = coinGO.AddComponent<TextMeshProUGUI>();
            coinDisplayText.text      = "Coin: 0";
            coinDisplayText.fontSize  = CoinFontSize;
            coinDisplayText.color     = new Color(0.20f, 0.08f, 0.12f, 1f);
            coinDisplayText.alignment = TextAlignmentOptions.MidlineRight;
            coinDisplayText.raycastTarget = false;
        }

        private void BuildCategoryBar()
        {
            var go = NewUIObj("CategoryBar", transform);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -(HeaderH + CategoryBarH));
            rt.offsetMax = new Vector2(0f, -HeaderH);

            var img   = go.AddComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.16f, 1f);

            tabYemek      = BuildTabBtn("TabBtn_Yemek",      go.transform, 0f,      1f/3f, "Yemek/Icecek");
            tabDekorasyon = BuildTabBtn("TabBtn_Dekorasyon", go.transform, 1f/3f,   2f/3f, "Dekorasyon");
            tabUpgrade    = BuildTabBtn("TabBtn_Upgrade",    go.transform, 2f/3f,   1f,    "Upgrade");
        }

        private static Button BuildTabBtn(string objName, Transform parent, float xMin, float xMax, string label)
        {
            var go = NewUIObj(objName, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, 0f);
            rt.anchorMax = new Vector2(xMax, 1f);
            rt.offsetMin = new Vector2( 2f,  4f);
            rt.offsetMax = new Vector2(-2f, -4f);

            var img   = go.AddComponent<Image>();
            img.color = new Color(0.22f, 0.22f, 0.28f, 1f);

            var btn = go.AddComponent<Button>();

            var labelGO = NewUIObj("Label", go.transform);
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;

            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text          = label;
            tmp.fontSize      = TabFontSize;
            tmp.color         = Color.white;
            tmp.alignment     = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            return btn;
        }

        private void BuildScrollView()
        {
            var go = NewUIObj("ScrollView", transform);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(0f,  BottomBarH);
            rt.offsetMax = new Vector2(0f, -(HeaderH + CategoryBarH));

            var maskImg   = go.AddComponent<Image>();
            maskImg.color = new Color(1f, 1f, 1f, 0.01f);
            go.AddComponent<Mask>().showMaskGraphic = false;

            var scrollRect            = go.AddComponent<ScrollRect>();
            scrollRect.horizontal     = false;
            scrollRect.vertical       = true;
            scrollRect.scrollSensitivity = 30f;

            var contentGO = NewUIObj("Content", go.transform);
            var contentRT = contentGO.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot     = new Vector2(0.5f, 1f);
            contentRT.offsetMin = Vector2.zero;
            contentRT.offsetMax = Vector2.zero;

            var vlg                    = contentGO.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = false;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing                = 4f;
            vlg.padding                = new RectOffset(8, 8, 8, 8);

            var csf              = contentGO.AddComponent<ContentSizeFitter>();
            csf.verticalFit      = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRT;
            contentRoot        = contentGO.transform;
        }

        private void BuildBottomBar()
        {
            var go = NewUIObj("BottomBar", transform);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = new Vector2(0f, BottomBarH);

            var img   = go.AddComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.16f, 1f);

            var btnGO = NewUIObj("BackBtn", go.transform);
            var btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.anchorMin        = new Vector2(0.5f, 0.5f);
            btnRT.anchorMax        = new Vector2(0.5f, 0.5f);
            btnRT.pivot            = new Vector2(0.5f, 0.5f);
            btnRT.sizeDelta        = new Vector2(240f, BottomBarH - 20f);
            btnRT.anchoredPosition = Vector2.zero;

            var btnImg   = btnGO.AddComponent<Image>();
            btnImg.color = new Color(0.35f, 0.20f, 0.20f, 1f);

            backBtn = btnGO.AddComponent<Button>();

            var labelGO = NewUIObj("Label", btnGO.transform);
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;

            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text          = "<- Geri";
            tmp.fontSize      = BackFontSize;
            tmp.fontStyle     = FontStyles.Bold;
            tmp.color         = Color.white;
            tmp.alignment     = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }
#endif
    }
}