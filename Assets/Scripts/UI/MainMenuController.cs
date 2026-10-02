using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using FrioAndino.Art;
using FrioAndino.Audio;

namespace FrioAndino.UI
{
    /// <summary>
    /// Menu principal montado sobre la pantalla pintada del proyecto.
    /// Las zonas interactivas se anclan en proporciones de la imagen, de modo que
    /// [Nueva Partida], [Opciones] y [Creditos] siguen al arte en cualquier resolucion.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        public static MainMenuController Instance { get; private set; }

        // Coordenadas de la imagen original (576 x 320) de cada zona clicable
        private const float MW = 576f, MH = 320f;

        private Canvas _canvas;
        private RectTransform _art;
        private Font _font;
        private Image _startGlow;
        private GameObject _optionsPanel, _creditsPanel, _howToPlayPanel;
        private bool _started;

        private static readonly Color32 PanelBg = new Color32(24, 26, 38, 238);
        private static readonly Color32 Gold = new Color32(240, 205, 110, 255);
        private static readonly Color32 Cream = new Color32(244, 238, 224, 255);

        private void Awake()
        {
            Instance = this;
            _font = HUDController.GetGameFont();
            Build();
        }

        // =====================================================================
        private void Build()
        {
            var go = new GameObject("MenuCanvas");
            go.transform.SetParent(transform, false);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 300;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();

            // Fondo negro por si la imagen no llega a los bordes
            var back = new GameObject("Black");
            back.transform.SetParent(_canvas.transform, false);
            var bi = back.AddComponent<Image>();
            bi.color = new Color(0.02f, 0.03f, 0.06f, 1f);
            Stretch(back.GetComponent<RectTransform>());

            // ---- Arte del menu, recortado tipo "cover" ----
            var artGo = new GameObject("MenuArt");
            artGo.transform.SetParent(_canvas.transform, false);
            var img = artGo.AddComponent<Image>();
            img.sprite = GameArt.Menu;
            img.preserveAspect = false;
            img.raycastTarget = true;
            _art = artGo.GetComponent<RectTransform>();
            _art.anchorMin = _art.anchorMax = _art.pivot = new Vector2(0.5f, 0.5f);
            _art.sizeDelta = new Vector2(1920f, 1080f);
            var fitter = artGo.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = MW / MH;

            // Toda la pantalla inicia la partida con click
            var full = artGo.AddComponent<Button>();
            full.transition = Selectable.Transition.None;
            full.onClick.AddListener(StartGame);

            // ---- Brillo pulsante sobre el boton pintado "PRESIONA ESPACIO" ----
            _startGlow = MakeZoneImage("StartGlow", 152, 221, 436, 279, new Color(1f, 0.82f, 0.35f, 0.16f));

            // ---- Zonas clicables sobre el texto pintado del pie ----
            MakeZoneButton("Nueva Partida", 165, 281, 265, 303, StartGame);
            MakeZoneButton("Opciones", 266, 281, 339, 303, () => Toggle(_optionsPanel));
            MakeZoneButton("Creditos", 340, 281, 410, 303, () => Toggle(_creditsPanel));

            BuildHowToPlay();
            BuildOptions();
            BuildCredits();

            // Boton flotante elegante de "Como Jugar"
            MakeButton(_canvas.transform, "[H] COMO JUGAR (ODS)", () => Toggle(_howToPlayPanel),
                       new Vector2(0f, 68f), new Vector2(240f, 36f),
                       new Color32(45, 80, 130, 240), Gold, true);

            // Aviso de controles bajo el arte
            var hint = MakeText(_canvas.transform, "[ESPACIO] jugar     [H] saberes / como jugar     [ESC] salir",
                                20, TextAnchor.LowerCenter, Cream);
            var hr = hint.GetComponent<RectTransform>();
            hr.anchorMin = new Vector2(0.5f, 0f); hr.anchorMax = new Vector2(0.5f, 0f);
            hr.anchoredPosition = new Vector2(0f, 22f); hr.sizeDelta = new Vector2(1000f, 32f);
        }

        private static void EnsureEventSystem()
        {
#if UNITY_2023_1_OR_NEWER
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
#else
            if (Object.FindObjectOfType<EventSystem>() != null) return;
#endif
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        /// <summary>Crea un RectTransform anclado a una zona de la imagen (en px del arte original).</summary>
        private RectTransform Zone(string name, float x0, float y0, float x1, float y1)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_art, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(x0 / MW, 1f - y1 / MH);
            rt.anchorMax = new Vector2(x1 / MW, 1f - y0 / MH);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return rt;
        }

