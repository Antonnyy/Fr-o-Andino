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

        // Paneles y pestañas de sistemas
        private GameObject _introPanel, _reportPanel, _endPanel, _inGameOptionsPanel;
        private Text _reportText, _reportRightText, _endText, _endTitle, _optMusicText, _optSfxText, _optFsText;
        private GameObject _tabRecursosObj, _tabSecundariosObj, _tabProgresionObj, _tabAjustesObj;
        private Image[] _tabBtnImages = new Image[4];
        private Text _tabRecursosInfo, _tabSecundariosInfo, _tabProgresionInfo;
        private int _activeTab = 0;

        // Cuaderno del Yachachiq
        private GameObject _notebookPanel;
        private Image[] _notebookTabBtns = new Image[4];
        private Text _nbTitleText, _nbTheoryText, _nbStatsText, _nbPageNumText, _nbActionBtnLabel;
        private int _nbActivePage = 0;

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
            BuildYachachiqNotebook();
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
            scaler.matchWidthOrHeight = 1.0f;
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

            // Termómetro Polar estilizado (icono con cristales de hielo y mercurio)
            var thGo = new GameObject("ThermometerIcon");
            thGo.transform.SetParent(bar, false);
            var thImg = thGo.AddComponent<Image>();
            thImg.sprite = GameArt.ThermometerHud;
            thImg.preserveAspect = true;
            thImg.raycastTarget = false;
            var thRt = thGo.GetComponent<RectTransform>();
            thRt.anchorMin = new Vector2(0f, 0.5f);
            thRt.anchorMax = new Vector2(0f, 0.5f);
            thRt.offsetMin = new Vector2(395f, -38f);
            thRt.offsetMax = new Vector2(430f, 38f);

            // Termometro
            Label(bar, "TEMPERATURA", 15, TextAnchor.LowerLeft,
                  new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(438f, 4f), new Vector2(638f, 28f),
                  new Color32(180, 190, 210, 255));
            Image tbg;
            _tempFill = Bar(bar, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                            new Vector2(438f, -16f), new Vector2(680f, 2f), new Color32(120, 190, 240, 255), out tbg);
            _tempText = Label(bar, "+8.0 C", 28, TextAnchor.MiddleLeft,
                              new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(692f, -22f), new Vector2(810f, 22f));
            _tonightText = Label(bar, "Esta noche: -3 C", 15, TextAnchor.UpperLeft,
                                 new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(438f, -38f), new Vector2(680f, -18f),
                                 new Color32(150, 180, 220, 255));

            // Botón directo para abrir Sistemas y Menús de Opciones
            MakeBtn(bar, "SISTEMAS [TAB]", ToggleInGameOptions,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Color32(180, 135, 45, 255));
            var bRt = bar.Find("Btn_SISTEMAS [TAB]")?.GetComponent<RectTransform>();
            if (bRt != null)
            {
                bRt.offsetMin = new Vector2(815f, -22f);
                bRt.offsetMax = new Vector2(970f, 22f);
            }

            // Botón directo para el Cuaderno del Yachachiq (con icono oficial)
            var nbBtnGo = new GameObject("Btn_NotebookYachachiq");
            nbBtnGo.transform.SetParent(bar, false);
            var nbrt = nbBtnGo.AddComponent<RectTransform>();
            nbrt.anchorMin = new Vector2(0f, 0.5f);
            nbrt.anchorMax = new Vector2(0f, 0.5f);
            nbrt.offsetMin = new Vector2(985f, -22f);
            nbrt.offsetMax = new Vector2(1185f, 22f);
            var nbImg = nbBtnGo.AddComponent<Image>();
            nbImg.color = new Color32(40, 80, 115, 255);
            var nbBtn = nbBtnGo.AddComponent<Button>();
            nbBtn.targetGraphic = nbImg;
            nbBtn.onClick.AddListener(ToggleYachachiqNotebook);

            var icGo = new GameObject("Icon");
            icGo.transform.SetParent(nbBtnGo.transform, false);
            var icImg = icGo.AddComponent<Image>();
            icImg.sprite = GameArt.YachachiqNotebook;
            icImg.preserveAspect = true;
            icImg.raycastTarget = false;
            var icRt = icGo.GetComponent<RectTransform>();
            icRt.anchorMin = new Vector2(0f, 0.5f);
            icRt.anchorMax = new Vector2(0f, 0.5f);
            icRt.offsetMin = new Vector2(8f, -17f);
            icRt.offsetMax = new Vector2(42f, 17f);

            var nbLbl = Label(nbBtnGo.transform, "CUADERNO [J]", 16, TextAnchor.MiddleCenter,
                              new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(34f, 0f), Vector2.zero,
                              new Color32(245, 235, 205, 255));
            nbLbl.raycastTarget = false;

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
                          new Vector2(16f, -270f), new Vector2(280f, -116f), PanelBg);

            Label(p, "ESTADO COMUNAL", 15, TextAnchor.UpperLeft,
                  new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -24f), new Vector2(-14f, -6f),
                  new Color32(240, 205, 110, 255));

            Image bg;
            _odsHealthText = Label(p, "Salud Comunal", 14, TextAnchor.LowerLeft,
                                   new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -46f), new Vector2(-14f, -26f));
            _healthFill = Bar(p, new Vector2(0f, 1f), new Vector2(1f, 1f),
                              new Vector2(14f, -58f), new Vector2(-14f, -48f), new Color32(120, 220, 150, 255), out bg);

            _odsHerdText = Label(p, "Rebaño de Alpacas", 14, TextAnchor.LowerLeft,
                                 new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -80f), new Vector2(-14f, -60f));
            _herdFill = Bar(p, new Vector2(0f, 1f), new Vector2(1f, 1f),
                            new Vector2(14f, -92f), new Vector2(-14f, -82f), new Color32(240, 220, 190, 255), out bg);

            _odsMoraleText = Label(p, "Moral", 14, TextAnchor.LowerLeft,
                                   new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -114f), new Vector2(-14f, -94f));
            _moraleFill = Bar(p, new Vector2(0f, 1f), new Vector2(1f, 1f),
                              new Vector2(14f, -126f), new Vector2(-14f, -116f), new Color32(220, 170, 240, 255), out bg);

            _fireText = Label(p, "Fogón Comunal", 14, TextAnchor.LowerLeft,
                              new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -148f), new Vector2(-14f, -128f));
            _fireFill = Bar(p, new Vector2(0f, 1f), new Vector2(1f, 1f),
                            new Vector2(14f, -160f), new Vector2(-14f, -150f), new Color32(255, 150, 60, 255), out bg);
        }

        private void BuildQuestPanel()
        {
            // Libreta y metas se consultan en el Cuaderno del Yachachiq [J] para mantener la pantalla despejada
        }

        // ---------------- Zona inferior ----------------
        private void BuildBottom()
        {
            // Prompt de interaccion limpio y compacto (sin ocupar toda la pantalla)
            var prt = Panel(_canvas.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                            new Vector2(-220f, 85f), new Vector2(220f, 130f), new Color32(24, 26, 38, 235));
            _promptBg = prt.GetComponent<Image>();
            _promptText = Label(prt, "", 20, TextAnchor.MiddleCenter,
                                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(10f, 4f), new Vector2(-10f, -4f));
            _promptBg.enabled = false;

            // Barra de aliento sutil en el fondo
            Image sbg;
            _staminaFill = Bar(_canvas.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                               new Vector2(-120f, 22f), new Vector2(120f, 34f), new Color32(150, 220, 255, 200), out sbg);

            // Log: solo aviso breve individual (sin parrafos acumulados)
            _logText = Label(_canvas.transform, "", 16, TextAnchor.LowerLeft,
                             new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 16f), new Vector2(500f, 46f),
                             new Color32(230, 225, 210, 240));

            // Banner central para avisos importantes
            var brt = Panel(_canvas.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                            new Vector2(-360f, -220f), new Vector2(360f, -150f), new Color32(40, 70, 130, 235));
            _bannerBg = brt.GetComponent<Image>();
            _bannerText = Label(brt, "", 28, TextAnchor.MiddleCenter,
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
                "Eres INTI, Yachachiq de la comunidad andina (4,200 m s.n.m.).\n" +
                "En 5 noches caerá la GRAN HELADA (-20 °C).\n\n" +
                "• DE DÍA: Cosecha recursos con [E] y arrea alpacas al cobertizo.\n" +
                "• AYNI: Toca el pututo [Q] para convocar faenas y construir Muros Trombe.\n" +
                "• SALUD: Atiende enfermos en la posta y mantén el fogón encendido.\n\n" +
                "Consulta saberes y diagnósticos en el Cuaderno [J] o Sistemas [TAB].";

            Label(p, body, 22, TextAnchor.UpperLeft,
                  new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(45f, 75f), new Vector2(-45f, -155f));

            Label(p, "Presiona [ESPACIO] o [ENTER] para comenzar la faena", 24, TextAnchor.LowerCenter,
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
                          new Vector2(-530f, -340f), new Vector2(530f, 340f), PanelBg);

            Label(p, "REPORTE DEL AMANECER", 34, TextAnchor.UpperCenter,
                  new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -58f), new Vector2(0f, -14f),
                  new Color32(240, 205, 110, 255));

            // Columna Izquierda: ODS 3 Salud, ODS 13 Ganado y eventos
            _reportText = Label(p, "", 18, TextAnchor.UpperLeft,
                                new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(36f, 75f), new Vector2(-15f, -68f));

            // Columna Derecha: Estado Bioclimático y Consejos del Yachachiq
            _reportRightText = Label(p, "", 18, TextAnchor.UpperLeft,
                                     new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(15f, 75f), new Vector2(-36f, -68f));

            // Pie de página dedicado sin colisiones
            var footer = Panel(p, new Vector2(0.05f, 0f), new Vector2(0.95f, 0f), new Vector2(0f, 14f), new Vector2(0f, 62f),
                               new Color32(28, 55, 42, 240));
            Label(footer, "Presiona [ESPACIO] o haz clic para comenzar la faena comunal", 21, TextAnchor.MiddleCenter,
                  Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color32(140, 230, 170, 255));

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

        // ---------------- Menu de Sistemas y Opciones en Partida ----------------
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
            dimg.color = new Color(0.02f, 0.03f, 0.07f, 0.85f);
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            var close = dim.AddComponent<Button>();
            close.transition = Selectable.Transition.None;
            close.onClick.AddListener(ToggleInGameOptions);

            var p = Panel(root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                          new Vector2(-540f, -345f), new Vector2(540f, 345f), PanelBg);

            Label(p, "SISTEMAS Y OPCIONES COMUNALES", 30, TextAnchor.UpperCenter,
                  new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f), new Vector2(0f, -12f),
                  new Color32(240, 205, 110, 255));

            // Fila superior de pestañas interactivas (1..4)
            string[] tabNames = { "[1] Recursos & HUD", "[2] Clima y Pastoreo", "[3] Mejoras Bioclimáticas", "[4] Ajustes y Audio" };
            float startX = 35f;
            float tabWidth = 240f;
            for (int i = 0; i < 4; i++)
            {
                int tabIdx = i;
                var tabBtn = new GameObject("TabBtn_" + i);
                tabBtn.transform.SetParent(p, false);
                var trt = tabBtn.AddComponent<RectTransform>();
                trt.anchorMin = new Vector2(0f, 1f);
                trt.anchorMax = new Vector2(0f, 1f);
                trt.offsetMin = new Vector2(startX + (i * 255f), -96f);
                trt.offsetMax = new Vector2(startX + (i * 255f) + tabWidth, -56f);
                var timg = tabBtn.AddComponent<Image>();
                timg.color = (i == 0) ? new Color32(185, 135, 40, 255) : new Color32(50, 55, 75, 255);
                _tabBtnImages[i] = timg;
                var btn = tabBtn.AddComponent<Button>();
                btn.targetGraphic = timg;
                btn.onClick.AddListener(() => SwitchTab(tabIdx));

                var lbl = Label(tabBtn.transform, tabNames[i], 16, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextCream);
                lbl.raycastTarget = false;
            }

            // Pestaña 0: Recursos y HUD (media_1791507253316.png)
            _tabRecursosObj = CreateTabContent(p, "TabRecursos", GameArt.MenuRecursos);
            _tabRecursosInfo = Label(_tabRecursosObj.transform, "", 18, TextAnchor.LowerCenter,
                                     new Vector2(0.05f, 0f), new Vector2(0.95f, 0.20f), Vector2.zero, Vector2.zero,
                                     new Color32(245, 225, 175, 255));

            // Pestaña 1: Sistemas Secundarios / Clima y Pastoreo (media_1791507265771.png)
            _tabSecundariosObj = CreateTabContent(p, "TabSecundarios", GameArt.MenuSecundarios);
            _tabSecundariosInfo = Label(_tabSecundariosObj.transform, "", 18, TextAnchor.LowerCenter,
                                        new Vector2(0.05f, 0f), new Vector2(0.95f, 0.20f), Vector2.zero, Vector2.zero,
                                        new Color32(160, 215, 255, 255));

            // Pestaña 2: Sistema de Mejoras / Progresión (media_1791507275896.png)
            _tabProgresionObj = CreateTabContent(p, "TabProgresion", GameArt.MenuProgresion);
            _tabProgresionInfo = Label(_tabProgresionObj.transform, "", 18, TextAnchor.LowerCenter,
                                       new Vector2(0.05f, 0f), new Vector2(0.95f, 0.20f), Vector2.zero, Vector2.zero,
                                       new Color32(230, 240, 190, 255));

            // Pestaña 3: Ajustes y Audio
            _tabAjustesObj = new GameObject("TabAjustes");
            _tabAjustesObj.transform.SetParent(p, false);
            var art = _tabAjustesObj.AddComponent<RectTransform>();
            art.anchorMin = new Vector2(0f, 0f); art.anchorMax = new Vector2(1f, 1f);
            art.offsetMin = new Vector2(40f, 75f); art.offsetMax = new Vector2(-40f, -110f);

            // Music Row
            _optMusicText = Label(_tabAjustesObj.transform, "Música: 80%", 22, TextAnchor.MiddleLeft,
                                  new Vector2(0.08f, 0.72f), new Vector2(0.40f, 0.82f), Vector2.zero, Vector2.zero);
            MakeBtn(_tabAjustesObj.transform, "-", () => AdjustMusic(-0.1f), new Vector2(0.40f, 0.73f), new Vector2(0.46f, 0.81f));
            MakeBtn(_tabAjustesObj.transform, "+", () => AdjustMusic(+0.1f), new Vector2(0.48f, 0.73f), new Vector2(0.54f, 0.81f));

            // SFX Row (Viento y efectos)
            _optSfxText = Label(_tabAjustesObj.transform, "Efectos SFX / Viento: 80%", 22, TextAnchor.MiddleLeft,
                                new Vector2(0.08f, 0.58f), new Vector2(0.40f, 0.68f), Vector2.zero, Vector2.zero);
            MakeBtn(_tabAjustesObj.transform, "-", () => AdjustSfx(-0.1f), new Vector2(0.40f, 0.59f), new Vector2(0.46f, 0.67f));
            MakeBtn(_tabAjustesObj.transform, "+", () => AdjustSfx(+0.1f), new Vector2(0.48f, 0.59f), new Vector2(0.54f, 0.67f));

            // Sound Test & Screen mode
            MakeBtn(_tabAjustesObj.transform, "Probar Pututo", () => Audio.AudioDirector.Instance?.Pututo(),
                    new Vector2(0.58f, 0.73f), new Vector2(0.92f, 0.81f), new Color32(180, 110, 40, 255));
            _optFsText = Label(_tabAjustesObj.transform, Screen.fullScreen ? "Pantalla: Completa" : "Pantalla: Ventana", 18, TextAnchor.MiddleCenter,
                               new Vector2(0.58f, 0.58f), new Vector2(0.92f, 0.68f), Vector2.zero, Vector2.zero);
            MakeBtn(_tabAjustesObj.transform, "Cambiar Modo", () => {
                Screen.fullScreen = !Screen.fullScreen;
                if (_optFsText != null) _optFsText.text = Screen.fullScreen ? "Pantalla: Completa" : "Pantalla: Ventana";
            }, new Vector2(0.58f, 0.46f), new Vector2(0.92f, 0.55f), new Color32(50, 80, 120, 255));

            // Controles
            string info = "[A][D] Moverse   |   [Shift] Correr   |   [Espacio] Saltar   |   [E] Interactuar / Construir\n" +
                          "[Q] Pututo (convocar Ayni)   |   [T] Avanzar noche   |   [Tab / 1..4] Cambiar pestañas";
            Label(_tabAjustesObj.transform, info, 18, TextAnchor.MiddleCenter,
                  new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.38f), Vector2.zero, Vector2.zero,
                  new Color32(200, 215, 235, 255));

            // Pie inferior con botón CERRAR MENÚ (idéntico al diseño oficial)
            Label(p, "Atajos: [1..4] Pestañas   |   [ESC] o [TAB] Salir", 17, TextAnchor.MiddleLeft,
                  new Vector2(0f, 0f), new Vector2(0.6f, 0f), new Vector2(40f, 15f), new Vector2(0f, 55f),
                  new Color32(180, 195, 215, 255));

            MakeBtn(p, "CERRAR MENÚ", ToggleInGameOptions,
                    new Vector2(1f, 0f), new Vector2(1f, 0f), new Color32(180, 75, 45, 255));
            var closeBtnRt = p.Find("Btn_CERRAR MENÚ")?.GetComponent<RectTransform>();
            if (closeBtnRt != null)
            {
                closeBtnRt.offsetMin = new Vector2(-220f, 15f);
                closeBtnRt.offsetMax = new Vector2(-40f, 58f);
            }

            _inGameOptionsPanel = root;
            _inGameOptionsPanel.SetActive(false);
            SwitchTab(0);
        }

        private GameObject CreateTabContent(Transform parent, string name, Sprite menuSprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(40f, 65f); rt.offsetMax = new Vector2(-40f, -105f);

            var imgObj = new GameObject("Artwork");
            imgObj.transform.SetParent(go.transform, false);
            var img = imgObj.AddComponent<Image>();
            img.sprite = menuSprite;
            img.preserveAspect = true;
            var irt = imgObj.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.05f, 0.12f);
            irt.anchorMax = new Vector2(0.95f, 0.98f);
            irt.offsetMin = Vector2.zero; irt.offsetMax = Vector2.zero;

            return go;
        }

        public void SwitchTab(int index)
        {
            _activeTab = Mathf.Clamp(index, 0, 3);
            if (_tabRecursosObj != null) _tabRecursosObj.SetActive(_activeTab == 0);
            if (_tabSecundariosObj != null) _tabSecundariosObj.SetActive(_activeTab == 1);
            if (_tabProgresionObj != null) _tabProgresionObj.SetActive(_activeTab == 2);
            if (_tabAjustesObj != null) _tabAjustesObj.SetActive(_activeTab == 3);

            for (int i = 0; i < 4; i++)
            {
                if (_tabBtnImages[i] != null)
                    _tabBtnImages[i].color = (i == _activeTab) ? new Color32(185, 135, 40, 255) : new Color32(50, 55, 75, 255);
            }
            RefreshTabContent();
        }

        private void RefreshTabContent()
        {
            var rm = ResourceManager.Instance;
            var ttm = TimeTemperatureManager.Instance;
            var hsm = HealthShelterManager.Instance;

            if (_tabRecursosInfo != null && rm != null)
            {
                int ronderos = VillagerTaskSystem.Instance != null ? VillagerTaskSystem.Instance.AvailableRonderos : 6;
                _tabRecursosInfo.text = $"STOCK ACTUAL:  Ichu: {rm.Get(ResourceType.Ichu)}   |   Leña: {rm.Get(ResourceType.Firewood)}   |   Muña/Hierbas: {rm.Get(ResourceType.Herbs)}   |   Fuerza laboral: {ronderos} ronderos\nBodega de Materiales: Resguardada y seca para abastecer el Ayni de la jornada.";
            }

            if (_tabSecundariosInfo != null && ttm != null)
            {
                string fase = ttm.Phase == DayPhase.Day ? "Fase diurna de trabajo comunal" : "Fase nocturna: Ola polar activa";
                _tabSecundariosInfo.text = $"TERMÓMETRO AMBIENTAL: {ttm.CurrentTemperature:+0.0;-0.0} °C   (Mínima nocturna: {ttm.TonightLowTemperature:0} °C)\n{fase}.  Rebaño protegido: {(hsm != null ? hsm.AlpacasSheltered : 0)} alpacas resguardadas en el cobertizo.";
            }

            if (_tabProgresionInfo != null)
            {
                int trombe = CountTrombeHouses();
                _tabProgresionInfo.text = $"PROGRESIÓN DE VIVIENDAS:  {trombe}/2 Casas acondicionadas con Muro Trombe (+8 °C interior).\nCuaderno Bioclimático activo. La comunidad gana resistencia térmica con cada módulo de adobe e ichu sellado.";
            }
        }

        private int CountTrombeHouses()
        {
            int count = 0;
            var buildings = FindObjectsOfType<ThermalBuilding>();
            foreach (var b in buildings)
            {
                if (b.Kind == BuildingKind.House && b.Level >= 1) count++;
            }
            return count;
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
                if (_optSfxText != null) _optSfxText.text = $"Efectos SFX / Viento: {Mathf.RoundToInt(ad.sfxVolume * 100)}%";
            }
            if (_optFsText != null)
                _optFsText.text = Screen.fullScreen ? "Pantalla: Completa" : "Pantalla: Ventana";
        }

        // ---------------- Cuaderno del Yachachiq ----------------
        private void BuildYachachiqNotebook()
        {
            var root = new GameObject("NotebookPanel");
            root.transform.SetParent(_canvas.transform, false);
            var rrt = root.AddComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;

            var dim = new GameObject("Dim");
            dim.transform.SetParent(root.transform, false);
            var dimg = dim.AddComponent<Image>();
            dimg.sprite = PixelArtFactory.Solid(new Color32(255, 255, 255, 255), 8, 8);
            dimg.color = new Color(0.04f, 0.03f, 0.06f, 0.88f);
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            var close = dim.AddComponent<Button>();
            close.transition = Selectable.Transition.None;
            close.onClick.AddListener(ToggleYachachiqNotebook);

            // Marco del Cuaderno (estilo cuero y pergamino andino)
            var p = Panel(root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                          new Vector2(-540f, -345f), new Vector2(540f, 345f), new Color32(35, 26, 20, 250));

            // Encabezado con el icono oficial
            var iconGo = new GameObject("BookIcon");
            iconGo.transform.SetParent(p, false);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = GameArt.YachachiqNotebook;
            iconImg.preserveAspect = true;
            var icRt = iconGo.GetComponent<RectTransform>();
            icRt.anchorMin = new Vector2(0f, 1f); icRt.anchorMax = new Vector2(0f, 1f);
            icRt.offsetMin = new Vector2(30f, -80f); icRt.offsetMax = new Vector2(100f, -10f);

            Label(p, "CUADERNO DEL YACHACHIQ", 30, TextAnchor.MiddleLeft,
                  new Vector2(0f, 1f), new Vector2(0.85f, 1f), new Vector2(115f, -48f), new Vector2(0f, -14f),
                  new Color32(245, 215, 120, 255));
            Label(p, "Saberes Bioclimáticos & Guía de Supervivencia a la Helada Andina (-20°C)", 16, TextAnchor.MiddleLeft,
                  new Vector2(0f, 1f), new Vector2(0.85f, 1f), new Vector2(115f, -78f), new Vector2(0f, -50f),
                  new Color32(205, 220, 235, 255));

            // Pestañas superiores (1..4)
            string[] tabNames = { "[1] Muros Trombe", "[2] Ganado y Cobertizo", "[3] Salud y Medicina", "[4] Ayni y Pututo" };
            float startX = 30f;
            float tabWidth = 245f;
            for (int i = 0; i < 4; i++)
            {
                int pageIdx = i;
                var tabBtn = new GameObject("NbTabBtn_" + i);
                tabBtn.transform.SetParent(p, false);
                var trt = tabBtn.AddComponent<RectTransform>();
                trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(0f, 1f);
                trt.offsetMin = new Vector2(startX + (i * 258f), -126f);
                trt.offsetMax = new Vector2(startX + (i * 258f) + tabWidth, -88f);
                var timg = tabBtn.AddComponent<Image>();
                timg.color = (i == 0) ? new Color32(185, 135, 40, 255) : new Color32(55, 42, 32, 255);
                _notebookTabBtns[i] = timg;
                var btn = tabBtn.AddComponent<Button>();
                btn.targetGraphic = timg;
                btn.onClick.AddListener(() => SwitchNotebookPage(pageIdx));

                var lbl = Label(tabBtn.transform, tabNames[i], 16, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextCream);
                lbl.raycastTarget = false;
            }

            // Columna Izquierda: Pergamino de Sabiduría y Teoría
            var leftPage = Panel(p, new Vector2(0f, 0f), new Vector2(0.5f, 1f),
                                 new Vector2(30f, 65f), new Vector2(-15f, -140f), new Color32(236, 226, 202, 255));
            _nbTitleText = Label(leftPage, "", 21, TextAnchor.UpperLeft,
                                 new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(25f, -42f), new Vector2(-25f, -10f),
                                 new Color32(110, 45, 15, 255));
            _nbTheoryText = Label(leftPage, "", 16, TextAnchor.UpperLeft,
                                  new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(25f, 20f), new Vector2(-25f, -48f),
                                  new Color32(40, 32, 22, 255));

            // Columna Derecha: Pergamino de Diagnóstico en Vivo & Acción
            var rightPage = Panel(p, new Vector2(0.5f, 0f), new Vector2(1f, 1f),
                                  new Vector2(15f, 65f), new Vector2(-30f, -140f), new Color32(244, 236, 216, 255));
            Label(rightPage, "DIAGNÓSTICO EN VIVO DE LA COMUNIDAD", 19, TextAnchor.UpperLeft,
                  new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(25f, -42f), new Vector2(-25f, -10f),
                  new Color32(25, 65, 100, 255));
            _nbStatsText = Label(rightPage, "", 16, TextAnchor.UpperLeft,
                                 new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(25f, 65f), new Vector2(-25f, -48f),
                                 new Color32(35, 30, 25, 255));

            // Botón interactivo de acción comunal
            var actBtnGo = new GameObject("Btn_AccionComunal");
            actBtnGo.transform.SetParent(rightPage, false);
            var actRt = actBtnGo.AddComponent<RectTransform>();
            actRt.anchorMin = new Vector2(0.5f, 0f); actRt.anchorMax = new Vector2(0.5f, 0f);
            actRt.offsetMin = new Vector2(-190f, 12f); actRt.offsetMax = new Vector2(190f, 52f);
            var actImg = actBtnGo.AddComponent<Image>();
            actImg.color = new Color32(175, 75, 40, 255);
            var actBtn = actBtnGo.AddComponent<Button>();
            actBtn.targetGraphic = actImg;
            actBtn.onClick.AddListener(OnNotebookActionButton);
            _nbActionBtnLabel = Label(actBtnGo.transform, "EJECUTAR ACCIÓN", 16, TextAnchor.MiddleCenter,
                                      Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextCream);
            _nbActionBtnLabel.raycastTarget = false;

            // Pie inferior con paginador y botón cerrar
            MakeBtn(p, "< ANTERIOR", () => SwitchNotebookPage(_nbActivePage - 1),
                    new Vector2(0f, 0f), new Vector2(0f, 0f), new Color32(70, 55, 45, 255));
            var prevRt = p.Find("Btn_< ANTERIOR")?.GetComponent<RectTransform>();
            if (prevRt != null) { prevRt.offsetMin = new Vector2(30f, 15f); prevRt.offsetMax = new Vector2(160f, 55f); }

            _nbPageNumText = Label(p, "Página 1 de 4", 17, TextAnchor.MiddleCenter,
                                   new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(170f, 15f), new Vector2(300f, 55f),
                                   new Color32(235, 220, 190, 255));

            MakeBtn(p, "SIGUIENTE >", () => SwitchNotebookPage(_nbActivePage + 1),
                    new Vector2(0f, 0f), new Vector2(0f, 0f), new Color32(70, 55, 45, 255));
            var nextRt = p.Find("Btn_SIGUIENTE >")?.GetComponent<RectTransform>();
            if (nextRt != null) { nextRt.offsetMin = new Vector2(310f, 15f); nextRt.offsetMax = new Vector2(440f, 55f); }

            Label(p, "Atajos: [J] o [ESC] Cerrar  |  [1..4] Pestañas  |  [A / D] Pasar página", 16, TextAnchor.MiddleLeft,
                  new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(460f, 15f), new Vector2(-235f, 55f),
                  new Color32(185, 195, 215, 255));

            MakeBtn(p, "CERRAR CUADERNO", ToggleYachachiqNotebook,
                    new Vector2(1f, 0f), new Vector2(1f, 0f), new Color32(175, 65, 45, 255));
            var nbCloseRt = p.Find("Btn_CERRAR CUADERNO")?.GetComponent<RectTransform>();
            if (nbCloseRt != null)
            {
                nbCloseRt.offsetMin = new Vector2(-225f, 15f);
                nbCloseRt.offsetMax = new Vector2(-30f, 55f);
            }

            _notebookPanel = root;
            _notebookPanel.SetActive(false);
            SwitchNotebookPage(0);
        }

        public void SwitchNotebookPage(int index)
        {
            if (index < 0) index = 3;
            if (index > 3) index = 0;
            _nbActivePage = index;

            for (int i = 0; i < 4; i++)
            {
                if (_notebookTabBtns[i] != null)
                    _notebookTabBtns[i].color = (i == _nbActivePage) ? new Color32(185, 135, 40, 255) : new Color32(55, 42, 32, 255);
            }

            if (_nbPageNumText != null)
                _nbPageNumText.text = $"Página {_nbActivePage + 1} de 4";

            RefreshNotebookContent();
        }

        private void RefreshNotebookContent()
        {
            var hsm = HealthShelterManager.Instance;
            var ttm = TimeTemperatureManager.Instance;
            var rm = ResourceManager.Instance;

            switch (_nbActivePage)
            {
                case 0:
                    if (_nbTitleText != null) _nbTitleText.text = "MUROS TROMBE: TECNOLOGÍA SOLAR (ODS 13)";
                    if (_nbTheoryText != null)
                    {
                        _nbTheoryText.text =
                            "• Principio Físico:\n" +
                            "  Aprovecha la radiación solar diurna a 4,200 m s.n.m. Un muro de adobe oscuro tras vidrio absorbe calor masivo.\n\n" +
                            "• Termosifón Pasivo:\n" +
                            "  De noche, el aire frío entra por aberturas inferiores, se calienta y reingresa arriba por convección natural (+8 a +12 °C interior).\n\n" +
                            "• Sellado con Ichu:\n" +
                            "  Elimina puentes térmicos en aleros y techos de paja andina evitando filtraciones del viento helado.";
                    }
                    if (_nbStatsText != null)
                    {
                        int totalCasas = 0, casasTrombe = 0, casasSelladas = 0;
                        if (hsm != null && hsm.Buildings != null)
                        {
                            foreach (var b in hsm.Buildings)
                            {
                                if (b != null && b.Kind == BuildingKind.House)
                                {
                                    totalCasas++;
                                    if (b.Level >= 2) casasTrombe++;
                                    else if (b.Level >= 1) casasSelladas++;
                                }
                            }
                        }
                        if (totalCasas == 0) totalCasas = 4;
                        float ganancia = (casasTrombe * 10f) + (casasSelladas * 5f);
                        _nbStatsText.text =
                            $"• Viviendas con Muro Trombe N3:  {casasTrombe} / {totalCasas}\n" +
                            $"• Viviendas selladas con Ichu N2:  {casasSelladas} / {totalCasas}\n" +
                            $"• Viviendas básicas de adobe N1:   {Mathf.Max(0, totalCasas - casasTrombe - casasSelladas)}\n" +
                            $"• Confort térmico acumulado:      +{ganancia:0.0} °C ganados\n" +
                            $"• Ichu en almacén comunal:       {(rm != null ? rm.Get(ResourceType.Ichu) : 0)} unidades\n" +
                            $"• Vidrio térmico en almacén:     {(rm != null ? rm.Get(ResourceType.Stone) : 0)} unidades";
                    }
                    if (_nbActionBtnLabel != null) _nbActionBtnLabel.text = "VER PLANO TROMBE [TAB]";
                    break;

                case 1:
                    if (_nbTitleText != null) _nbTitleText.text = "PROTECCIÓN DEL REBAÑO Y COBERTIZOS (ODS 13)";
                    if (_nbTheoryText != null)
                    {
                        _nbTheoryText.text =
                            "• Mortandad por Helada:\n" +
                            "  A -20 °C las crías (tuis) y alpacas débiles sufren hipotermia letal si quedan a la intemperie.\n\n" +
                            "• Cobertizos Rústicos Comunitarios:\n" +
                            "  Pircas de piedra y techo de ichu cortan el 70% del viento helado, preservando el calor corporal del rebaño.\n\n" +
                            "• Recolección de Bosta:\n" +
                            "  El estiércol seco es el combustible térmico tradicional más eficiente de la puna para alimentar el fogón.";
                    }
                    if (_nbStatsText != null)
                    {
                        int total = hsm != null ? hsm.AlpacaCount : 12;
                        int sheltered = hsm != null ? hsm.AlpacasSheltered : 0;
                        int outside = Mathf.Max(0, total - sheltered);
                        string estado = outside == 0 ? "¡REBAÑO 100% PROTEGIDO!" : "¡ALERTA: Alpacas a la intemperie!";
                        _nbStatsText.text =
                            $"• Alpacas en Cobertizo:    {sheltered} / {total}\n" +
                            $"• Alpacas expuestas al frío: {outside}\n" +
                            $"• Bajas acumuladas de ganado: {(hsm != null ? hsm.DeadAlpacas : 0)}\n" +
                            $"• Diagnóstico del Yachachiq:  {estado}\n\n" +
                            $"Consejo: Usa el silbido [F] o toca el pututo [Q] para arrear al ganado antes de las 18:00 h.";
                    }
                    if (_nbActionBtnLabel != null) _nbActionBtnLabel.text = "SILBAR AL REBAÑO [F]";
                    break;

                case 2:
                    if (_nbTitleText != null) _nbTitleText.text = "MEDICINA ANDINA Y SALUD COMUNAL (ODS 3)";
                    if (_nbTheoryText != null)
                    {
                        _nbTheoryText.text =
                            "• Prevención de Neumonía e IRAS:\n" +
                            "  Las bajas temperaturas inflaman las vías respiratorias. La atención precoz salva vidas de niños y ancianos.\n\n" +
                            "• Muña (Minthostachys mollis):\n" +
                            "  Infusión caliente con aceites esenciales broncodilatadores y antisépticos.\n\n" +
                            "• Wira-Wira (Achyrocline alata):\n" +
                            "  Medicina ancestral de puna con mucílagos que desinflaman la garganta y calman la tos crónica.\n\n" +
                            "• Fogón Comunal:\n" +
                            "  Espacio de calor donde los comuneros comparten caldo y elevan su moral frente a la noche gélida.";
                    }
                    if (_nbStatsText != null)
                    {
                        int pop = hsm != null ? hsm.HumanPopulation : 10;
                        int sick = hsm != null ? hsm.SickCount : 0;
                        int sanos = Mathf.Max(0, pop - sick);
                        int herbs = rm != null ? rm.Get(ResourceType.Herbs) : 0;
                        var fire = Campfire.Instance;
                        string fogonSt = (fire != null && fire.IsLit) ? "ENCENDIDO (+Calor)" : "APAGADO (Peligro)";
                        _nbStatsText.text =
                            $"• Población Comunitaria:   {pop} personas\n" +
                            $"• Comuneros Sanos:         {sanos}\n" +
                            $"• Comuneros con Tos/IRAS:   {sick}\n" +
                            $"• Hierbas en la Posta:     {herbs} paquetes\n" +
                            $"• Estado del Fogón:        {fogonSt}\n" +
                            $"• Moral Comunitaria:       {(hsm != null ? hsm.Morale : 100):0}%\n\n" +
                            $"Atiende a Don Mariano y a los abuelos en la Posta de Doña Tomasa con mate caliente.";
                    }
                    if (_nbActionBtnLabel != null) _nbActionBtnLabel.text = "PREPARAR MATE DE MUÑA";
                    break;

                case 3:
                    if (_nbTitleText != null) _nbTitleText.text = "ORGANIZACIÓN ANCESTRAL: AYNI Y EL PUTUTO";
                    if (_nbTheoryText != null)
                    {
                        _nbTheoryText.text =
                            "• Ayni (Reciprocidad Andina):\n" +
                            "  'Hoy por ti, mañana por mí'. La resiliencia ante el cambio climático no es individual, es colectiva.\n\n" +
                            "• El Pututo (Caracola Marina):\n" +
                            "  Instrumento de convocatoria comunal. Al hacerlo sonar, la Ronda Campesina acude de inmediato a faenar.\n\n" +
                            "• Ciencia Ancestral y SENAMHI:\n" +
                            "  Combinar la lectura de las heladas y el cielo andino con las alertas meteorológicas oficiales para salvar vidas.";
                    }
                    if (_nbStatsText != null)
                    {
                        int ronderos = VillagerTaskSystem.Instance != null ? VillagerTaskSystem.Instance.AvailableRonderos : 6;
                        string hora = ttm != null ? ttm.ClockString() : "06:00";
                        float low = ttm != null ? ttm.TonightLowTemperature : -12f;
                        _nbStatsText.text =
                            $"• Ronderos en Guardia:      {ronderos} comuneros\n" +
                            $"• Día de Supervivencia:    {(ttm != null ? ttm.CurrentDay : 1)} de 5\n" +
                            $"• Hora Actual:             {hora} (Fase: {(ttm != null && ttm.Phase == DayPhase.Day ? "Día" : "Noche")})\n" +
                            $"• Pronóstico de Helada:     {low:0.0} °C para esta noche\n\n" +
                            $"¡Toca el pututo para organizar una faena comunal de sellado de viviendas y arreo!";
                    }
                    if (_nbActionBtnLabel != null) _nbActionBtnLabel.text = "TOCAR PUTUTO COMUNAL [Q]";
                    break;
            }
        }

        private void OnNotebookActionButton()
        {
            var p2d = Player.PlayerController2D.Instance;
            switch (_nbActivePage)
            {
                case 0: // Muros Trombe
                    ToggleYachachiqNotebook();
                    ToggleInGameOptions();
                    SwitchTab(2); // Pestaña Mejoras bioclimáticas
                    break;
                case 1: // Alpacas
                    if (p2d != null) p2d.WhistleHerd();
                    RefreshNotebookContent();
                    break;
                case 2: // Medicina
                    var rm = ResourceManager.Instance;
                    if (rm != null && rm.Get(ResourceType.Herbs) > 0)
                    {
                        rm.Spend(ResourceType.Herbs, 1);
                        HealthShelterManager.Instance?.AddMorale(12f);
                        Log("Inti y Doña Tomasa preparan infusión caliente de Muña y Wira-wira. ¡Comunidad aliviada!");
                        Audio.AudioDirector.Instance?.Pututo();
                    }
                    else
                    {
                        Log("No hay hierbas suficientes en la posta para preparar mate de muña.");
                    }
                    RefreshNotebookContent();
                    break;
                case 3: // Ayni y Pututo
                    if (p2d != null) p2d.BlowPututo();
                    RefreshNotebookContent();
                    break;
            }
        }

        public void ToggleInGameOptions()
        {
            if (_introPanel != null && _introPanel.activeSelf) return;
            if (_reportPanel != null && _reportPanel.activeSelf) return;
            if (_endPanel != null && _endPanel.activeSelf) return;
            if (_notebookPanel != null && _notebookPanel.activeSelf) _notebookPanel.SetActive(false);

            if (_inGameOptionsPanel != null)
            {
                bool opening = !_inGameOptionsPanel.activeSelf;
                _inGameOptionsPanel.SetActive(opening);
                Time.timeScale = opening ? 0f : 1f;
                if (opening)
                {
                    RefreshInGameOptionsUI();
                    RefreshTabContent();
                }
            }
        }

        public void ToggleYachachiqNotebook()
        {
            if (_introPanel != null && _introPanel.activeSelf) return;
            if (_reportPanel != null && _reportPanel.activeSelf) return;
            if (_endPanel != null && _endPanel.activeSelf) return;
            if (_inGameOptionsPanel != null && _inGameOptionsPanel.activeSelf) _inGameOptionsPanel.SetActive(false);

            if (_notebookPanel != null)
            {
                bool opening = !_notebookPanel.activeSelf;
                _notebookPanel.SetActive(opening);
                Time.timeScale = opening ? 0f : 1f;
                if (opening)
                {
                    SwitchNotebookPage(_nbActivePage);
                }
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
                _odsHealthText.text = $"Salud: {sanos} sanos ({hsm.SickCount} enfermos)";
                _healthFill.fillAmount = hsm.InitialHumans > 0 ? (float)sanos / hsm.InitialHumans : 1f;

                _odsHerdText.text = $"Alpacas: {hsm.AlpacaCount}/{hsm.InitialAlpacas} ({hsm.AlpacasSheltered} a salvo)";
                _herdFill.fillAmount = hsm.HerdRatio;
                _herdFill.color = hsm.HerdRatio < 0.6f ? new Color32(240, 120, 100, 255) : new Color32(240, 220, 190, 255);

                _odsMoraleText.text = $"Moral: {hsm.Morale:0}%";
                _moraleFill.fillAmount = hsm.Morale / 100f;
            }

            var fire = Campfire.Instance;
            if (fire != null)
            {
                _fireText.text = fire.IsLit ? $"Fogón: {fire.FuelRatio * 100f:0}%" : "¡FOGÓN APAGADO!";
                _fireText.color = fire.IsLit ? TextCream : new Color32(255, 120, 100, 255);
                _fireFill.fillAmount = fire.FuelRatio;
            }

            if (VillagerTaskSystem.Instance != null)
                _ronderosText.text = $"{VillagerTaskSystem.Instance.AvailableRonderos}";

            // Prompt de interaccion estable y fijo (sin temblor ni vibracion)
            var pi = FindPlayerInteraction();
            if (pi != null && pi.Current != null)
            {
                _promptBg.enabled = true;
                _promptText.text = pi.Current.CanInteract()
                    ? $"[E]  {pi.Current.GetPrompt()}"
                    : pi.Current.GetPrompt();
                _promptText.color = pi.Current.CanInteract() ? TextCream : new Color32(170, 175, 190, 255);
                _promptBg.transform.localScale = Vector3.one;
            }
            else
            {
                _promptBg.enabled = false;
                _promptText.text = "";
                _promptBg.transform.localScale = Vector3.one;
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

            // Cuaderno del Yachachiq [J]
            if (Input.GetKeyDown(KeyCode.J) && gm != null && gm.IsPlaying)
            {
                ToggleYachachiqNotebook();
            }

            // Pausa y opciones
            if ((Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab)) && gm != null && gm.IsPlaying)
            {
                ToggleInGameOptions();
            }
        }

        private void HandlePanels()
        {
            var gm = GameManager.Instance;
            var ttm = TimeTemperatureManager.Instance;

            if (_notebookPanel != null && _notebookPanel.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.J))
                {
                    ToggleYachachiqNotebook();
                }
                else if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) SwitchNotebookPage(0);
                else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) SwitchNotebookPage(1);
                else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) SwitchNotebookPage(2);
                else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) SwitchNotebookPage(3);
                else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) SwitchNotebookPage(_nbActivePage - 1);
                else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) SwitchNotebookPage(_nbActivePage + 1);
                return;
            }

            if (_inGameOptionsPanel != null && _inGameOptionsPanel.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
                {
                    ToggleInGameOptions();
                }
                else if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) SwitchTab(0);
                else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) SwitchTab(1);
                else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) SwitchTab(2);
                else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) SwitchTab(3);
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
            if (_logText != null) _logText.text = "> " + message;
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
            var sbLeft = new StringBuilder();
            sbLeft.AppendLine($"Amanece el DIA {rep.Day + 1}.  La noche marco {rep.LowTemperature:0} C.");
            sbLeft.AppendLine();
            sbLeft.AppendLine("ODS 3 - SALUD COMUNITARIA:");
            sbLeft.AppendLine($"   • Nuevos enfermos (IRA): {rep.NewSick}");
            sbLeft.AppendLine($"   • Recuperados con muña: {rep.Recovered}");
            sbLeft.AppendLine($"   • Fallecimientos: {(rep.HumanDeaths == 0 ? "0 (¡Comunidad a salvo!)" : rep.HumanDeaths.ToString())}");
            sbLeft.AppendLine();
            sbLeft.AppendLine("ODS 13 - GANADO ALPAQUERO:");
            sbLeft.AppendLine($"   • En cobertizo termico: {rep.AlpacasSheltered}");
            sbLeft.AppendLine($"   • A la intemperie: {rep.AlpacasOutside}");
            sbLeft.AppendLine($"   • Bajas por helada: {rep.AlpacaDeaths}");
            sbLeft.AppendLine();
            foreach (var l in rep.Lines) sbLeft.AppendLine("• " + l);

            _reportText.text = sbLeft.ToString();

            var sbRight = new StringBuilder();
            sbRight.AppendLine("ESTADO BIOCLIMATICO DE VIVIENDAS:");
            var hsm = HealthShelterManager.Instance;
            if (hsm != null)
            {
                sbRight.Append(hsm.BuildingSummary());
            }
            sbRight.AppendLine();
            sbRight.AppendLine("SABIDURIA DEL YACHACHIQ (Ayni):");
            sbRight.AppendLine("• Construye Muros Trombe para elevar la T_interior.");
            sbRight.AppendLine("• Arrea alpacas antes de las 18:00 tocando el pututo.");
            sbRight.AppendLine("• Prepara mates calientes en la posta para enfermos.");

            if (_reportRightText != null) _reportRightText.text = sbRight.ToString();
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
