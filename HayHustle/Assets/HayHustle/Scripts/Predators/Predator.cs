using UnityEngine;

namespace HayHustle
{
    /// <summary>
    /// Fox / Hawk / Wolf / Snake / Rat / Bear.
    ///  - Fox, hawk, wolf, bear: grab an animal and run back to the forest.
    ///  - Snake: hides in tall grass and swallows eggs on the ground (then chicks).
    ///  - Rat: runs to the warehouse, steals one item and runs away.
    /// Hit it with the net (or let a cat/dog bite it) enough times to catch it.
    /// </summary>
    public class Predator : MonoBehaviour
    {
        enum State { Hunting, Fleeing }

        public PredatorKind Kind { get; private set; }
        public PredatorDef Def { get; private set; }
        public int HitsLeft { get; private set; }
        public bool Hidden { get; private set; }
        public bool IsCarrying => carrying != null || stolen.HasValue;
        /// <summary>Height of the health bar above the predator.</summary>
        public float BarHeight => Kind == PredatorKind.Bear ? 2.7f : Kind == PredatorKind.Wolf ? 1.8f : Kind == PredatorKind.Fox ? 1.3f : Kind == PredatorKind.Hawk ? 0.9f : 0.7f;
        public Color FurColor => Kind == PredatorKind.Fox ? new Color(0.86f, 0.42f, 0.12f) : Kind == PredatorKind.Wolf ? new Color(0.5f, 0.5f, 0.52f) : Kind == PredatorKind.Bear ? new Color(0.33f, 0.21f, 0.12f) : Kind == PredatorKind.Snake ? new Color(0.3f, 0.5f, 0.15f) : Kind == PredatorKind.Hawk ? new Color(0.4f, 0.27f, 0.15f) : new Color(0.45f, 0.42f, 0.4f);

        public bool CanBeHit => !caught && Def != null && (!Def.Flying || transform.position.y < 3.5f);

        const float CruiseHeight = 9f;
        const float EscapeRadius = GameData.EscapeRadius;

        State state;
        Animal target, carrying;
        ProductPickup eggTarget;
        Product? stolen;
        int eggsEaten;
        Vector3 escapeDir;
        Transform visual, wingL, wingR;
        LimbAnimator limbs;
        float hitFlash, retarget, legPhase;
        bool caught;

        public void Init(PredatorKind kind)
        {
            Kind = kind;
            Def = GameData.Predators[kind];
            HitsLeft = Def.Hits;
            visual = Models.BuildPredator(kind, transform);
            wingL = visual.Find("WingL");
            wingR = visual.Find("WingR");
            limbs = visual.gameObject.AddComponent<LimbAnimator>();
            if (Def.Flying) transform.position += Vector3.up * CruiseHeight;
            GameManager.I.Predators.Add(this);
        }

        void OnDestroy()
        {
            if (GameManager.I) GameManager.I.Predators.Remove(this);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || Def == null) return;
            var gm = GameManager.I;

            Vector3 pos = transform.position;
            Vector3 flat = new Vector3(pos.x, 0f, pos.z);
            // Fence upgrades slow predators down inside your fields.
            float slow = !Def.Flying && gm.FenceLevel > 0 && gm.InOwnedField(flat) ? 1f - GameData.FenceSlow[gm.FenceLevel] : 1f;
            if (gm.NearLitLamp(flat)) slow *= 0.6f; // predators hate the lamp light
            Vector3 move = Vector3.zero;
            float desiredY = Def.Flying ? CruiseHeight : 0f;

            if (state == State.Hunting)
            {
                Vector3? goal;
                if (Kind == PredatorKind.Rat) goal = RatGoal(gm, flat);
                else
                {
                    goal = Kind == PredatorKind.Snake ? EggGoal(gm, flat) : null;
                    if (goal == null && state == State.Hunting) goal = AnimalGoal(gm, flat, pos, dt, ref desiredY);
                }

                if (state == State.Hunting)
                {
                    if (goal == null) StartFleeing(flat); // nothing left to take here
                    else
                    {
                        Vector3 d = goal.Value - flat;
                        float dist = d.magnitude;
                        if (dist > 0.01f) move = d / dist * Mathf.Min(dist, Def.Speed * slow * dt);
                    }
                }
            }

            if (state == State.Fleeing)
            {
                move = escapeDir * Def.FleeSpeed * slow * dt;
                if (Def.Flying) desiredY = CruiseHeight + 2f;
                if ((flat - GameManager.PastureCenter).magnitude > EscapeRadius)
                {
                    Escape(gm);
                    return;
                }
            }

