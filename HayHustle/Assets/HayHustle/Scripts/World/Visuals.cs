using System.Collections.Generic;
using UnityEngine;

namespace HayHustle
{
    /// <summary>
    /// Builds simple placeholder shapes. If a real model prefab exists at
    /// Assets/HayHustle/Resources/Models/&lt;Name&gt;.prefab it is used instead.
    /// </summary>
    public static class Visuals
    {
        static Material baseMaterial;
        static readonly Dictionary<Color, Material> cache = new Dictionary<Color, Material>();

        public static Material Mat(Color c)
        {
            if (cache.TryGetValue(c, out var m) && m != null) return m;
            if (baseMaterial == null)
            {
                // Grab the render pipeline's default lit material (URP Lit in a URP project).
                var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tmp.SetActive(false);
                baseMaterial = tmp.GetComponent<Renderer>().sharedMaterial;
                Object.Destroy(tmp);
            }
            m = new Material(baseMaterial) { color = c };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.15f);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.15f);
            cache[c] = m;
            return m;
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
    }
}
