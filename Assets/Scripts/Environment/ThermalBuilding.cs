using UnityEngine;
using System.Collections.Generic;
using FrioAndino.Player;
using FrioAndino.Economy;
using FrioAndino.Audio;
using FrioAndino.Art;
using FrioAndino.Villagers;

namespace FrioAndino.Environment
{
    public enum BuildingKind { House, Shed }

    /// <summary>
    /// Vivienda de adobe o cobertizo de alpacas con 3 niveles bioclimaticos.
    /// La estructura base ya esta pintada en el fondo; este componente le monta encima
    /// las mejoras (paja de ichu, Muro Trombe) y calcula su Resistencia Termica (Rt):
    ///     T_interior = T_exterior + (Rt * 26) + calor del fogon
    /// Aqui vive la mecanica central del ODS 13: prevenir ANTES de que caiga la helada.
    /// </summary>
    public class ThermalBuilding : MonoBehaviour, IInteractable
    {
        [Header("Identidad")]
        [SerializeField] private BuildingKind kind = BuildingKind.House;
        [SerializeField] private string displayName = "Casa vecinal";

        [Header("Estado bioclimatico")]
        [Tooltip("0 = basico, 1 = sellado con ichu, 2 = Muro Trombe / modulo cerrado.")]
        [SerializeField] private int level = 0;
        [Tooltip("Resistencia termica por nivel (grados ganados = Rt * 26).")]
        [SerializeField] private float[] thermalResistance = { 0.15f, 0.48f, 0.88f };

        [Header("Ocupacion")]
        [SerializeField] private int occupants = 4;
        [Tooltip("Alpacas que caben por nivel (solo cobertizo).")]
        [SerializeField] private int[] shelterCapacity = { 0, 9, 20 };

        [Header("Faena")]
        [Tooltip("Segundos de faena con un solo trabajador; cada rondero acelera la obra.")]
        [SerializeField] private float baseWorkTime = 8f;

        public BuildingKind Kind => kind;
        public int Level => level;
        public int MaxLevel => 2;
        public string DisplayName => displayName;
        public int Occupants => occupants;
        public bool IsWorking => _working;
        public float WorkProgress => _workTotal > 0f ? _workDone / _workTotal : 0f;
        public float ThermalR => thermalResistance[Mathf.Clamp(level, 0, thermalResistance.Length - 1)];
        public int ShelterCapacity => shelterCapacity[Mathf.Clamp(level, 0, shelterCapacity.Length - 1)];

        private Vector3 _artPos;       // base de la construccion sobre el fondo pintado
        private float _widthUnits;
        private SpriteRenderer _ichu, _glass, _warmth;
        private Transform _barBg, _barFill;
        private SpriteRenderer[] _pips;
        private bool _working;
        private float _workDone, _workTotal;
        private int _assignedWorkers;
        private readonly List<VillagerAI> _crew = new List<VillagerAI>();

        /// <summary>Configuracion desde el bootstrap (posicion y ancho vienen del arte).</summary>
        public void Setup(BuildingKind buildingKind, string name, int startLevel, Vector3 artPosition, float widthUnits)
        {
            kind = buildingKind;
            displayName = name;
            level = Mathf.Clamp(startLevel, 0, MaxLevel);
            _artPos = artPosition;
            _widthUnits = widthUnits;
            occupants = kind == BuildingKind.House ? 4 : 0;
            BuildVisuals();
            RefreshVisuals();
        }

        private void Start()
        {
            Core.HealthShelterManager.Instance?.RegisterBuilding(this);
        }

