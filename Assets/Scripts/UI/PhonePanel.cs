#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.UI;

namespace Freeline
{
    public class PhonePanel : MonoBehaviour
    {
        // PhoneDevice
        private const float DeviceW         = 600f;
        private const float DeviceH         = 900f;
        private const float DevicePadding   = 40f;

        // PhoneStatusBar / PhoneInfoBar / PhoneExitBar
        private const float StatusBarH      = 60f;
        private const float InfoBarH        = 80f;
        private const float ExitBarH        = 80f;

        // AppGrid
        private const float AppBtnSize      = 220f;
        private const float AppBtnGap       = 20f;
        private const float AppLabelSize    = 28f;

        // -------------------------------------------------------------------------
        // SerializeField refs
        // -------------------------------------------------------------------------

        [SerializeField] private TMPro.TextMeshProUGUI phoneInfoText;

        [Header("App Buttons")]
        [SerializeField] private Button appBtnZenitoon;
        [SerializeField] private Button appBtnWebtoon;
        [SerializeField] private Button appBtnPoster;
        [SerializeField] private Button appBtnMarket;

        [Header("Panels")]
        [SerializeField] private ZenitoonPanel zenitoonPanel;

        [Header("Buttons")]
        [SerializeField] private Button exitBtn;

        // =========================================================================
        // Runtime
        // =========================================================================

        void Awake()
        {
            if (appBtnZenitoon != null && zenitoonPanel != null)
                appBtnZenitoon.onClick.AddListener(zenitoonPanel.Open);
            if (exitBtn != null)
                exitBtn.onClick.AddListener(Close);
        }

        public void Open()
        {
            Debug.Log("[Phone] Open called");
            gameObject.SetActive(true);
        }

        public void Close()
        {
            Debug.Log("[Phone] Close called");
            gameObject.SetActive(false);
        }

        // =========================================================================
        // EDITOR — hierarchy builder
        // =========================================================================

#if UNITY_EDITOR

        [ContextMenu("Build Phone Hierarchy")]
        private void BuildPhoneHierarchy()
        {
            while (transform.childCount > 0)
                DestroyImmediate(transform.GetChild(0).gameObject);

            // Tam ekran yarı-saydam arka plan
            var selfRT       = GetComponent<RectTransform>();
            selfRT.anchorMin = Vector2.zero;
            selfRT.anchorMax = Vector2.one;
            selfRT.offsetMin = Vector2.zero;
            selfRT.offsetMax = Vector2.zero;

            var selfImg   = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            selfImg.color = new Color(0f, 0f, 0f, 0.72f);

            BuildDevice();

            gameObject.SetActive(false);

            EditorUtility.SetDirty(gameObject);
            Debug.Log("[PhonePanel] Hierarchy built.");
        }

        // ---- PhoneDevice --------------------------------------------------------

        private void BuildDevice()
        {
            var go = NewUIObject("PhoneDevice", transform);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(DeviceW, DeviceH);
            rt.anchoredPosition = Vector2.zero;

            var img   = go.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.20f, 1f);
            Debug.Log("[Phone] PhoneDevice created");

            BuildScreen(go.transform);
        }

        // ---- PhoneScreen --------------------------------------------------------

        private void BuildScreen(Transform device)
        {
            Debug.Log("[Phone] BuildScreen started");

            var go = NewUIObject("PhoneScreen", device);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2( DevicePadding,  DevicePadding);
            rt.offsetMax = new Vector2(-DevicePadding, -DevicePadding);

            var img   = go.AddComponent<Image>();
            img.color = new Color(0.10f, 0.10f, 0.14f, 1f);
            Debug.Log("[Phone] PhoneScreen created");

            BuildStatusBar(go.transform);
            BuildInfoBar(go.transform);
            BuildAppGrid(go.transform);
            BuildExitBar(go.transform);

            // ZenitoonPanel overlay — kendi ContextMenu'suyla içi doldurulur
            var zpGO = NewUIObject("ZenitoonPanel", go.transform);
            var zpRT = zpGO.GetComponent<RectTransform>();
            zpRT.anchorMin = Vector2.zero;
            zpRT.anchorMax = Vector2.one;
            zpRT.offsetMin = Vector2.zero;
            zpRT.offsetMax = Vector2.zero;
            zenitoonPanel  = zpGO.AddComponent<ZenitoonPanel>();
            zpGO.SetActive(false);
            Debug.Log("[Phone] ZenitoonPanel created: " + (zenitoonPanel != null ? "OK" : "NULL"));
        }

