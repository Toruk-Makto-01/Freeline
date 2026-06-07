using System;
using System.Collections.Generic;
using UnityEngine;

namespace Freeline
{
    /// <summary>
    /// İş sisteminin durum makinesini tanımlar.
    /// Akış: Idle → BoardShowing → JobSelected → JobActive → (tamamlama/terk) → BoardShowing.
    /// </summary>
    public enum JobState
    {
        Idle,
        BoardShowing,
        JobSelected,
        JobActive
    }

    /// <summary>
    /// Komisyon iş panosunu ve iş yaşam döngüsünü yönetir.
    /// İş seçimi, başlatma, tamamlama ve terk etme işlemlerini koordine eder;
    /// enerji yeterliliğini denetler ve kazancı SaveData'ya yazar.
    /// </summary>
    public class JobManager : MonoBehaviour
    {
        [SerializeField] private JobConfig     config;
        [SerializeField] private List<JobData> allJobs;

        /// <summary>Şu anda aktif olan iş; iş yokken <c>null</c>.</summary>
        public JobData              ActiveJob        { get; private set; }

        /// <summary>İş sisteminin anlık durumu.</summary>
        public JobState             CurrentJobState  { get; private set; }

        /// <summary>Panoda görünen mevcut iş listesine salt-okunur erişim.</summary>
        public IReadOnlyList<JobData> CurrentBoardJobs => _currentBoardJobs;

        /// <summary>Bugün kalan yenileme hakkı sayısı.</summary>
        public int                  RefreshesRemaining => config.maxDailyRefreshes - _refreshesUsedToday;

        /// <summary>
        /// Erken bitirme bonusu aktifse <c>true</c> yapılır.
        /// Beceri sistemi tarafından dışarıdan set edilir; <see cref="CalculatePayout"/> bunu okur.
        /// </summary>
        public bool earlyFinishBonusActive;

        /// <summary>
        /// Pano oluşturulduğunda veya yenilendiğinde tetiklenir; güncel iş listesini taşır.
        /// </summary>
        public event Action<IReadOnlyList<JobData>> OnJobBoardRefreshed;

        /// <summary>Panodaki bir iş seçildiğinde tetiklenir (henüz başlamamış).</summary>
        public event Action<JobData>                OnJobSelected;

        /// <summary>
        /// İş aktif hale geçtiğinde tetiklenir.
        /// <see cref="DrawingMinigame"/> bu event'i dinleyerek kendini gösterir.
        /// </summary>
        public event Action<JobData>                OnJobStarted;

        /// <summary>
        /// İş başarıyla tamamlandığında tetiklenir.
        /// Parametreler: (tamamlanan iş, nihai ödeme miktarı).
        /// </summary>
        public event Action<JobData, float>         OnJobCompleted;

        /// <summary>Oyuncu iş ortasında vazgeçtiğinde tetiklenir; ödeme yapılmaz.</summary>
        public event Action<JobData>                OnJobAbandoned;

        /// <summary>
        /// <see cref="SelectJob"/> veya <see cref="StartJob"/> enerji yetersizliği nedeniyle
        /// reddedildiğinde tetiklenir. UI'ın uyarı göstermesi için kullanılabilir.
        /// </summary>
        public event Action                         OnJobBlockedByEnergy;

        private readonly List<JobData> _currentBoardJobs = new();
        private int  _refreshesUsedToday;

        // EnergyManager'dan gelen OnEnergyDepleted / OnEnergyChanged event'leriyle güncellenir.
        // Anlık CurrentEnergy sorgusu yerine bayrak tutmak, her karede erişimi önler.
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

