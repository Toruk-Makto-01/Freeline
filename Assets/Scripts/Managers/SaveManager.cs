using System;
using System.IO;
using UnityEngine;

namespace Freeline
{
    /// <summary>
    /// Oyun verilerini diske kaydeder ve diskten yükler.
    /// Veriler JSON formatında <c>Application.persistentDataPath</c> altına yazılır.
    /// Her yeni günde otomatik kayıt tetiklenir.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        private const string SaveFileName = "freeline_save.json";

        // persistentDataPath platforma göre değişir; sabit string yerine çalışma zamanında birleştiriliyor.
        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        /// <summary>
        /// Bellekteki aktif kayıt nesnesi.
        /// <see cref="LoadGame"/> veya yeni oyun başlatılmadan önce <c>null</c>'dır.
        /// JobManager ve WebtoonManager bu nesneye doğrudan yazarak anlık değişiklikleri saklar.
        /// </summary>
        public SaveData CurrentData { get; private set; }

        void Start()
        {
            // OnNewDayStarted, TimeManager gün sayacını artırdıktan VE
            // EnergyManager enerjiyi sıfırladıktan sonra tetiklenir
            // (EnergyManager DefaultExecutionOrder(-10) ile önce çalışır).
            // Bu sıralama sayesinde sabah anlık görüntüsü temiz biçimde kaydedilir.
            GameManager.Instance.TimeManager.OnNewDayStarted += HandleNewDayStarted;
        }

        void OnDestroy()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.TimeManager.OnNewDayStarted -= HandleNewDayStarted;
        }

        /// <summary>
        /// Kayıt dosyasını diskten okur ve <see cref="CurrentData"/>'yı doldurur.
        /// Dosya yoksa yeni bir oyun verisiyle başlar.
        /// Başlangıçta çağrılmalı; ardından <see cref="ApplyToManagers"/> ile durum geri yüklenir.
        /// </summary>
        /// <returns>Yüklenen ya da yeni oluşturulan <see cref="SaveData"/>.</returns>
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
                // Bozuk kayıt dosyası oyunu kitlememeli; hata loglanır ve temiz başlanır.
                Debug.LogError($"[SaveManager] Load failed: {e.Message}. Starting fresh.");
                CurrentData = NewGame();
                return CurrentData;
            }
        }

        /// <summary>
        /// Yöneticilerden güncel değerleri alır ve diske yazar.
        /// </summary>
        public void SaveGame()
        {
            CaptureFromManagers();
            WriteToDisk();
        }

        /// <summary>
        /// Kayıt dosyasını siler ve <see cref="CurrentData"/>'yı varsayılan değerlere sıfırlar.
        /// Yalnızca hata ayıklama ve oyun sıfırlama için kullanılır.
        /// </summary>
        public void DeleteSave()
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);

            CurrentData = NewGame();
            Debug.Log("[SaveManager] Save deleted.");
        }

        /// <summary>
        /// <see cref="CurrentData"/> içindeki değerleri ilgili yöneticilere uygular.
        /// Oyun başlangıcında <see cref="LoadGame"/> çağrısının hemen ardından çağrılır.
        /// </summary>
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

            // JobManager ve WebtoonManager SaveData'ya doğrudan yazıp okur; ayrı LoadState gerekmez.
            // Yeni sistemler (örn. LevelManager) eklendikçe burada çağrı eklenmeli.
        }

        /// <summary>
        /// Yöneticilerdeki güncel değerleri <see cref="CurrentData"/>'ya çeker.
        /// <see cref="WriteToDisk"/> çağrısından önce çağrılır.
        /// </summary>
        public void CaptureFromManagers()
        {
            if (CurrentData == null) CurrentData = new SaveData();

            TimeManager time = GameManager.Instance.TimeManager;
            CurrentData.currentDay  = time.CurrentDay;
            CurrentData.currentHour = time.CurrentHour;

            EnergyManager energy = GameManager.Instance.EnergyManager;
            CurrentData.currentEnergy      = energy.CurrentEnergy;
            CurrentData.hoursSinceLastFood = energy.HoursSinceLastFood;

            // currentCoins, totalJobsCompleted, playerLevel, webtoonData —
            // bu alanlar JobManager ve WebtoonManager tarafından doğrudan SaveData'ya yazılır;
            // burada ayrıca yakalanmalarına gerek yok.
        }

        /// <summary>
        /// Varsayılan değerlerle dolu yeni bir <see cref="SaveData"/> nesnesi oluşturur.
        /// </summary>
        private SaveData NewGame() => new SaveData();

        /// <summary>
        /// <see cref="CurrentData"/>'yı JSON'a dönüştürüp diske yazar.
        /// Yazma hatası oyunu durdurmaz; yalnızca loglanır.
        /// </summary>
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

        /// <summary>
        /// Yeni gün başladığında otomatik kayıt yapar.
        /// Bu noktada EnergyManager zaten enerjiyi sıfırlamış, TimeManager günü ilerlemiştir.
        /// </summary>
        private void HandleNewDayStarted(int newDay)
        {
            SaveGame();
        }
    }
}
