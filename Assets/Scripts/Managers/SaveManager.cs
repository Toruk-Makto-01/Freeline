using System;
using System.IO;
using UnityEngine;

namespace Freeline
{
    public class SaveManager : MonoBehaviour
    {
        private const string SaveFileName = "freeline_save.json";

        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        // The live save data object. Populated by LoadGame() or NewGame() before play begins.
        public SaveData CurrentData { get; private set; }

        void Start()
        {
            // OnNewDayStarted fires after TimeManager has incremented day+hour and after
            // EnergyManager has reset energy (guaranteed by EnergyManager's DefaultExecutionOrder(-10)).
            // This gives us a clean new-morning snapshot to persist.
            GameManager.Instance.TimeManager.OnNewDayStarted += HandleNewDayStarted;
        }

        void OnDestroy()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.TimeManager.OnNewDayStarted -= HandleNewDayStarted;
        }

        // Reads the save file from disk. Returns a fresh SaveData if no file exists.
        // Call this at game startup, then call ApplyToManagers() to restore state.
        public SaveData LoadGame()
        {
            if (!File.Exists(SavePath))
            {
                CurrentData = NewGame();
                return CurrentData;
            }

            try
            {
                string json = File.ReadAllText(SavePath);
                CurrentData = JsonUtility.FromJson<SaveData>(json);
                return CurrentData;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Load failed: {e.Message}. Starting fresh.");
                CurrentData = NewGame();
                return CurrentData;
            }
        }

        // Captures current manager state then writes to disk.
        public void SaveGame()
        {
            CaptureFromManagers();
            WriteToDisk();
        }

        // Deletes the save file and resets CurrentData to defaults. Debug / reset use only.
        public void DeleteSave()
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);

            CurrentData = NewGame();
            Debug.Log("[SaveManager] Save deleted.");
        }

        // Pushes CurrentData values back into all managers. Call after LoadGame() at startup.
        public void ApplyToManagers()
        {
            if (CurrentData == null) return;

            GameManager.Instance.TimeManager.LoadState(
                CurrentData.currentDay,
                CurrentData.currentHour
            );

            GameManager.Instance.EnergyManager.LoadState(
                CurrentData.currentEnergy,
                CurrentData.hoursSinceLastFood
            );

            // JobManager and WebtoonManager read/write SaveData directly; no LoadState calls needed.
            // Add LevelManager etc. here as they are built.
        }

        // Pulls current values from all managers into CurrentData. Call before SaveGame().
        public void CaptureFromManagers()
        {
            if (CurrentData == null) CurrentData = new SaveData();

            TimeManager time = GameManager.Instance.TimeManager;
            CurrentData.currentDay  = time.CurrentDay;
            CurrentData.currentHour = time.CurrentHour;

            EnergyManager energy = GameManager.Instance.EnergyManager;
            CurrentData.currentEnergy      = energy.CurrentEnergy;
            CurrentData.hoursSinceLastFood = energy.HoursSinceLastFood;

            // currentCoins, totalJobsCompleted, playerLevel, webtoonData — written directly to
            // SaveData by JobManager and WebtoonManager; no capture step needed here.
            // currentGems — same pattern when that system is built.
        }

        private SaveData NewGame() => new SaveData();

        private void WriteToDisk()
        {
            try
            {
                string json = JsonUtility.ToJson(CurrentData, prettyPrint: true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Write failed: {e.Message}");
            }
        }

        // Auto-save on new day. EnergyManager has already reset before this fires.
        private void HandleNewDayStarted(int newDay)
        {
            SaveGame();
        }
    }
}
