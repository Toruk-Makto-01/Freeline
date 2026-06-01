using System;
using System.Collections.Generic;
using UnityEngine;

namespace Freeline
{
    [Serializable]
    public struct EnergyBuff
    {
        [Tooltip("Job speed multiplier while buff is active (e.g. 1.5 = 50% faster).")]
        public float speedMultiplier;

        [Tooltip("How many in-game hours this buff lasts.")]
        public float durationHours;
    }

    // Runs before SaveManager (order 0) so energy is reset before SaveManager captures state
    // on OnNewDayStarted — keeps the auto-save capturing a clean new-morning snapshot.
    [DefaultExecutionOrder(-10)]
    public class EnergyManager : MonoBehaviour
    {
        [SerializeField] private EnergyConfig config;

        public float CurrentEnergy { get; private set; }
        public float MaxEnergy     => config.maxEnergy;

        // True if no food has been consumed for longer than the hunger threshold.
        public bool  IsHungry          => _hoursSinceLastFood >= config.hungerThresholdHours;
        public float HoursSinceLastFood => _hoursSinceLastFood;

        // (currentEnergy, maxEnergy)
        public event Action<float, float> OnEnergyChanged;

        // Fires once when energy drops to or below energyDepletionWarningThreshold.
        // Resets if energy recovers above the threshold.
        public event Action OnLowEnergy;

        // Fires once when energy hits 0. Resets if energy is restored above 0.
        public event Action OnEnergyDepleted;

        private readonly List<ActiveBuff> _activeBuffs = new();
        private float _hoursSinceLastFood;
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

        // Called by SaveManager after loading a save file.
        public void LoadState(float energy, float hoursSinceLastFood)
        {
            _hoursSinceLastFood = Mathf.Max(0f, hoursSinceLastFood);
            CurrentEnergy       = Mathf.Clamp(energy, 0f, config.maxEnergy);
            _lowEnergyFired     = CurrentEnergy <= config.energyDepletionWarningThreshold;
            _depletedFired      = CurrentEnergy <= 0f;
            OnEnergyChanged?.Invoke(CurrentEnergy, config.maxEnergy);
        }

        // Called by JobManager when a task completes.
        public void ConsumeEnergy(float amount)
        {
            if (amount <= 0f) return;
            SetEnergy(CurrentEnergy - amount);
        }

        // Called by food or sleep items. Applies hunger penalty if applicable.
        public void RestoreEnergy(float amount)
        {
            if (amount <= 0f) return;
            float effective = IsHungry ? amount * config.hungerPenaltyMultiplier : amount;
            SetEnergy(CurrentEnergy + effective);
        }

        // Mechanical energy restore that bypasses the hunger penalty (e.g. job abandon refund).
        public void RestoreEnergyDirect(float amount)
        {
            if (amount <= 0f) return;
            SetEnergy(CurrentEnergy + amount);
        }

        // Called when player consumes a food item. Resets hunger clock and registers the speed buff.
        public void EatFood(EnergyBuff buff)
        {
            _hoursSinceLastFood = 0f;
            if (buff.speedMultiplier > 0f && buff.durationHours > 0f)
                _activeBuffs.Add(new ActiveBuff(buff));
        }

        // Combined job-speed multiplier from all active food buffs. Returns 1 when no buffs are active.
        public float GetCurrentSpeedMultiplier()
        {
            if (_activeBuffs.Count == 0) return 1f;
            float result = 1f;
            foreach (ActiveBuff b in _activeBuffs)
                result *= b.speedMultiplier;
            return result;
        }

        private void SetEnergy(float value)
        {
            float previous = CurrentEnergy;
            CurrentEnergy = Mathf.Clamp(value, 0f, config.maxEnergy);

            if (Mathf.Approximately(previous, CurrentEnergy)) return;

            OnEnergyChanged?.Invoke(CurrentEnergy, config.maxEnergy);

            // Reset one-shot flags when energy recovers past their thresholds.
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

        private void HandleNewDay(int day)
        {
            // Energy fully restores on sleep. Hunger persists — skipping dinner means waking up hungry.
            CurrentEnergy  = config.maxEnergy;
            _lowEnergyFired = false;
            _depletedFired  = false;
            _activeBuffs.Clear();
            OnEnergyChanged?.Invoke(CurrentEnergy, config.maxEnergy);
        }

        private void HandleTimeAdvanced(float previousHour, float newHour)
        {
            float delta = newHour - previousHour;
            _hoursSinceLastFood += delta;

            // Expire active buffs, iterating backwards to allow safe removal.
            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                _activeBuffs[i].remainingHours -= delta;
                if (_activeBuffs[i].remainingHours <= 0f)
                    _activeBuffs.RemoveAt(i);
            }
        }

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
