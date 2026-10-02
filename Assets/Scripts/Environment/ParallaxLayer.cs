using UnityEngine;

namespace FrioAndino.Environment
{
    /// <summary>
    /// Parallax por diferencia de camara.
    ///
    /// No asume nada del controlador de camara: solo mide cuanto se movio la camara
    /// este frame y desplaza la capa una fraccion de eso. Por eso funciona igual con
    /// CameraFollow2D, con Cinemachine o arrastrando la camara a mano en el Editor.
    ///
    ///   factor 0.0  ->  la capa se queda quieta en el mundo (pasa a toda velocidad)
    ///   factor 0.5  ->  la capa copia la mitad del movimiento (se ve lejana)
    ///   factor 1.0  ->  la capa sigue a la camara (se ve infinitamente lejos: el cielo)
    ///
    /// Va en LateUpdate para correr DESPUES de que la camara ya se movio: si se hace
    /// en Update la capa va un frame atrasada y se nota como temblor.
    /// </summary>
    [DisallowMultipleComponent]
    public class ParallaxLayer : MonoBehaviour
    {
        [Header("Cuanto copia del movimiento de la camara (0 = mundo, 1 = cielo)")]
        [Range(0f, 1f)] [SerializeField] private float factorX = 0.45f;
        [Range(0f, 1f)] [SerializeField] private float factorY = 0.12f;

        [Header("Repetir la imagen al salir de cuadro")]
        [SerializeField] private bool infinito = false;

        private Transform _tf;
        private Transform _cam;
        private Vector3 _lastCamPos;
        private float _tileWidth;

        /// <summary>Configuracion por codigo (lo usa StageBuilder al montar la escena).</summary>
        public void Configure(Camera camera, float seguimientoX, float seguimientoY, bool repetir = false)
        {
            _tf = transform;
            _cam = camera != null ? camera.transform : null;
            factorX = Mathf.Clamp01(seguimientoX);
            factorY = Mathf.Clamp01(seguimientoY);
            infinito = repetir;
            if (_cam != null) _lastCamPos = _cam.position;
            CacheTileWidth();
        }

        private void Awake()
        {
            _tf = transform;
            if (_cam == null && Camera.main != null) _cam = Camera.main.transform;
            if (_cam != null) _lastCamPos = _cam.position;
            CacheTileWidth();
        }

        private void OnEnable()
        {
            if (_cam != null) _lastCamPos = _cam.position;   // evita un salto al reactivar
        }

        private void CacheTileWidth()
        {
            var sr = GetComponent<SpriteRenderer>();
            _tileWidth = (sr != null && sr.sprite != null) ? sr.bounds.size.x : 0f;
        }

        private void LateUpdate()
        {
            if (_cam == null)
            {
                if (Camera.main == null) return;
                _cam = Camera.main.transform;
                _lastCamPos = _cam.position;
                return;
            }

            Vector3 camPos = _cam.position;
            float dx = camPos.x - _lastCamPos.x;
            float dy = camPos.y - _lastCamPos.y;
            _lastCamPos = camPos;

            // Camara quieta: ni una sola escritura al transform (eso es lo "optimizado")
            if (dx == 0f && dy == 0f) return;

            Vector3 p = _tf.position;
            p.x += dx * factorX;
            p.y += dy * factorY;

            // Repeticion: cuando la camara se alejo mas de un ancho de imagen,
            // la capa se teletransporta un ancho exacto. Es invisible porque el
            // recorte cae justo donde el dibujo vuelve a empezar.
            if (infinito && _tileWidth > 0f)
            {
                float offset = camPos.x - p.x;
                if (Mathf.Abs(offset) >= _tileWidth)
                    p.x += Mathf.Sign(offset) * _tileWidth;
            }

            _tf.position = p;
        }
    }
}
