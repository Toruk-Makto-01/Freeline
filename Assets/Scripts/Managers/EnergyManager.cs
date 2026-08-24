using System;
using System.Collections.Generic;
using UnityEngine;

namespace Freeline
{
    [DefaultExecutionOrder(-10)]
    public class EnergyManager : MonoBehaviour
    {
        [SerializeField] private EnergyConfig config;

        public float CurrentEnergy { get; private set; }
        public float MaxEnergy => config.maxEnergy;

        public bool IsHungry => _hoursSinceLastFood >= config.hungerThresholdHours;
        public float HoursSinceLastFood => _hoursSinceLastFood;
        public float MaxHungerHours => config.hungerThresholdHours;

        public event Action<float, float> OnEnergyChanged;
        public event Action OnLowEnergy;
        public event Action OnEnergyDepleted;
        public event Action<float, float> OnHungerChanged;

        private float _hoursSinceLastFood;
        private bool _lowEnergyFired;
        private bool _depletedFired;

        // Gerçek zamanlı aktif güçlendirmelerimiz
        private readonly List<ActiveBuffSaveData> _activeBuffs = new();

        void Awake()
        {
            CurrentEnergy = config.maxEnergy;
        }

        void Start()
        {
            TimeManager time = GameManager.Instance.TimeManager;
            time.OnTimeAdvanced += HandleTimeAdvanced;
            time.OnNewDayStarted += HandleNewDay;
        }

        void OnDestroy()
        {
            if (GameManager.Instance == null) return;
            TimeManager time = GameManager.Instance.TimeManager;
            time.OnTimeAdvanced -= HandleTimeAdvanced;
            time.OnNewDayStarted -= HandleNewDay;
        }

        void Update()
        {
            // Her saniye gerçek zamanlı buff'ların süresinin bitip bitmediğini kontrol et
            CheckBuffExpirations();
        }

        public void LoadState(float energy, float hoursSinceLastFood, List<ActiveBuffSaveData> savedBuffs)
        {
            _hoursSinceLastFood = Mathf.Max(0f, hoursSinceLastFood);
            CurrentEnergy = Mathf.Clamp(energy, 0f, config.maxEnergy);

            _lowEnergyFired = CurrentEnergy <= config.energyDepletionWarningThreshold;
            _depletedFired = CurrentEnergy <= 0f;

            _activeBuffs.Clear();
            if (savedBuffs != null)
            {
                _activeBuffs.AddRange(savedBuffs);
            }

            OnEnergyChanged?.Invoke(CurrentEnergy, config.maxEnergy);
        }

        // --- YENİ SİSTEM: MARKET EŞYALARINI (CONSUMABLE) UYGULAMA ---
        public void ApplyConsumable(ConsumableItemData item)
        {
            switch (item.effectType)
            {
                case ConsumableEffectType.InstantEnergy:
                    RestoreEnergyDirect(item.effectValue);
                    break;

                case ConsumableEffectType.HungerRelief:
                    _hoursSinceLastFood = 0f;
                    OnHungerChanged?.Invoke(_hoursSinceLastFood, config.hungerThresholdHours);
                    break;

                case ConsumableEffectType.EnergyCostReduction:
                case ConsumableEffectType.EnergyRegenOverTime:
                    // Süreli bir özellikse gerçek zamanlı olarak listeye ekle
                    DateTime endTime = DateTime.Now.AddMinutes(item.durationInMinutes);
                    _activeBuffs.Add(new ActiveBuffSaveData
                    {
                        effectType = item.effectType,
                        effectValue = item.effectValue,
                        endTimeString = endTime.ToString("O") // Tam tarih formatı
                    });
                    break;
            }
        }

        private void CheckBuffExpirations()
        {
            if (_activeBuffs.Count == 0) return;

            bool buffRemoved = false;
            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                if (DateTime.TryParse(_activeBuffs[i].endTimeString, out DateTime endTime))
                {
                    if (DateTime.Now >= endTime)
                    {
                        _activeBuffs.RemoveAt(i);
                        buffRemoved = true;
                    }
                }
            }

            if (buffRemoved)
            {
                GameManager.Instance.SaveManager.SaveGame(); // Buff bittiyse kaydet
            }
        }

        // JobManager iş yaparken enerji harcayacağında bu fonksiyonu çağırır. 
        // Aktif bir EnergyCostReduction buff'ı varsa maliyeti DÜŞÜRÜR!
        public float CalculateEnergyCost(float baseCost)
        {
            float finalCost = baseCost;
            foreach (var buff in _activeBuffs)
            {
                if (buff.effectType == ConsumableEffectType.EnergyCostReduction)
                {
                    finalCost -= buff.effectValue; // Örn: 25 - 2 = 23 enerji
                }
            }
            return Mathf.Max(1f, finalCost); // İşler en az 1 enerji harcasın
        }

        public void ConsumeEnergy(float amount)
        {
            if (amount <= 0f) return;
            SetEnergy(CurrentEnergy - amount);
        }

        public void RestoreEnergy(float amount)
        {
            if (amount <= 0f) return;
            float effective = IsHungry ? amount * config.hungerPenaltyMultiplier : amount;
            SetEnergy(CurrentEnergy + effective);
        }

        public void RestoreEnergyDirect(float amount)
        {
            if (amount <= 0f) return;
            SetEnergy(CurrentEnergy + amount);
        }

        private void SetEnergy(float value)
        {
            float previous = CurrentEnergy;
            CurrentEnergy = Mathf.Clamp(value, 0f, config.maxEnergy);

            if (Mathf.Approximately(previous, CurrentEnergy)) return;

            OnEnergyChanged?.Invoke(CurrentEnergy, config.maxEnergy);

            if (CurrentEnergy > config.energyDepletionWarningThreshold) _lowEnergyFired = false;
            if (CurrentEnergy > 0f) _depletedFired = false;

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

        private void HandleNewDay(int day)
        {
            CurrentEnergy = config.maxEnergy;
            _lowEnergyFired = false;
            _depletedFired = false;
            OnEnergyChanged?.Invoke(CurrentEnergy, config.maxEnergy);
        }

        private void HandleTimeAdvanced(float previousHour, float newHour)
        {
            float delta = newHour - previousHour;
            _hoursSinceLastFood += delta;
            OnHungerChanged?.Invoke(_hoursSinceLastFood, config.hungerThresholdHours);
        }
        
        // Eski mini oyunların hata vermemesi için eklendi. 
        // İleride markete "1 Saat Boyunca İşleri %50 Hızlı Yap" gibi bir eşya eklersek
        // o mantığı buraya yazacağız. Şimdilik normal hız (1) dönüyoruz.
        public float GetCurrentSpeedMultiplier()
        {
            return 1f;
        }
        // SaveManager kullanacak
        public List<ActiveBuffSaveData> GetActiveBuffs() => _activeBuffs;
    }
}