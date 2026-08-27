using System;
using UnityEngine;

namespace Freeline
{
    /// <summary>
    /// Oyun içi saati ve gün döngüsünü yönetir.
    /// Zaman 24 saatlik dilimde akar ve karakter uyuduğunda (veya iş yaptığında) ilerler.
    /// </summary>
    public class TimeManager : MonoBehaviour
    {
        [SerializeField] private TimeConfig config;

        /// <summary>Günün şu anki saati (ondalıklı; örn. 14.5 = 14:30).</summary>
        public float CurrentHour { get; private set; }

        /// <summary>Oyun başlangıcından itibaren kaçıncı günde olduğumuz (1'den başlar).</summary>
        public int CurrentDay { get; private set; }

        /// <summary>Saat her ilerlediğinde tetiklenir (Önceki Saat, Yeni Saat).</summary>
        public event Action<float, float> OnTimeAdvanced;

        /// <summary>Saat değiştiğinde HUD kadranını döndürmek için tetiklenir.</summary>
        public event Action<float> OnHourChanged;

        /// <summary>Gün bittiğinde (Gece 00:00 olduğunda) kayıt almak için tetiklenir.</summary>
        public event Action<int> OnDayEnded;

        /// <summary>Yeni güne geçildiğinde (00:00'dan sonra) iş panosunu yenilemek için tetiklenir.</summary>
        public event Action<int> OnNewDayStarted;

        void Awake()
        {
            CurrentDay = 1;
            CurrentHour = config.startHour;
        }

        /// <summary>
        /// Kayıt dosyasından yüklenen gün ve saat bilgisini uygular.
        /// </summary>
        public void LoadState(int day, float hour)
        {
            CurrentDay = day;
            CurrentHour = Mathf.Clamp(hour, 0f, 24f); // Artık 24 saatlik döngüdeyiz
        }

        /// <summary>
        /// Saati belirtilen miktar kadar ilerletir.
        /// Eğer saat 24.00'ı geçerse otomatik olarak yeni güne atlar.
        /// </summary>
        public void AdvanceTime(float hoursToAdvance)
        {
            float previousHour = CurrentHour;
            CurrentHour += hoursToAdvance;

            // Uyanık kalınan süreyi artır
            if (GameManager.Instance?.SaveManager?.CurrentData != null)
            {
                GameManager.Instance.SaveManager.CurrentData.hoursAwake += hoursToAdvance;
            }

            // Gece 12'yi (24:00) geçme kontrolü
            while (CurrentHour >= 24f)
            {
                CurrentHour -= 24f; // Saati sıfırla (Örn: 25.00 ise 01.00 olur)
                AdvanceDay();       // Yeni güne geç
            }

            OnTimeAdvanced?.Invoke(previousHour, CurrentHour);
            OnHourChanged?.Invoke(CurrentHour);
        }

        /// <summary>
        /// Gece yarısı olduğunda gün atlatma ve kira sayacı mantığını işletir.
        /// </summary>
        private void AdvanceDay()
        {
            OnDayEnded?.Invoke(CurrentDay); // Gün bitiyor haberini ver (Örn: Save almak için)

            CurrentDay++;

            if (GameManager.Instance?.SaveManager?.CurrentData != null)
            {
                var data = GameManager.Instance.SaveManager.CurrentData;

                // Kira döngüsünü ayarla
                if (!data.isGracePeriodActive)
                {
                    data.currentRentDay++;
                }
                else
                {
                    data.rentGraceDaysLeft--;
                }
            }

            OnNewDayStarted?.Invoke(CurrentDay); // Yeni gün başladı haberini ver
        }

        /// <summary>
        /// Oyuncunun "Uyu" butonuna basmasıyla tetiklenir.
        /// Saati 8 saat ileri atar, yorgunluğu sıfırlar ve enerjiyi doldurur.
        /// </summary>
        public void Sleep(float sleepHours = 8f)
        {
            // 1. Saati direkt 8 saat ileri at (Eğer gece 12'yi geçerse AdvanceTime içindeki sistem günü otomatik atlatır)
            AdvanceTime(sleepHours);

            if (GameManager.Instance?.SaveManager?.CurrentData != null && GameManager.Instance?.EnergyManager != null)
            {
                // 2. Uykusuzluk sayacını sıfırla
                GameManager.Instance.SaveManager.CurrentData.hoursAwake = 0f;

                // 3. Enerjiyi fulle, açlığı %40 düşür
                var energyManager = GameManager.Instance.EnergyManager;
                energyManager.RestoreEnergyDirect(energyManager.MaxEnergy);
                energyManager.RestoreHunger(-40f);

                // 4. Oyunu kaydet
                GameManager.Instance.SaveManager.SaveGame();
            }

            // Yeni güne başlarken dünün adisyonunu temizle
            var data = GameManager.Instance.SaveManager.CurrentData;
            data.dailyIncome = 0f;
            data.dailyExpense = 0f;
            data.dailyTransactions.Clear();

            Debug.Log($"[TimeManager] Uyku tamamlandı! {sleepHours} saat geçildi. Uykusuzluk sıfırlandı.");
        }

        /// <summary>
        /// Şu anki saati "SS:DD" biçiminde döndürür (örn. "09:30").
        /// HUD saat metninde doğrudan kullanılabilir.
        /// </summary>
        public string GetFormattedTime()
        {
            int h = Mathf.FloorToInt(CurrentHour);
            int m = Mathf.RoundToInt((CurrentHour - h) * 60f);
            return $"{h:D2}:{m:D2}";
        }
    }
}