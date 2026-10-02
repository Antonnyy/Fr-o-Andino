using UnityEngine;
using System.Collections.Generic;

namespace FrioAndino.Art
{
    /// <summary>
    /// Genera TODO el pixel art del juego por codigo (no requiere assets externos).
    /// Paleta basada en el GDD: terracota adobe, ocre ichu, azul helada, blanco glaciar, fuego calido.
    /// </summary>
    public static class PixelArtFactory
    {
        public const float PPU = 32f;

        // ---------- Paleta oficial del GDD ----------
        public static readonly Color32 AdobeDark   = new Color32(122, 62, 40, 255);
        public static readonly Color32 Adobe       = new Color32(165, 92, 58, 255);
        public static readonly Color32 AdobeLight  = new Color32(198, 124, 82, 255);
        public static readonly Color32 IchuDark    = new Color32(150, 110, 30, 255);
        public static readonly Color32 Ichu        = new Color32(212, 160, 23, 255);
        public static readonly Color32 IchuLight   = new Color32(240, 205, 110, 255);
        public static readonly Color32 NightBlue   = new Color32(26, 43, 76, 255);
        public static readonly Color32 IceBlue     = new Color32(140, 180, 230, 255);
        public static readonly Color32 Glacier     = new Color32(224, 234, 252, 255);
        public static readonly Color32 FireOrange  = new Color32(255, 140, 0, 255);
        public static readonly Color32 FireYellow  = new Color32(255, 215, 120, 255);
        public static readonly Color32 StoneDark   = new Color32(70, 74, 86, 255);
        public static readonly Color32 Stone       = new Color32(110, 114, 126, 255);
        public static readonly Color32 StoneLight  = new Color32(150, 154, 168, 255);
        public static readonly Color32 GrassDark   = new Color32(84, 96, 56, 255);
        public static readonly Color32 Grass       = new Color32(120, 136, 74, 255);
        public static readonly Color32 SkinTone    = new Color32(196, 142, 104, 255);
        public static readonly Color32 PonchoRed   = new Color32(186, 52, 52, 255);
        public static readonly Color32 PonchoBrown = new Color32(140, 96, 58, 255);
        public static readonly Color32 PonchoBlue  = new Color32(58, 86, 142, 255);
        public static readonly Color32 WoolWhite   = new Color32(238, 236, 228, 255);
        public static readonly Color32 Transparent = new Color32(0, 0, 0, 0);

        private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();
        private static System.Random _rng = new System.Random(20260913);

        // =====================================================================
        //  Utilidades de dibujo sobre buffer de pixeles
        // =====================================================================
        private class Canvas
        {
            public int W, H;
            public Color32[] Px;

            public Canvas(int w, int h)
            {
                W = w; H = h;
                Px = new Color32[w * h];
                for (int i = 0; i < Px.Length; i++) Px[i] = Transparent;
            }

            public void Set(int x, int y, Color32 c)
            {
                if (x < 0 || y < 0 || x >= W || y >= H) return;
                Px[y * W + x] = c;
            }

            public Color32 Get(int x, int y)
            {
                if (x < 0 || y < 0 || x >= W || y >= H) return Transparent;
                return Px[y * W + x];
            }

            public void Rect(int x, int y, int w, int h, Color32 c)
            {
                for (int j = y; j < y + h; j++)
                    for (int i = x; i < x + w; i++)
                        Set(i, j, c);
            }

            public void RectOutline(int x, int y, int w, int h, Color32 c)
            {
                for (int i = x; i < x + w; i++) { Set(i, y, c); Set(i, y + h - 1, c); }
                for (int j = y; j < y + h; j++) { Set(x, j, c); Set(x + w - 1, j, c); }
            }

            public void Line(int x0, int y0, int x1, int y1, Color32 c)
            {
                int dx = Mathf.Abs(x1 - x0), dy = -Mathf.Abs(y1 - y0);
                int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
                int err = dx + dy;
                while (true)
                {
                    Set(x0, y0, c);
                    if (x0 == x1 && y0 == y1) break;
                    int e2 = 2 * err;
                    if (e2 >= dy) { err += dy; x0 += sx; }
                    if (e2 <= dx) { err += dx; y0 += sy; }
                }
            }

            /// <summary>Triangulo relleno: util para techos y montanas.</summary>
            public void Triangle(int cx, int baseY, int halfW, int height, Color32 c)
            {
                for (int j = 0; j < height; j++)
                {
                    float t = 1f - (float)j / height;
                    int hw = Mathf.RoundToInt(halfW * t);
                    for (int i = cx - hw; i <= cx + hw; i++) Set(i, baseY + j, c);
                }
            }

