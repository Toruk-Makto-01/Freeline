using System;
using System.Collections.Generic;
using UnityEngine;

namespace Freeline
{
    /// <summary>
    /// Yiyecek tüketiminin iş hızına ve süresine etkisini tanımlayan veri yapısı.
    /// <see cref="EnergyManager.EatFood"/> çağrısıyla aktif buff listesine eklenir.
    /// </summary>
    [Serializable]
    public struct EnergyBuff
    {
        [Tooltip("Job speed multiplier while buff is active (e.g. 1.5 = 50% faster).")]
        public float speedMultiplier;

        [Tooltip("How many in-game hours this buff lasts.")]
        public float durationHours;
    }

    /// <summary>
    /// Oyuncunun enerji, açlık ve yiyecek buff durumlarını yönetir.
    /// Enerji tükenmesi iş başlatmayı engeller; açlık enerji yenileme verimini düşürür.
    /// </summary>
    /// <remarks>
    /// DefaultExecutionOrder(-10): SaveManager'dan önce çalışması gerekir.
    /// OnNewDayStarted tetiklendiğinde enerji önce buradan sıfırlanır,
    /// ardından SaveManager bu temiz sabah anlık görüntüsünü kaydeder.
    /// </remarks>
    [DefaultExecutionOrder(-10)]
    public class EnergyManager : MonoBehaviour
    {
        [SerializeField] private EnergyConfig config;

        /// <summary>Anlık enerji değeri (0 – <see cref="MaxEnergy"/> aralığında).</summary>
        public float CurrentEnergy { get; private set; }

        /// <summary>Maksimum enerji kapasitesi; <see cref="EnergyConfig"/> üzerinden okunur.</summary>
        public float MaxEnergy     => config.maxEnergy;

        /// <summary>
        /// Son yemekten bu yana geçen süre açlık eşiğini (<see cref="EnergyConfig.hungerThresholdHours"/>)
        /// aştıysa <c>true</c> döner. Açken enerji yenilemesi cezalı çalışır.
        /// </summary>
        public bool  IsHungry          => _hoursSinceLastFood >= config.hungerThresholdHours;

        /// <summary>Son yemekten bu yana geçen oyun içi saat miktarı.</summary>
        public float HoursSinceLastFood => _hoursSinceLastFood;

        /// <summary>Açlık eşiği (saat cinsinden); HUD çubuğunun tam dolum noktası.</summary>
        public float MaxHungerHours => config.hungerThresholdHours;

        /// <summary>
        /// Enerji değeri her değiştiğinde tetiklenir.
        /// Parametreler: (mevcut enerji, maksimum enerji).
        /// </summary>
        public event Action<float, float> OnEnergyChanged;

        /// <summary>
        /// Enerji ilk kez uyarı eşiğine düştüğünde bir kez tetiklenir.
        /// Enerji eşiğin üzerine çıkarsa bayrak sıfırlanır ve tekrar tetiklenebilir hale gelir.
        /// </summary>
        public event Action OnLowEnergy;

        /// <summary>
        /// Enerji ilk kez 0'a düştüğünde bir kez tetiklenir.
        /// Enerji 0'ın üzerine çıkarsa bayrak sıfırlanır.
        /// JobManager bu event'i dinleyerek iş başlatmayı engeller.
        /// </summary>
        public event Action OnEnergyDepleted;

        /// <summary>
        /// Açlık durumu değiştiğinde tetiklenir.
        /// Parametreler: (son yemekten bu yana geçen saat, açlık eşiği saati).
        /// HUD açlık çubuğu bu event'i dinler.
        /// </summary>
        public event Action<float, float> OnHungerChanged;

        private readonly List<ActiveBuff> _activeBuffs = new();
        private float _hoursSinceLastFood;

        // Bu iki bayrak, tek seferlik event'lerin aynı eşikte defalarca ateşlenmesini önler.
        private bool  _lowEnergyFired;
        private bool  _depletedFired;

        void Awake()
        {
            CurrentEnergy = config.maxEnergy;
        }

        void Start()
        {
            TimeManager time = GameManager.Instance.TimeManager;
            time.OnTimeAdvanced  += HandleTimeAdvanced;
            time.OnNewDayStarted += HandleNewDay;
        }

        void OnDestroy()
        {
            if (GameManager.Instance == null) return;
            TimeManager time = GameManager.Instance.TimeManager;
            time.OnTimeAdvanced  -= HandleTimeAdvanced;
            time.OnNewDayStarted -= HandleNewDay;
        }

        /// <summary>
        /// Kayıt dosyasından yüklenen enerji ve açlık durumunu uygular.
        /// SaveManager, oyun yüklendikten sonra bu metodu çağırır.
        /// </summary>
        /// <param name="energy">Kaydedilen enerji değeri.</param>
        /// <param name="hoursSinceLastFood">Son yemekten bu yana geçen kaydedilmiş süre.</param>
        public void LoadState(float energy, float hoursSinceLastFood)
        {
            _hoursSinceLastFood = Mathf.Max(0f, hoursSinceLastFood);
            CurrentEnergy       = Mathf.Clamp(energy, 0f, config.maxEnergy);
            // Yüklenirken eşik bayrakları mevcut enerji durumuna göre ayarlanır;
            // böylece zaten düşük enerjide yüklenen bir oyun yanlış event tetiklemez.
            _lowEnergyFired     = CurrentEnergy <= config.energyDepletionWarningThreshold;
            _depletedFired      = CurrentEnergy <= 0f;
            OnEnergyChanged?.Invoke(CurrentEnergy, config.maxEnergy);
        }