        // ---- PhoneStatusBar — üstte sabit 60px yükseklik ----------------------------

        private static void BuildStatusBar(Transform screen)
        {
            var go = NewUIObject("PhoneStatusBar", screen);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f,   -StatusBarH);
            rt.offsetMax = new Vector2(0f,    0f);

            var img   = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);

            // Sol metin: şarj
            var chargeGO = NewUIObject("ChargeText", go.transform);
            var chargeRT = chargeGO.GetComponent<RectTransform>();
            chargeRT.anchorMin = new Vector2(0f, 0f);
            chargeRT.anchorMax = new Vector2(0.5f, 1f);
            chargeRT.offsetMin = new Vector2(12f, 0f);
            chargeRT.offsetMax = Vector2.zero;

            var chargeTMP = chargeGO.AddComponent<TMPro.TextMeshProUGUI>();
            chargeTMP.text          = "Şarj %100";
            chargeTMP.fontSize      = 22f;
            chargeTMP.color         = Color.white;
            chargeTMP.alignment     = TMPro.TextAlignmentOptions.MidlineLeft;
            chargeTMP.raycastTarget = false;

            // Sağ metin: wifi
            var wifiGO = NewUIObject("WifiText", go.transform);
            var wifiRT = wifiGO.GetComponent<RectTransform>();
            wifiRT.anchorMin = new Vector2(0.5f, 0f);
            wifiRT.anchorMax = new Vector2(1f,   1f);
            wifiRT.offsetMin = Vector2.zero;
            wifiRT.offsetMax = new Vector2(-12f, 0f);

            var wifiTMP = wifiGO.AddComponent<TMPro.TextMeshProUGUI>();
            wifiTMP.text          = "Wifi";
            wifiTMP.fontSize      = 22f;
            wifiTMP.color         = Color.white;
            wifiTMP.alignment     = TMPro.TextAlignmentOptions.MidlineRight;
            wifiTMP.raycastTarget = false;
        }

        // ---- PhoneInfoBar — StatusBar'ın hemen altında, 80px ------------------

        private void BuildInfoBar(Transform screen)
        {
            var go = NewUIObject("PhoneInfoBar", screen);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f,   -(StatusBarH + InfoBarH));
            rt.offsetMax = new Vector2(0f,    -StatusBarH);

            var img   = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);

            var infoGO = NewUIObject("InfoText", go.transform);
            var infoRT = infoGO.GetComponent<RectTransform>();
            infoRT.anchorMin = Vector2.zero;
            infoRT.anchorMax = Vector2.one;
            infoRT.offsetMin = new Vector2(8f, 0f);
            infoRT.offsetMax = new Vector2(-8f, 0f);

            phoneInfoText           = infoGO.AddComponent<TMPro.TextMeshProUGUI>();
            phoneInfoText.text      = "Saat / Enerji / Coin / Takipçi";
            phoneInfoText.fontSize  = 26f;
            phoneInfoText.color     = Color.white;
            phoneInfoText.alignment = TMPro.TextAlignmentOptions.Center;
            phoneInfoText.raycastTarget = false;
        }

        // ---- AppGrid — 2×2, ortalanmış ------------------------------------------

        private void BuildAppGrid(Transform screen)
        {
            var go = NewUIObject("AppGrid", screen);
            var rt = go.GetComponent<RectTransform>();

            // 2×2 ızgara toplam boyutu
            float gridW = AppBtnSize * 2 + AppBtnGap;
            float gridH = AppBtnSize * 2 + AppBtnGap;

            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(gridW, gridH);
            rt.anchoredPosition = Vector2.zero;

            var img   = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);

            appBtnZenitoon = BuildAppBtn("AppBtn_Zenitoon", go.transform, col: 0, row: 1,
                new Color(0.96f, 0.76f, 0.80f, 1f), "Zenitoon",
                new Color(0.30f, 0.10f, 0.15f, 1f));

            appBtnWebtoon  = BuildAppBtn("AppBtn_Webtoon",  go.transform, col: 1, row: 1,
                new Color(0.98f, 0.72f, 0.35f, 1f), "Webtoon",
                new Color(0.30f, 0.18f, 0.05f, 1f));

            appBtnPoster   = BuildAppBtn("AppBtn_Poster",   go.transform, col: 0, row: 0,
                new Color(0.35f, 0.65f, 0.35f, 1f), "Poster",
                new Color(0.08f, 0.20f, 0.08f, 1f));

            appBtnMarket   = BuildAppBtn("AppBtn_Market",   go.transform, col: 1, row: 0,
                new Color(0.40f, 0.55f, 0.65f, 1f), "Market",
                new Color(0.08f, 0.14f, 0.20f, 1f));
        }

        // col: 0=sol, 1=sağ; row: 1=üst, 0=alt (pivot sol-alt)
        private static Button BuildAppBtn(string objName, Transform grid,
                                          int col, int row,
                                          Color bgColor, string label, Color labelColor)
        {
            var go = NewUIObject(objName, grid);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 0f);
            rt.anchorMax        = new Vector2(0f, 0f);
            rt.pivot            = new Vector2(0f, 0f);
            rt.sizeDelta        = new Vector2(AppBtnSize, AppBtnSize);
            rt.anchoredPosition = new Vector2(
                col * (AppBtnSize + AppBtnGap),
                row * (AppBtnSize + AppBtnGap)
            );

            var img   = go.AddComponent<Image>();
            img.color = bgColor;

            var btn = go.AddComponent<Button>();

            var labelGO = NewUIObject("Label", go.transform);
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;

            var tmp = labelGO.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text          = label;
            tmp.fontSize      = AppLabelSize;
            tmp.fontStyle     = TMPro.FontStyles.Bold;
            tmp.color         = labelColor;
            tmp.alignment     = TMPro.TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            return btn;
        }

        // ---- PhoneExitBar — altta 80px, kapat düğmesi ---------------------------

        private void BuildExitBar(Transform screen)
        {
            var go = NewUIObject("PhoneExitBar", screen);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(0f, 0f);
            rt.offsetMax = new Vector2(0f, ExitBarH);

            var img   = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);

            // Kapatma düğmesi — merkezi, yuvarlak görünüm için kare
            var btnGO = NewUIObject("CloseButton", go.transform);
            var btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.anchorMin        = new Vector2(0.5f, 0.5f);
            btnRT.anchorMax        = new Vector2(0.5f, 0.5f);
            btnRT.pivot            = new Vector2(0.5f, 0.5f);
            btnRT.sizeDelta        = new Vector2(ExitBarH - 16f, ExitBarH - 16f);
            btnRT.anchoredPosition = Vector2.zero;

            var btnImg   = btnGO.AddComponent<Image>();
            btnImg.color = new Color(0.35f, 0.35f, 0.40f, 1f);

            exitBtn = btnGO.AddComponent<Button>();

            var dotGO = NewUIObject("Label", btnGO.transform);
            var dotRT = dotGO.GetComponent<RectTransform>();
            dotRT.anchorMin = Vector2.zero;
            dotRT.anchorMax = Vector2.one;
            dotRT.offsetMin = Vector2.zero;
            dotRT.offsetMax = Vector2.zero;

            var dotTMP = dotGO.AddComponent<TMPro.TextMeshProUGUI>();
            dotTMP.text          = "●";
            dotTMP.fontSize      = 32f;
            dotTMP.color         = Color.white;
            dotTMP.alignment     = TMPro.TextAlignmentOptions.Center;
            dotTMP.raycastTarget = false;
        }

        // ---- Yardımcı -----------------------------------------------------------

        private static GameObject NewUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

#endif
    }
}
