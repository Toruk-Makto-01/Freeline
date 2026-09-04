using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Freeline
{
    public class DailyReportPanel : MonoBehaviour
    {
        [Header("UI Referansları")]
        [SerializeField] private Transform contentContainer; // Prefab'ların dizileceği yer
        [SerializeField] private GameObject transactionRowPrefab; // Fatura satırı şablonu
        [SerializeField] private TextMeshProUGUI netProfitText;
        [SerializeField] private TextMeshProUGUI rentStatusText;

        [Header("Butonlar")]
        [SerializeField] private Button wakeUpButton; // Yeni güne başla
        [SerializeField] private Button payCoinButton; // 1500 Coin ile öde (Gizli)
        [SerializeField] private Button payGemButton; // 20 Gem ile öde (Gizli)

        [Header("Kira Ayarları")]
        [SerializeField] private float rentCoinCost = 1500f;
        [SerializeField] private int rentGemCost = 20;
        [SerializeField] private int rentCycleDays = 30;

        private void Awake()
        {
            if (wakeUpButton != null) wakeUpButton.onClick.AddListener(WakeUpAndClose);
            if (payCoinButton != null) payCoinButton.onClick.AddListener(() => PayRent(false));
            if (payGemButton != null) payGemButton.onClick.AddListener(() => PayRent(true));
        }

        public void OpenPanel()
        {
            gameObject.SetActive(true);
            ProcessWebtoonIncome(); // Faturadan önce Webtoon gelirini tahsil et!
            ProcessRentLogic();     // Kira kontrolünü yap
            RefreshUI();            // Faturayı çiz
        }

        private void ProcessWebtoonIncome()
        {
            var wm = WebtoonManager.Instance;
            if (wm != null && wm.TotalFollowers > 0)
            {
                float webtoonIncome = wm.DailyIncome;

                // Parayı ana kasaya ekle
                GameManager.Instance.SaveManager.AddCoins(webtoonIncome);

                // Makbuza "Gelir (true)" olarak yazdır
                GameManager.Instance.SaveManager.LogDailyTransaction("Webtoon Pasif Gelir", webtoonIncome, true);
            }
        }

        private void ProcessRentLogic()
        {
            var data = GameManager.Instance.SaveManager.CurrentData;

            // 1. İFLAS KONTROLÜ (Ek süre bittiyse)
            if (data.isGracePeriodActive && data.rentGraceDaysLeft <= 0)
            {
                Debug.Log("<color=red>SÜRE DOLDU! İFLAS ETTİNİZ! (Game Over Ekranı Gelecek)</color>");
                return;
            }

            // 2. KİRA GÜNÜ GELDİYSE
            if (data.currentRentDay >= rentCycleDays && !data.isGracePeriodActive)
            {
                if (data.currentCoins >= rentCoinCost)
                {
                    // Otomatik Öde
                    GameManager.Instance.SaveManager.AddCoins(-rentCoinCost);
                    data.currentRentDay = 1;

                    // Görsel olarak faturaya yansıması için listeye ekliyoruz
                    GameManager.Instance.SaveManager.LogDailyTransaction("Aylık Ev Kirası", rentCoinCost, false);
                }
                else
                {
                    // Para yetmedi, 10 günlük kriz süreci başlıyor
                    data.isGracePeriodActive = true;
                    data.rentGraceDaysLeft = 10;
                }
            }
        }

        private void RefreshUI()
        {
            ClearTransactions();
            PopulateTransactions();
            UpdateNetProfit();
            UpdateRentStatus();
        }

        private void ClearTransactions()
        {
            foreach (Transform child in contentContainer)
            {
                Destroy(child.gameObject);
            }
        }

        private void PopulateTransactions()
        {
            var data = GameManager.Instance.SaveManager.CurrentData;
            if (data.dailyTransactions == null || data.dailyTransactions.Count == 0) return;

            foreach (var t in data.dailyTransactions)
            {
                GameObject row = Instantiate(transactionRowPrefab, contentContainer);

                // Şablondaki Text'leri buluyoruz (0: İsim, 1: Fiyat)
                TextMeshProUGUI[] texts = row.GetComponentsInChildren<TextMeshProUGUI>();

                if (texts.Length >= 2)
                {
                    string amountText = t.amount > 1 ? $" x{t.amount}" : "";
                    texts[0].text = $"{t.itemName}{amountText}";

                    if (t.isIncome)
                        texts[1].text = $"<color=#00FF00>+{t.totalPrice}</color>";
                    else
                        texts[1].text = $"<color=#FF0000>-{t.totalPrice}</color>";
                }
            }
        }

        private void UpdateNetProfit()
        {
            var data = GameManager.Instance.SaveManager.CurrentData;
            float netProfit = data.dailyIncome - data.dailyExpense;

            if (netProfit >= 0)
                netProfitText.text = $"Bugünkü Net Kâr: <color=#00FF00>+{netProfit} Coin</color>";
            else
                netProfitText.text = $"Bugünkü Net Kâr: <color=#FF0000>{netProfit} Coin</color>";
        }

        private void UpdateRentStatus()
        {
            var data = GameManager.Instance.SaveManager.CurrentData;

            // Varsayılan olarak ödeme butonlarını gizle
            payCoinButton.gameObject.SetActive(false);
            payGemButton.gameObject.SetActive(false);
            wakeUpButton.gameObject.SetActive(true);

            if (data.isGracePeriodActive)
            {
                rentStatusText.text = $"<color=#FF0000>KİRA ÖDENEMEDİ! İflas için son {data.rentGraceDaysLeft} gün!</color>";

                // Oyuncu elden ödeme yapabilsin diye butonları göster
                payCoinButton.gameObject.SetActive(true);
                payGemButton.gameObject.SetActive(true);
            }
            else
            {
                int daysLeft = rentCycleDays - data.currentRentDay;
                rentStatusText.text = $"Kira Ödemesine Kalan Gün: {daysLeft}";
            }
        }

        private void PayRent(bool payWithGem)
        {
            var data = GameManager.Instance.SaveManager.CurrentData;
            bool success = false;

            if (payWithGem)
            {
                if (data.currentGems >= rentGemCost)
                {
                    data.currentGems -= rentGemCost;
                    success = true;
                }
            }
            else
            {
                if (data.currentCoins >= rentCoinCost)
                {
                    GameManager.Instance.SaveManager.AddCoins(-rentCoinCost);
                    success = true;
                }
            }

            if (success)
            {
                data.isGracePeriodActive = false;
                data.currentRentDay = 1;
                data.rentGraceDaysLeft = 10;
                GameManager.Instance.SaveManager.SaveGame();

                RefreshUI(); // Paneli yenile ki kırmızı uyarı gitsin!
            }
        }

        private void WakeUpAndClose()
        {
            // 1. Karakteri uyut ve zamanı ileri al (8 saat)
            GameManager.Instance.TimeManager.Sleep(8f);

            // 2. Paneli kapat
            gameObject.SetActive(false);

            // 3. Dünün harcamalarını yarına yansımaması için sil!
            var data = GameManager.Instance.SaveManager.CurrentData;
            data.dailyIncome = 0f;
            data.dailyExpense = 0f;
            data.dailyTransactions.Clear();

            GameManager.Instance.SaveManager.SaveGame();
        }
    }
}