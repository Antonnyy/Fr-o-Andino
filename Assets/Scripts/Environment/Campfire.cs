using UnityEngine;
using FrioAndino.Player;
using FrioAndino.Economy;
using FrioAndino.Audio;
using FrioAndino.Art;

namespace FrioAndino.Environment
{
    /// <summary>
    /// Fogon comunal: corazon termico y moral del caserio.
    /// El fuego esta pintado en el fondo; aqui se le anade el resplandor vivo y, si se
    /// queda sin combustible, se cubre con el parche de ceniza fria.
    /// </summary>
    public class Campfire : MonoBehaviour, IInteractable
    {
        [Header("Combustible")]
        [SerializeField] private float maxFuel = 60f;
        [SerializeField] private float currentFuel = 20f;
        [Tooltip("Consumo por segundo durante el dia.")]
        [SerializeField] private float burnPerSecondDay = 0.22f;
        [Tooltip("Consumo por segundo durante la noche (el frio obliga a avivarlo).")]
        [SerializeField] private float burnPerSecondNight = 1.0f;
        [SerializeField] private int logsPerRefill = 5;

        [Header("Calor")]
        [Tooltip("Radio en unidades donde el fogon aporta calor a las viviendas.")]
        [SerializeField] private float heatRadiusPixels = 150f;

        public static Campfire Instance { get; private set; }

        private SpriteRenderer _glow, _core, _ash;
        private Vector3 _embersPos;

        public float FuelRatio => Mathf.Clamp01(currentFuel / maxFuel);
        public bool IsLit => currentFuel > 0.01f;
        public float HeatRadius => GameArt.U(heatRadiusPixels);

        private void Awake() => Instance = this;

        /// <summary>Coloca el resplandor sobre las brasas pintadas y el parche de ceniza sobre la llama.</summary>
        public void Setup(Vector3 embersPos, Vector3 firePatchPos)
        {
            _embersPos = embersPos;

            _glow = Layer("Resplandor", PixelArtFactory.Circle(new Color32(255, 168, 62, 74), 64), embersPos, 9);
            Medir(_glow, GameArt.U(22f));

            _core = Layer("Brasa", PixelArtFactory.Circle(new Color32(255, 226, 150, 120), 32),
                          embersPos + new Vector3(0f, GameArt.U(2f), 0f), 14);
            Medir(_core, GameArt.U(7f));

            _ash = Layer("Ceniza", GameArt.FireOut, firePatchPos, 15);
            if (_ash.sprite != null)
                _ash.transform.localScale = Vector3.one * (8f / _ash.sprite.rect.height) * GameArt.Scale;
            _ash.enabled = false;
        }

        /// <summary>Escala un sprite para que mida exactamente lo pedido, sea cual sea su PPU.</summary>
        private static void Medir(SpriteRenderer sr, float diametroUnidades)
        {
            if (sr == null || sr.sprite == null) return;
            float nat = sr.sprite.bounds.size.x;
            if (nat <= 0.0001f) return;
            sr.transform.localScale = Vector3.one * (diametroUnidades / nat);
        }

        private SpriteRenderer Layer(string name, Sprite sprite, Vector3 pos, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        private void Update()
        {
            var ttm = Core.TimeTemperatureManager.Instance;
            var gm = Core.GameManager.Instance;
            if (ttm != null && !ttm.Paused && gm != null && gm.IsPlaying)
            {
                float burn = ttm.Phase == Core.DayPhase.Night ? burnPerSecondNight : burnPerSecondDay;
                currentFuel = Mathf.Max(0f, currentFuel - burn * Time.deltaTime);
            }

            bool lit = IsLit;
            if (_ash != null) _ash.enabled = !lit;

            if (_glow != null)
            {
                _glow.enabled = lit;
                _core.enabled = lit;
                if (lit)
                {
                    float flicker = 1f + Mathf.Sin(Time.time * 7.3f) * 0.05f + Mathf.Sin(Time.time * 17f) * 0.02f;
                    float size = GameArt.U(Mathf.Lerp(70f, 190f, FuelRatio)) * flicker;
                    _glow.transform.localScale = Vector3.one * size;
                    _core.transform.localScale = Vector3.one * GameArt.U(Mathf.Lerp(20f, 44f, FuelRatio)) * flicker;

                    var c = _glow.color;
                    c.a = Mathf.Lerp(0.16f, 0.42f, FuelRatio);
                    // De noche el fogon es lo unico que ilumina: se nota mas
                    if (ttm != null && ttm.Phase == Core.DayPhase.Night) c.a *= 1.5f;
                    _glow.color = c;
                }
            }

            var player = gm != null ? gm.PlayerTransform : null;
            if (player != null && AudioDirector.Instance != null)
            {
                float d = Mathf.Abs(player.position.x - _embersPos.x);
                float near = Mathf.Clamp01(1f - d / GameArt.U(180f));
                AudioDirector.Instance.SetFireIntensity(near * (lit ? 1f : 0f));
            }
        }

        /// <summary>Aporte de calor en un punto del mapa (0 = nada, 1 = junto al fuego lleno).</summary>
        public float HeatAt(Vector3 worldPos)
        {
            if (!IsLit) return 0f;
            float d = Mathf.Abs(worldPos.x - _embersPos.x);
            float r = HeatRadius;
            if (d > r) return 0f;
            return (1f - d / r) * Mathf.Clamp01(0.35f + FuelRatio * 0.65f);
        }

        public void AddFuel(int logs) => currentFuel = Mathf.Min(maxFuel, currentFuel + logs * 4.5f);

        // ---------------- IInteractable ----------------
        public string GetPrompt() =>
            IsLit ? $"Avivar el fogon comunal  [{Mathf.RoundToInt(FuelRatio * 100f)}%]  -{logsPerRefill} lena"
                  : $"ENCENDER el fogon comunal  -{logsPerRefill} lena";

        public bool CanInteract() => FuelRatio < 0.99f;

        public string Interact(GameObject actor)
        {
            var rm = ResourceManager.Instance;
            if (rm == null) return null;
            if (!rm.Has(ResourceType.Firewood, logsPerRefill))
                return "Falta lena. Recoge lena y bosta seca por el caserio.";
            bool wasOut = !IsLit;
            rm.Spend(ResourceType.Firewood, logsPerRefill);
            AddFuel(logsPerRefill);
            AudioDirector.Instance?.BuildDone();
            UI.HUDController.Instance?.FloatingText(_embersPos + Vector3.up * GameArt.U(30f), "+Calor",
                                                    new Color32(255, 180, 90, 255));
            return wasOut ? "El fogon comunal volvio a arder." : $"Fogon avivado ({Mathf.RoundToInt(FuelRatio * 100f)}%).";
        }

        public Transform GetTransform() => transform;
    }
}