        /// <summary>
        /// Belirtilen miktarda enerji tüketir.
        /// JobManager, bir iş tamamlandığında bu metodu çağırır.
        /// </summary>
        /// <param name="amount">Tüketilecek enerji miktarı.</param>
        public void ConsumeEnergy(float amount)
        {
            if (amount <= 0f) return;
            SetEnergy(CurrentEnergy - amount);
        }

        /// <summary>
        /// Enerjiyi yeniler; açlık cezası varsa etkin miktar düşürülür.
        /// Yiyecek veya uyku eylemleri bu metodu kullanır.
        /// </summary>
        /// <param name="amount">İstenen yenileme miktarı (açlık cezasından önce).</param>
        public void RestoreEnergy(float amount)
        {
            if (amount <= 0f) return;
            // Açken enerji yenilemek cezalıdır; yiyecek yemek bu cezayı önler.
            float effective = IsHungry ? amount * config.hungerPenaltyMultiplier : amount;
            SetEnergy(CurrentEnergy + effective);
        }

        /// <summary>
        /// Açlık cezasını atlayarak enerjiyi doğrudan yeniler.
        /// Yalnızca iş terk etme geri ödemesi gibi mekanik geri iadeler için kullanılır.
        /// </summary>
        /// <param name="amount">Doğrudan eklenecek enerji miktarı.</param>
        public void RestoreEnergyDirect(float amount)
        {
            if (amount <= 0f) return;
            SetEnergy(CurrentEnergy + amount);
        }

        /// <summary>
        /// Oyuncu bir yiyecek tükettiğinde çağrılır.
        /// Açlık saatini sıfırlar ve hız buff'ını aktif buff listesine ekler.
        /// </summary>
        /// <param name="buff">Yiyeceğin sağladığı hız ve süre verisi.</param>
        public void EatFood(EnergyBuff buff)
        {
            _hoursSinceLastFood = 0f;
            if (buff.speedMultiplier > 0f && buff.durationHours > 0f)
                _activeBuffs.Add(new ActiveBuff(buff));
            OnHungerChanged?.Invoke(_hoursSinceLastFood, config.hungerThresholdHours);
        }

        /// <summary>
        /// Tüm aktif buff'ların çarpımsal hız çarpanını döndürür.
        /// Hiç buff yoksa 1 (normal hız) döner.
        /// <see cref="DrawingMinigame"/> bu değeri dolgu hızını ölçeklendirmek için kullanır.
        /// </summary>
        public float GetCurrentSpeedMultiplier()
        {
            if (_activeBuffs.Count == 0) return 1f;
            float result = 1f;
            foreach (ActiveBuff b in _activeBuffs)
                result *= b.speedMultiplier;
            return result;
        }

        /// <summary>
        /// Enerji değerini sıkıştırır, event'leri tetikler ve tek-seferlik bayrakları yönetir.
        /// Tüm enerji değişimleri bu merkezi metod üzerinden geçer.
        /// </summary>
        private void SetEnergy(float value)
        {
            float previous = CurrentEnergy;
            CurrentEnergy = Mathf.Clamp(value, 0f, config.maxEnergy);

            // Gerçek bir değişim olmadıysa event tetiklemekten kaçın.
            if (Mathf.Approximately(previous, CurrentEnergy)) return;

            OnEnergyChanged?.Invoke(CurrentEnergy, config.maxEnergy);

            // Enerji eşiğin üzerine çıktıysa bayrakları sıfırla; böylece tekrar düşünce yeniden tetiklenebilir.
            if (CurrentEnergy > config.energyDepletionWarningThreshold)
                _lowEnergyFired = false;
            if (CurrentEnergy > 0f)
                _depletedFired = false;

            if (!_lowEnergyFired && CurrentEnergy <= config.energyDepletionWarningThreshold)
            {
                _lowEnergyFired = true;
                OnLowEnergy?.Invoke();
            }

            if (!_depletedFired && CurrentEnergy <= 0f)
            {
                _depletedFired = true;
                OnEnergyDepleted?.Invoke();
            }
        }

        /// <summary>
        /// Yeni gün başladığında enerjiyi tam doldurur ve buff listesini temizler.
        /// Açlık saati sıfırlanmaz — akşam yemek yemeden uyuyan oyuncu sabah aç uyanır.
        /// </summary>
        private void HandleNewDay(int day)
        {
            CurrentEnergy  = config.maxEnergy;
            _lowEnergyFired = false;
            _depletedFired  = false;
            _activeBuffs.Clear();
            OnEnergyChanged?.Invoke(CurrentEnergy, config.maxEnergy);
        }

        /// <summary>
        /// Zaman ilerlediğinde açlık saatini günceller ve süresi dolan buff'ları kaldırır.
        /// </summary>
        private void HandleTimeAdvanced(float previousHour, float newHour)
        {
            float delta = newHour - previousHour;
            _hoursSinceLastFood += delta;
            OnHungerChanged?.Invoke(_hoursSinceLastFood, config.hungerThresholdHours);

            // Süresi dolan buff'ları kaldırırken indeksi bozmamak için sondan başa iterasyon yapılır.
            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                _activeBuffs[i].remainingHours -= delta;
                if (_activeBuffs[i].remainingHours <= 0f)
                    _activeBuffs.RemoveAt(i);
            }
        }

        /// <summary>
        /// Çalışma zamanında takip edilen aktif buff örneği.
        /// <see cref="EnergyBuff"/> struct'ının değişebilir kopyasıdır.
        /// </summary>
        private class ActiveBuff
        {
            public readonly float speedMultiplier;
            public float remainingHours;

            public ActiveBuff(EnergyBuff buff)
            {
                speedMultiplier = buff.speedMultiplier;
                remainingHours  = buff.durationHours;
            }
        }
    }
}
