using UnityEngine;
using System.Collections.Generic;

namespace FrioAndino.Art
{
    /// <summary>
    /// Carga el arte del juego (PNG guardados como .bytes en Resources) y define el mapeo
    /// entre los pixeles del fondo pintado y las coordenadas del mundo.
    ///
    /// Se usan .bytes en vez de sprites importados para que las texturas entren SIEMPRE
    /// con filtro Point, sin compresion y sin depender de la configuracion del importador.
    /// </summary>
    public static class GameArt
    {
        // ---- Medidas del arte (se reconfiguran si hay escenario panoramico) ----
        public const float PPU = 32f;

        /// <summary>Ancho del arte en pixeles. Define el ancho del mundo jugable.</summary>
        public static float BackgroundPixelWidth { get; private set; } = 805f;
        /// <summary>Alto total del arte en pixeles. Es lo que cubre la camara de arriba a abajo.</summary>
        public static float BackgroundPixelHeight { get; private set; } = 220f;
        /// <summary>Altura en pixeles del arte donde pisan los personajes (linea de suelo).</summary>
        public static float GroundPixelY { get; private set; } = 140f;
        /// <summary>Altura deseada del jugador medida en pixeles del arte.</summary>
        public static float PlayerPixelHeight { get; private set; } = 72f;
        /// <summary>Altura de los comuneros en pixeles del arte (un poco menor que Inti).</summary>
        public static float VillagerPixelHeight { get; private set; } = 65f;

        /// <summary>True cuando corre el escenario panoramico de dos capas.</summary>
        public static bool LayeredStage { get; private set; }

        /// <summary>
        /// Reconfigura el mapeo pixel->mundo para el escenario panoramico.
        /// A partir de aqui TODAS las llamadas a X/Y/U/Pos/Ground usan las coordenadas
        /// del mapa nuevo, sin tener que tocar el resto de los scripts.
        /// </summary>
        public static void ConfigureStage(float pixelWidth, float pixelHeight, float groundPixelY,
                                          float playerPixelHeight, float villagerPixelHeight)
        {
            BackgroundPixelWidth = pixelWidth;
            BackgroundPixelHeight = pixelHeight;
            GroundPixelY = groundPixelY;
            PlayerPixelHeight = playerPixelHeight;
            VillagerPixelHeight = villagerPixelHeight;
            LayeredStage = true;
            _sprites.Clear();   // los PPU de Inti/comuneros dependen de estas medidas
        }

        /// <summary>Fija la escala directamente (la calcula StageBuilder con el alto de camara).</summary>
        public static void SetScale(float scale) => Scale = scale;

        // ---- Limites de caminata: margen en pixeles a cada lado del mapa ----
        public static float WalkMarginPixels { get; set; } = 12f;
        public static float MinWalkX => X(WalkMarginPixels);
        public static float MaxWalkX => X(BackgroundPixelWidth - WalkMarginPixels);

        public static float WorldWidth => BackgroundPixelWidth / PPU;    // 17.69 u
        public static float WorldHeight => BackgroundPixelHeight / PPU;  // 9.75 u

        /// <summary>Escala global para que el fondo cubra la pantalla en cualquier proporcion.</summary>
        public static float Scale { get; private set; } = 1f;

        /// <summary>Limite horizontal del mundo en unidades (mitad del ancho).</summary>
        public static float WorldHalfWidth => WorldWidth * 0.5f * Scale;

        private static readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();

        /// <summary>Texturas que pueden faltar sin que sea un error (escenario por capas).</summary>
        private static readonly HashSet<string> Opcionales = new HashSet<string> { "fondo_villa", "suelo" };

        public static void ComputeScale(Camera cam)
        {
            if (cam == null) { Scale = 1f; return; }
            float viewH = cam.orthographicSize * 2f;
            float viewW = viewH * cam.aspect;
            // "cover": el fondo nunca deja franjas vacias
            Scale = Mathf.Max(viewW / WorldWidth, viewH / WorldHeight, 1f);
        }

        // =====================================================================
        //  Conversion pixel del fondo -> mundo
        // =====================================================================
        public static float X(float px) => (px - BackgroundPixelWidth * 0.5f) / PPU * Scale;
        public static float Y(float py) => (GroundPixelY - py) / PPU * Scale;
        public static Vector3 Pos(float px, float py) => new Vector3(X(px), Y(py), 0f);
        /// <summary>Punto sobre la linea de suelo, a partir de la X en pixeles del fondo.</summary>
        public static Vector3 Ground(float px) => new Vector3(X(px), 0f, 0f);
        /// <summary>Convierte un tamano en pixeles del fondo a unidades del mundo.</summary>
        public static float U(float pixels) => pixels / PPU * Scale;

