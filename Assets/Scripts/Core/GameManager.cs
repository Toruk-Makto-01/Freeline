using UnityEngine;

namespace Freeline
{
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

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private TimeManager    timeManager;
        [SerializeField] private EnergyManager  energyManager;
        [SerializeField] private SaveManager    saveManager;
        [SerializeField] private JobManager     jobManager;
        [SerializeField] private WebtoonManager webtoonManager;

        public TimeManager    TimeManager    => timeManager;
        public EnergyManager  EnergyManager  => energyManager;
        public SaveManager    SaveManager    => saveManager;
        public JobManager     JobManager     => jobManager;
        public WebtoonManager WebtoonManager => webtoonManager;

        public GameState CurrentState { get; private set; } = GameState.Initializing;

        public event System.Action<GameState, GameState> OnStateChanged;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

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
        }

        public void SetState(GameState newState)
        {
            if (newState == CurrentState) return;

            GameState previous = CurrentState;
            CurrentState = newState;
            OnStateChanged?.Invoke(previous, newState);
        }
    }
}
