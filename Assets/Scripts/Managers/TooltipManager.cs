using UnityEngine;
using TMPro;

namespace Freeline
{
    public class TooltipManager : MonoBehaviour
    {
        public static TooltipManager Instance { get; private set; }

        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descText;

        private void Awake() { Instance = this; }

        public void ShowTooltip(string title, string desc, bool isPositive, Vector2 touchPos)
        {
            tooltipPanel.SetActive(true);
            
            // Paneli parmağın dokunduğu yerin biraz yukarısında göster
            tooltipPanel.transform.position = touchPos + new Vector2(0, 150f); 
            
            string colorHex = isPositive ? "#00FF00" : "#FF0000";
            titleText.text = $"<color={colorHex}>{title}</color>";
            descText.text = desc;
        }

        public void HideTooltip()
        {
            tooltipPanel.SetActive(false);
        }
    }
}