        // =====================================================================
        //  Visuales montados sobre el arte
        // =====================================================================
        private SpriteRenderer MakeLayer(string name, Sprite sprite, Vector3 pos, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        private void BuildVisuals()
        {
            // Sobre el MAPA PANORAMICO no se dibuja ningun parche encima de las casas.
            // vidrio.bytes e ichu_techo.bytes son recortes del fondo ANTIGUO: no calzan con
            // el arte nuevo y se veian como cuadrados de color sueltos. El nivel de cada
            // construccion se lee en los 3 puntitos, que es lo unico que no ensucia el dibujo.
            bool panoramico = GameArt.LayeredStage;

            // --- Nivel 2: paja de ichu sellando vanos y techo ---
            var ichuSprite = panoramico ? null : GameArt.IchuThatch;
            _ichu = MakeLayer("Sellado_Ichu", ichuSprite,
                              _artPos + new Vector3(0f, _widthUnits * 0.46f, 0f), 12);
            if (ichuSprite != null)
            {
                float natW = ichuSprite.rect.width / GameArt.PPU;
                float natH = ichuSprite.rect.height / GameArt.PPU;
                _ichu.transform.localScale = new Vector3((_widthUnits * 0.92f) / natW,
                                                         (_widthUnits * 0.30f) / natH, 1f);
            }
            _ichu.enabled = false;

            // --- Nivel 3: panel de vidrio del Muro Trombe ---
            var glassSprite = panoramico ? null : GameArt.TrombeGlass;
            _glass = MakeLayer("Muro_Trombe", glassSprite,
                               _artPos + new Vector3(0f, _widthUnits * 0.20f, 0f), 13);
            if (glassSprite != null)
            {
                float natW = glassSprite.rect.width / GameArt.PPU;
                float natH = glassSprite.rect.height / GameArt.PPU;
                // Escala uniforme para no deformar el recorte del arte original
                float k = Mathf.Min((_widthUnits * 0.62f) / natW, (_widthUnits * 0.52f) / natH);
                _glass.transform.localScale = new Vector3(k, k, 1f);
            }
            _glass.enabled = false;

            // --- Calidez interior: se enciende de noche si la casa es habitable ---
            // Con el mapa panoramico tampoco se dibuja: se veia como una mancha palida.
            _warmth = MakeLayer("Calor",
                                panoramico ? null : PixelArtFactory.Circle(new Color32(255, 196, 108, 64), 64),
                                _artPos + new Vector3(0f, _widthUnits * 0.30f, 0f), 11);
            if (_warmth.sprite != null)
            {
                // El sprite del circulo mide 64/PPU unidades a escala 1: hay que dividir
                float nat = _warmth.sprite.bounds.size.x;
                _warmth.transform.localScale = Vector3.one * (_widthUnits * 1.15f / Mathf.Max(0.0001f, nat));
            }
            _warmth.enabled = false;

            // --- Indicador de nivel (3 puntos sobre la construccion) ---
            // Todo se mide contra el ancho de la construccion, asi encaja igual de bien
            // con el arte panoramico que con el fondo unico.
            _pips = new SpriteRenderer[3];
            float pipY = _widthUnits * (kind == BuildingKind.House ? 0.78f : 0.52f);
            float pipSize = _widthUnits * 0.075f;
            float pipGap = _widthUnits * 0.13f;
            for (int i = 0; i < 3; i++)
            {
                var p = MakeLayer("Pip" + i, PixelArtFactory.Solid(new Color32(255, 255, 255, 255), 8, 8),
                                  _artPos + new Vector3((i - 1) * pipGap, pipY, 0f), 46);
                p.transform.localScale = Vector3.one * pipSize;
                _pips[i] = p;
            }

            // --- Barra de faena ---
            float barY = pipY + pipSize * 2.4f;
            _barBg = MakeLayer("BarraFondo", PixelArtFactory.Solid(new Color32(18, 18, 26, 225), 8, 8),
                               _artPos + new Vector3(0f, barY, 0f), 47).transform;
            _barFill = MakeLayer("BarraRelleno", PixelArtFactory.Solid(new Color32(240, 190, 60, 255), 8, 8),
                                 _artPos + new Vector3(0f, barY, 0f), 48).transform;
            _barBg.localScale = new Vector3(_widthUnits * 0.9f, _widthUnits * 0.09f, 1f);
            _barFill.localScale = new Vector3(_widthUnits * 0.84f, _widthUnits * 0.06f, 1f);
            _barBg.gameObject.SetActive(false);
            _barFill.gameObject.SetActive(false);
        }

        private void RefreshVisuals()
        {
            if (_ichu != null) _ichu.enabled = level >= 1;
            if (_glass != null) _glass.enabled = level >= 2;
            for (int i = 0; i < 3; i++)
            {
                if (_pips[i] == null) continue;
                bool on = i <= level;
                _pips[i].color = on ? new Color32(120, 230, 160, 235) : new Color32(30, 34, 44, 190);
            }
        }

        private void Update()
        {
            if (_working)
            {
                // Cada rondero suma: el ayni es literalmente velocidad de obra
                float speed = 1f + _assignedWorkers * 0.9f;
                _workDone += Time.deltaTime * speed;
                UpdateBar();
                if (_workDone >= _workTotal) CompleteWork();
            }

            var ttm = Core.TimeTemperatureManager.Instance;
            if (ttm != null && _warmth != null)
            {
                bool night = ttm.Phase == Core.DayPhase.Night;
                _warmth.enabled = night && level >= 1;
                if (_warmth.enabled)
                {
                    var c = _warmth.color;
                    c.a = (0.16f + 0.10f * level) + Mathf.Sin(Time.time * 1.7f) * 0.02f;
                    _warmth.color = c;
                }
            }
        }

        private void UpdateBar()
        {
            _barBg.gameObject.SetActive(_working);
            _barFill.gameObject.SetActive(_working);
            if (!_working) return;
            float full = _widthUnits * 0.84f;
            float t = Mathf.Clamp01(WorkProgress);
            _barFill.localScale = new Vector3(full * t, _widthUnits * 0.06f, 1f);
            _barFill.position = _barBg.position + new Vector3(-(full - full * t) * 0.5f, 0f, 0f);
        }

        // =====================================================================
        //  Costos y faena
        // =====================================================================
        public List<KeyValuePair<ResourceType, int>> CostForNextLevel()
        {
            var list = new List<KeyValuePair<ResourceType, int>>();
            if (level >= MaxLevel) return list;
            if (kind == BuildingKind.House)
            {
                if (level == 0) list.Add(new KeyValuePair<ResourceType, int>(ResourceType.Ichu, 12));
                else { list.Add(new KeyValuePair<ResourceType, int>(ResourceType.Stone, 10));
                       list.Add(new KeyValuePair<ResourceType, int>(ResourceType.Ichu, 8)); }
            }
            else
            {
                if (level == 0) { list.Add(new KeyValuePair<ResourceType, int>(ResourceType.Ichu, 16));
                                  list.Add(new KeyValuePair<ResourceType, int>(ResourceType.Stone, 6)); }
                else { list.Add(new KeyValuePair<ResourceType, int>(ResourceType.Ichu, 18));
                       list.Add(new KeyValuePair<ResourceType, int>(ResourceType.Stone, 12)); }
            }
            return list;
        }

        public string NextLevelName()
        {
            if (level >= MaxLevel) return "Completo";
            if (kind == BuildingKind.House) return level == 0 ? "Sellado con ichu" : "Muro Trombe";
            return level == 0 ? "Techo y cama de ichu" : "Modulo cerrado para crias";
        }

        private void StartWork()
        {
            _working = true;
            _workDone = 0f;
            _workTotal = baseWorkTime;
            _crew.Clear();
            var vts = VillagerTaskSystem.Instance;
            if (vts != null) _crew.AddRange(vts.RequestCrew(transform.position, 4));
            _assignedWorkers = _crew.Count;
            UpdateBar();
        }

        private void CompleteWork()
        {
            _working = false;
            level = Mathf.Min(level + 1, MaxLevel);
            RefreshVisuals();
            UpdateBar();
            foreach (var v in _crew) if (v != null) v.ReleaseFromWork();
            _crew.Clear();
            AudioDirector.Instance?.BuildDone();
            UI.HUDController.Instance?.Log($"{displayName}: ahora tiene {LevelName(level)} (Rt {ThermalR:0.00}).");
            UI.HUDController.Instance?.FloatingText(_artPos + Vector3.up * GameArt.U(46f), "+Aislamiento",
                                                    new Color32(120, 230, 160, 255));
        }

        private string LevelName(int lv)
        {
            if (kind == BuildingKind.House)
                return lv == 0 ? "adobe sin sellar" : lv == 1 ? "sellado con ichu" : "Muro Trombe";
            return lv == 0 ? "pirca abierta" : lv == 1 ? "techo de ichu" : "modulo cerrado";
        }

        // =====================================================================
        //  IInteractable
        // =====================================================================
        public string GetPrompt()
        {
            if (_working) return $"{displayName}: faena en curso ({Mathf.RoundToInt(WorkProgress * 100f)}%)";
            if (level >= MaxLevel) return $"{displayName}: {LevelName(level)} — maximo nivel";
            string c = "";
            foreach (var kv in CostForNextLevel()) c += $"{ResourceManager.DisplayName(kv.Key)} x{kv.Value}  ";
            return $"Mejorar {displayName} -> {NextLevelName()}   [{c.Trim()}]";
        }

        public bool CanInteract() => !_working && level < MaxLevel;

        public string Interact(GameObject actor)
        {
            if (!CanInteract()) return null;
            var cost = CostForNextLevel();
            var rm = ResourceManager.Instance;
            if (rm == null || !rm.CanAfford(cost)) return "Faltan materiales para esta mejora.";
            rm.SpendAll(cost);
            StartWork();
            return _assignedWorkers > 0
                ? $"Faena iniciada con {_assignedWorkers} rondero(s) — ayni."
                : "Faena iniciada. Toca el pututo [Q] para convocar ronderos y acelerarla.";
        }

        public Transform GetTransform() => transform;

        /// <summary>Temperatura interior estimada segun Rt y el calor del fogon.</summary>
        public float InteriorTemperature(float exteriorTemp)
        {
            float fromInsulation = ThermalR * 26f;
            float fromFire = Campfire.Instance != null ? Campfire.Instance.HeatAt(transform.position) * 9f : 0f;
            return exteriorTemp + fromInsulation + fromFire;
        }
    }
}
