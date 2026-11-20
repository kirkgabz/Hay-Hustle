using System.Collections.Generic;
using UnityEngine;

namespace HayHustle
{
    /// <summary>A fenced field. The Home Pasture is owned from the start; more can be bought.</summary>
    public class FarmField
    {
        public FieldDef Def;
        public bool Owned;
        public Transform Root;
        public FieldSign Sign;
        public FieldGate GateObj;
        public readonly List<GrassPatch> Grass = new List<GrassPatch>();

        public int Index => Def.Index;
        public Vector3 Center => Def.Center;
        public float HalfX => Def.Cols * 1.25f + 0.55f;   // fence line
        public float HalfZ => Def.Rows * 1.25f + 0.55f;

        public bool Contains(Vector3 p, float margin = 0f) =>
            Mathf.Abs(p.x - Center.x) <= HalfX + margin && Mathf.Abs(p.z - Center.z) <= HalfZ + margin;

        /// <summary>Keeps animals inside the fence.</summary>
        public Vector3 Clamp(Vector3 p) => new Vector3(
            Mathf.Clamp(p.x, Center.x - HalfX + 0.8f, Center.x + HalfX - 0.8f), 0f,
            Mathf.Clamp(p.z, Center.z - HalfZ + 0.8f, Center.z + HalfZ - 0.8f));

        public Vector3 RandomPoint() => new Vector3(
            Center.x + Random.Range(-HalfX + 1.5f, HalfX - 1.5f), 0f,
            Center.z + Random.Range(-HalfZ + 1.5f, HalfZ - 1.5f));

        public Vector3 PlotPosition(int ix, int iz) => new Vector3(
            Center.x - (Def.Cols - 1) * 1.25f + ix * 2.5f, 0f,
            Center.z - (Def.Rows - 1) * 1.25f + iz * 2.5f);

        /// <summary>The gate is in the middle of the north side.</summary>
        public Vector3 Gate => new Vector3(Center.x, 0f, Center.z + HalfZ);
    }
}
