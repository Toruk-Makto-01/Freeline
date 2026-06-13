#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Freeline
{
    [System.Serializable]
    public class PosterTemplate
    {
        public string posterName;
        public int    drawingCost;    // enerji maliyeti
        public int    drawingHours;   // süre maliyeti
        public int    sellPrice;      // satış fiyatı (coin)
        public bool   isUnlocked;
        public Color  previewColor;
    }

    public class PosterPanel : MonoBehaviour
    {
        // ---- Sabitler -------------------------------------------------------
        private const float HeaderH       = 80f;
        private const float BottomBarH    = 80f;
        private const float FeedbackH     = 52f;
        private const float TitleFontSize = 32f;
        private const float StockFontSize = 26f;
        private const float SlotLabelFS   = 22f;
        private const float LockFS        = 22f;
        private const float FeedbackFS    = 24f;
        private const float BackFontSize  = 28f;

        // GridLayoutGroup hücre boyutu
        // PhoneScreen 520×660 (Header+BottomBar çıkarıldı), padding/gap 8px
        // CellW = (520 - 8*2 - 8*2) / 3 ≈ 162  CellH = (660 - 8*2 - 8*2) / 3 ≈ 209
        private const float GridPad = 8f;
        private const float GridGap = 8f;
        private const float CellW   = 162f;
        private const float CellH   = 209f;

        // ---- Hazır şablonlar ------------------------------------------------
        private static readonly PosterTemplate[] Templates =
        {
            new PosterTemplate { posterName="Manzara",    drawingCost=20, drawingHours=2, sellPrice=150, isUnlocked=true,  previewColor=new Color(0.5f,0.7f,0.9f) },
            new PosterTemplate { posterName="Karakter",   drawingCost=25, drawingHours=3, sellPrice=200, isUnlocked=true,  previewColor=new Color(0.9f,0.6f,0.7f) },
            new PosterTemplate { posterName="Soyut",      drawingCost=15, drawingHours=1, sellPrice=100, isUnlocked=true,  previewColor=new Color(0.7f,0.5f,0.9f) },
            new PosterTemplate { posterName="Hayvan",     drawingCost=20, drawingHours=2, sellPrice=150, isUnlocked=true,  previewColor=new Color(0.5f,0.8f,0.6f) },
            new PosterTemplate { posterName="Sehir",      drawingCost=30, drawingHours=4, sellPrice=250, isUnlocked=true,  previewColor=new Color(0.8f,0.7f,0.4f) },
            new PosterTemplate { posterName="Fantezi",    drawingCost=35, drawingHours=4, sellPrice=300, isUnlocked=true,  previewColor=new Color(0.6f,0.4f,0.8f) },
            new PosterTemplate { posterName="Minimalist", drawingCost=15, drawingHours=1, sellPrice=120, isUnlocked=true,  previewColor=new Color(0.9f,0.9f,0.8f) },
            new PosterTemplate { posterName="Retro",      drawingCost=25, drawingHours=3, sellPrice=180, isUnlocked=true,  previewColor=new Color(0.9f,0.7f,0.4f) },
            new PosterTemplate { posterName="Uzay",       drawingCost=40, drawingHours=5, sellPrice=400, isUnlocked=false, previewColor=new Color(0.3f,0.3f,0.5f) },
        };

        // ---- SerializeField refs --------------------------------------------
        [SerializeField] private TextMeshProUGUI stockText;
        [SerializeField] private Transform       gridContent;
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private Button          backBtn;

        // ---- Runtime state --------------------------------------------------
        private int       _posterStock;
        private Coroutine _feedbackCoroutine;

        // =========================================================================
        // Runtime
        // =========================================================================

        void Awake()
        {
            if (backBtn != null) backBtn.onClick.AddListener(Close);
        }

        public void Open()
        {
            gameObject.SetActive(true);
            RefreshGrid();
        }

        public void Close() => gameObject.SetActive(false);

        public void RefreshGrid()
        {
            if (gridContent == null) return;

            for (int i = gridContent.childCount - 1; i >= 0; i--)
            {
                var child = gridContent.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            for (int i = 0; i < Templates.Length; i++)
                BuildPosterSlot(i, Templates[i]);

            UpdateStockText();
        }

        private void UpdateStockText()
        {
            if (stockText != null)
                stockText.text = $"Stok: {_posterStock}";
        }

        // ---- Slot oluşturma (runtime) ----------------------------------------

        private void BuildPosterSlot(int index, PosterTemplate template)
        {
            var slotGO = NewUIObj("PosterSlot_" + index, gridContent);

            var bgImg   = slotGO.AddComponent<Image>();
            bgImg.color = template.isUnlocked
                ? template.previewColor
                : new Color(0.25f, 0.25f, 0.28f, 1f);

            // İsim etiketi
            var labelGO = NewUIObj("PreviewLabel", slotGO.transform);
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(4f, 4f);
            labelRT.offsetMax = new Vector2(-4f, -4f);

            var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text          = template.isUnlocked ? template.posterName : "";
            labelTMP.fontSize      = SlotLabelFS;
            labelTMP.fontStyle     = FontStyles.Bold;
            labelTMP.color         = new Color(0.08f, 0.08f, 0.10f, 1f);
            labelTMP.alignment     = TextAlignmentOptions.Center;
            labelTMP.raycastTarget = false;

            // Kilit overlay — yalnızca kilitli slotlarda
            if (!template.isUnlocked)
            {
                var lockGO = NewUIObj("LockOverlay", slotGO.transform);
                var lockRT = lockGO.GetComponent<RectTransform>();
                lockRT.anchorMin = Vector2.zero;
                lockRT.anchorMax = Vector2.one;
                lockRT.offsetMin = Vector2.zero;
                lockRT.offsetMax = Vector2.zero;

                lockGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

                var lockLabelGO = NewUIObj("LockText", lockGO.transform);
                var lockLabelRT = lockLabelGO.GetComponent<RectTransform>();
                lockLabelRT.anchorMin = Vector2.zero;
                lockLabelRT.anchorMax = Vector2.one;
                lockLabelRT.offsetMin = Vector2.zero;
                lockLabelRT.offsetMax = Vector2.zero;

                var lockTMP = lockLabelGO.AddComponent<TextMeshProUGUI>();
                lockTMP.text          = "Kilitli";
                lockTMP.fontSize      = LockFS;
                lockTMP.fontStyle     = FontStyles.Bold;
                lockTMP.color         = Color.white;
                lockTMP.alignment     = TextAlignmentOptions.Center;
                lockTMP.raycastTarget = false;
            }

            var btn      = slotGO.AddComponent<Button>();
            var captured = template;
            btn.onClick.AddListener(() => OnSlotTapped(captured));
        }

        private void OnSlotTapped(PosterTemplate template)
        {
            if (!template.isUnlocked) return;

            var gm = GameManager.Instance;
            if (gm.EnergyManager.CurrentEnergy < template.drawingCost)
            {
                ShowFeedback("Enerji yetersiz!", new Color(1f, 0.4f, 0.4f, 1f), 2f);
                return;
            }

            gm.EnergyManager.ConsumeEnergy(template.drawingCost);
            gm.TimeManager.AdvanceTime(template.drawingHours);
            _posterStock++;
            UpdateStockText();
            ShowFeedback("Poster tamamlandi! Dukkana gonderildi.", new Color(0.4f, 1f, 0.5f, 1f), 2f);
        }

        private void ShowFeedback(string message, Color color, float duration)
        {
            if (feedbackText == null) return;
            if (_feedbackCoroutine != null) StopCoroutine(_feedbackCoroutine);
            _feedbackCoroutine = StartCoroutine(FeedbackFade(message, color, duration));
        }

        private IEnumerator FeedbackFade(string message, Color color, float duration)
        {
            feedbackText.text  = message;
            feedbackText.color = color;
            feedbackText.gameObject.SetActive(true);

            yield return new WaitForSeconds(duration);

            if (feedbackText != null)
                feedbackText.gameObject.SetActive(false);
            _feedbackCoroutine = null;
        }

        // NewUIObj runtime'da da kullanıldığı için #if dışında
        private static GameObject NewUIObj(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        // =========================================================================
        // EDITOR — hiyerarşi kurucusu
        // =========================================================================

#if UNITY_EDITOR

        [ContextMenu("Build Poster Hierarchy")]
        private void BuildPosterHierarchy()
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
            BuildPosterGrid();
            BuildFeedbackText();
            BuildBottomBar();

            gameObject.SetActive(false);

            EditorUtility.SetDirty(gameObject);
            Debug.Log("[PosterPanel] Hierarchy built.");
        }

        // ---- Header -----------------------------------------------------------

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
            img.color = new Color(0.35f, 0.65f, 0.35f, 1f);

            // Başlık — sol
            var titleGO = NewUIObj("TitleText", go.transform);
            var titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0f,   0f);
            titleRT.anchorMax = new Vector2(0.6f, 1f);
            titleRT.offsetMin = new Vector2(20f, 0f);
            titleRT.offsetMax = Vector2.zero;

            var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            titleTMP.text          = "Poster Uygulamasi";
            titleTMP.fontSize      = TitleFontSize;
            titleTMP.fontStyle     = FontStyles.Bold;
            titleTMP.color         = new Color(0.05f, 0.18f, 0.05f, 1f);
            titleTMP.alignment     = TextAlignmentOptions.MidlineLeft;
            titleTMP.raycastTarget = false;

            // Stok göstergesi — sağ
            var stockGO = NewUIObj("StockText", go.transform);
            var stockRT = stockGO.GetComponent<RectTransform>();
            stockRT.anchorMin = new Vector2(0.6f, 0f);
            stockRT.anchorMax = new Vector2(1f,   1f);
            stockRT.offsetMin = Vector2.zero;
            stockRT.offsetMax = new Vector2(-16f, 0f);

            stockText           = stockGO.AddComponent<TextMeshProUGUI>();
            stockText.text      = "Stok: 0";
            stockText.fontSize  = StockFontSize;
            stockText.color     = new Color(0.05f, 0.18f, 0.05f, 1f);
            stockText.alignment = TextAlignmentOptions.MidlineRight;
            stockText.raycastTarget = false;
        }

        // ---- PosterGrid -------------------------------------------------------

        private void BuildPosterGrid()
        {
            var go = NewUIObj("PosterGrid", transform);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(0f,  BottomBarH);
            rt.offsetMax = new Vector2(0f, -HeaderH);

            var img   = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);

            var glg              = go.AddComponent<GridLayoutGroup>();
            glg.constraint       = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount  = 3;
            glg.cellSize         = new Vector2(CellW, CellH);
            glg.spacing          = new Vector2(GridGap, GridGap);
            glg.padding          = new RectOffset((int)GridPad, (int)GridPad, (int)GridPad, (int)GridPad);
            glg.startAxis        = GridLayoutGroup.Axis.Horizontal;
            glg.startCorner      = GridLayoutGroup.Corner.UpperLeft;
            glg.childAlignment   = TextAnchor.UpperCenter;

            gridContent = go.transform;
        }

        // ---- FeedbackText — grid üzerinde, sadece anlık görünür ---------------

        private void BuildFeedbackText()
        {
            var go = NewUIObj("FeedbackText", transform);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(8f,  BottomBarH + 4f);
            rt.offsetMax = new Vector2(-8f, BottomBarH + FeedbackH);

            feedbackText           = go.AddComponent<TextMeshProUGUI>();
            feedbackText.text      = "";
            feedbackText.fontSize  = FeedbackFS;
            feedbackText.color     = new Color(0.4f, 1f, 0.5f, 1f);
            feedbackText.alignment = TextAlignmentOptions.Center;
            feedbackText.raycastTarget = false;

            go.SetActive(false);
        }

        // ---- BottomBar --------------------------------------------------------

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
