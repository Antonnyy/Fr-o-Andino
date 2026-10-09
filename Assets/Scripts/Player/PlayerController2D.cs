using UnityEngine;
using FrioAndino.Art;
using FrioAndino.Core;
using FrioAndino.Audio;
using FrioAndino.Villagers;

namespace FrioAndino.Player
{
    /// <summary>
    /// Inti, el Yachachiq. Recorre el caserio, cosecha, ordena faenas y toca el pututo.
    /// No combate: su unica "arma" es organizar a la comunidad.
    ///
    /// Se mueve por transform con gravedad propia (el caserio es un plano horizontal),
    /// asi corre igual en cualquier version de Unity sin depender de la API de fisicas.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerController2D : MonoBehaviour
    {
        [Header("Movimiento (en pixeles del fondo por segundo)")]
        [SerializeField] private float walkPixelsPerSecond = 46f;
        [SerializeField] private float runPixelsPerSecond = 88f;
        [SerializeField] private float jumpPixelsPerSecond = 150f;
        [SerializeField] private float gravityPixels = 620f;

        [Header("Fatiga por altura (4,200 m s.n.m.)")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float staminaDrain = 26f;
        [Tooltip("Recuperacion por segundo: lenta, hay poco oxigeno.")]
        [SerializeField] private float staminaRegen = 13f;

        [Header("Pututo")]
        [SerializeField] private float pututoCooldown = 6f;

        public static PlayerController2D Instance { get; private set; }
        public float Stamina { get; private set; }
        public float StaminaRatio => Stamina / maxStamina;
        public float PututoReadyRatio => 1f - Mathf.Clamp01(_pututoTimer / pututoCooldown);
        public bool IsRunning { get; private set; }

        private SpriteRenderer _sr;
        private float _vy;
        private bool _grounded = true;
        private float _pututoTimer;
        private float _walkPhase;
        private float _groundY;
        private float _minX, _maxX;
        private Transform _breath;
        private SpriteRenderer _breathSr;
        private float _breathTimer;
        private Vector3 _baseScale;

        private SpriteAnimator _anim;
        private bool _animated;
        private float _pututoAnimTimer;

        private void Awake()
        {
            Instance = this;
            _sr = GetComponent<SpriteRenderer>();
            if (_sr.sprite == null) _sr.sprite = GameArt.Inti;

            // Si existen las hojas inti_idle/walk/run/jump.bytes, Inti se anima por cuadros.
            // Si no, sigue con el balanceo procedural de siempre.
            _anim = gameObject.AddComponent<SpriteAnimator>();
            _animated = GameArt.LoadPlayerClips(_anim) > 0;
            Stamina = maxStamina;
            _groundY = transform.position.y;
            _baseScale = transform.localScale;

            _minX = GameArt.MinWalkX;
            _maxX = GameArt.MaxWalkX;

            var go = new GameObject("Vaho");
            go.transform.SetParent(transform, false);
            _breathSr = go.AddComponent<SpriteRenderer>();
            _breathSr.sprite = PixelArtFactory.Circle(new Color32(232, 242, 255, 130), 16);
            _breathSr.sortingOrder = 41;
            _breath = go.transform;
            _breath.localPosition = new Vector3(0.22f, 1.45f, 0f);
            _breathSr.enabled = false;
        }

        private void Start()
        {
            if (GameManager.Instance != null) GameManager.Instance.PlayerTransform = transform;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                transform.localScale = _baseScale;
                return;
            }

            float h = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;

            bool wantsRun = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Mathf.Abs(h) > 0.01f;
            IsRunning = wantsRun && Stamina > 1f;

            // La altura castiga: correr agota rapido y se recupera despacio
            if (IsRunning) Stamina = Mathf.Max(0f, Stamina - staminaDrain * Time.deltaTime);
            else Stamina = Mathf.Min(maxStamina, Stamina + staminaRegen * Time.deltaTime);

            float speed = GameArt.U(IsRunning ? runPixelsPerSecond : walkPixelsPerSecond);

            if (_grounded && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)))
            {
                _vy = GameArt.U(jumpPixelsPerSecond);
                _grounded = false;
            }
            _vy -= GameArt.U(gravityPixels) * Time.deltaTime;

