using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

namespace Freeline
{
    // Seviye 2 çizim mekaniği — ekranda beliren çizgiyi parmakla takip et
    // Takip kalitesine göre görev ücreti değişir
    public class LineTraceMinigame : MonoBehaviour
    {
        private static JobManager JM => GameManager.Instance.JobManager;

        [Header("Panel Root")]
        [SerializeField] private GameObject    panel;
        [SerializeField] private RectTransform lineContainer; // çizgi segment Image'ları buraya eklenir

        [Header("Info")]
        [SerializeField] private TextMeshProUGUI jobTitleText;
        [SerializeField] private TextMeshProUGUI payoutText;

        [Header("Progress & Accuracy")]
        [SerializeField] private Image           progressBarFill;
        [SerializeField] private TextMeshProUGUI accuracyText;

        [Header("Frontier Indicator")]
        [SerializeField] private RectTransform frontierDot; // oyuncunun takip etmesi gereken noktayı gösterir

        [Header("Buttons")]
        [SerializeField] private Button cancelButton;

        [Header("Settings")]
        [SerializeField] private float traceSpeed   = 400f; // frontier'ın canvas piksel/saniye ilerleme hızı
        [SerializeField] private float maxDeviation = 80f;  // piksel; bu mesafe içinde kalınmalı
        [SerializeField] private int   waypointCount = 10;  // oluşturulacak waypoint sayısı (8–12)
        [SerializeField] private float lineThickness = 12f;

        private bool      _active;
        private JobData   _job;
        private Vector2[] _waypoints;    // canvas kök yerel koordinatlarında
        private float[]   _segLengths;   // ardışık waypoint'ler arası mesafeler
        private float     _totalLength;
        private float     _tracedLength; // şimdiye kadar iz bırakılan toplam mesafe

        // İz sürülürken örneklenen doğruluk birikimi
        private float _accuracySum;
        private int   _accuracySamples;

        private readonly List<Image> _segments = new();

        // ScreenPointToLocalPointInRectangle için kök canvas RectTransform
        private RectTransform _canvasRT;

        void Awake()
        {
            _canvasRT = GetComponent<RectTransform>();
            if (panel != null) panel.SetActive(false);
        }

        void OnDisable()
        {
            if (GameManager.Instance == null) return;
            JM.OnJobStarted   -= HandleJobStarted;
            JM.OnJobCompleted -= HandleJobEnded;
            JM.OnJobAbandoned -= HandleJobAbandoned;
        }

        void Start()
        {
            cancelButton.onClick.AddListener(OnCancelPressed);
            if (GameManager.Instance?.JobManager == null)
            {
                Debug.LogError("[LineTrace] JobManager null — event aboneliği yapılamadı.");
                return;
            }
            JM.OnJobStarted   += HandleJobStarted;
            JM.OnJobCompleted += HandleJobEnded;
            JM.OnJobAbandoned += HandleJobAbandoned;
        }

        // Gerçek zamanlı dokunma takibi için Update zorunlu
        void Update()
        {
            if (!_active) return;

            bool pressed = Pointer.current != null && Pointer.current.press.isPressed;
            if (!pressed) return;

            // Ekran koordinatını canvas kök yerel uzayına çevir
            Vector2 screenPos = Pointer.current.position.ReadValue();
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRT, screenPos, null, out Vector2 localPos))
                return;

            Vector2 frontier = GetPointAtDistance(_tracedLength);
            float   dist     = Vector2.Distance(localPos, frontier);

            if (dist <= maxDeviation)
            {
                // 1 = tam isabetli (dist=0), 0 = tolerans sınırında (dist=maxDeviation)
                _accuracySum += 1f - (dist / maxDeviation);
                _accuracySamples++;

                float speed = GameManager.Instance.EnergyManager.GetCurrentSpeedMultiplier();
                _tracedLength = Mathf.Min(_tracedLength + traceSpeed * speed * Time.deltaTime, _totalLength);
                UpdateVisuals();
            }

