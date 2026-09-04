using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Freeline
{
    public class DrawingDeskPanel : MonoBehaviour
    {
        [Header("Üst Bilgi")]
        [SerializeField] private TextMeshProUGUI jobTitleText;
        [SerializeField] private TextMeshProUGUI progressText;
        
        [Header("Mini Oyunlar")]
        [SerializeField] private DrawRevealGame revealGame;
        [SerializeField] private SlideBarGame slideBarGame; 

        [Header("Tamamlandı Bildirim Penceresi")]
        [SerializeField] private GameObject completionPopup;
        [SerializeField] private TextMeshProUGUI popupRewardText;
        [SerializeField] private Button popupCloseButton;

        [Header("Geri Dönülecek Panel")]
        [SerializeField] private GameObject phonePanel;

        private JobData _currentJob;

        private void Awake()
        {
            if (popupCloseButton != null)
            {
                popupCloseButton.onClick.RemoveAllListeners();
                popupCloseButton.onClick.AddListener(OnPopupCloseClicked);
            }
        }

        private void OnEnable()
        {
            if (revealGame != null)
            {
                revealGame.OnProgressChanged += HandleProgressChanged;
                revealGame.OnDrawingCompleted += HandleJobCompleted;
            }

            if (slideBarGame != null)
            {
                slideBarGame.OnGameFinished += HandleSlideBarFinished;
            }
        }

        private void OnDisable()
        {
            if (revealGame != null)
            {
                revealGame.OnProgressChanged -= HandleProgressChanged;
                revealGame.OnDrawingCompleted -= HandleJobCompleted;
            }

            if (slideBarGame != null)
            {
                slideBarGame.OnGameFinished -= HandleSlideBarFinished;
            }
        }

        public void StartJob(JobData job)
        {
            _currentJob = job;
            if (jobTitleText != null) jobTitleText.text = job.jobTitle;
            if (progressText != null) progressText.text = "%0";
            if (completionPopup != null) completionPopup.SetActive(false);

            GameManager.Instance.EnergyManager.ConsumeEnergy(job.energyCost);
            HUDManager.Instance.RefreshAllUI();

            // TAMAMEN BAĞIMSIZ ÇALIŞMA MANTIĞI
            if (job.difficulty == JobDifficulty.Beginner)
            {
                if (revealGame != null) revealGame.gameObject.SetActive(false);
                if (slideBarGame != null)
                {
                    slideBarGame.gameObject.SetActive(true);
                    slideBarGame.Setup();
                }
            }
            else
            {
                if (slideBarGame != null) slideBarGame.gameObject.SetActive(false);
                if (revealGame != null)
                {
                    revealGame.gameObject.SetActive(true);
                    revealGame.Setup();
                }
            }
        }

        private void HandleProgressChanged(float rawProgress)
        {
            if (progressText != null)
            {
                int percent = Mathf.Clamp(Mathf.FloorToInt(rawProgress * 100f), 0, 100);
                progressText.text = $"%{percent}";
            }
        }

        private void HandleSlideBarFinished(bool isSuccess)
        {
            if (isSuccess) HandleJobCompleted();
            else
            {
                gameObject.SetActive(false);
                if (phonePanel != null) phonePanel.SetActive(true);
            }
        }

        private void HandleJobCompleted()
        {
            if (_currentJob != null)
            {
                // Parayı kasaya ekle
                GameManager.Instance.SaveManager.CurrentData.currentCoins += _currentJob.basePayout;
                
                // FATURAYA YAZDIRMA KODU (YENİ EKLENDİ)
                GameManager.Instance.SaveManager.LogDailyTransaction("Freelance İş", _currentJob.basePayout, true);

                GameManager.Instance.SaveManager.SaveGame();
                HUDManager.Instance.RefreshAllUI();

                if (popupRewardText != null) popupRewardText.text = $"+{_currentJob.basePayout} Coin";
                if (completionPopup != null) completionPopup.SetActive(true);
            }
        }

        private void OnPopupCloseClicked()
        {
            if (completionPopup != null) completionPopup.SetActive(false);
            gameObject.SetActive(false);
            if (phonePanel != null) phonePanel.SetActive(true);
        }
    }
}