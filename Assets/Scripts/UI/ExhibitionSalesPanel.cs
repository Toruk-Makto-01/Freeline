#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Freeline
{
    // Sergi başladığında sırayla müşterileri ağırladığımız satış ekranı
    public class ExhibitionSalesPanel : MonoBehaviour
    {
        [Header("Panel Root")]
        [SerializeField] private GameObject panel;

        [Header("NPC Visuals")]
        [SerializeField] private Image npcImage;
        [SerializeField] private Sprite[] npcSprites; // NPC resimlerini buraya atacağız

        [Header("Dialogue UI")]
        [SerializeField] private TextMeshProUGUI npcDialogueText;
        [SerializeField] private TextMeshProUGUI feedbackText;

        [Header("Action Buttons")]
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button bargainButton;
        [SerializeField] private Button rejectButton;
        [SerializeField] private CanvasGroup buttonsGroup;

        [Header("Wiring")]
        [SerializeField] private ExhibitionManager exhibitionManager;

        private Queue<ExhibitionNpc> _visitorsQueue;
        private ExhibitionNpc _currentVisitor;
        private ExhibitionStock _wantedItem;

        private static readonly Color PanelBg      = new Color(0.12f, 0.12f, 0.15f, 0.98f);
        private static readonly Color AcceptBtn    = new Color(0.15f, 0.55f, 0.25f, 1.00f);
        private static readonly Color BargainBtn   = new Color(0.85f, 0.55f, 0.15f, 1.00f);
        private static readonly Color RejectBtn    = new Color(0.65f, 0.20f, 0.20f, 1.00f);
        private static readonly Color FeedbackPos  = new Color(0.40f, 0.90f, 0.40f, 1.00f);
        private static readonly Color FeedbackNeg  = new Color(0.90f, 0.40f, 0.40f, 1.00f);

        void Awake()
        {
            acceptButton.onClick.AddListener(OnAcceptClicked);
            bargainButton.onClick.AddListener(OnBargainClicked);
            rejectButton.onClick.AddListener(OnRejectClicked);

            Hide(); // Başlangıçta paneli ve karartmayı gizler
        }

        void Start()
        {
            if (exhibitionManager == null)
                exhibitionManager = GameManager.Instance.ExhibitionManager;

            exhibitionManager.OnExhibitionStarted += HandleExhibitionStarted;
        }

        void OnDestroy()
        {
            if (exhibitionManager != null)
                exhibitionManager.OnExhibitionStarted -= HandleExhibitionStarted;
        }

        public void Show()
        {
            if (panel != null) panel.SetActive(true);

            Transform blocker = transform.Find("Blocker");
            if (blocker != null) blocker.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
            
            Transform blocker = transform.Find("Blocker");
            if (blocker != null) blocker.gameObject.SetActive(false);
        }

        private void HandleExhibitionStarted()
        {
            var visitorsList = exhibitionManager.GenerateVisitors();
            _visitorsQueue = new Queue<ExhibitionNpc>(visitorsList);

            panel.SetActive(true);
            Transform blocker = transform.Find("Blocker");
            if (blocker != null) blocker.gameObject.SetActive(true);

            ServeNextVisitor();
        }

        private void ServeNextVisitor()
        {
            feedbackText.text = string.Empty;
            buttonsGroup.interactable = true;

            var currentStock = exhibitionManager.GetStock();

            if (_visitorsQueue.Count == 0 || currentStock.Count == 0)
            {
                EndSales();
                return;
            }

            _currentVisitor = _visitorsQueue.Dequeue();
            _wantedItem = currentStock[Random.Range(0, currentStock.Count)];

            // NPC Görselini rastgele seç
            if (npcSprites != null && npcSprites.Length > 0)
            {
                npcImage.sprite = npcSprites[Random.Range(0, npcSprites.Length)];
                npcImage.color = Color.white; // Görünür olduğundan emin ol
            }

            int offerPrice = Mathf.RoundToInt(_wantedItem.basePrice * _currentVisitor.offerMultiplier);

            npcDialogueText.text = $"\"{_currentVisitor.greetingLine}\"\n\n" +
                                   $"Şu <color=#FFD700>{_wantedItem.productName}</color> ilgimi çekti. " +
                                   $"Bunun için sana <color=#00FF00>{offerPrice}</color> coin verebilirim. Ne dersin?";
        }

        private void OnAcceptClicked()
        {
            buttonsGroup.interactable = false;
            int offerPrice = Mathf.RoundToInt(_wantedItem.basePrice * _currentVisitor.offerMultiplier);
            exhibitionManager.ProcessSale(_wantedItem, _currentVisitor, true, false);
            
            ShowFeedback($"Harika! {_wantedItem.productName} <color=#FFD700>{offerPrice}</color> coine satıldı.", FeedbackPos);
            StartCoroutine(WaitAndNext());
        }

        private void OnBargainClicked()
        {
            buttonsGroup.interactable = false;
            exhibitionManager.ProcessSale(_wantedItem, _currentVisitor, false, true);

            if (_currentVisitor.hasPurchased)
            {
                // Pazarlık kârı eklenmiş hali
                int finalPrice = Mathf.RoundToInt((_wantedItem.basePrice * _currentVisitor.offerMultiplier) * 1.25f);
                ShowFeedback($"Pazarlık işe yaradı! Ürün <color=#FFD700>{finalPrice}</color> coine satıldı.", FeedbackPos);
            }
            else
            {
                ShowFeedback("Müşteri fiyatta anlaşamayınca sinirlenip gitti...", FeedbackNeg);
            }

            StartCoroutine(WaitAndNext());
        }

        private void OnRejectClicked()
        {
            buttonsGroup.interactable = false;
            ShowFeedback("Müşteriyi reddettin. Dükkandan ayrıldı.", new Color(0.7f, 0.7f, 0.7f));
            StartCoroutine(WaitAndNext());
        }

        private void ShowFeedback(string msg, Color color)
        {
            feedbackText.text = msg;
            feedbackText.color = color;
        }

        private IEnumerator WaitAndNext()
        {
            yield return new WaitForSeconds(1.5f);
            ServeNextVisitor();
        }

        private void EndSales()
        {
            exhibitionManager.EndExhibition();
            // Hide(); // Paneli kapatmak yerine, sergi özet paneline geçiş yapılabilir
            Debug.Log("Sergi satışları tamamlandı!");
        }

#if UNITY_EDITOR
        [ContextMenu("Build Exhibition Sales Hierarchy")]
        private void BuildExhibitionSalesHierarchy()
        {
            while (transform.childCount > 0)
                DestroyImmediate(transform.GetChild(0).gameObject);

            Stretch(GetComponent<RectTransform>());

            var blocker = NewUIObject("Blocker", transform);
            blocker.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
            Stretch(blocker.GetComponent<RectTransform>());

            var panelGO = NewUIObject("Panel", transform);
            panelGO.AddComponent<Image>().color = PanelBg;
            var panelRT = panelGO.GetComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = new Vector2(950f, 1300f);
            panelRT.anchoredPosition = Vector2.zero;

            var header = NewTMP("Header", panelGO.transform, "SERGİ SATIŞI", 60f, TextAlignmentOptions.Center);
            header.fontStyle = FontStyles.Bold;
            PositionFromTop(header.rectTransform, 0f, 1f, 40f, 80f, 0f);

            var npcImageGO = NewUIObject("NpcImagePlaceholder", panelGO.transform);
            var npcImg = npcImageGO.AddComponent<Image>();
            npcImg.color = new Color(1f, 1f, 1f, 0.1f);
            var npcImgRT = npcImageGO.GetComponent<RectTransform>();
            npcImgRT.anchorMin = new Vector2(0.5f, 1f);
            npcImgRT.anchorMax = new Vector2(0.5f, 1f);
            npcImgRT.pivot = new Vector2(0.5f, 1f);
            npcImgRT.sizeDelta = new Vector2(400f, 400f);
            npcImgRT.anchoredPosition = new Vector2(0f, -150f);
            
            var npcTmp = NewTMP("NpcLabel", npcImageGO.transform, "NPC GÖRSELİ", 30f, TextAlignmentOptions.Center);
            Stretch(npcTmp.rectTransform);

            var dialogueBgGO = NewUIObject("DialogueBg", panelGO.transform);
            dialogueBgGO.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 1f);
            var dialogueBgRT = dialogueBgGO.GetComponent<RectTransform>();
            dialogueBgRT.anchorMin = new Vector2(0.5f, 1f);
            dialogueBgRT.anchorMax = new Vector2(0.5f, 1f);
            dialogueBgRT.pivot = new Vector2(0.5f, 1f);
            dialogueBgRT.sizeDelta = new Vector2(850f, 250f);
            dialogueBgRT.anchoredPosition = new Vector2(0f, -600f);

            var dialogueTxt = NewTMP("DialogueText", dialogueBgGO.transform, "Diyalog buraya gelecek...", 40f, TextAlignmentOptions.Center);
            var dialogueTxtRT = dialogueTxt.GetComponent<RectTransform>();
            Stretch(dialogueTxtRT);
            dialogueTxtRT.offsetMin = new Vector2(30f, 30f);
            dialogueTxtRT.offsetMax = new Vector2(-30f, -30f);
            dialogueTxt.enableWordWrapping = true;

            var fbTxt = NewTMP("FeedbackText", panelGO.transform, "", 36f, TextAlignmentOptions.Center);
            fbTxt.fontStyle = FontStyles.Bold;
            PositionFromTop(fbTxt.rectTransform, 0f, 1f, 900f, 80f, 20f);

            var btnGroupGO = NewUIObject("ButtonGroup", panelGO.transform);
            var canvasGroup = btnGroupGO.AddComponent<CanvasGroup>();
            var vlg = btnGroupGO.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 20f;
            vlg.childForceExpandHeight = false;
            
            var btnGroupRT = btnGroupGO.GetComponent<RectTransform>();
            btnGroupRT.anchorMin = new Vector2(0.5f, 0f);
            btnGroupRT.anchorMax = new Vector2(0.5f, 0f);
            btnGroupRT.pivot = new Vector2(0.5f, 0f);
            btnGroupRT.sizeDelta = new Vector2(850f, 300f);
            btnGroupRT.anchoredPosition = new Vector2(0f, 50f);

            var btnAccept = BuildButton("AcceptBtn", btnGroupGO.transform, "Kabul Et (Normal Fiyat)", AcceptBtn);
            var btnBargain = BuildButton("BargainBtn", btnGroupGO.transform, "Pazarlık Yap (Riskli)", BargainBtn);
            var btnReject = BuildButton("RejectBtn", btnGroupGO.transform, "Reddet (Gönder)", RejectBtn);

            panel = panelGO;
            npcDialogueText = dialogueTxt;
            feedbackText = fbTxt;
            buttonsGroup = canvasGroup;
            acceptButton = btnAccept;
            bargainButton = btnBargain;
            rejectButton = btnReject;

            panelGO.SetActive(false);
            EditorUtility.SetDirty(this);
        }

        private static Button BuildButton(string name, Transform parent, string label, Color bg)
        {
            var go = NewUIObject(name, parent);
            var img = go.AddComponent<Image>();
            img.color = bg;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var rt = go.AddComponent<LayoutElement>();
            rt.minHeight = 80f;

            var tmp = NewTMP("Label", go.transform, label, 36f, TextAlignmentOptions.Center);
            tmp.fontStyle = FontStyles.Bold;
            Stretch(tmp.rectTransform);

            return btn;
        }

        private static GameObject NewUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static TextMeshProUGUI NewTMP(string name, Transform parent, string text, float fontSize, TextAlignmentOptions align)
        {
            var go = NewUIObject(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
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

        private static void PositionFromTop(RectTransform rt, float xMin, float xMax, float topOffset, float height, float padH)
        {
            rt.anchorMin = new Vector2(xMin, 1f);
            rt.anchorMax = new Vector2(xMax, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(padH, -topOffset - height);
            rt.offsetMax = new Vector2(-padH, -topOffset);
        }
#endif
    }
}