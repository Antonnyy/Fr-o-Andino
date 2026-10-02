using UnityEngine;
using FrioAndino.Art;

namespace FrioAndino.Player
{
    /// <summary>
    /// Camara lateral suave confinada al ancho del fondo pintado.
    /// Permite modo centrado (para el menu principal y panoramas) y modo seguimiento
    /// para acompanar a Inti por el caserio andino.
    /// </summary>
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.22f;
        [SerializeField] private float fixedY = 1.309f;
        [SerializeField] private float orthoSize = 4.8f;

        public bool IsLocked { get; set; } = false;
        public float LockedX { get; set; } = 0f;

        private float _vel;

        public void Configure(Transform t, float y, float ortho)
        {
            target = t;
            fixedY = y;
            orthoSize = ortho;
        }

        public void SnapTo(float x, float y)
        {
            fixedY = y;
            transform.position = new Vector3(x, fixedY, -10f);
            _vel = 0f;
        }

        public void SetLocked(bool locked, float targetX = 0f)
        {
            IsLocked = locked;
            LockedX = targetX;
        }

        private void LateUpdate()
        {
            var cam = GetComponent<Camera>();
            float viewHalfW = orthoSize * (cam != null ? cam.aspect : 1.7778f);
            float limit = Mathf.Max(0f, GameArt.WorldHalfWidth - viewHalfW);

            float desired = 0f;
            if (IsLocked)
            {
                desired = Mathf.Clamp(LockedX, -limit, limit);
            }
            else if (target != null)
            {
                desired = Mathf.Clamp(target.position.x, -limit, limit);
            }

            float x = limit <= 0.001f ? 0f : Mathf.SmoothDamp(transform.position.x, desired, ref _vel, smoothTime);
            transform.position = new Vector3(x, fixedY, -10f);
        }
    }
}