            public void Noise(int x, int y, int w, int h, Color32 c, float chance, System.Random rng)
            {
                for (int j = y; j < y + h; j++)
                    for (int i = x; i < x + w; i++)
                        if (rng.NextDouble() < chance) Set(i, j, c);
            }
        }

        private static Sprite Build(string key, Canvas c, Vector2 pivot)
        {
            var tex = new Texture2D(c.W, c.H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels32(c.Px);
            tex.Apply();
            var sp = Sprite.Create(tex, new Rect(0, 0, c.W, c.H), pivot, PPU, 0, SpriteMeshType.FullRect);
            sp.name = key;
            _cache[key] = sp;
            return sp;
        }

        private static bool Cached(string key, out Sprite sp) => _cache.TryGetValue(key, out sp);

        // =====================================================================
        //  SPRITES SOLIDOS / AUXILIARES
        // =====================================================================
        public static Sprite Solid(Color32 color, int w = 8, int h = 8)
        {
            string key = $"solid_{color.r}_{color.g}_{color.b}_{color.a}_{w}x{h}";
            if (Cached(key, out var s)) return s;
            var c = new Canvas(w, h);
            c.Rect(0, 0, w, h, color);
            return Build(key, c, new Vector2(0.5f, 0.5f));
        }

        public static Sprite Circle(Color32 color, int d = 16)
        {
            string key = $"circle_{color.r}_{color.g}_{color.b}_{d}";
            if (Cached(key, out var s)) return s;
            var c = new Canvas(d, d);
            float r = d / 2f;
            for (int y = 0; y < d; y++)
                for (int x = 0; x < d; x++)
                {
                    float dx = x - r + 0.5f, dy = y - r + 0.5f;
                    if (dx * dx + dy * dy <= r * r) c.Set(x, y, color);
                }
            return Build(key, c, new Vector2(0.5f, 0.5f));
        }

        // =====================================================================
        //  FONDOS PARALLAX
        // =====================================================================
        /// <summary>Capa 1: nevados lejanos.</summary>
        public static Sprite SnowMountains(int w = 512, int h = 192)
        {
            string key = "bg_snow";
            if (Cached(key, out var s)) return s;
            var rng = new System.Random(7);
            var c = new Canvas(w, h);

            int peaks = 7;
            for (int p = 0; p < peaks; p++)
            {
                int cx = (int)((p + 0.5f) * w / peaks) + rng.Next(-20, 20);
                int ph = rng.Next(h / 2, h - 10);
                int hw = rng.Next(50, 90);
                var body = new Color32(96, 108, 150, 255);
                c.Triangle(cx, 0, hw, ph, body);
                // Cara iluminada
                for (int j = 0; j < ph; j++)
                {
                    float t = 1f - (float)j / ph;
                    int hwj = Mathf.RoundToInt(hw * t);
                    for (int i = cx - hwj; i < cx; i++)
                        c.Set(i, j, new Color32(120, 132, 176, 255));
                }
                // Nieve en la cumbre
                int snowH = Mathf.RoundToInt(ph * 0.38f);
                for (int j = ph - snowH; j < ph; j++)
                {
                    float t = 1f - (float)j / ph;
                    int hwj = Mathf.RoundToInt(hw * t);
                    for (int i = cx - hwj; i <= cx + hwj; i++)
                    {
                        bool edge = rng.NextDouble() < 0.2 && j < ph - snowH + 6;
                        c.Set(i, j, edge ? new Color32(190, 205, 235, 255) : Glacier);
                    }
                }
            }
            return Build(key, c, new Vector2(0.5f, 0f));
        }

        /// <summary>Capa 2: cerros con andenes.</summary>
        public static Sprite Terraces(int w = 512, int h = 128)
        {
            string key = "bg_terraces";
            if (Cached(key, out var s)) return s;
            var rng = new System.Random(21);
            var c = new Canvas(w, h);
            for (int x = 0; x < w; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.012f, 3.1f);
                int top = Mathf.RoundToInt(h * (0.35f + n * 0.55f));
                for (int y = 0; y < top; y++)
                {
                    Color32 col = new Color32(92, 84, 72, 255);
                    if (y > top - 3) col = new Color32(116, 104, 84, 255);
                    c.Set(x, y, col);
                }
                // Lineas de andenes
                for (int t = 12; t < top; t += 11)
                    c.Set(x, t, new Color32(70, 64, 54, 255));
            }
            c.Noise(0, 0, w, h, new Color32(140, 130, 105, 255), 0.02f, rng);
            return Build(key, c, new Vector2(0.5f, 0f));
        }

