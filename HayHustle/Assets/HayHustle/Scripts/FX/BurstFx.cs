using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HayHustle
{
    /// <summary>A small burst of flying bits (feathers, water drops, grass, coins...).</summary>
    public class BurstFx : MonoBehaviour
    {
        const float Life = 0.7f;
        readonly List<Transform> bits = new List<Transform>();
        readonly List<Vector3> vel = new List<Vector3>();
        float t, size;

        public void Init(Color c, int count, float s, float power)
        {
            size = s;
            for (int i = 0; i < count; i++)
            {
                var g = Visuals.Prim(PrimitiveType.Cube, transform, Vector3.zero, Vector3.one * s, c);
                g.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                bits.Add(g.transform);
                vel.Add(new Vector3(Random.Range(-1f, 1f), Random.Range(0.8f, 1.8f), Random.Range(-1f, 1f)) * power * Random.Range(0.5f, 1f));
            }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            float k = Mathf.Max(0f, 1f - t / Life);
            for (int i = 0; i < bits.Count; i++)
            {
                Vector3 v = vel[i];
                v.y -= 12f * dt;
                Vector3 p = bits[i].position + v * dt;
                if (p.y < 0.03f)
                {
                    p.y = 0.03f;
                    v = new Vector3(v.x * 0.5f, -v.y * 0.3f, v.z * 0.5f);
                }
                vel[i] = v;
                bits[i].position = p;
                bits[i].localScale = Vector3.one * size * k;
                bits[i].Rotate(300f * dt, 200f * dt, 0f);
            }
            if (t >= Life) Destroy(gameObject);
        }
    }
}
