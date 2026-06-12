// APARTMENT SCENE SETUP
// 1. Create new scene: File > New Scene > Basic (Built-in) → save as Assets/Scenes/Apartment.unity
// 2. Delete default Main Camera if present
// 3. Create empty GameObject → name it "HUD" → add HUDManager component
// 4. Right-click HUD in Inspector → "Build HUD Hierarchy"
// 5. Add scene to Build Settings (File > Build Settings > Add Open Scenes)

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.UI;

namespace Freeline
{
    public class HUDManager : MonoBehaviour
    {
        // Canvas
        private const float RefWidth      = 1080f;
        private const float RefHeight     = 1920f;

        // TopPanel
        private const float TopPanelH     = 320f;
        private const float BottomNavBarH = 200f;

        // ClockDial
        private const float ClockDialSize    = 280f;
        private const float ClockDialOffsetX = 20f;

        // StatBlock
        private const float StatBlockLeftEdge = 320f;  // ClockDialOffsetX + ClockDialSize + padding
        private const float Row1H             = 110f;
        private const float Row2H             = 80f;

        // EnergyBar / HungerBar
        private const float StatBarH          = 60f;

        // AddBtn (Row2)
        private const float AddBtnSize        = 48f;

        // Saat → derece çevirimi: 24 saat × 15° = 360° tam tur
        private const float HoursToDegreesMultiplier = 15f;

        // BottomNavBar
        private const float NavSlotW          = 216f;   // RefWidth / 5
        private const float NavBtnStdSize     = 160f;
        private const float NavBtnSleepSize   = 200f;
        private const float NavBtnSleepRise   = 60f;    // NavBtn_Sleep protrudes above BottomNavBar
        private const float NavLabelFontSize  = 28f;

        // -------------------------------------------------------------------------
        // SerializeField refs
        // -------------------------------------------------------------------------

        [SerializeField] private RectTransform topPanel;
        [SerializeField] private RectTransform bottomNavBar;

        [Header("Top Panel")]
        [SerializeField] private RectTransform clockDialRect;
        [SerializeField] private Image          clockHandImg;
        [SerializeField] private Image          energyBarFill;
        [SerializeField] private Image          hungerBarFill;
        [SerializeField] private TMPro.TextMeshProUGUI followerText;
        [SerializeField] private TMPro.TextMeshProUGUI coinText;
        [SerializeField] private TMPro.TextMeshProUGUI gemText;

        [Header("Bottom Nav Bar")]
        [SerializeField] private Button navBtnDraw;
        [SerializeField] private Button navBtnPhone;
        [SerializeField] private Button navBtnSleep;
        [SerializeField] private Button navBtnShop;
        [SerializeField] private Button navBtnHome;

        [Header("Panels")]
        [SerializeField] private PhonePanel phonePanel;

        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------

        public static HUDManager Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // =========================================================================
        // Runtime — data binding
        // =========================================================================

        void Start()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            gm.TimeManager.OnHourChanged             += UpdateClock;
            gm.EnergyManager.OnEnergyChanged         += UpdateEnergy;
            gm.EnergyManager.OnHungerChanged         += UpdateHunger;
            gm.SaveManager.OnCoinsChanged            += UpdateCoins;
            gm.SaveManager.OnGemsChanged             += UpdateGems;
            gm.WebtoonManager.OnFollowersChanged     += HandleFollowersChanged;

            RefreshAll();

            if (navBtnPhone != null && phonePanel != null)
                navBtnPhone.onClick.AddListener(phonePanel.Open);
        }

