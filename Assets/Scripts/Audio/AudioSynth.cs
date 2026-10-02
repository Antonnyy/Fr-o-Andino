using UnityEngine;
using System;

namespace FrioAndino.Audio
{
    /// <summary>
    /// Sintetiza por codigo toda la musica y los SFX (el proyecto no lleva archivos de audio).
    ///
    /// REGLA DE ESTE ARCHIVO: aqui NO se genera ruido blanco. Ni un solo rng en la senal.
    /// El "disco rayado" que se oia venia de ahi: ruido aleatorio por muestra = estatica,
    /// y chasquidos sorteados por muestra = chirrido de vinilo. Todo timbre se arma con
    /// ondas afinadas (seno / triangulo / sierra) y envolventes, asi que no hay nada que
    /// pueda sonar a rayadura.
    ///
    /// Escala pentatonica menor de La: base del huayno andino.
    /// </summary>
    public static class AudioSynth
    {
        public const int SampleRate = 44100;

        // La, Do, Re, Mi, Sol en dos octavas
        private static readonly float[] Penta =
        {
            220.00f, 261.63f, 293.66f, 329.63f, 392.00f,
            440.00f, 523.25f, 587.33f, 659.25f, 783.99f, 880.00f
        };

        // ---------------------------------------------------------------
        //  Osciladores
        // ---------------------------------------------------------------
        private static float Sine(float phase) => Mathf.Sin(phase * Mathf.PI * 2f);
        private static float Triangle(float phase) { float p = phase % 1f; return 4f * Mathf.Abs(p - 0.5f) - 1f; }
        private static float Saw(float phase) { float p = phase % 1f; return p * 2f - 1f; }

        /// <summary>Quena: flauta de caña. Fundamental fuerte, armonicos suaves, vibrato tardio.</summary>
        private static float Quena(float phase, float t)
        {
            float v = Sine(phase) * 0.66f
                    + Sine(phase * 2f) * 0.13f
                    + Sine(phase * 3f) * 0.09f
                    + Sine(phase * 4f) * 0.025f;
            // El soplo se emula con un armonico agudo que respira, no con ruido
            float soplo = Sine(phase * 6f) * 0.02f * (0.6f + 0.4f * Mathf.Sin(t * 17f));
            float vib = 1f + Mathf.Sin(t * Mathf.PI * 2f * 5.4f) * Mathf.Min(1f, t * 3.2f) * 0.016f;
            return (v + soplo) * vib;
        }

        /// <summary>Zampoña (siku): cuerpo hueco, octava marcada, sonido mas ancho que la quena.</summary>
        private static float Siku(float phase, float t)
        {
            float v = Sine(phase) * 0.55f
                    + Sine(phase * 2f) * 0.24f
                    + Sine(phase * 3f) * 0.06f
                    + Triangle(phase * 0.5f) * 0.07f;
            float aire = Sine(phase * 8f) * 0.018f * (0.5f + 0.5f * Mathf.Sin(t * 11f));
            return v + aire;
        }

        /// <summary>Charango: cuerda metalica de 5 ordenes, brillante y de decaimiento corto.</summary>
        private static float Charango(float phase) =>
            Triangle(phase) * 0.44f + Saw(phase * 2f) * 0.11f
            + Sine(phase * 3f) * 0.12f + Sine(phase * 4.02f) * 0.06f;

        private static float Envelope(float t, float dur, float attack, float release)
        {
            if (t < attack) return t / Mathf.Max(attack, 0.0001f);
            if (t > dur - release) return Mathf.Max(0f, (dur - t) / Mathf.Max(release, 0.0001f));
            return 1f;
        }

