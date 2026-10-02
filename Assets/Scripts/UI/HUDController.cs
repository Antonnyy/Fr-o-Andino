using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using FrioAndino.Core;
using FrioAndino.Economy;
using FrioAndino.Art;
using FrioAndino.Villagers;
using FrioAndino.Player;
using FrioAndino.Environment;

namespace FrioAndino.UI
{
    /// <summary>
    /// HUD completo construido por codigo: reloj solar, termometro, recursos,
    /// medidores ODS 3 / ODS 13, parte del amanecer y pantallas de fin de partida.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        public static HUDController Instance { get; private set; }

        private Canvas _canvas;
        private Font _font;

        // Barra superior
        private Text _dayText, _clockText, _tempText, _tonightText;
        private Image _tempFill;
        private readonly Dictionary<ResourceType, Text> _resourceTexts = new Dictionary<ResourceType, Text>();
        private Text _ronderosText;

        // Medidores ODS
        private Text _odsHealthText, _odsHerdText, _odsMoraleText, _fireText;
        private Image _healthFill, _herdFill, _moraleFill, _fireFill;

        // Inferior
        private Text _promptText;
        private Image _promptBg;
        private Image _staminaFill;
        private Text _logText;
        private Text _bannerText;
        private Image _bannerBg;

        // Paneles
        private GameObject _introPanel, _reportPanel, _endPanel, _inGameOptionsPanel;
        private Text _reportText, _endText, _endTitle, _optMusicText, _optSfxText, _optFsText;

        private readonly Queue<string> _log = new Queue<string>();
        private float _bannerTimer;