            float y = Def.Flying ? Mathf.MoveTowards(pos.y, desiredY, 5f * dt) : 0f;
            transform.position = new Vector3(pos.x + move.x, y, pos.z + move.z);
            bool moving = move.sqrMagnitude > 0.000001f;
            if (moving) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(move.x, 0f, move.z)), 10f * dt);

            UpdateHiding(gm);
            Animate(moving, dt);
        }

        // ---------------- Goals ----------------

        Vector3? AnimalGoal(GameManager gm, Vector3 flat, Vector3 pos, float dt, ref float desiredY)
        {
            retarget -= dt;
            if (!ValidTarget(target) || retarget <= 0f)
            {
                target = PickTarget(gm, flat);
                retarget = 2f;
            }
            if (target == null) return null;

            Vector3 tp = target.transform.position;
            tp.y = 0f;
            float dist = GameManager.FlatDistance(tp, flat);
            if (Def.Flying) desiredY = dist < 7f ? 0.5f : CruiseHeight; // dive
            if (dist < 0.9f && (!Def.Flying || pos.y < 1.3f))
            {
                Grab(target, flat);
                return null;
            }
            return tp;
        }

        Vector3? EggGoal(GameManager gm, Vector3 flat)
        {
            if (eggsEaten >= 3) { StartFleeing(flat); return null; } // full, slithers away
            if (eggTarget == null) eggTarget = NearestEgg(flat);
            if (eggTarget == null) return null;

            Vector3 ep = eggTarget.transform.position;
            ep.y = 0f;
            if (GameManager.FlatDistance(ep, flat) < 0.6f)
            {
                FX.Text(eggTarget.transform.position + Vector3.up, "Egg eaten!", Palette.UiRed);
                FX.Burst(eggTarget.transform.position, new Color(0.96f, 0.92f, 0.8f), 6, 0.06f, 1.5f);
                Destroy(eggTarget.gameObject);
                eggTarget = null;
                eggsEaten++;
                gm.Notify("A snake swallowed an egg!", Palette.UiRed);
                return flat;
            }
            return ep;
        }

        Vector3? RatGoal(GameManager gm, Vector3 flat)
        {
            if (gm.InventoryTotal == 0) return null;
            Vector3 door = gm.WarehouseDoor;
            if (GameManager.FlatDistance(door, flat) < 1.0f)
            {
                Steal(gm);
                StartFleeing(flat);
                return null;
            }
            return door;
        }

        static ProductPickup NearestEgg(Vector3 from)
        {
            ProductPickup best = null;
            float bestD = float.MaxValue;
            foreach (var p in ProductPickup.All)
            {
                if (!p || p.Product != Product.Egg) continue;
                float d = GameManager.FlatDistance(p.transform.position, from);
                if (d < bestD) { bestD = d; best = p; }
            }
            return best;
        }

        static bool ValidTarget(Animal a) => a != null && a.isActiveAndEnabled && !a.IsCarried && !a.InBarn;

        Animal PickTarget(GameManager gm, Vector3 from)
        {
            Animal best = null;
            float bestScore = float.MaxValue;
            foreach (var a in gm.Animals)
            {
                if (!ValidTarget(a) || System.Array.IndexOf(Def.Targets, a.Kind) < 0) continue;
                // Turkey perk: hawks won't dive near adult turkeys.
                if (Kind == PredatorKind.Hawk && gm.AnyAnimalNear(AnimalKind.Turkey, a.transform.position, 5f, true)) continue;
                float s = (a.transform.position - from).sqrMagnitude;
                if (a.Stage == Stage.Baby) s *= 0.4f; // babies are easy prey
                if (gm.NearLitLamp(a.transform.position)) s *= 4f; // animals near a lit lamp are left alone if possible
                if (s < bestScore) { bestScore = s; best = a; }
            }
            return best;
        }

        // ---------------- Actions ----------------

        void Grab(Animal a, Vector3 flat)
        {
            carrying = a;
            target = null;
            Vector3 hold = Def.Flying ? new Vector3(0f, -0.45f, 0f)
                : Kind == PredatorKind.Bear ? new Vector3(0f, 0.6f, 1.3f)
                : new Vector3(0f, 0.35f, 0.55f);
            a.SetCarried(transform, hold);
            StartFleeing(flat);
            GameManager.I.Notify($"A {Def.Name.ToLower()} grabbed your {a.DisplayName.ToLower()}! Chase it and hit it!", Palette.UiRed, 4f);
            FX.Text(transform.position + Vector3.up * (BarHeight + 0.6f), "HELP!", Palette.UiRed, 1.2f);
            FX.Play(Sfx.Grab, 0.6f);
        }

        void Steal(GameManager gm)
        {
            int count = 0;
            for (int i = 0; i < gm.Inventory.Length; i++) if (gm.Inventory[i] > 0) count++;
            if (count == 0) return;
            int pick = Random.Range(0, count);
            for (int i = 0; i < gm.Inventory.Length; i++)
            {
                if (gm.Inventory[i] <= 0) continue;
                if (pick-- > 0) continue;
                stolen = (Product)i;
                gm.RemoveProduct((Product)i, 1);
                gm.Notify($"A rat stole {GameData.ProductName((Product)i)} from the warehouse! Catch it!", Palette.UiRed);
                FX.Text(transform.position + Vector3.up * 1.2f, $"-1 {GameData.ProductName((Product)i)}", Palette.UiRed);
                FX.Play(Sfx.Grab, 0.4f);
                return;
            }
        }

        void StartFleeing(Vector3 flat)
        {
            state = State.Fleeing;
            Vector3 d = flat - GameManager.PastureCenter;
            d.y = 0f;
            escapeDir = d.sqrMagnitude > 0.01f ? d.normalized : Vector3.forward;
        }

        void Escape(GameManager gm)
        {
            if (carrying != null)
            {
                var a = carrying;
                carrying = null;
                gm.OnAnimalLost(a, $"a {Def.Name.ToLower()} carried it away");
            }
            if (stolen.HasValue)
                gm.Notify($"A rat escaped with your {GameData.ProductName(stolen.Value)}.", Palette.UiRed);
            Destroy(gameObject);
        }

        public void Hit(Vector3 from)
        {
            if (!CanBeHit) return;
            HitsLeft--;
            hitFlash = 0.15f;
            Vector3 push = transform.position - from;
            push.y = 0f;
            if (push.sqrMagnitude > 0.001f) transform.position += push.normalized * 0.7f;

            FX.Play(Sfx.Hit, 0.7f);
            FX.Burst(transform.position + Vector3.up * 0.5f, FurColor, 6, 0.08f, 2.5f);
            if (HitsLeft > 0)
                FX.Text(transform.position + Vector3.up * (BarHeight + 0.5f), $"HIT!  {HitsLeft} left", new Color(1f, 0.9f, 0.4f), 1.1f);

            if (HitsLeft <= 0) { Catch(); return; }

            if (carrying != null && Random.value < 0.4f)
            {
                DropPrey();
                GameManager.I.Notify($"The {Def.Name.ToLower()} dropped your animal!", Palette.UiGreen);
            }
        }

        /// <summary>Stepped on a trap: caught at once (a bear only takes 4 hits).</summary>
        public void TrapHit()
        {
            if (caught) return;
            if (Kind == PredatorKind.Bear)
            {
                for (int i = 0; i < 4 && !caught; i++) Hit(transform.position + transform.forward);
                return;
            }
            Catch();
        }

        void Catch()
        {
            caught = true;
            DropPrey();
            var gm = GameManager.I;
            if (stolen.HasValue)
            {
                gm.AddProduct(stolen.Value);
                stolen = null;
            }
            gm.PredatorsCaught++;
            gm.AddCoins(Def.Bounty);
            gm.AddXp(Kind == PredatorKind.Bear ? 40 : Kind == PredatorKind.Rat ? 3 : 10);
            gm.Notify($"Caught the {Def.Name.ToLower()}! Sold to the zoo for {Def.Bounty} coins.", Palette.UiGreen, 4f);
            FX.Text(transform.position + Vector3.up * (BarHeight + 0.5f), $"CAUGHT!  +{Def.Bounty} coins", Palette.UiGreen, 1.4f);
            FX.Burst(transform.position + Vector3.up * 0.6f, Palette.Gold, 16, 0.12f, 4f);
            FX.Play(Sfx.Catch, 0.6f);
            Destroy(gameObject);
        }

        void DropPrey()
        {
            if (carrying == null) return;
            var a = carrying;
            carrying = null;
            a.Release();
            state = State.Hunting; // it will try again!
        }

        // ---------------- Visuals ----------------

        void UpdateHiding(GameManager gm)
        {
            if (Kind != PredatorKind.Snake || !visual) return;
            var g = gm.GrassNear(transform.position, 1.3f);
            bool tallGrass = g != null && g.Amount > 0.6f;
            bool playerNear = gm.Player && GameManager.FlatDistance(gm.Player.transform.position, transform.position) < 5f;
            bool catNear = gm.AnyHelperNear(HelperKind.Cat, transform.position, 4f);
            Hidden = tallGrass && !playerNear && !catNear;
            if (visual.gameObject.activeSelf == Hidden) visual.gameObject.SetActive(!Hidden);
        }

        void Animate(bool moving, float dt)
        {
            if (!visual) return;
            if (hitFlash > 0f) hitFlash -= dt;
            visual.localScale = Vector3.one * (1f + Mathf.Max(0f, hitFlash) * 1.5f);

            if (wingL && wingR)
            {
                float a = Mathf.Sin(Time.time * 12f) * 30f;
                wingL.localRotation = Quaternion.Euler(0f, 0f, -a);
                wingR.localRotation = Quaternion.Euler(0f, 0f, a);
            }
            else if (Kind == PredatorKind.Snake)
            {
                if (moving) legPhase += dt * 8f;
                visual.localRotation = Quaternion.Euler(0f, Mathf.Sin(legPhase) * 15f, 0f);
            }
            else
            {
                if (moving) legPhase += dt * 14f;
                visual.localPosition = new Vector3(0f, moving ? Mathf.Abs(Mathf.Sin(legPhase)) * 0.05f : 0f, 0f);
                if (limbs) limbs.Tick(moving ? (state == State.Fleeing ? Def.FleeSpeed : Def.Speed) : 0f, dt, 45f);
            }
        }
    }
}
