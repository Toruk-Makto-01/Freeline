using UnityEngine;

namespace Freeline
{
    /// <summary>
    /// Oyunun başlangıç sırasını yönetir: Kayıtları yükler ve yöneticilere dağıtır.
    /// DefaultExecutionOrder(-100) sayesinde diğer tüm Start() metotlarından önce çalışarak
    /// sistemin (panolar, enerjiler) boş veriyle başlamasını engeller.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class BootstrapManager : MonoBehaviour
    {
        void Start()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.SaveManager == null) return;

            // 1. Kayıtlı veriyi cihazdan oku
            gm.SaveManager.LoadGame();
            
            // 2. Okunan veriyi tüm yöneticilere (Enerji, Zaman, vb.) dağıt
            gm.SaveManager.ApplyToManagers();

            // 3. Durumu konsola yazdır
            LogStatus();
        }

        private void LogStatus()
        {
            var gm = GameManager.Instance;
            var time = gm.TimeManager;
            var energy = gm.EnergyManager;
            var save = gm.SaveManager.CurrentData;
            var wm = WebtoonManager.Instance; // Yeni sistem

            if (save == null) return;

            Debug.Log(
                $"[Freeline] GÜN: {time.CurrentDay} | " +
                $"SAAT: {time.GetFormattedTime()} | " +
                $"ENERJİ: {energy.CurrentEnergy:F0}/{energy.MaxEnergy:F0} | " +
                $"COIN: {save.currentCoins:F0}"
            );

            if (wm != null)
            {
                Debug.Log(
                    $"[Freeline] WEBTOON | " +
                    $"Takipçi: {wm.TotalFollowers} | " +
                    $"Okunmamış Yorum: {wm.UnreadCommentCount} | " +
                    $"Günlük Pasif Gelir: {wm.DailyIncome:F2} Coin"
                );
            }
        }
    }
}