using UnityEngine;
using UnityEngine.Rendering;

namespace HayHustle
{
    /// <summary>
    /// A lamp post that lights up at night. Predators avoid lit areas: they move slower near a lamp
    /// and prefer animals that are in the dark.
    /// </summary>
    public class Lamp : MonoBehaviour
    {
        static Material glassOn, glassOff;

        Light pointLight;
        Renderer glass;
        bool lit;

        public bool IsLit => lit;

        void Start()
        {
            var post = new Color(0.16f, 0.16f, 0.18f);
            Visuals.Prim(PrimitiveType.Cylinder, transform, new Vector3(0f, 0.15f, 0f), new Vector3(0.4f, 0.15f, 0.4f), post);
            Visuals.Prim(PrimitiveType.Cylinder, transform, new Vector3(0f, 1.6f, 0f), new Vector3(0.12f, 1.5f, 0.12f), post);
            Visuals.Prim(PrimitiveType.Cube, transform, new Vector3(0f, 3.05f, 0f), new Vector3(0.5f, 0.06f, 0.5f), post);
            glass = Visuals.Prim(PrimitiveType.Cube, transform, new Vector3(0f, 3.3f, 0f), new Vector3(0.34f, 0.44f, 0.34f), new Color(0.9f, 0.85f, 0.6f)).GetComponent<Renderer>();
            glass.shadowCastingMode = ShadowCastingMode.Off;
            Visuals.Prim(PrimitiveType.Cube, transform, new Vector3(0f, 3.57f, 0f), new Vector3(0.48f, 0.1f, 0.48f), post);

            var lightGo = new GameObject("Light");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 3.6f, 0f);
            pointLight = lightGo.AddComponent<Light>();
            pointLight.type = LightType.Point;
            // Bright enough to light a circle about 8 plots wide (~20 m) before fading out.
            pointLight.range = 22f;
            pointLight.intensity = 28f;
            pointLight.color = new Color(1f, 0.82f, 0.55f);
            pointLight.shadows = LightShadows.None;
            pointLight.enabled = false;

            if (glassOn == null)
            {
                glassOff = Visuals.Mat(new Color(0.55f, 0.55f, 0.5f));
                glassOn = new Material(Visuals.Mat(new Color(1f, 0.9f, 0.6f)));
                glassOn.EnableKeyword("_EMISSION");
                if (glassOn.HasProperty("_EmissionColor")) glassOn.SetColor("_EmissionColor", new Color(1f, 0.8f, 0.45f) * 3f);
            }
            glass.sharedMaterial = glassOff;
            GameManager.I.LampObjects.Add(this);
        }

        void OnDestroy()
        {
            if (GameManager.I) GameManager.I.LampObjects.Remove(this);
        }

        void Update()
        {
            var gm = GameManager.I;
            bool night = gm.DayNight && gm.DayNight.IsNight;
            if (night == lit) return;
            lit = night;
            pointLight.enabled = lit;
            glass.sharedMaterial = lit ? glassOn : glassOff;
        }
    }
}
