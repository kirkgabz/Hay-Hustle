using UnityEngine;

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

            Visuals.Prim(PrimitiveType.Plane, world, Vector3.zero, new Vector3(16f, 1f, 16f), Palette.Grass, true).name = "Ground";
            Visuals.Prim(Cube, world, new Vector3(0f, 0.005f, 11f), new Vector3(46f, 0.01f, 12f), Palette.Dirt).name = "Farmyard";
            Visuals.Prim(Cube, world, new Vector3(0f, 0.01f, 47f), new Vector3(5f, 0.01f, 60f), Palette.Road).name = "Road";

            BuildFence(world);
            BuildGrass(gm, world);
            BuildWell(world);
            BuildNest(world);
            foreach (var r in GameData.Recipes) BuildProcessing(world, r);
            BuildBarn(world);
            BuildWarehouse(world);
            gm.Truck = BuildTruck(world);
            BuildDecor(world);
            BuildForest(world);
            BuildScatter(world);
            BuildPath(world);
            SetupLightAndCamera(gm);

            // Merge scenery that never moves into big batches - much faster on laptop graphics.
            foreach (var name in new[] { "Fence", "Forest", "Decor", "Scatter", "Path" })
            {
                var t = world.Find(name);
                if (t) StaticBatchingUtility.Combine(t.gameObject);
            }
        }

        // ---------------- Nature details ----------------

        static bool FreeSpot(Vector3 p)
        {
            if (p.magnitude > 47f) return false;
            if (p.x > -14f && p.x < 14f && p.z > -21f && p.z < 7f) return false;   // pasture
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
                Vector3 p = new Vector3(Random.Range(-46f, 46f), 0f, Random.Range(-52f, 40f));
                if (!FreeSpot(p)) continue;
                float r = Random.Range(3f, 7f);
                NoShadow(Visuals.Prim(Cyl, root, p + Vector3.up * 0.01f, new Vector3(r, 0.01f, r * Random.Range(0.6f, 1f)), greens[Random.Range(0, greens.Length)], false, new Vector3(0f, Random.Range(0f, 180f), 0f)));
            }

            int placed = 0, tries = 0;
            while (placed < 170 && tries < 3000)
            {
                tries++;
                Vector3 p = new Vector3(Random.Range(-46f, 46f), 0f, Random.Range(-52f, 40f));
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
                    Visuals.Prim(Sphere, root, p + Vector3.up * 0.12f, new Vector3(Random.Range(0.4f, 1f), Random.Range(0.3f, 0.55f), Random.Range(0.4f, 1f)),
                        rocks[Random.Range(0, rocks.Length)], false, new Vector3(0f, Random.Range(0f, 360f), 0f));
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
            lamp.range = 10f;
            lamp.intensity = 2.2f;
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

        static void BuildFence(Transform world)
        {
            var root = new GameObject("Fence").transform;
            root.SetParent(world);
            float x0 = -11.8f, x1 = 11.8f, z0 = -17.8f, z1 = 3.8f;
            Rail(root, new Vector3(x0, 0, z0), new Vector3(x1, 0, z0));
            Rail(root, new Vector3(x0, 0, z0), new Vector3(x0, 0, z1));
            Rail(root, new Vector3(x1, 0, z0), new Vector3(x1, 0, z1));
            Rail(root, new Vector3(x0, 0, z1), new Vector3(-2f, 0, z1)); // gate gap on the north side
            Rail(root, new Vector3(2f, 0, z1), new Vector3(x1, 0, z1));
        }

        static void Rail(Transform root, Vector3 a, Vector3 b)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            Vector3 mid = (a + b) / 2f;
            float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            foreach (float h in new[] { 0.45f, 0.85f })
                Visuals.Prim(Cube, root, mid + Vector3.up * h, new Vector3(0.08f, 0.12f, len), Palette.Wood, false, new Vector3(0f, yaw, 0f));
            int posts = Mathf.Max(1, Mathf.RoundToInt(len / 2.2f));
            for (int i = 0; i <= posts; i++)
                Visuals.Prim(Cube, root, Vector3.Lerp(a, b, i / (float)posts) + Vector3.up * 0.55f, new Vector3(0.16f, 1.1f, 0.16f), Palette.DarkWood);
        }

        static void BuildGrass(GameManager gm, Transform world)
        {
            var root = new GameObject("Pasture").transform;
            root.SetParent(world);
            for (int ix = 0; ix < 9; ix++)
            for (int iz = 0; iz < 8; iz++)
            {
                var go = new GameObject("Grass Patch");
                go.transform.SetParent(root);
                go.transform.position = new Vector3(-10f + ix * 2.5f, 0f, -16f + iz * 2.5f);
                var g = go.AddComponent<GrassPatch>();
                g.Init();
                gm.Grass.Add(g);
            }
        }

        static void BuildWell(Transform world)
        {
            var root = Root(world, "Well", new Vector3(0f, 0f, 8.5f), 0f);
            Visuals.Prim(Cyl, root, new Vector3(0, 0.45f, 0), new Vector3(1.6f, 0.45f, 1.6f), Palette.Stone, true);
            Visuals.Prim(Cyl, root, new Vector3(0, 0.91f, 0), new Vector3(1.25f, 0.01f, 1.25f), new Color(0.12f, 0.25f, 0.4f));
            Visuals.Prim(Cube, root, new Vector3(-0.75f, 1.4f, 0), new Vector3(0.12f, 1.9f, 0.12f), Palette.DarkWood);
            Visuals.Prim(Cube, root, new Vector3(0.75f, 1.4f, 0), new Vector3(0.12f, 1.9f, 0.12f), Palette.DarkWood);
            Visuals.Prim(Cube, root, new Vector3(0, 2.4f, 0), new Vector3(1.9f, 0.1f, 1.3f), Palette.Roof);
            Visuals.Prim(Cyl, root, new Vector3(0, 1.9f, 0), new Vector3(0.12f, 0.75f, 0.12f), Palette.Wood, false, new Vector3(0, 0, 90f));
            Visuals.Prim(Cyl, root, new Vector3(0, 1.45f, 0), new Vector3(0.3f, 0.18f, 0.3f), Palette.Wood);
            root.gameObject.AddComponent<Well>().Range = 3f;
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
            Visuals.Prim(Cube, root, new Vector3(0, size.y / 2f, 0), size, wall, true);

            float halfW = size.x / 2f, slope = 30f;
            float rise = Mathf.Tan(slope * Mathf.Deg2Rad) * halfW;
            float slab = halfW / Mathf.Cos(slope * Mathf.Deg2Rad) + 0.35f;
            Visuals.Prim(Cube, root, new Vector3(-halfW / 2f, size.y + rise / 2f, 0), new Vector3(slab, 0.16f, size.z + 0.5f), roof, false, new Vector3(0, 0, slope));
            Visuals.Prim(Cube, root, new Vector3(halfW / 2f, size.y + rise / 2f, 0), new Vector3(slab, 0.16f, size.z + 0.5f), roof, false, new Vector3(0, 0, -slope));

            // Triangular gable ends (a squashed diamond whose lower half hides inside the walls).
            var gable = new GameObject("Gable").transform;
            gable.SetParent(root, false);
            gable.localPosition = new Vector3(0, size.y, 0);
            gable.localScale = new Vector3(1f, rise / halfW, 1f);
            float side = size.x / Mathf.Sqrt(2f);
            Visuals.Prim(Cube, gable, Vector3.zero, new Vector3(side, side, size.z - 0.05f), wall, false, new Vector3(0, 0, 45f));

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
            Visuals.Prim(Cube, root, new Vector3(-9f, 1.1f, -9f), new Vector3(0.12f, 2.2f, 0.12f), Palette.DarkWood);
            Visuals.Prim(Cube, root, new Vector3(-9f, 1.6f, -9f), new Vector3(1.6f, 0.12f, 0.12f), Palette.DarkWood);
            Visuals.Prim(Cube, root, new Vector3(-9f, 1.45f, -9f), new Vector3(0.5f, 0.6f, 0.3f), new Color(0.3f, 0.45f, 0.65f));
            Visuals.Prim(Sphere, root, new Vector3(-9f, 2.05f, -9f), Vector3.one * 0.35f, Palette.Straw);
            Visuals.Prim(Cyl, root, new Vector3(-9f, 2.22f, -9f), new Vector3(0.6f, 0.02f, 0.6f), Palette.Straw);
        }

        static void BuildForest(Transform world)
        {
            var root = new GameObject("Forest").transform;
            root.SetParent(world);
            var saved = Random.state;
            Random.InitState(1234);
            Color[] leaves = { Palette.Leaf, new Color(0.22f, 0.44f, 0.17f), new Color(0.15f, 0.32f, 0.16f) };
            for (int i = 0; i < 95; i++)
            {
                float ang = Random.Range(0f, Mathf.PI * 2f), r = Random.Range(48f, 72f);
                Vector3 p = new Vector3(Mathf.Sin(ang) * r, 0f, -7f + Mathf.Cos(ang) * r);
                if (Mathf.Abs(p.x) < 5f && p.z > 15f) continue; // keep the road clear
                float s = Random.Range(0.8f, 1.4f);
                Color leaf = leaves[Random.Range(0, leaves.Length)];
                Visuals.Prim(Cyl, root, p + Vector3.up * 1.4f * s, new Vector3(0.35f, 1.4f, 0.35f) * s, Palette.DarkWood);
                if (Random.value < 0.5f)
                {
                    for (int k = 0; k < 3; k++)
                        Visuals.Prim(Sphere, root, p + Vector3.up * (2.6f + k * 1.2f) * s, new Vector3(3.0f - k * 0.8f, 1.4f, 3.0f - k * 0.8f) * s, leaf);
                }
                else
                {
                    Visuals.Prim(Sphere, root, p + Vector3.up * 3.6f * s, new Vector3(3.4f, 3.0f, 3.4f) * s, leaf);
                }
            }
            Random.state = saved;
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
            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            gm.CameraRig = follow;
        }
    }
}