        /// <summary>
        /// Oyuncunun mevcut seviyesine uygun işlerden rastgele bir pano oluşturur.
        /// Aktif iş yokken her zaman çağrılabilir.
        /// </summary>
        public void GenerateJobBoard()
        {
            // SaveData henüz yoksa (ilk yeni oyun) seviyeyi 1 kabul et.
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

        /// <summary>
        /// Panodaki bir işi seçer; durum makinesini <see cref="JobState.JobSelected"/>'a taşır.
        /// Geçersiz indeks, aktif iş veya sıfır enerji durumunda <c>false</c> döner.
        /// </summary>
        /// <param name="boardIndex">Panodaki işin sıfır tabanlı indeksi.</param>
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

        /// <summary>
        /// Seçili işi başlatır; enerji maliyetini doğrular ve durumu <see cref="JobState.JobActive"/>'e taşır.
        /// <see cref="OnJobStarted"/> tetiklendikten sonra mini-oyun kendini göstermelidir.
        /// </summary>
        /// <returns>İş başarıyla başlatıldıysa <c>true</c>, enerji yetersizse <c>false</c>.</returns>
        public bool StartJob()
        {
            if (ActiveJob == null || CurrentJobState != JobState.JobSelected) return false;

            if (_energyDepleted || GameManager.Instance.EnergyManager.CurrentEnergy < ActiveJob.energyCost)
            {
                // JobSelected 'ta takılı kalmaktan kaçın; panoyu yeniden gösterilebilir hale getir.
                ActiveJob       = null;
                CurrentJobState = JobState.BoardShowing;
                OnJobBlockedByEnergy?.Invoke();
                OnJobBoardRefreshed?.Invoke(_currentBoardJobs);
                return false;
            }

            CurrentJobState = JobState.JobActive;
            OnJobStarted?.Invoke(ActiveJob);
            return true;
        }

        /// <summary>
        /// Mini-oyun sistemi tarafından oyuncu başarıyla tamamladığında çağrılır.
        /// Zaman ilerletme, enerji tüketimi, kazanç hesaplama ve pano yenileme işlemlerini yürütür.
        /// </summary>
        public void CompleteJob()
        {
            if (ActiveJob == null || CurrentJobState != JobState.JobActive) return;

            JobData completedJob = ActiveJob;

            // AdvanceTime çağrısından ÖNCE aktif durumu temizle.
            // AdvanceTime gün sonunu tetiklerse HandleNewDay senkron çalışır;
            // o noktada JobActive durumunda kalmak panoyu yenilememesine neden olur.
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

            // AdvanceTime yeni bir gün tetiklediyse HandleNewDay zaten GenerateJobBoard'u çağırdı.
            // Hâlâ Idle'daysa (yeni gün açılmadıysa) panoyu burada yenile.
            if (CurrentJobState == JobState.Idle)
                GenerateJobBoard();
        }

        // Mini-oyun tarafından zaten hesaplanmış finalPayout ile işi tamamlar.
        // CompleteJob'dan tek farkı CalculatePayout'u atlamasıdır.
        public void CompleteJobWithPayout(float finalPayout)
        {
            if (ActiveJob == null || CurrentJobState != JobState.JobActive) return;

            JobData completedJob = ActiveJob;
            ActiveJob       = null;
            CurrentJobState = JobState.Idle;

            var gm = GameManager.Instance;
            gm.TimeManager.AdvanceTime(completedJob.durationHours);
            gm.EnergyManager.ConsumeEnergy(completedJob.energyCost);

            SaveData save = gm.SaveManager.CurrentData;
            save.currentCoins       += finalPayout;
            save.totalJobsCompleted += 1;

            OnJobCompleted?.Invoke(completedJob, finalPayout);

            if (CurrentJobState == JobState.Idle)
                GenerateJobBoard();
        }

        /// <summary>
        /// Oyuncu iş ortasında vazgeçtiğinde çağrılır.
        /// Ödeme yapılmaz, zaman ilerlemez; enerji maliyetinin %50'si iade edilir.
        /// İade açlık cezasını atlatmak için <see cref="EnergyManager.RestoreEnergyDirect"/> kullanır.
        /// </summary>
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

        /// <summary>
        /// Panoyu yeniden rastgele oluşturur.
        /// Günlük yenileme limiti dolmuşsa veya aktif iş varsa <c>false</c> döner.
        /// </summary>
        public bool RefreshJobBoard()
        {
            if (CurrentJobState == JobState.JobActive) return false;
            if (_refreshesUsedToday >= config.maxDailyRefreshes) return false;

            _refreshesUsedToday++;
            GenerateJobBoard();
            return true;
        }

        /// <summary>
        /// İşin nihai ödemesini hesaplar.
        /// Erken bitirme bonusu aktifse bahşiş çarpanı eklenir.
        /// </summary>
        private float CalculatePayout(JobData job)
        {
            if (earlyFinishBonusActive)
                return job.basePayout + job.basePayout * config.tipBonusMultiplier;
            return job.basePayout;
        }

        /// <summary>
        /// EnergyManager'dan enerji tükendiği bildirimi geldiğinde bayrağı set eder
        /// ve UI'ı bilgilendirmek için <see cref="OnJobBlockedByEnergy"/>'yi tetikler.
        /// </summary>
        private void HandleEnergyDepleted()
        {
            _energyDepleted = true;
            OnJobBlockedByEnergy?.Invoke();
        }

        /// <summary>
        /// Enerji yeniden pozitif bir değere çıktığında enerji-tükenme bayrağını sıfırlar.
        /// </summary>
        private void HandleEnergyChanged(float current, float max)
        {
            if (current > 0f) _energyDepleted = false;
        }

        /// <summary>
        /// Yeni gün başladığında günlük yenileme sayacını ve enerji bayrağını sıfırlar,
        /// ardından yeni bir pano oluşturur.
        /// </summary>
        private void HandleNewDay(int day)
        {
            _refreshesUsedToday = 0;
            _energyDepleted     = false;
            GenerateJobBoard();
        }

        /// <summary>
        /// Listeyi yerinde Fisher-Yates algoritmasıyla karıştırır.
        /// Her çağrıda farklı bir pano sıralaması garantiler.
        /// </summary>
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
