using System;
using System.Collections.Generic;
using UnityEngine;

namespace Freeline
{
    public enum JobState
    {
        Idle,
        BoardShowing,
        JobSelected,
        JobActive
    }

    public class JobManager : MonoBehaviour
    {
        [SerializeField] private JobConfig     config;
        [SerializeField] private List<JobData> allJobs;

        public JobData              ActiveJob        { get; private set; }
        public JobState             CurrentJobState  { get; private set; }
        public IReadOnlyList<JobData> CurrentBoardJobs => _currentBoardJobs;
        public int                  RefreshesRemaining => config.maxDailyRefreshes - _refreshesUsedToday;

        // Set by the skill system when the early-finish bonus is unlocked.
        public bool earlyFinishBonusActive;

        // (board snapshot) — fired whenever the board is generated or refreshed.
        public event Action<IReadOnlyList<JobData>> OnJobBoardRefreshed;
        public event Action<JobData>                OnJobSelected;
        public event Action<JobData>                OnJobStarted;
        public event Action<JobData, float>         OnJobCompleted;    // (job, finalPayout)
        public event Action<JobData>                OnJobAbandoned;
        // Fired when SelectJob or StartJob is blocked because energy is 0.
        public event Action                         OnJobBlockedByEnergy;

        private readonly List<JobData> _currentBoardJobs = new();
        private int  _refreshesUsedToday;
        private bool _energyDepleted;

        void Start()
        {
            var em = GameManager.Instance.EnergyManager;
            em.OnEnergyDepleted += HandleEnergyDepleted;
            em.OnEnergyChanged  += HandleEnergyChanged;
            GameManager.Instance.TimeManager.OnNewDayStarted += HandleNewDay;

            GenerateJobBoard();
        }

        void OnDestroy()
        {
            if (GameManager.Instance == null) return;
            var em = GameManager.Instance.EnergyManager;
            em.OnEnergyDepleted -= HandleEnergyDepleted;
            em.OnEnergyChanged  -= HandleEnergyChanged;
            GameManager.Instance.TimeManager.OnNewDayStarted -= HandleNewDay;
        }

        // Picks up to config.boardSize jobs the player is currently eligible for.
        // Safe to call at any time outside of an active job.
        public void GenerateJobBoard()
        {
            int playerLevel = GameManager.Instance.SaveManager.CurrentData?.playerLevel ?? 1;

            var eligible = new List<JobData>();
            foreach (JobData job in allJobs)
            {
                if (job != null && job.requiredLevel <= playerLevel)
                    eligible.Add(job);
            }

            Shuffle(eligible);

            _currentBoardJobs.Clear();
            int count = Mathf.Min(config.boardSize, eligible.Count);
            for (int i = 0; i < count; i++)
                _currentBoardJobs.Add(eligible[i]);

            CurrentJobState = JobState.BoardShowing;
            OnJobBoardRefreshed?.Invoke(_currentBoardJobs);
        }

        // Pick a job from the current board. Returns false if the index is invalid,
        // the player has no energy, or a job is already active.
        public bool SelectJob(int boardIndex)
        {
            if (CurrentJobState == JobState.JobActive) return false;
            if (boardIndex < 0 || boardIndex >= _currentBoardJobs.Count) return false;

            if (_energyDepleted)
            {
                OnJobBlockedByEnergy?.Invoke();
                return false;
            }

            ActiveJob = _currentBoardJobs[boardIndex];
            CurrentJobState = JobState.JobSelected;
            OnJobSelected?.Invoke(ActiveJob);
            return true;
        }

        // Validates that energy covers the cost and transitions into the active state.
        // The mini-game scene should start after receiving OnJobStarted.
        public bool StartJob()
        {
            if (ActiveJob == null || CurrentJobState != JobState.JobSelected) return false;

            if (_energyDepleted || GameManager.Instance.EnergyManager.CurrentEnergy < ActiveJob.energyCost)
            {
                OnJobBlockedByEnergy?.Invoke();
                return false;
            }

            CurrentJobState = JobState.JobActive;
            OnJobStarted?.Invoke(ActiveJob);
            return true;
        }

        // Called by the mini-game system when the player succeeds.
        // Handles all side-effects: time, energy, payout, save data, board refresh.
        public void CompleteJob()
        {
            if (ActiveJob == null || CurrentJobState != JobState.JobActive) return;

            JobData completedJob = ActiveJob;

            // Clear active state BEFORE AdvanceTime so that if a new day is triggered
            // synchronously inside AdvanceTime, HandleNewDay won't encounter a stale JobActive state.
            ActiveJob       = null;
            CurrentJobState = JobState.Idle;

            var gm = GameManager.Instance;
            gm.TimeManager.AdvanceTime(completedJob.durationHours);
            gm.EnergyManager.ConsumeEnergy(completedJob.energyCost);

            float payout = CalculatePayout(completedJob);
            SaveData save = gm.SaveManager.CurrentData;
            save.currentCoins       += payout;
            save.totalJobsCompleted += 1;

            OnJobCompleted?.Invoke(completedJob, payout);

            // If AdvanceTime triggered a new day, HandleNewDay already called GenerateJobBoard.
            // Only generate here if we're still in Idle (no new-day board was generated).
            if (CurrentJobState == JobState.Idle)
                GenerateJobBoard();
        }

        // Called when the player quits mid-job. No payout, no time advance.
        // Returns 50% of the energy cost as a mechanical refund (bypasses hunger penalty).
        public void AbandonJob()
        {
            if (ActiveJob == null || CurrentJobState != JobState.JobActive) return;

            float refund = ActiveJob.energyCost * 0.5f;
            GameManager.Instance.EnergyManager.RestoreEnergyDirect(refund);

            JobData abandonedJob = ActiveJob;
            ActiveJob       = null;
            CurrentJobState = JobState.BoardShowing;

            OnJobAbandoned?.Invoke(abandonedJob);
        }

        // Re-rolls the board. Returns false if the daily refresh limit is reached
        // or a job is currently active.
        public bool RefreshJobBoard()
        {
            if (CurrentJobState == JobState.JobActive) return false;
            if (_refreshesUsedToday >= config.maxDailyRefreshes) return false;

            _refreshesUsedToday++;
            GenerateJobBoard();
            return true;
        }

        private float CalculatePayout(JobData job)
        {
            if (earlyFinishBonusActive)
                return job.basePayout + job.basePayout * config.tipBonusMultiplier;
            return job.basePayout;
        }

        private void HandleEnergyDepleted()
        {
            _energyDepleted = true;
            OnJobBlockedByEnergy?.Invoke();
        }

        private void HandleEnergyChanged(float current, float max)
        {
            if (current > 0f) _energyDepleted = false;
        }

        private void HandleNewDay(int day)
        {
            _refreshesUsedToday = 0;
            _energyDepleted     = false;
            GenerateJobBoard();
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
