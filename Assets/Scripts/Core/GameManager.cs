using UnityEngine;

namespace Freeline
{
    /// <summary>
    /// Oyunun genel akış durumlarını tanımlar.
    /// Her değer, oyuncunun hangi ekranda ya da modda olduğunu temsil eder.
    /// </summary>
    public enum GameState
    {
        Initializing,
        MainMenu,
        Apartment,
        DrawingDesk,
        JobBoard,
        WebtoonStudio,
        Paused
    }

    /// <summary>
    /// Oyunun merkezi yönetici sınıfı. Singleton olarak çalışır ve tüm alt yöneticilere
    /// (TimeManager, EnergyManager vb.) tek bir erişim noktası sağlar.
    /// Sahne geçişlerinde yok edilmez; tüm oyun boyunca yaşar.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        /// <summary>
        /// Küresel erişim noktası. Diğer sınıflar yöneticilere
        /// <c>GameManager.Instance.XxxManager</c> ile ulaşır.
        /// </summary>
        public static GameManager Instance { get; private set; }

        [SerializeField] private TimeManager       timeManager;
        [SerializeField] private EnergyManager    energyManager;
        [SerializeField] private SaveManager      saveManager;
        [SerializeField] private JobManager       jobManager;
        [SerializeField] private WebtoonManager   webtoonManager;
        [SerializeField] private ExhibitionManager exhibitionManager;

        /// <summary>Zaman yöneticisine salt-okunur erişim.</summary>
        public TimeManager       TimeManager       => timeManager;

        /// <summary>Enerji ve açlık yöneticisine salt-okunur erişim.</summary>
        public EnergyManager     EnergyManager     => energyManager;

        /// <summary>Kayıt sistemi yöneticisine salt-okunur erişim.</summary>
        public SaveManager       SaveManager       => saveManager;

        /// <summary>İş (komisyon) yöneticisine salt-okunur erişim.</summary>
        public JobManager        JobManager        => jobManager;

        /// <summary>Webtoon üretim ve yayın yöneticisine salt-okunur erişim.</summary>
        public WebtoonManager    WebtoonManager    => webtoonManager;

        /// <summary>Haftalık sergi yöneticisine salt-okunur erişim.</summary>
        public ExhibitionManager ExhibitionManager => exhibitionManager;

        /// <summary>
        /// Oyunun şu anki durumu. Başlangıç değeri <see cref="GameState.Initializing"/>'dir;
        /// Bootstrap tamamlandıktan sonra <see cref="SetState"/> ile güncellenir.
        /// </summary>
        public GameState CurrentState { get; private set; } = GameState.Initializing;

        /// <summary>
        /// Durum değiştiğinde tetiklenir. Parametreler sırasıyla: önceki durum, yeni durum.
        /// UI geçişleri ve müzik değişimi bu event'i dinleyebilir.
        /// </summary>
        public event System.Action<GameState, GameState> OnStateChanged;

        void Awake()
        {
            // Singleton koruması: sahnede ikinci bir GameManager varsa hemen yok et.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Inspector'da atanmamış referansları aynı GameObject üzerinde ara.
            // Tüm yöneticiler GameManager ile aynı GO'ya component olarak eklenir.
            if (timeManager == null)
                timeManager = GetComponent<TimeManager>();
            if (energyManager == null)
                energyManager = GetComponent<EnergyManager>();
            if (saveManager == null)
                saveManager = GetComponent<SaveManager>();
            if (jobManager == null)
                jobManager = GetComponent<JobManager>();
            if (webtoonManager == null)
                webtoonManager = GetComponent<WebtoonManager>();
            if (exhibitionManager == null)
                exhibitionManager = GetComponent<ExhibitionManager>();
        }

        /// <summary>Çizim masası ekranına geçiş yapar.</summary>
        public void OpenDrawingDesk() => SetState(GameState.DrawingDesk);

        /// <summary>
        /// Oyunun durumunu değiştirir ve dinleyicileri bilgilendirir.
        /// Aynı duruma geçiş isteği sessizce görmezden gelinir.
        /// </summary>
        /// <param name="newState">Geçilmek istenen yeni durum.</param>
        public void SetState(GameState newState)
        {
            if (newState == CurrentState) return;

            GameState previous = CurrentState;
            CurrentState = newState;
            OnStateChanged?.Invoke(previous, newState);
        }
    }
}