        /// <summary>Capa 3: colinas medias con pasto de puna.</summary>
        public static Sprite Hills(int w = 512, int h = 96)
        {
            string key = "bg_hills";
            if (Cached(key, out var s)) return s;
            var rng = new System.Random(33);
            var c = new Canvas(w, h);
            for (int x = 0; x < w; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.02f, 8.7f);
                int top = Mathf.RoundToInt(h * (0.3f + n * 0.5f));
                for (int y = 0; y < top; y++)
                    c.Set(x, y, y > top - 4 ? Grass : GrassDark);
                if (rng.NextDouble() < 0.10)
                    for (int k = 0; k < 4; k++) c.Set(x, top + k, IchuDark);
            }
            return Build(key, c, new Vector2(0.5f, 0f));
        }

        /// <summary>Capa 4 (foreground): rocas oscuras e ichu frontal.</summary>
        public static Sprite Foreground(int w = 384, int h = 64)
        {
            string key = "bg_fg";
            if (Cached(key, out var s)) return s;
            var rng = new System.Random(55);
            var c = new Canvas(w, h);
            for (int x = 0; x < w; x += rng.Next(14, 34))
            {
                int bh = rng.Next(12, 30);
                int bw = rng.Next(10, 26);
                c.Rect(x, 0, bw, bh, new Color32(38, 40, 50, 255));
                c.Rect(x + 1, bh - 3, bw - 2, 2, new Color32(56, 58, 70, 255));
                // Mata de ichu encima
                for (int k = 0; k < 7; k++)
                {
                    int bx = x + rng.Next(0, bw);
                    c.Line(bx, bh, bx + rng.Next(-3, 4), bh + rng.Next(6, 16), IchuDark);
                }
            }
            return Build(key, c, new Vector2(0.5f, 0f));
        }

        // =====================================================================
        //  SUELO
        // =====================================================================
        public static Sprite GroundTile(int w = 128, int h = 96)
        {
            string key = "ground";
            if (Cached(key, out var s)) return s;
            var rng = new System.Random(101);
            var c = new Canvas(w, h);
            c.Rect(0, 0, w, h, new Color32(62, 54, 46, 255));
            c.Rect(0, h - 10, w, 10, new Color32(92, 78, 60, 255));
            c.Rect(0, h - 4, w, 4, new Color32(116, 104, 74, 255));
            // Pasto seco y escarcha dispersa
            for (int x = 0; x < w; x++)
            {
                if (rng.NextDouble() < 0.25)
                    for (int k = 0; k < rng.Next(2, 6); k++) c.Set(x, h - 1 + k, IchuDark);
                if (rng.NextDouble() < 0.10) c.Set(x, h - 2, Glacier);
            }
            c.Noise(0, 0, w, h - 10, new Color32(48, 42, 36, 255), 0.06f, rng);
            return Build(key, c, new Vector2(0.5f, 1f)); // pivote arriba: y=0 queda al nivel del piso
        }

        // =====================================================================
        //  PERSONAJES
        // =====================================================================
        /// <summary>Inti: poncho rojo, chullo, pututo al hombro.</summary>
        public static Sprite Inti(int frame = 0)
        {
            string key = "inti_" + frame;
            if (Cached(key, out var s)) return s;
            var c = new Canvas(20, 30);
            int legOff = frame == 1 ? 1 : 0;

            // Piernas
            c.Rect(7, 0 + legOff, 3, 7, new Color32(60, 50, 44, 255));
            c.Rect(11, 0 + (frame == 2 ? 1 : 0), 3, 7, new Color32(60, 50, 44, 255));
            // Ojotas
            c.Rect(6, 0 + legOff, 5, 2, new Color32(38, 32, 28, 255));
            c.Rect(10, 0 + (frame == 2 ? 1 : 0), 5, 2, new Color32(38, 32, 28, 255));
            // Poncho
            c.Rect(5, 7, 11, 11, PonchoRed);
            c.Rect(5, 12, 11, 2, new Color32(230, 190, 90, 255));  // franja ocre
            c.Rect(5, 9, 11, 1, new Color32(140, 34, 34, 255));
            c.Rect(4, 8, 1, 8, new Color32(140, 34, 34, 255));
            c.Rect(16, 8, 1, 8, new Color32(140, 34, 34, 255));
            // Cabeza
            c.Rect(7, 18, 7, 6, SkinTone);
            c.Set(9, 21, new Color32(40, 28, 22, 255));
            c.Set(12, 21, new Color32(40, 28, 22, 255));
            // Chullo
            c.Rect(6, 23, 9, 4, PonchoBlue);
            c.Rect(6, 25, 9, 1, new Color32(220, 200, 120, 255));
            c.Rect(9, 27, 3, 2, PonchoBlue);
            c.Rect(5, 22, 2, 3, PonchoBlue); // orejera izq
            c.Rect(14, 22, 2, 3, PonchoBlue); // orejera der
            // Pututo (caracola) colgado
            c.Rect(16, 10, 3, 3, new Color32(226, 210, 180, 255));
            c.Set(18, 12, new Color32(180, 160, 130, 255));
            return Build(key, c, new Vector2(0.5f, 0f));
        }

