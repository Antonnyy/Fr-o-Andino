using UnityEngine;
using FrioAndino.Art;

namespace FrioAndino.Core
{
    /// <summary>
    /// Atmosfera: tinte dia/noche sobre toda la pantalla, nieve/viento blanco y
    /// escarcha en los bordes cuando la temperatura se desploma.
    /// Reemplaza a Light2D de URP con un overlay simple, para que el proyecto corra
    /// en cualquier pipeline sin configuracion previa.
    /// </summary>
    public class WeatherFX : MonoBehaviour
    {
        [Header("Tinte de la helada")]
        [Tooltip("El arte ya es de atardecer: de dia se aclara, de noche se hunde en azul.")]
        [SerializeField] private Color dayTint = new Color(1f, 0.98f, 0.90f, 0.08f);
        [SerializeField] private Color duskTint = new Color(0.95f, 0.58f, 0.24f, 0.15f);
        [SerializeField] private Color nightTint = new Color(0.04f, 0.08f, 0.22f, 0.38f);

        [Header("Nieve / viento blanco")]
        [SerializeField] private int maxFlakes = 260;

        private Camera _cam;
        private SpriteRenderer _tint;
        private SpriteRenderer _vignette;
        private Transform[] _flakes;
        private float[] _flakeSpeed;
        private float[] _flakeSway;
        private SpriteRenderer[] _flakeSr;

        public void Configure(Camera cam) => _cam = cam;

        private void Start()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            float h = _cam.orthographicSize * 2f;
            float w = h * _cam.aspect;

            // --- Overlay de tinte (hijo de la camara, cubre toda la vista) ---
            var tintGo = new GameObject("ScreenTint");
            tintGo.transform.SetParent(_cam.transform, false);
            tintGo.transform.localPosition = new Vector3(0f, 0f, 1f);
            _tint = tintGo.AddComponent<SpriteRenderer>();
            _tint.sprite = PixelArtFactory.Solid(new Color32(255, 255, 255, 255), 8, 8);
            _tint.sortingOrder = 500;
            // El sprite NO mide 1 unidad: hay que dividir por su tamano real o el tinte
            // queda como un rectangulo translucido en medio de la pantalla en vez de cubrirla.
            Escalar(tintGo.transform, _tint.sprite, w * 1.6f, h * 1.6f);
            _tint.color = dayTint;

            // --- Vineta de escarcha en los bordes ---
            var vigGo = new GameObject("FrostVignette");
            vigGo.transform.SetParent(_cam.transform, false);
            vigGo.transform.localPosition = new Vector3(0f, 0f, 0.9f);
            _vignette = vigGo.AddComponent<SpriteRenderer>();
            _vignette.sprite = FrostSprite();
            _vignette.sortingOrder = 501;
            Escalar(vigGo.transform, _vignette.sprite, w * 1.05f, h * 1.05f);
            _vignette.color = new Color(1f, 1f, 1f, 0f);

            // --- Copos ---
            _flakes = new Transform[maxFlakes];
            _flakeSpeed = new float[maxFlakes];
            _flakeSway = new float[maxFlakes];
            _flakeSr = new SpriteRenderer[maxFlakes];
            var flakeSprite = PixelArtFactory.Snowflake();
            var parent = new GameObject("Snow").transform;
            parent.SetParent(transform, false);
            for (int i = 0; i < maxFlakes; i++)
            {
                var go = new GameObject("Flake");
                go.transform.SetParent(parent, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = flakeSprite;
                sr.sortingOrder = 480;
                sr.color = new Color(1f, 1f, 1f, 0.85f);
                go.transform.position = RandomFlakePos();
                go.transform.localScale = Vector3.one * Random.Range(0.6f, 1.6f);
                _flakes[i] = go.transform;
                _flakeSr[i] = sr;
                _flakeSpeed[i] = Random.Range(2.5f, 7f);
                _flakeSway[i] = Random.Range(0f, 10f);
                go.SetActive(false);
            }
        }

