using UnityEngine;
using FrioAndino.Player;
using FrioAndino.Audio;
using FrioAndino.Art;

namespace FrioAndino.Economy
{
    /// <summary>
    /// Mata de ichu, pila de lena, hierba medicinal o piedra cosechable.
    /// Reaparece tras un tiempo: la puna se regenera, pero mas lento de lo que la helada avanza.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ResourcePickup : MonoBehaviour, IInteractable
    {
        [Header("Recurso")]
        [SerializeField] private ResourceType type = ResourceType.Ichu;
        [SerializeField] private int amountPerHarvest = 4;
        [Tooltip("Segundos hasta que vuelve a crecer.")]
        [SerializeField] private float respawnTime = 22f;

        private SpriteRenderer _sr;
        private bool _harvested;
        private bool _marker;
        private float _timer;
        private Vector3 _baseScale;
        private float _swayOffset;

        public ResourceType Type => type;

        public void Configure(ResourceType t, int amount, float respawn)
        {
            type = t; amountPerHarvest = amount; respawnTime = respawn;
            ApplySprite();
        }

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _baseScale = transform.localScale;
            _swayOffset = Random.value * 10f;
            ApplySprite();
        }

        private void ApplySprite()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();

            // Con el mapa panoramico NO se dibuja vegetacion nueva: el recurso ES la mata
            // de ichu, la lena o la piedra que ya estan pintadas en el camino. Solo se
            // marca con un destello suave cuando el jugador esta cerca, para que se note
            // que ahi hay algo que cosechar.
            if (GameArt.LayeredStage)
            {
                _sr.sprite = PixelArtFactory.Circle(new Color32(255, 236, 170, 190), 24);
                _sr.sortingOrder = 9;
                _sr.color = new Color(1f, 1f, 1f, 0f);   // arranca invisible
                _marker = true;
                return;
            }

            switch (type)
            {
                case ResourceType.Ichu: _sr.sprite = PixelArtFactory.IchuBush(); break;
                case ResourceType.Firewood: _sr.sprite = PixelArtFactory.FirewoodPile(); break;
                case ResourceType.Herbs: _sr.sprite = PixelArtFactory.HerbPlant(); break;
                case ResourceType.Stone: _sr.sprite = PixelArtFactory.StoneChunk(); break;
            }
        }

        /// <summary>Destello discreto que aparece solo cuando el jugador se acerca.</summary>
        private void UpdateMarker()
        {
            var gm = Core.GameManager.Instance;
            Transform p = gm != null ? gm.PlayerTransform : null;

            float alpha = 0f;
            if (p != null && !_harvested)
            {
                float d = Mathf.Abs(p.position.x - transform.position.x);
                float cerca = GameArt.U(26f);
                if (d < cerca)
                    alpha = Mathf.Lerp(0.55f, 0f, d / cerca) * (0.72f + Mathf.Sin(Time.time * 3.2f) * 0.28f);
            }

            var c = _sr.color; c.a = Mathf.Lerp(c.a, alpha, Time.deltaTime * 8f); _sr.color = c;
            transform.localScale = _baseScale * (0.30f + alpha * 0.25f);
        }

        private void Update()
        {
            if (_marker)
            {
                if (_harvested)
                {
                    _timer += Time.deltaTime;
                    if (_timer >= respawnTime) { _harvested = false; _sr.enabled = true; }
                }
                UpdateMarker();
                return;
            }

            if (_harvested)
            {
                _timer += Time.deltaTime;
                if (_timer >= respawnTime)
                {
                    _harvested = false;
                    _sr.enabled = true;
                    transform.localScale = _baseScale * 0.2f;
                }
                return;
            }

            // Crecimiento suave al reaparecer
            if (transform.localScale.x < _baseScale.x)
                transform.localScale = Vector3.MoveTowards(transform.localScale, _baseScale, Time.deltaTime * 2f);

            // Vaiven por el viento (solo vegetacion)
            if (type == ResourceType.Ichu || type == ResourceType.Herbs)
            {
                float wind = Core.TimeTemperatureManager.Instance != null
                    ? 1f + Core.TimeTemperatureManager.Instance.ColdSeverity * 2f : 1f;
                float ang = Mathf.Sin(Time.time * 1.8f + _swayOffset) * 3.5f * wind;
                transform.localRotation = Quaternion.Euler(0f, 0f, ang);
            }
        }

        // ---------------- IInteractable ----------------
        public string GetPrompt()
        {
            switch (type)
            {
                case ResourceType.Ichu: return $"Cosechar ichu  +{amountPerHarvest}";
                case ResourceType.Firewood: return $"Recoger lena/bosta  +{amountPerHarvest}";
                case ResourceType.Herbs: return $"Recolectar muna/wira-wira  +{amountPerHarvest}";
                default: return $"Picar piedra  +{amountPerHarvest}";
            }
        }

        public bool CanInteract() => !_harvested;

        public string Interact(GameObject actor)
        {
            if (_harvested) return null;
            ResourceManager.Instance?.Add(type, amountPerHarvest);
            AudioDirector.Instance?.Pickup();
            UI.HUDController.Instance?.FloatingText(
                transform.position + Vector3.up * 1.0f,
                $"+{amountPerHarvest} {ResourceManager.DisplayName(type)}",
                new Color32(250, 230, 150, 255));
            _harvested = true;
            _timer = 0f;
            _sr.enabled = false;
            return null;
        }

        public Transform GetTransform() => transform;
    }
}