        private static readonly Color32 PanelBg = new Color32(24, 26, 38, 225);
        private static readonly Color32 PanelBorder = new Color32(212, 160, 23, 255);
        private static readonly Color32 TextCream = new Color32(244, 238, 224, 255);

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _font = GetGameFont();
            BuildCanvas();
            BuildTopBar();
            BuildOdsPanel();
            BuildQuestPanel();
            BuildBottom();
            BuildIntro();
            BuildReportPanel();
            BuildEndPanel();
            BuildInGameOptions();
        }

        private void Start()
        {
            if (ResourceManager.Instance != null) ResourceManager.Instance.OnChanged += RefreshResources;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnNightReport += ShowReport;
                GameManager.Instance.OnGameEnded += ShowEnd;
            }
            var ttm = TimeTemperatureManager.Instance;
            if (ttm != null)
            {
                ttm.OnSunsetWarning += () => Banner("ATARDECE: guarece las alpacas y aviva el fogon", new Color32(220, 120, 40, 235));
                ttm.OnNightFall += () => Banner("CAE LA HELADA", new Color32(40, 70, 130, 235));
            }
            RefreshResources();
            if (ttm != null) ttm.Paused = true;   // el reloj no corre hasta salir del menu
            _canvas.enabled = false;              // el menu principal va primero
        }

        private void OnDestroy()
        {
            if (ResourceManager.Instance != null) ResourceManager.Instance.OnChanged -= RefreshResources;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnNightReport -= ShowReport;
                GameManager.Instance.OnGameEnded -= ShowEnd;
            }
        }

        // =====================================================================
        //  CONSTRUCCION DE UI
        // =====================================================================
        public static Font GetGameFont()
        {
            Font f = null;
            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (f == null) { try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
            if (f == null)
            {
                var names = Font.GetOSInstalledFontNames();
                if (names != null && names.Length > 0) f = Font.CreateDynamicFontFromOSFont(names[0], 20);
            }
            return f;
        }

        private void BuildCanvas()
        {
            var go = new GameObject("HUDCanvas");
            go.transform.SetParent(transform, false);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
        }

        private RectTransform Panel(Transform parent, Vector2 anchorMin, Vector2 anchorMax,
                                    Vector2 offsetMin, Vector2 offsetMax, Color32 color, bool border = true)
        {
            var go = new GameObject("Panel");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = PixelArtFactory.UIPanel(color, border ? PanelBorder : color);
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            return rt;
        }

        private Text Label(Transform parent, string txt, int size, TextAnchor anchor,
                           Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color32? color = null)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = _font;
            t.text = txt;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color ?? TextCream;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            return t;
        }

        private Image Bar(Transform parent, Vector2 anchorMin, Vector2 anchorMax,
                          Vector2 offsetMin, Vector2 offsetMax, Color32 fill, out Image background)
        {
            var bg = new GameObject("BarBg");
            bg.transform.SetParent(parent, false);
            background = bg.AddComponent<Image>();
            background.sprite = PixelArtFactory.Solid(new Color32(255, 255, 255, 255), 8, 8);
            background.color = new Color32(16, 18, 26, 220);
            var brt = bg.GetComponent<RectTransform>();
            brt.anchorMin = anchorMin; brt.anchorMax = anchorMax;
            brt.offsetMin = offsetMin; brt.offsetMax = offsetMax;

            var f = new GameObject("BarFill");
            f.transform.SetParent(bg.transform, false);
            var img = f.AddComponent<Image>();
            img.sprite = PixelArtFactory.Solid(new Color32(255, 255, 255, 255), 8, 8);
            img.color = fill;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            var frt = f.GetComponent<RectTransform>();
            frt.anchorMin = new Vector2(0f, 0f); frt.anchorMax = new Vector2(1f, 1f);
            frt.offsetMin = new Vector2(2f, 2f); frt.offsetMax = new Vector2(-2f, -2f);
            return img;
        }

        private Image IconImage(Transform parent, string kind, Vector2 anchorMin, Vector2 anchorMax,
                                Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject("Icon");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = PixelArtFactory.Icon(kind);
            img.preserveAspect = true;
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            return img;
        }

        // ---------------- Barra superior ----------------
        private void BuildTopBar()
        {
            var bar = Panel(_canvas.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                            new Vector2(16f, -104f), new Vector2(-16f, -12f), PanelBg);

            _dayText = Label(bar, "DIA 1 / 5", 34, TextAnchor.MiddleLeft,
                             new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(22f, -26f), new Vector2(250f, 26f),
                             new Color32(240, 205, 110, 255));

            _clockText = Label(bar, "06:00", 26, TextAnchor.MiddleLeft,
                               new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(250f, -24f), new Vector2(400f, 24f));

            // Termometro
            Label(bar, "TEMPERATURA", 15, TextAnchor.LowerLeft,
                  new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(420f, 4f), new Vector2(620f, 28f),
                  new Color32(180, 190, 210, 255));
            Image tbg;
            _tempFill = Bar(bar, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                            new Vector2(420f, -16f), new Vector2(700f, 2f), new Color32(120, 190, 240, 255), out tbg);
            _tempText = Label(bar, "+8.0 C", 28, TextAnchor.MiddleLeft,
                              new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(712f, -22f), new Vector2(880f, 22f));
            _tonightText = Label(bar, "Esta noche: -3 C", 15, TextAnchor.UpperLeft,
                                 new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(420f, -38f), new Vector2(700f, -18f),
                                 new Color32(150, 180, 220, 255));

            // Recursos
            string[] kinds = { "ichu", "wood", "herb", "stone" };
            ResourceType[] types = { ResourceType.Ichu, ResourceType.Firewood, ResourceType.Herbs, ResourceType.Stone };
            float x = -700f;
            for (int i = 0; i < types.Length; i++)
            {
                IconImage(bar, kinds[i], new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                          new Vector2(x, -18f), new Vector2(x + 36f, 18f));
                var t = Label(bar, "0", 26, TextAnchor.MiddleLeft,
                              new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(x + 42f, -18f), new Vector2(x + 130f, 18f));
                _resourceTexts[types[i]] = t;
                x += 150f;
            }
            IconImage(bar, "people", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                      new Vector2(x, -18f), new Vector2(x + 36f, 18f));
            _ronderosText = Label(bar, "0", 26, TextAnchor.MiddleLeft,
                                  new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(x + 42f, -18f), new Vector2(x + 120f, 18f));
        }

        // ---------------- Medidores ODS ----------------
        private void BuildOdsPanel()
        {
            var p = Panel(_canvas.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                          new Vector2(16f, -320f), new Vector2(376f, -116f), PanelBg);

            Label(p, "INDICADORES ODS", 16, TextAnchor.UpperLeft,
                  new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -28f), new Vector2(-16f, -6f),
                  new Color32(240, 205, 110, 255));

            Image bg;
            _odsHealthText = Label(p, "ODS 3 - Salud: 12 sanos", 17, TextAnchor.LowerLeft,
                                   new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -56f), new Vector2(-16f, -32f));
            _healthFill = Bar(p, new Vector2(0f, 1f), new Vector2(1f, 1f),
                              new Vector2(16f, -70f), new Vector2(-16f, -58f), new Color32(120, 220, 150, 255), out bg);

            _odsHerdText = Label(p, "ODS 13 - Rebano: 15/15", 17, TextAnchor.LowerLeft,
                                 new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -100f), new Vector2(-16f, -76f));
            _herdFill = Bar(p, new Vector2(0f, 1f), new Vector2(1f, 1f),
                            new Vector2(16f, -114f), new Vector2(-16f, -102f), new Color32(240, 220, 190, 255), out bg);

            _odsMoraleText = Label(p, "Moral comunal: 100%", 17, TextAnchor.LowerLeft,
                                   new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -144f), new Vector2(-16f, -120f));
            _moraleFill = Bar(p, new Vector2(0f, 1f), new Vector2(1f, 1f),
                              new Vector2(16f, -158f), new Vector2(-16f, -146f), new Color32(220, 170, 240, 255), out bg);

            _fireText = Label(p, "Fogon comunal: 100%", 17, TextAnchor.LowerLeft,
                              new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -188f), new Vector2(-16f, -164f));
            _fireFill = Bar(p, new Vector2(0f, 1f), new Vector2(1f, 1f),
                            new Vector2(16f, -202f), new Vector2(-16f, -190f), new Color32(255, 150, 60, 255), out bg);
        }

        private void BuildQuestPanel()
        {
            var q = gameObject.AddComponent<QuestHUD>();
            q.Build(_canvas.transform);
        }

        // ---------------- Zona inferior ----------------
        private void BuildBottom()
        {
            // Prompt de interaccion
            var prt = Panel(_canvas.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                            new Vector2(-430f, 120f), new Vector2(430f, 172f), new Color32(24, 26, 38, 235));
            _promptBg = prt.GetComponent<Image>();
            _promptText = Label(prt, "", 24, TextAnchor.MiddleCenter,
                                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(10f, 6f), new Vector2(-10f, -6f));
            _promptBg.enabled = false;

            // Stamina
            Label(_canvas.transform, "ALIENTO (4,000 m s.n.m.)", 14, TextAnchor.LowerLeft,
                  new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-200f, 82f), new Vector2(200f, 102f),
                  new Color32(170, 185, 205, 255));
            Image sbg;
            _staminaFill = Bar(_canvas.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                               new Vector2(-200f, 62f), new Vector2(200f, 80f), new Color32(150, 220, 255, 255), out sbg);

            // Log
            _logText = Label(_canvas.transform, "", 19, TextAnchor.LowerLeft,
                             new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(22f, 22f), new Vector2(760f, 170f),
                             new Color32(226, 220, 205, 255));

            // Sello de version: sirve para confirmar de un vistazo que corre el proyecto correcto
            Label(_canvas.transform, "FRIO ANDINO  v3  ·  mapa panoramico", 15, TextAnchor.LowerLeft,
                  new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-420f, 2f), new Vector2(-20f, 22f),
                  new Color32(120, 200, 150, 200));

            // Controles
            Label(_canvas.transform, "[A/D] mover   [Shift] correr   [E] interactuar   [Q] pututo   [T] adelantar la noche   [M/N] audio",
                  17, TextAnchor.LowerRight,
                  new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-820f, 20f), new Vector2(-20f, 46f),
                  new Color32(150, 160, 180, 255));

            // Banner central
            var brt = Panel(_canvas.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                            new Vector2(-420f, -230f), new Vector2(420f, -150f), new Color32(40, 70, 130, 235));
            _bannerBg = brt.GetComponent<Image>();
            _bannerText = Label(brt, "", 34, TextAnchor.MiddleCenter,
                                new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            _bannerBg.enabled = false;
        }

        // ---------------- Panel de intro / tutorial ----------------
        private void BuildIntro()
        {
            var root = new GameObject("IntroPanel");
            root.transform.SetParent(_canvas.transform, false);
            var rrt = root.AddComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;

            var dim = new GameObject("Dim");
            dim.transform.SetParent(root.transform, false);
            var dimg = dim.AddComponent<Image>();
            dimg.sprite = PixelArtFactory.Solid(new Color32(255, 255, 255, 255), 8, 8);
            dimg.color = new Color(0.03f, 0.05f, 0.1f, 0.9f);
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;

            var p = Panel(root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                          new Vector2(-520f, -330f), new Vector2(520f, 330f), PanelBg);

            Label(p, "FRIO ANDINO", 62, TextAnchor.UpperCenter,
                  new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -100f), new Vector2(0f, -20f),
                  new Color32(150, 200, 245, 255));
            Label(p, "Sobrevivir a la Helada", 30, TextAnchor.UpperCenter,
                  new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -142f), new Vector2(0f, -102f),
                  new Color32(240, 205, 110, 255));

            string body =
                "Eres INTI, Yachachiq del caserio de Andes Village, a 4,200 m s.n.m.\n" +
                "En 5 noches llegara la GRAN HELADA (-20 C).\n\n" +
                "DE DIA:  recorre el caserio con [A] y [D]. Con [E] cosechas ichu en la puna,\n" +
                "lena junto al almacen, hierbas en las laderas y piedra en las rocas del este.\n\n" +
                "AYNI:  toca el PUTUTO [Q] y los ronderos quedan atentos donde esten; luego parate\n" +
                "frente a una casa y pulsa [E]: ellos van solos. Con cuadrilla la faena va al triple.\n\n" +
                "TU CASA YA TIENE MURO TROMBE: es el modelo. Replicalo en las casas vecinas\n" +
                "y aviva el fogon comunal para que no se apague.\n\n" +
                "ANTES DEL ATARDECER:  arrea las alpacas al cobertizo de la derecha.\n" +
                "Ponte detras de una y camina: ella avanza unos pasos. Empujalas de a pocos.\n\n" +
                "DE NOCHE:  se compara el frio exterior con el aislamiento logrado.\n" +
                "               T_interior = T_exterior + (Rt x 26) + calor del fogon\n\n" +
                "VICTORIA: amanecer el Dia 6 con 60% del rebano vivo y cero muertes humanas.\n" +
                "DERROTA:  3 fallecimientos, perder mas del 50% del rebano o moral en 0%.";

            Label(p, body, 21, TextAnchor.UpperLeft,
                  new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(40f, 70f), new Vector2(-40f, -160f));

            Label(p, "Presiona [ESPACIO] para comenzar", 26, TextAnchor.LowerCenter,
                  new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 22f), new Vector2(0f, 58f),
                  new Color32(140, 230, 170, 255));

            _introPanel = root;
            _introPanel.SetActive(false);
        }

        /// <summary>Llamado por el menu principal al pulsar Jugar.</summary>
        public void ShowIntro()
        {
            _canvas.enabled = true;
            if (_introPanel != null) _introPanel.SetActive(true);
        }

        // ---------------- Parte del amanecer ----------------
        private void BuildReportPanel()
        {
            var root = new GameObject("ReportPanel");
            root.transform.SetParent(_canvas.transform, false);
            var rrt = root.AddComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;

            var dim = new GameObject("Dim");
            dim.transform.SetParent(root.transform, false);
            var dimg = dim.AddComponent<Image>();
            dimg.sprite = PixelArtFactory.Solid(new Color32(255, 255, 255, 255), 8, 8);
            dimg.color = new Color(0.05f, 0.06f, 0.12f, 0.82f);
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;

            var p = Panel(root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                          new Vector2(-440f, -280f), new Vector2(440f, 280f), PanelBg);

            Label(p, "REPORTE DEL AMANECER", 34, TextAnchor.UpperCenter,
                  new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -62f), new Vector2(0f, -16f),
                  new Color32(240, 205, 110, 255));

            _reportText = Label(p, "", 22, TextAnchor.UpperLeft,
                                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(38f, 70f), new Vector2(-38f, -72f));

            Label(p, "Presiona [ESPACIO] para continuar", 24, TextAnchor.LowerCenter,
                  new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 22f), new Vector2(0f, 56f),
                  new Color32(140, 230, 170, 255));

            _reportPanel = root;
            _reportPanel.SetActive(false);
        }

        // ---------------- Fin de partida ----------------
        private void BuildEndPanel()
        {
            var root = new GameObject("EndPanel");
            root.transform.SetParent(_canvas.transform, false);
            var rrt = root.AddComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;

            var dim = new GameObject("Dim");
            dim.transform.SetParent(root.transform, false);
            var dimg = dim.AddComponent<Image>();
            dimg.sprite = PixelArtFactory.Solid(new Color32(255, 255, 255, 255), 8, 8);
            dimg.color = new Color(0.02f, 0.03f, 0.07f, 0.92f);
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;

            var p = Panel(root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                          new Vector2(-460f, -260f), new Vector2(460f, 260f), PanelBg);

            _endTitle = Label(p, "", 46, TextAnchor.UpperCenter,
                              new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -80f), new Vector2(0f, -18f),
                              new Color32(240, 205, 110, 255));

            _endText = Label(p, "", 24, TextAnchor.UpperCenter,
                             new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(40f, 80f), new Vector2(-40f, -90f));

            Label(p, "[R] volver a jugar     [ESC] salir", 24, TextAnchor.LowerCenter,
                  new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 24f), new Vector2(0f, 60f),
                  new Color32(140, 230, 170, 255));

            _endPanel = root;
            _endPanel.SetActive(false);
        }

        // ---------------- Menu de Pausa y Opciones en Partida ----------------
        private void BuildInGameOptions()
        {
            var root = new GameObject("InGameOptionsPanel");
            root.transform.SetParent(_canvas.transform, false);
            var rrt = root.AddComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;

            var dim = new GameObject("Dim");
            dim.transform.SetParent(root.transform, false);
            var dimg = dim.AddComponent<Image>();
            dimg.sprite = PixelArtFactory.Solid(new Color32(255, 255, 255, 255), 8, 8);
            dimg.color = new Color(0.02f, 0.03f, 0.07f, 0.82f);
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            var close = dim.AddComponent<Button>();
            close.transition = Selectable.Transition.None;
            close.onClick.AddListener(ToggleInGameOptions);

            var p = Panel(root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                          new Vector2(-460f, -310f), new Vector2(460f, 310f), PanelBg);

            Label(p, "PAUSA - OPCIONES", 34, TextAnchor.UpperCenter,
                  new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -60f), new Vector2(0f, -15f),
                  new Color32(240, 205, 110, 255));

            // Music Row
            _optMusicText = Label(p, "Música: 80%", 22, TextAnchor.MiddleLeft,
                                  new Vector2(0.08f, 0.72f), new Vector2(0.40f, 0.82f), Vector2.zero, Vector2.zero);
            MakeBtn(p, "-", () => AdjustMusic(-0.1f), new Vector2(0.40f, 0.73f), new Vector2(0.46f, 0.81f));
            MakeBtn(p, "+", () => AdjustMusic(+0.1f), new Vector2(0.48f, 0.73f), new Vector2(0.54f, 0.81f));

            // SFX Row
            _optSfxText = Label(p, "Efectos SFX: 80%", 22, TextAnchor.MiddleLeft,
                                new Vector2(0.08f, 0.60f), new Vector2(0.40f, 0.70f), Vector2.zero, Vector2.zero);
            MakeBtn(p, "-", () => AdjustSfx(-0.1f), new Vector2(0.40f, 0.61f), new Vector2(0.46f, 0.69f));
            MakeBtn(p, "+", () => AdjustSfx(+0.1f), new Vector2(0.48f, 0.61f), new Vector2(0.54f, 0.69f));

            // Sound Test & Screen mode
            MakeBtn(p, "Probar Pututo", () => Audio.AudioDirector.Instance?.Pututo(),
                    new Vector2(0.58f, 0.73f), new Vector2(0.92f, 0.81f), new Color32(180, 110, 40, 255));
            _optFsText = Label(p, Screen.fullScreen ? "Pantalla: Completa" : "Pantalla: Ventana", 18, TextAnchor.MiddleCenter,
                               new Vector2(0.58f, 0.60f), new Vector2(0.92f, 0.70f), Vector2.zero, Vector2.zero);
            MakeBtn(p, "Cambiar Modo", () => {
                Screen.fullScreen = !Screen.fullScreen;
                if (_optFsText != null) _optFsText.text = Screen.fullScreen ? "Pantalla: Completa" : "Pantalla: Ventana";
            }, new Vector2(0.58f, 0.50f), new Vector2(0.92f, 0.58f), new Color32(50, 80, 120, 255));

            // Controls summary
            string info = "[A][D] Moverse   |   [Shift] Correr   |   [Espacio] Saltar   |   [E] Interactuar\n" +
                          "[Q] Pututo (convocar)   |   [T] Avanzar noche   |   [M] Mute música   |   [N] Mute sfx";
            Label(p, info, 18, TextAnchor.MiddleCenter,
                  new Vector2(0.05f, 0.24f), new Vector2(0.95f, 0.44f), Vector2.zero, Vector2.zero,
                  new Color32(200, 210, 225, 255));

            // Resume button
            MakeBtn(p, "REANUDAR JUEGO (ESC)", ToggleInGameOptions,
                    new Vector2(0.25f, 0.08f), new Vector2(0.75f, 0.18f), new Color32(50, 130, 85, 255));

            _inGameOptionsPanel = root;
            _inGameOptionsPanel.SetActive(false);
        }

        private void MakeBtn(Transform parent, string txt, UnityEngine.Events.UnityAction action,
                             Vector2 aMin, Vector2 aMax, Color32? bg = null)
        {
            var go = new GameObject("Btn_" + txt);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = bg ?? new Color32(65, 70, 90, 255);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(action);
            var l = Label(go.transform, txt, 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextCream);
            l.raycastTarget = false;
        }

        private void AdjustMusic(float delta)
        {
            var ad = Audio.AudioDirector.Instance;
            if (ad == null) return;
            ad.SetMusicVolume(ad.musicVolume + delta);
            RefreshInGameOptionsUI();
        }

        private void AdjustSfx(float delta)
        {
            var ad = Audio.AudioDirector.Instance;
            if (ad == null) return;
            ad.SetSfxVolume(ad.sfxVolume + delta);
            RefreshInGameOptionsUI();
        }

        private void RefreshInGameOptionsUI()
        {
            var ad = Audio.AudioDirector.Instance;
            if (ad != null)
            {
                if (_optMusicText != null) _optMusicText.text = $"Música: {Mathf.RoundToInt(ad.musicVolume * 100)}%";
                if (_optSfxText != null) _optSfxText.text = $"Efectos SFX: {Mathf.RoundToInt(ad.sfxVolume * 100)}%";
            }
            if (_optFsText != null)
                _optFsText.text = Screen.fullScreen ? "Pantalla: Completa" : "Pantalla: Ventana";
        }

        public void ToggleInGameOptions()
        {
            if (_introPanel != null && _introPanel.activeSelf) return;
            if (_reportPanel != null && _reportPanel.activeSelf) return;
            if (_endPanel != null && _endPanel.activeSelf) return;

            if (_inGameOptionsPanel != null)
            {
                bool opening = !_inGameOptionsPanel.activeSelf;
                _inGameOptionsPanel.SetActive(opening);
                Time.timeScale = opening ? 0f : 1f;
                if (opening) RefreshInGameOptionsUI();
            }
        }

        // =====================================================================
        //  ACTUALIZACION
        // =====================================================================
        private void Update()
        {
            HandlePanels();

            var ttm = TimeTemperatureManager.Instance;
            var hsm = HealthShelterManager.Instance;
            var gm = GameManager.Instance;

            if (ttm != null)
            {
                _dayText.text = $"DIA {Mathf.Min(ttm.CurrentDay, gm != null ? gm.TotalDays : 5)} / {(gm != null ? gm.TotalDays : 5)}";
                _clockText.text = (ttm.Phase == DayPhase.Day ? "SOL  " : "NOCHE  ") + ttm.ClockString();
                _tempText.text = $"{ttm.CurrentTemperature:+0.0;-0.0} C";
                _tempText.color = TempColor(ttm.CurrentTemperature);
                _tempFill.fillAmount = Mathf.InverseLerp(-22f, 10f, ttm.CurrentTemperature);
                _tempFill.color = TempColor(ttm.CurrentTemperature);
                _tonightText.text = ttm.Phase == DayPhase.Day
                    ? $"Esta noche bajara a {ttm.TonightLowTemperature:0} C  -  quedan {ttm.SecondsLeftInPhase:0} s de luz"
                    : $"Minima de la noche: {ttm.TonightLowTemperature:0} C";
            }

            if (hsm != null)
            {
                int sanos = hsm.HumanPopulation - hsm.SickCount;
                _odsHealthText.text = $"ODS 3 - Sanos {sanos}  |  Enfermos {hsm.SickCount}  |  Bajas {hsm.DeceasedCount}";
                _healthFill.fillAmount = hsm.InitialHumans > 0 ? (float)sanos / hsm.InitialHumans : 1f;

                _odsHerdText.text = $"ODS 13 - Rebano {hsm.AlpacaCount}/{hsm.InitialAlpacas}  |  En cobertizo {hsm.AlpacasSheltered}";
                _herdFill.fillAmount = hsm.HerdRatio;
                _herdFill.color = hsm.HerdRatio < 0.6f ? new Color32(240, 120, 100, 255) : new Color32(240, 220, 190, 255);

                _odsMoraleText.text = $"Moral comunal: {hsm.Morale:0}%";
                _moraleFill.fillAmount = hsm.Morale / 100f;
            }

            var fire = Campfire.Instance;
            if (fire != null)
            {
                _fireText.text = fire.IsLit ? $"Fogon comunal: {fire.FuelRatio * 100f:0}%" : "FOGON APAGADO!";
                _fireText.color = fire.IsLit ? TextCream : new Color32(255, 120, 100, 255);
                _fireFill.fillAmount = fire.FuelRatio;
            }

            if (VillagerTaskSystem.Instance != null)
                _ronderosText.text = $"{VillagerTaskSystem.Instance.AvailableRonderos}";

            // Prompt de interaccion
            var pi = FindPlayerInteraction();
            if (pi != null && pi.Current != null)
            {
                _promptBg.enabled = true;
                _promptText.text = pi.Current.CanInteract()
                    ? $"[E]  {pi.Current.GetPrompt()}"
                    : pi.Current.GetPrompt();
                _promptText.color = pi.Current.CanInteract() ? TextCream : new Color32(170, 175, 190, 255);
            }
            else
            {
                _promptBg.enabled = false;
                _promptText.text = "";
            }

            var pc = FindPlayer();
            if (pc != null) _staminaFill.fillAmount = pc.StaminaRatio;

            // Banner temporal
            if (_bannerTimer > 0f)
            {
                _bannerTimer -= Time.unscaledDeltaTime;
                if (_bannerTimer <= 0f) { _bannerBg.enabled = false; _bannerText.text = ""; }
            }

            if (Input.GetKeyDown(KeyCode.M)) { var a = Audio.AudioDirector.Instance; if (a != null) { a.ToggleMusic(); Log(a.MusicMuted ? "Musica silenciada." : "Musica activada."); } }
            if (Input.GetKeyDown(KeyCode.N)) { var a = Audio.AudioDirector.Instance; if (a != null) { a.ToggleSfx(); Log(a.SfxMuted ? "Efectos silenciados." : "Efectos activados."); } }

            // Adelantar al anochecer
            if (Input.GetKeyDown(KeyCode.T) && ttm != null && gm != null && gm.IsPlaying && ttm.Phase == DayPhase.Day)
            {
                Log("Inti da por terminada la jornada. Cae la noche...");
                ttm.SkipToNight();
            }

            // Pausa y opciones
            if (Input.GetKeyDown(KeyCode.Escape) && gm != null && gm.IsPlaying)
            {
                ToggleInGameOptions();
            }
        }

        private void HandlePanels()
        {
            var gm = GameManager.Instance;
            var ttm = TimeTemperatureManager.Instance;

            if (_inGameOptionsPanel != null && _inGameOptionsPanel.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    ToggleInGameOptions();
                }
                return;
            }

            if (_introPanel != null && _introPanel.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
                {
                    _introPanel.SetActive(false);
                    gm?.BeginPlay();
                    Log("Dia 1. Cosecha ichu en la puna y sella la casa de los Wari antes del anochecer.");
                }
                return;
            }

            if (_reportPanel != null && _reportPanel.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
                {
                    _reportPanel.SetActive(false);
                    gm?.ResumeAfterReport();
                }
                return;
            }

            if (_endPanel != null && _endPanel.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.R)) gm?.Restart();
                if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
            }
        }

        private PlayerInteraction _cachedPI;
        private PlayerController2D _cachedPC;

        private PlayerInteraction FindPlayerInteraction()
        {
            if (_cachedPI == null)
#if UNITY_2023_1_OR_NEWER
                _cachedPI = Object.FindFirstObjectByType<PlayerInteraction>();
#else
                _cachedPI = Object.FindObjectOfType<PlayerInteraction>();
#endif
            return _cachedPI;
        }

        private PlayerController2D FindPlayer()
        {
            if (_cachedPC == null)
#if UNITY_2023_1_OR_NEWER
                _cachedPC = Object.FindFirstObjectByType<PlayerController2D>();
#else
                _cachedPC = Object.FindObjectOfType<PlayerController2D>();
#endif
            return _cachedPC;
        }

        private static Color TempColor(float t)
        {
            if (t > 4f) return new Color32(255, 200, 120, 255);
            if (t > 0f) return new Color32(200, 225, 245, 255);
            if (t > -8f) return new Color32(140, 195, 245, 255);
            return new Color32(120, 150, 255, 255);
        }

        // =====================================================================
        //  API publica
        // =====================================================================
        private void RefreshResources()
        {
            var rm = ResourceManager.Instance;
            if (rm == null) return;
            foreach (var kv in _resourceTexts)
                kv.Value.text = rm.Get(kv.Key).ToString();
        }

        public void Log(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            _log.Enqueue(message);
            while (_log.Count > 5) _log.Dequeue();
            var sb = new StringBuilder();
            foreach (var l in _log) sb.AppendLine("> " + l);
            _logText.text = sb.ToString();
        }

        public void Banner(string text, Color32 color)
        {
            _bannerBg.enabled = true;
            _bannerBg.sprite = PixelArtFactory.UIPanel(color, PanelBorder);
            _bannerText.text = text;
            _bannerTimer = 3.5f;
        }

        /// <summary>Texto flotante en el mundo (se convierte a coordenadas de pantalla).</summary>
        public void FloatingText(Vector3 worldPos, string text, Color32 color)
        {
            StartCoroutine(FloatRoutine(worldPos, text, color));
        }

        private IEnumerator FloatRoutine(Vector3 worldPos, string text, Color32 color)
        {
            var go = new GameObject("Float");
            go.transform.SetParent(_canvas.transform, false);
            var t = go.AddComponent<Text>();
            t.font = _font;
            t.text = text;
            t.fontSize = 26;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = color;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(360f, 44f);

            float time = 0f;
            var cam = Camera.main;
            while (time < 1.3f)
            {
                time += Time.deltaTime;
                if (cam != null)
                {
                    Vector3 sp = cam.WorldToScreenPoint(worldPos + Vector3.up * time * 0.9f);
                    rt.position = sp;
                }
                var c = t.color; c.a = Mathf.Lerp(1f, 0f, time / 1.3f); t.color = c;
                yield return null;
            }
            Destroy(go);
        }

        private void ShowReport(NightReport rep)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Amanece el DIA {rep.Day + 1}.  La noche marco {rep.LowTemperature:0} C.");
            sb.AppendLine();
            sb.AppendLine($"ODS 3 - SALUD");
            sb.AppendLine($"   Nuevos enfermos (IRA): {rep.NewSick}");
            sb.AppendLine($"   Recuperados: {rep.Recovered}");
            sb.AppendLine($"   Fallecimientos: {rep.HumanDeaths}");
            sb.AppendLine();
            sb.AppendLine($"ODS 13 - GANADO");
            sb.AppendLine($"   Alpacas en cobertizo: {rep.AlpacasSheltered}");
            sb.AppendLine($"   Alpacas a la intemperie: {rep.AlpacasOutside}");
            sb.AppendLine($"   Perdidas por congelamiento: {rep.AlpacaDeaths}");
            sb.AppendLine();
            foreach (var l in rep.Lines) sb.AppendLine("- " + l);

            var hsm = HealthShelterManager.Instance;
            if (hsm != null)
            {
                sb.AppendLine();
                sb.AppendLine("ESTADO BIOCLIMATICO:");
                sb.Append(hsm.BuildingSummary());
            }

            _reportText.text = sb.ToString();
            _reportPanel.SetActive(true);
        }

        private void ShowEnd(GameState state, string message)
        {
            _endTitle.text = state == GameState.Victory ? "AMANECIO" : "LA HELADA VENCIO";
            _endTitle.color = state == GameState.Victory ? new Color32(140, 230, 170, 255) : new Color32(240, 120, 110, 255);
            _endText.text = message;
            _endPanel.SetActive(true);
            _reportPanel.SetActive(false);
        }
    }
}
