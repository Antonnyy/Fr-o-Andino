using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using FrioAndino.Art;
using FrioAndino.Audio;
using FrioAndino.Economy;
using FrioAndino.Environment;
using FrioAndino.Player;
using FrioAndino.UI;
using FrioAndino.Villagers;

namespace FrioAndino.Core
{
    /// <summary>
    /// Monta el juego completo sobre el fondo pintado del caserio.
    /// Cada objeto interactivo se coloca en las coordenadas en pixeles del arte,
    /// asi la casa que ves es exactamente la casa con la que interactuas.
    ///
    /// Se autoinstala con RuntimeInitializeOnLoadMethod: funciona en cualquier escena.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        // ---------------- Camara ----------------
        private const float OrthoSize = 4.8f;
        private float _cameraY = 3.45f;

        // ---------------- Posiciones sobre el MAPA PANORAMICO (pixeles del arte) ----------
        // Medidas sacadas de la imagen: 805 x 220 px, linea de pisada en y=140.
        private const float PxPlayerStart = 420f;   // Inti arranca en el centro del caserio, junto a su casa y el fogon
        private const float PxCasaWari = 85f;       // caserio de la izquierda, casa de adobe
        private const float PxCasaQuispe = 155f;    // caserio de la izquierda, segunda casa
        private const float PxCasaInti = 420f;      // casa grande del centro (demostrativa con Trombe)
        private const float PxFogon = 450f;         // la fogata encendida del centro
        private const float PxTomasa = 438f;        // abuela junto al fuego
        private const float PxMariano = 464f;       // abuelo junto al fuego
        private const float PxCobertizo = 550f;     // cobertizo abierto con las alpacas

        private const float PyCasaBase = 115f;      // linea donde se apoyan las casas pintadas
        private const float PyCobertizoBase = 120f;

