using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HayHustle
{
    /// <summary>
    /// Anyone who works on the farm by themselves: hired Workers (paid in coins) and Survivors (paid in food).
    /// Jobs: Collector, Water Carrier, Factory Hand, Driver, Guard, Builder, Farmer, Cook, or Rest.
    /// </summary>
    public abstract class Laborer : MonoBehaviour
    {
        public string Status { get; protected set; } = "Starting work";

        /// <summary>-1 = visit all fields in turn (rotation); otherwise the index of the one field this laborer looks after.</summary>
        public int AssignedField = -1;

        public abstract JobKind Job { get; }
        public abstract string DisplayName { get; }
        protected virtual float SpeedBonus => 1f;
        protected virtual bool Ready => true;

        public bool UsesField => Job == JobKind.Collector || Job == JobKind.WaterCarrier || Job == JobKind.Guard;
        public string AreaName => Area(GameManager.I) is FarmField f ? f.Def.Name : "All fields (rotation)";

        const float BaseSpeed = 3.8f;
        static readonly HashSet<Object> claimed = new HashSet<Object>();
        public static void ClearClaims() => claimed.Clear();

        protected FarmerRig rig;
        Vector3 dest;
        bool walking;
        System.Action onArrive;
        Object watch;          // if this disappears while walking, the trip is cancelled
        bool hasWatch;
        float busyUntil, think, nextComplaint;
        int bucket, rotation;

        protected void SetupBody(Transform visual)
        {
            rig = visual.gameObject.AddComponent<FarmerRig>();
        }

        protected virtual void OnDestroy()
        {
            if (watch != null) claimed.Remove(watch);
        }

        // ================= Areas =================

        protected FarmField Area(GameManager gm) =>
            AssignedField >= 0 && AssignedField < gm.Fields.Count && gm.Fields[AssignedField].Owned ? gm.Fields[AssignedField] : null;

        FarmField RotationField(GameManager gm)
        {
            var owned = gm.Fields.Where(f => f.Owned).ToList();
            rotation = ((rotation % owned.Count) + owned.Count) % owned.Count;
            return owned[rotation];
        }

        FarmField WorkField(GameManager gm) => Area(gm) ?? RotationField(gm);

        bool InArea(GameManager gm, Vector3 p)
        {
            var a = Area(gm);
            return a == null || a.Contains(p, 2.5f);
        }

        // ================= Main loop =================

        protected virtual void OnTick(float dt) { }

        protected virtual void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || !Ready) return;
            OnTick(dt);
            if (Job == JobKind.Guard) { UpdateGuard(dt); return; }
            float speed = 0f;

            if (Time.time < busyUntil)
            {
                // doing an action
            }
            else if (walking)
            {
                if (hasWatch && watch == null) StopTrip();
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
                        float s = BaseSpeed * SpeedBonus;
                        Vector3 dir = d / dist;
                        transform.position += dir * Mathf.Min(dist, s * dt);
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * dt);
                        speed = s;
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

        protected void GoTo(Vector3 p, string status, System.Action arrive, Object watchObject = null)
        {
            dest = new Vector3(p.x, 0f, p.z);
            walking = true;
            Status = status;
            onArrive = arrive;
            watch = watchObject;
            hasWatch = watchObject != null;
            if (hasWatch) claimed.Add(watchObject);
        }

        /// <summary>Drop whatever they were doing (e.g. when their job changes).</summary>
        public void StopTrip()
        {
            walking = false;
            onArrive = null;
            prey = null;
            hasPatrol = false;
            Release();
        }

        void Release()
        {
            if (watch != null) claimed.Remove(watch);
            watch = null;
            hasWatch = false;
        }

        protected void Act(ActAnim a, float seconds)
        {
            if (rig) rig.Play(a, seconds);
            busyUntil = Time.time + seconds;
        }

        protected void Say(string text, Color c) => FX.Text(transform.position + Vector3.up * 2.4f, text, c, 0.8f);

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
                Status = "Fighting a " + prey.Def.Name.ToLower() + "!";
                if (dist > 1.6f)
                {
                    speed = 6.8f * SpeedBonus;
                    Step(d / dist, Mathf.Min(dist, speed * dt), dt);
                }
                else
                {
                    if (d.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(d);
                    if (prey.CanBeHit && Time.time >= nextStrike)
                    {
                        nextStrike = Time.time + 0.7f / SpeedBonus;
                        if (rig) rig.Play(ActAnim.Swing, 0.3f);
                        prey.Hit(pos);
                    }
                }
                hasPatrol = false;
            }
            else
            {
                if (!hasPatrol || GameManager.FlatDistance(pos, patrolPoint) < 0.5f)
                {
                    if (hasPatrol && Area(gm) == null) rotation++; // next field in the rotation
                    var field = WorkField(gm);
                    patrolPoint = field.RandomPoint();
                    hasPatrol = true;
                }
                Status = "Patrolling the " + WorkField(gm).Def.Name;
                Vector3 d = patrolPoint - pos;
                d.y = 0f;
                float dist = d.magnitude;
                if (dist > 0.01f)
                {
                    speed = 3f;
                    Step(d / dist, Mathf.Min(dist, speed * dt), dt);
                }
            }
            if (rig) rig.Tick(speed, dt);
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
            var area = Area(gm);
            foreach (var p in gm.Predators)
            {
                if (!p || p.Def == null) continue;
                float d = GameManager.FlatDistance(p.transform.position, transform.position);
                // A guard assigned to one field defends that field (and anything that comes very close).
                if (area != null && !area.Contains(p.transform.position, 15f) && d > 12f) continue;
                if (d < bd) { bd = d; best = p; }
            }
            return best;
        }

        // ================= Jobs =================

        void Think(GameManager gm)
        {
            switch (Job)
            {
                case JobKind.Collector: ThinkCollector(gm); break;
                case JobKind.WaterCarrier: ThinkWater(gm); break;
                case JobKind.FactoryHand: ThinkFactory(gm, false); break;
                case JobKind.Cook: ThinkFactory(gm, true); break;
                case JobKind.Driver: ThinkDriver(gm); break;
                case JobKind.Builder: ThinkBuilder(gm); break;
                case JobKind.Farmer: ThinkFarmer(gm); break;
                default: ThinkRest(gm); break;
            }
        }

        protected virtual void ThinkRest(GameManager gm)
        {
            Status = "Off duty";
            Idle(gm.BunkhouseDoor);
        }

        void ThinkCollector(GameManager gm)
        {
            if (gm.InventoryTotal >= gm.StorageCapacity)
            {
                Complain("Storage is full!");
                Idle(gm.WarehouseDoor);
                return;
            }
            ProductPickup best = null;
            float bd = float.MaxValue;
            foreach (var pk in ProductPickup.All)
            {
                if (!pk || claimed.Contains(pk) || !InArea(gm, pk.transform.position)) continue;
                float d = GameManager.FlatDistance(pk.transform.position, transform.position);
                if (d < bd) { bd = d; best = pk; }
            }
            if (best == null) { Idle(Area(gm) != null ? Area(gm).Gate : gm.WarehouseDoor); return; }
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
            if (bucket <= 0) { FetchWater(gm); return; }
            var field = WorkField(gm);
            GrassPatch best = NearestEmptyPlot(field.Grass);
            if (best == null && Area(gm) == null)
            {
                rotation++; // this field is done - move on to the next one
                best = NearestEmptyPlot(gm.Grass);
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

        void FetchWater(GameManager gm)
        {
            GoTo(gm.WellPosition + new Vector3(1.6f, 0f, 0f), "Fetching water", () =>
            {
                bucket = 5;
                Act(ActAnim.Fill, 1.2f);
                Say("+5 water", new Color(0.5f, 0.8f, 1f));
                FX.Play(Sfx.Water, 0.25f);
            });
        }

        GrassPatch NearestEmptyPlot(List<GrassPatch> plots)
        {
            GrassPatch best = null;
            float bd = float.MaxValue;
            foreach (var g in plots)
            {
                if (!g || !g.Selectable || claimed.Contains(g)) continue;
                float d = GameManager.FlatDistance(g.transform.position, transform.position);
                if (d < bd) { bd = d; best = g; }
            }
            return best;
        }

        void ThinkFactory(GameManager gm, bool kitchenOnly)
        {
            ProcessingBuilding best = null;
            float bd = float.MaxValue;
            foreach (var b in gm.Buildings)
            {
                if (!b || !b.NeedsWorker || claimed.Contains(b)) continue;
                if (kitchenOnly != (b.Recipe.Output == Product.Meal)) continue;
                float d = GameManager.FlatDistance(b.transform.position, transform.position);
                if (d < bd) { bd = d; best = b; }
            }
            if (best == null)
            {
                if (kitchenOnly && !gm.HasProduct(Product.Wheat, 2)) Status = "Needs wheat to cook";
                Idle(kitchenOnly ? new Vector3(-12f, 0f, 21f) : new Vector3(-4f, 0f, 13f));
                return;
            }
            var building = best;
            GoTo(building.FrontPosition, (kitchenOnly ? "Cooking at the " : "Working at the ") + building.Recipe.Building, () =>
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

        /// <summary>Builder: repairs the most damaged gate.</summary>
        void ThinkBuilder(GameManager gm)
        {
            FieldGate best = null;
            float worst = GameData.GateMaxHp;
            foreach (var g in gm.Gates)
            {
                if (!g || g.Hp >= GameData.GateMaxHp || claimed.Contains(g)) continue;
                if (g.Hp < worst) { worst = g.Hp; best = g; }
            }
            if (best == null) { Status = "All gates are strong"; Idle(new Vector3(0f, 0f, 4f)); return; }
            var gate = best;
            GoTo(gate.RepairPoint, $"Repairing a gate ({gate.Hp:0}/{GameData.GateMaxHp:0})", () =>
            {
                if (!gate) return;
                gate.Repair(20f * SpeedBonus);
                Act(ActAnim.Use, 1.0f);
            }, gate);
        }

        /// <summary>Farmer: harvest ripe crops, water planted ones, plant empty plots.</summary>
        void ThinkFarmer(GameManager gm)
        {
            CropPlot ripe = null, thirsty = null, empty = null;
            foreach (var p in gm.CropPlots)
            {
                if (!p || claimed.Contains(p)) continue;
                if (p.Ripe) { ripe = p; break; }
                if (p.NeedsWater && thirsty == null) thirsty = p;
                if (p.Empty && empty == null) empty = p;
            }
            if (ripe != null)
            {
                var plot = ripe;
                GoTo(plot.transform.position, "Harvesting " + plot.CropName, () =>
                {
                    if (!plot || !plot.Ripe) return;
                    plot.Harvest(null);
                    Act(ActAnim.Pickup, 0.6f);
                }, plot);
                return;
            }
            if (thirsty != null)
            {
                if (bucket <= 0) { FetchWater(gm); return; }
                var plot = thirsty;
                GoTo(plot.transform.position, "Watering " + plot.CropName, () =>
                {
                    if (!plot || !plot.NeedsWater) return;
                    plot.WaterFree();
                    bucket--;
                    Act(ActAnim.Plant, 0.6f);
                }, plot);
                return;
            }
            if (empty != null)
            {
                var crop = GameData.Crops[gm.FarmerCrop];
                if (gm.Rank < crop.UnlockRank) crop = GameData.Crops[CropKind.Wheat];
                if (gm.Seeds[(int)crop.Kind] <= 0) { Complain($"No {crop.Name} seeds - buy them in Shop > Seeds"); Idle(gm.CropFieldCenter + new Vector3(0f, 0f, 7f)); return; }
                var plot = empty;
                GoTo(plot.transform.position, "Planting " + crop.Name, () =>
                {
                    if (!plot || !plot.Empty) return;
                    if (plot.Plant(crop.Kind)) Act(ActAnim.Plant, 0.6f);
                }, plot);
                return;
            }
            Status = "Crops are growing";
            Idle(gm.CropFieldCenter + new Vector3(0f, 0f, 7f));
        }

        protected void Idle(Vector3 home)
        {
            if (!Status.StartsWith("Off") && !Status.StartsWith("Resting") && !Status.StartsWith("All") && !Status.StartsWith("Crops") && !Status.StartsWith("Needs"))
                Status = "Waiting for work";
            if (GameManager.FlatDistance(transform.position, home) > 4f)
                GoTo(home + new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f)), Status, null);
        }

        protected void Complain(string text)
        {
            if (Time.time < nextComplaint) return;
            nextComplaint = Time.time + 15f;
            Say(text, Palette.UiRed);
            GameManager.I.Notify($"{DisplayName}: {text}", Palette.UiRed);
        }
    }
}
