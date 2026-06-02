// =============================================================================
// BOOTSTRAP SCENE SETUP INSTRUCTIONS
// =============================================================================
//
// Create the following GameObjects in the Bootstrap scene:
//
// [1] "Managers" GameObject
//     Components (all on the same object):
//       - GameManager      → assign all five manager slots in the Inspector
//       - TimeManager      → assign TimeConfig SO
//       - EnergyManager    → assign EnergyConfig SO
//       - SaveManager      → no SO needed
//       - JobManager       → assign JobConfig SO, populate All Jobs list with JobData assets
//       - WebtoonManager   → assign WebtoonConfig SO
//
// [2] "Bootstrap" GameObject
//     Components:
//       - BootstrapManager → no references needed (uses GameManager.Instance)
//
// [3] "DebugTools" GameObject  (Editor testing only)
//     Components:
//       - DebugTestRunner  → no references needed
//
// ScriptableObjects to create (right-click in Project window):
//   Freeline/Config/Time Config      → Assets/ScriptableObjects/Config/TimeConfig
//   Freeline/Config/Energy Config    → Assets/ScriptableObjects/Config/EnergyConfig
//   Freeline/Config/Job Config       → Assets/ScriptableObjects/Config/JobConfig
//   Freeline/Config/Webtoon Config   → Assets/ScriptableObjects/Config/WebtoonConfig
//
// JobData assets:
//   Freeline/Jobs/Job Data           → Assets/ScriptableObjects/Jobs/
//   Create at least 3 to populate the board. Set requiredLevel = 1 so they appear on Day 1.
//
// =============================================================================

using UnityEngine;

namespace Freeline
{
    /// <summary>
    /// Oyun başlangıç sırasını yönetir: kayıt yükleme, yönetici başlatma ve ilk sahne geçişi.
    /// Tüm yöneticilerin <c>Start()</c> metotlarından önce çalışması zorunludur;
    /// aksi takdirde JobManager, playerLevel henüz yüklenmeden panoyu oluşturur.
    /// </summary>
    /// <remarks>
    /// DefaultExecutionOrder(-100): Diğer tüm yöneticilerden önce çalışmasını garantiler.
    /// Akış: LoadGame → ApplyToManagers → SetState(Apartment) → GenerateJobBoard → LogStatus.
    /// </remarks>
    [DefaultExecutionOrder(-100)]
    public class BootstrapManager : MonoBehaviour
    {
        void Start()
        {
            var gm   = GameManager.Instance;
            var save = gm.SaveManager;

            save.LoadGame();
            save.ApplyToManagers();

            gm.SetState(GameState.Apartment);

            // playerLevel artık SaveData'dan yüklendiği için pano doğru filtreyle oluşturulur.
            // JobManager.Start() da GenerateJobBoard çağırır; her ikisi aynı filtrelenmiş seti üretir,
            // dolayısıyla çift çağrı işlevsel bir sorun yaratmaz.
            gm.JobManager.GenerateJobBoard();

            LogStatus();
        }

        /// <summary>
        /// Bootstrap tamamlandıktan sonra konsola özet bir durum raporu yazar.
        /// Yalnızca başlangıç doğrulaması içindir; oyun akışını etkilemez.
        /// </summary>
        private void LogStatus()
        {
            var gm     = GameManager.Instance;
            var time   = gm.TimeManager;
            var energy = gm.EnergyManager;
            var save   = gm.SaveManager.CurrentData;
            var wt     = save.webtoonData;
            var jobs   = gm.JobManager.CurrentBoardJobs;

            Debug.Log(
                $"[Freeline] Day: {time.CurrentDay} | " +
                $"Hour: {time.GetFormattedTime()} | " +
                $"Energy: {energy.CurrentEnergy:F0}/{energy.MaxEnergy:F0} | " +
                $"Coins: {save.currentCoins:F0} | " +
                $"Followers: {wt.followers:F0}"
            );

            Debug.Log($"[Freeline] Job board: {jobs.Count} jobs loaded");

            Debug.Log(
                $"[Freeline] Webtoon: {wt.totalChaptersPublished} chapters, " +
                $"{wt.followers:F0} followers, " +
                $"daily income: {gm.WebtoonManager.GetDailyPassiveIncome():F2} coins"
            );
        }
    }
}
