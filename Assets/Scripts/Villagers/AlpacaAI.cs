using UnityEngine;
using FrioAndino.Art;
using FrioAndino.Core;
using FrioAndino.Environment;

namespace FrioAndino.Villagers
{
    /// <summary>
    /// Alpaca del rebano. Pastorea tranquila y se ARREA como en la puna de verdad:
    /// cuando Inti se le pega por detras, ella avanza unos pasos hacia adelante y
    /// vuelve a pastar. No lo sigue ni huye en estampida, y ademas se aparta de sus
    /// companeras para que el rebano se mantenga repartido y no se apelotone.
    /// Esa es la mecanica de arreo (ODS 13): empujarlas hasta el cobertizo antes del
    /// atardecer y contra reloj.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class AlpacaAI : MonoBehaviour
    {
        [Header("Comportamiento (pixeles del arte por segundo)")]
        [SerializeField] private float grazePixels = 16f;
        [Tooltip("Velocidad del paso corto cuando el pastor la arrea.")]
        [SerializeField] private float nudgePixels = 36f;
        [Tooltip("Si Inti se acerca mas que esto, la alpaca avanza unos pasos.")]
        [SerializeField] private float nudgeDistancePixels = 38f;
        [Tooltip("Cuanto avanza de un arreo antes de volver a pastar.")]
        [SerializeField] private float nudgeStepPixels = 48f;
        [Tooltip("Espacio personal: si otra alpaca esta mas cerca, se aparta.")]
        [SerializeField] private float spacingPixels = 24f;
        [SerializeField] private bool isBaby;

        private SpriteRenderer _sr;
        private ThermalBuilding _shed;
        private float _shedX;
        private Transform _player;
        private float _groundY, _minX, _maxX;
        private float _grazeTimer, _targetX, _bleatTimer, _walkPhase;
        private Vector3 _baseScale;
        private bool _dead;
        private SpriteAnimator _anim;
        private bool _animated;
        private float _nudgeCooldown;

        /// <summary>Rebano vivo: sirve para que ninguna alpaca se monte encima de otra.</summary>
        private static readonly System.Collections.Generic.List<AlpacaAI> Rebano =
            new System.Collections.Generic.List<AlpacaAI>();

        public bool IsBaby => isBaby;
        public bool IsDead => _dead;

        /// <summary>True si esta dentro del cobertizo y este ya tiene techo.</summary>
        public bool IsSheltered
        {
            get
            {
                if (_shed == null || _shed.Level < 1) return false;
                return Mathf.Abs(transform.position.x - _shedX) <= GameArt.U(26f);
            }
        }

        public void Configure(bool baby, ThermalBuilding shed, float shedWorldX)
        {
            isBaby = baby;
            _shed = shed;
            _shedX = shedWorldX;
            _sr = GetComponent<SpriteRenderer>();
            _sr.sprite = baby ? GameArt.AlpacaBaby : GameArt.Alpaca;

            // Tamano medido en pixeles del arte, para que calcen con el camino pintado
            float objetivoPx = baby ? 32f : 44f;
            float texPx = _sr.sprite != null ? _sr.sprite.rect.height : 16f;
            transform.localScale = Vector3.one * (objetivoPx / texPx) * GameArt.Scale;
            _baseScale = transform.localScale;

            // Hojas de animacion (alpaca_idle.bytes / alpaca_walk.bytes), si estan
            _anim = gameObject.AddComponent<SpriteAnimator>();
            _animated = GameArt.LoadAlpacaClips(_anim, baby, objetivoPx) > 0;
            if (_animated) { transform.localScale = Vector3.one * GameArt.Scale; _baseScale = transform.localScale; }
        }

        private void Awake()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr.sprite == null) _sr.sprite = GameArt.Alpaca;
            if (_baseScale == Vector3.zero) _baseScale = Vector3.one * GameArt.Scale;
        }

        private void OnEnable() { if (!Rebano.Contains(this)) Rebano.Add(this); }
        private void OnDisable() { Rebano.Remove(this); }

        private void Start()
        {
            _groundY = transform.position.y;
            _targetX = transform.position.x;
            _minX = GameArt.MinWalkX;
            _maxX = GameArt.MaxWalkX;
            _bleatTimer = Random.Range(5f, 22f);
            HealthShelterManager.Instance?.RegisterAlpaca(this);
            _player = GameManager.Instance != null ? GameManager.Instance.PlayerTransform : null;
        }

        private void Update()
        {
            if (_dead) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
            if (_player == null && GameManager.Instance != null) _player = GameManager.Instance.PlayerTransform;

            float speed = 0f, dir = 0f;
            if (_nudgeCooldown > 0f) _nudgeCooldown -= Time.deltaTime;

            // ---- Arreo de verdad: el pastor EMPUJA, la alpaca no lo sigue ----
            // Inti se le pega por un lado y ella avanza un tramo corto hacia el otro,
            // sin estampida y sin pegarse a el (si lo siguieran, todo el rebano
            // terminaria amontonado encima del jugador).
            if (_player != null && !IsSheltered && _nudgeCooldown <= 0f)
            {
                float dx = transform.position.x - _player.position.x;   // signo: lejos de Inti
                if (Mathf.Abs(dx) <= GameArt.U(nudgeDistancePixels))
                {
                    float haciaAfuera = Mathf.Abs(dx) < 0.0001f ? (Random.value < 0.5f ? -1f : 1f) : Mathf.Sign(dx);
                    _targetX = Mathf.Clamp(transform.position.x + haciaAfuera * GameArt.U(nudgeStepPixels), _minX, _maxX);
                    _grazeTimer = Random.Range(1.5f, 3f);
                    _nudgeCooldown = 0.45f;                              // no se re-empuja cada frame
                }
            }

            // ---- Espacio personal: el rebano se reparte en vez de apelotonarse ----
            float hueco = GameArt.U(spacingPixels);
            for (int i = 0; i < Rebano.Count; i++)
            {
                var otra = Rebano[i];
                if (otra == null || otra == this || otra._dead) continue;
                float sep = transform.position.x - otra.transform.position.x;
                if (Mathf.Abs(sep) < hueco)
                {
                    float lado = Mathf.Abs(sep) < 0.0001f ? (GetInstanceID() < otra.GetInstanceID() ? -1f : 1f) : Mathf.Sign(sep);
                    dir = lado;
                    speed = GameArt.U(grazePixels) * 1.6f;
                    _targetX = transform.position.x + lado * hueco;
                    break;
                }
            }

            // ---- Pastoreo tranquilo / camino al punto marcado ----
            if (speed <= 0f)
            {
                _grazeTimer -= Time.deltaTime;
                if (_grazeTimer <= 0f)
                {
                    _grazeTimer = Random.Range(3f, 8f);
                    _targetX = transform.position.x + Random.Range(-GameArt.U(20f), GameArt.U(20f));
                }
                float falta = Mathf.Abs(_targetX - transform.position.x);
                if (falta > GameArt.U(5f))
                {
                    dir = Mathf.Sign(_targetX - transform.position.x);
                    // Si le falta bastante es que la acaban de arrear: trota ese tramo.
                    // Si solo esta picando pasto al lado, va al paso lento de siempre.
                    speed = GameArt.U(falta > GameArt.U(nudgeStepPixels) * 0.45f ? nudgePixels : grazePixels);
                }
            }

            if (_animated)
            {
                // Con hojas de animacion: los cuadros ponen el movimiento, aqui solo
                // se avanza, se voltea y se elige el clip.
                if (speed > 0f)
                {
                    float nx = Mathf.Clamp(transform.position.x + dir * speed * Time.deltaTime, _minX, _maxX);
                    transform.position = new Vector3(nx, _groundY, transform.position.z);
                    transform.localScale = new Vector3(Mathf.Abs(_baseScale.x) * (dir < 0f ? -1f : 1f),
                                                       _baseScale.y, _baseScale.z);
                    _anim.Paused = false;
                    _anim.Play("walk");
                    _anim.SpeedScale = speed > GameArt.U(grazePixels) * 1.5f ? 1.4f : 1f;
                }
                else
                {
                    transform.position = new Vector3(transform.position.x, _groundY, transform.position.z);
                    _anim.SpeedScale = 1f;
                    _anim.Paused = false;
                    if (_anim.Has("idle")) _anim.Play("idle");
                    else { _anim.Play("walk", true); _anim.Paused = true; }
                }
            }
            else if (speed > 0f)
            {
                float nx = Mathf.Clamp(transform.position.x + dir * speed * Time.deltaTime, _minX, _maxX);
                _walkPhase += Time.deltaTime * 9f;
                float step = Mathf.Abs(Mathf.Sin(_walkPhase));
                transform.position = new Vector3(nx, _groundY + step * GameArt.U(1.2f), transform.position.z);
                transform.localScale = new Vector3(_baseScale.x * (dir < 0f ? -1f : 1f),
                                                   _baseScale.y * (1f - step * 0.03f), _baseScale.z);
            }
            else
            {
                transform.position = new Vector3(transform.position.x, _groundY, transform.position.z);
            }

            _bleatTimer -= Time.deltaTime;
            if (_bleatTimer <= 0f)
            {
                _bleatTimer = Random.Range(10f, 28f);
                Audio.AudioDirector.Instance?.Bleat();
            }

            // Tinte azulado cuando pasa frio a la intemperie
            var ttm = TimeTemperatureManager.Instance;
            if (ttm != null)
            {
                bool cold = ttm.CurrentTemperature < 0f && !IsSheltered;
                _sr.color = Color.Lerp(_sr.color, cold ? new Color(0.70f, 0.79f, 0.96f) : Color.white, Time.deltaTime * 2f);
            }
        }

        public void Die()
        {
            _dead = true;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Llamado de arreo colectivo (con Pututo [Q] o Silbido [F]).
        /// Las alpacas cercanas se alertan y avanzan en tropa hacia el cobertizo comunal.
        /// </summary>
        public static int HerdCallAll(Vector3 callerPos, float radiusUnits)
        {
            int count = 0;
            for (int i = 0; i < Rebano.Count; i++)
            {
                var a = Rebano[i];
                if (a == null || a._dead || a.IsSheltered) continue;
                float dist = Mathf.Abs(callerPos.x - a.transform.position.x);
                if (dist <= radiusUnits)
                {
                    a.HerdTowardsShed();
                    count++;
                }
            }
            if (count > 0)
            {
                Audio.AudioDirector.Instance?.Bleat();
            }
            return count;
        }

        /// <summary>Orienta y hace avanzar a la alpaca en direccion al cobertizo.</summary>
        public void HerdTowardsShed()
        {
            if (_dead || IsSheltered) return;
            float dir = Mathf.Sign(_shedX - transform.position.x);
            _targetX = Mathf.Clamp(transform.position.x + dir * GameArt.U(nudgeStepPixels * 1.8f), _minX, _maxX);
            _grazeTimer = Random.Range(2.2f, 4f);
            _nudgeCooldown = 0.5f;
        }
    }
}
