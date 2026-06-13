#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Freeline
{
    public class ZenitoonPanel : MonoBehaviour
    {
        // ---- Sabitler -------------------------------------------------------
        private const float HeaderH       = 80f;
        private const float BottomBarH    = 80f;
        private const float CardH         = 200f;
        private const float CardGap       = 16f;
        private const float CardPadV      = 8f;
        private const float CardPadH      = 12f;
        private const float BtnH          = 48f;
        private const float TitleFontSize = 36f;
        private const float TitlePadLeft  = 20f;
        private const float CardTitleFS   = 28f;
        private const float CardInfoFS    = 22f;

        private static readonly Color ColBeginner     = new Color(0.60f, 0.85f, 0.60f);
        private static readonly Color ColIntermediate = new Color(0.95f, 0.85f, 0.40f);
        private static readonly Color ColAdvanced     = new Color(0.95f, 0.65f, 0.30f);

        // ---- Kart referansları (editor'da doldurulur) -----------------------
        [Serializable]
        private struct JobCardRefs
        {
            public Image           bgImage;
            public TextMeshProUGUI titleText;
            public TextMeshProUGUI durationText;
            public TextMeshProUGUI payoutText;
            public TextMeshProUGUI difficultyText;
            public Button          selectBtn;
            public GameObject      lockOverlay;
        }

        [SerializeField] private Button          _refreshBtn;
        [SerializeField] private TextMeshProUGUI _refreshBtnLabel;
        [SerializeField] private Button          _backBtn;
        [SerializeField] private JobCardRefs[]   _cards = new JobCardRefs[3];

        // =========================================================================
        // Runtime
        // =========================================================================

        void Awake()
        {
            _refreshBtn.onClick.AddListener(OnRefreshClicked);
            _backBtn.onClick.AddListener(Close);

            for (int i = 0; i < _cards.Length; i++)
            {
                int captured = i;
                _cards[i].selectBtn.onClick.AddListener(() => OnSelectClicked(captured));
            }
        }

        void OnEnable()
        {
            GameManager.Instance.JobManager.OnJobBoardRefreshed += HandleBoardRefreshed;
            RefreshCards();
        }

        void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.JobManager.OnJobBoardRefreshed -= HandleBoardRefreshed;
        }

        public void Open()
        {
            gameObject.SetActive(true);
            // RefreshCards OnEnable'da çağrılır
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        private void RefreshCards()
        {
            List<JobData> jobs          = GameManager.Instance.JobManager.GetBoardJobs();
            float         currentEnergy = GameManager.Instance.EnergyManager.CurrentEnergy;

            for (int i = 0; i < _cards.Length; i++)
            {
                JobCardRefs card = _cards[i];

                if (i >= jobs.Count)
                {
                    card.bgImage.gameObject.SetActive(false);
                    continue;
                }

                JobData job       = jobs[i];
                bool    canAfford = currentEnergy >= job.energyCost;

                Color baseColor = DifficultyColor(job.difficulty);
                card.bgImage.color         = canAfford ? baseColor : baseColor * 0.6f;
                card.titleText.text        = job.jobTitle;
                card.durationText.text     = $"Sure: {job.durationHours:0.#} saat";
                card.payoutText.text       = $"Odeme: {job.basePayout:0} coin";
                card.difficultyText.text   = DifficultyStars(job.difficulty);
                card.selectBtn.interactable = canAfford;
                card.lockOverlay.SetActive(!canAfford);
            }

            UpdateRefreshLabel();
        }

        private void UpdateRefreshLabel()
        {
            _refreshBtnLabel.text = $"Yenile x{GameManager.Instance.JobManager.RefreshesRemaining}";
        }

        private void OnRefreshClicked()
        {
            GameManager.Instance.JobManager.RefreshJobBoard();
            // HandleBoardRefreshed aracılığıyla RefreshCards çağrılır
        }

        private void OnSelectClicked(int index)
        {
            GameManager.Instance.JobManager.SelectJob(index);
            Close();
        }

        private void HandleBoardRefreshed(IReadOnlyList<JobData> _)
        {
            RefreshCards();
        }

        private static Color DifficultyColor(JobDifficulty d) => d switch
        {
            JobDifficulty.Beginner     => ColBeginner,
            JobDifficulty.Intermediate => ColIntermediate,
            _                          => ColAdvanced,
        };

        private static string DifficultyStars(JobDifficulty d) => d switch
        {
            JobDifficulty.Beginner     => "* - -",
            JobDifficulty.Intermediate => "* * -",
            _                          => "* * *",
        };

        // =========================================================================
        // EDITOR — hiyerarşi kurucusu
        // =========================================================================

#if UNITY_EDITOR

        [ContextMenu("Build Zenitoon Hierarchy")]
        private void BuildZenitoonHierarchy()
        {
            while (transform.childCount > 0)
                DestroyImmediate(transform.GetChild(0).gameObject);

            // ZenitoonPanel ekranı tamamen kaplar
            var selfRT       = GetComponent<RectTransform>();
            selfRT.anchorMin = Vector2.zero;
            selfRT.anchorMax = Vector2.one;
            selfRT.offsetMin = Vector2.zero;
            selfRT.offsetMax = Vector2.zero;

            var selfImg   = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            selfImg.color = new Color(0.10f, 0.10f, 0.14f, 1f);

            BuildHeader();
            BuildJobList();
            BuildBottomBar();

            EditorUtility.SetDirty(gameObject);
            Debug.Log("[ZenitoonPanel] Hierarchy built.");
        }

        // ---- Başlık çubuğu --------------------------------------------------

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
            titleRT.anchorMin = Vector2.zero;
            titleRT.anchorMax = Vector2.one;
            titleRT.offsetMin = new Vector2(TitlePadLeft, 0f);
            titleRT.offsetMax = Vector2.zero;

            var tmp = titleGO.AddComponent<TextMeshProUGUI>();
            tmp.text          = "Zenitoon";
            tmp.fontSize      = TitleFontSize;
            tmp.fontStyle     = FontStyles.Bold;
            tmp.color         = new Color(0.20f, 0.05f, 0.10f, 1f);
            tmp.alignment     = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
        }

        // ---- İş kartları listesi --------------------------------------------

        private void BuildJobList()
        {
            var go = NewUIObj("JobList", transform);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(0f,  BottomBarH);
            rt.offsetMax = new Vector2(0f, -HeaderH);

            var vlg                    = go.AddComponent<VerticalLayoutGroup>();
            vlg.spacing                = CardGap;
            vlg.padding                = new RectOffset((int)CardPadH, (int)CardPadH,
                                                        (int)CardPadV,  (int)CardPadV);
            vlg.childAlignment         = TextAnchor.UpperCenter;
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = false;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;

            for (int i = 0; i < 3; i++)
                _cards[i] = BuildJobCard(i, go.transform);
        }

        // ---- Tek iş kartı ---------------------------------------------------

        private static JobCardRefs BuildJobCard(int index, Transform parent)
        {
            // Kart kapsayıcısı
            var cardGO = NewUIObj($"JobCard_{index}", parent);
            var cardRT = cardGO.GetComponent<RectTransform>();
            cardRT.sizeDelta = new Vector2(0f, CardH);

            // Arka plan (zorluk rengi runtime'da güncellenir)
            var bgGO  = NewUIObj("BgImage", cardGO.transform);
            var bgRT  = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.color = ColBeginner;

            // İş başlığı
            var titleTMP = MakeText("JobTitle",      cardGO.transform,
                                    CardTitleFS, FontStyles.Bold,
                                    new Vector2(12f, -44f), new Vector2(-12f, -10f));

            // Süre
            var durTMP   = MakeText("DurationText",  cardGO.transform,
                                    CardInfoFS, FontStyles.Normal,
                                    new Vector2(12f, -76f), new Vector2(-12f, -46f));
            durTMP.text = "Sure: 2 saat";

            // Ödeme
            var payTMP   = MakeText("PayoutText",    cardGO.transform,
                                    CardInfoFS, FontStyles.Normal,
                                    new Vector2(12f, -108f), new Vector2(-12f, -78f));
            payTMP.text = "Odeme: 150 coin";

            // Zorluk yıldızları
            var diffTMP  = MakeText("DifficultyText", cardGO.transform,
                                    CardInfoFS, FontStyles.Normal,
                                    new Vector2(12f, -140f), new Vector2(-12f, -110f));
            diffTMP.text = "* - -";

            // Enerji yetersizliği uyarısı (başlangıçta gizli)
            var lockGO = NewUIObj("LockOverlay", cardGO.transform);
            var lockRT = lockGO.GetComponent<RectTransform>();
            lockRT.anchorMin = new Vector2(0f, 0f);
            lockRT.anchorMax = new Vector2(1f, 1f);
            lockRT.offsetMin = new Vector2(0f,  BtnH);
            lockRT.offsetMax = Vector2.zero;
            var lockTMP = lockGO.AddComponent<TextMeshProUGUI>();
            lockTMP.text          = "Enerji Yetersiz";
            lockTMP.fontSize      = CardInfoFS;
            lockTMP.color         = new Color(0.80f, 0.20f, 0.20f, 1f);
            lockTMP.alignment     = TextAlignmentOptions.Center;
            lockTMP.raycastTarget = false;
            lockGO.SetActive(false);

            // Seç butonu — kartın altına sabitlenmiş
            var btnGO = NewUIObj("SelectBtn", cardGO.transform);
            var btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0f, 0f);
            btnRT.anchorMax = new Vector2(1f, 0f);
            btnRT.pivot     = new Vector2(0.5f, 0f);
            btnRT.offsetMin = new Vector2(0f, 0f);
            btnRT.offsetMax = new Vector2(0f, BtnH);
            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = new Color(0.20f, 0.55f, 0.20f, 1f);
            var btn = btnGO.AddComponent<Button>();

            var btnLabelGO = NewUIObj("Label", btnGO.transform);
            var btnLabelRT = btnLabelGO.GetComponent<RectTransform>();
            btnLabelRT.anchorMin = Vector2.zero;
            btnLabelRT.anchorMax = Vector2.one;
            btnLabelRT.offsetMin = Vector2.zero;
            btnLabelRT.offsetMax = Vector2.zero;
            var btnTMP = btnLabelGO.AddComponent<TextMeshProUGUI>();
            btnTMP.text          = "Seç";
            btnTMP.fontSize      = CardInfoFS;
            btnTMP.fontStyle     = FontStyles.Bold;
            btnTMP.color         = Color.white;
            btnTMP.alignment     = TextAlignmentOptions.Center;
            btnTMP.raycastTarget = false;

            return new JobCardRefs
            {
                bgImage       = bgImg,
                titleText     = titleTMP,
                durationText  = durTMP,
                payoutText    = payTMP,
                difficultyText = diffTMP,
                selectBtn     = btn,
                lockOverlay   = lockGO,
            };
        }

        // TMP düğümü: pivot sol-üst, offsetMin/Max kart GO'ya göre
        private static TextMeshProUGUI MakeText(string name, Transform parent,
                                                float fontSize, FontStyles style,
                                                Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = NewUIObj(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize      = fontSize;
            tmp.fontStyle     = style;
            tmp.color         = Color.black;
            tmp.alignment     = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            return tmp;
        }

        // ---- Alt çubuk (yenile + geri) --------------------------------------

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

            // Yenile — sol yarı
            var refGO = NewUIObj("RefreshBtn", go.transform);
            var refRT = refGO.GetComponent<RectTransform>();
            refRT.anchorMin = new Vector2(0f,   0.5f);
            refRT.anchorMax = new Vector2(0.5f, 0.5f);
            refRT.pivot     = new Vector2(0f, 0.5f);
            refRT.offsetMin = new Vector2(12f, -BtnH * 0.5f);
            refRT.offsetMax = new Vector2(-6f,  BtnH * 0.5f);
            var refImg   = refGO.AddComponent<Image>();
            refImg.color = new Color(0.30f, 0.30f, 0.40f, 1f);
            _refreshBtn  = refGO.AddComponent<Button>();

            var refLabelGO = NewUIObj("Label", refGO.transform);
            var refLabelRT = refLabelGO.GetComponent<RectTransform>();
            refLabelRT.anchorMin = Vector2.zero;
            refLabelRT.anchorMax = Vector2.one;
            refLabelRT.offsetMin = Vector2.zero;
            refLabelRT.offsetMax = Vector2.zero;
            _refreshBtnLabel           = refLabelGO.AddComponent<TextMeshProUGUI>();
            _refreshBtnLabel.text      = "Yenile x3";
            _refreshBtnLabel.fontSize  = CardInfoFS;
            _refreshBtnLabel.fontStyle = FontStyles.Bold;
            _refreshBtnLabel.color     = Color.white;
            _refreshBtnLabel.alignment = TextAlignmentOptions.Center;
            _refreshBtnLabel.raycastTarget = false;

            // Geri — sağ yarı
            var backGO = NewUIObj("BackBtn", go.transform);
            var backRT = backGO.GetComponent<RectTransform>();
            backRT.anchorMin = new Vector2(0.5f, 0.5f);
            backRT.anchorMax = new Vector2(1f,   0.5f);
            backRT.pivot     = new Vector2(0f, 0.5f);
            backRT.offsetMin = new Vector2(6f,   -BtnH * 0.5f);
            backRT.offsetMax = new Vector2(-12f,  BtnH * 0.5f);
            var backImg   = backGO.AddComponent<Image>();
            backImg.color = new Color(0.35f, 0.20f, 0.20f, 1f);
            _backBtn      = backGO.AddComponent<Button>();

            var backLabelGO = NewUIObj("Label", backGO.transform);
            var backLabelRT = backLabelGO.GetComponent<RectTransform>();
            backLabelRT.anchorMin = Vector2.zero;
            backLabelRT.anchorMax = Vector2.one;
            backLabelRT.offsetMin = Vector2.zero;
            backLabelRT.offsetMax = Vector2.zero;
            var backTMP = backLabelGO.AddComponent<TextMeshProUGUI>();
            backTMP.text          = "← Geri";
            backTMP.fontSize      = CardInfoFS;
            backTMP.fontStyle     = FontStyles.Bold;
            backTMP.color         = Color.white;
            backTMP.alignment     = TextAlignmentOptions.Center;
            backTMP.raycastTarget = false;
        }

        // ---- Yardımcı -------------------------------------------------------

        private static GameObject NewUIObj(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

#endif
    }
}