        // ---------------------------------------------------------------
        //  MUSICA
        // ---------------------------------------------------------------
        /// <summary>
        /// Tema diurno "Puna y Esperanza": huayno en 2/4 a 94 bpm.
        /// Estructura A-A'-B-A: quena guiando, segunda quena a la quinta, charango con el
        /// rasgueo de huayno y bombo legüero. Cierra con eco de quebrada.
        /// </summary>
        public static AudioClip DayTheme()
        {
            float bpm = 94f;
            float beat = 60f / bpm;
            float compas = beat * 2f;
            int compases = 24;
            float largo = compases * compas;

            int cola = (int)(SampleRate * 1.4f);
            int n = (int)(SampleRate * largo) + cola;
            float[] data = new float[n];

            // --- Melodia: A (sube al cerro), B (respuesta alta), A (vuelta a casa) ---
            int[] A = { 3, 4, 3, 2, 3, 5, 4, 3, 2, 1, 2, 3, 2, 1, 0, 0 };
            float[] Ad = { 1f, .5f, .5f, 1f, 1f, 1f, .5f, .5f, 1f, 1f, .5f, .5f, 1f, 1f, 1f, 2f };
            int[] B = { 5, 6, 5, 4, 5, 7, 6, 5, 4, 3, 4, 5, 4, 3, 2, 2 };
            float[] Bd = { 1f, .5f, .5f, 1f, 1f, 1f, .5f, .5f, 1f, 1f, .5f, .5f, 1f, 1.5f, .5f, 2f };

            float t = 0f;
            for (int bloque = 0; bloque < 3 && t < largo - beat; bloque++)
            {
                int[] linea = bloque == 1 ? B : A;
                float[] duras = bloque == 1 ? Bd : Ad;
                for (int i = 0; i < linea.Length && t < largo - beat * 0.5f; i++)
                {
                    float dur = duras[i] * beat;
                    float f = Penta[Mathf.Clamp(linea[i], 0, Penta.Length - 1)] * 2f;
                    RenderNote(data, t, dur * 0.92f, f, 0.185f, Quena, 0.045f, dur * 0.4f);

                    // Segunda quena a la quinta abajo en las notas largas: el duo serrano
                    if (duras[i] >= 1f)
                        RenderNote(data, t, dur * 0.92f, f * 0.6674f, 0.065f, Quena, 0.06f, dur * 0.45f);

                    t += dur;
                }
            }

            // --- Charango: rasgueo de huayno (larga - corta corta) ---
            float[] golpes = { 0f, 0.5f, 0.75f, 1f, 1.5f, 1.75f };
            for (int c = 0; c < compases; c++)
            {
                float t0 = c * compas;
                int grado = (c % 4 < 2) ? 0 : 2;              // Lam -> Do
                foreach (float off in golpes)
                {
                    float amp = (off == 0f || off == 1f) ? 0.070f : 0.042f;
                    for (int k = 0; k < 3; k++)
                    {
                        float f = Penta[(grado + k * 2) % 5];
                        RenderNote(data, t0 + off * beat + k * 0.011f, beat * 0.42f, f,
                                   amp, (ph, tt) => Charango(ph), 0.004f, beat * 0.38f);
                    }
                }
            }

            // --- Bombo legüero ---
            for (int c = 0; c < compases; c++)
            {
                float t0 = c * compas;
                RenderKick(data, t0, 0.30f, 70f);
                RenderKick(data, t0 + beat, 0.15f, 62f);
                RenderKick(data, t0 + beat * 1.5f, 0.09f, 58f);
            }

            Eco(data, 0.255f, 0.22f, 2);                      // aire de quebrada
            data = LoopSeamless(data, cola);
            Normalize(data, 0.52f);
            return FromData("FrioAndino_DayTheme", data, true);
        }

        /// <summary>
        /// Tema nocturno: zamponas graves sostenidas y latido lento. Puro aliento,
        /// sin percusion seca, para que la helada se sienta enorme y quieta.
        /// </summary>
        public static AudioClip NightTheme()
        {
            int seconds = 30;
            int cola = (int)(SampleRate * 2f);
            int n = SampleRate * seconds + cola;
            float[] data = new float[n];

            // Drone de dos zamponas apenas desafinadas entre si
            for (int i = 0; i < n; i++)
            {
                float tt = (float)i / SampleRate;
                float ciclo = tt / seconds * Mathf.PI * 2f;
                float respira = 0.55f + 0.45f * Mathf.Sin(ciclo);
                float v = Sine(tt * 110f) * 0.055f + Sine(tt * 110.6f) * 0.045f + Sine(tt * 55f) * 0.05f;
                data[i] += v * (0.55f + respira * 0.45f);
            }

            // Frases largas de siku
            float[] notas = { 146.83f, 164.81f, 130.81f, 174.61f, 110.0f, 146.83f };
            for (int k = 0; k < notas.Length; k++)
            {
                float start = k * 4.9f;
                if (start > seconds - 3f) break;
                RenderNote(data, start, 4.4f, notas[k], 0.15f, Siku, 1.3f, 1.9f);
            }

            // Latido de la comunidad
            for (float t = 0.6f; t < seconds; t += 1.8f)
            {
                RenderKick(data, t, 0.17f, 55f);
                RenderKick(data, t + 0.24f, 0.10f, 50f);
            }

            Eco(data, 0.42f, 0.25f, 2);
            data = LoopSeamless(data, cola);
            Normalize(data, 0.46f);
            return FromData("FrioAndino_NightTheme", data, true);
        }

