#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.UI;

namespace Freeline
{
    public class PhonePanel : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // SerializeField refs
        // -------------------------------------------------------------------------

        [SerializeField] private TMPro.TextMeshProUGUI phoneInfoText;

        [Header("Freelance Uygulamasi")]
        [SerializeField] private Button freelanceButton;
        [SerializeField] private FreelancePanel freelancePanel;

        [Header("App Buttons")]
        [SerializeField] private Button appBtnZenitoon;
        [SerializeField] private Button appBtnWebtoon;
        [SerializeField] private Button appBtnMarket;
        [SerializeField] private Button appBtnProduction;
        [SerializeField] private Button appBtnBackground;

        [Header("Panels")]
        [SerializeField] private MarketPanel marketPanel;
        [SerializeField] private WebtoonPanel webtoonPanel;
        [SerializeField] private ProductionPanel productionPanel;
        [SerializeField] private BackgroundPanel backgroundPanel;

        [Header("Buttons")]
        [SerializeField] private Button exitBtn;

        // =========================================================================
        // Runtime
        // =========================================================================

        void Awake()
        {
            if (freelanceButton != null)
            {
                freelanceButton.onClick.RemoveAllListeners();
                freelanceButton.onClick.AddListener(OnFreelanceClicked);
            }
            if (appBtnMarket != null && marketPanel != null)
                appBtnMarket.onClick.AddListener(marketPanel.Open);
            if (appBtnWebtoon != null && webtoonPanel != null)
                appBtnWebtoon.onClick.AddListener(webtoonPanel.Open);
            if (appBtnProduction != null && productionPanel != null)
                appBtnProduction.onClick.AddListener(productionPanel.Open);
            if (appBtnBackground != null && backgroundPanel != null)
                appBtnBackground.onClick.AddListener(backgroundPanel.Open);
            if (exitBtn != null)
                exitBtn.onClick.AddListener(Close);
        }

        public void Open()
        {
            Debug.Log("[Phone] Open called");
            gameObject.SetActive(true);
        }

        public void Close()
        {
            Debug.Log("[Phone] Close called");
            gameObject.SetActive(false);
        }

        private void OnFreelanceClicked()
        {
            if (freelancePanel != null)
            {
                freelancePanel.OpenPanel();
            }
        }
    }
}