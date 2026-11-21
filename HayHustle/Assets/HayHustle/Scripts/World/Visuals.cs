using System.Collections.Generic;
using UnityEngine;

namespace HayHustle
{
    /// <summary>Surface textures generated in code (no image files needed).</summary>
    public enum Surface { Grain, Grass, Dirt, Soil, Wood, Planks, Roof, Stone, Leaves }

    /// <summary>
    /// Builds simple shapes with textured materials. If a real model prefab exists at
    /// Assets/HayHustle/Resources/Models/&lt;Name&gt;.prefab it is used instead.
    /// </summary>
    public static class Visuals
    {
        static Material baseMaterial;
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static readonly Dictionary<Surface, Texture2D> textures = new Dictionary<Surface, Texture2D>();

        /// <summary>Plain colour with a subtle grain so surfaces don't look flat.</summary>
        public static Material Mat(Color c) => MatTex(c, Surface.Grain, Vector2.one);

        public static Material MatTex(Color c, Surface s, Vector2 tiling)
        {
            string key = ColorUtility.ToHtmlStringRGBA(c) + s + tiling.x.ToString("0.##") + "x" + tiling.y.ToString("0.##");
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            if (baseMaterial == null) baseMaterial = Resources.Load<Material>("Materials/HH_Lit");   // keeps the shader in web builds
            if (baseMaterial == null)
            {
                // The render pipeline's default lit material (URP Lit in a URP project).
                var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tmp.SetActive(false);
                baseMaterial = tmp.GetComponent<Renderer>().sharedMaterial;
                Object.Destroy(tmp);
            }
            // Textures darken a little on average, so lift the colour to compensate.
            Color lifted = new Color(Mathf.Min(1f, c.r * 1.1f), Mathf.Min(1f, c.g * 1.1f), Mathf.Min(1f, c.b * 1.1f), c.a);
            m = new Material(baseMaterial) { color = lifted };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", lifted);
            var tex = Texture(s);
            m.mainTexture = tex;
            m.mainTextureScale = tiling;
            if (m.HasProperty("_BaseMap"))
            {
                m.SetTexture("_BaseMap", tex);
                m.SetTextureScale("_BaseMap", tiling);
            }
            float smooth = s == Surface.Roof ? 0.3f : s == Surface.Stone ? 0.2f : 0.12f;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
            cache[key] = m;
            return m;
        }

        /// <summary>Give an object a textured surface.</summary>
        public static GameObject Skin(GameObject g, Color c, Surface s, Vector2 tiling)
        {
            g.GetComponent<Renderer>().sharedMaterial = MatTex(c, s, tiling);
            return g;
        }

        public static GameObject Prim(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Color color,
            bool keepCollider = false, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(type);
            if (!keepCollider)
            {
                var col = go.GetComponent<Collider>();
                if (col) Object.DestroyImmediate(col);
            }
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            go.GetComponent<Renderer>().sharedMaterial = Mat(color);
            return go;
        }

        public static Transform NewRoot(Transform parent)
        {
            var t = new GameObject("Visual").transform;
            t.SetParent(parent, false);
            return t;
        }

        /// <summary>Returns an instance of Resources/Models/name, or null when no model was added yet.</summary>
        public static Transform TryModel(string name, Transform parent)
        {
            var prefab = Resources.Load<GameObject>("Models/" + name);
            if (prefab == null) return null;
            var go = Object.Instantiate(prefab, parent, false);
            go.name = "Visual";
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            return go.transform;
        }

        // ================= Procedural textures =================

