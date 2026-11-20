using System.Collections.Generic;
using UnityEngine;

namespace HayHustle
{
    /// <summary>
    /// A hired worker who walks around the farm and does one job by themselves:
    /// Collector (picks up goods), Water Carrier (replants grass), Factory Hand (runs buildings), Driver (sells by truck).
    /// </summary>
    public class Worker : MonoBehaviour
    {
        public WorkerKind Kind { get; private set; }
        public WorkerDef Def { get; private set; }
        public string Status { get; private set; } = "Starting work";

        const float Speed = 3.8f;
        static readonly HashSet<Object> claimed = new HashSet<Object>();
        public static void ClearClaims() => claimed.Clear();

        FarmerRig rig;
        Vector3 dest;
        bool walking;
        System.Action onArrive;
        Object watch;          // if this disappears while walking, the trip is cancelled
        bool hasWatch;
        float busyUntil, think, nextComplaint;
        int bucket;

        public void Init(WorkerKind kind)
        {
            Kind = kind;
            Def = GameData.Workers[kind];
            var visual = Models.BuildWorker(kind, transform);
            rig = visual.gameObject.AddComponent<FarmerRig>();
            GameManager.I.Workers.Add(this);
        }

        void OnDestroy()
        {
            if (watch != null) claimed.Remove(watch);
            if (GameManager.I) GameManager.I.Workers.Remove(this);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || Def == null) return;
            if (Kind == WorkerKind.Guard) { UpdateGuard(dt); return; }
            float speed = 0f;

