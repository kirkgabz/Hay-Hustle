using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HayHustle
{
    /// <summary>Builds the whole farm from code, so no scene setup is needed.</summary>
    public static class WorldBuilder
    {
        public static Light Sun { get; private set; }

        const PrimitiveType Cube = PrimitiveType.Cube, Cyl = PrimitiveType.Cylinder, Sphere = PrimitiveType.Sphere;
        static readonly Vector3 YardCenter = new Vector3(0f, 0f, 6f);

        public static void Build(GameManager gm)
        {
            var world = new GameObject("World").transform;
            World = world;

            Visuals.Skin(Visuals.Prim(PrimitiveType.Plane, world, Vector3.zero, new Vector3(20f, 1f, 20f), Palette.Grass, true), Palette.Grass, Surface.Grass, new Vector2(35f, 35f)).name = "Ground";
            Visuals.Skin(Visuals.Prim(Cube, world, new Vector3(0f, 0.005f, 11f), new Vector3(46f, 0.01f, 12f), Palette.Dirt), Palette.Dirt, Surface.Dirt, new Vector2(8f, 2f)).name = "Farmyard";
            Visuals.Skin(Visuals.Prim(Cube, world, new Vector3(0f, 0.01f, 47f), new Vector3(5f, 0.01f, 60f), Palette.Road), Palette.Road, Surface.Dirt, new Vector2(1f, 12f)).name = "Road";

            gm.Fields.Clear();
            foreach (var def in GameData.Fields) gm.Fields.Add(new FarmField { Def = def });
            ResetFields(gm);
            ApplyFenceLevel(gm);

            BuildWell(world);
            BuildNest(world);
            foreach (var r in GameData.Recipes) BuildProcessing(world, r);
            BuildBarn(world);
            BuildWarehouse(world);
            gm.Truck = BuildTruck(world);
            BuildOrderBoard(gm, world);
            BuildDecor(world);
            BuildForest(world);
            BuildScatter(world);
            BuildPath(world);
            BuildStage(world);
            SetupLightAndCamera(gm);

            // Merge scenery that never moves into big batches - much faster on laptop graphics.
            foreach (var name in new[] { "Forest", "Decor", "Scatter", "Path" })
            {
                var t = world.Find(name);
                if (t) StaticBatchingUtility.Combine(t.gameObject);
            }
        }

        static Transform World;

        // ---------------- Nature details ----------------

        static bool FreeSpot(Vector3 p)
        {
            if ((p - GameManager.PastureCenter).magnitude > GameData.ForestInner - 3f) return false;
            foreach (var def in GameData.Fields)                                    // all fields (owned or for sale)
                if (Mathf.Abs(p.x - def.Center.x) < def.Cols * 1.25f + 3f && Mathf.Abs(p.z - def.Center.z) < def.Rows * 1.25f + 3f) return false;
            if (p.x > -25f && p.x < 25f && p.z > 4f && p.z < 19f) return false;    // farmyard
            if (Mathf.Abs(p.x) < 4f && p.z > 15f) return false;                     // road
            if (p.x < -14f && p.x > -26f && p.z > -19f && p.z < 10f) return false;  // west buildings
            if (p.x > 14f && p.x < 26f && p.z > -21f && p.z < 12f) return false;   // east buildings
            return true;
        }

        static void NoShadow(GameObject g) => g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        static void BuildScatter(Transform world)
        {
            var root = new GameObject("Scatter").transform;
            root.SetParent(world);
            var saved = Random.state;
            Random.InitState(777);
            Color[] flowers = { new Color(0.97f, 0.85f, 0.2f), new Color(0.95f, 0.42f, 0.55f), new Color(0.97f, 0.97f, 0.95f), new Color(0.62f, 0.5f, 0.95f) };
            Color[] greens = { new Color(0.3f, 0.5f, 0.2f), new Color(0.42f, 0.6f, 0.25f), new Color(0.27f, 0.45f, 0.19f) };
            Color[] rocks = { new Color(0.55f, 0.54f, 0.5f), new Color(0.46f, 0.45f, 0.43f) };
            Color stem = new Color(0.25f, 0.5f, 0.18f);

            // Ground colour variation (lighter/darker grass areas)
            for (int i = 0; i < 45; i++)
            {
                Vector3 p = new Vector3(Random.Range(-60f, 60f), 0f, Random.Range(-66f, 50f));
                if (!FreeSpot(p)) continue;
                float r = Random.Range(3f, 7f);
                NoShadow(Visuals.Prim(Cyl, root, p + Vector3.up * 0.01f, new Vector3(r, 0.01f, r * Random.Range(0.6f, 1f)), greens[Random.Range(0, greens.Length)], false, new Vector3(0f, Random.Range(0f, 180f), 0f)));
            }

            int placed = 0, tries = 0;
            while (placed < 240 && tries < 4000)
            {
                tries++;
                Vector3 p = new Vector3(Random.Range(-60f, 60f), 0f, Random.Range(-66f, 50f));
                if (!FreeSpot(p)) continue;
                float roll = Random.value;
                if (roll < 0.45f)
                {
                    Color c = flowers[Random.Range(0, flowers.Length)];
                    for (int k = 0; k < 4; k++)
                    {
                        Vector3 o = p + new Vector3(Random.Range(-0.6f, 0.6f), 0f, Random.Range(-0.6f, 0.6f));
                        NoShadow(Visuals.Prim(Cube, root, o + Vector3.up * 0.12f, new Vector3(0.03f, 0.24f, 0.03f), stem));
                        NoShadow(Visuals.Prim(Sphere, root, o + Vector3.up * 0.26f, Vector3.one * 0.14f, c));
                    }
                }
                else if (roll < 0.68f)
                {
                    Color rc = rocks[Random.Range(0, rocks.Length)];
                    Visuals.Skin(Visuals.Prim(Sphere, root, p + Vector3.up * 0.12f, new Vector3(Random.Range(0.4f, 1f), Random.Range(0.3f, 0.55f), Random.Range(0.4f, 1f)),
                        rc, false, new Vector3(0f, Random.Range(0f, 360f), 0f)), rc, Surface.Stone, Vector2.one);
                }
                else if (roll < 0.88f)
                {
                    Color g = greens[Random.Range(0, greens.Length)];
                    float s = Random.Range(0.8f, 1.4f);
                    Visuals.Prim(Sphere, root, p + Vector3.up * 0.45f * s, new Vector3(1.3f, 0.9f, 1.2f) * s, g);
                    Visuals.Prim(Sphere, root, p + new Vector3(0.5f, 0.35f, 0.3f) * s, new Vector3(0.9f, 0.7f, 0.9f) * s, g);
                }
                else
                {
                    for (int k = 0; k < 4; k++)
                        NoShadow(Visuals.Prim(Cube, root, p + new Vector3(Random.Range(-0.3f, 0.3f), 0.2f, Random.Range(-0.3f, 0.3f)), new Vector3(0.1f, 0.4f, 0.1f),
                            greens[1], false, new Vector3(Random.Range(-15f, 15f), Random.Range(0f, 360f), 0f)));
                }
                placed++;
            }
            Random.state = saved;
        }

        static void BuildPath(Transform world)
        {
            var root = new GameObject("Path").transform;
            root.SetParent(world);
            Color stone = new Color(0.62f, 0.6f, 0.56f);
            // From the pasture gate past the well to the truck stop.
            for (float z = 4.2f; z < 18.5f; z += 1.35f)
            {
                if (z > 7f && z < 10f) continue; // the well stands here
                float x = Mathf.Sin(z * 1.7f) * 0.25f + (z > 7f && z < 10.5f ? 1.6f : 0f);
                NoShadow(Visuals.Prim(Cyl, root, new Vector3(x, 0.03f, z), new Vector3(0.9f, 0.025f, 0.7f), stone, false, new Vector3(0f, z * 40f, 0f)));
            }
        }

        // ---------------- Player ----------------

        public static PlayerController SpawnPlayer(CharacterChoice choice)
        {
            var go = new GameObject("Player (" + choice + ")");
            go.transform.position = new Vector3(3f, 0.05f, 6f);
            go.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.92f, 0f);
            cc.stepOffset = 0.3f;
            cc.skinWidth = 0.05f;

            var visual = Models.BuildFarmer(choice, go.transform);

            var lampGo = new GameObject("Lantern");
            lampGo.transform.SetParent(go.transform, false);
            lampGo.transform.localPosition = new Vector3(0f, 2.4f, 0.3f);
            var lamp = lampGo.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.range = 14f;
            lamp.intensity = 10f;
            lamp.color = new Color(1f, 0.78f, 0.45f);
            lamp.shadows = LightShadows.None;
            lamp.enabled = false;

            var p = go.AddComponent<PlayerController>();
            p.Setup(visual, lamp);
            return p;
        }

        // ---------------- Farm pieces ----------------

        static Transform Root(Transform world, string name, Vector3 pos, float yaw)
        {
            var t = new GameObject(name).transform;
            t.SetParent(world);
            t.position = pos;
            t.rotation = Quaternion.Euler(0f, yaw, 0f);
            return t;
        }

        static float Face(Vector3 from, Vector3 to)
        {
            Vector3 d = to - from;
            return Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        }

        // ---------------- Fields ----------------

        /// <summary>Home pasture is built; other fields get a "for sale" sign.</summary>
        public static void ResetFields(GameManager gm)
        {
            foreach (var f in gm.Fields)
            {
                if (f.Index == 0)
                {
                    if (!f.Owned) BuildField(gm, f);
                    continue;
                }
                if (f.Owned)
                {
                    foreach (var g in f.Grass) gm.Grass.Remove(g);
                    f.Grass.Clear();
                    if (f.Root) Object.Destroy(f.Root.gameObject);
                    f.Root = null;
                    f.Owned = false;
                }
                if (f.Sign == null) BuildFieldSign(f);
            }
        }

        public static void BuildField(GameManager gm, FarmField f)
        {
            if (f.Sign) Object.Destroy(f.Sign.gameObject);
            f.Sign = null;
            f.Owned = true;
            f.Root = new GameObject(f.Def.Name).transform;
            f.Root.SetParent(World);

            // Fence with a gate gap in the middle of the north side
            var fence = new GameObject("Fence").transform;
            fence.SetParent(f.Root);
            float x0 = f.Center.x - f.HalfX, x1 = f.Center.x + f.HalfX;
            float z0 = f.Center.z - f.HalfZ, z1 = f.Center.z + f.HalfZ;
            Rail(fence, new Vector3(x0, 0, z0), new Vector3(x1, 0, z0));
            Rail(fence, new Vector3(x0, 0, z0), new Vector3(x0, 0, z1));
            Rail(fence, new Vector3(x1, 0, z0), new Vector3(x1, 0, z1));
            Rail(fence, new Vector3(x0, 0, z1), new Vector3(f.Center.x - 2f, 0, z1));
            Rail(fence, new Vector3(f.Center.x + 2f, 0, z1), new Vector3(x1, 0, z1));

            for (int ix = 0; ix < f.Def.Cols; ix++)
            for (int iz = 0; iz < f.Def.Rows; iz++)
            {
                var go = new GameObject("Grass Patch");
                go.transform.SetParent(f.Root);
                go.transform.position = f.PlotPosition(ix, iz);
                var g = go.AddComponent<GrassPatch>();
                g.Field = f.Index;
                g.Init();
                f.Grass.Add(g);
                gm.Grass.Add(g);
            }
        }

        static void BuildFieldSign(FarmField f)
        {
            var root = new GameObject(f.Def.Name + " (for sale)").transform;
            root.SetParent(World);
            root.position = f.Gate + new Vector3(0f, 0f, 1.2f);
            root.rotation = Quaternion.Euler(0f, 0f, 0f);
            Models.BuildSign(root, "For sale");
            // Corner stakes show where the field will be
            foreach (var cx in new[] { -1f, 1f })
            foreach (var cz in new[] { -1f, 1f })
                Visuals.Prim(Cube, root, new Vector3(cx * f.HalfX, 0.4f, cz * f.HalfZ - f.HalfZ - 1.2f), new Vector3(0.12f, 0.8f, 0.12f), Palette.Wood);
            var sign = root.gameObject.AddComponent<FieldSign>();
            sign.FieldIndex = f.Index;
            sign.Range = 3.5f;
            f.Sign = sign;
        }

        static void Rail(Transform root, Vector3 a, Vector3 b)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            Vector3 mid = (a + b) / 2f;
            float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            foreach (float h in new[] { 0.3f, 0.6f, 0.9f })
            {
                var r = Visuals.Prim(Cube, root, mid + Vector3.up * h, new Vector3(0.08f, 0.12f, len), Palette.Wood, false, new Vector3(0f, yaw, 0f));
                r.name = h < 0.4f ? "RailLow" : "Rail";
            }
            int posts = Mathf.Max(1, Mathf.RoundToInt(len / 2.2f));
            for (int i = 0; i <= posts; i++)
                Visuals.Prim(Cube, root, Vector3.Lerp(a, b, i / (float)posts) + Vector3.up * 0.55f, new Vector3(0.16f, 1.1f, 0.16f), Palette.DarkWood).name = "Post";
        }

        /// <summary>Fence look per upgrade level: wood, sturdy dark wood (3 rails), iron.</summary>
        public static void ApplyFenceLevel(GameManager gm)
        {
            Color rail = gm.FenceLevel == 0 ? Palette.Wood : gm.FenceLevel == 1 ? new Color(0.36f, 0.24f, 0.13f) : new Color(0.42f, 0.44f, 0.48f);
            Color post = gm.FenceLevel == 0 ? Palette.DarkWood : gm.FenceLevel == 1 ? new Color(0.24f, 0.15f, 0.08f) : new Color(0.25f, 0.26f, 0.3f);
            foreach (var f in gm.Fields)
            {
                if (!f.Owned || !f.Root) continue;
                var fence = f.Root.Find("Fence");
                if (!fence) continue;
                foreach (Transform t in fence)
                {
                    if (t.name == "RailLow") t.gameObject.SetActive(gm.FenceLevel > 0);
                    var r = t.GetComponent<Renderer>();
                    if (r) r.sharedMaterial = Visuals.Mat(t.name == "Post" ? post : rail);
                }
            }
        }

        static void BuildOrderBoard(GameManager gm, Transform world)
        {
            Vector3 pos = new Vector3(7f, 0f, 17.5f);
            var root = Root(world, "Order Board", pos, Face(pos, new Vector3(0f, 0f, 10f)));
            Models.BuildOrderBoard(root);
            root.gameObject.AddComponent<OrderBoard>().Range = 3.5f;
            gm.OrderBoardTransform = root;
        }

        // ---------------- Character select stage ----------------

        public static readonly Vector3 StageCameraPos = new Vector3(0f, 1.75f, 31.5f);
        public static readonly Vector3 StageLookAt = new Vector3(0f, 1.15f, 26f);
        static Transform stage;
        static FarmerRig boyRig, girlRig;
        static Renderer boyPad, girlPad;
        static float nextWave;

        static void BuildStage(Transform world)
        {
            stage = new GameObject("Character Stage").transform;
            stage.SetParent(world);
            boyRig = StageFarmer(CharacterChoice.Boy, new Vector3(-1.15f, 0f, 26f), out boyPad);
            girlRig = StageFarmer(CharacterChoice.Girl, new Vector3(1.15f, 0f, 26f), out girlPad);
            stage.gameObject.SetActive(false);
        }

        static FarmerRig StageFarmer(CharacterChoice c, Vector3 pos, out Renderer pad)
        {
            var root = new GameObject(c + " preview").transform;
            root.SetParent(stage);
            root.position = pos;
            root.rotation = Quaternion.Euler(0f, c == CharacterChoice.Boy ? 12f : -12f, 0f);
            pad = Visuals.Prim(Cyl, root, new Vector3(0f, 0.05f, 0f), new Vector3(1.3f, 0.05f, 1.3f), Palette.Wood).GetComponent<Renderer>();
            var visual = Models.BuildFarmer(c, root);
            var rig = visual.gameObject.AddComponent<FarmerRig>();
            rig.Unscaled = true;
            return rig;
        }

        public static void ShowStage(bool on)
        {
            if (stage) stage.gameObject.SetActive(on);
        }

        public static Vector3 StagePosition(CharacterChoice c) => new Vector3(c == CharacterChoice.Boy ? -1.15f : 1.15f, 0f, 26f);

        /// <summary>Idle animation on the character select screen. The chosen farmer waves.</summary>
        public static void TickStage(CharacterChoice selected)
        {
            if (!stage || !stage.gameObject.activeSelf) return;
            float dt = Time.unscaledDeltaTime;
            var chosen = selected == CharacterChoice.Boy ? boyRig : girlRig;
            if (Time.unscaledTime > nextWave) { nextWave = Time.unscaledTime + 3.5f; chosen.Play(ActAnim.Wave, 1.4f); }
            boyRig.Tick(0f, dt);
            girlRig.Tick(0f, dt);
            boyPad.sharedMaterial = Visuals.Mat(selected == CharacterChoice.Boy ? Palette.Gold : Palette.Wood);
            girlPad.sharedMaterial = Visuals.Mat(selected == CharacterChoice.Girl ? Palette.Gold : Palette.Wood);
        }

        public static void WaveNow(CharacterChoice c)
        {
            var rig = c == CharacterChoice.Boy ? boyRig : girlRig;
            if (rig) rig.Play(ActAnim.Wave, 1.4f);
            nextWave = Time.unscaledTime + 3.5f;
        }

        static void BuildWell(Transform world)
        {
            var root = Root(world, "Well", new Vector3(0f, 0f, 8.5f), 0f);
            Visuals.Skin(Visuals.Prim(Cyl, root, new Vector3(0, 0.45f, 0), new Vector3(1.6f, 0.45f, 1.6f), Palette.Stone, true), Palette.Stone, Surface.Stone, new Vector2(3f, 1f));
            Visuals.Prim(Cyl, root, new Vector3(0, 0.91f, 0), new Vector3(1.25f, 0.01f, 1.25f), new Color(0.12f, 0.25f, 0.4f));
            Visuals.Prim(Cube, root, new Vector3(-0.75f, 1.4f, 0), new Vector3(0.12f, 1.9f, 0.12f), Palette.DarkWood);
            Visuals.Prim(Cube, root, new Vector3(0.75f, 1.4f, 0), new Vector3(0.12f, 1.9f, 0.12f), Palette.DarkWood);
            Visuals.Prim(Cube, root, new Vector3(0, 2.4f, 0), new Vector3(1.9f, 0.1f, 1.3f), Palette.Roof);
            Visuals.Prim(Cyl, root, new Vector3(0, 1.9f, 0), new Vector3(0.12f, 0.75f, 0.12f), Palette.Wood, false, new Vector3(0, 0, 90f));
            Visuals.Prim(Cyl, root, new Vector3(0, 1.45f, 0), new Vector3(0.3f, 0.18f, 0.3f), Palette.Wood);
            root.gameObject.AddComponent<Well>().Range = 3f;
            GameManager.I.WellPosition = root.position;
        }

        static void BuildNest(Transform world)
        {
            var root = Root(world, "Nest", new Vector3(-5f, 0f, 7.5f), 0f);
            Visuals.Prim(Cyl, root, new Vector3(0, 0.12f, 0), new Vector3(1.1f, 0.12f, 1.1f), Palette.Straw);
            Visuals.Prim(Cyl, root, new Vector3(0, 0.2f, 0), new Vector3(0.75f, 0.05f, 0.75f), Palette.Wood);
            var egg = Visuals.Prim(Sphere, root, new Vector3(0, 0.33f, 0), new Vector3(0.2f, 0.26f, 0.2f), Palette.White);
            root.gameObject.AddComponent<Nest>().Init(egg.transform);
        }

        static Transform House(Transform world, string name, Vector3 pos, Vector3 size, Color wall, Color roof, float yaw, out float top)
        {
            var root = Root(world, name, pos, yaw);
            Visuals.Skin(Visuals.Prim(Cube, root, new Vector3(0, size.y / 2f, 0), size, wall, true), wall, Surface.Planks, new Vector2(size.x / 1.6f, 1f));

            float halfW = size.x / 2f, slope = 30f;
            float rise = Mathf.Tan(slope * Mathf.Deg2Rad) * halfW;
            float slab = halfW / Mathf.Cos(slope * Mathf.Deg2Rad) + 0.35f;
            Vector2 roofTiling = new Vector2(slab / 2f, (size.z + 0.5f) / 2f);
            Visuals.Skin(Visuals.Prim(Cube, root, new Vector3(-halfW / 2f, size.y + rise / 2f, 0), new Vector3(slab, 0.16f, size.z + 0.5f), roof, false, new Vector3(0, 0, slope)), roof, Surface.Roof, roofTiling);
            Visuals.Skin(Visuals.Prim(Cube, root, new Vector3(halfW / 2f, size.y + rise / 2f, 0), new Vector3(slab, 0.16f, size.z + 0.5f), roof, false, new Vector3(0, 0, -slope)), roof, Surface.Roof, roofTiling);

            // Triangular gable ends (a squashed diamond whose lower half hides inside the walls).
            var gable = new GameObject("Gable").transform;
            gable.SetParent(root, false);
            gable.localPosition = new Vector3(0, size.y, 0);
            gable.localScale = new Vector3(1f, rise / halfW, 1f);
            float side = size.x / Mathf.Sqrt(2f);
            Visuals.Skin(Visuals.Prim(Cube, gable, Vector3.zero, new Vector3(side, side, size.z - 0.05f), wall, false, new Vector3(0, 0, 45f)), wall, Surface.Planks, new Vector2(size.x / 1.6f, 1f));

            Visuals.Prim(Cube, root, new Vector3(0, 1.1f, size.z / 2f + 0.03f), new Vector3(Mathf.Min(1.6f, size.x * 0.35f), 2.2f, 0.06f), Palette.DarkWood);
            Color glass = new Color(0.62f, 0.8f, 0.9f);
            Visuals.Prim(Cube, root, new Vector3(size.x * 0.32f, size.y * 0.6f, size.z / 2f + 0.03f), new Vector3(0.8f, 0.7f, 0.05f), glass);
            Visuals.Prim(Cube, root, new Vector3(-size.x * 0.32f, size.y * 0.6f, size.z / 2f + 0.03f), new Vector3(0.8f, 0.7f, 0.05f), glass);
            top = size.y + rise;
            return root;
        }

        static void BuildProcessing(Transform world, RecipeDef r)
        {
            var root = House(world, r.Building, r.Position, new Vector3(5f, 3f, 4f), r.Wall, r.Roof, Face(r.Position, YardCenter), out float top);
            Visuals.Prim(Cube, root, new Vector3(1.3f, top - 0.2f, -0.6f), new Vector3(0.5f, 1.4f, 0.5f), Palette.Stone);
            var lamp = Visuals.Prim(Sphere, root, new Vector3(0, top + 0.7f, 0), Vector3.one * 0.6f, Color.white);
            root.gameObject.AddComponent<ProcessingBuilding>().Init(r, lamp.GetComponent<Renderer>());
        }

        static void BuildBarn(Transform world)
        {
            Vector3 pos = new Vector3(19.5f, 0f, -6f);
            var root = House(world, "Breeding Barn", pos, new Vector3(7f, 4.2f, 6f), Palette.BarnRed, Palette.Roof, Face(pos, GameManager.PastureCenter), out _);
            Visuals.Prim(Cube, root, new Vector3(0, 1.1f, 3.07f), new Vector3(0.12f, 2.6f, 0.04f), Palette.White, false, new Vector3(0, 0, 32f));
            Visuals.Prim(Cube, root, new Vector3(0, 1.1f, 3.07f), new Vector3(0.12f, 2.6f, 0.04f), Palette.White, false, new Vector3(0, 0, -32f));
            root.gameObject.AddComponent<BreedingBarn>().Range = 5.2f;
        }

        static void BuildWarehouse(Transform world)
        {
            Vector3 pos = new Vector3(19f, 0f, 7f);
            var root = House(world, "Warehouse", pos, new Vector3(6f, 3.5f, 5f), new Color(0.48f, 0.53f, 0.58f), new Color(0.22f, 0.24f, 0.27f), Face(pos, YardCenter), out _);
            root.gameObject.AddComponent<Warehouse>().Range = 4.8f;
            GameManager.I.WarehouseTransform = root;
            Vector3 door = root.position + root.forward * 3.4f;
            GameManager.I.WarehouseDoor = new Vector3(door.x, 0f, door.z);
        }

        static Truck BuildTruck(Transform world)
        {
            var stop = Root(world, "Truck Stop", new Vector3(0f, 0f, 21f), 0f);
            var body = new GameObject("Truck").transform;
            body.SetParent(world);
            body.position = stop.position;
            Color paint = new Color(0.2f, 0.45f, 0.75f);
            Visuals.Prim(Cube, body, new Vector3(0, 1.0f, 1.3f), new Vector3(2.1f, 1.3f, 1.6f), paint, true);
            Visuals.Prim(Cube, body, new Vector3(0, 1.35f, 2.11f), new Vector3(1.8f, 0.55f, 0.04f), new Color(0.65f, 0.82f, 0.92f));
            Visuals.Prim(Cube, body, new Vector3(0, 0.7f, -1.0f), new Vector3(2.2f, 0.4f, 3.0f), Palette.Wood, true);
            Visuals.Prim(Cube, body, new Vector3(1.05f, 1.1f, -1.0f), new Vector3(0.1f, 0.45f, 3.0f), Palette.DarkWood);
            Visuals.Prim(Cube, body, new Vector3(-1.05f, 1.1f, -1.0f), new Vector3(0.1f, 0.45f, 3.0f), Palette.DarkWood);
            foreach (var wx in new[] { -1.1f, 1.1f })
            foreach (var wz in new[] { -1.5f, 1.4f })
                Visuals.Prim(Cyl, body, new Vector3(wx, 0.4f, wz), new Vector3(0.8f, 0.15f, 0.8f), Palette.Black, false, new Vector3(0, 0, 90f));
            var truck = stop.gameObject.AddComponent<Truck>();
            truck.Init(body);
            return truck;
        }

        static void BuildDecor(Transform world)
        {
            var root = new GameObject("Decor").transform;
            root.SetParent(world);
            foreach (var p in new[] { new Vector3(15f, 0, -11f), new Vector3(16.6f, 0, -11.6f), new Vector3(15.8f, 0, -13f), new Vector3(-16f, 0, 9f) })
                Visuals.Prim(Cyl, root, p + Vector3.up * 0.6f, new Vector3(1.2f, 0.7f, 1.2f), Palette.Straw, false, new Vector3(0, 0, 90f));
            // feeding trough
            Visuals.Prim(Cube, root, new Vector3(8f, 0.35f, 2.2f), new Vector3(2.4f, 0.5f, 0.7f), Palette.Wood);
            // scarecrow
            Visuals.Prim(Cube, root, new Vector3(0f, 1.1f, -11f), new Vector3(0.12f, 2.2f, 0.12f), Palette.DarkWood);
            Visuals.Prim(Cube, root, new Vector3(0f, 1.6f, -11f), new Vector3(1.6f, 0.12f, 0.12f), Palette.DarkWood);
            Visuals.Prim(Cube, root, new Vector3(0f, 1.45f, -11f), new Vector3(0.5f, 0.6f, 0.3f), new Color(0.3f, 0.45f, 0.65f));
            Visuals.Prim(Sphere, root, new Vector3(0f, 2.05f, -11f), Vector3.one * 0.35f, Palette.Straw);
            Visuals.Prim(Cyl, root, new Vector3(0f, 2.22f, -11f), new Vector3(0.6f, 0.02f, 0.6f), Palette.Straw);
        }

        static void BuildForest(Transform world)
        {
            var root = new GameObject("Forest").transform;
            root.SetParent(world);
            var saved = Random.state;
            Random.InitState(1234);
            Color[] leaves = { Palette.Leaf, new Color(0.22f, 0.44f, 0.17f), new Color(0.15f, 0.32f, 0.16f) };
            for (int i = 0; i < 150; i++)
            {
                float ang = Random.Range(0f, Mathf.PI * 2f), r = Random.Range(GameData.ForestInner, GameData.ForestOuter);
                Vector3 p = new Vector3(Mathf.Sin(ang) * r, 0f, -7f + Mathf.Cos(ang) * r);
                if (Mathf.Abs(p.x) < 5f && p.z > 15f) continue; // keep the road clear
                bool inField = false;
                foreach (var def in GameData.Fields)
                    if (Mathf.Abs(p.x - def.Center.x) < def.Cols * 1.25f + 3f && Mathf.Abs(p.z - def.Center.z) < def.Rows * 1.25f + 3f) inField = true;
                if (inField) continue;
                float s = Random.Range(0.8f, 1.4f);
                Color leaf = leaves[Random.Range(0, leaves.Length)];
                Visuals.Prim(Cyl, root, p + Vector3.up * 1.4f * s, new Vector3(0.35f, 1.4f, 0.35f) * s, Palette.DarkWood);
                if (Random.value < 0.5f)
                {
                    for (int k = 0; k < 3; k++)
                        Visuals.Skin(Visuals.Prim(Sphere, root, p + Vector3.up * (2.6f + k * 1.2f) * s, new Vector3(3.0f - k * 0.8f, 1.4f, 3.0f - k * 0.8f) * s, leaf), leaf, Surface.Leaves, new Vector2(2f, 2f));
                }
                else
                {
                    Visuals.Skin(Visuals.Prim(Sphere, root, p + Vector3.up * 3.6f * s, new Vector3(3.4f, 3.0f, 3.4f) * s, leaf), leaf, Surface.Leaves, new Vector2(2f, 2f));
                }
            }
            Random.state = saved;
        }

        public static Volume PostVolume { get; private set; }
        public static UniversalAdditionalCameraData CameraData { get; private set; }

        /// <summary>Smooth edges (SMAA) and a filmic colour grade so the farm looks less flat.</summary>
        static void SetupPostProcessing(Camera cam)
        {
            var data = cam.GetComponent<UniversalAdditionalCameraData>();
            if (data == null) data = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            CameraData = data;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.35f);
            color.contrast.Override(14f);
            color.saturation.Override(10f);
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.25f);
            bloom.threshold.Override(1.1f);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.2f);
            vignette.smoothness.Override(0.5f);

            var go = new GameObject("Hay Hustle Post FX");
            var vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 10f;
            vol.sharedProfile = profile;
            PostVolume = vol;
        }

        static void SetupLightAndCamera(GameManager gm)
        {
            Sun = RenderSettings.sun;
            if (Sun == null)
            {
                var found = GameObject.Find("Directional Light");
                if (found != null) Sun = found.GetComponent<Light>();
            }
            if (Sun == null)
            {
                Sun = new GameObject("Sun").AddComponent<Light>();
                Sun.type = LightType.Directional;
            }
            Sun.shadows = LightShadows.Soft;
            RenderSettings.sun = Sun;

            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.farClipPlane = 300f;
            cam.nearClipPlane = 0.2f;
            SetupPostProcessing(cam);
            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            gm.CameraRig = follow;
        }
    }
}
