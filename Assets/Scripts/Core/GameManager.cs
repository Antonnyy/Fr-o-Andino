using UnityEngine;
using System;
using FrioAndino.Audio;

namespace FrioAndino.Core
{
    public enum GameState { Intro, Playing, DawnReport, Victory, Defeat }

    /// <summary>
    /// Coordinador de la partida: encadena los 5 dias, recibe el balance nocturno
    /// y decide victoria o derrota segun los indicadores ODS 3 y ODS 13.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Campana")]
        [SerializeField] private int totalDays = 5;

        [Header("Condiciones de derrota")]
        [Tooltip("Fallecimientos humanos que provocan Game Over (ODS 3).")]
        [SerializeField] private int maxHumanDeaths = 3;
        [Tooltip("Porcentaje minimo del rebano que debe sobrevivir (ODS 13).")]
        [Range(0f, 1f)][SerializeField] private float minHerdRatio = 0.5f;

        [Header("Victoria")]
        [Range(0f, 1f)][SerializeField] private float victoryHerdRatio = 0.6f;

        public GameState State { get; private set; } = GameState.Intro;
        public bool IsPlaying => State == GameState.Playing;
        public Transform PlayerTransform { get; set; }
        public int TotalDays => totalDays;

        public event Action<NightReport> OnNightReport;
        public event Action<GameState, string> OnGameEnded;

        private NightReport _pendingReport;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void Start()
        {
            var ttm = TimeTemperatureManager.Instance;
            if (ttm != null) ttm.OnDawn += HandleDawn;
        }

        private void OnDestroy()
        {
            var ttm = TimeTemperatureManager.Instance;
            if (ttm != null) ttm.OnDawn -= HandleDawn;
        }

        /// <summary>Arranca la partida al cerrar el tutorial (el menu manda antes).</summary>
        public void BeginPlay()
        {
            if (State != GameState.Intro) return;
            State = GameState.Playing;
            if (TimeTemperatureManager.Instance != null) TimeTemperatureManager.Instance.Paused = false;
        }

        public void OnNightEvaluated(NightReport report)
        {
            _pendingReport = report;
        }

        private void HandleDawn(int newDay)
        {
            // Primero se revisan derrotas; si la comunidad resistio, se muestra el parte del dia
            if (CheckDefeat()) return;

            if (newDay > totalDays)
            {
                CheckVictory();
                return;
            }

            if (_pendingReport != null)
            {
                State = GameState.DawnReport;
                TimeTemperatureManager.Instance.Paused = true;
                OnNightReport?.Invoke(_pendingReport);
                _pendingReport = null;
            }
        }

        /// <summary>Cierra el parte del amanecer y devuelve el control al jugador.</summary>
        public void ResumeAfterReport()
        {
            if (State != GameState.DawnReport) return;
            State = GameState.Playing;
            TimeTemperatureManager.Instance.Paused = false;
        }

        private bool CheckDefeat()
        {
            var hsm = HealthShelterManager.Instance;
            if (hsm == null) return false;

            if (hsm.DeceasedCount >= maxHumanDeaths)
            {
                EndGame(GameState.Defeat,
                    $"La comunidad perdio {hsm.DeceasedCount} vidas por hipotermia y neumonia.\n" +
                    "Sin acondicionamiento termico, la helada gana (ODS 3, meta 3.9).");
                return true;
            }
            if (hsm.InitialAlpacas > 0 && hsm.HerdRatio < minHerdRatio)
            {
                EndGame(GameState.Defeat,
                    $"Solo sobrevivio el {hsm.HerdRatio * 100f:0}% del rebano.\n" +
                    "El sustento economico de la comunidad colapso (ODS 13, meta 13.1).");
                return true;
            }
            if (hsm.Morale <= 0f)
            {
                EndGame(GameState.Defeat,
                    "La moral comunal llego a 0%: el fogon se apago demasiadas noches.\n" +
                    "Sin fuego comun no hay comunidad.");
                return true;
            }
            return false;
        }

        private void CheckVictory()
        {
            var hsm = HealthShelterManager.Instance;
            if (hsm == null) return;

            bool herdOk = hsm.HerdRatio >= victoryHerdRatio;
            bool humansOk = hsm.DeceasedCount == 0;

            if (herdOk && humansOk)
            {
                EndGame(GameState.Victory,
                    "COMUNIDAD ALTOANDINA RESILIENTE\n\n" +
                    $"Rebano vivo: {hsm.AlpacaCount}/{hsm.InitialAlpacas} ({hsm.HerdRatio * 100f:0}%)\n" +
                    $"Vidas humanas perdidas: 0\n" +
                    $"Moral comunal: {hsm.Morale:0}%\n\n" +
                    "El Muro Trombe, los cobertizos y el ayni vencieron a la Gran Helada.");
            }
            else if (herdOk)
            {
                EndGame(GameState.Victory,
                    "AMANECIO EL DIA 6\n\n" +
                    $"La comunidad resistio, pero se perdieron {hsm.DeceasedCount} vida(s).\n" +
                    $"Rebano vivo: {hsm.HerdRatio * 100f:0}%\n\n" +
                    "Victoria parcial: aun falta prevencion (ODS 3).");
            }
            else
            {
                EndGame(GameState.Defeat,
                    $"Se supero el Dia 5, pero solo queda el {hsm.HerdRatio * 100f:0}% del rebano.\n" +
                    "La comunidad no alcanzo el umbral de resiliencia (60%).");
            }
        }

        public void EndGame(GameState result, string message)
        {
            if (State == GameState.Victory || State == GameState.Defeat) return;
            State = result;
            if (TimeTemperatureManager.Instance != null) TimeTemperatureManager.Instance.Paused = true;

            if (result == GameState.Victory) AudioDirector.Instance?.PlayVictory();
            else AudioDirector.Instance?.PlayDefeat();

            OnGameEnded?.Invoke(result, message);
        }

        public void Restart()
        {
            // Reinicio limpio: el bootstrap reconstruye todo el caserio al recargar la escena
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
}
