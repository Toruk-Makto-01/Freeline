#if UNITY_EDITOR
using UnityEditor;
#endif

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Freeline
{
    public class HUDManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Inspector references — populated by [Build HUD Hierarchy] or manually
        // -------------------------------------------------------------------------

        [Header("Top Panel — Time")]
        [SerializeField] private TextMeshProUGUI clockText;

        [Header("Top Panel — Energy")]
        [SerializeField] private Slider          energySlider;
        [SerializeField] private TextMeshProUGUI energyLabel;
        [SerializeField] private TextMeshProUGUI hungerText;

        [Header("Top Panel — Economy")]
        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private TextMeshProUGUI gemText;

        [Header("Bottom Nav Bar")]
        [SerializeField] private Button drawButton;
        [SerializeField] private Button webtoonButton;
        [SerializeField] private Button sleepButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button homeButton;

        // -------------------------------------------------------------------------
        // Placeholder colours
        // -------------------------------------------------------------------------

        private static readonly Color PanelBg     = new Color(0.08f, 0.08f, 0.12f, 0.88f);
        private static readonly Color NavDefault   = new Color(0.18f, 0.18f, 0.26f, 1.00f);
        private static readonly Color NavActive    = new Color(0.28f, 0.58f, 1.00f, 1.00f);
        private static readonly Color NavSleep     = new Color(0.32f, 0.12f, 0.52f, 1.00f);
        private static readonly Color EnergyFill   = new Color(0.20f, 0.85f, 0.30f, 1.00f);
        private static readonly Color EnergyBg     = new Color(0.12f, 0.12f, 0.18f, 0.80f);
        private static readonly Color HungerWarn   = new Color(1.00f, 0.38f, 0.18f, 1.00f);

        private Button _activeNavButton;

        // -------------------------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------------------------

        void Start()
        {
            var gm = GameManager.Instance;

            gm.TimeManager.OnTimeAdvanced           += HandleTimeAdvanced;
            gm.TimeManager.OnNewDayStarted          += HandleNewDayStarted;
            gm.EnergyManager.OnEnergyChanged        += HandleEnergyChanged;
            gm.JobManager.OnJobCompleted            += HandleJobCompleted;
            gm.WebtoonManager.OnPassiveIncomeEarned += HandlePassiveIncome;

            drawButton.onClick.AddListener(OnDrawClicked);
            webtoonButton.onClick.AddListener(OnWebtoonClicked);
            sleepButton.onClick.AddListener(OnSleepClicked);
            shopButton.onClick.AddListener(OnShopClicked);
            homeButton.onClick.AddListener(OnHomeClicked);

            SetActiveNavButton(homeButton);
            RefreshAll();
        }

        void OnDestroy()
        {
            if (GameManager.Instance == null) return;
            var gm = GameManager.Instance;

            gm.TimeManager.OnTimeAdvanced           -= HandleTimeAdvanced;
            gm.TimeManager.OnNewDayStarted          -= HandleNewDayStarted;
            gm.EnergyManager.OnEnergyChanged        -= HandleEnergyChanged;
            gm.JobManager.OnJobCompleted            -= HandleJobCompleted;
            gm.WebtoonManager.OnPassiveIncomeEarned -= HandlePassiveIncome;
        }

        // -------------------------------------------------------------------------
        // Full refresh — reads every manager and syncs every element.
        // Called once on init; also callable after scene transitions or save loads.
        // -------------------------------------------------------------------------

        public void RefreshAll()
        {
            var gm = GameManager.Instance;
            if (gm?.SaveManager?.CurrentData == null) return;

            UpdateClock();
            UpdateEnergy(gm.EnergyManager.CurrentEnergy, gm.EnergyManager.MaxEnergy);
            UpdateHunger(gm.EnergyManager.IsHungry);
            UpdateCoins(gm.SaveManager.CurrentData.currentCoins);
            UpdateGems(gm.SaveManager.CurrentData.currentGems);
        }

        // -------------------------------------------------------------------------
        // Targeted updaters
        // -------------------------------------------------------------------------

        private void UpdateClock()
            => clockText.text = GameManager.Instance.TimeManager.GetFormattedTime();

        private void UpdateEnergy(float current, float max)
        {
            energySlider.maxValue = max;
            energySlider.value    = current;
        }

        private void UpdateHunger(bool hungry)
        {
            hungerText.text  = hungry ? "FOOD: HUNGRY" : "FOOD: OK";
            hungerText.color = hungry ? HungerWarn  : Color.white;
        }

        private void UpdateCoins(float coins)
            => coinText.text = $"COIN: {Mathf.FloorToInt(coins)}";

        private void UpdateGems(int gems)
            => gemText.text = $"GEM: {gems}";

        // -------------------------------------------------------------------------
        // Event handlers
        // -------------------------------------------------------------------------

        private void HandleTimeAdvanced(float prev, float next) => UpdateClock();
        private void HandleNewDayStarted(int day)               => UpdateClock();

        private void HandleEnergyChanged(float current, float max)
        {
            UpdateEnergy(current, max);
            UpdateHunger(GameManager.Instance.EnergyManager.IsHungry);
        }

        private void HandleJobCompleted(JobData job, float payout)
            => UpdateCoins(GameManager.Instance.SaveManager.CurrentData.currentCoins);

        private void HandlePassiveIncome(float amount)
            => UpdateCoins(GameManager.Instance.SaveManager.CurrentData.currentCoins);

        // -------------------------------------------------------------------------
        // Nav button handlers
        // -------------------------------------------------------------------------

        private void OnDrawClicked()
        {
            SetActiveNavButton(drawButton);
            Debug.Log("[HUD] Navigate to DrawingDesk");
        }

        private void OnWebtoonClicked()
        {
            SetActiveNavButton(webtoonButton);
            Debug.Log("[HUD] Navigate to WebtoonStudio");
        }

        private void OnSleepClicked()
        {
            SetActiveNavButton(sleepButton);
            bool slept = GameManager.Instance.TimeManager.TrySleep();
            Debug.Log(slept
                ? "[HUD] Sleep triggered — good night, Maya."
                : "[HUD] Sleep not available yet (before 21:00).");
        }

        private void OnShopClicked()
        {
            SetActiveNavButton(shopButton);
            Debug.Log("[HUD] Navigate to Shop");
        }

        private void OnHomeClicked()
        {
            SetActiveNavButton(homeButton);
            Debug.Log("[HUD] Navigate to Apartment");
        }

        private void SetActiveNavButton(Button btn)
        {
            if (_activeNavButton != null)
            {
                var prevImg = _activeNavButton.GetComponent<Image>();
                if (prevImg != null)
                    prevImg.color = (_activeNavButton == sleepButton) ? NavSleep : NavDefault;
            }

            _activeNavButton = btn;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = NavActive;
        }

        // =========================================================================
        // EDITOR — Hierarchy builder
        // Right-click this component in the Inspector → "Build HUD Hierarchy"
        // Destroys existing children, rebuilds the full Canvas hierarchy, and
        // populates every SerializeField reference automatically.
        // =========================================================================

#if UNITY_EDITOR
        [ContextMenu("Build HUD Hierarchy")]
        private void BuildHUDHierarchy()
        {
            // Clear existing children
            while (transform.childCount > 0)
                DestroyImmediate(transform.GetChild(0).gameObject);

            SetupCanvas(gameObject);

            BuildTopPanel();
            BuildBottomNavBar();

            EditorUtility.SetDirty(gameObject);
            Debug.Log("[HUD] Hierarchy built and SerializeField references populated.");
        }

        // ---- Canvas / CanvasScaler / GraphicRaycaster ----

        private static void SetupCanvas(GameObject go)
        {
            Canvas canvas = go.GetComponent<Canvas>();
            if (canvas == null) canvas = go.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight  = 0.5f;

            if (go.GetComponent<GraphicRaycaster>() == null)
                go.AddComponent<GraphicRaycaster>();
        }

        // ---- Top panel ----

        private void BuildTopPanel()
        {
            // Panel background — anchored to top, full width, 200px tall
            var panel = NewUIObject("TopPanel", transform);
            var img   = panel.AddComponent<Image>();
            img.color = PanelBg;
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.offsetMin        = new Vector2(0f, -200f);
            rt.offsetMax        = new Vector2(0f,    0f);

            // Clock text — left 17%, full height, with inner padding
            clockText = NewTMP("ClockText", panel.transform, "09:00", 52f, TextAlignmentOptions.Center);
            AnchorPadded(clockText.rectTransform, 0.00f, 0f, 0.17f, 1f, 16f, 8f);

            // Energy container — 17–42%, holds slider + label stacked
            var energyContainer = NewUIObject("EnergyContainer", panel.transform);
            AnchorPadded(energyContainer.GetComponent<RectTransform>(), 0.17f, 0f, 0.42f, 1f, 8f, 8f);

            energySlider = BuildEnergySlider("EnergySlider", energyContainer.transform);
            var sliderRT = energySlider.GetComponent<RectTransform>();
            sliderRT.anchorMin = new Vector2(0f, 0.30f);
            sliderRT.anchorMax = new Vector2(1f, 1.00f);
            sliderRT.offsetMin = new Vector2(4f, 4f);
            sliderRT.offsetMax = new Vector2(-4f, -8f);

            energyLabel = NewTMP("EnergyLabel", energyContainer.transform, "ENERGY", 22f, TextAlignmentOptions.Center);
            energyLabel.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            var labelRT = energyLabel.rectTransform;
            labelRT.anchorMin = new Vector2(0f, 0f);
            labelRT.anchorMax = new Vector2(1f, 0.30f);
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;

            // Hunger text — 42–62%
            hungerText = NewTMP("HungerText", panel.transform, "FOOD: OK", 32f, TextAlignmentOptions.Center);
            AnchorPadded(hungerText.rectTransform, 0.42f, 0f, 0.62f, 1f, 8f, 8f);

            // Coin text — 62–80%
            coinText = NewTMP("CoinText", panel.transform, "COIN: 0", 34f, TextAlignmentOptions.Center);
            AnchorPadded(coinText.rectTransform, 0.62f, 0f, 0.80f, 1f, 8f, 8f);

            // Gem text — 80–100%
            gemText = NewTMP("GemText", panel.transform, "GEM: 0", 34f, TextAlignmentOptions.Center);
            AnchorPadded(gemText.rectTransform, 0.80f, 0f, 1.00f, 1f, 8f, 8f);
        }

        private Slider BuildEnergySlider(string name, Transform parent)
        {
            var root   = NewUIObject(name, parent);
            var slider = root.AddComponent<Slider>();
            slider.interactable = false;
            slider.direction    = Slider.Direction.LeftToRight;
            slider.minValue     = 0f;
            slider.maxValue     = 100f;
            slider.value        = 100f;

            // Dark background track
            var bg    = NewUIObject("Background", root.transform);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = EnergyBg;
            Stretch(bg.GetComponent<RectTransform>());

            // Fill Area (defines the animatable region with inset)
            var fillArea   = NewUIObject("Fill Area", root.transform);
            var fillAreaRT = fillArea.GetComponent<RectTransform>();
            fillAreaRT.anchorMin = new Vector2(0f, 0.15f);
            fillAreaRT.anchorMax = new Vector2(1f, 0.85f);
            fillAreaRT.offsetMin = new Vector2(4f, 0f);
            fillAreaRT.offsetMax = new Vector2(-4f, 0f);

            // Fill image — Slider animates fillRT.anchorMax.x
            var fill    = NewUIObject("Fill", fillArea.transform);
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = EnergyFill;
            var fillRT = fill.GetComponent<RectTransform>();
            fillRT.anchorMin = new Vector2(0f, 0f);
            fillRT.anchorMax = new Vector2(1f, 1f);
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;
            fillRT.pivot     = new Vector2(0f, 0.5f);

            slider.fillRect   = fillRT;
            slider.handleRect = null;    // No handle

            return slider;
        }

        // ---- Bottom nav bar ----

        private void BuildBottomNavBar()
        {
            // Panel background — anchored to bottom, full width, 180px tall
            var bar   = NewUIObject("BottomNavBar", transform);
            var img   = bar.AddComponent<Image>();
            img.color = PanelBg;
            var rt    = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.offsetMin = new Vector2(0f, 0f);
            rt.offsetMax = new Vector2(0f, 180f);

            // Five equal-width buttons
            drawButton    = BuildNavButton("DRAW",    bar.transform, 0.00f, 0.20f, NavDefault, false);
            webtoonButton = BuildNavButton("WEBTOON", bar.transform, 0.20f, 0.40f, NavDefault, false);
            sleepButton   = BuildNavButton("SLEEP",   bar.transform, 0.40f, 0.60f, NavSleep,   true);
            shopButton    = BuildNavButton("SHOP",    bar.transform, 0.60f, 0.80f, NavDefault, false);
            homeButton    = BuildNavButton("HOME",    bar.transform, 0.80f, 1.00f, NavDefault, false);
        }

        private static Button BuildNavButton(string label, Transform parent,
                                             float xMin, float xMax,
                                             Color bgColor, bool isSleep)
        {
            var go  = NewUIObject(label + "Button", parent);
            var img = go.AddComponent<Image>();
            img.color = bgColor;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            // Suppress Unity's default colour-tint transitions; HUDManager drives colour manually
            var colors              = btn.colors;
            colors.normalColor      = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.85f);
            colors.pressedColor     = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.fadeDuration     = 0.05f;
            btn.colors              = colors;

            // Anchor within the nav bar
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, 0f);
            rt.anchorMax = new Vector2(xMax, 1f);
            rt.offsetMin = new Vector2(2f, 2f);
            rt.offsetMax = new Vector2(-2f, isSleep ? 14f : 0f); // sleep peeks above bar

            // Label
            var tmp = NewTMP("Label", go.transform, label, isSleep ? 30f : 24f, TextAlignmentOptions.Center);
            tmp.fontStyle = isSleep ? FontStyles.Bold : FontStyles.Normal;
            Stretch(tmp.rectTransform);

            return btn;
        }

        // ---- UI helpers (Editor-time only) ----

        private static GameObject NewUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static TextMeshProUGUI NewTMP(string name, Transform parent,
                                              string text, float fontSize,
                                              TextAlignmentOptions align)
        {
            var go  = NewUIObject(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.color     = Color.white;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // Anchor with uniform pixel padding on all sides
        private static void AnchorPadded(RectTransform rt,
                                         float xMin, float yMin, float xMax, float yMax,
                                         float padH, float padV)
        {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = new Vector2( padH,  padV);
            rt.offsetMax = new Vector2(-padH, -padV);
        }
#endif
    }
}