        /// <summary>Comunero / rondero.</summary>
        public static Sprite Villager(int variant = 0)
        {
            string key = "villager_" + variant;
            if (Cached(key, out var s)) return s;
            var c = new Canvas(18, 28);
            Color32 poncho = variant == 0 ? PonchoBrown
                           : variant == 1 ? new Color32(120, 70, 110, 255)
                           : new Color32(70, 110, 100, 255);
            c.Rect(6, 0, 3, 6, new Color32(60, 50, 44, 255));
            c.Rect(10, 0, 3, 6, new Color32(60, 50, 44, 255));
            c.Rect(4, 6, 11, 10, poncho);
            c.Rect(4, 10, 11, 2, IchuLight);
            c.Rect(6, 16, 7, 6, SkinTone);
            c.Set(8, 19, new Color32(40, 28, 22, 255));
            c.Set(11, 19, new Color32(40, 28, 22, 255));
            // Sombrero / montera
            c.Rect(4, 21, 11, 2, new Color32(48, 42, 38, 255));
            c.Rect(6, 23, 7, 3, new Color32(48, 42, 38, 255));
            return Build(key, c, new Vector2(0.5f, 0f));
        }

        /// <summary>Abuelos (Tomasa / Mariano): mas bajos, pollera o poncho gris.</summary>
        public static Sprite Elder(bool female)
        {
            string key = "elder_" + (female ? "f" : "m");
            if (Cached(key, out var s)) return s;
            var c = new Canvas(18, 26);
            Color32 cloth = female ? new Color32(156, 60, 96, 255) : new Color32(96, 96, 104, 255);
            c.Rect(6, 0, 3, 5, new Color32(60, 50, 44, 255));
            c.Rect(10, 0, 3, 5, new Color32(60, 50, 44, 255));
            if (female) c.Rect(3, 5, 13, 8, cloth); else c.Rect(4, 5, 11, 9, cloth);
            c.Rect(4, 9, 11, 2, IchuLight);
            c.Rect(6, 14, 7, 6, new Color32(180, 130, 96, 255));
            c.Set(8, 17, new Color32(40, 28, 22, 255));
            c.Set(11, 17, new Color32(40, 28, 22, 255));
            c.Rect(5, 19, 9, 2, new Color32(220, 220, 220, 255)); // cabello cano
            c.Rect(3, 20, 13, 2, new Color32(70, 60, 50, 255));   // sombrero
            c.Rect(6, 22, 7, 3, new Color32(70, 60, 50, 255));
            return Build(key, c, new Vector2(0.5f, 0f));
        }