            if (Time.time < busyUntil)
            {
                // doing an action
            }
            else if (walking)
            {
                if (hasWatch && watch == null) { StopTrip(); }
                else
                {
                    Vector3 d = dest - transform.position;
                    d.y = 0f;
                    float dist = d.magnitude;
                    if (dist < 0.35f)
                    {
                        walking = false;
                        var a = onArrive;
                        onArrive = null;
                        a?.Invoke();
                        Release();
                    }
                    else
                    {
                        Vector3 dir = d / dist;
                        transform.position += dir * Mathf.Min(dist, Speed * dt);
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * dt);
                        speed = Speed;
                    }
                }
            }
            else
            {
                think -= dt;
                if (think <= 0f)
                {
                    think = 0.6f;
                    Think(GameManager.I);
                }
            }
            if (rig) rig.Tick(speed, dt);
        }

        void GoTo(Vector3 p, string status, System.Action arrive, Object watchObject = null)
        {
            dest = new Vector3(p.x, 0f, p.z);
            walking = true;
            Status = status;
            onArrive = arrive;
            watch = watchObject;
            hasWatch = watchObject != null;
            if (hasWatch) claimed.Add(watchObject);
        }

        void StopTrip()
        {
            walking = false;
            onArrive = null;
            Release();
        }

        void Release()
        {
            if (watch != null) claimed.Remove(watch);
            watch = null;
            hasWatch = false;
        }

        void Act(ActAnim a, float seconds)
        {
            rig.Play(a, seconds);
            busyUntil = Time.time + seconds;
        }

        void Say(string text, Color c) => FX.Text(transform.position + Vector3.up * 2.4f, text, c, 0.8f);

        // ================= Guard =================

        Predator prey;
        Vector3 patrolPoint;
        bool hasPatrol;
        float guardRetarget, nextStrike;

        void UpdateGuard(float dt)
        {
            var gm = GameManager.I;
            guardRetarget -= dt;
            if (prey == null || guardRetarget <= 0f)
            {
                prey = FindPrey(gm);
                guardRetarget = 0.5f;
            }

            float speed = 0f;
            Vector3 pos = transform.position;
            if (prey != null)
            {
                Vector3 d = prey.transform.position - pos;
                d.y = 0f;
                float dist = d.magnitude;
                Status = "Chasing a " + prey.Def.Name.ToLower() + "!";
                if (dist > 1.6f)
                {
                    speed = 6.8f;
                    Step(d / dist, Mathf.Min(dist, speed * dt), dt);
                }
                else
                {
                    if (d.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(d);
                    if (prey.CanBeHit && Time.time >= nextStrike)
                    {
                        nextStrike = Time.time + 0.7f;
                        rig.Play(ActAnim.Swing, 0.3f);
                        prey.Hit(pos);
                    }
                }
                hasPatrol = false;
            }
            else
            {
                Status = "Patrolling";
                if (!hasPatrol || GameManager.FlatDistance(pos, patrolPoint) < 0.5f)
                {
                    patrolPoint = gm.RandomPasturePoint();
                    hasPatrol = true;
                }
                Vector3 d = patrolPoint - pos;
                d.y = 0f;
                float dist = d.magnitude;
                if (dist > 0.01f)
                {
                    speed = 3f;
                    Step(d / dist, Mathf.Min(dist, speed * dt), dt);
                }
            }
            rig.Tick(speed, dt);
        }

        void Step(Vector3 dir, float distance, float dt)
        {
            transform.position += dir * distance;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 12f * dt);
        }

        Predator FindPrey(GameManager gm)
        {
            Predator best = null;
            float bd = 45f;
            foreach (var p in gm.Predators)
            {
                if (!p || p.Def == null) continue;
                float d = GameManager.FlatDistance(p.transform.position, transform.position);
                if (d < bd) { bd = d; best = p; }
            }
            return best;
        }

        // ================= Jobs =================

        void Think(GameManager gm)
        {
            switch (Kind)
            {
                case WorkerKind.Collector: ThinkCollector(gm); break;
                case WorkerKind.WaterCarrier: ThinkWater(gm); break;
                case WorkerKind.FactoryHand: ThinkFactory(gm); break;
                case WorkerKind.Driver: ThinkDriver(gm); break;
            }
        }

        void ThinkCollector(GameManager gm)
        {
            if (gm.InventoryTotal >= gm.WarehouseCapacity)
            {
                Complain("Warehouse is full!");
                Idle(gm.WarehouseDoor);
                return;
            }
            ProductPickup best = null;
            float bd = float.MaxValue;
            foreach (var pk in ProductPickup.All)
            {
                if (!pk || claimed.Contains(pk)) continue;
                float d = GameManager.FlatDistance(pk.transform.position, transform.position);
                if (d < bd) { bd = d; best = pk; }
            }
            if (best == null) { Idle(gm.WarehouseDoor); return; }
            var target = best;
            GoTo(target.transform.position, "Collecting " + GameData.ProductName(target.Product), () =>
            {
                if (!target) return;
                if (gm.AddProduct(target.Product))
                {
                    Act(ActAnim.Pickup, 0.35f);
                    Say("+1 " + GameData.ProductName(target.Product), Color.white);
                    FX.Play(Sfx.Pickup, 0.25f);
                    Destroy(target.gameObject);
                }
            }, target);
        }

        void ThinkWater(GameManager gm)
        {
            if (bucket <= 0)
            {
                GoTo(gm.WellPosition + new Vector3(1.6f, 0f, 0f), "Fetching water", () =>
                {
                    bucket = 5;
                    Act(ActAnim.Fill, 1.2f);
                    Say("+5 water", new Color(0.5f, 0.8f, 1f));
                    FX.Play(Sfx.Water, 0.25f);
                });
                return;
            }
            GrassPatch best = null;
            float bd = float.MaxValue;
            foreach (var g in gm.Grass)
            {
                if (!g || !g.Selectable || claimed.Contains(g)) continue;
                float d = GameManager.FlatDistance(g.transform.position, transform.position);
                if (d < bd) { bd = d; best = g; }
            }
            if (best == null) { Idle(gm.WellPosition + new Vector3(2.5f, 0f, -1f)); return; }
            var plot = best;
            GoTo(plot.transform.position, "Planting grass", () =>
            {
                if (!plot || !plot.Selectable) return;
                plot.PlantByWorker();
                bucket--;
                Act(ActAnim.Plant, 0.6f);
            }, plot);
        }

        void ThinkFactory(GameManager gm)
        {
            ProcessingBuilding best = null;
            float bd = float.MaxValue;
            foreach (var b in gm.Buildings)
            {
                if (!b || !b.NeedsWorker || claimed.Contains(b)) continue;
                float d = GameManager.FlatDistance(b.transform.position, transform.position);
                if (d < bd) { bd = d; best = b; }
            }
            if (best == null) { Idle(new Vector3(-4f, 0f, 13f)); return; }
            var building = best;
            GoTo(building.FrontPosition, "Working at the " + building.Recipe.Building, () =>
            {
                if (!building) return;
                building.Operate(null);
                Act(ActAnim.Use, 0.6f);
            }, building);
        }

        void ThinkDriver(GameManager gm)
        {
            var truck = gm.Truck;
            if (truck == null || truck.Away || truck.DriverSellableCount() < 8) { Idle(truck ? truck.transform.position + new Vector3(3f, 0f, -3f) : transform.position); return; }
            GoTo(truck.transform.position + new Vector3(1.8f, 0f, -2.5f), "Loading the truck", () =>
            {
                int loaded = truck.DriverLoadAndSend();
                if (loaded > 0) { Act(ActAnim.Use, 0.8f); Say($"Sent {loaded} goods to town", Palette.UiGold); }
            });
        }

        void Idle(Vector3 home)
        {
            Status = "Waiting for work";
            if (GameManager.FlatDistance(transform.position, home) > 4f)
                GoTo(home + new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f)), "Walking", null);
        }

        void Complain(string text)
        {
            if (Time.time < nextComplaint) return;
            nextComplaint = Time.time + 15f;
            Say(text, Palette.UiRed);
            GameManager.I.Notify($"{Def.Name}: {text}", Palette.UiRed);
        }
    }
}
