using UnityEngine;
using System.Collections;

namespace FrioAndino.Audio
{
    /// <summary>
    /// Orquesta musica y SFX. Genera los clips al arrancar (sintesis) y hace crossfade
    /// entre el tema diurno y el nocturno segun la fase del ciclo.
    /// </summary>
    public class AudioDirector : MonoBehaviour
    {
        public static AudioDirector Instance { get; private set; }

        private AudioSource _musicA, _musicB, _sfx, _ambient, _fire;
        private bool _usingA = true;

        private AudioClip _day, _night, _victory, _defeat;
        private AudioClip _pututo, _pickup, _build, _dawn, _alarm, _bleat, _cough;
        private AudioClip _windLoop, _fireLoop;

        [Range(0f, 1f)] public float musicVolume = 0.45f;
        [Range(0f, 1f)] public float sfxVolume = 0.8f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _musicA = CreateSource("MusicA", musicVolume, true);
            _musicB = CreateSource("MusicB", 0f, true);
            _sfx = CreateSource("SFX", sfxVolume, false);
            _ambient = CreateSource("Ambient", 0.10f, true);
            _fire = CreateSource("Fire", 0f, true);

            // Carga de clips reales de audio (Resources/FrioAndino/) con fallback a AudioSynth
            _day = LoadClip("tema_dia") ?? LoadClip("tema_dia_nuevo") ?? AudioSynth.DayTheme();
            _night = LoadClip("tema_noche") ?? AudioSynth.NightTheme();
            _victory = LoadClip("victoria") ?? AudioSynth.VictoryTheme();
            _defeat = LoadClip("derrota") ?? AudioSynth.DefeatTheme();
            _pututo = LoadClip("pututo") ?? AudioSynth.Pututo();
            _pickup = LoadClip("pickup") ?? AudioSynth.Pickup();
            _build = LoadClip("construir") ?? AudioSynth.Build();
            _dawn = LoadClip("amanecer") ?? AudioSynth.Dawn();
            _alarm = LoadClip("alarma") ?? AudioSynth.NightAlarm();
            _bleat = LoadClip("alpaca") ?? AudioSynth.AlpacaBleat();
            _cough = LoadClip("tos") ?? AudioSynth.Cough();
            _windLoop = LoadClip("viento") ?? AudioSynth.WindLoop();
            _fireLoop = LoadClip("fuego") ?? AudioSynth.FireLoop();

            _ambient.clip = _windLoop; _ambient.Play();
            _fire.clip = _fireLoop; _fire.Play();
        }

        private static AudioClip LoadClip(string name)
        {
            var c = Resources.Load<AudioClip>("FrioAndino/" + name);
            if (c != null) return c;
            return Resources.Load<AudioClip>(name);
        }

        public void SetMusicVolume(float vol)
        {
            musicVolume = Mathf.Clamp01(vol);
            if (!MusicMuted)
            {
                var current = _usingA ? _musicA : _musicB;
                if (current != null) current.volume = musicVolume;
            }
        }

        public void SetSfxVolume(float vol)
        {
            sfxVolume = Mathf.Clamp01(vol);
            if (!SfxMuted)
            {
                if (_sfx != null) _sfx.volume = sfxVolume;
            }
        }

        private AudioSource CreateSource(string name, float vol, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.volume = vol;
            src.loop = loop;
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            return src;
        }

        public bool MusicMuted { get; private set; }
        public bool SfxMuted { get; private set; }

        public void ToggleMusic()
        {
            MusicMuted = !MusicMuted;
            _musicA.mute = _musicB.mute = MusicMuted;
        }

        public void ToggleSfx()
        {
            SfxMuted = !SfxMuted;
            _sfx.mute = _ambient.mute = _fire.mute = SfxMuted;
        }

        // ------------------- Musica -------------------
        public void PlayDayMusic() => Crossfade(_day, 2.5f);
        public void PlayNightMusic() => Crossfade(_night, 2.5f);

        public void PlayVictory()
        {
            StopAllCoroutines();
            _musicA.Stop(); _musicB.Stop();
            _sfx.PlayOneShot(_victory, 1f);
            _ambient.volume = 0.05f;
        }

        public void PlayDefeat()
        {
            StopAllCoroutines();
            _musicA.Stop(); _musicB.Stop();
            _sfx.PlayOneShot(_defeat, 1f);
            _ambient.volume = 0.35f;
        }

        private void Crossfade(AudioClip next, float dur)
        {
            if (next == null) return;
            var from = _usingA ? _musicA : _musicB;
            var to = _usingA ? _musicB : _musicA;
            if (from.clip == next && from.isPlaying) return;
            to.clip = next;
            to.volume = 0f;
            to.Play();
            StopAllCoroutines();
            StartCoroutine(CrossfadeRoutine(from, to, dur));
            _usingA = !_usingA;
        }

        private IEnumerator CrossfadeRoutine(AudioSource from, AudioSource to, float dur)
        {
            float t = 0f;
            float startVol = from.volume;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                from.volume = Mathf.Lerp(startVol, 0f, k);
                to.volume = Mathf.Lerp(0f, musicVolume, k);
                yield return null;
            }
            from.Stop();
        }

        // ------------------- Ambiente -------------------
        /// <summary>El viento arrecia mientras baja la temperatura (0 = templado, 1 = -20 C).</summary>
        public void SetWindIntensity(float t01)
        {
            _ambient.volume = Mathf.Lerp(0.05f, 0.22f, Mathf.Clamp01(t01));
            _ambient.pitch = Mathf.Lerp(0.96f, 1.06f, Mathf.Clamp01(t01));
        }

        /// <summary>Volumen del crepitar segun cercania del jugador al fogon y combustible.</summary>
        public void SetFireIntensity(float t01)
        {
            _fire.volume = Mathf.Clamp01(t01) * 0.30f;
        }

        // ------------------- SFX -------------------
        public void Pututo() => Play(_pututo, 1f);
        public void Pickup() => Play(_pickup, 0.8f);
        public void BuildDone() => Play(_build, 0.9f);
        public void Dawn() => Play(_dawn, 0.9f);
        public void NightAlarm() => Play(_alarm, 0.9f);
        public void Bleat() => Play(_bleat, 0.55f);
        public void Cough() => Play(_cough, 0.7f);

        private void Play(AudioClip c, float vol)
        {
            if (c != null) _sfx.PlayOneShot(c, vol * sfxVolume);
        }
    }
}
