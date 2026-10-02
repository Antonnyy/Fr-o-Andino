using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FrioAndino.Environment;
using FrioAndino.Villagers;

namespace FrioAndino.Core
{
    /// <summary>Resultado de una noche, para el reporte del amanecer.</summary>
    public class NightReport
    {
        public int Day;
        public float LowTemperature;
        public int NewSick;
        public int Recovered;
        public int HumanDeaths;
        public int AlpacaDeaths;
        public int AlpacasSheltered;
        public int AlpacasOutside;
        public bool FireWentOut;
        public List<string> Lines = new List<string>();
    }

    /// <summary>
    /// Corazon de los ODS: cada noche compara el frio exterior contra el aislamiento
    /// conseguido de dia y traduce esa diferencia en salud humana (ODS 3) y ganado (ODS 13).
    /// </summary>
    public class HealthShelterManager : MonoBehaviour
    {
        public static HealthShelterManager Instance { get; private set; }

        [Header("Umbrales")]
        [Tooltip("Bajo esta temperatura interior los ocupantes empiezan a enfermar.")]
        [SerializeField] private float sickThreshold = 0f;
        [Tooltip("Bajo esta temperatura interior un enfermo puede fallecer.")]
        [SerializeField] private float lethalThreshold = -6f;
        [Tooltip("Temperatura bajo la cual una alpaca a la intemperie corre riesgo mortal.")]
        [SerializeField] private float alpacaLethalTemp = -6f;

        private readonly List<ThermalBuilding> _buildings = new List<ThermalBuilding>();
        private readonly List<VillagerAI> _villagers = new List<VillagerAI>();
        private readonly List<AlpacaAI> _alpacas = new List<AlpacaAI>();

        public int InitialHumans { get; private set; }
        public int InitialAlpacas { get; private set; }
        public int DeceasedCount { get; private set; }
        public int DeadAlpacas { get; private set; }
        public float Morale { get; private set; } = 100f;
        public NightReport LastReport { get; private set; }

        public void AddMorale(float delta)
        {
            Morale = Mathf.Clamp(Morale + delta, 0f, 100f);
        }

        public int HumanPopulation => _villagers.Count(v => v != null && v.gameObject.activeSelf);
        public int SickCount => _villagers.Count(v => v != null && v.gameObject.activeSelf && v.IsSick);
        public int AlpacaCount => _alpacas.Count(a => a != null && !a.IsDead);
        public int AlpacasSheltered => _alpacas.Count(a => a != null && !a.IsDead && a.IsSheltered);
        public float HerdRatio => InitialAlpacas > 0 ? (float)AlpacaCount / InitialAlpacas : 1f;

        public IReadOnlyList<ThermalBuilding> Buildings => _buildings;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void Start()
        {
            InitialHumans = HumanPopulation;
            InitialAlpacas = AlpacaCount;

            var ttm = TimeTemperatureManager.Instance;
            if (ttm != null) ttm.OnNightFall += EvaluateNightImpact;
        }

        private void OnDestroy()
        {
            var ttm = TimeTemperatureManager.Instance;
            if (ttm != null) ttm.OnNightFall -= EvaluateNightImpact;
        }

        public void RegisterBuilding(ThermalBuilding b) { if (!_buildings.Contains(b)) _buildings.Add(b); }
        public void RegisterVillager(VillagerAI v) { if (!_villagers.Contains(v)) { _villagers.Add(v); InitialHumans = Mathf.Max(InitialHumans, _villagers.Count); } }
        public void RegisterAlpaca(AlpacaAI a) { if (!_alpacas.Contains(a)) { _alpacas.Add(a); InitialAlpacas = Mathf.Max(InitialAlpacas, _alpacas.Count); } }

        public void CureOne()
        {
            var sick = _villagers.FirstOrDefault(v => v != null && v.gameObject.activeSelf && v.IsSick);
            if (sick != null) { sick.SetSick(false); Morale = Mathf.Min(100f, Morale + 3f); }
        }