        // =====================================================================
        //  Carga de texturas
        // =====================================================================
        private static Texture2D LoadTexture(string name)
        {
            if (_textures.TryGetValue(name, out var t) && t != null) return t;
            var asset = Resources.Load<TextAsset>("FrioAndino/" + name);
            if (asset == null)
            {
                // Las piezas opcionales (escenario por capas) no avisan: el juego
                // simplemente cae al fondo unico si no estan puestas.
                if (!Opcionales.Contains(name))
                    Debug.LogWarning($"[FrioAndino] No se encontro Resources/FrioAndino/{name}.bytes");
                return null;
            }
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(asset.bytes);          // decodifica el PNG tal cual, sin compresion
            tex.filterMode = FilterMode.Point;   // pixel art nitido
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.name = name;
            _textures[name] = tex;
            return tex;
        }

        private static Sprite Make(string key, string texName, float ppu, Vector2 pivot)
        {
            if (_sprites.TryGetValue(key, out var s) && s != null) return s;
            var tex = LoadTexture(texName);
            if (tex == null) return null;
            var sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, ppu, 0, SpriteMeshType.FullRect);
            sp.name = key;
            _sprites[key] = sp;
            return sp;
        }

        // =====================================================================
        //  Sprites del juego
        // =====================================================================
        /// <summary>Fondo del caserio. Pivote en la linea de suelo para que escale sin moverla.</summary>
        public static Sprite Background =>
            Make("bg", "fondo", PPU, new Vector2(0.5f, (BackgroundPixelHeight - GroundPixelY) / BackgroundPixelHeight));

        /// <summary>Inti. PPU calculado para que mida PlayerPixelHeight pixeles del fondo.</summary>
        public static Sprite Inti
        {
            get
            {
                var tex = LoadTexture("inti");
                if (tex == null) return null;
                float ppu = tex.height / (PlayerPixelHeight / PPU);
                return Make("inti", "inti", ppu, new Vector2(0.5f, 0f));
            }
        }

        // Personajes secundarios con arte propio:
        //   comunero_a = el lenador (carga lena, hace adobes)
        //   comunero_b = la abuela (camina por el caserio, cocina en su fogon)
        private static readonly string[] VillagerTextures = { "comunero_a", "comunero_b" };

        /// <summary>
        /// Sprite fijo del comunero. Con las hojas nuevas puestas devuelve el primer cuadro
        /// del "idle" de esa variante; asi nadie aparece nunca con el sprite prestado de Inti.
        /// </summary>
        public static Sprite Villager(int variant)
        {
            string n = VillagerTextures[Mathf.Abs(variant) % VillagerTextures.Length];

            // 1) hoja de animacion propia (comunero_a_idle.bytes ...)
            var quieto = Frames(n + "_idle", VillagerPixelHeight);
            if (quieto == null) quieto = Frames(n + "_walk", VillagerPixelHeight);
            if (quieto != null && quieto.Length > 0) return quieto[0];

            // 2) textura suelta antigua, si alguien la vuelve a poner
            var tex = LoadTexture(n);
            if (tex == null) return Inti;
            float ppu = tex.height / (VillagerPixelHeight / PPU);
            return Make("v_" + n, n, ppu, new Vector2(0.5f, 0f));
        }

        // ---------------------------------------------------------------------
        //  ESCENARIO POR CAPAS (side-scroller)
        //  Estas dos son opcionales: si no estan, el juego usa "fondo" a pantalla completa.
        // ---------------------------------------------------------------------

        /// <summary>
        /// Background_Village: cielo, cordillera, rio glacial, casas, fogon y cobertizo.
        /// Pivote abajo-centro; StageBuilder lo coloca en la Y exacta del mapa.
        /// Archivo: Resources/FrioAndino/fondo_villa.bytes
        /// </summary>
        public static Sprite Panorama => Make("panorama", "fondo_villa", PPU, new Vector2(0.5f, 0f));

        /// <summary>
        /// Foreground_Ground: camino con nieve/ichu + muro de pirca + puente.
        /// Pivote abajo-centro; StageBuilder lo coloca en la Y exacta del mapa.
        /// Archivo: Resources/FrioAndino/suelo.bytes
        /// </summary>
        public static Sprite GroundStrip => Make("suelo", "suelo", PPU, new Vector2(0.5f, 0f));