        void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            gm.TimeManager.OnHourChanged             -= UpdateClock;
            gm.EnergyManager.OnEnergyChanged         -= UpdateEnergy;
            gm.EnergyManager.OnHungerChanged         -= UpdateHunger;
            gm.SaveManager.OnCoinsChanged            -= UpdateCoins;
            gm.SaveManager.OnGemsChanged             -= UpdateGems;
            gm.WebtoonManager.OnFollowersChanged     -= HandleFollowersChanged;
        }

        // Tüm yöneticilerden mevcut değerleri okuyarak ilk ekran durumunu kurar.
        public void RefreshAll()
        {
            var gm = GameManager.Instance;
            if (gm?.TimeManager == null || gm.EnergyManager == null || gm.SaveManager?.CurrentData == null)
                return;

            UpdateClock(gm.TimeManager.CurrentHour);
            UpdateEnergy(gm.EnergyManager.CurrentEnergy, gm.EnergyManager.MaxEnergy);
            UpdateHunger(gm.EnergyManager.HoursSinceLastFood, gm.EnergyManager.MaxHungerHours);
            UpdateCoins(Mathf.FloorToInt(gm.SaveManager.CurrentData.currentCoins));
            UpdateGems(gm.SaveManager.CurrentData.currentGems);
            UpdateFollowers(Mathf.RoundToInt(gm.SaveManager.CurrentData.webtoonData.followers));
        }

        // Saati 0–24 aralığından 0–360° dönüşe çevirir; saat yönünün tersine döner.
        private void UpdateClock(float hour)
        {
            if (clockHandImg == null) return;
            clockHandImg.rectTransform.localRotation =
                Quaternion.Euler(0f, 0f, -hour * HoursToDegreesMultiplier);
        }

        private void UpdateEnergy(float energy, float max)
        {
            if (energyBarFill == null) return;
            var rt      = energyBarFill.rectTransform;
            rt.anchorMax = new Vector2(Mathf.Clamp01(energy / max), 1f);
        }

        private void UpdateHunger(float hunger, float max)
        {
            if (hungerBarFill == null) return;
            // Bar tersine çalışır: yeni yendi = dolu, aç = boş
            hungerBarFill.rectTransform.anchorMax =
                new Vector2(1f - Mathf.Clamp01(hunger / max), 1f);
        }

        // OnFollowersChanged imzası Action<float,float> (güncel, delta) — yalnızca güncel değer kullanılır.
        private void HandleFollowersChanged(float current, float _)
            => UpdateFollowers(Mathf.RoundToInt(current));

        private void UpdateFollowers(int count)
        {
            if (followerText == null) return;
            followerText.text = count >= 1000
                ? $"{count / 1000f:0.#}K"
                : count.ToString();
        }

        private void UpdateCoins(int coins)
        {
            if (coinText != null) coinText.text = coins.ToString();
        }

        private void UpdateGems(int gems)
        {
            if (gemText != null) gemText.text = gems.ToString();
        }

        // =========================================================================
        // EDITOR — hierarchy builders
        // =========================================================================