            float nx = Mathf.Clamp(transform.position.x + h * speed * Time.deltaTime, _minX, _maxX);
            float ny = transform.position.y + _vy * Time.deltaTime;
            if (ny <= _groundY) { ny = _groundY; _vy = 0f; _grounded = true; }
            transform.position = new Vector3(nx, ny, transform.position.z);

            if (_pututoTimer > 0f) _pututoTimer -= Time.deltaTime;
            if (_pututoAnimTimer > 0f) _pututoAnimTimer -= Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.Q) && _pututoTimer <= 0f) BlowPututo();
            if (Input.GetKeyDown(KeyCode.F)) WhistleHerd();

            if (Mathf.Abs(h) > 0.01f) _sr.flipX = h < 0f;
            if (_animated) UpdateClip(Mathf.Abs(h) > 0.01f);
            else Animate(Mathf.Abs(h) > 0.01f && _grounded);
            UpdateBreath();
        }

        /// <summary>
        /// Elige el clip segun lo que esta haciendo Inti.
        /// Si falta alguna hoja tira de la que si existe: sin "run" corre la de caminar
        /// mas rapido, y sin "idle" se queda congelado en el primer cuadro.
        /// </summary>
        private void UpdateClip(bool moving)
        {
            transform.localScale = _baseScale;   // los cuadros ya traen su propio movimiento
            _anim.Paused = false;

            // Mientras suena el pututo, la animacion del cuerno manda sobre cualquier otra
            if (_pututoAnimTimer > 0f && _anim.Has("pututo")) { _anim.Play("pututo"); _anim.SpeedScale = 1f; return; }

            if (!_grounded && _anim.Has("jump")) { _anim.Play("jump"); _anim.SpeedScale = 1f; return; }

            if (moving)
            {
                if (IsRunning && _anim.Has("run")) { _anim.Play("run"); _anim.SpeedScale = 1f; }
                else { _anim.Play("walk"); _anim.SpeedScale = IsRunning ? 1.45f : 1f; }
                return;
            }

            _anim.SpeedScale = 1f;
            if (_anim.Has("idle")) _anim.Play("idle");
            else { _anim.Play("walk", true); _anim.Paused = true; }
        }

        /// <summary>Balanceo de caminata: sin hoja de sprites, el personaje "pisa" con escala.</summary>
        private void Animate(bool moving)
        {
            if (moving) _walkPhase += Time.deltaTime * (IsRunning ? 13f : 8.5f);
            else _walkPhase = Mathf.Lerp(_walkPhase, 0f, Time.deltaTime * 8f);

            float step = Mathf.Abs(Mathf.Sin(_walkPhase));
            float squash = moving ? 1f - step * 0.035f : 1f;
            float stretch = moving ? 1f + step * 0.03f : 1f;
            transform.localScale = new Vector3(_baseScale.x * stretch, _baseScale.y * squash, _baseScale.z);

            if (moving && _grounded)
            {
                float bob = step * GameArt.U(1.6f);
                transform.position = new Vector3(transform.position.x, _groundY + bob, transform.position.z);
            }
        }

        /// <summary>Convoca a los ronderos libres y alerta al rebano. Es el nucleo del ayni: nadie trabaja solo.
        /// Tecla [Q]. Si hay hoja de animacion "inti_pututo", se ve a Inti tocando el cuerno.</summary>
        public void UsePututo() => BlowPututo();
        public void BlowPututo()
        {
            _pututoTimer = pututoCooldown;
            if (_animated && _anim.Has("pututo"))
                _pututoAnimTimer = Mathf.Max(0.5f, _anim.GetDuration("pututo") * 2.2f);   // 2 vueltas del ciclo
            AudioDirector.Instance?.Pututo();
            int called = VillagerTaskSystem.Instance != null ? VillagerTaskSystem.Instance.CallPututo(transform) : 0;
            int alpacas = AlpacaAI.HerdCallAll(transform.position, GameArt.U(240f));

            string info = called > 0 ? $"Pututo! {called} ronderos" : "Pututo!";
            if (alpacas > 0) info += $"  |  {alpacas} alpacas";
            UI.HUDController.Instance?.FloatingText(transform.position + Vector3.up * GameArt.U(70f),
                info, new Color32(255, 220, 140, 255));

            string logMsg = called > 0
                ? $"{called} rondero(s) listos para faenar."
                : "El eco del pututo resuena en las montanas.";
            if (alpacas > 0) logMsg += $" {alpacas} alpaca(s) se orientan hacia el cobertizo.";
            UI.HUDController.Instance?.Log(logMsg);

            StartCoroutine(SoundWave());
        }

        /// <summary>Silbido de arreo [F]: agrupa y hace avanzar a las alpacas cercanas hacia el cobertizo.</summary>
        public void WhistleHerd()
        {
            int alpacas = AlpacaAI.HerdCallAll(transform.position, GameArt.U(240f));
            UI.HUDController.Instance?.FloatingText(transform.position + Vector3.up * GameArt.U(70f),
                alpacas > 0 ? $"¡Haku!  {alpacas} alpacas" : "¡Haku!  (nadie cerca)",
                new Color32(220, 245, 170, 255));
            if (alpacas > 0)
            {
                UI.HUDController.Instance?.Log($"Inti silba con fuerza: {alpacas} alpaca(s) avanzan hacia el cobertizo comunal.");
            }
        }

        private System.Collections.IEnumerator SoundWave()
        {
            var go = new GameObject("OndaPututo");
            go.transform.position = transform.position + Vector3.up * GameArt.U(38f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PixelArtFactory.Circle(new Color32(255, 232, 180, 80), 64);
            sr.sortingOrder = 6;
            float nat = Mathf.Max(0.0001f, sr.sprite.bounds.size.x);   // el sprite no mide 1 unidad
            float t = 0f;
            while (t < 1.3f)
            {
                t += Time.deltaTime;
                float k = t / 1.3f;
                go.transform.localScale = Vector3.one * Mathf.Lerp(GameArt.U(6f), GameArt.U(110f), k) / nat;
                var c = sr.color; c.a = Mathf.Lerp(0.32f, 0f, k); sr.color = c;
                yield return null;
            }
            Destroy(go);
        }

        private void UpdateBreath()
        {
            var ttm = TimeTemperatureManager.Instance;
            if (ttm == null) return;
            _breathTimer -= Time.deltaTime;
            if (ttm.CurrentTemperature < 3f && _breathTimer <= 0f)
            {
                _breathTimer = 2.4f;
                StartCoroutine(BreathPuff());
            }
        }

        private System.Collections.IEnumerator BreathPuff()
        {
            _breathSr.enabled = true;
            float t = 0f;
            float dir = _sr.flipX ? -1f : 1f;
            while (t < 0.9f)
            {
                t += Time.deltaTime;
                float k = t / 0.9f;
                _breath.localPosition = new Vector3(dir * (0.18f + k * 0.5f), 1.45f + k * 0.3f, 0f);
                _breath.localScale = Vector3.one * Mathf.Lerp(0.25f, 1.1f, k);
                var c = _breathSr.color; c.a = Mathf.Lerp(0.5f, 0f, k); _breathSr.color = c;
                yield return null;
            }
            _breathSr.enabled = false;
            var c2 = _breathSr.color; c2.a = 0.5f; _breathSr.color = c2;
        }
    }
}