        /// <summary>True si el proyecto tiene el escenario desacoplado en dos capas.</summary>
        public static bool HasLayeredStage =>
            LoadTexture("fondo_villa") != null && LoadTexture("suelo") != null;

        public static Sprite Menu => Make("menu", "menu", PPU, new Vector2(0.5f, 0.5f));

        /// <summary>Panel de vidrio del Muro Trombe, recortado del propio fondo.</summary>
        public static Sprite TrombeGlass => Make("vidrio", "vidrio", PPU, new Vector2(0.5f, 0f));

        /// <summary>Paja de ichu, para el sellado de viviendas.</summary>
        public static Sprite IchuThatch => Make("ichu_techo", "ichu_techo", PPU, new Vector2(0.5f, 0.5f));

        /// <summary>Parche de ceniza fria para cuando el fogon se apaga.</summary>
        public static Sprite FireOut => Make("fogon_apagado", "fogon_apagado", PPU, new Vector2(0.5f, 0.5f));

        public static Sprite Alpaca => Make("alpaca", "alpaca", PPU, new Vector2(0.5f, 0f));
        public static Sprite AlpacaBaby => Make("alpaca_cria", "alpaca_cria", PPU, new Vector2(0.5f, 0f));

        public static Texture2D MenuTexture => LoadTexture("menu");

        public static bool Available => LoadTexture("fondo") != null;

        // =====================================================================
        //  HOJAS DE ANIMACION
        //  Un PNG con los cuadros en fila -> array de Sprite listo para SpriteAnimator.
        // =====================================================================

        /// <summary>Carga una textura sin avisar si no existe (las animaciones son opcionales).</summary>
        private static Texture2D LoadTextureQuiet(string name)
        {
            if (_textures.TryGetValue(name, out var t) && t != null) return t;
            var asset = Resources.Load<TextAsset>("FrioAndino/" + name);
            if (asset == null) return null;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(asset.bytes);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.name = name;
            _textures[name] = tex;
            return tex;
        }

        /// <summary>True si existe ese archivo .bytes en Resources/FrioAndino.</summary>
        public static bool HasTexture(string name) => LoadTextureQuiet(name) != null;

        /// <summary>
        /// Detecta cuantos cuadros tiene una hoja buscando columnas completamente
        /// transparentes entre figura y figura. Si los cuadros estan pegados (sin
        /// separacion), asume que son cuadrados y divide ancho/alto.
        /// </summary>
        private static int DetectFrameCount(Texture2D tex)
        {
            int w = tex.width, h = tex.height;
            var px = tex.GetPixels32();

            bool[] vacia = new bool[w];
            for (int x = 0; x < w; x++)
            {
                bool empty = true;
                for (int y = 0; y < h; y++)
                {
                    if (px[y * w + x].a > 8) { empty = false; break; }
                }
                vacia[x] = empty;
            }

            int bloques = 0;
            bool dentro = false;
            for (int x = 0; x < w; x++)
            {
                if (!vacia[x] && !dentro) { dentro = true; bloques++; }
                else if (vacia[x] && dentro) dentro = false;
            }

            if (bloques >= 2) return bloques;

            // Sin separaciones: cuadros cuadrados
            int cuadrados = Mathf.RoundToInt((float)w / Mathf.Max(1, h));
            return Mathf.Max(1, cuadrados);
        }

        /// <summary>
        /// Corta una hoja en cuadros del mismo ancho y los devuelve como Sprites,
        /// escalados para que el personaje mida <paramref name="targetPixelHeight"/>
        /// pixeles del arte. Pivote abajo-centro: los pies no se despegan del suelo.
        /// </summary>
        public static Sprite[] Frames(string texName, float targetPixelHeight, int frameCount = 0)
        {
            var tex = LoadTextureQuiet(texName);
            if (tex == null) return null;

            if (frameCount <= 0)
            {
                if (texName.EndsWith("_walk") && tex.width == 770) frameCount = 7;
                else if (texName.EndsWith("_run") && tex.width == 840) frameCount = 6;
                else if (texName.EndsWith("_pututo") && tex.width == 560) frameCount = 4;
                else if (texName.EndsWith("_idle") && tex.width == 110) frameCount = 1;
                else if (texName.EndsWith("comunero_a_work") && tex.width == 450) frameCount = 3;
                else if (texName.EndsWith("comunero_a_walk") && tex.width == 186) frameCount = 2;
                else if (texName.EndsWith("comunero_a_idle") && tex.width == 188) frameCount = 2;
                else if (texName.EndsWith("comunero_b_work") && tex.width == 232) frameCount = 2;
                else if (texName.EndsWith("comunero_b_walk") && tex.width == 158) frameCount = 2;
                else if (texName.EndsWith("comunero_b_idle") && tex.width == 70) frameCount = 1;
                else frameCount = DetectFrameCount(tex);
            }
            frameCount = Mathf.Clamp(frameCount, 1, 64);

            int fw = tex.width / frameCount;
            if (fw <= 0) return null;

            float ppu = tex.height * PPU / Mathf.Max(1f, targetPixelHeight);
            var pivot = new Vector2(0.5f, 0f);

            var result = new Sprite[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                var rect = new Rect(i * fw, 0, fw, tex.height);
                var sp = Sprite.Create(tex, rect, pivot, ppu, 0, SpriteMeshType.FullRect);
                sp.name = texName + "_" + i;
                result[i] = sp;
            }
            return result;
        }