            if (_tracedLength >= _totalLength)
                Finish();
        }

        // Yalnızca LineTrace türündeki işler için paneli açar
        private void HandleJobStarted(JobData job)
        {
            if (job.jobType != JobType.LineTrace) return;
            _job              = job;
            jobTitleText.text = job.jobTitle;
            payoutText.text   = $"{job.basePayout:N0} coins";
            BeginTrace();
            panel.SetActive(true);
        }

        private void HandleJobEnded(JobData job, float payout) => HidePanel();
        private void HandleJobAbandoned(JobData job)           => HidePanel();

        private void OnCancelPressed()
        {
            if (!_active) return;
            JM.AbandonJob();
        }

        private void BeginTrace()
        {
            foreach (var seg in _segments)
                if (seg != null) Destroy(seg.gameObject);
            _segments.Clear();

            _tracedLength    = 0f;
            _accuracySum     = 0f;
            _accuracySamples = 0;
            _active          = true;

            GenerateWaypoints();
            BuildSegmentVisuals();
            UpdateVisuals();
        }

        // Yukarıdan aşağıya inen, yatay zikzaklı waypoint dizisi oluşturur
        private void GenerateWaypoints()
        {
            float canvasW = _canvasRT.rect.width;
            float canvasH = _canvasRT.rect.height;

            float margin = 100f;
            float xMin   = -canvasW / 2f + margin;
            float xMax   =  canvasW / 2f - margin;
            // Alt: iptal düğmesine yer bırak; Üst: başlık alanına yer bırak
            float yMin   = -canvasH / 2f + margin + 200f;
            float yMax   =  canvasH / 2f - margin - 260f;

            _waypoints = new Vector2[waypointCount];
            float prevX = Random.Range(xMin, xMax);

            for (int i = 0; i < waypointCount; i++)
            {
                float t = (float)i / (waypointCount - 1);
                float y = Mathf.Lerp(yMax, yMin, t);

                // Her adımda önceki X'ten makul sapma; sınırlar içinde kal
                float stepRange = (xMax - xMin) * 0.35f;
                float x = Mathf.Clamp(prevX + Random.Range(-stepRange, stepRange), xMin, xMax);
                prevX = x;

                _waypoints[i] = new Vector2(x, y);
            }

            _segLengths  = new float[waypointCount - 1];
            _totalLength = 0f;
            for (int i = 0; i < waypointCount - 1; i++)
            {
                _segLengths[i] = Vector2.Distance(_waypoints[i], _waypoints[i + 1]);
                _totalLength  += _segLengths[i];
            }
        }

        // Her segment için rotasyonlu ince dikdörtgen Image oluşturur
        private void BuildSegmentVisuals()
        {
            for (int i = 0; i < _waypoints.Length - 1; i++)
            {
                Vector2 a   = _waypoints[i];
                Vector2 b   = _waypoints[i + 1];
                Vector2 dir = b - a;
                float   len = dir.magnitude;

                GameObject go = new GameObject($"Seg_{i}");
                go.transform.SetParent(lineContainer, false);

                RectTransform rt = go.AddComponent<RectTransform>();
                rt.sizeDelta        = new Vector2(len, lineThickness);
                rt.anchoredPosition = (a + b) * 0.5f;
                rt.localRotation    = Quaternion.Euler(0f, 0f,
                    Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

                Image img = go.AddComponent<Image>();
                img.color = Color.white;
                _segments.Add(img);
            }
        }

        // İlerleme çubuğu, doğruluk metni ve frontier göstergesini günceller
        private void UpdateVisuals()
        {
            float progress = _totalLength > 0f ? _tracedLength / _totalLength : 0f;
            if (progressBarFill != null)
                progressBarFill.fillAmount = progress;

            float accPct = _accuracySamples > 0
                ? (_accuracySum / _accuracySamples) * 100f
                : 100f;
            if (accuracyText != null)
                accuracyText.text = $"{accPct:F0}%";

            if (frontierDot != null)
                frontierDot.anchoredPosition = GetPointAtDistance(_tracedLength);
        }

        // Verilen mesafedeki çizgi üzerindeki noktayı döndürür
        private Vector2 GetPointAtDistance(float distance)
        {
            float remaining = distance;
            for (int i = 0; i < _segLengths.Length; i++)
            {
                if (remaining <= _segLengths[i])
                    return Vector2.Lerp(_waypoints[i], _waypoints[i + 1],
                        remaining / _segLengths[i]);
                remaining -= _segLengths[i];
            }
            return _waypoints[_waypoints.Length - 1];
        }

        private void Finish()
        {
            _active = false;

            float accPct     = _accuracySamples > 0
                ? (_accuracySum / _accuracySamples) * 100f
                : 0f;
            float multiplier = accPct >= 90f ? 1f : accPct >= 70f ? 0.75f : 0.5f;
            float finalPayout = _job != null ? _job.basePayout * multiplier : 0f;

            // HidePanel, OnJobCompleted event zinciri üzerinden HandleJobEnded'da çağrılır
            JM.CompleteJobWithPayout(finalPayout);
        }

        private void HidePanel()
        {
            _active = false;
            _job    = null;
            foreach (var seg in _segments)
                if (seg != null) Destroy(seg.gameObject);
            _segments.Clear();
            panel.SetActive(false);
        }

        // -----------------------------------------------------------------------
        // Editor helper — BuildLineTraceHierarchy
        // Inspector'da sağ tıklayıp bir kez çalıştırın; ardından sahneye wire edin.
        // -----------------------------------------------------------------------

        [ContextMenu("BuildLineTraceHierarchy")]
        private void BuildLineTraceHierarchy()
        {
#if UNITY_EDITOR
            Canvas ownCanvas = gameObject.GetComponent<Canvas>();
            if (ownCanvas == null) ownCanvas = gameObject.AddComponent<Canvas>();
            ownCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            ownCanvas.sortingOrder = 20;

            if (gameObject.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            RectTransform selfRT = gameObject.GetComponent<RectTransform>();
            if (selfRT == null) selfRT = gameObject.AddComponent<RectTransform>();
            selfRT.anchorMin = Vector2.zero;
            selfRT.anchorMax = Vector2.one;
            selfRT.offsetMin = Vector2.zero;
            selfRT.offsetMax = Vector2.zero;

            // ── Panel kökü ───────────────────────────────────────────────────
            GameObject panelGO = new GameObject("LineTracePanel");
            panelGO.transform.SetParent(transform, false);
            RectTransform panelRT = panelGO.AddComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;
            Image panelBg = panelGO.AddComponent<Image>();
            panelBg.color = new Color(0.1f, 0.1f, 0.15f, 0.97f);

            // ── Başlık: iş adı (sol) + ücret (sağ) ─────────────────────────
            GameObject titleGO = CreateLabel("JobTitleText", panelGO.transform,
                new Vector2(0f,    1f), new Vector2(0.65f, 1f),
                new Vector2(20f, -160f), new Vector2(0f, -80f),
                38, FontStyles.Bold, Color.white);

            GameObject payoutGO = CreateLabel("PayoutText", panelGO.transform,
                new Vector2(0.65f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -160f), new Vector2(-20f, -80f),
                32, FontStyles.Normal, new Color(0.4f, 0.9f, 0.4f, 1f));

            // ── İlerleme çubuğu (sol-üst) ───────────────────────────────────
            GameObject pbBgGO = new GameObject("ProgressBarBG");
            pbBgGO.transform.SetParent(panelGO.transform, false);
            RectTransform pbBgRT = pbBgGO.AddComponent<RectTransform>();
            pbBgRT.anchorMin        = new Vector2(0.05f, 1f);
            pbBgRT.anchorMax        = new Vector2(0.62f, 1f);
            pbBgRT.anchoredPosition = new Vector2(0f, -210f);
            pbBgRT.sizeDelta        = new Vector2(0f, 28f);
            Image pbBgImg = pbBgGO.AddComponent<Image>();
            pbBgImg.color = new Color(0.25f, 0.25f, 0.25f, 1f);

            GameObject pbFillGO = new GameObject("ProgressBarFill");
            pbFillGO.transform.SetParent(pbBgGO.transform, false);
            RectTransform pbFillRT = pbFillGO.AddComponent<RectTransform>();
            pbFillRT.anchorMin = Vector2.zero;
            pbFillRT.anchorMax = Vector2.one;
            pbFillRT.offsetMin = Vector2.zero;
            pbFillRT.offsetMax = Vector2.zero;
            Image pbFillImg = pbFillGO.AddComponent<Image>();
            pbFillImg.color      = new Color(0.2f, 0.7f, 1f, 1f);
            pbFillImg.type       = Image.Type.Filled;
            pbFillImg.fillMethod = Image.FillMethod.Horizontal;
            pbFillImg.fillOrigin = 0;
            pbFillImg.fillAmount = 0f;

            // ── Doğruluk % göstergesi (sağ-üst) ────────────────────────────
            GameObject accGO = CreateLabel("AccuracyText", panelGO.transform,
                new Vector2(0.65f, 1f), new Vector2(0.95f, 1f),
                new Vector2(0f, -225f), new Vector2(0f, -175f),
                28, FontStyles.Normal, new Color(1f, 0.85f, 0.2f, 1f));

            // ── Çizgi segment container ──────────────────────────────────────
            GameObject lineContGO = new GameObject("LineContainer");
            lineContGO.transform.SetParent(panelGO.transform, false);
            RectTransform lineContRT = lineContGO.AddComponent<RectTransform>();
            lineContRT.anchorMin = Vector2.zero;
            lineContRT.anchorMax = Vector2.one;
            lineContRT.offsetMin = Vector2.zero;
            lineContRT.offsetMax = Vector2.zero;

            // ── Frontier göstergesi (turuncu daire) ─────────────────────────
            GameObject dotGO = new GameObject("FrontierDot");
            dotGO.transform.SetParent(lineContGO.transform, false);
            RectTransform dotRT = dotGO.AddComponent<RectTransform>();
            dotRT.sizeDelta = new Vector2(36f, 36f);
            dotRT.anchorMin = new Vector2(0.5f, 0.5f);
            dotRT.anchorMax = new Vector2(0.5f, 0.5f);
            Image dotImg = dotGO.AddComponent<Image>();
            dotImg.color = new Color(1f, 0.5f, 0f, 1f);

            // ── İptal düğmesi ────────────────────────────────────────────────
            GameObject btnGO = new GameObject("CancelButton");
            btnGO.transform.SetParent(panelGO.transform, false);
            RectTransform btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.anchorMin        = new Vector2(0.3f, 0f);
            btnRT.anchorMax        = new Vector2(0.7f, 0f);
            btnRT.anchoredPosition = new Vector2(0f, 120f);
            btnRT.sizeDelta        = new Vector2(0f, 80f);
            Image btnImg = btnGO.AddComponent<Image>();
            btnImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);
            Button btn = btnGO.AddComponent<Button>();

            GameObject btnLabelGO = new GameObject("Label");
            btnLabelGO.transform.SetParent(btnGO.transform, false);
            RectTransform btnLabelRT = btnLabelGO.AddComponent<RectTransform>();
            btnLabelRT.anchorMin = Vector2.zero;
            btnLabelRT.anchorMax = Vector2.one;
            btnLabelRT.offsetMin = Vector2.zero;
            btnLabelRT.offsetMax = Vector2.zero;
            TextMeshProUGUI btnLabel = btnLabelGO.AddComponent<TextMeshProUGUI>();
            btnLabel.text      = "CANCEL";
            btnLabel.fontSize  = 32;
            btnLabel.fontStyle = FontStyles.Bold;
            btnLabel.alignment = TextAlignmentOptions.Center;
            btnLabel.color     = Color.white;

            // ── SerializeField referanslarını ata ────────────────────────────
            panel           = panelGO;
            lineContainer   = lineContRT;
            jobTitleText    = titleGO.GetComponent<TextMeshProUGUI>();
            payoutText      = payoutGO.GetComponent<TextMeshProUGUI>();
            progressBarFill = pbFillImg;
            accuracyText    = accGO.GetComponent<TextMeshProUGUI>();
            frontierDot     = dotRT;
            cancelButton    = btn;

            panelGO.SetActive(false);

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("LineTraceMinigame hierarchy built.");
#endif
        }

#if UNITY_EDITOR
        private static GameObject CreateLabel(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax,
            float fontSize, FontStyles style, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = name;
            tmp.fontSize  = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = color;
            return go;
        }
#endif
    }
}
