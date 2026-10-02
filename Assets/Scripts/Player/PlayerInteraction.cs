using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using FrioAndino.Core;
using FrioAndino.Art;

namespace FrioAndino.Player
{
    /// <summary>
    /// Detecta el IInteractable mas cercano y ejecuta la accion contextual con 'E'.
    /// Mantiene una cache de interactuables (el caserio no cambia de composicion en partida),
    /// refrescada cada cierto tiempo para incluir lo que el bootstrap cree despues.
    /// </summary>
    public class PlayerInteraction : MonoBehaviour
    {
        [Tooltip("Distancia horizontal maxima, en pixeles del fondo, para accionar algo.")]
        [SerializeField] private float interactRangePixels = 46f;
        [SerializeField] private float refreshInterval = 2f;

        private readonly List<IInteractable> _cache = new List<IInteractable>();
        private float _refreshTimer;

        public IInteractable Current { get; private set; }

        private void Start() => Refresh();

        private void Refresh()
        {
            _cache.Clear();
#if UNITY_2023_1_OR_NEWER
            var all = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
#else
            var all = Object.FindObjectsOfType<MonoBehaviour>();
#endif
            foreach (var mb in all)
                if (mb is IInteractable it) _cache.Add(it);
        }

        private void Update()
        {
            _refreshTimer -= Time.deltaTime;
            if (_refreshTimer <= 0f) { _refreshTimer = refreshInterval; Refresh(); }

            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) { Current = null; return; }

            // ---- Busca el interactuable valido mas cercano en X ----
            IInteractable best = null;
            float bestD = float.MaxValue;
            foreach (var it in _cache)
            {
                var t = it?.GetTransform();
                if (t == null || !t.gameObject.activeInHierarchy) continue;
                float d = Mathf.Abs(t.position.x - transform.position.x);
                if (d > GameArt.U(interactRangePixels)) continue;
                if (!it.CanInteract()) { if (best == null && d < bestD) { best = it; bestD = d; } continue; }
                if (d < bestD) { best = it; bestD = d; }
            }
            Current = best;

            if (Current != null && Input.GetKeyDown(KeyCode.E))
            {
                if (Current.CanInteract())
                {
                    string msg = Current.Interact(gameObject);
                    if (!string.IsNullOrEmpty(msg)) UI.HUDController.Instance?.Log(msg);
                }
                else
                {
                    UI.HUDController.Instance?.Log("Aqui no hay nada que hacer ahora mismo.");
                }
            }
        }
    }
}