        private Image MakeZoneImage(string name, float x0, float y0, float x1, float y1, Color color)
        {
            var rt = Zone(name, x0, y0, x1, y1);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private void MakeZoneButton(string name, float x0, float y0, float x1, float y1, UnityEngine.Events.UnityAction action)
        {
            var rt = Zone(name, x0, y0, x1, y1);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);      // invisible: el texto ya esta pintado
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(1f, 0.85f, 0.4f, 0.28f);
            colors.pressedColor = new Color(1f, 0.85f, 0.4f, 0.45f);
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.selectedColor = new Color(1f, 1f, 1f, 0f);
            btn.colors = colors;
            btn.onClick.AddListener(action);
        }

        private Text MakeText(Transform parent, string txt, int size, TextAnchor anchor, Color32 color)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = _font; t.text = txt; t.fontSize = size;
            t.alignment = anchor; t.color = color; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        // =====================================================================
        private GameObject MakeDialog(string title, string body, float w, float h)
        {
            var root = new GameObject(title + "Panel");
            root.transform.SetParent(_canvas.transform, false);
            var rrt = root.AddComponent<RectTransform>();
            Stretch(rrt);

            var dim = new GameObject("Dim");
            dim.transform.SetParent(root.transform, false);
            var di = dim.AddComponent<Image>();
            di.color = new Color(0f, 0f, 0f, 0.7f);
            Stretch(dim.GetComponent<RectTransform>());
            var close = dim.AddComponent<Button>();
            close.transition = Selectable.Transition.None;
            close.onClick.AddListener(() => root.SetActive(false));

            var panel = new GameObject("Panel");
            panel.transform.SetParent(root.transform, false);
            var pi = panel.AddComponent<Image>();
            pi.color = PanelBg;
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f); prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(w, h);

            var ti = MakeText(panel.transform, title, 40, TextAnchor.UpperCenter, Gold);
            var trt = ti.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(20f, -78f); trt.offsetMax = new Vector2(-20f, -18f);

            var bt = MakeText(panel.transform, body, 24, TextAnchor.UpperLeft, Cream);
            var brt = bt.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = new Vector2(38f, 66f); brt.offsetMax = new Vector2(-38f, -88f);

