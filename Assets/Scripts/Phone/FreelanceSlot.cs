using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Freeline
{
    public class FreelanceSlot : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI energyText;
        [SerializeField] private TextMeshProUGUI rewardText;
        [SerializeField] private TextMeshProUGUI durationText; // YENİ
        [SerializeField] private TextMeshProUGUI difficultyText; // YENİ
        [SerializeField] private Image iconImage;
        [SerializeField] private Button selectButton;

        public void Setup(JobData job, System.Action<JobData> onJobSelected)
        {
            titleText.text = job.jobTitle;
            energyText.text = $"-{job.energyCost} Enerji";
            rewardText.text = $"+{job.basePayout} Coin";
            durationText.text = $"{job.durationHours} Saat";
            
            difficultyText.text = job.difficulty switch
            {
                JobDifficulty.Beginner => "Kolay",
                JobDifficulty.Intermediate => "Orta",
                _ => "Zor"
            };

            if (job.jobIcon != null) iconImage.sprite = job.jobIcon;

            // Enerji yetmiyorsa butonu kapat
            bool hasEnergy = GameManager.Instance.EnergyManager.CurrentEnergy >= job.energyCost;
            selectButton.interactable = hasEnergy;

            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => 
            {
                if (GameManager.Instance.EnergyManager.CurrentEnergy >= job.energyCost)
                    onJobSelected(job);
            });
        }
    }
}