        /// <summary>
        /// Escala un objeto para que su sprite ocupe exactamente el tamano pedido en
        /// unidades de mundo, sin importar los PPU con que se creo el sprite.
        /// </summary>
        private static void Escalar(Transform t, Sprite sprite, float anchoUnidades, float altoUnidades)
        {
            if (sprite == null) return;
            Vector2 natural = sprite.bounds.size;
            if (natural.x <= 0.0001f || natural.y <= 0.0001f) return;
            t.localScale = new Vector3(anchoUnidades / natural.x, altoUnidades / natural.y, 1f);
        }

        private Vector3 RandomFlakePos()
        {
            if (_cam == null) return Vector3.zero;
            float h = _cam.orthographicSize * 2f;
            float w = h * _cam.aspect;
            return new Vector3(
                _cam.transform.position.x + Random.Range(-w * 0.7f, w * 0.7f),
                _cam.transform.position.y + Random.Range(-h * 0.5f, h * 0.7f),
                0f);
        }

        private void Update()
        {
            var ttm = TimeTemperatureManager.Instance;
            if (ttm == null || _cam == null) return;

            // ---- Tinte segun fase ----
            Color target;
            if (ttm.Phase == DayPhase.Day)
            {
                float p = ttm.PhaseProgress;
                target = p < 0.75f
                    ? Color.Lerp(dayTint, dayTint, p)
                    : Color.Lerp(dayTint, duskTint, Mathf.InverseLerp(0.75f, 1f, p));
            }
            else
            {
                float p = Mathf.Clamp01(ttm.PhaseProgress * 3f);
                target = Color.Lerp(duskTint, nightTint, p);
            }
            _tint.color = Color.Lerp(_tint.color, target, Time.deltaTime * 1.5f);

            // ---- Escarcha en bordes segun temperatura bajo cero ----
            float frost = Mathf.Clamp01(Mathf.InverseLerp(3f, -18f, ttm.CurrentTemperature));
            var vc = _vignette.color;
            vc.a = Mathf.Lerp(vc.a, frost * 0.85f, Time.deltaTime * 2.5f);
            _vignette.color = vc;

            // ---- Nieve proporcional al frio ----
            float sev = ttm.ColdSeverity;
            int active = Mathf.RoundToInt(Mathf.Clamp01((sev - 0.25f) / 0.75f) * maxFlakes);
            float h2 = _cam.orthographicSize * 2f;
            float w2 = h2 * _cam.aspect;
            float wind = Mathf.Lerp(1.5f, 7f, sev);

            for (int i = 0; i < maxFlakes; i++)
            {
                bool shouldBeOn = i < active;
                if (_flakes[i].gameObject.activeSelf != shouldBeOn)
                {
                    _flakes[i].gameObject.SetActive(shouldBeOn);
                    if (shouldBeOn) _flakes[i].position = RandomFlakePos();
                }
                if (!shouldBeOn) continue;

                var p = _flakes[i].position;
                p.y -= _flakeSpeed[i] * Time.deltaTime;
                p.x -= wind * Time.deltaTime + Mathf.Sin(Time.time * 2f + _flakeSway[i]) * 0.01f;

                float bottom = _cam.transform.position.y - h2 * 0.55f;
                float left = _cam.transform.position.x - w2 * 0.75f;
                if (p.y < bottom || p.x < left)
                {
                    p = RandomFlakePos();
                    p.y = _cam.transform.position.y + h2 * 0.6f;
                }
                _flakes[i].position = p;
            }
        }

        private Sprite FrostSprite()
        {
            int s = 128;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x / (float)s - 0.5f) * 2f;
                    float dy = (y / (float)s - 0.5f) * 2f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01((d - 0.62f) / 0.55f);
                    a *= a;
                    byte alpha = (byte)(a * 255f);
                    px[y * s + x] = new Color32(215, 235, 255, alpha);
                }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
        }
    }
}