        static Texture2D Texture(Surface s)
        {
            if (textures.TryGetValue(s, out var t) && t != null) return t;
            int size = s == Surface.Grass || s == Surface.Dirt ? 256 : 128;
            t = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4,
                name = "HH_" + s,
            };
            var px = new Color[size * size];
            int seed = (int)s * 97 + 13;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size;
                float g = Sample(s, u, v, seed);
                px[y * size + x] = new Color(g, g, g, 1f);
            }
            t.SetPixels(px);
            t.Apply(true);
            textures[s] = t;
            return t;
        }

        static float Sample(Surface s, float u, float v, int seed)
        {
            switch (s)
            {
                case Surface.Grass:
                {
                    float n = Fbm(u, v, 6, 4, seed);
                    float fine = Hash(Mathf.FloorToInt(u * 256), Mathf.FloorToInt(v * 256), seed + 7);
                    return 0.7f + 0.22f * n + 0.08f * fine;
                }
                case Surface.Dirt:
                {
                    float n = Fbm(u, v, 5, 4, seed);
                    float pebble = Hash(Mathf.FloorToInt(u * 64), Mathf.FloorToInt(v * 64), seed + 3) > 0.93f ? -0.12f : 0f;
                    return 0.74f + 0.22f * n + pebble;
                }
                case Surface.Soil:
                {
                    float rows = 0.5f + 0.5f * Mathf.Sin(v * Mathf.PI * 2f * 6f);
                    return 0.68f + 0.14f * rows + 0.16f * Fbm(u, v, 8, 3, seed);
                }
                case Surface.Wood:
                {
                    float grain = Mathf.Sin((u * 14f + Fbm(u, v, 4, 3, seed) * 2.5f) * Mathf.PI * 2f);
                    return 0.78f + 0.1f * grain + 0.1f * Fbm(u, v, 8, 2, seed + 1);
                }
                case Surface.Planks:
                {
                    const int boards = 6;
                    float bu = u * boards;
                    int board = Mathf.FloorToInt(bu);
                    float edge = bu - board;
                    float gap = edge < 0.05f || edge > 0.95f ? -0.3f : 0f;
                    float tone = (Hash(board, 0, seed) - 0.5f) * 0.12f;
                    float grain = Mathf.Sin((v * 9f + Fbm(u, v, 6, 2, seed) * 2f) * Mathf.PI * 2f) * 0.05f;
                    return 0.86f + tone + grain + gap;
                }
                case Surface.Roof:
                {
                    const int rowsCount = 8;
                    float ru = u * rowsCount;
                    int row = Mathf.FloorToInt(ru);
                    float inRow = ru - row;
                    float sv = v * 10f + (row % 2) * 0.5f;
                    int tile = Mathf.FloorToInt(sv);
                    float inTile = sv - tile;
                    float shade = inRow < 0.15f ? -0.25f : 0f;
                    float seam = inTile < 0.06f ? -0.15f : 0f;
                    float tone = (Hash(row, tile, seed) - 0.5f) * 0.14f;
                    return 0.85f + shade + seam + tone + 0.05f * Fbm(u, v, 8, 2, seed);
                }
                case Surface.Stone:
                {
                    const int rowsCount = 5;
                    float rv = v * rowsCount;
                    int row = Mathf.FloorToInt(rv);
                    float su = u * 4f + (row % 2) * 0.5f;
                    float inRow = rv - row, inCol = su - Mathf.Floor(su);
                    float mortar = inRow < 0.07f || inCol < 0.04f ? -0.3f : 0f;
                    float tone = (Hash(Mathf.FloorToInt(su), row, seed) - 0.5f) * 0.18f;
                    return 0.82f + tone + mortar + 0.1f * Fbm(u, v, 8, 3, seed);
                }
                case Surface.Leaves:
                {
                    float n = Fbm(u, v, 6, 4, seed);
                    return 0.62f + 0.38f * Mathf.SmoothStep(0.2f, 0.8f, n);
                }
                default:
                    return 0.9f + 0.1f * Fbm(u, v, 8, 3, seed);
            }
        }

        // Tileable value noise (wraps around at the texture edges, so no seams when repeated).
        static float Hash(int x, int y, int s)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + s * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                return (h & 0x7fffffff) / (float)int.MaxValue;
            }
        }

        static float ValueNoise(float x, float y, int period, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            int xa = ((x0 % period) + period) % period, ya = ((y0 % period) + period) % period;
            int xb = (xa + 1) % period, yb = (ya + 1) % period;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Mathf.Lerp(Hash(xa, ya, seed), Hash(xb, ya, seed), fx);
            float b = Mathf.Lerp(Hash(xa, yb, seed), Hash(xb, yb, seed), fx);
            return Mathf.Lerp(a, b, fy);
        }

        static float Fbm(float u, float v, int period, int octaves, int seed)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            for (int o = 0; o < octaves; o++)
            {
                sum += ValueNoise(u * period, v * period, period, seed + o * 31) * amp;
                norm += amp;
                period *= 2;
                amp *= 0.5f;
            }
            return sum / norm;
        }
    }
}
