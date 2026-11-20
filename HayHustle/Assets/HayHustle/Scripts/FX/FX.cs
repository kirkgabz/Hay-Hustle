using System.Collections.Generic;
using UnityEngine;

namespace HayHustle
{
    public enum Sfx { Pickup, Water, Plant, Hit, Whoosh, Catch, Warn, Coin, Error, Grab, Lost, RankUp, Click }

    public class Popup
    {
        public Vector3 World;
        public string Text;
        public Color Color;
        public float Born, Life, Size;
    }

    /// <summary>
    /// Action feedback: floating text, particle bursts, click rings and sound effects.
    /// Sounds are generated in code, so no audio files are needed.
    /// </summary>
    public class FX : MonoBehaviour
    {
        public static FX I { get; private set; }
        public readonly List<Popup> Popups = new List<Popup>();

        const int Rate = 22050;
        AudioSource source;
        readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();

        void Awake()
        {
            I = this;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            BuildSounds();
        }

        void Update() => Popups.RemoveAll(p => Time.unscaledTime - p.Born > p.Life);

        // ---------------- Public helpers ----------------

        public static void Text(Vector3 world, string text, Color color, float size = 1f)
        {
            if (I == null) return;
            I.Popups.Add(new Popup { World = world, Text = text, Color = color, Born = Time.unscaledTime, Life = 1.6f, Size = size });
            if (I.Popups.Count > 40) I.Popups.RemoveAt(0);
        }

        public static void Play(Sfx s, float volume = 0.5f)
        {
            if (I == null || !I.clips.TryGetValue(s, out var c)) return;
            I.source.PlayOneShot(c, volume);
        }

        public static void Burst(Vector3 pos, Color color, int count = 10, float size = 0.12f, float power = 3f)
        {
            var go = new GameObject("FX Burst");
            go.transform.position = pos;
            go.AddComponent<BurstFx>().Init(color, count, size, power);
        }

        public static void Ring(Vector3 pos, Color color)
        {
            var go = new GameObject("FX Ring");
            go.transform.position = new Vector3(pos.x, 0.08f, pos.z);
            go.AddComponent<RingFx>().Init(color);
        }

        // ---------------- Generated sounds ----------------

        void BuildSounds()
        {
            clips[Sfx.Pickup] = Make("pickup", 0.12f, t => Sweep(880f, 1500f, 0.12f, t));
            clips[Sfx.Coin]   = Make("coin", 0.28f, t => Sq(t < 0.08f ? 988f : 1319f, t) * 0.5f);
            clips[Sfx.Plant]  = Make("plant", 0.12f, t => Sweep(300f, 650f, 0.12f, t));
            clips[Sfx.Click]  = Make("click", 0.04f, t => Sine(1400f, t));
            clips[Sfx.Hit]    = Make("hit", 0.16f, t => Sweep(260f, 70f, 0.16f, t) * 0.8f + Noise() * 0.35f * Mathf.Clamp01(1f - t / 0.05f));
            clips[Sfx.Whoosh] = Make("whoosh", 0.18f, t => Noise() * 0.35f * Mathf.Sin(Mathf.PI * t / 0.18f));
            clips[Sfx.Water]  = Make("water", 0.5f, t => (Noise() * 0.3f + Sine(180f + 60f * Mathf.Sin(t * 40f), t) * 0.3f) * Mathf.Sin(Mathf.PI * t / 0.5f));
            clips[Sfx.Catch]  = Make("catch", 0.4f, t => Sq(Notes(t, 0.1f, 523f, 659f, 784f, 1047f), t) * 0.45f);
            clips[Sfx.RankUp] = Make("rankup", 0.72f, t => Sq(Notes(t, 0.12f, 523f, 659f, 784f, 1047f, 1319f, 1568f), t) * 0.45f);
            clips[Sfx.Warn]   = Make("warn", 0.6f, t => Sq(Notes(t, 0.15f, 660f, 440f, 660f, 440f), t) * 0.4f);
            clips[Sfx.Error]  = Make("error", 0.2f, t => Sq(140f, t) * 0.4f);
            clips[Sfx.Grab]   = Make("grab", 0.3f, t => Sq(Mathf.Lerp(700f, 200f, t / 0.3f), t) * 0.4f);
            clips[Sfx.Lost]   = Make("lost", 0.6f, t => Sine(Notes(t, 0.2f, 392f, 330f, 262f), t));
        }

        static AudioClip Make(string name, float dur, System.Func<float, float> wave)
        {
            int n = Mathf.CeilToInt(dur * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Clamp01(t / 0.005f) * Mathf.Clamp01((dur - t) / 0.05f);
                data[i] = Mathf.Clamp(wave(t), -1f, 1f) * env * 0.6f;
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Sine(float f, float t) => Mathf.Sin(2f * Mathf.PI * f * t);
        static float Sq(float f, float t) => Mathf.Sin(2f * Mathf.PI * f * t) >= 0f ? 0.6f : -0.6f;
        static float Sweep(float f0, float f1, float d, float t) => Mathf.Sin(2f * Mathf.PI * (f0 * t + (f1 - f0) * t * t / (2f * d)));
        static float Noise() => Random.Range(-1f, 1f);
        static float Notes(float t, float step, params float[] f) => f[Mathf.Min(f.Length - 1, (int)(t / step))];
    }
}
