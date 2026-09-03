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

        public event Action OnPassedOut; // Bayılma anında tetiklenecek
        private bool _isPassingOut;      // Arka arkaya tetiklenmeyi önlemek için

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

            // 30 saati geçerse ve henüz bayılma işlemi başlamadıysa
            if (GameManager.Instance.SaveManager.CurrentData.hoursAwake >= 30f && !_isPassingOut)
            {
                _isPassingOut = true;
                GameManager.Instance.SaveManager.CurrentData.hasPassOutPenalty = true; // Ceza verildi!
                OnPassedOut?.Invoke();
            }
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
            var data = GameManager.Instance.SaveManager.CurrentData;
            var energyManager = GameManager.Instance.EnergyManager;

            if (data != null && energyManager != null)
            {
                // 1. SADECE NORMAL UYKUYSA CEZAYI KALDIR
                // Eğer 30 saatten az uyanıksa kendi isteğiyle uyuyordur (eski günün cezasını sileriz).
                // Eğer 30 saati geçtiyse az önce bayılmıştır, ceza YARINA KALMALIDIR!
                if (data.hoursAwake < 30f)
                {
                    data.hasPassOutPenalty = false;
                }

                // 2. UYKUDA BAYILMAYI ÖNLEME
                // AdvanceTime arka planda uyanıklığa +8 ekleyecek.
                // 30'u geçip sistemi tetiklemesin diye süreyi eksi uyku saatine çekiyoruz.
                data.hoursAwake = -sleepHours;

                // 3. Saati İleri At (Bu işlem bittiğinde hoursAwake 0'a denk gelecek)
                AdvanceTime(sleepHours);

                // 4. Değerleri Gerçekten Sıfırla ve Yenile
                data.hoursAwake = 0f;
                _isPassingOut = false;

                energyManager.RestoreEnergyDirect(energyManager.MaxEnergy);
                energyManager.RestoreHunger(-40f);

                GameManager.Instance.SaveManager.SaveGame();
            }
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

        public float GetWorkSpeedMultiplier()
        {
            float awake = GameManager.Instance.SaveManager.CurrentData.hoursAwake;
            if (awake >= 24f) return 0.5f;  // %50 daha yavaş (Ağır ceza)
            if (awake >= 20f) return 0.7f;  // %30 daha yavaş
            if (awake >= 16f) return 0.85f; // %15 daha yavaş
            return 1f; // Normal hız
        }
    }
}