using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Freeline
{
    public class ExhibitionSummaryPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panel;

        [SerializeField] private TextMeshProUGUI soldText;
        [SerializeField] private TextMeshProUGUI earnedText;
        [SerializeField] private TextMeshProUGUI remainingText;

        [SerializeField] private Button okButton;

        private void Awake()
        {
            okButton.onClick.AddListener(Close);
        }

        private void Start()
        {
            panel.SetActive(false);
        }

        public void Open(int sold, int earned, int remaining)
        {
            panel.SetActive(true);

            soldText.text = $"Satılan Ürün : {sold}";
            earnedText.text = $"Kazanılan Coin : {earned}";
            remainingText.text = $"Kalan Ürün : {remaining}";
        }

        public void Close()
        {
            panel.SetActive(false);
        }
    }
}