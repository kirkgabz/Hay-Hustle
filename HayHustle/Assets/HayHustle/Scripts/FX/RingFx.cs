using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HayHustle
{
    /// <summary>An expanding ring of dots on the ground where you clicked.</summary>
    public class RingFx : MonoBehaviour
    {
        const float Life = 0.45f;
        const int Dots = 12;
        readonly List<Transform> dots = new List<Transform>();
        float t;

        public void Init(Color c)
        {
            for (int i = 0; i < Dots; i++)
            {
                var g = Visuals.Prim(PrimitiveType.Cube, transform, Vector3.zero, new Vector3(0.12f, 0.03f, 0.12f), c);
                g.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                dots.Add(g.transform);
            }
            Update();
        }

        void Update()
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / Life);
            float r = Mathf.Lerp(0.25f, 1.1f, k);
            float s = Mathf.Lerp(0.16f, 0.04f, k);
            for (int i = 0; i < dots.Count; i++)
            {
                float a = i * Mathf.PI * 2f / Dots;
                dots[i].localPosition = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                dots[i].localScale = new Vector3(s, 0.03f, s);
            }
            if (t >= Life) Destroy(gameObject);
        }
    }
}
