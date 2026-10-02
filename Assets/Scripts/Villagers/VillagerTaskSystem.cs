using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using FrioAndino.Art;

namespace FrioAndino.Villagers
{
    /// <summary>
    /// Coordina el ayni: quien esta libre, quien responde al pututo y quien va a cada faena.
    /// La mano de obra es un recurso mas — sin comuneros convocados, las obras tardan el triple.
    /// </summary>
    public class VillagerTaskSystem : MonoBehaviour
    {
        public static VillagerTaskSystem Instance { get; private set; }

        [Tooltip("Radio en pixeles del fondo dentro del cual los comuneros escuchan el pututo.")]
        [SerializeField] private float pututoRadiusPixels = 320f;

        private readonly List<VillagerAI> _villagers = new List<VillagerAI>();
        private readonly List<VillagerAI> _followers = new List<VillagerAI>();

        public int TotalVillagers => _villagers.Count(v => v != null && v.gameObject.activeSelf);
        public int AvailableRonderos => _villagers.Count(v => v != null && v.gameObject.activeSelf && !v.IsElder && v.IsAvailable);
        public int FollowerCount => _followers.Count(v => v != null && v.IsConvocado);

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        public void Register(VillagerAI v)
        {
            if (!_villagers.Contains(v)) _villagers.Add(v);
        }

        public IReadOnlyList<VillagerAI> All => _villagers;

        /// <summary>
        /// Toca el pututo: los ronderos libres del radio quedan CONVOCADOS en su sitio.
        /// No caminan detras de Inti; salen recien cuando el marca la faena con [E].
        /// </summary>
        public int CallPututo(Transform leader)
        {
            _followers.Clear();
            int called = 0;
            foreach (var v in _villagers)
            {
                if (v == null || !v.gameObject.activeSelf) continue;
                if (v.IsElder || !v.IsAvailable) continue;
                if (Mathf.Abs(v.transform.position.x - leader.position.x) > GameArt.U(pututoRadiusPixels)) continue;
                v.RespondToPututo(leader);
                _followers.Add(v);
                called++;
            }
            return called;
        }

        /// <summary>Una obra pide cuadrilla: primero los convocados por el pututo, luego los libres cercanos.</summary>
        public List<VillagerAI> RequestCrew(Vector3 position, int maxWorkers)
        {
            var crew = new List<VillagerAI>();

            foreach (var v in _followers)
            {
                if (crew.Count >= maxWorkers) break;
                if (v == null || !v.gameObject.activeSelf) continue;
                if (!v.IsConvocado) continue;
                v.SendToWork(position);
                crew.Add(v);
            }

            if (crew.Count < maxWorkers)
            {
                var nearby = _villagers
                    .Where(v => v != null && v.gameObject.activeSelf && !v.IsElder && v.IsAvailable && !crew.Contains(v))
                    .OrderBy(v => Mathf.Abs(v.transform.position.x - position.x))
                    .Take(Mathf.Max(0, maxWorkers - crew.Count) - 1); // deja al menos uno libre para el HUD
                foreach (var v in nearby)
                {
                    if (Mathf.Abs(v.transform.position.x - position.x) > GameArt.U(220f)) continue;
                    v.SendToWork(position);
                    crew.Add(v);
                }
            }

            _followers.RemoveAll(v => crew.Contains(v));
            return crew;
        }
    }
}