#if UNITY_EDITOR

        [ContextMenu("Build HUD Hierarchy")]
        private void BuildHUDHierarchy()
        {
            while (transform.childCount > 0)
                DestroyImmediate(transform.GetChild(0).gameObject);

            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
            scaler.matchWidthOrHeight  = 0.5f;

            if (gameObject.GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            EnsureCamera();

            topPanel     = CreatePanel("TopPanel",    anchorTop: true,  height: TopPanelH);
            bottomNavBar = CreatePanel("BottomNavBar", anchorTop: false, height: BottomNavBarH);

            topPanel.GetComponent<Image>().color     = new Color(0.12f, 0.12f, 0.18f, 1f);
            bottomNavBar.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.18f, 1f);

            BuildTopPanel();
            BuildBottomNavBar();
            BuildPhonePanelObject();

            EditorUtility.SetDirty(gameObject);
            Debug.Log("[HUD] Hierarchy built.");
        }

        // -------------------------------------------------------------------------

        private void BuildPhonePanelObject()
        {
            var go = new GameObject("PhonePanel", typeof(RectTransform));
            go.transform.SetParent(transform, false);

            // PhonePanel'in kendi RectTransform'u tam ekranı kaplar; PhonePanel.BuildPhoneHierarchy bunu ayarlar.
            var rt       = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            phonePanel = go.AddComponent<PhonePanel>();
            EditorUtility.SetDirty(go);
        }

        // -------------------------------------------------------------------------

        private static void EnsureCamera()
        {
            if (Camera.main != null) return;

            var camGO = new GameObject("Main Camera");
            var cam   = camGO.AddComponent<Camera>();
            cam.clearFlags       = CameraClearFlags.SolidColor;
            cam.backgroundColor  = new Color(0.1f, 0.1f, 0.1f);
            camGO.tag            = "MainCamera";
        }

        [ContextMenu("Build Top Panel")]
        private void BuildTopPanel()
        {
            if (topPanel == null)
            {
                Debug.LogError("[HUD] topPanel ref is null — run Build HUD Hierarchy first.");
                return;
            }

            // Temizle
            while (topPanel.childCount > 0)
                DestroyImmediate(topPanel.GetChild(0).gameObject);

            BuildClockDial();
            BuildStatBlock();

            EditorUtility.SetDirty(gameObject);
            Debug.Log("[HUD] TopPanel built.");
        }

        // ---- ClockDial ----------------------------------------------------------

        private void BuildClockDial()
        {
            var go = NewUIObject("ClockDial", topPanel);
            var rt = go.GetComponent<RectTransform>();

            // Kare, sol-orta hizalı
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot     = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(ClockDialSize, ClockDialSize);
            rt.anchoredPosition = new Vector2(ClockDialOffsetX, 0f);

            var bg   = go.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.20f, 0.35f, 1f);

            clockDialRect = rt;

            // Dönen ibre / gösterge
            var handGO  = NewUIObject("ClockHandImg", go.transform);
            var handRT  = handGO.GetComponent<RectTransform>();
            handRT.anchorMin = Vector2.zero;
            handRT.anchorMax = Vector2.one;
            handRT.offsetMin = Vector2.zero;
            handRT.offsetMax = Vector2.zero;

            clockHandImg       = handGO.AddComponent<Image>();
            clockHandImg.color = Color.white;
        }

        // ---- StatBlock ----------------------------------------------------------

        private void BuildStatBlock()
        {
            var go = NewUIObject("StatBlock", topPanel);
            var rt = go.GetComponent<RectTransform>();

            // ClockDial'ın sağından itibaren tam germe
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(StatBlockLeftEdge, 0f);
            rt.offsetMax = new Vector2(0f, 0f);

            var bg   = go.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0f);

            BuildRow1(go.transform);
            BuildRow2(go.transform);
        }

        // ---- Row1: EnergyBar | HungerBar | FollowerBox --------------------------

        private void BuildRow1(Transform statBlock)
        {
            var go = NewUIObject("Row1", statBlock);
            var rt = go.GetComponent<RectTransform>();

            // StatBlock'un üst kısmı
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -Row1H);
            rt.offsetMax = new Vector2(0f, 0f);

            var bg   = go.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0f);

            energyBarFill = BuildStatBar("EnergyBar", go.transform, 0.00f, 0.40f,
                new Color(0.2f,  0.2f,  0.2f,  1f),
                new Color(1f,    0.85f, 0.2f,  1f));
            hungerBarFill = BuildStatBar("HungerBar", go.transform, 0.40f, 0.80f,
                new Color(0.2f,  0.2f,  0.2f,  1f),
                new Color(1f,    0.55f, 0.2f,  1f));
            BuildFollowerBox(go.transform, 0.80f, 1.00f);
        }

        // Yatay dolum çubuğu: arka plan + iç Fill görüntüsü.
        // Unity Slider yerine manuel yapıdır; fill.sizeDelta.x runtime'da güncellenir.
        private Image BuildStatBar(string barName, Transform parent, float xMin, float xMax,
                                   Color bgColor, Color fillColor)
        {
            // Dış konteyner
            var outer   = NewUIObject(barName, parent);
            var outerRT = outer.GetComponent<RectTransform>();
            outerRT.anchorMin = new Vector2(xMin, 0.5f);
            outerRT.anchorMax = new Vector2(xMax, 0.5f);
            outerRT.pivot     = new Vector2(0.5f, 0.5f);
            outerRT.sizeDelta = new Vector2(0f, StatBarH);
            // Anchor aralığı genişliği zaten yüzde tabanlı; sadece yükseklik sabitlenir.
            outerRT.anchorMin = new Vector2(xMin, 0f);
            outerRT.anchorMax = new Vector2(xMax, 1f);
            outerRT.offsetMin = new Vector2(8f,  (Row1H - StatBarH) * 0.5f);
            outerRT.offsetMax = new Vector2(-8f, -(Row1H - StatBarH) * 0.5f);

            var bgImg   = outer.AddComponent<Image>();
            bgImg.color = bgColor;

            // Fill çocuğu — sol kenara pivot'lu, runtime'da genişlik değiştirilir
            var fill   = NewUIObject("Fill", outer.transform);
            var fillRT = fill.GetComponent<RectTransform>();
            fillRT.anchorMin        = new Vector2(0f, 0f);
            fillRT.anchorMax        = new Vector2(1f, 1f);
            fillRT.offsetMin        = Vector2.zero;
            fillRT.offsetMax        = Vector2.zero;
            fillRT.pivot            = new Vector2(0f, 0.5f);

            var fillImg   = fill.AddComponent<Image>();
            fillImg.color = fillColor;

            return fillImg;
        }

        private void BuildFollowerBox(Transform parent, float xMin, float xMax)
        {
            var go = NewUIObject("FollowerBox", parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, 0f);
            rt.anchorMax = new Vector2(xMax, 1f);
            rt.offsetMin = new Vector2(4f, 4f);
            rt.offsetMax = new Vector2(-4f, -4f);

            var img   = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.15f, 0.35f, 1f);

            // İki figür ikonu — ileride sprite atanacak yer tutucular
            NewIconSlot("FigureIcon1", go.transform, new Vector2(0f, 0f), new Vector2(0.4f, 1f));
            NewIconSlot("FigureIcon2", go.transform, new Vector2(0.4f, 0f), new Vector2(0.7f, 1f));

            // Takipçi sayısı metni
            var textGO = NewUIObject("FollowerText", go.transform);
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            followerText           = textGO.AddComponent<TMPro.TextMeshProUGUI>();
            followerText.text      = "0";
            followerText.fontSize  = 24f;
            followerText.color     = Color.white;
            followerText.alignment = TMPro.TextAlignmentOptions.Center;
            followerText.raycastTarget = false;
        }

        // ---- Row2: CoinDisplay | CoinAddBtn | GemDisplay | GemAddBtn | BalanceBox

        private void BuildRow2(Transform statBlock)
        {
            var go = NewUIObject("Row2", statBlock);
            var rt = go.GetComponent<RectTransform>();

            // StatBlock'un alt kısmı
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(0f, 0f);
            rt.offsetMax = new Vector2(0f, Row2H);

            var bg   = go.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0f);

            // Coin
            coinText = BuildCurrencyText("CoinDisplay", go.transform, 0.00f, 0.22f);
            BuildAddButton("CoinAddBtn",  go.transform, 0.22f, 0.32f);

            // Gem
            gemText  = BuildCurrencyText("GemDisplay",  go.transform, 0.32f, 0.54f);
            BuildAddButton("GemAddBtn",   go.transform, 0.54f, 0.64f);

            // Kalan alan
            var balance   = NewUIObject("BalanceBox", go.transform);
            var balanceRT = balance.GetComponent<RectTransform>();
            balanceRT.anchorMin = new Vector2(0.64f, 0f);
            balanceRT.anchorMax = new Vector2(1.00f, 1f);
            balanceRT.offsetMin = new Vector2(4f, 4f);
            balanceRT.offsetMax = new Vector2(-4f, -4f);

            var balanceImg   = balance.AddComponent<Image>();
            balanceImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        }

        private static TMPro.TextMeshProUGUI BuildCurrencyText(
            string objName, Transform parent, float xMin, float xMax)
        {
            var go = NewUIObject(objName, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, 0f);
            rt.anchorMax = new Vector2(xMax, 1f);
            rt.offsetMin = new Vector2(6f, 4f);
            rt.offsetMax = new Vector2(-2f, -4f);

            var tmp = go.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text           = "0";
            tmp.fontSize       = 36f;
            tmp.color          = Color.white;
            tmp.alignment      = TMPro.TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget  = false;
            return tmp;
        }

        private static void BuildAddButton(string btnName, Transform parent, float xMin, float xMax)
        {
            var go = NewUIObject(btnName, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, 0.5f);
            rt.anchorMax = new Vector2(xMax, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            // En az 48×48px dokunma hedefi
            rt.sizeDelta = new Vector2(
                Mathf.Max(AddBtnSize, (xMax - xMin) * RefWidth),
                AddBtnSize);

            var img   = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);

            go.AddComponent<Button>();

            var label = NewUIObject("Label", go.transform);
            var labelRT = label.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;

            var tmp = label.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text          = "+";
            tmp.fontSize      = 30f;
            tmp.color         = Color.white;
            tmp.alignment     = TMPro.TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }

        // ---- BottomNavBar -------------------------------------------------------

        [ContextMenu("Build Bottom Nav Bar")]
        private void BuildBottomNavBar()
        {
            if (bottomNavBar == null)
            {
                Debug.LogError("[HUD] bottomNavBar ref is null — run Build HUD Hierarchy first.");
                return;
            }

            while (bottomNavBar.childCount > 0)
                DestroyImmediate(bottomNavBar.GetChild(0).gameObject);

            // 5 eşit slot; Sleep merkez (slot 2, index 2)
            navBtnDraw  = BuildNavBtn("NavBtn_Draw",  bottomNavBar, slotIndex: 0, label: "Draw",  isSleep: false);
            navBtnPhone = BuildNavBtn("NavBtn_Phone", bottomNavBar, slotIndex: 1, label: "Phone", isSleep: false);
            navBtnSleep = BuildNavBtn("NavBtn_Sleep", bottomNavBar, slotIndex: 2, label: "Sleep", isSleep: true);
            navBtnShop  = BuildNavBtn("NavBtn_Shop",  bottomNavBar, slotIndex: 3, label: "Shop",  isSleep: false);
            navBtnHome  = BuildNavBtn("NavBtn_Home",  bottomNavBar, slotIndex: 4, label: "Home",  isSleep: false);

            EditorUtility.SetDirty(gameObject);
            Debug.Log("[HUD] BottomNavBar built.");
        }

        // Tek bir nav düğmesi oluşturur.
        // Standart düğmeler BottomNavBar içinde dikey olarak ortalanır (160×160px).
        // Sleep düğmesi NavBtnSleepRise kadar yukarı taşar (200×200px).
        private static Button BuildNavBtn(string objName, RectTransform parent,
                                          int slotIndex, string label, bool isSleep)
        {
            float size     = isSleep ? NavBtnSleepSize : NavBtnStdSize;
            float slotCentreX = (slotIndex + 0.5f) * NavSlotW; // slot ortasının x'i (piksel)

            var go = NewUIObject(objName, parent);
            var rt = go.GetComponent<RectTransform>();

            // Merkez-pivot, alt-kenar hizalaması: Sleep yukarı taşar, diğerleri çubuk ortasında.
            rt.anchorMin        = new Vector2(0f, 0.5f);
            rt.anchorMax        = new Vector2(0f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(size, size);
            rt.anchoredPosition = new Vector2(
                slotCentreX,
                isSleep ? NavBtnSleepRise : 0f
            );

            var img   = go.AddComponent<Image>();
            img.color = isSleep
                ? new Color(0.25f, 0.22f, 0.35f, 1f)
                : new Color(0.18f, 0.18f, 0.18f, 1f);

            var btn = go.AddComponent<Button>();

            var labelGO = NewUIObject("Label", go.transform);
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;

            var tmp = labelGO.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text          = label;
            tmp.fontSize      = NavLabelFontSize;
            tmp.color         = Color.white;
            tmp.alignment     = TMPro.TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            return btn;
        }

        // ---- Yardımcılar --------------------------------------------------------

        private static GameObject NewUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void NewIconSlot(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = NewUIObject(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(4f, 4f);
            rt.offsetMax = new Vector2(-4f, -4f);

            var img   = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
        }

        // -------------------------------------------------------------------------

        private RectTransform CreatePanel(string panelName, bool anchorTop, float height)
        {
            var go  = new GameObject(panelName, typeof(RectTransform));
            go.transform.SetParent(transform, false);

            var img   = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);

            var rt       = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorTop ? new Vector2(0f, 1f) : new Vector2(0f, 0f);
            rt.anchorMax = anchorTop ? new Vector2(1f, 1f) : new Vector2(1f, 0f);
            rt.offsetMin = anchorTop ? new Vector2(0f, -height) : new Vector2(0f, 0f);
            rt.offsetMax = anchorTop ? new Vector2(0f, 0f)      : new Vector2(0f, height);

            return rt;
        }

#endif
    }
}