        private Camera _cam;
        private Transform _worldRoot;
        private StageBuilder.Stage _stage;
        private readonly List<ThermalBuilding> _houses = new List<ThermalBuilding>();
        private ThermalBuilding _shed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBoot()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureBootstrap();
        }

        private static void OnSceneLoaded(Scene s, LoadSceneMode m) => EnsureBootstrap();

        private static void EnsureBootstrap()
        {
#if UNITY_2023_1_OR_NEWER
            var existing = Object.FindFirstObjectByType<GameBootstrap>();
#else
            var existing = Object.FindObjectOfType<GameBootstrap>();
#endif
            if (existing != null) return;
            var go = new GameObject("== FRIO ANDINO ==");
            go.AddComponent<GameBootstrap>();
        }

        private void Awake()
        {
            SetupCamera();
            SetupManagers();
            BuildBackground();
            BuildBuildings();
            BuildCampfire();
            BuildPickups();
            SpawnPlayer();
            SpawnVillagers();
            SpawnHerd();
            BuildHUD();
            BuildMenu();
        }

        // =====================================================================
        //  CAMARA
        // =====================================================================
        private void SetupCamera()
        {
            _cam = Camera.main;
            if (_cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                _cam = go.AddComponent<Camera>();
            }
            _cam.orthographic = true;
            _cam.orthographicSize = OrthoSize;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0.07f, 0.09f, 0.16f);
            _cam.transform.position = new Vector3(0f, _cameraY, -10f);
            if (_cam.GetComponent<AudioListener>() == null) _cam.gameObject.AddComponent<AudioListener>();

            // Escala provisional: si hay mapa panoramico, StageBuilder la recalcula
            GameArt.ComputeScale(_cam);

            var follow = _cam.gameObject.AddComponent<CameraFollow2D>();
            follow.Configure(null, _cameraY, OrthoSize);

            var fx = new GameObject("WeatherFX").AddComponent<WeatherFX>();
            fx.transform.SetParent(transform, false);
            fx.Configure(_cam);
        }

        // =====================================================================
        //  MANAGERS
        // =====================================================================
        private void SetupManagers()
        {
            if (AudioDirector.Instance == null)
            {
                var audio = new GameObject("AudioDirector");
                DontDestroyOnLoad(audio);
                audio.AddComponent<AudioDirector>();
            }

            var managers = new GameObject("Managers");
            managers.transform.SetParent(transform, false);
            managers.AddComponent<GameManager>();
            managers.AddComponent<TimeTemperatureManager>();
            managers.AddComponent<ResourceManager>();
            managers.AddComponent<HealthShelterManager>();
            managers.AddComponent<VillagerTaskSystem>();

            _worldRoot = new GameObject("Caserio").transform;
            _worldRoot.SetParent(transform, false);
        }

        // =====================================================================
        //  FONDO PINTADO
        // =====================================================================
        private void BuildBackground()
        {
            // --- Opcion A: mapa panoramico de dos capas (fondo_villa + suelo) ---
            _stage = StageBuilder.Build(_worldRoot, _cam);
            if (_stage != null)
            {
                // El mapa define donde va la camara verticalmente
                _cameraY = _stage.CameraY;
                _cam.transform.position = new Vector3(GameArt.X(PxPlayerStart), _cameraY, -10f);
                var f = _cam.GetComponent<CameraFollow2D>();
                if (f != null)
                {
                    f.Configure(null, _cameraY, OrthoSize);
                    f.SnapTo(GameArt.X(PxPlayerStart), _cameraY);
                    f.SetLocked(true, GameArt.X(PxPlayerStart));
                }
                return;
            }

            // --- Opcion B: fondo unico a pantalla completa (el de siempre) ---
            var go = new GameObject("Fondo_Caserio");
            go.transform.SetParent(_worldRoot, false);
            go.transform.position = Vector3.zero;                 // el pivote es la linea de suelo
            go.transform.localScale = Vector3.one * GameArt.Scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GameArt.Background;
            StageBuilder.SetSorting(sr, "Fondo", -200);

            if (sr.sprite == null)
                Debug.LogError("[FrioAndino] Falta el arte en Assets/Resources/FrioAndino/. " +
                               "Revisa que los .bytes esten en el proyecto.");
        }

        // =====================================================================
        //  EDIFICIOS (se apoyan sobre las casas ya pintadas en el fondo)
        // =====================================================================
        private ThermalBuilding MakeBuilding(BuildingKind kind, float px, float basePy,
                                             float widthPx, string name, int startLevel)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_worldRoot, false);
            go.transform.position = GameArt.Ground(px);
            var tb = go.AddComponent<ThermalBuilding>();
            tb.Setup(kind, name, startLevel, GameArt.Pos(px, basePy), GameArt.U(widthPx));
            return tb;
        }

        private void BuildBuildings()
        {
            // La casa del Yachachiq ya tiene el Muro Trombe: es el modelo a replicar
            _houses.Add(MakeBuilding(BuildingKind.House, PxCasaInti, PyCasaBase, 34f,
                                     "Casa demostrativa de Inti", 2));
            _houses.Add(MakeBuilding(BuildingKind.House, PxCasaWari, PyCasaBase, 28f,
                                     "Casa de la familia Wari", 0));
            _houses.Add(MakeBuilding(BuildingKind.House, PxCasaQuispe, PyCasaBase, 28f,
                                     "Casa de la familia Quispe", 0));

            _shed = MakeBuilding(BuildingKind.Shed, PxCobertizo, PyCobertizoBase, 46f,
                                 "Cobertizo comunal de alpacas", 0);
        }

        private void BuildCampfire()
        {
            var go = new GameObject("Fogon_Comunal");
            go.transform.SetParent(_worldRoot, false);
            go.transform.position = GameArt.Ground(PxFogon);
            var fire = go.AddComponent<Campfire>();
            // Brasas sobre la fogata pintada; el parche de ceniza justo encima
            fire.Setup(GameArt.Pos(PxFogon, 86f), GameArt.Pos(PxFogon, 83f));
        }

        // =====================================================================
        //  RECURSOS DEL MAPA
        // =====================================================================
        private void BuildPickups()
        {
            var root = new GameObject("Recursos").transform;
            root.SetParent(_worldRoot, false);

            const float PyCamino = 135f;   // sobre la franja de camino, encima de la pirca

            // Matas de ichu del camino (oeste -> este)
            Pickup(root, ResourceType.Ichu, 60f, PyCamino, 5, 18f);
            Pickup(root, ResourceType.Ichu, 260f, PyCamino, 5, 18f);
            Pickup(root, ResourceType.Ichu, 380f, PyCamino, 5, 20f);
            Pickup(root, ResourceType.Ichu, 620f, PyCamino, 4, 22f);
            Pickup(root, ResourceType.Ichu, 750f, PyCamino, 5, 20f);

            // Hierbas medicinales entre las matas
            Pickup(root, ResourceType.Herbs, 180f, PyCamino, 3, 26f);
            Pickup(root, ResourceType.Herbs, 490f, PyCamino, 3, 26f);
            Pickup(root, ResourceType.Herbs, 710f, PyCamino, 3, 26f);

            // Lena y bosta seca junto a las viviendas
            Pickup(root, ResourceType.Firewood, 95f, PyCamino, 4, 22f);
            Pickup(root, ResourceType.Firewood, 390f, PyCamino, 4, 22f);
            Pickup(root, ResourceType.Firewood, 480f, PyCamino, 5, 20f);

            // Piedra: en el muro de pirca y en el pedregal del puente
            Pickup(root, ResourceType.Stone, 130f, PyCamino, 4, 24f);
            Pickup(root, ResourceType.Stone, 290f, PyCamino, 4, 24f);
            Pickup(root, ResourceType.Stone, 580f, PyCamino, 4, 24f);
        }

        private void Pickup(Transform parent, ResourceType type, float px, float py, int amount, float respawn)
        {
            var go = new GameObject("Recurso_" + type);
            go.transform.SetParent(parent, false);
            go.transform.position = GameArt.Pos(px, py);
            go.transform.localScale = Vector3.one * GameArt.Scale * 0.85f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 8;
            go.AddComponent<ResourcePickup>().Configure(type, amount, respawn);
        }

        // =====================================================================
        //  PERSONAJES
        // =====================================================================
        private void SpawnPlayer()
        {
            var go = new GameObject("Inti");
            go.transform.SetParent(_worldRoot, false);
            go.transform.position = GameArt.Ground(PxPlayerStart);
            go.transform.localScale = Vector3.one * GameArt.Scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GameArt.Inti;
            sr.sortingOrder = 40;
            go.AddComponent<PlayerController2D>();
            go.AddComponent<PlayerInteraction>();

            if (GameManager.Instance != null) GameManager.Instance.PlayerTransform = go.transform;
            var follow = _cam.GetComponent<CameraFollow2D>();
            if (follow != null)
            {
                follow.Configure(go.transform, _cameraY, OrthoSize);
                follow.SnapTo(GameArt.X(PxPlayerStart), _cameraY);
                follow.SetLocked(true, GameArt.X(PxPlayerStart));
            }
        }

        private void SpawnVillagers()
        {
            var root = new GameObject("Comuneros").transform;
            root.SetParent(_worldRoot, false);

            // Los abuelos ya estan pintados sentados junto al fogon: alli se quedan.
            // Dona Tomasa atiende la posta comunal con sus mates de muna (ODS 3).
            var tomasa = MakeVillager(root, PxTomasa, true, true, _houses[0], "Dona_Tomasa", 1);
            tomasa.MakeSeated(GameArt.Pos(PxTomasa, 115f));

            // La posta vive en su propio objeto: sigue atendiendo aunque Tomasa caiga enferma
            var posta = new GameObject("Posta_de_Salud");
            posta.transform.SetParent(root, false);
            posta.transform.position = GameArt.Ground(PxTomasa);
            posta.AddComponent<HealthPost>();

            var mariano = MakeVillager(root, PxMariano, true, false, _houses[0], "Don_Mariano", 0, TintFor(1));
            mariano.MakeSeated(GameArt.Pos(PxMariano, 115f));

            // Ronderos con arte propio, bien separados por el mapa:
            //   variante 0 = el lenador (camina con su carga, hace adobes en la faena)
            //   variante 1 = la abuela  (camina por el caserio, cocina en su fogon)
            float[] spots = { 120f, 360f, 600f };
            int[] variantes = { 0, 1, 0 };
            for (int i = 0; i < spots.Length; i++)
                MakeVillager(root, spots[i], false, variantes[i] == 1, _houses[i % _houses.Count],
                            "Comunero_" + (i + 1), variantes[i], TintFor(i + 2));
        }

        /// <summary>
        /// Comunero: sprite base (una de 3 variantes de Inti) + un tinte suave propio.
        /// El tinte NO cambia el color del poncho de forma drastica: solo asegura que,
        /// aunque dos ronderos compartan variante, nunca se vean como copias identicas
        /// del jugador paradas una al lado de la otra.
        /// </summary>
        private static readonly Color[] AcentosComuneros =
        {
            new Color(1.00f, 1.00f, 1.00f), // 0 - Dona Tomasa: sin tinte, ella manda su propio look
            new Color(0.90f, 0.92f, 1.00f), // 1 - Don Mariano: ligeramente frio/gris
            new Color(1.00f, 0.90f, 0.82f), // 2 - calido/tierra
            new Color(0.85f, 1.00f, 0.90f), // 3 - verdoso apagado
            new Color(1.00f, 0.85f, 0.95f), // 4 - rosado apagado
            new Color(0.85f, 0.93f, 1.00f), // 5 - azulado
            new Color(1.00f, 0.95f, 0.78f), // 6 - ocre
            new Color(0.90f, 0.85f, 1.00f), // 7 - lila
            new Color(0.82f, 1.00f, 1.00f), // 8 - turquesa
            new Color(1.00f, 0.80f, 0.80f), // 9 - terracota
        };

        private static Color TintFor(int index) => AcentosComuneros[Mathf.Abs(index) % AcentosComuneros.Length];

        private VillagerAI MakeVillager(Transform parent, float px, bool elder, bool female,
                                        ThermalBuilding home, string name, int variant = -1, Color? tint = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = GameArt.Ground(px);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 30;
            var v = go.AddComponent<VillagerAI>();
            v.Configure(elder, female, home, variant, tint);
            return v;
        }

        private void SpawnHerd()
        {
            var root = new GameObject("Rebano").transform;
            root.SetParent(_worldRoot, false);

            // 15 alpacas pastando en los bofedales del este, incluida Sami
            // Pastan por el sector este; hay que arrearlas hacia el oeste hasta el cobertizo
            for (int i = 0; i < 13; i++)
                MakeAlpaca(root, Random.Range(620f, 780f), false, "Alpaca_" + (i + 1));
            MakeAlpaca(root, 650f, true, "Sami");
            MakeAlpaca(root, 720f, true, "Cria_2");
        }

        private void MakeAlpaca(Transform parent, float px, bool baby, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = GameArt.Ground(px);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 26;
            go.AddComponent<AlpacaAI>().Configure(baby, _shed, GameArt.X(PxCobertizo));
        }

        // =====================================================================
        //  INTERFAZ
        // =====================================================================
        private void BuildHUD()
        {
            var go = new GameObject("HUD");
            go.transform.SetParent(transform, false);
            go.AddComponent<HUDController>();
        }

        private void BuildMenu()
        {
            var go = new GameObject("MenuPrincipal");
            go.transform.SetParent(transform, false);
            go.AddComponent<MainMenuController>();
        }
    }
}
