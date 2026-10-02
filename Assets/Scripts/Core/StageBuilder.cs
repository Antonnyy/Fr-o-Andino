using UnityEngine;
using FrioAndino.Art;
using FrioAndino.Environment;

namespace FrioAndino.Core
{
    /// <summary>
    /// Monta el MAPA PANORAMICO en dos capas, tal como esta disenado el arte:
    ///
    ///   Escenario
    ///   |-- Background_Village   cielo + cordillera + casas + fogon + cobertizo + laguna
    ///   |                        (solo visual, sin colisiones)
    ///   |-- Foreground_Ground    camino con nieve/ichu + muro de pirca + puente
    ///                            (superficie fisica a 0 grados)
    ///
    /// El mapa NO se estira para caber en pantalla: se ajusta a la ALTURA de la camara y
    /// se extiende a lo ancho, asi queda mas ancho que la pantalla y hay que caminarlo.
    /// Las dos capas van a la misma velocidad (sin parallax entre ellas) porque son
    /// una sola pintura: si se desincronizan, las casas dejan de calzar con el camino.
    ///
    /// Coordenadas del arte (pixeles del mapa compuesto, origen arriba-izquierda):
    ///   ancho total        579
    ///   alto total         161
    ///   fondo_villa.bytes  579 x 100   -> su borde inferior esta en y=100
    ///   suelo.bytes        579 x  67   -> su borde inferior esta en y=161
    ///   linea de pisada    y=112       (sobre el camino, justo encima de la pirca)
    /// </summary>
    public static class StageBuilder
    {
        // ---- Medidas del mapa compuesto, en pixeles del arte ----
        public const float MapPixelWidth = 805f;
        public const float MapPixelHeight = 220f;
        /// <summary>Donde pisan los personajes, en pixeles del arte.</summary>
        public const float WalkPixelY = 140f;
        /// <summary>Borde inferior de la capa de fondo, en pixeles del arte.</summary>
        public const float BackgroundBottomPixelY = 140f;
        /// <summary>Borde inferior de la capa de suelo, en pixeles del arte.</summary>
        public const float GroundBottomPixelY = 220f;

        // ---- Tamano de los personajes, en pixeles del arte ----
        public const float PlayerPixelHeight = 72f;
        public const float VillagerPixelHeight = 65f;

        public class Stage
        {
            public Transform Root;
            public GameObject Background;
            public GameObject Foreground;
            public GroundSurface Ground;
            public float LeftX, RightX;
            public float CameraY;
        }

        public static Stage Build(Transform parent, Camera cam)
        {
            if (cam == null) return null;

            var spFondo = GameArt.Panorama;
            var spSuelo = GameArt.GroundStrip;
            if (spFondo == null || spSuelo == null) return null;   // -> fondo unico de siempre

            // =============================================================
            //  1. Mapeo pixel del arte -> unidades de mundo
            // =============================================================
            GameArt.ConfigureStage(MapPixelWidth, MapPixelHeight, WalkPixelY,
                                   PlayerPixelHeight, VillagerPixelHeight);

            // El alto del mapa llena exactamente el alto de la camara.
            // El ancho queda libre: por eso el mapa es mas ancho que la pantalla.
            float viewHeight = cam.orthographicSize * 2f;
            float unitsPerPixel = viewHeight / MapPixelHeight;
            GameArt.SetScale(unitsPerPixel * GameArt.PPU);

            var stage = new Stage();
            stage.LeftX = GameArt.X(0f);
            stage.RightX = GameArt.X(MapPixelWidth);

            // La camara se centra verticalmente entre el techo y el pie del mapa
            stage.CameraY = (GameArt.Y(0f) + GameArt.Y(MapPixelHeight)) * 0.5f;

            var root = new GameObject("Escenario").transform;
            root.SetParent(parent, false);
            root.localPosition = Vector3.zero;
            stage.Root = root;

            // =============================================================
            //  2. CAPA SUPERIOR - Background_Village (solo visual)
            // =============================================================
            {
                var go = new GameObject("Background_Village");
                go.transform.SetParent(root, false);
                go.transform.position = new Vector3(0f, GameArt.Y(BackgroundBottomPixelY), 0f);
                go.transform.localScale = Vector3.one * GameArt.Scale;

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spFondo;
                SetSorting(sr, "Fondo", -200);

                stage.Background = go;
            }

            // =============================================================
            //  3. CAPA INFERIOR - Foreground_Ground (camino + pirca + puente)
            // =============================================================
            {
                var go = new GameObject("Foreground_Ground");
                go.transform.SetParent(root, false);
                go.transform.position = new Vector3(0f, GameArt.Y(GroundBottomPixelY), 0f);
                go.transform.localScale = Vector3.one * GameArt.Scale;

                int capaSuelo = LayerMask.NameToLayer("Suelo");
                if (capaSuelo >= 0) go.layer = capaSuelo;

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spSuelo;
                SetSorting(sr, "Suelo", -50);

                stage.Foreground = go;
            }

            // =============================================================
            //  4. Fisica del camino: una sola recta a 0 grados de punta a punta
            // =============================================================
            {
                var go = new GameObject("Fisica_Camino");
                go.transform.SetParent(root, false);
                go.transform.position = Vector3.zero;

                var ground = go.AddComponent<GroundSurface>();
                float grosor = GameArt.U(MapPixelHeight - WalkPixelY);

                // Un solo tramo continuo: el camino de la imagen no tiene huecos,
                // el puente es parte de la misma superficie.
                ground.Build(0f, grosor, new Vector2(stage.LeftX, stage.RightX));
                ground.AddBordes(stage.LeftX, stage.RightX, GameArt.U(MapPixelHeight));

                stage.Ground = ground;
            }

            Debug.Log($"[FrioAndino] Mapa panoramico montado: {MapPixelWidth}x{MapPixelHeight}px del arte, " +
                      $"mundo de {stage.LeftX:0.0} a {stage.RightX:0.0} ({stage.RightX - stage.LeftX:0.0} u), " +
                      $"pantalla {cam.orthographicSize * 2f * cam.aspect:0.0} u de ancho.");

            return stage;
        }

        /// <summary>
        /// Asigna Sorting Layer por nombre solo si existe en el proyecto; si no, cae a
        /// Default con el mismo orden. Asi el codigo no revienta en proyectos sin capas.
        /// </summary>
        public static void SetSorting(SpriteRenderer sr, string capa, int orden)
        {
            if (sr == null) return;
            if (ExisteSortingLayer(capa)) sr.sortingLayerName = capa;
            sr.sortingOrder = orden;
        }

        private static bool ExisteSortingLayer(string nombre)
        {
            var capas = SortingLayer.layers;
            for (int i = 0; i < capas.Length; i++)
                if (capas[i].name == nombre) return true;
            return false;
        }
    }
}
