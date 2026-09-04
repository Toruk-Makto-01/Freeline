#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Freeline
{
    // Klavye kısayolları (Sadece Unity Editöründe çalışır):
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

            if (kb.wKey.wasPressedThisFrame) SimulateProduceChapter();
            if (kb.fKey.wasPressedThisFrame) SimulateFeed();
            if (kb.nKey.wasPressedThisFrame) SimulateNewDay();
            if (kb.rKey.wasPressedThisFrame) SimulateReset();
        }

        private void SimulateProduceChapter()
        {
            var wm = WebtoonManager.Instance;
            if (wm == null) { Debug.LogWarning("[Debug] WebtoonManager bulunamadı!"); return; }

            int followersBefore = wm.TotalFollowers;

            // Test için hafızada geçici bir bölüm (Chapter) verisi yaratıyoruz
            WebtoonChapterData dummyChapter = ScriptableObject.CreateInstance<WebtoonChapterData>();
            dummyChapter.chapterName = "Debug Test Bölümü";
            dummyChapter.baseFollowerGain = Random.Range(300, 800); // Rastgele takipçi versin

            // Bölümü yeni sisteme gönder
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