using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Freeline
{
    public class DrawingDeskPanel : MonoBehaviour
    {
        [Header("Üst Bilgi")]
        [SerializeField] private TextMeshProUGUI jobTitleText;
        [SerializeField] private TextMeshProUGUI progressText; // "%0" yazan metin
        
        [Header("Mini Oyun")]
        [SerializeField] private DrawRevealGame revealGame;

        [Header("Tamamlandı Bildirim Penceresi")]
        [SerializeField] private GameObject completionPopup; // Bildirim paneli objesi
        [SerializeField] private TextMeshProUGUI popupRewardText; // "+150 Coin" yazan metin
        [SerializeField] private Button popupCloseButton; // Kapat butonu

        [Header("Geri Dönülecek Panel")]
        [SerializeField] private GameObject phonePanel; // Kaldığımız telefon veya freelance arayüzü

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
        }

        private void OnDisable()
        {
            if (revealGame != null)
            {
                revealGame.OnProgressChanged -= HandleProgressChanged;
                revealGame.OnDrawingCompleted -= HandleJobCompleted;
            }
        }

        public void StartJob(JobData job)
        {
            _currentJob = job;
            
            if (jobTitleText != null) jobTitleText.text = job.jobTitle;
            if (progressText != null) progressText.text = "%0";
            if (completionPopup != null) completionPopup.SetActive(false);

            // Enerjiyi işe başlarken düş
            GameManager.Instance.EnergyManager.ConsumeEnergy(job.energyCost);
            HUDManager.Instance.RefreshAllUI();

            if (revealGame != null)
            {
                revealGame.gameObject.SetActive(true);
                revealGame.Setup();
            }
        }

        private void HandleProgressChanged(float rawProgress)
        {
            if (progressText != null && revealGame != null)
            {
                // Hedef eşiğe (%85'e) göre oyuncuya 0-100 arası ölçeklendirilmiş oran göster
                float normalizedProgress = rawProgress / revealGame.CompletionThreshold;
                int percent = Mathf.Clamp(Mathf.FloorToInt(normalizedProgress * 100f), 0, 100);
                progressText.text = $"%{percent}";
            }
        }

        private void HandleJobCompleted()
        {
            if (_currentJob != null)
            {
                // Ödülü ekle ve kaydet
                GameManager.Instance.SaveManager.CurrentData.currentCoins += _currentJob.basePayout;
                GameManager.Instance.SaveManager.SaveGame();
                HUDManager.Instance.RefreshAllUI();

                // Bildirim pop-up'ını hazırla ve aç
                if (popupRewardText != null)
                {
                    popupRewardText.text = $"+{_currentJob.basePayout} Coin";
                }

                if (completionPopup != null)
                {
                    completionPopup.SetActive(true);
                }
            }
        }

        private void OnPopupCloseClicked()
        {
            // 1. Bildirim penceresini kapat
            if (completionPopup != null)
            {
                completionPopup.SetActive(false);
            }

            // 2. Çizim masasını kapat
            gameObject.SetActive(false);

            // 3. Telefondaki ilgili paneli tekrar görünür yap
            if (phonePanel != null)
            {
                phonePanel.SetActive(true);
            }
        }
    }
}