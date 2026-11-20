using UnityEngine;

namespace HayHustle
{
    /// <summary>
    /// Farm helpers bought in the shop.
    /// Cat: waits by the warehouse, hunts rats and snakes.
    /// Guard Dog: patrols the pasture, hunts foxes, wolves and bears.
    /// </summary>
    public class Helper : MonoBehaviour
    {
        public HelperKind Kind { get; private set; }
        public HelperDef Def { get; private set; }

        Predator target;
        Transform visual;
        LimbAnimator limbs;
        Vector3 wanderTarget;
        float nextHit, wanderWait, phase, retarget;

        public void Init(HelperKind kind)
        {
            Kind = kind;
            Def = GameData.Helpers[kind];
            visual = Models.BuildHelper(kind, transform);
            limbs = visual.gameObject.AddComponent<LimbAnimator>();
            wanderTarget = transform.position;
            GameManager.I.Helpers.Add(this);
        }

        void OnDestroy()
        {
            if (GameManager.I) GameManager.I.Helpers.Remove(this);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || Def == null) return;
            var gm = GameManager.I;

            retarget -= dt;
            if (target == null || !target.CanBeHit || retarget <= 0f)
            {
                target = FindTarget(gm);
                retarget = 1f;
            }

            Vector3 pos = transform.position;
            Vector3 goal;
            float speed = Def.Speed;
            if (target != null)
            {
                goal = target.transform.position;
                goal.y = 0f;
                if (GameManager.FlatDistance(goal, pos) < 1.4f && Time.time >= nextHit)
                {
                    nextHit = Time.time + Def.HitInterval;
                    target.Hit(pos);
                }
            }
            else
            {
                goal = Wander(gm, dt, pos);
                speed *= 0.35f;
            }

            Vector3 d = goal - pos;
            d.y = 0f;
            float dist = d.magnitude;
            bool moving = dist > 0.2f;
            if (moving)
            {
                Vector3 dir = d / dist;
                Vector3 next = pos + dir * Mathf.Min(dist, speed * dt);
                Vector2 flat = new Vector2(next.x, next.z);
                if (flat.magnitude > 50f) flat = flat.normalized * 50f;
                transform.position = new Vector3(flat.x, 0f, flat.y);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * dt);
                phase += dt * speed * 3f;
            }
            if (visual) visual.localPosition = new Vector3(0f, moving ? Mathf.Abs(Mathf.Sin(phase)) * 0.04f : 0f, 0f);
            if (limbs) limbs.Tick(moving ? speed : 0f, dt, 45f);
        }

        Predator FindTarget(GameManager gm)
        {
            Predator best = null;
            float bestD = 30f;
            foreach (var p in gm.Predators)
            {
                if (!p || !p.CanBeHit || System.Array.IndexOf(Def.Hunts, p.Kind) < 0) continue;
                float d = GameManager.FlatDistance(p.transform.position, transform.position);
                if (d < bestD) { bestD = d; best = p; }
            }
            return best;
        }

        Vector3 Wander(GameManager gm, float dt, Vector3 pos)
        {
            if (GameManager.FlatDistance(pos, wanderTarget) < 0.4f)
            {
                wanderWait -= dt;
                if (wanderWait <= 0f)
                {
                    wanderWait = Random.Range(2f, 5f);
                    wanderTarget = Kind == HelperKind.Cat
                        ? gm.WarehouseDoor + new Vector3(Random.Range(-4f, 4f), 0f, Random.Range(-3f, 3f))
                        : gm.RandomPasturePoint();
                }
                return pos;
            }
            return wanderTarget;
        }
    }
}
