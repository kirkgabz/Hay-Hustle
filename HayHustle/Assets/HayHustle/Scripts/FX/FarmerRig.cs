using UnityEngine;

namespace HayHustle
{
    public enum ActAnim { None, Swing, Fill, Plant, Pickup, Use, Wave, Cheer }

    /// <summary>
    /// Procedural body animation for the farmer, workers and the character-select previews:
    /// walking/running, idle breathing and actions (net swing, cranking the well, planting, picking up, waving).
    /// Needs the joints made by Models.BuildPerson (Upper, LegL/R, ArmL/R, ToolPivot).
    /// </summary>
    public class FarmerRig : MonoBehaviour
    {
        public float RunSpeed = 8.5f;
        public bool Unscaled;   // animate even while the game is paused (menus)

        Transform upper, legL, legR, armL, armR, tool;
        float walkPhase, actStart, actDuration;
        ActAnim act;

        float Now => Unscaled ? Time.unscaledTime : Time.time;

        void Awake()
        {
            upper = FindDeep(transform, "Upper");
            legL = FindDeep(transform, "LegL");
            legR = FindDeep(transform, "LegR");
            armL = FindDeep(transform, "ArmL");
            armR = FindDeep(transform, "ArmR");
            tool = FindDeep(transform, "ToolPivot");
        }

        public bool Acting => act != ActAnim.None && Now - actStart < actDuration;

        public void Play(ActAnim a, float duration)
        {
            act = a;
            actStart = Now;
            actDuration = Mathf.Max(0.05f, duration);
        }

        public void Tick(float speed, float dt)
        {
            bool moving = speed > 0.1f;
            if (moving) walkPhase += dt * speed * 1.6f;
            float amount = moving ? Mathf.Clamp01(speed / RunSpeed) * 40f + 12f : 0f;
            float sin = Mathf.Sin(walkPhase);

            float legLx = sin * amount, legRx = -sin * amount;
            float armLx = -sin * amount * 0.8f, armRx = sin * amount * 0.8f;
            float armLz = 0f, armRz = 0f;
            float upperX = moving ? 4f + amount * 0.1f : Mathf.Sin(Now * 2f) * 1.5f;
            float bodyY = moving ? Mathf.Abs(sin) * 0.06f : 0f;
            float toolX = 40f;
            float k = 1f - Mathf.Exp(-18f * dt);

            float t = (Now - actStart) / actDuration;
            if (act != ActAnim.None && t < 1f)
            {
                float w = Mathf.Sin(Mathf.PI * t); // 0 -> 1 -> 0
                float wiggle = Mathf.Sin(Now * 14f);
                switch (act)
                {
                    case ActAnim.Swing:
                        armRx = t < 0.35f ? Mathf.Lerp(0f, -170f, t / 0.35f) : Mathf.Lerp(-170f, -20f, (t - 0.35f) / 0.65f);
                        upperX = 12f * w;
                        toolX = 10f;
                        k = 1f;
                        break;
                    case ActAnim.Fill:
                        upperX = 25f * w;
                        armRx = (-75f + wiggle * 20f) * w;
                        armLx = (-75f - wiggle * 20f) * w;
                        break;
                    case ActAnim.Plant:
                        upperX = 55f * w;
                        bodyY = -0.25f * w;
                        armRx = (-45f + wiggle * 15f) * w;
                        armLx = -35f * w;
                        legLx = -20f * w;
                        legRx = 25f * w;
                        break;
                    case ActAnim.Pickup:
                        upperX = 50f * w;
                        bodyY = -0.15f * w;
                        armRx = -60f * w;
                        armLx = -30f * w;
                        break;
                    case ActAnim.Use:
                        upperX = 10f * w;
                        armRx = (-85f + wiggle * 20f) * w;
                        break;
                    case ActAnim.Wave:
                        armRx = -165f * w;
                        armRz = (25f + Mathf.Sin(Now * 10f) * 25f) * w;
                        upperX = -4f * w;
                        break;
                    case ActAnim.Cheer:
                        armRx = -170f * w;
                        armLx = -170f * w;
                        bodyY = Mathf.Abs(Mathf.Sin(Now * 9f)) * 0.15f * w;
                        break;
                }
            }
            else act = ActAnim.None;

            transform.localPosition = new Vector3(0f, bodyY, 0f);
            Rot(upper, upperX, 0f, k);
            Rot(legL, legLx, 0f, k);
            Rot(legR, legRx, 0f, k);
            Rot(armL, armLx, armLz, k);
            Rot(armR, armRx, armRz, k);
            Rot(tool, toolX, 0f, k);
        }

        static void Rot(Transform t, float x, float z, float k)
        {
            if (t) t.localRotation = Quaternion.Slerp(t.localRotation, Quaternion.Euler(x, 0f, z), k);
        }

        static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t)
            {
                var r = FindDeep(c, name);
                if (r) return r;
            }
            return null;
        }
    }
}
