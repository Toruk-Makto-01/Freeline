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
        
        // --- YENİ %100 AÇLIK SİSTEMİ ---
        public float CurrentHunger { get; private set; } // 0 (Çok Aç) ile 100 (Tam Tok) arası
        public bool IsHungry => CurrentHunger <= 0f; // Açlık 0 ise debuff (ceza) uygulanır

        public event Action<float, float> OnEnergyChanged;
        public event Action OnLowEnergy;
        public event Action OnEnergyDepleted;
        public event Action<float, float> OnHungerChanged; // Market ve HUD bu event ile güncellenebilir (isteğe bağlı)

        private bool _lowEnergyFired;
        private bool _depletedFired;
        private readonly List<ActiveBuffSaveData> _activeBuffs = new();

        void Awake() => CurrentEnergy = config.maxEnergy;

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

        void Update() => CheckBuffExpirations();

        public void LoadState(float energy, float hunger, List<ActiveBuffSaveData> savedBuffs)
        {
            CurrentEnergy = Mathf.Clamp(energy, 0f, config.maxEnergy);
            CurrentHunger = Mathf.Clamp(hunger, 0f, 100f);
            
            _lowEnergyFired = CurrentEnergy <= config.energyDepletionWarningThreshold;
            _depletedFired = CurrentEnergy <= 0f;
            
            _activeBuffs.Clear();
            if (savedBuffs != null) _activeBuffs.AddRange(savedBuffs);

            OnEnergyChanged?.Invoke(CurrentEnergy, config.maxEnergy);
        }

        public int GetHungerPercentage() => Mathf.FloorToInt(CurrentHunger);

        public void RestoreHunger(float amount)
        {
            CurrentHunger = Mathf.Clamp(CurrentHunger + amount, 0f, 100f);
            OnHungerChanged?.Invoke(CurrentHunger, 100f);
        }

        public void ApplyConsumable(ConsumableItemData item)
        {
            switch (item.effectType)
            {
                case ConsumableEffectType.InstantEnergy:
                    RestoreEnergyDirect(item.effectValue);
                    break;
                case ConsumableEffectType.HungerRelief:
                    // effectValue = 50 ise, doğrudan %50 tok tutar! Mantıklı ve kusursuz.
                    RestoreHunger(item.effectValue);
                    break;
                case ConsumableEffectType.EnergyCostReduction:
                case ConsumableEffectType.EnergyRegenOverTime:
                    DateTime endTime = DateTime.Now.AddMinutes(item.durationInMinutes);
                    _activeBuffs.Add(new ActiveBuffSaveData 
                    {
                        effectType = item.effectType,
                        effectValue = item.effectValue,
                        endTimeString = endTime.ToString("O")
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
                if (DateTime.TryParse(_activeBuffs[i].endTimeString, out DateTime endTime) && DateTime.Now >= endTime)
                {
                    _activeBuffs.RemoveAt(i);
                    buffRemoved = true;
                }
            }
            if (buffRemoved) GameManager.Instance.SaveManager.SaveGame(); 
        }

        public float CalculateEnergyCost(float baseCost)
        {
            float finalCost = baseCost;
            foreach (var buff in _activeBuffs)
            {
                if (buff.effectType == ConsumableEffectType.EnergyCostReduction) finalCost -= buff.effectValue; 
            }
            return Mathf.Max(1f, finalCost); 
        }

        public float GetCurrentSpeedMultiplier() => 1f;

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
            // config.hungerThresholdHours'ı (örn: 24) kullanarak saatlik düşüşü hesaplıyoruz.
            // 24 saatte 100'den 0'a düşmesi için saatte ~4.16 düşer.
            float decayRate = 100f / config.hungerThresholdHours; 
            
            CurrentHunger = Mathf.Clamp(CurrentHunger - (delta * decayRate), 0f, 100f);
            OnHungerChanged?.Invoke(CurrentHunger, 100f);
        }

        public List<ActiveBuffSaveData> GetActiveBuffs() => _activeBuffs;
    }
}