            var hint = MakeText(panel.transform, "Click fuera para cerrar", 20, TextAnchor.LowerCenter, new Color32(150, 230, 175, 255));
            var hrt = hint.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0f, 0f); hrt.anchorMax = new Vector2(1f, 0f);
            hrt.offsetMin = new Vector2(0f, 18f); hrt.offsetMax = new Vector2(0f, 52f);

            root.SetActive(false);
            return root;
        }

        private void BuildOptions()
        {
            var root = new GameObject("OptionsPanel");
            root.transform.SetParent(_canvas.transform, false);
            var rrt = root.AddComponent<RectTransform>();
            Stretch(rrt);

            var dim = new GameObject("Dim");
            dim.transform.SetParent(root.transform, false);
            var di = dim.AddComponent<Image>();
            di.color = new Color(0f, 0f, 0f, 0.75f);
            Stretch(dim.GetComponent<RectTransform>());
            var close = dim.AddComponent<Button>();
            close.transition = Selectable.Transition.None;
            close.onClick.AddListener(() => root.SetActive(false));

            var panel = new GameObject("Panel");
            panel.transform.SetParent(root.transform, false);
            var pi = panel.AddComponent<Image>();
            pi.color = PanelBg;
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f); prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(1000f, 680f);

            var ti = MakeText(panel.transform, "AJUSTES Y OPCIONES", 36, TextAnchor.UpperCenter, Gold);
            var trt = ti.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(20f, -70f); trt.offsetMax = new Vector2(-20f, -15f);

            // Container for options content
            var content = new GameObject("Content");
            content.transform.SetParent(panel.transform, false);
            var crt = content.AddComponent<RectTransform>();
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = new Vector2(40f, 80f); crt.offsetMax = new Vector2(-40f, -80f);

            // --- SECCION 1: AUDIO ---
            var audioSec = MakeText(content.transform, "CONFIGURACION DE AUDIO", 26, TextAnchor.UpperLeft, Gold);
            var asrt = audioSec.GetComponent<RectTransform>();
            asrt.anchorMin = new Vector2(0f, 1f); asrt.anchorMax = new Vector2(0.5f, 1f);
            asrt.anchoredPosition = new Vector2(0f, -10f); asrt.sizeDelta = new Vector2(450f, 32f);

            // Music Volume Row
            var musicText = MakeText(content.transform, "Música: 80%", 22, TextAnchor.MiddleLeft, Cream);
            var mtrt = musicText.GetComponent<RectTransform>();
            mtrt.anchorMin = new Vector2(0f, 1f); mtrt.anchorMax = new Vector2(0f, 1f);
            mtrt.anchoredPosition = new Vector2(140f, -55f); mtrt.sizeDelta = new Vector2(200f, 30f);

            MakeButton(content.transform, "-", () => {
                var ad = AudioDirector.Instance;
                if (ad != null) {
                    float v = Mathf.Clamp01(ad.musicVolume - 0.1f);
                    ad.SetMusicVolume(v);
                    musicText.text = $"Música: {Mathf.RoundToInt(v * 100)}%";
                }
            }, new Vector2(260f, -55f), new Vector2(40f, 30f), new Color32(60, 65, 85, 255), Cream);

            MakeButton(content.transform, "+", () => {
                var ad = AudioDirector.Instance;
                if (ad != null) {
                    float v = Mathf.Clamp01(ad.musicVolume + 0.1f);
                    ad.SetMusicVolume(v);
                    musicText.text = $"Música: {Mathf.RoundToInt(v * 100)}%";
                }
            }, new Vector2(310f, -55f), new Vector2(40f, 30f), new Color32(60, 65, 85, 255), Cream);

            // SFX Volume Row
            var sfxText = MakeText(content.transform, "Efectos SFX: 80%", 22, TextAnchor.MiddleLeft, Cream);
            var strt = sfxText.GetComponent<RectTransform>();
            strt.anchorMin = new Vector2(0f, 1f); strt.anchorMax = new Vector2(0f, 1f);
            strt.anchoredPosition = new Vector2(140f, -95f); strt.sizeDelta = new Vector2(200f, 30f);

            MakeButton(content.transform, "-", () => {
                var ad = AudioDirector.Instance;
                if (ad != null) {
                    float v = Mathf.Clamp01(ad.sfxVolume - 0.1f);
                    ad.SetSfxVolume(v);
                    sfxText.text = $"Efectos SFX: {Mathf.RoundToInt(v * 100)}%";
                }
            }, new Vector2(260f, -95f), new Vector2(40f, 30f), new Color32(60, 65, 85, 255), Cream);

            MakeButton(content.transform, "+", () => {
                var ad = AudioDirector.Instance;
                if (ad != null) {
                    float v = Mathf.Clamp01(ad.sfxVolume + 0.1f);
                    ad.SetSfxVolume(v);
                    sfxText.text = $"Efectos SFX: {Mathf.RoundToInt(v * 100)}%";
                }
            }, new Vector2(310f, -95f), new Vector2(40f, 30f), new Color32(60, 65, 85, 255), Cream);

            // Test Sound button
            MakeButton(content.transform, "Probar Sonido (Pututo)", () => {
                AudioDirector.Instance?.Pututo();
            }, new Vector2(190f, -145f), new Vector2(280f, 38f), new Color32(180, 110, 40, 255), Cream);

            // --- SECCION 2: PANTALLA ---
            var displaySec = MakeText(content.transform, "PANTALLA", 26, TextAnchor.UpperLeft, Gold);
            var dsrt = displaySec.GetComponent<RectTransform>();
            dsrt.anchorMin = new Vector2(0f, 1f); dsrt.anchorMax = new Vector2(0.5f, 1f);
            dsrt.anchoredPosition = new Vector2(0f, -205f); dsrt.sizeDelta = new Vector2(450f, 32f);

            var fsBtnText = Screen.fullScreen ? "Modo: Pantalla Completa" : "Modo: Ventana";
            var fsLabel = MakeText(content.transform, fsBtnText, 22, TextAnchor.MiddleLeft, Cream);
            var fsrt = fsLabel.GetComponent<RectTransform>();
            fsrt.anchorMin = new Vector2(0f, 1f); fsrt.anchorMax = new Vector2(0f, 1f);
            fsrt.anchoredPosition = new Vector2(140f, -250f); fsrt.sizeDelta = new Vector2(280f, 30f);

            MakeButton(content.transform, "Cambiar Modo", () => {
                Screen.fullScreen = !Screen.fullScreen;
                fsLabel.text = Screen.fullScreen ? "Modo: Pantalla Completa" : "Modo: Ventana";
            }, new Vector2(330f, -250f), new Vector2(160f, 34f), new Color32(50, 80, 120, 255), Cream);

            // --- SECCION 3: GUIA DE CONTROLES (Columna derecha) ---
            var ctrlSec = MakeText(content.transform, "CONTROLES DEL JUEGO", 26, TextAnchor.UpperLeft, Gold);
            var csrt = ctrlSec.GetComponent<RectTransform>();
            csrt.anchorMin = new Vector2(0.52f, 1f); csrt.anchorMax = new Vector2(1f, 1f);
            csrt.anchoredPosition = new Vector2(0f, -10f); csrt.sizeDelta = new Vector2(450f, 32f);

            string controlsGuide =
                "[A] [D] o [Flechas] : Mover a Inti\n" +
                "[Shift] : Correr (consume aliento en la puna)\n" +
                "[Espacio] : Saltar obstaculos y pircas\n" +
                "[E] : Interactuar (cosechar, construir, curar)\n" +
                "[Q] : Tocar el Pututo (convoca ronderos)\n" +
                "[T] : Adelantar jornada al atardecer\n" +
                "[M] : Silenciar / Activar Música\n" +
                "[N] : Silenciar / Activar Efectos\n" +
                "[ESC] : Pausa y Menu de Opciones";

            var ctText = MakeText(content.transform, controlsGuide, 20, TextAnchor.UpperLeft, Cream);
            var ctr_rt = ctText.GetComponent<RectTransform>();
            ctr_rt.anchorMin = new Vector2(0.52f, 1f); ctr_rt.anchorMax = new Vector2(1f, 1f);
            ctr_rt.anchoredPosition = new Vector2(0f, -50f); ctr_rt.sizeDelta = new Vector2(450f, 340f);

            // Boton Volver
            MakeButton(panel.transform, "GUARDAR Y VOLVER", () => {
                root.SetActive(false);
            }, new Vector2(0f, 30f), new Vector2(260f, 44f), new Color32(190, 80, 40, 255), Cream, true);

            root.SetActive(false);
            _optionsPanel = root;
        }

        private void MakeButton(Transform parent, string labelText, UnityEngine.Events.UnityAction onClick,
                                Vector2 pos, Vector2 size, Color32 bgColor, Color32 textColor, bool anchorBottomCenter = false)
        {
            var go = new GameObject("Btn_" + labelText);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            if (anchorBottomCenter)
            {
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
            }
            else
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
            }
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = bgColor;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var colors = btn.colors;
            colors.highlightedColor = new Color(bgColor.r * 1.25f / 255f, bgColor.g * 1.25f / 255f, bgColor.b * 1.25f / 255f, 1f);
            colors.pressedColor = new Color(bgColor.r * 0.8f / 255f, bgColor.g * 0.8f / 255f, bgColor.b * 0.8f / 255f, 1f);
            btn.colors = colors;
            btn.onClick.AddListener(onClick);

            var txt = MakeText(go.transform, labelText, 18, TextAnchor.MiddleCenter, textColor);
            var trt = txt.GetComponent<RectTransform>();
            Stretch(trt);
        }

        private void BuildHowToPlay()
        {
            _howToPlayPanel = MakeDialog("COMO JUGAR - SABERES DEL YACHACHIQ",
                "Bienvenido, Inti. Tu mision es liderar a la comunidad frente a la helada andina (-20 C):\n\n" +
                "1. ODS 13: ACCION POR EL CLIMA Y PROTECCION DEL REBANO\n" +
                "   * Arrea a las alpacas al cobertizo antes de las 18:00 (usa [F] para silbarles o [Q] con el pututo).\n" +
                "   * Instala Muros Trombe y sellado con ichu en las viviendas de adobe con [E].\n\n" +
                "2. ODS 3: SALUD COMUNITARIA Y PREVENCION DE NEUMONIA\n" +
                "   * Dona Tomasa atiende la Posta: recolecta hierbas andinas para preparar Mate de Muna y Wira-wira.\n" +
                "   * Lleva infusiones calientes a los abuelos que tosen (Don Mariano) antes de que caiga la noche.\n" +
                "   * Alimenta el fogon comunal con lena y bosta seca para mantener la moral alta.\n\n" +
                "3. ORGANIZACION COMUNAL (AYNI)\n" +
                "   * Toca el Pututo ([Q]) para convocar a la Ronda Campesina: los comuneros vendran a faenar contigo.\n\n" +
                "CONTROLES RAPIDOS:\n" +
                "[A][D] Moverse  |  [Shift] Correr  |  [Espacio] Saltar  |  [E] Interactuar  |  [Q] Pututo / Ayni  |  [F] Silbido  |  [ESC] Pausa",
                1080f, 720f);
        }

        private void BuildCredits()
        {
            _creditsPanel = MakeDialog("CREDITOS",
                "FRIO ANDINO: SOBREVIVIR A LA HELADA\n\n" +
                "Videojuego educativo de micro-gestion y supervivencia bioclimatica\n" +
                "ambientado en una comunidad altoandina del Peru a 4,200 m s.n.m.\n\n" +
                "ODS 13 - Accion por el Clima (meta 13.1)\n" +
                "   Resiliencia y adaptacion mediante tecnicas bioclimaticas preventivas:\n" +
                "   Muro Trombe, sellado con ichu y cobertizos termicos.\n\n" +
                "ODS 3 - Salud y Bienestar (meta 3.9)\n" +
                "   Prevencion de IRAS y neumonia por frio extremo mediante confort\n" +
                "   termico intradomiciliario y medicina andina (muna y wira-wira).\n\n" +
                "Universidad Continental - Desarrollo de Videojuegos, 6to ciclo\n" +
                "Referencias reales: SENAMHI y MINSA (heladas de Puno, Cusco,\n" +
                "Huancavelica, Junin, Pasco y Arequipa).", 1040f, 680f);
        }

        // =====================================================================
        private void Update()
        {
            if (_started) return;

            // Latido del boton pintado
            if (_startGlow != null)
            {
                var c = _startGlow.color;
                c.a = 0.10f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.2f)) * 0.18f;
                _startGlow.color = c;
            }

            bool dialogOpen = (_optionsPanel != null && _optionsPanel.activeSelf) ||
                              (_creditsPanel != null && _creditsPanel.activeSelf) ||
                              (_howToPlayPanel != null && _howToPlayPanel.activeSelf);

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (dialogOpen)
                {
                    if (_optionsPanel != null) _optionsPanel.SetActive(false);
                    if (_creditsPanel != null) _creditsPanel.SetActive(false);
                    if (_howToPlayPanel != null) _howToPlayPanel.SetActive(false);
                }
                else Application.Quit();
                return;
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                Toggle(_howToPlayPanel);
                return;
            }

            if (dialogOpen) return;

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                StartGame();
        }

        private void Toggle(GameObject panel)
        {
            if (panel == null) return;
            bool targetActive = !panel.activeSelf;
            if (_optionsPanel != null) _optionsPanel.SetActive(panel == _optionsPanel && targetActive);
            if (_creditsPanel != null) _creditsPanel.SetActive(panel == _creditsPanel && targetActive);
            if (_howToPlayPanel != null) _howToPlayPanel.SetActive(panel == _howToPlayPanel && targetActive);
        }

        public void StartGame()
        {
            if (_started) return;
            if ((_optionsPanel != null && _optionsPanel.activeSelf) ||
                (_creditsPanel != null && _creditsPanel.activeSelf) ||
                (_howToPlayPanel != null && _howToPlayPanel.activeSelf)) return;

            _started = true;
            AudioDirector.Instance?.BuildDone();
            StartCoroutine(FadeOutAndPlay());
        }

        private IEnumerator FadeOutAndPlay()
        {
            var group = _canvas.gameObject.AddComponent<CanvasGroup>();
            float t = 0f;
            while (t < 0.45f)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = 1f - t / 0.45f;
                yield return null;
            }
            _canvas.gameObject.SetActive(false);

            var follow = Camera.main?.GetComponent<FrioAndino.Player.CameraFollow2D>();
            if (follow != null) follow.SetLocked(false);

            HUDController.Instance?.ShowIntro();
        }
    }
}