        /// <summary>Fanfarria andina de victoria.</summary>
        public static AudioClip VictoryTheme()
        {
            int seconds = 8;
            float[] data = new float[SampleRate * seconds];
            float beat = 0.32f;
            int[] fanfare = { 0, 2, 4, 5, 7, 5, 7, 8, 7, 5, 4, 2, 4, 5, 7, 9 };
            for (int i = 0; i < fanfare.Length; i++)
            {
                float t = i * beat * 0.75f;
                float f = Penta[Mathf.Clamp(fanfare[i], 0, Penta.Length - 1)];
                RenderNote(data, t, beat, f, 0.21f, Siku, 0.02f, 0.16f);
                RenderNote(data, t, beat, f * 0.5f, 0.11f, (ph, tt) => Charango(ph), 0.01f, 0.2f);
            }
            for (float t = 0; t < seconds - 1f; t += beat * 0.75f) RenderKick(data, t, 0.20f, 70f);
            Eco(data, 0.24f, 0.2f, 2);
            Normalize(data, 0.68f);
            return FromData("FrioAndino_Victory", data, false);
        }

        /// <summary>Tema de derrota: quena descendente y desolada.</summary>
        public static AudioClip DefeatTheme()
        {
            int seconds = 7;
            float[] data = new float[SampleRate * seconds];
            float[] desc = { 293.66f, 261.63f, 220f, 196f, 174.61f, 146.83f };
            for (int i = 0; i < desc.Length; i++)
                RenderNote(data, i * 1.0f, 1.5f, desc[i], 0.20f, Quena, 0.22f, 0.75f);
            Eco(data, 0.38f, 0.28f, 2);
            Normalize(data, 0.58f);
            return FromData("FrioAndino_Defeat", data, false);
        }

        // ---------------------------------------------------------------
        //  AMBIENTE (tonal, nunca ruido)
        // ---------------------------------------------------------------
        /// <summary>
        /// Viento de la puna: acorde grave que respira, sin un solo sample de ruido.
        /// Antes era ruido filtrado y por eso siseaba como estatica de radio.
        /// </summary>
        public static AudioClip WindLoop()
        {
            float dur = 12f;
            int n = (int)(SampleRate * dur);
            float[] data = new float[n];
            // Quintas vacias muy graves: suena a quebrada abierta, no a ruido
            float[] capas = { 55f, 82.41f, 110f, 164.81f };
            float[] velo = { 0.037f, 0.023f, 0.017f, 0.007f };

            // Cada capa se afina al ciclo exacto del loop: si no cae justo, al volver a
            // empezar salta la onda y se oye el clic del vinilo.
            for (int k = 0; k < capas.Length; k++)
                capas[k] = Mathf.Round(capas[k] * dur) / dur;

            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float ciclo = t / dur * Mathf.PI * 2f;
                float v = 0f;
                for (int k = 0; k < capas.Length; k++)
                {
                    // cada capa entra y sale en ciclos exactos del loop: empalma solo
                    float mov = 0.55f + 0.45f * Mathf.Sin(ciclo * (k + 1) + k * 1.7f);
                    float det = 1f + Mathf.Sin(ciclo * (k + 2)) * 0.0015f;   // vaiven de aire
                    v += Sine(t * capas[k] * det) * velo[k] * mov;
                }
                data[i] = v;
            }
            Normalize(data, 0.32f);
            return FromData("SFX_Wind", data, true);
        }

