using UnityEngine;
using FrioAndino.Art;
using FrioAndino.Core;
using FrioAndino.Environment;

namespace FrioAndino.Villagers
{
    public enum VillagerState { Idle, FollowingLeader, WalkingToTask, Working, RestingInShelter, Seated }

    /// <summary>
    /// Comunero / rondero. Maquina de estados simple:
    /// patrulla -> responde al pututo -> camina a la faena -> trabaja -> se guarece al anochecer.
    ///
    /// Los abuelos (Tomasa y Mariano) ya estan pintados sentados junto al fogon: su entidad
    /// existe para la logica de salud (ODS 3) pero no dibuja sprite propio.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class VillagerAI : MonoBehaviour
    {
        [Header("Movimiento (pixeles del fondo por segundo)")]
        [SerializeField] private float walkPixels = 26f;
        [SerializeField] private float runPixels = 52f;
        [SerializeField] private float patrolPixels = 38f;

        [Header("Estado")]
        [SerializeField] private VillagerState state = VillagerState.Idle;
        [SerializeField] private bool isSick;
        [SerializeField] private bool isElder;

        private SpriteRenderer _sr;
        private Vector3 _home, _target;
        private ThermalBuilding _shelter;
        private float _idleTimer, _coughTimer, _walkPhase;
        private float _groundY, _minX, _maxX;
        private Transform _sickMark;
        private Vector3 _baseScale;
        private bool _seated;
        private Color _baseTint = Color.white;
        private SpriteAnimator _anim;
        private bool _animated;

        [Tooltip("Cuanto tiempo queda atento un comunero despues de oir el pututo.")]
        [SerializeField] private float convocadoDuracion = 30f;
        private float _convocadoTimer;

        public VillagerState State => state;
        public bool IsSick => isSick;
        public bool IsElder => isElder;
        public bool IsSeated => _seated;
        public bool IsAvailable => !_seated && (state == VillagerState.Idle || state == VillagerState.FollowingLeader);

        /// <summary>Oyo el pututo hace poco: es el primero en salir a la faena.</summary>
        public bool IsConvocado => _convocadoTimer > 0f && IsAvailable;
        public ThermalBuilding Shelter => _shelter;

        /// <summary>
        /// Configura al comunero. <paramref name="variant"/> y <paramref name="tint"/> los asigna
        /// GameBootstrap de forma que dos ronderos vecinos NUNCA compartan el mismo look: sin eso,
        /// al reciclar un solo sprite (Inti) en varias comuneros, dos con la misma variante puestos
        /// juntos se ven como "el jugador clonado".
        /// </summary>
        public void Configure(bool elder, bool female, ThermalBuilding shelter, int variant = -1, Color? tint = null)
        {
            isElder = elder;
            _shelter = shelter;
            _sr = GetComponent<SpriteRenderer>();
            int v = variant >= 0 ? variant : Random.Range(0, 2);
            _sr.sprite = GameArt.Villager(v);

            // Hojas de animacion del comunero, si estan puestas en Resources
            _anim = gameObject.AddComponent<SpriteAnimator>();
            _animated = GameArt.LoadVillagerClips(_anim, v) > 0;

            // Con arte propio NO se tine: el tinte solo existia para diferenciar copias
            // del sprite de Inti. Ahora cada variante es un personaje distinto de verdad.
            _baseTint = _animated ? Color.white : (tint ?? Color.white);
            _sr.color = _baseTint;
            _sr.flipX = !_animated && Random.value > 0.5f;
            float size = GameArt.Scale * (elder ? 0.90f : Random.Range(0.93f, 1.03f));
            transform.localScale = Vector3.one * size;
            _baseScale = transform.localScale;
        }

        /// <summary>Convierte al comunero en una figura ya pintada en el fondo (no dibuja sprite).</summary>
        public void MakeSeated(Vector3 position)
        {
            _seated = true;
            state = VillagerState.Seated;
            transform.position = position;
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            _sr.enabled = false;
        }

        private void Awake()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr.sprite == null) _sr.sprite = GameArt.Villager(0);
            if (_baseScale == Vector3.zero) _baseScale = Vector3.one * GameArt.Scale;
        }

        private void Start()
        {
            _home = transform.position;
            _target = _home;
            _groundY = transform.position.y;
            _minX = GameArt.MinWalkX;
            _maxX = GameArt.MaxWalkX;

            VillagerTaskSystem.Instance?.Register(this);
            HealthShelterManager.Instance?.RegisterVillager(this);

            var ttm = TimeTemperatureManager.Instance;
            if (ttm != null)
            {
                ttm.OnNightFall += GoToShelter;
                ttm.OnDawn += OnDawn;
            }

            // Marca de comunero enfermo (ODS 3)
            var go = new GameObject("Enfermo");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PixelArtFactory.Icon("herb");
            sr.color = new Color32(255, 118, 118, 255);
            sr.sortingOrder = 45;
            _sickMark = go.transform;
            _sickMark.localPosition = new Vector3(0f, _seated ? 1.2f : 2.2f, 0f);
            _sickMark.localScale = Vector3.one * 0.9f;
            _sickMark.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            var ttm = TimeTemperatureManager.Instance;
            if (ttm != null)
            {
                ttm.OnNightFall -= GoToShelter;
                ttm.OnDawn -= OnDawn;
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

            if (_convocadoTimer > 0f) _convocadoTimer -= Time.deltaTime;

            if (!_seated)
            {
                switch (state)
                {
                    case VillagerState.Idle: TickIdle(); break;
                    case VillagerState.FollowingLeader: TickFollow(); break;
                    case VillagerState.WalkingToTask: TickWalkToTask(); break;
                    case VillagerState.Working: TickWorking(); break;
                    case VillagerState.RestingInShelter: TickResting(); break;
                }
            }

            if (isSick)
            {
                _coughTimer -= Time.deltaTime;
                if (_coughTimer <= 0f)
                {
                    _coughTimer = Random.Range(7f, 16f);
                    Audio.AudioDirector.Instance?.Cough();
                }
                if (_sickMark != null)
                {
                    var p = _sickMark.localPosition;
                    p.y = (_seated ? 1.2f : 2.2f) + Mathf.Sin(Time.time * 2f) * 0.08f;
                    _sickMark.localPosition = p;
                }
            }
        }

        // ------------- Estados -------------
        private void TickIdle()
        {
            _idleTimer -= Time.deltaTime;
            if (_idleTimer <= 0f)
            {
                _idleTimer = Random.Range(2f, 5f);
                float r = GameArt.U(patrolPixels);
                _target = _home + new Vector3(Random.Range(-r, r), 0f, 0f);
            }
            MoveTowards(_target.x, GameArt.U(walkPixels) * (isSick ? 0.6f : 1f));
        }

        /// <summary>Ya no se usa: los comuneros nunca caminan detras de Inti.</summary>
        private void TickFollow()
        {
            state = VillagerState.Idle;
            Animate(false);
        }

        private void TickWalkToTask()
        {
            MoveTowards(_target.x, GameArt.U(runPixels));
            if (Mathf.Abs(transform.position.x - _target.x) < GameArt.U(14f))
                state = VillagerState.Working;
        }

        private void TickWorking()
        {
            if (_animated && _anim.Has("work"))
            {
                _anim.Paused = false;
                _anim.Play("work");
                transform.localScale = _baseScale;
                transform.position = new Vector3(transform.position.x, _groundY, transform.position.z);
                return;
            }

            float bob = Mathf.Abs(Mathf.Sin(Time.time * 8f)) * GameArt.U(3f);
            transform.position = new Vector3(transform.position.x, _groundY + bob, transform.position.z);
            float squash = 1f - Mathf.Abs(Mathf.Sin(Time.time * 8f)) * 0.05f;
            transform.localScale = new Vector3(_baseScale.x, _baseScale.y * squash, _baseScale.z);
        }

        private void TickResting()
        {
            if (_shelter == null) { Animate(false); return; }
            float tx = _shelter.transform.position.x;
            if (Mathf.Abs(transform.position.x - tx) > GameArt.U(10f)) MoveTowards(tx, GameArt.U(runPixels));
            else { _sr.enabled = false; Animate(false); }   // ya esta dentro de la vivienda
        }

        private void MoveTowards(float targetX, float speed)
        {
            float dx = targetX - transform.position.x;
            if (Mathf.Abs(dx) < GameArt.U(4f)) { Animate(false); return; }
            float dir = Mathf.Sign(dx);
            float nx = Mathf.Clamp(transform.position.x + dir * speed * Time.deltaTime, _minX, _maxX);
            transform.position = new Vector3(nx, _groundY, transform.position.z);
            _sr.flipX = dir < 0f;
            Animate(true);
        }

        private void Animate(bool moving)
        {
            if (_animated)
            {
                transform.localScale = _baseScale;
                transform.position = new Vector3(transform.position.x, _groundY, transform.position.z);
                _anim.Paused = false;

                if (moving)
                {
                    // Si esa variante no trajo hoja de caminar, se mueve con la de quieto
                    if (_anim.Has("walk")) _anim.Play("walk");
                    else _anim.Play("idle");
                    return;
                }

                if (_anim.Has("idle")) _anim.Play("idle");
                else { _anim.Play("walk", true); _anim.Paused = true; }
                return;
            }

            if (moving) _walkPhase += Time.deltaTime * 9f;
            else _walkPhase = Mathf.Lerp(_walkPhase, 0f, Time.deltaTime * 8f);
            float step = Mathf.Abs(Mathf.Sin(_walkPhase));
            transform.localScale = new Vector3(_baseScale.x * (1f + step * 0.025f),
                                               _baseScale.y * (1f - step * 0.03f), _baseScale.z);
            if (moving)
                transform.position = new Vector3(transform.position.x, _groundY + step * GameArt.U(1.4f), transform.position.z);
        }

        // ------------- API publica -------------
        /// <summary>
        /// Escucha el pututo y queda CONVOCADO: sigue en su sitio, atento, esperando que
        /// Inti marque la faena con [E]. No camina detras del jugador — un rebano de
        /// comuneros pegados a la espalda no se ve ni se juega bien.
        /// </summary>
        public void RespondToPututo(Transform leader)
        {
            if (_seated || state == VillagerState.Working || state == VillagerState.WalkingToTask) return;
            state = VillagerState.Idle;
            _convocadoTimer = convocadoDuracion;
            _sr.enabled = true;

            // Se queda quieto un momento y mira hacia donde sono el pututo
            _idleTimer = Random.Range(1.2f, 2.2f);
            _target = transform.position;
            if (leader != null) _sr.flipX = leader.position.x < transform.position.x;

            UI.HUDController.Instance?.FloatingText(transform.position + Vector3.up * GameArt.U(70f),
                "¡Ayni!", new Color32(255, 235, 140, 255));
        }

        public void SendToWork(Vector3 position)
        {
            if (_seated) return;
            _target = position + new Vector3(Random.Range(-GameArt.U(18f), GameArt.U(18f)), 0f, 0f);
            state = VillagerState.WalkingToTask;
            _sr.enabled = true;
        }

        public void ReleaseFromWork()
        {
            if (_seated) return;
            state = VillagerState.Idle;
            _home = transform.position;
            transform.position = new Vector3(transform.position.x, _groundY, transform.position.z);
            transform.localScale = _baseScale;
        }

        public void GoToShelter()
        {
            if (_seated) return;
            state = VillagerState.RestingInShelter;
        }

        private void OnDawn(int day)
        {
            if (_seated) return;
            state = VillagerState.Idle;
            _sr.enabled = true;
            _home = transform.position;
        }

        public void SetSick(bool sick)
        {
            isSick = sick;
            if (_sickMark != null) _sickMark.gameObject.SetActive(sick);
            if (_sr != null && !_seated)
                _sr.color = sick ? Color.Lerp(_baseTint, new Color(0.72f, 0.80f, 0.95f), 0.75f) : _baseTint;
        }

        public void Die()
        {
            if (_sickMark != null) _sickMark.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }
    }
}
