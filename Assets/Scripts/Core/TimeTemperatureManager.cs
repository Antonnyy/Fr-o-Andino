using UnityEngine;
using System;
using FrioAndino.Audio;

namespace FrioAndino.Core
{
    public enum DayPhase { Day, Night }

    /// <summary>
    /// Reloj solar + curva termica de los 5 dias. Es el "antagonista" del juego:
    /// la temperatura baja sola, el jugador solo puede prepararse antes de que caiga.
    /// </summary>
    public class TimeTemperatureManager : MonoBehaviour
    {
        public static TimeTemperatureManager Instance { get; private set; }

        [Header("Duracion del ciclo (segundos)")]
        [Tooltip("Fase diurna: tiempo para recolectar, construir y arrear alpacas.")]
        [SerializeField] private float dayDuration = 120f;
        [Tooltip("Fase nocturna: la comunidad resiste la helada.")]
        [SerializeField] private float nightDuration = 25f;

        [Header("Curva termica de la campana (5 dias)")]
        [SerializeField] private float[] dayTemperatures = { 8f, 6f, 4f, 2f, 0f };
        [SerializeField] private float[] nightTemperatures = { -3f, -6f, -10f, -14f, -20f };

        public int CurrentDay { get; private set; } = 1;
        public DayPhase Phase { get; private set; } = DayPhase.Day;
        public float PhaseTimer { get; private set; }
        public float CurrentTemperature { get; private set; }
        public bool Paused { get; set; }

        /// <summary>0 = recien amanece, 1 = termina la fase actual.</summary>
        public float PhaseProgress => Mathf.Clamp01(PhaseTimer / (Phase == DayPhase.Day ? dayDuration : nightDuration));
        public float SecondsLeftInPhase => Mathf.Max(0f, (Phase == DayPhase.Day ? dayDuration : nightDuration) - PhaseTimer);

        /// <summary>Temperatura minima que alcanzara esta noche (para avisos del HUD).</summary>
        public float TonightLowTemperature => nightTemperatures[Mathf.Clamp(CurrentDay - 1, 0, nightTemperatures.Length - 1)];
        public float TodayHighTemperature => dayTemperatures[Mathf.Clamp(CurrentDay - 1, 0, dayTemperatures.Length - 1)];

        /// <summary>Severidad 0..1 (0 = templado, 1 = -20 C). Controla viento, tinte y riesgo.</summary>
        public float ColdSeverity => Mathf.InverseLerp(8f, -20f, CurrentTemperature);

        public event Action OnNightFall;
        public event Action<int> OnDawn;            // recibe el nuevo dia
        public event Action OnSunsetWarning;        // 30 s antes del anochecer
        public event Action<DayPhase> OnPhaseChanged;

        private bool _warned;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            CurrentTemperature = TodayHighTemperature;
        }

        private void Start()
        {
            AudioDirector.Instance?.PlayDayMusic();
            OnPhaseChanged?.Invoke(Phase);
        }

        private void Update()
        {
            if (Paused) return;
            PhaseTimer += Time.deltaTime;

            if (Phase == DayPhase.Day)
            {
                // El dia empieza en su maxima y decae hacia 0 C al atardecer
                float t = PhaseProgress;
                float high = TodayHighTemperature;
                CurrentTemperature = Mathf.Lerp(high, Mathf.Min(high, 1f), Mathf.Pow(t, 1.6f));

                if (!_warned && SecondsLeftInPhase <= 30f)
                {
                    _warned = true;
                    OnSunsetWarning?.Invoke();
                    AudioDirector.Instance?.NightAlarm();
                }
                if (PhaseTimer >= dayDuration) EnterNight();
            }
            else
            {
                // La noche se desploma rapido y toca fondo a la mitad
                float t = PhaseProgress;
                float low = TonightLowTemperature;
                CurrentTemperature = Mathf.Lerp(1f, low, Mathf.Clamp01(t * 2f));
                if (PhaseTimer >= nightDuration) EnterDawn();
            }

            AudioDirector.Instance?.SetWindIntensity(ColdSeverity);
        }

        private void EnterNight()
        {
            Phase = DayPhase.Night;
            PhaseTimer = 0f;
            _warned = false;
            AudioDirector.Instance?.PlayNightMusic();
            OnPhaseChanged?.Invoke(Phase);
            OnNightFall?.Invoke();
        }

        private void EnterDawn()
        {
            Phase = DayPhase.Day;
            PhaseTimer = 0f;
            CurrentDay++;
            CurrentTemperature = TodayHighTemperature;
            AudioDirector.Instance?.Dawn();
            AudioDirector.Instance?.PlayDayMusic();
            OnPhaseChanged?.Invoke(Phase);
            OnDawn?.Invoke(CurrentDay);
        }

        /// <summary>Salto manual al anochecer (boton "Dormir / adelantar" del HUD).</summary>
        public void SkipToNight()
        {
            if (Phase != DayPhase.Day) return;
            PhaseTimer = 0f;
            EnterNight();
        }

        public string ClockString()
        {
            // 06:00 -> 18:00 de dia, 18:00 -> 06:00 de noche
            float h = Phase == DayPhase.Day ? Mathf.Lerp(6f, 18f, PhaseProgress)
                                            : Mathf.Repeat(Mathf.Lerp(18f, 30f, PhaseProgress), 24f);
            int hh = Mathf.FloorToInt(h);
            int mm = Mathf.FloorToInt((h - hh) * 60f);
            return $"{hh:00}:{mm:00}";
        }
    }
}
