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
        [SerializeField] private JobConfig config;
        [SerializeField] private List<JobData> allJobs;

        public JobData ActiveJob { get; private set; }
        public JobState CurrentJobState { get; private set; }
        public IReadOnlyList<JobData> CurrentBoardJobs => _currentBoardJobs;

        public List<JobData> GetBoardJobs() => new List<JobData>(_currentBoardJobs);
        public int RefreshesRemaining => config.maxDailyRefreshes - _refreshesUsedToday;
        public bool earlyFinishBonusActive;

        public event Action<IReadOnlyList<JobData>> OnJobBoardRefreshed;
        public event Action<JobData> OnJobSelected;
        public event Action<JobData> OnJobStarted;
        public event Action<JobData, float> OnJobCompleted;
        public event Action<JobData> OnJobAbandoned;
        public event Action OnJobBlockedByEnergy;

        private readonly List<JobData> _currentBoardJobs = new();
        private int _refreshesUsedToday;
        private bool _energyDepleted;

        void Start()
        {
            var em = GameManager.Instance.EnergyManager;
            em.OnEnergyDepleted += HandleEnergyDepleted;
            em.OnEnergyChanged += HandleEnergyChanged;
            GameManager.Instance.TimeManager.OnNewDayStarted += HandleNewDay;

            GenerateJobBoard();
        }

        void OnDestroy()
        {
            if (GameManager.Instance == null) return;
            var em = GameManager.Instance.EnergyManager;
            em.OnEnergyDepleted -= HandleEnergyDepleted;
            em.OnEnergyChanged -= HandleEnergyChanged;
            GameManager.Instance.TimeManager.OnNewDayStarted -= HandleNewDay;
        }

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

        public bool StartJob()
        {
            if (ActiveJob == null || CurrentJobState != JobState.JobSelected) return false;

            if (_energyDepleted || GameManager.Instance.EnergyManager.CurrentEnergy < ActiveJob.energyCost)
            {
                ActiveJob = null;
                CurrentJobState = JobState.BoardShowing;
                OnJobBlockedByEnergy?.Invoke();
                OnJobBoardRefreshed?.Invoke(_currentBoardJobs);
                return false;
            }

            CurrentJobState = JobState.JobActive;
            OnJobStarted?.Invoke(ActiveJob);
            return true;
        }

        // =========================================================================
        // --- BİRLEŞTİRİLMİŞ İŞ TAMAMLAMA SİSTEMİ (ÖNCEKİ TEKRARLAR TEMİZLENDİ) ---
        // =========================================================================
        
        public void CompleteJob() => FinishJobProcess(CalculatePayout(ActiveJob));
        
        public void CompleteJobWithPayout(float finalPayout) => FinishJobProcess(finalPayout);
        
        public void CompleteActiveJob() => FinishJobProcess(CalculatePayout(ActiveJob));

        /// <summary>
        /// Tüm iş bitirme komutlarının toplandığı, enerji tüketen ve parayı veren ana merkez.
        /// </summary>
        private void FinishJobProcess(float finalPayout)
        {
            if (ActiveJob == null) return;
            if (CurrentJobState != JobState.JobActive && CurrentJobState != JobState.JobSelected) return;

            JobData completedJob = ActiveJob;
            ActiveJob = null;
            CurrentJobState = JobState.Idle;

            var gm = GameManager.Instance;
            
            // 1. Zamanı ve Enerjiyi Harca
            gm.TimeManager.AdvanceTime(completedJob.durationHours);
            gm.EnergyManager.ConsumeEnergy(completedJob.energyCost);

            // 2. Parayı Ver ve İstatistiği Artır
            gm.SaveManager.AddCoins(finalPayout);
            gm.SaveManager.CurrentData.totalJobsCompleted += 1;

            // --- YENİ EKLENEN KOD BURADA (Günlük Faturaya Gelir Olarak Yaz) ---
            gm.SaveManager.LogDailyTransaction(completedJob.jobTitle, finalPayout, true);
            // ------------------------------------------------------------------

            OnJobCompleted?.Invoke(completedJob, finalPayout);

            // 3. Panoyu Yenile
            if (CurrentJobState == JobState.Idle)
                GenerateJobBoard();
        }

        // =========================================================================

        public void AbandonJob()
        {
            if (ActiveJob == null || CurrentJobState != JobState.JobActive) return;

            float refund = ActiveJob.energyCost * 0.5f;
            GameManager.Instance.EnergyManager.RestoreEnergyDirect(refund);

            JobData abandonedJob = ActiveJob;
            ActiveJob = null;
            CurrentJobState = JobState.BoardShowing;

            OnJobAbandoned?.Invoke(abandonedJob);
        }

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
            _energyDepleted = false;
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