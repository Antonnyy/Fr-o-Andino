using UnityEngine;
using System.Collections.Generic;

namespace FrioAndino.Art
{
    /// <summary>
    /// Reproductor de animaciones por cuadros sobre un SpriteRenderer.
    ///
    /// No usa el Animator de Unity a proposito: las animaciones se arman en codigo
    /// desde los PNG de Resources, asi el proyecto sigue abriendo y corriendo sin
    /// tener que crear AnimatorControllers ni clips en el Editor.
    ///
    /// Si un personaje no tiene hojas de animacion, este componente simplemente no
    /// tiene clips y quien lo use sigue con su animacion procedural de siempre.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteAnimator : MonoBehaviour
    {
        public class Clip
        {
            public string Name;
            public Sprite[] Frames;
            public float Fps = 8f;
            public bool Loop = true;
            public float Duration => Frames != null && Frames.Length > 0 ? Frames.Length / Mathf.Max(1f, Fps) : 0f;
        }

        private SpriteRenderer _sr;
        private readonly Dictionary<string, Clip> _clips = new Dictionary<string, Clip>();
        private Clip _current;
        private float _timer;
        private int _frame;

        /// <summary>True si se cargo al menos una hoja de animacion para este personaje.</summary>
        public bool HasAny => _clips.Count > 0;
        public string CurrentName => _current != null ? _current.Name : null;
        /// <summary>True cuando un clip sin loop llego al ultimo cuadro.</summary>
        public bool Finished { get; private set; }
        /// <summary>Velocidad de reproduccion (1 = normal). Util para correr mas rapido.</summary>
        public float SpeedScale { get; set; } = 1f;
        /// <summary>Congela el clip en el cuadro actual (pose fija).</summary>
        public bool Paused { get; set; }

        private void Awake()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        }

        public void Add(string name, Sprite[] frames, float fps, bool loop = true)
        {
            if (frames == null || frames.Length == 0) return;
            _clips[name] = new Clip { Name = name, Frames = frames, Fps = fps, Loop = loop };
        }

        public bool Has(string name) => name != null && _clips.ContainsKey(name);

        /// <summary>Duracion de un clip en segundos (0 si no existe). Sirve para saber
        /// cuanto durar una animacion de una sola vez, como tocar el pututo.</summary>
        public float GetDuration(string name) =>
            (name != null && _clips.TryGetValue(name, out var c)) ? c.Duration : 0f;

        /// <summary>
        /// Pone un clip. Si ya estaba sonando ese mismo no lo reinicia (salvo restart),
        /// asi el ciclo de caminata no se corta cada frame.
        /// </summary>
        public void Play(string name, bool restart = false)
        {
            if (name == null || !_clips.TryGetValue(name, out var clip)) return;
            if (_current == clip && !restart) return;

            _current = clip;
            _frame = 0;
            _timer = 0f;
            Finished = false;
            Apply();
        }

        /// <summary>Pone el primero que exista de la lista. Sirve para "run, si no walk".</summary>
        public bool PlayFirstAvailable(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                if (Has(names[i])) { Play(names[i]); return true; }
            }
            return false;
        }

        private void Apply()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_current != null && _sr != null && _frame < _current.Frames.Length)
                _sr.sprite = _current.Frames[_frame];
        }

        private void Update()
        {
            if (Paused || _current == null || _current.Frames.Length <= 1) return;

            _timer += Time.deltaTime * Mathf.Max(0.01f, SpeedScale);
            float step = 1f / Mathf.Max(1f, _current.Fps);
            if (_timer < step) return;

            int avanzar = Mathf.FloorToInt(_timer / step);
            _timer -= avanzar * step;
            _frame += avanzar;

            if (_frame >= _current.Frames.Length)
            {
                if (_current.Loop) _frame %= _current.Frames.Length;
                else { _frame = _current.Frames.Length - 1; Finished = true; }
            }
            Apply();
        }
    }
}