        /// <summary>Alpaca adulta o cria (Sami).</summary>
        public static Sprite Alpaca(bool baby = false)
        {
            string key = "alpaca_" + (baby ? "baby" : "adult");
            if (Cached(key, out var s)) return s;
            var c = new Canvas(30, 28);
            Color32 wool = baby ? new Color32(248, 246, 238, 255) : WoolWhite;
            Color32 shade = new Color32(206, 202, 190, 255);
            // Patas
            c.Rect(6, 0, 3, 8, shade);
            c.Rect(12, 0, 3, 8, shade);
            c.Rect(18, 0, 3, 8, shade);
            c.Rect(23, 0, 3, 8, shade);
            // Cuerpo
            c.Rect(5, 7, 22, 10, wool);
            c.Rect(5, 7, 22, 2, shade);
            // Cuello
            c.Rect(21, 14, 5, 9, wool);
            // Cabeza
            c.Rect(20, 21, 8, 5, wool);
            c.Rect(27, 22, 2, 2, new Color32(60, 50, 46, 255)); // hocico
            c.Set(25, 24, new Color32(40, 32, 28, 255));        // ojo
            // Orejas
            c.Rect(21, 26, 2, 3, wool);
            c.Rect(25, 26, 2, 3, wool);
            // Pompon de lana (marca de Sami)
            if (baby) c.Rect(24, 28, 3, 2, new Color32(230, 70, 90, 255));
            // Manta tejida
            c.Rect(8, 13, 10, 3, baby ? new Color32(230, 120, 60, 255) : PonchoRed);
            return Build(key, c, baby ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0f));
        }

        // =====================================================================
        //  EDIFICIOS
        // =====================================================================
        /// <summary>Casa de adobe. level 0 = basica, 1 = sellada con ichu, 2 = Muro Trombe.</summary>
        public static Sprite House(int level)
        {
            string key = "house_" + level;
            if (Cached(key, out var s)) return s;
            var rng = new System.Random(400 + level);
            int w = 96, h = 88;
            var c = new Canvas(w, h);

            // Cuerpo de adobe
            c.Rect(6, 0, w - 12, 52, Adobe);
            c.Rect(6, 0, w - 12, 6, AdobeDark);
            c.Noise(7, 2, w - 14, 48, AdobeDark, 0.05f, rng);
            c.Noise(7, 2, w - 14, 48, AdobeLight, 0.04f, rng);
            // Lineas de bloques de adobe
            for (int y = 10; y < 52; y += 10) c.Line(7, y, w - 8, y, AdobeDark);

            // Techo de ichu (mas grueso segun nivel)
            int roofH = 26 + level * 3;
            for (int j = 0; j < roofH; j++)
            {
                float t = 1f - (float)j / roofH;
                int hw = Mathf.RoundToInt((w / 2 - 2) * t);
                for (int i = w / 2 - hw; i <= w / 2 + hw; i++)
                    c.Set(i, 52 + j, (j % 4 == 0) ? IchuDark : Ichu);
            }
            if (level >= 1)
            {
                // Ichu extra amarrado en el alero
                for (int i = 8; i < w - 8; i += 3)
                    c.Line(i, 52, i + rng.Next(-2, 3), 46 - rng.Next(0, 4), IchuLight);
            }

            // Puerta
            c.Rect(14, 0, 14, 26, new Color32(74, 50, 34, 255));
            c.Rect(15, 1, 12, 24, new Color32(96, 66, 44, 255));
            c.Set(25, 12, new Color32(50, 40, 30, 255));
            if (level >= 1)
            {
                // Cortina termica
                c.Rect(13, 18, 16, 9, PonchoRed);
                c.Rect(13, 22, 16, 2, IchuLight);
            }

            // Ventana
            c.Rect(58, 24, 16, 14, new Color32(40, 34, 30, 255));
            c.Rect(59, 25, 14, 12, new Color32(70, 58, 46, 255));
            if (level >= 1) { c.Rect(59, 25, 14, 12, new Color32(150, 120, 80, 255)); }

            if (level >= 2)
            {
                // MURO TROMBE: muro de piedra oscura + vidrio + marco de madera
                c.Rect(40, 2, 42, 44, new Color32(34, 34, 40, 255));      // masa termica oscura
                c.Rect(42, 4, 38, 40, new Color32(120, 170, 210, 200));   // vidrio
                for (int i = 42; i < 80; i += 9) c.Line(i, 4, i, 43, new Color32(180, 210, 235, 220));
                for (int j = 4; j < 44; j += 9) c.Line(42, j, 79, j, new Color32(180, 210, 235, 220));
                c.RectOutline(40, 2, 42, 44, new Color32(92, 62, 40, 255));
                // Rejillas de termocirculacion
                c.Rect(43, 45, 36, 2, new Color32(60, 50, 44, 255));
                c.Rect(43, 1, 36, 2, new Color32(60, 50, 44, 255));
            }
            return Build(key, c, new Vector2(0.5f, 0f));
        }

        /// <summary>Cobertizo de alpacas. 0 = pirca abierta, 1 = techado con ichu, 2 = modulo cerrado.</summary>
        public static Sprite Shed(int level)
        {
            string key = "shed_" + level;
            if (Cached(key, out var s)) return s;
            var rng = new System.Random(500 + level);
            int w = 160, h = 92;
            var c = new Canvas(w, h);

            // Pirca de piedra (siempre)
            for (int x = 2; x < w - 2; x += 9)
                for (int y = 0; y < 26; y += 7)
                {
                    int off = (y / 7) % 2 == 0 ? 0 : 4;
                    Color32 st = rng.NextDouble() < 0.5 ? Stone : StoneDark;
                    c.Rect(x + off, y, 8, 6, st);
                    c.Rect(x + off, y + 5, 8, 1, new Color32(50, 52, 60, 255));
                }

            if (level >= 1)
            {
                // Postes de eucalipto
                c.Rect(8, 26, 5, 40, new Color32(110, 78, 48, 255));
                c.Rect(w - 13, 26, 5, 40, new Color32(110, 78, 48, 255));
                c.Rect(w / 2 - 2, 26, 5, 40, new Color32(110, 78, 48, 255));
                // Techo a dos aguas de ichu
                int roofH = 24;
                for (int j = 0; j < roofH; j++)
                {
                    float t = 1f - (float)j / roofH;
                    int hw = Mathf.RoundToInt((w / 2 - 4) * t);
                    for (int i = w / 2 - hw; i <= w / 2 + hw; i++)
                        c.Set(i, 64 + j, (j % 4 == 0) ? IchuDark : Ichu);
                }
                c.Rect(6, 62, w - 12, 4, new Color32(92, 66, 42, 255)); // viga
            }

            if (level >= 2)
            {
                // Cerramiento de adobe + cortina + area de crias
                c.Rect(2, 26, w - 4, 36, Adobe);
                c.Noise(4, 28, w - 8, 32, AdobeDark, 0.05f, rng);
                c.Rect(2, 26, w - 4, 3, AdobeDark);
                // Puerta con cortina termica
                c.Rect(w / 2 - 18, 26, 36, 30, new Color32(70, 48, 34, 255));
                c.Rect(w / 2 - 16, 28, 32, 26, PonchoRed);
                c.Rect(w / 2 - 16, 36, 32, 3, IchuLight);
                c.Rect(w / 2 - 16, 44, 32, 3, PonchoBlue);
                // Ventanita del area de crias
                c.Rect(18, 40, 14, 12, new Color32(255, 200, 120, 255));
                c.RectOutline(18, 40, 14, 12, new Color32(70, 48, 34, 255));
                c.Rect(w - 32, 40, 14, 12, new Color32(255, 200, 120, 255));
                c.RectOutline(w - 32, 40, 14, 12, new Color32(70, 48, 34, 255));
                // Cama de ichu en el piso
                c.Rect(4, 26, w - 8, 4, IchuLight);
            }
            return Build(key, c, new Vector2(0.5f, 0f));
        }

        /// <summary>Posta de salud comunal (botiquin andino).</summary>
        public static Sprite HealthPost()
        {
            string key = "post";
            if (Cached(key, out var s)) return s;
            var rng = new System.Random(600);
            int w = 88, h = 80;
            var c = new Canvas(w, h);
            c.Rect(4, 0, w - 8, 48, new Color32(208, 196, 172, 255));
            c.Noise(6, 2, w - 12, 44, new Color32(184, 172, 150, 255), 0.05f, rng);
            int roofH = 24;
            for (int j = 0; j < roofH; j++)
            {
                float t = 1f - (float)j / roofH;
                int hw = Mathf.RoundToInt((w / 2 - 2) * t);
                for (int i = w / 2 - hw; i <= w / 2 + hw; i++)
                    c.Set(i, 48 + j, (j % 4 == 0) ? IchuDark : Ichu);
            }
            c.Rect(12, 0, 14, 26, new Color32(74, 50, 34, 255));
            c.Rect(50, 18, 18, 16, new Color32(255, 214, 150, 255));
            c.RectOutline(50, 18, 18, 16, new Color32(74, 50, 34, 255));
            // Cruz de la posta
            c.Rect(w / 2 - 3, 54, 6, 16, new Color32(70, 150, 90, 255));
            c.Rect(w / 2 - 8, 59, 16, 6, new Color32(70, 150, 90, 255));
            return Build(key, c, new Vector2(0.5f, 0f));
        }

        /// <summary>Almacen de lena y bosta seca.</summary>
        public static Sprite Storehouse()
        {
            string key = "store";
            if (Cached(key, out var s)) return s;
            var rng = new System.Random(700);
            int w = 76, h = 62;
            var c = new Canvas(w, h);
            c.Rect(2, 0, w - 4, 36, new Color32(112, 82, 52, 255));
            for (int y = 2; y < 36; y += 6) c.Line(3, y, w - 4, y, new Color32(86, 62, 40, 255));
            int roofH = 20;
            for (int j = 0; j < roofH; j++)
            {
                float t = 1f - (float)j / roofH;
                int hw = Mathf.RoundToInt((w / 2 - 2) * t);
                for (int i = w / 2 - hw; i <= w / 2 + hw; i++)
                    c.Set(i, 36 + j, (j % 3 == 0) ? IchuDark : Ichu);
            }
            // Lena apilada dentro
            for (int k = 0; k < 16; k++)
            {
                int x = rng.Next(6, w - 14), y = rng.Next(2, 20);
                c.Rect(x, y, 8, 4, new Color32(140, 98, 58, 255));
                c.Set(x, y + 1, new Color32(90, 62, 36, 255));
            }
            return Build(key, c, new Vector2(0.5f, 0f));
        }

        // =====================================================================
        //  OBJETOS DEL MAPA
        // =====================================================================
        public static Sprite IchuBush()
        {
            string key = "pick_ichu";
            if (Cached(key, out var s)) return s;
            var rng = new System.Random(800);
            var c = new Canvas(32, 32);
            for (int k = 0; k < 26; k++)
            {
                int bx = 16 + rng.Next(-9, 10);
                int top = rng.Next(14, 30);
                Color32 col = k % 3 == 0 ? IchuLight : (k % 3 == 1 ? Ichu : IchuDark);
                c.Line(bx, 0, bx + rng.Next(-6, 7), top, col);
            }
            return Build(key, c, new Vector2(0.5f, 0f));
        }

        public static Sprite FirewoodPile()
        {
            string key = "pick_wood";
            if (Cached(key, out var s)) return s;
            var c = new Canvas(30, 20);
            Color32 log = new Color32(140, 98, 58, 255);
            Color32 logD = new Color32(96, 64, 38, 255);
            c.Rect(2, 0, 26, 5, log); c.Rect(2, 4, 26, 1, logD);
            c.Rect(4, 5, 22, 5, log); c.Rect(4, 9, 22, 1, logD);
            c.Rect(8, 10, 14, 5, log); c.Rect(8, 14, 14, 1, logD);
            c.Set(3, 2, new Color32(210, 180, 130, 255));
            c.Set(26, 7, new Color32(210, 180, 130, 255));
            // Bosta seca al costado
            c.Rect(0, 0, 6, 4, new Color32(92, 74, 52, 255));
            return Build(key, c, new Vector2(0.5f, 0f));
        }

        public static Sprite HerbPlant()
        {
            string key = "pick_herb";
            if (Cached(key, out var s)) return s;
            var rng = new System.Random(900);
            var c = new Canvas(24, 24);
            for (int k = 0; k < 12; k++)
            {
                int bx = 12 + rng.Next(-6, 7);
                int top = rng.Next(8, 18);
                c.Line(12, 0, bx, top, new Color32(80, 130, 74, 255));
                c.Set(bx, top, new Color32(150, 200, 140, 255));
                if (k % 3 == 0) c.Rect(bx - 1, top, 3, 3, new Color32(220, 230, 200, 255)); // flor wira-wira
            }
            return Build(key, c, new Vector2(0.5f, 0f));
        }

        public static Sprite StoneChunk()
        {
            string key = "pick_stone";
            if (Cached(key, out var s)) return s;
            var c = new Canvas(26, 18);
            c.Rect(2, 0, 20, 10, Stone);
            c.Rect(4, 8, 14, 6, StoneLight);
            c.Rect(2, 0, 20, 2, StoneDark);
            c.Rect(18, 0, 7, 6, StoneDark);
            return Build(key, c, new Vector2(0.5f, 0f));
        }

        public static Sprite Campfire(int frame)
        {
            string key = "fire_" + frame;
            if (Cached(key, out var s)) return s;
            var c = new Canvas(40, 44);
            // Circulo de piedras
            for (int i = 0; i < 5; i++)
                c.Rect(3 + i * 7, 0, 6, 5, i % 2 == 0 ? Stone : StoneDark);
            // Lenos cruzados
            c.Line(8, 4, 30, 12, new Color32(110, 76, 44, 255));
            c.Line(30, 4, 8, 12, new Color32(140, 98, 58, 255));
            // Llama animada
            int extra = frame * 4;
            c.Triangle(20, 6, 10, 16 + extra, FireOrange);
            c.Triangle(20, 8, 6, 13 + extra, FireYellow);
            c.Triangle(20, 10, 3, 9 + extra, new Color32(255, 250, 220, 255));
            if (frame == 2) { c.Set(14, 26, FireOrange); c.Set(27, 24, FireYellow); }
            return Build(key, c, new Vector2(0.5f, 0f));
        }

        public static Sprite Bridge()
        {
            string key = "bridge";
            if (Cached(key, out var s)) return s;
            int w = 320, h = 40;
            var c = new Canvas(w, h);
            c.Rect(0, 24, w, 8, new Color32(122, 86, 52, 255));
            for (int x = 0; x < w; x += 12) c.Line(x, 24, x, 31, new Color32(92, 62, 38, 255));
            // Barandas
            c.Rect(0, 32, w, 3, new Color32(104, 72, 44, 255));
            for (int x = 6; x < w; x += 40) c.Rect(x, 24, 4, 14, new Color32(86, 58, 36, 255));
            // Vigas de soporte
            c.Rect(20, 0, 6, 24, new Color32(86, 58, 36, 255));
            c.Rect(w - 26, 0, 6, 24, new Color32(86, 58, 36, 255));
            return Build(key, c, new Vector2(0.5f, 0f));
        }

        public static Sprite Water(bool frozen)
        {
            string key = "water_" + frozen;
            if (Cached(key, out var s)) return s;
            var rng = new System.Random(frozen ? 11 : 12);
            int w = 128, h = 48;
            var c = new Canvas(w, h);
            Color32 deep = frozen ? new Color32(150, 185, 220, 255) : new Color32(48, 92, 128, 255);
            Color32 top = frozen ? new Color32(200, 226, 245, 255) : new Color32(88, 142, 180, 255);
            c.Rect(0, 0, w, h, deep);
            c.Rect(0, h - 8, w, 8, top);
            for (int k = 0; k < 40; k++)
            {
                int x = rng.Next(0, w), y = rng.Next(0, h - 6);
                c.Rect(x, y, rng.Next(4, 12), 1, frozen ? Glacier : new Color32(120, 170, 205, 255));
            }
            return Build(key, c, new Vector2(0.5f, 1f));
        }

        public static Sprite Snowflake()
        {
            string key = "snowflake";
            if (Cached(key, out var s)) return s;
            var c = new Canvas(4, 4);
            c.Rect(1, 0, 2, 4, Glacier);
            c.Rect(0, 1, 4, 2, Glacier);
            return Build(key, c, new Vector2(0.5f, 0.5f));
        }

        /// <summary>Icono cuadrado simple para el HUD.</summary>
        public static Sprite Icon(string kind)
        {
            string key = "icon_" + kind;
            if (Cached(key, out var s)) return s;
            var c = new Canvas(16, 16);
            switch (kind)
            {
                case "ichu":
                    for (int k = 0; k < 8; k++) c.Line(8, 1, 2 + k * 2, 14, k % 2 == 0 ? Ichu : IchuLight);
                    break;
                case "wood":
                    c.Rect(1, 3, 14, 4, new Color32(140, 98, 58, 255));
                    c.Rect(3, 8, 10, 4, new Color32(110, 76, 44, 255));
                    break;
                case "herb":
                    c.Line(8, 1, 8, 12, new Color32(80, 130, 74, 255));
                    c.Rect(3, 8, 4, 4, new Color32(120, 180, 110, 255));
                    c.Rect(9, 10, 4, 4, new Color32(120, 180, 110, 255));
                    break;
                case "stone":
                    c.Rect(2, 2, 12, 9, Stone);
                    c.Rect(4, 8, 8, 5, StoneLight);
                    break;
                case "people":
                    c.Rect(3, 1, 4, 8, PonchoBrown); c.Rect(3, 9, 4, 4, SkinTone);
                    c.Rect(9, 1, 4, 8, PonchoRed); c.Rect(9, 9, 4, 4, SkinTone);
                    break;
                case "alpaca":
                    c.Rect(2, 4, 10, 6, WoolWhite);
                    c.Rect(10, 8, 4, 6, WoolWhite);
                    c.Rect(3, 0, 2, 4, WoolWhite); c.Rect(9, 0, 2, 4, WoolWhite);
                    break;
                case "heat":
                    c.Triangle(8, 1, 5, 13, FireOrange);
                    c.Triangle(8, 3, 3, 8, FireYellow);
                    break;
                default:
                    c.Rect(2, 2, 12, 12, Glacier);
                    break;
            }
            return Build(key, c, new Vector2(0.5f, 0.5f));
        }

        /// <summary>Panel redondeado simple para UI (con borde).</summary>
        public static Sprite UIPanel(Color32 fill, Color32 border)
        {
            string key = $"uipanel_{fill.r}_{fill.g}_{fill.b}_{fill.a}_{border.r}_{border.g}_{border.b}";
            if (Cached(key, out var s)) return s;
            int w = 24, h = 24;
            var c = new Canvas(w, h);
            c.Rect(0, 0, w, h, fill);
            c.RectOutline(0, 0, w, h, border);
            c.RectOutline(1, 1, w - 2, h - 2, border);
            // Esquinas transparentes (efecto redondeado)
            c.Set(0, 0, Transparent); c.Set(w - 1, 0, Transparent);
            c.Set(0, h - 1, Transparent); c.Set(w - 1, h - 1, Transparent);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels32(c.Px);
            tex.Apply();
            var sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PPU, 0,
                                   SpriteMeshType.FullRect, new Vector4(4, 4, 4, 4));
            sp.name = key;
            _cache[key] = sp;
            return sp;
        }

        public static void ResetRandom() => _rng = new System.Random(20260913);
    }
}