        /// <summary>
        /// Balance de la noche. Se calcula al anochecer con la minima prevista:
        /// T_interior = T_exterior + Rt*26 + calor del fogon.
        /// </summary>
        public void EvaluateNightImpact()
        {
            var ttm = TimeTemperatureManager.Instance;
            if (ttm == null) return;

            var rep = new NightReport
            {
                Day = ttm.CurrentDay,
                LowTemperature = ttm.TonightLowTemperature
            };

            float low = rep.LowTemperature;
            bool fireLit = Campfire.Instance != null && Campfire.Instance.IsLit;
            rep.FireWentOut = !fireLit;

            // ---------------- ODS 3: salud humana ----------------
            var houses = _buildings.Where(b => b != null && b.Kind == BuildingKind.House).ToList();
            var alive = _villagers.Where(v => v != null && v.gameObject.activeSelf).ToList();

            for (int i = 0; i < alive.Count; i++)
            {
                var v = alive[i];
                var house = v.Shelter != null ? v.Shelter : (houses.Count > 0 ? houses[i % houses.Count] : null);
                float interior = house != null ? house.InteriorTemperature(low) : low;

                // Los abuelos y los ya enfermos son mas vulnerables (realidad de las IRAS altoandinas)
                float vulnerability = 1f + (v.IsElder ? 0.6f : 0f) + (v.IsSick ? 0.5f : 0f);

                if (interior < lethalThreshold && v.IsSick)
                {
                    float pDeath = Mathf.Clamp01((lethalThreshold - interior) / 12f) * vulnerability;
                    if (Random.value < pDeath)
                    {
                        v.Die();
                        DeceasedCount++;
                        rep.HumanDeaths++;
                        rep.Lines.Add($"Un comunero fallecio de hipotermia ({(house != null ? house.DisplayName : "intemperie")}: {interior:0.0} C).");
                        continue;
                    }
                }

                if (interior < sickThreshold)
                {
                    float pSick = Mathf.Clamp01((sickThreshold - interior) / 14f) * vulnerability;
                    if (!v.IsSick && Random.value < pSick)
                    {
                        v.SetSick(true);
                        rep.NewSick++;
                    }
                }
                else if (v.IsSick && interior > 6f)
                {
                    // Casa caliente = recuperacion natural
                    if (Random.value < 0.4f) { v.SetSick(false); rep.Recovered++; }
                }
            }

            // ---------------- ODS 13: ganado ----------------
            var shed = _buildings.FirstOrDefault(b => b != null && b.Kind == BuildingKind.Shed);
            int capacity = shed != null ? shed.ShelterCapacity : 0;
            int insideCount = 0;

            var herd = _alpacas.Where(a => a != null && !a.IsDead).ToList();
            foreach (var a in herd)
            {
                bool inside = a.IsSheltered && insideCount < capacity;
                if (inside) insideCount++;

                float effectiveTemp = low;
                if (inside && shed != null) effectiveTemp = shed.InteriorTemperature(low);

                if (effectiveTemp < alpacaLethalTemp)
                {
                    // Las crias mueren primero: el dato real que el juego quiere transmitir
                    float pDeath = Mathf.Clamp01((alpacaLethalTemp - effectiveTemp) / 16f) * (a.IsBaby ? 1.8f : 1f);
                    if (Random.value < pDeath)
                    {
                        a.Die();
                        DeadAlpacas++;
                        rep.AlpacaDeaths++;
                    }
                }
            }
            rep.AlpacasSheltered = insideCount;
            rep.AlpacasOutside = herd.Count - insideCount;

            // ---------------- Moral comunal ----------------
            if (!fireLit) { Morale -= 22f; rep.Lines.Add("El fogon comunal se apago: la moral cayo."); }
            Morale -= rep.HumanDeaths * 18f;
            Morale -= rep.AlpacaDeaths * 2.5f;
            if (rep.HumanDeaths == 0 && rep.AlpacaDeaths == 0 && fireLit)
            {
                Morale = Mathf.Min(100f, Morale + 8f);
                rep.Lines.Add("Nadie sufrio danos esta noche. La comunidad recupera animo.");
            }
            Morale = Mathf.Clamp(Morale, 0f, 100f);

            if (rep.NewSick > 0) rep.Lines.Add($"{rep.NewSick} comunero(s) amanecieron con IRA. Atiendelos en la posta con mate de muna.");
            if (rep.Recovered > 0) rep.Lines.Add($"{rep.Recovered} comunero(s) se recuperaron gracias al confort termico.");
            if (rep.AlpacaDeaths > 0) rep.Lines.Add($"Se perdieron {rep.AlpacaDeaths} alpaca(s) por congelamiento.");
            if (rep.AlpacasOutside > 0) rep.Lines.Add($"{rep.AlpacasOutside} alpaca(s) pasaron la noche a la intemperie.");

            LastReport = rep;
            GameManager.Instance?.OnNightEvaluated(rep);
        }

        public string BuildingSummary()
        {
            var sb = new StringBuilder();
            foreach (var b in _buildings)
            {
                if (b == null) continue;
                sb.AppendLine($"{b.DisplayName}: nivel {b.Level + 1}/3  (Rt {b.ThermalR:0.00})");
            }
            return sb.ToString();
        }
    }
}
