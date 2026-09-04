#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Freeline
{
    // Klavye kısayolları (Sadece Unity Editöründe çalışır):
    //   J  →  Hızlı iş tamamla (Enerji ve zaman harcar, Coin kazandırır)
    //   W  →  Test amaçlı yeni bir Webtoon bölümü yayınla ve takipçi kazan
    //   F  →  Yemek ye (Açlığı sıfırla, 30 enerji kazan)
    //   N  →  Yeni güne geç
    //   R  →  Kayıt dosyasını sil ve sahneyi yeniden yükle
    public class DebugTestRunner : MonoBehaviour
    {
        public static DebugTestRunner Instance;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Update()
        {
            if (GameManager.Instance == null) return;
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
            var gm = GameManager.Instance;
            var energy = gm.EnergyManager;
            var time = gm.TimeManager;
            var save = gm.SaveManager?.CurrentData;

            if (energy == null || time == null || save == null) return;

            // Simüle edilecek işin bedelleri ve ödülü
            float energyCost = 25f;
            float timeCost = 3f; // 3 saat
            float coinReward = Random.Range(100f, 300f);

            if (energy.CurrentEnergy < energyCost)
            {
                Debug.LogWarning($"[DebugTest] J → İş yapılamadı! Yetersiz enerji. (Gereken: {energyCost}, Mevcut: {energy.CurrentEnergy:F0})");
                return;
            }

            // Enerji düşme metodun "ConsumeEnergy" veya benzeri ise burayı kendi sistemine göre uyarlayabilirsin
            energy.ConsumeEnergy(energyCost);
            time.AdvanceTime(timeCost);
            save.currentCoins += coinReward;
            GameManager.Instance.SaveManager.LogDailyTransaction("Freelance İş (Debug)", coinReward, true);

            Debug.Log(
                $"[DebugTest] J → Hızlı İş Tamamlandı! | " +
                $"Kazanılan: +{coinReward:F0} Coin | " +
                $"Harcanan Enerji: -{energyCost} | " +
                $"Geçen Süre: {timeCost} Saat | " +
                $"Toplam Coin: {save.currentCoins:F0}"
            );
        }

        private void SimulateProduceChapter()
        {
            var wm = WebtoonManager.Instance;
            if (wm == null) { Debug.LogWarning("[Debug] WebtoonManager bulunamadı!"); return; }

            int followersBefore = wm.TotalFollowers;

            WebtoonChapterData dummyChapter = ScriptableObject.CreateInstance<WebtoonChapterData>();
            dummyChapter.chapterName = "Debug Test Bölümü";
            dummyChapter.baseFollowerGain = Random.Range(300, 800);

            wm.PublishChapter(dummyChapter);

            Debug.Log(
                $"[DebugTest] W → {dummyChapter.chapterName} yayınlandı! | " +
                $"Takipçi: {followersBefore} -> {wm.TotalFollowers} (+{dummyChapter.baseFollowerGain}) | " +
                $"Okunmamış Yorum: {wm.UnreadCommentCount}"
            );
        }

        private void SimulateFeed()
        {
            var gm = GameManager.Instance;
            var energy = gm.EnergyManager;
            if (energy == null) return;

            float beforeE = energy.CurrentEnergy;
            float beforeH = energy.CurrentHunger;

            energy.RestoreHunger(100f);
            energy.RestoreEnergyDirect(30f);

            Debug.Log($"[DebugTest] F → Yemek Yenildi! | Açlık: %{beforeH:F0} -> %{energy.CurrentHunger:F0} | Enerji: {beforeE:F0} -> {energy.CurrentEnergy:F0}");
        }

        private void SimulateNewDay()
        {
            var gm = GameManager.Instance;
            var time = gm.TimeManager;
            var save = gm.SaveManager?.CurrentData;

            if (time == null || save == null) return;

            float coinsBefore = save.currentCoins;
            time.AdvanceTime(24f);

            Debug.Log($"[DebugTest] N → Yeni Gün: {time.CurrentDay} | Pasif Gelir/Fark: +{save.currentCoins - coinsBefore:F2} Coin");
        }

        private void SimulateReset()
        {
            var save = GameManager.Instance?.SaveManager;
            Debug.Log("[DebugTest] R → Kayıt dosyası siliniyor ve sahne sıfırlanıyor...");
            save?.DeleteSave();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
#endif