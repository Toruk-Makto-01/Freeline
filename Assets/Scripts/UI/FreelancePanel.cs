using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Freeline
{
    public class FreelancePanel : MonoBehaviour
    {
        [Header("UI Elementleri")]
        [SerializeField] private FreelanceSlot[] jobSlots; // 3 adet slot objesi
        [SerializeField] private Button refreshButton;
        [SerializeField] private TextMeshProUGUI refreshCostText;
        [SerializeField] private Button closeButton;

        [Header("Ayarlar")]
        [SerializeField] private int refreshGemCost = 5;
        [SerializeField] private List<JobData> allFreelanceJobs; // Havuzdaki tüm freelance işler


        public void OpenPanel() => gameObject.SetActive(true);
        public void ClosePanel() => gameObject.SetActive(false);
        private void OnEnable()
        {
            if (refreshButton != null)
            {
                refreshButton.onClick.RemoveAllListeners();
                refreshButton.onClick.AddListener(OnRefreshClicked);
            }
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(ClosePanel);
            }
            UpdateRefreshUI();
            RefreshJobs(); // Panel açıldığında listeyi doldur
        }

        private void UpdateRefreshUI()
        {
            var data = GameManager.Instance.SaveManager.CurrentData;
            if (data.dailyFreelanceRefreshCount == 0)
            {
                refreshCostText.text = "Ücretsiz Yenile";
            }
            else
            {
                refreshCostText.text = $"{refreshGemCost} Gem Yenile";
            }
        }

        private void OnRefreshClicked()
        {
            var data = GameManager.Instance.SaveManager.CurrentData;

            if (data.dailyFreelanceRefreshCount > 0)
            {
                if (data.currentGems >= refreshGemCost)
                {
                    data.currentGems -= refreshGemCost;
                    HUDManager.Instance.RefreshAllUI(); // Bakiye düştüğü için üst barı anında güncelle
                }
                else
                {
                    Debug.Log("Yeterli Gem yok!");
                    return; // Parası yetmiyorsa yenilemeyi iptal et
                }
            }

            data.dailyFreelanceRefreshCount++;
            GameManager.Instance.SaveManager.SaveGame();

            UpdateRefreshUI();
            RefreshJobs();
        }

        private void RefreshJobs()
        {
            if (allFreelanceJobs == null || allFreelanceJobs.Count == 0) return;

            List<JobData> selectedJobs = GetRandomJobs(3);
            for (int i = 0; i < jobSlots.Length; i++)
            {
                if (i < selectedJobs.Count)
                {
                    jobSlots[i].gameObject.SetActive(true);
                    jobSlots[i].Setup(selectedJobs[i], OnJobSelected);
                }
            }
        }

        private void OnJobSelected(JobData job)
        {
            ClosePanel();
            // İş seçildiğinde doğrudan mini oyuna köprü at
            HUDManager.Instance.TransitFromTabletToDrawingDesk(job);
        }

        private List<JobData> GetRandomJobs(int count)
        {
            // Listeyi karıştırıp belirtilen sayı kadar iş döndürür
            List<JobData> shuffled = new List<JobData>(allFreelanceJobs);
            for (int i = 0; i < shuffled.Count; i++)
            {
                JobData temp = shuffled[i];
                int randomIndex = Random.Range(i, shuffled.Count);
                shuffled[i] = shuffled[randomIndex];
                shuffled[randomIndex] = temp;
            }
            return shuffled.GetRange(0, Mathf.Min(count, shuffled.Count));
        }
    }
}