using UnityEngine;

namespace HayHustle
{
    /// <summary>
    /// A farm animal: grows Baby -> Adult -> Prime, gets hungry, eats grass,
    /// produces goods, panics near predators, and can starve.
    /// </summary>
    public class Animal : MonoBehaviour
    {
        public AnimalKind Kind;
        public Stage Stage;
        public float Age, Hunger = 1f, WellFedTime, BreedCooldown;
        public bool Rare;
        public int Field;   // which fenced field this animal lives in
        public bool IsCarried { get; private set; }
        public bool InBarn { get; private set; }

        public AnimalDef Def => GameData.Animals[Kind];
        FarmField Home => GameManager.I.Fields[Mathf.Clamp(Field, 0, GameManager.I.Fields.Count - 1)];
        public bool CanBreed => Stage != Stage.Baby && Hunger > 0.6f && !IsCarried && !InBarn && BreedCooldown <= 0f;

        const float PrimeAfterWellFed = 150f;
        const float StarveDeathTime = 45f;

        Transform visual;
        LimbAnimator limbs;
        float prodTimer, starveTimer, fertTimer, messTimer, panicUntil, nextKick, nextKickMessage, wanderWait;
        Vector3 wanderTarget, panicDir;
        GrassPatch food;
        bool eating, chewing;

        public string DisplayName
        {
            get
            {
                if (Rare) return Stage == Stage.Baby ? Def.RareName + " (baby)" : Def.RareName;
                switch (Stage)
                {
                    case Stage.Baby: return Def.BabyName;
                    case Stage.Prime: return "Prime " + Def.Name;
                    default: return Def.Name;
                }
            }
        }

        public void Init(AnimalKind kind, Stage stage, bool rare)
        {
            Kind = kind;
            Stage = stage;
            Rare = rare;
            Hunger = 1f;
            visual = Models.BuildAnimal(kind, rare, transform);
            limbs = visual.gameObject.AddComponent<LimbAnimator>();
            ApplyScale();
            wanderTarget = transform.position;
            prodTimer = Random.Range(0f, Def.ProduceTime * 0.5f);
        }

        void ApplyScale()
        {
            float s = Stage == Stage.Baby ? 0.55f : Stage == Stage.Prime ? 1.15f : 1f;
            if (visual) visual.localScale = Vector3.one * s;
        }

        void OnDestroy()
        {
            if (GameManager.I) GameManager.I.Animals.Remove(this);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            var gm = GameManager.I;

            if (IsCarried)
            {
                Hunger = Mathf.Max(0f, Hunger - Def.HungerPerSecond * dt);
                return;
            }

            BreedCooldown -= dt;
            Age += dt;
            Hunger = Mathf.Max(0f, Hunger - Def.HungerPerSecond * (Stage == Stage.Baby ? 0.6f : 1f) * dt);

            UpdateGrowth(gm, dt);

            if (Hunger <= 0f)
            {
                starveTimer += dt;
                if (starveTimer > StarveDeathTime) { gm.OnAnimalLost(this, "it starved"); return; }
            }
            else starveTimer = 0f;

            var threat = gm.NearestPredator(transform.position, 6f);
            if (threat != null)
            {
                panicUntil = Time.time + 2f;
                Vector3 away = transform.position - threat.transform.position;
                away.y = 0f;
                panicDir = away.sqrMagnitude > 0.01f ? away.normalized : transform.forward;

                // Defensive perks: Llama / Brave Cow kick big predators, Goat headbutts small ones.
                if (Stage != Stage.Baby && Time.time > nextKick && CanFightBack(threat) &&
                    GameManager.FlatDistance(threat.transform.position, transform.position) < 2.3f)
                {
                    nextKick = Time.time + 1.3f;
                    threat.Hit(transform.position);
                    if (Time.time > nextKickMessage)
                    {
                        nextKickMessage = Time.time + 10f;
                        gm.Notify($"Your {DisplayName.ToLower()} fought off a {threat.Def.Name.ToLower()}!", Palette.UiGreen);
                    }
                }
            }

            // Pig perk: Scrap Eater cleans up spoiled food so fewer rats come.
            if (Kind == AnimalKind.Pig && Stage != Stage.Baby && gm.Mess > 0)
            {
                messTimer += dt;
                if (messTimer >= 20f) { messTimer = 0f; gm.Mess--; }
            }
            bool panicking = Time.time < panicUntil;

            UpdateProduction(dt, panicking);

            // Cow perk: fertilizer makes nearby grass regrow.
            if (Kind == AnimalKind.Cow && Stage != Stage.Baby)
            {
                fertTimer += dt;
                if (fertTimer >= 5f)
                {
                    fertTimer = 0f;
                    var g = gm.GrassNear(transform.position, 2.5f);
                    if (g != null) g.Grow(0.2f);
                }
            }

            UpdateMovement(gm, dt, panicking);
        }

        bool CanFightBack(Predator p)
        {
            if (p.Def == null || !p.CanBeHit) return false;
            bool big = p.Kind == PredatorKind.Fox || p.Kind == PredatorKind.Wolf || p.Kind == PredatorKind.Bear || p.Kind == PredatorKind.Zombie;
            bool small = p.Kind == PredatorKind.Snake || p.Kind == PredatorKind.Rat;
            if (Kind == AnimalKind.Llama) return big;
            if (Kind == AnimalKind.Cow && Rare) return big;
            if (Kind == AnimalKind.Goat) return small;
            return false;
        }

