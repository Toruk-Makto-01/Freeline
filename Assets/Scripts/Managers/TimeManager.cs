using System;
using UnityEngine;

namespace Freeline
{
    /// <summary>
    /// Oyun içi saati ve gün döngüsünü yönetir.
    /// Zaman ayrık adımlarla ilerler (gerçek zamanlı değil); her iş veya eylem
    /// tamamlandığında <see cref="AdvanceTime"/> çağrılarak saat ileri alınır.
    /// Gün sonu <see cref="TimeConfig.dayEndHour"/>'a ulaşıldığında otomatik tetiklenir.
    /// </summary>
    public class TimeManager : MonoBehaviour
    {
        [SerializeField] private TimeConfig config;

        /// <summary>Günün şu anki saati (ondalıklı; örn. 14.5 = 14:30).</summary>
        public float CurrentHour { get; private set; }

        /// <summary>Oyun başlangıcından itibaren kaçıncı günde olduğumuz (1'den başlar).</summary>
        public int   CurrentDay  { get; private set; }

        /// <summary>
        /// Saat, uyku penceresinin başlangıcına ulaştıysa <c>true</c> döner.
        /// UI'ın "Uyu" düğmesini etkinleştirmesi için kullanılabilir.
        /// </summary>
        public bool IsInSleepWindow => CurrentHour >= config.sleepWindowStart;

        /// <summary>
        /// Her saat ilerlemesinde tetiklenir.
        /// Parametreler: (önceki saat, yeni saat).
        /// </summary>
        public event Action<float, float> OnTimeAdvanced;

        /// <summary>
        /// Saat o gün ilk kez <see cref="TimeConfig.sleepWindowStart"/> değerini geçtiğinde bir kez tetiklenir.
        /// HUD'ın uyku hatırlatıcısını göstermesi için kullanılabilir.
        /// </summary>
        public event Action OnSleepWindowOpened;

        /// <summary>
        /// Gün bitmeden hemen önce tetiklenir; parametre olarak biten günün numarasını taşır.
        /// SaveManager bu event'i dinleyerek otomatik kayıt yapar.
        /// </summary>
        public event Action<int> OnDayEnded;

        /// <summary>
        /// Saat sıfırlanıp yeni gün başladıktan sonra tetiklenir; yeni günün numarasını taşır.
        /// JobManager bu event'i dinleyerek iş panosunu yeniler.
        /// </summary>
        public event Action<int> OnNewDayStarted;

        // O gün için uyku penceresi bildiriminin yapılıp yapılmadığını takip eder.
        // Aynı gün içinde OnSleepWindowOpened'ın birden fazla kez tetiklenmesini önler.
        private bool _sleepWindowNotified;

        void Awake()
        {
            CurrentDay  = 1;
            CurrentHour = config.startHour;
            _sleepWindowNotified = false;
        }

        /// <summary>
        /// Kayıt dosyasından yüklenen gün ve saat bilgisini uygular.
        /// SaveManager, oyun yüklendikten sonra bu metodu çağırır.
        /// </summary>
        /// <param name="day">Kaydedilen gün numarası.</param>
        /// <param name="hour">Kaydedilen saat (0 – dayEndHour aralığında sıkıştırılır).</param>
        public void LoadState(int day, float hour)
        {
            CurrentDay  = day;
            CurrentHour = Mathf.Clamp(hour, 0f, config.dayEndHour);
            // Geç saatte yükleme yapıldığında uyku penceresi bildirimi yeniden tetiklenmemeli.
            _sleepWindowNotified = CurrentHour >= config.sleepWindowStart;
        }

        /// <summary>
        /// Saati belirtilen miktar kadar ilerletir.
        /// Eğer yeni saat <see cref="TimeConfig.dayEndHour"/>'ı aşarsa gün otomatik sona erer.
        /// JobManager, bir iş tamamlandığında bu metodu çağırır.
        /// </summary>
        /// <param name="hours">İlerletilecek saat miktarı.</param>
        public void AdvanceTime(float hours)
        {
            if (hours <= 0f) return;

            float previous = CurrentHour;
            float next     = CurrentHour + hours;

            if (next >= config.dayEndHour)
            {
                // Gün sonu sınırını aşıyorsa saati tam sınıra sabitle, ardından günü kapat.
                CurrentHour = config.dayEndHour;
                NotifySleepWindowIfCrossed(previous);
                OnTimeAdvanced?.Invoke(previous, CurrentHour);
                SleepNow();
            }
            else
            {
                CurrentHour = next;
                OnTimeAdvanced?.Invoke(previous, CurrentHour);
                NotifySleepWindowIfCrossed(previous);
            }
        }

        /// <summary>
        /// Oyuncunun manuel uyuma isteğini işler.
        /// Uyku penceresi dışındaysa isteği reddeder.
        /// </summary>
        /// <returns>Uyku kabul edildiyse <c>true</c>, pencere dışındaysa <c>false</c>.</returns>
        public bool TrySleep()
        {
            if (!IsInSleepWindow) return false;
            SleepNow();
            return true;
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

        /// <summary>
        /// Saat bu adımda uyku penceresi eşiğini ilk kez geçtiyse <see cref="OnSleepWindowOpened"/>
        /// event'ini tetikler. Her gün yalnızca bir kez çalışır.
        /// </summary>
        private void NotifySleepWindowIfCrossed(float previousHour)
        {
            if (_sleepWindowNotified) return;
            if (CurrentHour >= config.sleepWindowStart && previousHour < config.sleepWindowStart)
            {
                _sleepWindowNotified = true;
                OnSleepWindowOpened?.Invoke();
            }
        }

        /// <summary>
        /// Günü kapatır: <see cref="OnDayEnded"/> tetiklenir, gün sayacı artar,
        /// saat <see cref="TimeConfig.startHour"/>'a sıfırlanır ve <see cref="OnNewDayStarted"/> tetiklenir.
        /// </summary>
        private void SleepNow()
        {
            OnDayEnded?.Invoke(CurrentDay);

            CurrentDay++;
            CurrentHour = config.startHour;
            _sleepWindowNotified = false; // Yeni gün için uyku penceresi bildirimi sıfırlanır.

            OnNewDayStarted?.Invoke(CurrentDay);
        }
    }
}
