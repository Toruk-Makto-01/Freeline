#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Freeline
{
    // Keyboard shortcuts for runtime integration testing — Editor only.
    //
    //   J  →  Complete the first board job
    //   W  →  Produce a webtoon chapter
    //   F  →  Eat food (reset hunger + restore 30 energy)
    //   N  →  Force new day (advances time past midnight)
    //   R  →  Delete save and reload scene
    //
    // Attach to the "DebugTools" GameObject in the Bootstrap scene.
    public class DebugTestRunner : MonoBehaviour
    {
        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.jKey.wasPressedThisFrame) SimulateCompleteJob();
            if (kb.wKey.wasPressedThisFrame) SimulateProduceChapter();
            if (kb.fKey.wasPressedThisFrame) SimulateFeed();
            if (kb.nKey.wasPressedThisFrame) SimulateNewDay();
            if (kb.rKey.wasPressedThisFrame) SimulateReset();
        }

        private void SimulateCompleteJob()
        {
            var gm  = GameManager.Instance;
            var jm  = gm.JobManager;
            var save = gm.SaveManager.CurrentData;

            if (jm.CurrentBoardJobs.Count == 0)
            {
                Debug.LogWarning("[DebugTest] J pressed — no jobs on board. Add JobData assets to JobManager.");
                return;
            }

            JobData job = jm.CurrentBoardJobs[0];
            float coinsBefore = save.currentCoins;

            Debug.Log(
                $"[DebugTest] J → Starting job: '{job.jobTitle}' | " +
                $"Cost: {job.energyCost} energy, {job.durationHours}h | " +
                $"Payout: {job.basePayout} coins"
            );

            if (!jm.SelectJob(0))
            {
                Debug.LogWarning("[DebugTest] SelectJob(0) failed — energy depleted or job active.");
                return;
            }

            if (!jm.StartJob())
            {
                Debug.LogWarning(
                    $"[DebugTest] StartJob() failed — energy ({gm.EnergyManager.CurrentEnergy:F0}) " +
                    $"< job cost ({job.energyCost:F0})."
                );
                return;
            }

            jm.CompleteJob();

            float payout = save.currentCoins - coinsBefore;
            Debug.Log(
                $"[DebugTest] J → Job complete | " +
                $"Payout: +{payout:F0} coins | " +
                $"Time: {gm.TimeManager.GetFormattedTime()} | " +
                $"Energy: {gm.EnergyManager.CurrentEnergy:F0}/{gm.EnergyManager.MaxEnergy:F0} | " +
                $"Total coins: {save.currentCoins:F0}"
            );
        }

        private void SimulateProduceChapter()
        {
            var gm = GameManager.Instance;
            var wm = gm.WebtoonManager;
            var wt = gm.SaveManager.CurrentData.webtoonData;

            float followersBefore = wt.followers;
            int   chaptersBefore  = wt.totalChaptersPublished;

            Debug.Log(
                $"[DebugTest] W → Producing chapter | " +
                $"Energy before: {gm.EnergyManager.CurrentEnergy:F0} | " +
                $"Followers before: {followersBefore:F0}"
            );

            bool success = wm.ProduceChapter();

            if (!success)
            {
                Debug.LogWarning(
                    $"[DebugTest] ProduceChapter() failed — not enough energy " +
                    $"({gm.EnergyManager.CurrentEnergy:F0} available)."
                );
                return;
            }

            float followerDelta = wt.followers - followersBefore;
            Debug.Log(
                $"[DebugTest] W → Chapter {wt.totalChaptersPublished} published | " +
                $"Follower change: +{followerDelta:F0} | " +
                $"Total followers: {wt.followers:F0} | " +
                $"Time: {gm.TimeManager.GetFormattedTime()}"
            );
        }

        private void SimulateFeed()
        {
            var gm     = GameManager.Instance;
            var energy = gm.EnergyManager;
            float before = energy.CurrentEnergy;

            Debug.Log(
                $"[DebugTest] F → Eating food | " +
                $"Hungry: {energy.IsHungry} | " +
                $"Energy before: {before:F0}"
            );

            // Reset hunger clock (no buff), then restore energy at full rate.
            energy.EatFood(new EnergyBuff { speedMultiplier = 0f, durationHours = 0f });
            energy.RestoreEnergy(30f);

            Debug.Log(
                $"[DebugTest] F → Fed | " +
                $"Energy after: {energy.CurrentEnergy:F0} " +
                $"(+{energy.CurrentEnergy - before:F0}) | " +
                $"Hungry: {energy.IsHungry}"
            );
        }

        private void SimulateNewDay()
        {
            var gm   = GameManager.Instance;
            var save = gm.SaveManager.CurrentData;

            int   dayBefore   = gm.TimeManager.CurrentDay;
            float coinsBefore = save.currentCoins;

            Debug.Log(
                $"[DebugTest] N → Forcing new day from Day {dayBefore} | " +
                $"Current hour: {gm.TimeManager.GetFormattedTime()}"
            );

            // Advance past midnight regardless of current hour.
            gm.TimeManager.AdvanceTime(24f);

            float incomeEarned = save.currentCoins - coinsBefore;
            Debug.Log(
                $"[DebugTest] N → Now Day {gm.TimeManager.CurrentDay} | " +
                $"Passive income earned: +{incomeEarned:F2} coins | " +
                $"Followers: {save.webtoonData.followers:F0} | " +
                $"Energy: {gm.EnergyManager.CurrentEnergy:F0}/{gm.EnergyManager.MaxEnergy:F0}"
            );
        }

        private void SimulateReset()
        {
            Debug.Log("[DebugTest] R → Deleting save and reloading scene...");
            GameManager.Instance.SaveManager.DeleteSave();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}

#endif