        void UpdateGrowth(GameManager gm, float dt)
        {
            if (Stage == Stage.Baby && Age >= Def.GrowTime)
            {
                Stage = Stage.Adult;
                ApplyScale();
                gm.Notify($"Your {Def.BabyName.ToLower()} grew into an adult {Def.Name.ToLower()}!", Palette.UiGreen);
                FX.Text(transform.position + Vector3.up * 1.5f, "Grown up!", Palette.UiGreen);
                FX.Burst(transform.position + Vector3.up * 0.5f, Palette.UiGreen, 8, 0.08f, 2f);
                gm.AddXp(3);
            }
            else if (Stage == Stage.Adult)
            {
                if (Hunger > 0.6f) WellFedTime += dt;
                else WellFedTime = Mathf.Max(0f, WellFedTime - dt * 2f);
                if (WellFedTime >= PrimeAfterWellFed)
                {
                    Stage = Stage.Prime;
                    ApplyScale();
                    gm.Notify($"A well-fed {Def.Name.ToLower()} became PRIME (+50% production)!", Palette.UiGold);
                    FX.Text(transform.position + Vector3.up * 1.8f, "PRIME!", Palette.UiGold, 1.2f);
                    FX.Burst(transform.position + Vector3.up * 0.8f, Palette.Gold, 14, 0.1f, 3f);
                    gm.AddXp(5);
                }
            }
        }

        void UpdateProduction(float dt, bool panicking)
        {
            if (Stage == Stage.Baby || Hunger < 0.15f || panicking) return;
            float rate = Hunger > 0.5f ? 1f : 0.5f;
            if (Stage == Stage.Prime) rate *= 1.5f;
            if (Rare) rate *= 2f;
            prodTimer += dt * rate;
            if (prodTimer >= Def.ProduceTime)
            {
                prodTimer = 0f;
                Vector3 off = new Vector3(Random.Range(-0.4f, 0.4f), 0f, Random.Range(-0.4f, 0.4f));
                ProductPickup.Spawn(Def.Produces, transform.position - transform.forward * 0.7f + off);
            }
        }

        void UpdateMovement(GameManager gm, float dt, bool panicking)
        {
            Vector3 pos = transform.position;
            Vector3 target = pos;
            float speed = Def.Speed * (Stage == Stage.Baby ? 0.8f : 1f);
            chewing = false;

            if (panicking)
            {
                eating = false;
                target = pos + panicDir * 3f;
                speed *= 1.9f;
            }
            else
            {
                if (!eating && Hunger < 0.6f) eating = true;
                if (eating && Hunger >= 0.98f) eating = false;

                if (eating)
                {
                    if (food == null || food.Amount <= 0.02f) food = gm.NearestGrassWithFood(pos, Field);
                    if (food == null)
                    {
                        eating = false;
                        target = WanderTarget(gm, dt, pos);
                    }
                    else
                    {
                        Vector3 fp = food.transform.position;
                        fp.y = 0f;
                        if (GameManager.FlatDistance(pos, fp) < 0.8f)
                        {
                            float eaten = food.Eat(0.3f * dt);
                            Hunger = Mathf.Min(1f, Hunger + eaten * Def.FoodValue);
                            chewing = true;
                        }
                        else target = fp;
                    }
                }
                else target = WanderTarget(gm, dt, pos);
            }

            target = Home.Clamp(target);
            Vector3 d = target - pos;
            d.y = 0f;
            float dist = d.magnitude;
            bool moving = dist > 0.15f;
            if (moving)
            {
                Vector3 dir = d / dist;
                transform.position = pos + dir * Mathf.Min(dist, speed * dt);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 8f * dt);
            }

            if (visual)
            {
                float bob = moving ? Mathf.Abs(Mathf.Sin(Time.time * speed * 5f)) * 0.03f : 0f;
                visual.localPosition = new Vector3(0f, bob, 0f);
                visual.localRotation = chewing ? Quaternion.Euler(10f + Mathf.Sin(Time.time * 8f) * 4f, 0f, 0f) : Quaternion.identity;
            }
            if (limbs) limbs.Tick(moving ? speed : 0f, dt);
        }

        Vector3 WanderTarget(GameManager gm, float dt, Vector3 pos)
        {
            if (GameManager.FlatDistance(pos, wanderTarget) < 0.3f)
            {
                wanderWait -= dt;
                if (wanderWait <= 0f)
                {
                    wanderTarget = Home.RandomPoint();
                    wanderWait = Random.Range(1.5f, 4f);
                }
                return pos;
            }
            return wanderTarget;
        }

        // ---------------- Predators & barn ----------------

        public void SetCarried(Transform carrier, Vector3 localOffset)
        {
            IsCarried = true;
            eating = false;
            transform.SetParent(carrier, true);
            transform.localPosition = localOffset;
        }

        public void Release()
        {
            IsCarried = false;
            transform.SetParent(null, true);
            Vector3 p = transform.position;
            transform.position = new Vector3(p.x, 0f, p.z);
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            wanderTarget = Home.Clamp(p);
            panicUntil = Time.time + 1.5f;
        }

        public void EnterBarn()
        {
            InBarn = true;
            gameObject.SetActive(false);
        }

        public void ExitBarn(Vector3 pos)
        {
            transform.position = new Vector3(pos.x, 0f, pos.z);
            InBarn = false;
            BreedCooldown = 120f;
            gameObject.SetActive(true);
        }
    }
}
