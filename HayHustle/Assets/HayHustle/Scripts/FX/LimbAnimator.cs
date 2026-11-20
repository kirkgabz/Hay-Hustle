using System.Collections.Generic;
using UnityEngine;

namespace HayHustle
{
    /// <summary>
    /// Swings the legs of animals, predators and helpers while they walk.
    /// Works on any visual whose leg pivots are named "Leg" (see Models.Leg).
    /// </summary>
    public class LimbAnimator : MonoBehaviour
    {
        readonly List<Transform> legs = new List<Transform>();
        float phase;

        void Awake()
        {
            foreach (Transform c in transform)
                if (c.name == "Leg") legs.Add(c);
        }

        public void Tick(float speed, float dt, float swing = 35f)
        {
            if (legs.Count == 0 || dt <= 0f) return;
            bool moving = speed > 0.05f;
            if (moving) phase += dt * (5f + speed * 3.5f);
            float amp = moving ? swing : 0f;
            float k = 1f - Mathf.Exp(-15f * dt);
            for (int i = 0; i < legs.Count; i++)
            {
                // 4 legs: diagonal pairs move together. 2 legs: alternate.
                float sign = legs.Count == 4 ? ((i == 0 || i == 3) ? 1f : -1f) : (i % 2 == 0 ? 1f : -1f);
                float target = Mathf.Sin(phase) * amp * sign;
                legs[i].localRotation = Quaternion.Slerp(legs[i].localRotation, Quaternion.Euler(target, 0f, 0f), k);
            }
        }
    }
}