        /// <summary>Hoja en rejilla (varias filas). Se lee de arriba-izquierda a abajo-derecha.</summary>
        public static Sprite[] FramesGrid(string texName, int cols, int rows, float targetPixelHeight)
        {
            var tex = LoadTextureQuiet(texName);
            if (tex == null || cols <= 0 || rows <= 0) return null;

            int fw = tex.width / cols, fh = tex.height / rows;
            float ppu = fh * PPU / Mathf.Max(1f, targetPixelHeight);
            var pivot = new Vector2(0.5f, 0f);

            var result = new Sprite[cols * rows];
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    // En Unity el origen de la textura esta abajo: invertimos la fila
                    var rect = new Rect(c * fw, (rows - 1 - r) * fh, fw, fh);
                    var sp = Sprite.Create(tex, rect, pivot, ppu, 0, SpriteMeshType.FullRect);
                    sp.name = $"{texName}_{r}_{c}";
                    result[r * cols + c] = sp;
                }
            }
            return result;
        }

        /// <summary>Velocidad por defecto de cada estado, en cuadros por segundo.</summary>
        private static float FpsPara(string estado)
        {
            switch (estado)
            {
                case "run": return 12f;
                case "walk": return 9f;
                case "work": return 8f;
                case "jump": return 6f;
                case "pututo": return 4f;
                default: return 5f;   // idle
            }
        }

        /// <summary>
        /// Busca las hojas de un personaje y las carga en su SpriteAnimator.
        /// Convencion de nombres en Resources/FrioAndino:
        ///     &lt;prefijo&gt;_idle.bytes, _walk.bytes, _run.bytes, _jump.bytes, _work.bytes
        /// Devuelve cuantos clips encontro (0 = sigue la animacion procedural).
        /// </summary>
        public static int LoadClips(SpriteAnimator anim, string prefijo, float targetPixelHeight)
        {
            if (anim == null || string.IsNullOrEmpty(prefijo)) return 0;

            string[] estados = { "idle", "walk", "run", "jump", "work", "pututo" };
            int cargados = 0;

            foreach (var estado in estados)
            {
                var frames = Frames(prefijo + "_" + estado, targetPixelHeight);
                if (frames == null) continue;
                anim.Add(estado, frames, FpsPara(estado), estado != "jump");
                cargados++;
            }
            return cargados;
        }

        /// <summary>Animaciones de Inti: inti_idle / inti_walk / inti_run / inti_jump.</summary>
        public static int LoadPlayerClips(SpriteAnimator anim) =>
            LoadClips(anim, "inti", PlayerPixelHeight);

        /// <summary>
        /// Animaciones de comunero. Busca primero las de su variante de color
        /// (comunero_a_walk...) y si no hay, usa una hoja compartida (comunero_walk...).
        /// </summary>
        public static int LoadVillagerClips(SpriteAnimator anim, int variant)
        {
            string n = VillagerTextures[Mathf.Abs(variant) % VillagerTextures.Length];
            int c = LoadClips(anim, n, VillagerPixelHeight);
            if (c == 0) c = LoadClips(anim, "comunero", VillagerPixelHeight);
            return c;
        }

        /// <summary>Animaciones de alpaca: alpaca_idle / alpaca_walk (y alpaca_cria_*).</summary>
        public static int LoadAlpacaClips(SpriteAnimator anim, bool baby, float pixelHeight)
        {
            int c = LoadClips(anim, baby ? "alpaca_cria" : "alpaca", pixelHeight);
            if (c == 0 && baby) c = LoadClips(anim, "alpaca", pixelHeight);
            return c;
        }
    }
}
