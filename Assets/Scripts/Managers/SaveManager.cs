using System;
using System.IO;
using UnityEngine;

namespace Freeline
{
    public class SaveManager : MonoBehaviour
    {
        private const string SaveFileName = "freeline_save.json";
        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public SaveData CurrentData { get; private set; }

        public event Action<int> OnCoinsChanged;
        public event Action<int> OnGemsChanged;

        void Start()
        {
            GameManager.Instance.TimeManager.OnNewDayStarted += HandleNewDayStarted;
        }

        void OnDestroy()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.TimeManager.OnNewDayStarted -= HandleNewDayStarted;
        }

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

        public void SaveGame()
        {
            CaptureFromManagers();
            WriteToDisk();
        }

        public void AddCoins(float amount)
        {
            if (CurrentData == null) return;
            CurrentData.currentCoins += amount;
            OnCoinsChanged?.Invoke(Mathf.FloorToInt(CurrentData.currentCoins));
        }

        public void SetCoins(float value)
        {
            if (CurrentData == null) return;
            CurrentData.currentCoins = value;
            OnCoinsChanged?.Invoke(Mathf.FloorToInt(CurrentData.currentCoins));
        }

        public void AddGems(int amount)
        {
            if (CurrentData == null) return;
            CurrentData.currentGems += amount;
            OnGemsChanged?.Invoke(CurrentData.currentGems);
        }

        public void DeleteSave()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            CurrentData = NewGame();
        }

        public void ApplyToManagers()
        {
            if (CurrentData == null) return;

            GameManager.Instance.TimeManager.LoadState(
                CurrentData.currentDay,
                CurrentData.currentHour
            );

            // GÜNCELLEME: Artık aktif buff'ları da EnergyManager'a yüklüyoruz
            GameManager.Instance.EnergyManager.LoadState(
                CurrentData.currentEnergy,
                CurrentData.hoursSinceLastFood,
                CurrentData.activeRealTimeBuffs 
            );
        }

        public void CaptureFromManagers()
        {
            if (CurrentData == null) CurrentData = new SaveData();

            TimeManager time = GameManager.Instance.TimeManager;
            CurrentData.currentDay = time.CurrentDay;
            CurrentData.currentHour = time.CurrentHour;

            EnergyManager energy = GameManager.Instance.EnergyManager;
            CurrentData.currentEnergy = energy.CurrentEnergy;
            CurrentData.hoursSinceLastFood = energy.HoursSinceLastFood;
            
            // GÜNCELLEME: Aktif buff'ları EnergyManager'dan alıp kaydediyoruz
            CurrentData.activeRealTimeBuffs = new System.Collections.Generic.List<ActiveBuffSaveData>(energy.GetActiveBuffs());
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

        private void HandleNewDayStarted(int newDay)
        {
            SaveGame();
        }
    }
}