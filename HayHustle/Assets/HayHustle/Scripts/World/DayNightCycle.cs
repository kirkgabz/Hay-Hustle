using UnityEngine;
using UnityEngine.Rendering;

namespace HayHustle
{
    /// <summary>Moves the sun, switches to moonlight at night, darkens sky and fog.</summary>
    public class DayNightCycle : MonoBehaviour
    {
        public float DayLengthSeconds = 720f; // 12 real minutes = 1 farm day
        public float TimeOfDay = 0.3f;        // 0 = midnight, 0.25 = 6:00, 0.5 = noon
        public int Day = 1;
        public Light Sun;
        public event System.Action OnNewDay;

        public float Daylight { get; private set; } = 1f;
        public bool IsNight => Daylight < 0.12f;

        public string Clock
        {
            get
            {
                float h = TimeOfDay * 24f;
                int hh = Mathf.FloorToInt(h);
                int mm = Mathf.FloorToInt((h - hh) * 60f);
                return $"{hh:00}:{mm:00}";
            }
        }

        Light moon;
        Material sky;

        void Start()
        {
            var go = new GameObject("Moon");
            moon = go.AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = new Color(0.55f, 0.65f, 1f);
            moon.shadows = LightShadows.None;
            moon.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            if (RenderSettings.skybox != null)
            {
                sky = new Material(RenderSettings.skybox); // copy so the project asset is not modified
                RenderSettings.skybox = sky;
            }
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 45f;
            RenderSettings.fogEndDistance = 140f;
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm != null && gm.Mode == GameMode.Playing)
            {
                TimeOfDay += Time.deltaTime / DayLengthSeconds;
                if (TimeOfDay >= 1f)
                {
                    TimeOfDay -= 1f;
                    Day++;
                    OnNewDay?.Invoke();
                }
            }
            Apply();
        }

        void Apply()
        {
            Daylight = Mathf.Clamp01(-Mathf.Cos(TimeOfDay * Mathf.PI * 2f) * 1.6f + 0.35f);

            if (Sun)
            {
                Sun.transform.rotation = Quaternion.Euler(TimeOfDay * 360f - 90f, 170f, 0f);
                Sun.intensity = Mathf.Lerp(0f, 1.25f, Daylight);
                Sun.color = Color.Lerp(new Color(1f, 0.55f, 0.3f), new Color(1f, 0.96f, 0.88f), Daylight);
            }
            if (moon) moon.intensity = Mathf.Lerp(0.35f, 0f, Daylight * 2f);

            RenderSettings.ambientLight = Color.Lerp(new Color(0.07f, 0.09f, 0.16f), new Color(0.5f, 0.52f, 0.55f), Daylight);
            RenderSettings.fogColor = Color.Lerp(new Color(0.05f, 0.07f, 0.12f), new Color(0.72f, 0.8f, 0.88f), Daylight);
            if (sky != null && sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", Mathf.Lerp(0.12f, 1.3f, Daylight));
        }
    }
}
