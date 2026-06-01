using System;
using UnityEngine;

namespace Freeline
{
    public class TimeManager : MonoBehaviour
    {
        [SerializeField] private TimeConfig config;

        public float CurrentHour { get; private set; }
        public int   CurrentDay  { get; private set; }

        public bool IsInSleepWindow => CurrentHour >= config.sleepWindowStart;

        // (previousHour, newHour) — fired on every clock advance.
        public event Action<float, float> OnTimeAdvanced;

        // Fired the first time the clock crosses sleepWindowStart each day.
        public event Action OnSleepWindowOpened;

        // Fired when a day ends (before clock resets). Passes the day number that ended.
        public event Action<int> OnDayEnded;

        // Fired after the clock resets to startHour. Passes the new day number.
        public event Action<int> OnNewDayStarted;

        private bool _sleepWindowNotified;

        void Awake()
        {
            CurrentDay  = 1;
            CurrentHour = config.startHour;
            _sleepWindowNotified = false;
        }

        // Called by SaveManager after loading a save file.
        public void LoadState(int day, float hour)
        {
            CurrentDay  = day;
            CurrentHour = Mathf.Clamp(hour, 0f, config.dayEndHour);
            // Don't re-fire OnSleepWindowOpened if we loaded into an already-late hour.
            _sleepWindowNotified = CurrentHour >= config.sleepWindowStart;
        }

        // Called by JobManager when a job or task completes.
        public void AdvanceTime(float hours)
        {
            if (hours <= 0f) return;

            float previous = CurrentHour;
            float next     = CurrentHour + hours;

            if (next >= config.dayEndHour)
            {
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

        // Returns true if sleep was accepted, false if outside the sleep window.
        public bool TrySleep()
        {
            if (!IsInSleepWindow) return false;
            SleepNow();
            return true;
        }

        // "HH:MM" — ready for UI or debug use.
        public string GetFormattedTime()
        {
            int h = Mathf.FloorToInt(CurrentHour);
            int m = Mathf.RoundToInt((CurrentHour - h) * 60f);
            return $"{h:D2}:{m:D2}";
        }

        private void NotifySleepWindowIfCrossed(float previousHour)
        {
            if (_sleepWindowNotified) return;
            if (CurrentHour >= config.sleepWindowStart && previousHour < config.sleepWindowStart)
            {
                _sleepWindowNotified = true;
                OnSleepWindowOpened?.Invoke();
            }
        }

        private void SleepNow()
        {
            OnDayEnded?.Invoke(CurrentDay);

            CurrentDay++;
            CurrentHour = config.startHour;
            _sleepWindowNotified = false;

            OnNewDayStarted?.Invoke(CurrentDay);
        }
    }
}
