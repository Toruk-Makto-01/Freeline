#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Freeline
{
    public class DrawingDeskPanel : MonoBehaviour
    {
        // ---- Sabitler -------------------------------------------------------
        private const float TopBarH        = 80f;
        private const float BottomBarH     = 120f;
        private const float BackBtnW       = 160f;
        private const float ActionBtnH     = 72f;
        private const float ProgressBarH   = 24f;
        private const float TitleFontSize  = 32f;
        private const float LabelFontSize  = 28f;
        private const float ActionFontSize = 36f;

        private const float FillSpeed  = 0.15f;
        private const float DrainSpeed = 0.05f;

        // ---- Refs -----------------------------------------------------------
        [SerializeField] private TextMeshProUGUI jobTitleText;
        [SerializeField] private Image           progressBar;
        [SerializeField] private Button          actionBtn;
        [SerializeField] private Button          backBtn;

        // ---- Minigame state -------------------------------------------------
        private float   _progress;
        private bool    _isHolding;
        private JobData _activeJob;
        private bool    _jobActive;

        // =========================================================================
        // Runtime
        // =========================================================================

        void Awake()
        {
            if (backBtn != null)
                backBtn.onClick.AddListener(Close);

            if (actionBtn != null)
            {
                var trigger = actionBtn.gameObject.GetComponent<EventTrigger>()
                              ?? actionBtn.gameObject.AddComponent<EventTrigger>();

                var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                down.callback.AddListener(_ => _isHolding = true);
                trigger.triggers.Add(down);

                var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
                up.callback.AddListener(_ => _isHolding = false);
                trigger.triggers.Add(up);
            }
        }

        void Update()
        {
            if (!_jobActive) return;

            _progress += (_isHolding ? FillSpeed : -DrainSpeed) * Time.deltaTime;
            _progress  = Mathf.Clamp01(_progress);

            progressBar.rectTransform.anchorMax = new Vector2(_progress, 1f);

            if (_progress >= 1f)
                CompleteJob();
        }

        public void Open(JobData job)
        {
            _activeJob = job;
            _progress  = 0f;
            _isHolding = false;
            _jobActive = true;

            gameObject.SetActive(true);

            if (jobTitleText != null && job != null)
                jobTitleText.text = job.jobTitle;
            if (progressBar != null)
                progressBar.rectTransform.anchorMax = new Vector2(0f, 1f);
        }

        public void Close()
        {
            _jobActive = false;
            _isHolding = false;
            gameObject.SetActive(false);
            GameManager.Instance.SetState(GameState.Apartment);
        }

        private void CompleteJob()
        {
            _jobActive = false;
            _isHolding = false;
            GameManager.Instance.JobManager.CompleteActiveJob();
            Close();
        }

        // =========================================================================
        // EDITOR — hiyerarşi kurucusu
        // =========================================================================

#if UNITY_EDITOR

        [ContextMenu("Build Drawing Desk Hierarchy")]
        private void BuildDrawingDeskHierarchy()
        {
            while (transform.childCount > 0)
                DestroyImmediate(transform.GetChild(0).gameObject);

            var selfRT       = GetComponent<RectTransform>();
            selfRT.anchorMin = Vector2.zero;
            selfRT.anchorMax = Vector2.one;
            selfRT.offsetMin = Vector2.zero;
            selfRT.offsetMax = Vector2.zero;

            var selfImg   = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            selfImg.color = new Color(0.08f, 0.08f, 0.10f, 1f);

            BuildTopBar();
            BuildMinigameAreaDirect();
            BuildBottomBar();

            gameObject.SetActive(false);

            EditorUtility.SetDirty(gameObject);
            Debug.Log("[DrawingDeskPanel] Hierarchy built.");
        }

        // ---- Üst çubuk: iş başlığı + geri butonu ----------------------------

        private void BuildTopBar()
        {
            var go = NewUIObj("TopBar", transform);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -TopBarH);
            rt.offsetMax = Vector2.zero;

            var img   = go.AddComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.18f, 1f);

            // İş başlığı — sol taraf
            var titleGO = NewUIObj("JobTitleText", go.transform);
            var titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0f, 0f);
            titleRT.anchorMax = new Vector2(1f, 1f);
            titleRT.offsetMin = new Vector2(16f, 0f);
            titleRT.offsetMax = new Vector2(-(BackBtnW + 8f), 0f);

            jobTitleText           = titleGO.AddComponent<TextMeshProUGUI>();
            jobTitleText.text      = "Is Basligi";
            jobTitleText.fontSize  = TitleFontSize;
            jobTitleText.fontStyle = FontStyles.Bold;
            jobTitleText.color     = Color.white;
            jobTitleText.alignment = TextAlignmentOptions.MidlineLeft;
            jobTitleText.raycastTarget = false;

            // Geri butonu — sağ taraf
            var backGO = NewUIObj("BackBtn", go.transform);
            var backRT = backGO.GetComponent<RectTransform>();
            backRT.anchorMin = new Vector2(1f, 0.5f);
            backRT.anchorMax = new Vector2(1f, 0.5f);
            backRT.pivot     = new Vector2(1f, 0.5f);
            backRT.sizeDelta = new Vector2(BackBtnW, TopBarH - 16f);
            backRT.anchoredPosition = new Vector2(-8f, 0f);

            var backImg   = backGO.AddComponent<Image>();
            backImg.color = new Color(0.35f, 0.20f, 0.20f, 1f);

            backBtn = backGO.AddComponent<Button>();

            var backLabelGO = NewUIObj("Label", backGO.transform);
            var backLabelRT = backLabelGO.GetComponent<RectTransform>();
            backLabelRT.anchorMin = Vector2.zero;
            backLabelRT.anchorMax = Vector2.one;
            backLabelRT.offsetMin = Vector2.zero;
            backLabelRT.offsetMax = Vector2.zero;

            var backTMP = backLabelGO.AddComponent<TextMeshProUGUI>();
            backTMP.text          = "<- Geri";
            backTMP.fontSize      = LabelFontSize;
            backTMP.fontStyle     = FontStyles.Bold;
            backTMP.color         = Color.white;
            backTMP.alignment     = TextAlignmentOptions.Center;
            backTMP.raycastTarget = false;
        }

        // ---- Minigame alanı: orta placeholder ----------------------------

        private void BuildMinigameAreaDirect()
        {
            var go = NewUIObj("MinigameArea", transform);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(0f,  BottomBarH);
            rt.offsetMax = new Vector2(0f, -TopBarH);

            var img   = go.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            var textGO = NewUIObj("PlaceholderText", go.transform);
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text          = "Minigame buraya gelecek";
            tmp.fontSize      = LabelFontSize;
            tmp.color         = new Color(0.5f, 0.5f, 0.5f, 1f);
            tmp.alignment     = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }

        // ---- Alt çubuk: ilerleme çubuğu + Çiz butonu -----------------------

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
            img.color = new Color(0.12f, 0.12f, 0.18f, 1f);

            BuildProgressBar(go.transform);
            BuildActionBtn(go.transform);
        }

        private void BuildProgressBar(Transform parent)
        {
            // BottomBar'ın üst %20'sini kaplar
            var containerGO = NewUIObj("ProgressBarContainer", parent);
            var containerRT = containerGO.GetComponent<RectTransform>();
            containerRT.anchorMin = new Vector2(0f, 0.8f);
            containerRT.anchorMax = new Vector2(1f, 1f);
            containerRT.offsetMin = new Vector2(12f,  4f);
            containerRT.offsetMax = new Vector2(-12f, -4f);

            var containerImg   = containerGO.AddComponent<Image>();
            containerImg.color = new Color(0.20f, 0.20f, 0.20f, 1f);

            // Dolgu — runtime'da anchorMax.x = ilerleme değeri ile güncellenir
            var fillGO = NewUIObj("Fill", containerGO.transform);
            var fillRT = fillGO.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = new Vector2(0f, 1f);
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;

            progressBar       = fillGO.AddComponent<Image>();
            progressBar.color = new Color(0.40f, 0.80f, 0.40f, 1f);
        }

        private void BuildActionBtn(Transform parent)
        {
            var go = NewUIObj("ActionBtn", parent);
            var rt = go.GetComponent<RectTransform>();
            // BottomBar'ın alt %75'ini kaplar; ProgressBar üstte %20 aldıktan sonra aralarında boşluk kalır
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0.75f);
            rt.offsetMin = new Vector2(12f,  4f);
            rt.offsetMax = new Vector2(-12f, -4f);

            var btnImg   = go.AddComponent<Image>();
            btnImg.color = new Color(0.20f, 0.55f, 0.85f, 1f);

            actionBtn = go.AddComponent<Button>();
            go.AddComponent<EventTrigger>();

            var labelGO = NewUIObj("Label", go.transform);
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;

            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text          = "Ciz!";
            tmp.fontSize      = ActionFontSize;
            tmp.fontStyle     = FontStyles.Bold;
            tmp.color         = Color.white;
            tmp.alignment     = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }

        // ---- Yardımcı -------------------------------------------------------

        private static GameObject NewUIObj(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null)
                go.transform.SetParent(parent, false);
            return go;
        }

#endif
    }
}