        /// <summary>
        /// Fogon: resplandor sonoro grave que titila. Los chasquidos van con tono, no con
        /// ruido, y son pocos; el crepitar aleatorio anterior era el disco rayado.
        /// </summary>
        public static AudioClip FireLoop()
        {
            float dur = 8f;
            int n = (int)(SampleRate * dur);
            float[] data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float ciclo = t / dur * Mathf.PI * 2f;
                float titila = 0.6f + 0.25f * Mathf.Sin(ciclo * 7f) + 0.15f * Mathf.Sin(ciclo * 13f);
                float v = Sine(t * 68f) * 0.05f + Sine(t * 101f) * 0.028f + Sine(t * 143f) * 0.014f;
                data[i] = v * titila;
            }
            // Sin chasquidos: cualquier golpecito repetido cada pocos segundos vuelve a
            // sonar a disco que salta. El fogon se nota por el titileo grave y ya.
            Normalize(data, 0.30f);
            return FromData("SFX_Fire", data, true);
        }

        // ---------------------------------------------------------------
        //  SFX
        // ---------------------------------------------------------------
        /// <summary>Pututo: caracola andina, grave y resonante.</summary>
        public static AudioClip Pututo()
        {
            float dur = 1.8f;
            int n = (int)(SampleRate * dur);
            float[] data = new float[n];
            float baseFreq = 138f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float bend = 1f + Mathf.Clamp01(t / 0.25f) * 0.06f;
                float ph = t * baseFreq * bend;
                float v = Sine(ph) * 0.55f + Sine(ph * 2f) * 0.30f
                        + Sine(ph * 3f) * 0.15f + Sine(ph * 5f) * 0.05f;
                float aire = Sine(ph * 7f) * 0.05f * Mathf.Exp(-t * 3f);
                float env = Envelope(t, dur, 0.12f, 0.7f) * Mathf.Exp(-t * 0.5f);
                data[i] = (v + aire) * env * 0.8f;
            }
            Eco(data, 0.19f, 0.3f, 2);
            Normalize(data, 0.85f);
            return FromData("SFX_Pututo", data, false);
        }

        /// <summary>Balido de alpaca.</summary>
        public static AudioClip AlpacaBleat()
        {
            float dur = 0.65f;
            int n = (int)(SampleRate * dur);
            float[] data = new float[n];
            float ph = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float f = 340f + Mathf.Sin(t * 38f) * 45f - t * 90f;
                ph += f / SampleRate;
                float v = Saw(ph) * 0.3f + Sine(ph) * 0.42f + Sine(ph * 2f) * 0.1f;
                data[i] = v * Envelope(t, dur, 0.03f, 0.25f);
            }
            Normalize(data, 0.48f);
            return FromData("SFX_Alpaca", data, false);
        }

        /// <summary>Blip corto al recolectar.</summary>
        public static AudioClip Pickup()
        {
            float dur = 0.22f;
            int n = (int)(SampleRate * dur);
            float[] data = new float[n];
            float ph = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float f = 660f + t * 1400f;
                ph += f / SampleRate;
                data[i] = (Sine(ph) * 0.6f + Triangle(ph * 2f) * 0.18f) * Envelope(t, dur, 0.005f, 0.15f);
            }
            Normalize(data, 0.45f);
            return FromData("SFX_Pickup", data, false);
        }

        /// <summary>Golpe de faena terminada: adobe asentado.</summary>
        public static AudioClip Build()
        {
            float dur = 0.5f;
            int n = (int)(SampleRate * dur);
            float[] data = new float[n];
            float ph = 0f, ph2 = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float f = Mathf.Max(38f, 120f - t * 90f);
                ph += f / SampleRate;
                ph2 += 430f / SampleRate;
                float golpe = Sine(ph) * Mathf.Exp(-t * 9f);
                float madera = Triangle(ph2) * Mathf.Exp(-t * 26f) * 0.28f;   // tono, no ruido
                data[i] = (golpe + madera) * 0.8f;
            }
            Normalize(data, 0.58f);
            return FromData("SFX_Build", data, false);
        }

        /// <summary>Campanita de amanecer.</summary>
        public static AudioClip Dawn()
        {
            float dur = 2.2f;
            int n = (int)(SampleRate * dur);
            float[] data = new float[n];
            float[] notas = { 523.25f, 659.25f, 783.99f, 1046.5f };
            for (int k = 0; k < notas.Length; k++)
                RenderNote(data, k * 0.16f, dur - k * 0.16f, notas[k], 0.17f,
                           (ph, t) => Sine(ph) + Sine(ph * 2.01f) * 0.3f, 0.01f, 1.4f);
            Normalize(data, 0.5f);
            return FromData("SFX_Dawn", data, false);
        }

        /// <summary>Alerta grave de anochecer.</summary>
        public static AudioClip NightAlarm()
        {
            float dur = 2.0f;
            int n = (int)(SampleRate * dur);
            float[] data = new float[n];
            float ph = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                ph += (180f - t * 40f) / SampleRate;
                float trem = 0.6f + 0.4f * Mathf.Sin(t * Mathf.PI * 2f * 5f);
                data[i] = (Sine(ph) * 0.8f + Sine(ph * 2f) * 0.2f) * trem * Envelope(t, dur, 0.06f, 1.0f) * 0.7f;
            }
            Normalize(data, 0.5f);
            return FromData("SFX_NightAlarm", data, false);
        }

        /// <summary>Tos del comunero enfermo (marca ODS 3): golpes de garganta con tono.</summary>
        public static AudioClip Cough()
        {
            float dur = 0.4f;
            int n = (int)(SampleRate * dur);
            float[] data = new float[n];
            float ph = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float golpe = t < 0.11f ? 1f : (t < 0.2f ? 0.15f : (t < 0.29f ? 0.55f : 0f));
                float f = 165f - t * 60f;
                ph += f / SampleRate;
                float v = Sine(ph) * 0.5f + Triangle(ph * 2.3f) * 0.3f + Sine(ph * 4.7f) * 0.12f;
                data[i] = v * golpe * Mathf.Exp(-t * 5f);
            }
            Normalize(data, 0.4f);
            return FromData("SFX_Cough", data, false);
        }

        // ---------------------------------------------------------------
        //  Helpers
        // ---------------------------------------------------------------
        private static void RenderNote(float[] data, float start, float dur, float freq, float amp,
                                       Func<float, float, float> osc, float attack, float release)
        {
            int s = (int)(start * SampleRate);
            int len = (int)(dur * SampleRate);
            for (int i = 0; i < len; i++)
            {
                int idx = s + i;
                if (idx < 0 || idx >= data.Length) continue;
                float t = (float)i / SampleRate;
                data[idx] += osc(t * freq, t) * amp * Envelope(t, dur, attack, release);
            }
        }

        /// <summary>Bombo legüero: parche grave con caida de tono. Sin ruido de cuero.</summary>
        private static void RenderKick(float[] data, float start, float amp, float baseF = 70f)
        {
            int s = (int)(start * SampleRate);
            int len = (int)(0.35f * SampleRate);
            float ph = 0f, phClick = 0f;
            for (int i = 0; i < len; i++)
            {
                int idx = s + i;
                float t = (float)i / SampleRate;
                float f = Mathf.Max(30f, baseF - t * 90f);
                ph += f / SampleRate;
                phClick += 220f / SampleRate;
                if (idx < 0 || idx >= data.Length) continue;
                float cuerpo = Sine(ph) * Mathf.Exp(-t * 12f);
                float ataque = Triangle(phClick) * Mathf.Exp(-t * 70f) * 0.18f;
                data[idx] += (cuerpo + ataque) * amp;
            }
        }

        /// <summary>Eco de quebrada: dos repeticiones suaves. Da aire de altura a la mezcla.</summary>
        private static void Eco(float[] data, float retardo, float nivel, int repeticiones)
        {
            int d = (int)(retardo * SampleRate);
            if (d <= 0) return;
            for (int r = 1; r <= repeticiones; r++)
            {
                float g = nivel * Mathf.Pow(0.55f, r - 1);
                int off = d * r;
                for (int i = data.Length - 1; i >= off; i--)
                    data[i] += data[i - off] * g;
            }
        }

        /// <summary>
        /// Cierra un loop sin costura: funde la cola dentro de la cabeza y recorta ese trozo,
        /// para que el ultimo sample enlace con el primero (sin clic ni bajon a silencio).
        /// </summary>
        private static float[] LoopSeamless(float[] data, int fadeSamples)
        {
            int n = data.Length;
            int fade = Mathf.Clamp(fadeSamples, 1, n / 3);
            for (int i = 0; i < fade; i++)
            {
                float w = (float)i / fade;
                data[i] = data[i] * w + data[n - fade + i] * (1f - w);
            }
            var cortado = new float[n - fade];
            Array.Copy(data, cortado, n - fade);
            return cortado;
        }

        private static void Normalize(float[] data, float target)
        {
            float max = 0f;
            for (int i = 0; i < data.Length; i++) max = Mathf.Max(max, Mathf.Abs(data[i]));
            if (max < 0.0001f) return;
            float g = target / max;
            for (int i = 0; i < data.Length; i++) data[i] = Mathf.Clamp(data[i] * g, -1f, 1f);
        }

        private static AudioClip FromData(string name, float[] data, bool loop)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
