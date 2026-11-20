using UnityEngine;

namespace HayHustle
{
    /// <summary>Placeholder character, animal, predator and product shapes.</summary>
    public static class Models
    {
        const PrimitiveType Cube = PrimitiveType.Cube, Sphere = PrimitiveType.Sphere, Cyl = PrimitiveType.Cylinder;

        static GameObject P(PrimitiveType t, Transform parent, float x, float y, float z, float sx, float sy, float sz, Color c,
            float rx = 0f, float ry = 0f, float rz = 0f)
        {
            return Visuals.Prim(t, parent, new Vector3(x, y, z), new Vector3(sx, sy, sz), c, false, new Vector3(rx, ry, rz));
        }

        // ---------------- Animals ----------------

        public static Transform BuildAnimal(AnimalKind kind, bool rare, Transform parent)
        {
            var model = rare ? Visuals.TryModel("Rare" + kind, parent) : null;
            if (model == null) model = Visuals.TryModel(kind.ToString(), parent);
            if (model != null) return model;

            var root = Visuals.NewRoot(parent);
            switch (kind)
            {
                case AnimalKind.Chicken:
                {
                    Color body = rare ? Palette.Gold : Palette.White;
                    P(Sphere, root, 0, 0.34f, 0, 0.42f, 0.4f, 0.55f, body);
                    P(Sphere, root, 0, 0.6f, 0.2f, 0.24f, 0.26f, 0.24f, body);
                    P(Cube, root, 0, 0.76f, 0.2f, 0.05f, 0.1f, 0.14f, Palette.Red);
                    P(Cube, root, 0, 0.6f, 0.35f, 0.07f, 0.05f, 0.1f, Palette.Orange);
                    P(Cube, root, 0, 0.53f, 0.3f, 0.04f, 0.07f, 0.04f, Palette.Red);
                    P(Cube, root, 0, 0.48f, -0.26f, 0.2f, 0.26f, 0.1f, body, -25f);
                    P(Sphere, root, 0.09f, 0.64f, 0.29f, 0.04f, 0.04f, 0.04f, Palette.Black);
                    P(Sphere, root, -0.09f, 0.64f, 0.29f, 0.04f, 0.04f, 0.04f, Palette.Black);
                    Leg(root, 0.08f, 0.16f, 0, 0.04f, Palette.Orange);
                    Leg(root, -0.08f, 0.16f, 0, 0.04f, Palette.Orange);
                    break;
                }
                case AnimalKind.Sheep:
                {
                    Color wool = rare ? new Color(0.82f, 0.9f, 1f) : new Color(0.93f, 0.92f, 0.88f);
                    Color face = new Color(0.16f, 0.14f, 0.13f);
                    P(Sphere, root, 0, 0.8f, 0, 0.85f, 0.7f, 1.2f, wool);
                    P(Sphere, root, 0, 1.05f, 0.45f, 0.5f, 0.4f, 0.4f, wool);
                    P(Cube, root, 0, 0.88f, 0.7f, 0.3f, 0.34f, 0.42f, face);
                    P(Cube, root, 0.2f, 0.98f, 0.62f, 0.18f, 0.06f, 0.1f, face);
                    P(Cube, root, -0.2f, 0.98f, 0.62f, 0.18f, 0.06f, 0.1f, face);
                    P(Sphere, root, 0.1f, 0.97f, 0.9f, 0.05f, 0.05f, 0.03f, Palette.White);
                    P(Sphere, root, -0.1f, 0.97f, 0.9f, 0.05f, 0.05f, 0.03f, Palette.White);
                    foreach (var lx in new[] { -0.24f, 0.24f })
                    foreach (var lz in new[] { -0.35f, 0.35f })
                        Leg(root, lx, 0.5f, lz, 0.1f, face);
                    break;
                }
                case AnimalKind.Cow:
                {
                    Color hide = rare ? new Color(0.5f, 0.3f, 0.17f) : Palette.White;
                    Color spot = rare ? new Color(0.25f, 0.14f, 0.08f) : Palette.Black;
                    P(Cube, root, 0, 1.05f, 0, 0.9f, 0.85f, 1.8f, hide);
                    P(Cube, root, 0.455f, 1.1f, 0.25f, 0.02f, 0.45f, 0.55f, spot);
                    P(Cube, root, -0.455f, 1.0f, -0.4f, 0.02f, 0.5f, 0.6f, spot);
                    P(Cube, root, 0.2f, 1.48f, -0.5f, 0.4f, 0.02f, 0.5f, spot);
                    P(Cube, root, 0, 1.3f, 1.05f, 0.45f, 0.5f, 0.55f, hide);
                    P(Cube, root, 0, 1.15f, 1.36f, 0.42f, 0.26f, 0.1f, Palette.Pink);
                    P(Sphere, root, 0.12f, 1.42f, 1.33f, 0.07f, 0.07f, 0.04f, Palette.Black);
                    P(Sphere, root, -0.12f, 1.42f, 1.33f, 0.07f, 0.07f, 0.04f, Palette.Black);
                    P(Cyl, root, 0.2f, 1.6f, 1.0f, 0.05f, 0.1f, 0.05f, new Color(0.92f, 0.88f, 0.75f));
                    P(Cyl, root, -0.2f, 1.6f, 1.0f, 0.05f, 0.1f, 0.05f, new Color(0.92f, 0.88f, 0.75f));
                    P(Cube, root, 0.3f, 1.42f, 0.95f, 0.2f, 0.08f, 0.12f, hide);
                    P(Cube, root, -0.3f, 1.42f, 0.95f, 0.2f, 0.08f, 0.12f, hide);
                    P(Sphere, root, 0, 0.58f, -0.4f, 0.28f, 0.2f, 0.28f, Palette.Pink);
                    P(Cube, root, 0, 0.9f, -0.95f, 0.05f, 0.6f, 0.05f, hide, 15f);
                    foreach (var lx in new[] { -0.33f, 0.33f })
                    foreach (var lz in new[] { -0.65f, 0.65f })
                        Leg(root, lx, 0.64f, lz, 0.15f, hide);
                    break;
                }
                case AnimalKind.Goat:
                {
                    Color coat = rare ? new Color(0.62f, 0.62f, 0.65f) : new Color(0.88f, 0.84f, 0.76f);
                    Color horn = new Color(0.35f, 0.3f, 0.25f);
                    P(Cube, root, 0, 0.72f, 0, 0.45f, 0.45f, 0.95f, coat);
                    P(Cube, root, 0, 0.98f, 0.55f, 0.24f, 0.3f, 0.34f, coat);
                    P(Cube, root, 0.08f, 1.2f, 0.48f, 0.05f, 0.25f, 0.05f, horn, -30f);
                    P(Cube, root, -0.08f, 1.2f, 0.48f, 0.05f, 0.25f, 0.05f, horn, -30f);
                    P(Cube, root, 0, 0.8f, 0.72f, 0.06f, 0.14f, 0.04f, horn);
                    P(Sphere, root, 0.08f, 1.03f, 0.72f, 0.04f, 0.04f, 0.03f, Palette.Black);
                    P(Sphere, root, -0.08f, 1.03f, 0.72f, 0.04f, 0.04f, 0.03f, Palette.Black);
                    P(Cube, root, 0, 0.9f, -0.5f, 0.06f, 0.14f, 0.06f, coat, 30f);
                    foreach (var lx in new[] { -0.15f, 0.15f })
                    foreach (var lz in new[] { -0.35f, 0.35f })
                        Leg(root, lx, 0.5f, lz, 0.08f, coat);
                    break;
                }
                case AnimalKind.Pig:
                {
                    Color skin = rare ? new Color(0.62f, 0.42f, 0.3f) : new Color(0.96f, 0.72f, 0.72f);
                    Color snout = skin * 0.85f;
                    snout.a = 1f;
                    P(Sphere, root, 0, 0.55f, 0, 0.7f, 0.6f, 1.1f, skin);
                    P(Sphere, root, 0, 0.62f, 0.48f, 0.45f, 0.42f, 0.4f, skin);
                    P(Cyl, root, 0, 0.58f, 0.7f, 0.22f, 0.04f, 0.22f, snout, 90f);
                    P(Cube, root, 0.15f, 0.84f, 0.42f, 0.13f, 0.13f, 0.04f, skin, -20f);
                    P(Cube, root, -0.15f, 0.84f, 0.42f, 0.13f, 0.13f, 0.04f, skin, -20f);
                    P(Sphere, root, 0.1f, 0.7f, 0.66f, 0.05f, 0.05f, 0.03f, Palette.Black);
                    P(Sphere, root, -0.1f, 0.7f, 0.66f, 0.05f, 0.05f, 0.03f, Palette.Black);
                    P(Sphere, root, 0, 0.62f, -0.56f, 0.08f, 0.08f, 0.08f, skin);
                    foreach (var lx in new[] { -0.2f, 0.2f })
                    foreach (var lz in new[] { -0.3f, 0.3f })
                        Leg(root, lx, 0.3f, lz, 0.1f, skin);
                    break;
                }
                case AnimalKind.Turkey:
                {
                    Color feathers = rare ? new Color(0.94f, 0.94f, 0.92f) : new Color(0.38f, 0.25f, 0.14f);
                    Color fan = rare ? new Color(0.85f, 0.85f, 0.82f) : new Color(0.55f, 0.36f, 0.18f);
                    P(Sphere, root, 0, 0.55f, 0, 0.55f, 0.55f, 0.65f, feathers);
                    P(Cube, root, 0, 0.88f, -0.35f, 0.9f, 0.7f, 0.06f, fan, -15f);
                    P(Cube, root, 0, 1.0f, -0.37f, 0.92f, 0.08f, 0.07f, Palette.White, -15f);
                    P(Cyl, root, 0, 0.85f, 0.25f, 0.08f, 0.15f, 0.08f, feathers);
                    P(Sphere, root, 0, 1.03f, 0.3f, 0.15f, 0.15f, 0.15f, new Color(0.6f, 0.72f, 0.88f));
                    P(Cube, root, 0, 0.93f, 0.37f, 0.04f, 0.1f, 0.04f, Palette.Red);
                    P(Cube, root, 0, 1.03f, 0.39f, 0.05f, 0.04f, 0.07f, Palette.Gold);
                    Leg(root, 0.1f, 0.3f, 0, 0.05f, Palette.Orange);
                    Leg(root, -0.1f, 0.3f, 0, 0.05f, Palette.Orange);
                    break;
                }
                case AnimalKind.Llama:
                {
                    Color wool = rare ? new Color(0.98f, 0.98f, 1f) : new Color(0.9f, 0.84f, 0.7f);
                    P(Cube, root, 0, 1.15f, 0, 0.6f, 0.6f, 1.2f, wool);
                    P(Cube, root, 0, 1.47f, 0, 0.62f, 0.05f, 0.7f, new Color(0.8f, 0.15f, 0.2f));
                    P(Cube, root, 0, 1.65f, 0.5f, 0.25f, 0.8f, 0.25f, wool);
                    P(Cube, root, 0, 2.05f, 0.62f, 0.25f, 0.25f, 0.4f, wool);
                    P(Cube, root, 0.08f, 2.25f, 0.55f, 0.05f, 0.16f, 0.05f, wool);
                    P(Cube, root, -0.08f, 2.25f, 0.55f, 0.05f, 0.16f, 0.05f, wool);
                    P(Sphere, root, 0.1f, 2.1f, 0.78f, 0.05f, 0.05f, 0.03f, Palette.Black);
                    P(Sphere, root, -0.1f, 2.1f, 0.78f, 0.05f, 0.05f, 0.03f, Palette.Black);
                    foreach (var lx in new[] { -0.2f, 0.2f })
                    foreach (var lz in new[] { -0.45f, 0.45f })
                        Leg(root, lx, 0.9f, lz, 0.12f, wool);
                    break;
                }
            }
            return root;
        }

        // ---------------- Helpers (cat, dog) ----------------

        public static Transform BuildHelper(HelperKind kind, Transform parent)
        {
            var model = Visuals.TryModel(kind.ToString(), parent);
            if (model != null) return model;
            var root = Visuals.NewRoot(parent);
            if (kind == HelperKind.Dog)
            {
                Canine(root, 1.15f, new Color(0.55f, 0.38f, 0.2f), new Color(0.85f, 0.72f, 0.5f), Palette.Black);
                P(Cube, root, 0, 0.62f * 1.15f, 0.36f * 1.15f, 0.34f, 0.07f, 0.14f, Palette.Red);
            }
            else
            {
                Canine(root, 0.6f, new Color(0.92f, 0.55f, 0.2f), Palette.White, new Color(0.3f, 0.8f, 0.3f));
            }
            return root;
        }

        // ---------------- Predators ----------------

        public static Transform BuildPredator(PredatorKind kind, Transform parent)
        {
            var model = Visuals.TryModel(kind.ToString(), parent);
            if (model != null) return model;

            var root = Visuals.NewRoot(parent);
            switch (kind)
            {
                case PredatorKind.Fox:
                    Canine(root, 1f, new Color(0.86f, 0.42f, 0.12f), new Color(0.96f, 0.93f, 0.88f), Palette.Black);
                    break;
                case PredatorKind.Wolf:
                    Canine(root, 1.45f, new Color(0.42f, 0.42f, 0.44f), new Color(0.7f, 0.7f, 0.7f), new Color(1f, 0.85f, 0.2f));
                    break;
                case PredatorKind.Hawk:
                {
                    Color brown = new Color(0.4f, 0.27f, 0.15f);
                    P(Sphere, root, 0, 0, 0, 0.3f, 0.26f, 0.65f, brown);
                    P(Sphere, root, 0, 0.1f, 0.36f, 0.2f, 0.2f, 0.2f, new Color(0.9f, 0.88f, 0.8f));
                    P(Cube, root, 0, 0.07f, 0.48f, 0.06f, 0.06f, 0.1f, Palette.Gold);
                    P(Cube, root, 0, 0, -0.42f, 0.25f, 0.03f, 0.25f, brown);
                    foreach (var side in new[] { -1f, 1f })
                    {
                        var pivot = new GameObject(side < 0 ? "WingL" : "WingR").transform;
                        pivot.SetParent(root, false);
                        pivot.localPosition = new Vector3(0.12f * side, 0.03f, 0);
                        P(Cube, pivot, 0.45f * side, 0, 0, 0.9f, 0.04f, 0.38f, brown);
                    }
                    break;
                }
                case PredatorKind.Snake:
                {
                    Color green = new Color(0.3f, 0.5f, 0.15f);
                    for (int i = 0; i < 5; i++)
                        P(Cube, root, (i % 2 == 0 ? 0.06f : -0.06f), 0.07f, 0.25f - i * 0.3f, 0.14f - i * 0.015f, 0.11f, 0.34f, green);
                    P(Cube, root, 0, 0.09f, 0.48f, 0.19f, 0.12f, 0.24f, green);
                    P(Sphere, root, 0.06f, 0.15f, 0.55f, 0.04f, 0.04f, 0.04f, Palette.Gold);
                    P(Sphere, root, -0.06f, 0.15f, 0.55f, 0.04f, 0.04f, 0.04f, Palette.Gold);
                    P(Cube, root, 0, 0.07f, 0.65f, 0.02f, 0.01f, 0.1f, Palette.Red);
                    break;
                }
                case PredatorKind.Rat:
                {
                    Color grey = new Color(0.45f, 0.42f, 0.4f);
                    P(Sphere, root, 0, 0.14f, 0, 0.22f, 0.18f, 0.36f, grey);
                    P(Sphere, root, 0, 0.16f, 0.2f, 0.14f, 0.13f, 0.16f, grey);
                    P(Sphere, root, 0.06f, 0.24f, 0.17f, 0.07f, 0.07f, 0.02f, Palette.Pink);
                    P(Sphere, root, -0.06f, 0.24f, 0.17f, 0.07f, 0.07f, 0.02f, Palette.Pink);
                    P(Sphere, root, 0, 0.15f, 0.29f, 0.03f, 0.03f, 0.03f, Palette.Black);
                    P(Cyl, root, 0, 0.08f, -0.38f, 0.02f, 0.2f, 0.02f, Palette.Pink, 80f);
                    break;
                }
                case PredatorKind.Bear:
                {
                    Color fur = new Color(0.33f, 0.21f, 0.12f);
                    Color muzzle = new Color(0.6f, 0.45f, 0.3f);
                    P(Cube, root, 0, 1.0f, 0, 1.1f, 1.0f, 1.8f, fur);
                    P(Cube, root, 0, 1.25f, -0.3f, 1.15f, 0.5f, 0.8f, fur);
                    P(Cube, root, 0, 1.35f, 1.05f, 0.65f, 0.6f, 0.6f, fur);
                    P(Cube, root, 0, 1.2f, 1.42f, 0.3f, 0.26f, 0.25f, muzzle);
                    P(Cube, root, 0, 1.3f, 1.56f, 0.1f, 0.08f, 0.04f, Palette.Black);
                    P(Sphere, root, 0.25f, 1.72f, 1.0f, 0.2f, 0.2f, 0.12f, fur);
                    P(Sphere, root, -0.25f, 1.72f, 1.0f, 0.2f, 0.2f, 0.12f, fur);
                    P(Sphere, root, 0.15f, 1.48f, 1.36f, 0.07f, 0.07f, 0.04f, Palette.Black);
                    P(Sphere, root, -0.15f, 1.48f, 1.36f, 0.07f, 0.07f, 0.04f, Palette.Black);
                    foreach (var lx in new[] { -0.38f, 0.38f })
                    foreach (var lz in new[] { -0.6f, 0.6f })
                        Leg(root, lx, 0.7f, lz, 0.3f, fur);
                    break;
                }
            }
            return root;
        }

        static void Canine(Transform root, float s, Color fur, Color light, Color eye)
        {
            P(Cube, root, 0, 0.45f * s, 0, 0.32f * s, 0.3f * s, 0.85f * s, fur);
            P(Cube, root, 0, 0.45f * s, 0.4f * s, 0.28f * s, 0.25f * s, 0.1f * s, light);
            P(Cube, root, 0, 0.6f * s, 0.52f * s, 0.3f * s, 0.27f * s, 0.3f * s, fur);
            P(Cube, root, 0, 0.53f * s, 0.72f * s, 0.14f * s, 0.12f * s, 0.2f * s, light);
            P(Cube, root, 0, 0.57f * s, 0.83f * s, 0.05f * s, 0.05f * s, 0.04f * s, Palette.Black);
            P(Cube, root, 0.09f * s, 0.79f * s, 0.5f * s, 0.08f * s, 0.13f * s, 0.05f * s, fur);
            P(Cube, root, -0.09f * s, 0.79f * s, 0.5f * s, 0.08f * s, 0.13f * s, 0.05f * s, fur);
            P(Sphere, root, 0.08f * s, 0.66f * s, 0.67f * s, 0.05f * s, 0.05f * s, 0.05f * s, eye);
            P(Sphere, root, -0.08f * s, 0.66f * s, 0.67f * s, 0.05f * s, 0.05f * s, 0.05f * s, eye);
            P(Sphere, root, 0, 0.5f * s, -0.62f * s, 0.2f * s, 0.2f * s, 0.55f * s, fur, -20f);
            P(Sphere, root, 0, 0.56f * s, -0.88f * s, 0.13f * s, 0.13f * s, 0.13f * s, light);
            Color leg = fur * 0.6f;
            leg.a = 1f;
            foreach (var lx in new[] { -0.11f, 0.11f })
            foreach (var lz in new[] { -0.3f, 0.3f })
                Leg(root, lx * s, 0.3f * s, lz * s, 0.07f * s, leg);
        }

        // ---------------- Farmer (player) ----------------

        /// <summary>How a person looks (colours and style).</summary>
        public class Look
        {
            public Color Shirt, Torso, Pants, Boots, Hat, HatBand;
            public bool Plaid, Overalls, Braid, Cap, NoHat;
            public float Brim = 0.62f;
            public Color Skin = Palette.Skin, Eyes = Palette.Black, Hair = Palette.Hair;
        }

        public static Look FarmerLook(CharacterChoice c)
        {
            bool boy = c == CharacterChoice.Boy;
            var overalls = new Color(0.25f, 0.4f, 0.72f);
            return new Look
            {
                Shirt = boy ? new Color(0.7f, 0.15f, 0.13f) : new Color(0.95f, 0.95f, 0.92f),
                Torso = boy ? new Color(0.7f, 0.15f, 0.13f) : overalls,
                Pants = boy ? Palette.Jeans : overalls,
                Boots = boy ? new Color(0.35f, 0.22f, 0.12f) : new Color(0.15f, 0.35f, 0.2f),
                Hat = boy ? Palette.Straw : new Color(0.97f, 0.88f, 0.62f),
                HatBand = boy ? Palette.DarkWood : new Color(0.9f, 0.4f, 0.5f),
                Plaid = boy, Overalls = !boy, Braid = !boy, Brim = boy ? 0.62f : 0.82f,
            };
        }

        public static Transform BuildFarmer(CharacterChoice c, Transform parent)
        {
            bool boy = c == CharacterChoice.Boy;
            var model = Visuals.TryModel(boy ? "FarmerBoy" : "FarmerGirl", parent);
            if (model != null)
            {
                AddNet(model, new Vector3(0.36f, 1.0f, 0.05f));
                return model;
            }
            var root = BuildPerson(FarmerLook(c), parent);
            AddNet(FindArm(root), new Vector3(0f, -0.6f, 0.05f));
            return root;
        }

        public static Transform BuildWorker(WorkerKind kind, Transform parent)
        {
            var model = Visuals.TryModel("Worker" + kind, parent);
            if (model != null) return model;
            var d = GameData.Workers[kind];
            var look = new Look
            {
                Shirt = d.Shirt, Torso = d.Shirt,
                Pants = kind == WorkerKind.FactoryHand ? new Color(0.3f, 0.3f, 0.32f) : new Color(0.35f, 0.3f, 0.22f),
                Boots = new Color(0.25f, 0.18f, 0.1f),
                Hat = d.Cap, HatBand = d.Cap * 0.7f + Color.black * 0.3f,
                Cap = kind != WorkerKind.WaterCarrier,
                Overalls = kind == WorkerKind.FactoryHand,
            };
            if (kind == WorkerKind.FactoryHand) look.Torso = new Color(0.55f, 0.35f, 0.18f); // apron
            var root = BuildPerson(look, parent);
            var hand = Pivot(FindArm(root), "ToolPivot", 0f, -0.62f, 0.08f);
            switch (kind)
            {
                case WorkerKind.WaterCarrier:
                    P(Cyl, hand, 0, -0.18f, 0, 0.28f, 0.16f, 0.28f, new Color(0.6f, 0.62f, 0.66f));
                    P(Cyl, hand, 0, -0.03f, 0, 0.25f, 0.01f, 0.25f, new Color(0.35f, 0.6f, 0.95f));
                    break;
                case WorkerKind.Collector:
                    P(Cube, hand, 0, -0.18f, 0.05f, 0.34f, 0.22f, 0.26f, Palette.Straw);
                    break;
                case WorkerKind.FactoryHand:
                    P(Cube, hand, 0, -0.05f, 0.12f, 0.06f, 0.06f, 0.35f, new Color(0.5f, 0.5f, 0.55f));
                    break;
                case WorkerKind.Guard:
                    break; // the weapon is added by Laborer.RefreshTool
                case WorkerKind.Driver:
                    P(Cube, hand, 0, -0.08f, 0.05f, 0.2f, 0.14f, 0.04f, new Color(0.9f, 0.85f, 0.6f)); // delivery list
                    break;
            }
            return root;
        }

        static Transform FindArm(Transform root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>()) if (t.name == "ArmR") return t;
            return root;
        }

        /// <summary>A jointed person (legs, waist, arms) that FarmerRig can animate.</summary>
        public static Transform BuildPerson(Look look, Transform parent)
        {
            var root = Visuals.NewRoot(parent);

            foreach (var side in new[] { -1f, 1f })
            {
                var leg = Pivot(root, side < 0 ? "LegL" : "LegR", 0.12f * side, 0.95f, 0f);
                P(Cube, leg, 0, -0.37f, 0, 0.19f, 0.75f, 0.2f, look.Pants);
                P(Cube, leg, 0, -0.83f, 0.03f, 0.2f, 0.24f, 0.3f, look.Boots);
            }

            var upper = Pivot(root, "Upper", 0f, 0.95f, 0f);
            P(Cube, upper, 0, 0.3f, 0, 0.52f, 0.62f, 0.28f, look.Torso);
            if (look.Plaid)
            {
                Color plaid = look.Shirt * 0.5f + Color.black * 0.5f;
                P(Cube, upper, 0, 0.43f, 0.145f, 0.53f, 0.05f, 0.01f, plaid);
                P(Cube, upper, 0, 0.2f, 0.145f, 0.53f, 0.05f, 0.01f, plaid);
                P(Cube, upper, 0.1f, 0.3f, 0.145f, 0.05f, 0.62f, 0.01f, plaid);
                P(Cube, upper, -0.1f, 0.3f, 0.145f, 0.05f, 0.62f, 0.01f, plaid);
                P(Cube, upper, 0, 0.0f, 0, 0.54f, 0.06f, 0.3f, Palette.DarkWood);
            }
            if (look.Overalls)
            {
                P(Cube, upper, 0, 0.55f, 0, 0.5f, 0.14f, 0.27f, look.Shirt);
                P(Cube, upper, 0.15f, 0.55f, 0.142f, 0.07f, 0.16f, 0.01f, look.Torso);
                P(Cube, upper, -0.15f, 0.55f, 0.142f, 0.07f, 0.16f, 0.01f, look.Torso);
                P(Cube, upper, 0, 0.25f, 0.145f, 0.18f, 0.14f, 0.01f, look.Torso * 0.85f + Color.black * 0.15f);
            }
            P(Cube, upper, 0, 0.65f, 0, 0.14f, 0.1f, 0.14f, look.Skin);
            P(Sphere, upper, 0, 0.85f, 0, 0.36f, 0.4f, 0.36f, look.Skin);
            P(Sphere, upper, 0, 0.91f, -0.04f, 0.38f, 0.36f, 0.36f, look.Hair);
            P(Sphere, upper, 0.07f, 0.87f, 0.165f, 0.05f, 0.05f, 0.03f, look.Eyes);
            P(Sphere, upper, -0.07f, 0.87f, 0.165f, 0.05f, 0.05f, 0.03f, look.Eyes);
            P(Cube, upper, 0, 0.76f, 0.172f, 0.08f, 0.015f, 0.01f, new Color(0.6f, 0.3f, 0.3f));
            if (look.Braid)
            {
                P(Cube, upper, 0, 0.6f, -0.2f, 0.1f, 0.42f, 0.1f, look.Hair);
                P(Sphere, upper, 0, 0.38f, -0.2f, 0.08f, 0.08f, 0.08f, new Color(0.9f, 0.4f, 0.5f));
            }
            if (look.NoHat) { }
            else if (look.Cap)
            {
                P(Sphere, upper, 0, 1.0f, -0.01f, 0.4f, 0.22f, 0.4f, look.Hat);
                P(Cube, upper, 0, 0.98f, 0.2f, 0.3f, 0.03f, 0.2f, look.HatBand);
            }
            else
            {
                P(Cyl, upper, 0, 1.05f, 0, look.Brim, 0.015f, look.Brim, look.Hat);
                P(Cyl, upper, 0, 1.13f, 0, 0.34f, 0.08f, 0.34f, look.Hat);
                P(Cyl, upper, 0, 1.08f, 0, 0.35f, 0.02f, 0.35f, look.HatBand);
            }

            foreach (var side in new[] { -1f, 1f })
            {
                var arm = Pivot(upper, side < 0 ? "ArmL" : "ArmR", 0.34f * side, 0.55f, 0f);
                P(Cube, arm, 0, -0.25f, 0, 0.15f, 0.58f, 0.17f, look.Shirt);
                P(Cube, arm, 0, -0.59f, 0, 0.13f, 0.12f, 0.14f, look.Skin);
            }
            return root;
        }

        /// <summary>A survivor: every one looks a bit different (seeded by their id).</summary>
        public static Transform BuildSurvivor(int seed, Transform parent)
        {
            var rng = new System.Random(seed);
            Color Pick(params Color[] c) => c[rng.Next(c.Length)];
            var look = new Look
            {
                Shirt = Pick(new Color(0.55f, 0.45f, 0.3f), new Color(0.3f, 0.4f, 0.3f), new Color(0.6f, 0.2f, 0.2f), new Color(0.35f, 0.35f, 0.55f), new Color(0.75f, 0.7f, 0.6f)),
                Pants = Pick(new Color(0.25f, 0.25f, 0.28f), new Color(0.35f, 0.3f, 0.2f), Palette.Jeans),
                Boots = Pick(new Color(0.25f, 0.18f, 0.1f), new Color(0.15f, 0.15f, 0.15f)),
                Hat = Pick(new Color(0.3f, 0.35f, 0.25f), new Color(0.6f, 0.15f, 0.12f), new Color(0.2f, 0.2f, 0.22f)),
                Skin = Pick(Palette.Skin, new Color(0.8f, 0.6f, 0.45f), new Color(0.6f, 0.42f, 0.3f), new Color(0.95f, 0.82f, 0.7f)),
                Hair = Pick(Palette.Hair, new Color(0.1f, 0.08f, 0.06f), new Color(0.55f, 0.4f, 0.2f), new Color(0.6f, 0.6f, 0.6f)),
                Braid = rng.Next(3) == 0,
            };
            look.Torso = look.Shirt;
            look.HatBand = look.Hat * 0.7f + Color.black * 0.3f;
            int hat = rng.Next(3);
            look.Cap = hat == 1;
            look.NoHat = hat == 2;
            var root = BuildPerson(look, parent);
            // A backpack - they have travelled far.
            var upper = FindChild(root, "Upper");
            if (upper) P(Cube, upper, 0, 0.32f, -0.22f, 0.4f, 0.45f, 0.18f, new Color(0.35f, 0.3f, 0.2f));
            return root;
        }

        /// <summary>A zombie: grey-green skin, torn dark clothes, glowing red eyes.</summary>
        public static Transform BuildZombie(Transform parent)
        {
            var model = Visuals.TryModel("Zombie", parent);
            if (model != null) return model;
            var look = new Look
            {
                Shirt = new Color(0.32f, 0.3f, 0.26f), Torso = new Color(0.32f, 0.3f, 0.26f),
                Pants = new Color(0.2f, 0.22f, 0.25f), Boots = new Color(0.15f, 0.12f, 0.1f),
                Skin = new Color(0.48f, 0.6f, 0.42f), Eyes = new Color(1f, 0.15f, 0.1f), Hair = new Color(0.2f, 0.2f, 0.18f),
                NoHat = true,
            };
            var root = BuildPerson(look, parent);
            var upper = FindChild(root, "Upper");
            if (upper)
            {
                Color torn = new Color(0.45f, 0.12f, 0.1f);
                P(Cube, upper, 0.12f, 0.2f, 0.146f, 0.14f, 0.18f, 0.01f, torn);
                P(Cube, upper, -0.1f, 0.45f, 0.146f, 0.1f, 0.08f, 0.01f, torn);
            }
            return root;
        }

        static Transform FindChild(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>()) if (t.name == name) return t;
            return null;
        }

        /// <summary>Crop plant for a growth stage: 1 = sprout, 2 = growing, 3 = ripe.</summary>
        public static Transform BuildCrop(CropKind kind, int stage, Transform parent)
        {
            var root = Visuals.NewRoot(parent);
            Color leaf = new Color(0.3f, 0.6f, 0.2f);
            var d = GameData.Crops[kind];
            float[,] spots = { { -0.6f, -0.6f }, { 0.6f, -0.6f }, { -0.6f, 0.6f }, { 0.6f, 0.6f }, { 0f, 0f } };
            for (int i = 0; i < spots.GetLength(0); i++)
            {
                float x = spots[i, 0], z = spots[i, 1];
                if (stage == 1) { P(Cube, root, x, 0.1f, z, 0.08f, 0.2f, 0.08f, leaf); continue; }
                switch (kind)
                {
                    case CropKind.Wheat:
                    {
                        float h = stage == 2 ? 0.5f : 0.85f;
                        Color c = stage == 3 ? d.Color : new Color(0.5f, 0.7f, 0.3f);
                        P(Cube, root, x, h / 2f, z, 0.06f, h, 0.06f, c);
                        P(Cube, root, x + 0.12f, h / 2f, z + 0.08f, 0.06f, h * 0.9f, 0.06f, c, 0f, 0f, 8f);
                        if (stage == 3) P(Cube, root, x, h + 0.08f, z, 0.1f, 0.2f, 0.1f, d.Color * 0.9f + Color.black * 0.1f);
                        break;
                    }
                    case CropKind.Carrot:
                        P(Cube, root, x, 0.18f, z, 0.25f, 0.3f, 0.06f, leaf, 0f, 45f, 0f);
                        P(Cube, root, x, 0.18f, z, 0.25f, 0.3f, 0.06f, leaf, 0f, -45f, 0f);
                        if (stage == 3) P(Cube, root, x, 0.04f, z, 0.16f, 0.1f, 0.16f, d.Color);
                        break;
                    case CropKind.Corn:
                    {
                        float h = stage == 2 ? 0.8f : 1.5f;
                        P(Cyl, root, x, h / 2f, z, 0.07f, h / 2f, 0.07f, leaf);
                        P(Cube, root, x, h * 0.6f, z, 0.45f, 0.04f, 0.1f, leaf, 0f, 30f, -20f);
                        if (stage == 3) P(Cube, root, x + 0.08f, h * 0.65f, z, 0.1f, 0.3f, 0.1f, d.Color);
                        break;
                    }
                    case CropKind.Pumpkin:
                        P(Cube, root, x, 0.06f, z, 0.5f, 0.06f, 0.5f, leaf, 0f, 30f, 0f);
                        if (stage == 3) P(Sphere, root, x, 0.2f, z, 0.45f, 0.36f, 0.45f, d.Color);
                        else P(Sphere, root, x, 0.1f, z, 0.18f, 0.16f, 0.18f, new Color(0.5f, 0.65f, 0.25f));
                        break;
                }
            }
            return root;
        }

        // ---------------- Tools & weapons (big enough to see when zoomed in) ----------------

        public static void BuildAxe(Transform pivot)
        {
            P(Cyl, pivot, 0, 0, 0.45f, 0.045f, 0.5f, 0.045f, Palette.Wood, 90f);
            P(Cube, pivot, 0, 0.12f, 0.9f, 0.05f, 0.3f, 0.2f, new Color(0.72f, 0.74f, 0.78f));
        }

        public static void BuildWeapon(int level, Transform pivot)
        {
            Color steel = new Color(0.78f, 0.8f, 0.85f), dark = new Color(0.25f, 0.25f, 0.28f);
            switch (level)
            {
                case 0: // Pitchfork
                    P(Cyl, pivot, 0, 0, 0.7f, 0.045f, 0.8f, 0.045f, Palette.Wood, 90f);
                    P(Cube, pivot, 0, 0, 1.5f, 0.42f, 0.05f, 0.06f, steel);
                    foreach (var tx in new[] { -0.18f, 0f, 0.18f })
                        P(Cube, pivot, tx, 0, 1.75f, 0.05f, 0.05f, 0.45f, steel);
                    break;
                case 1: // Machete
                    P(Cube, pivot, 0, 0, 0.1f, 0.07f, 0.09f, 0.25f, dark);
                    P(Cube, pivot, 0, 0.04f, 0.65f, 0.03f, 0.2f, 0.9f, steel);
                    break;
                case 2: // Spear
                    P(Cyl, pivot, 0, 0, 0.9f, 0.04f, 1.1f, 0.04f, Palette.Wood, 90f);
                    P(Cube, pivot, 0, 0, 2.1f, 0.14f, 0.04f, 0.4f, steel, 0f, 0f, 45f);
                    P(Cube, pivot, 0, 0, 1.85f, 0.12f, 0.12f, 0.06f, new Color(0.7f, 0.15f, 0.12f));
                    break;
                case 3: // Battle Axe
                    P(Cyl, pivot, 0, 0, 0.7f, 0.055f, 0.75f, 0.055f, Palette.DarkWood, 90f);
                    P(Cube, pivot, 0, 0.22f, 1.3f, 0.06f, 0.5f, 0.42f, steel);
                    P(Cube, pivot, 0, -0.15f, 1.3f, 0.06f, 0.22f, 0.22f, steel);
                    break;
                default: // Flaming Sword
                {
                    P(Cube, pivot, 0, 0, 0.12f, 0.08f, 0.08f, 0.28f, dark);
                    P(Cube, pivot, 0, 0, 0.28f, 0.35f, 0.06f, 0.06f, Palette.Gold);
                    var blade = P(Cube, pivot, 0, 0, 0.95f, 0.05f, 0.14f, 1.25f, new Color(1f, 0.55f, 0.15f));
                    var m = new Material(blade.GetComponent<Renderer>().sharedMaterial);
                    m.EnableKeyword("_EMISSION");
                    if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", new Color(1f, 0.45f, 0.1f) * 3f);
                    blade.GetComponent<Renderer>().sharedMaterial = m;
                    break;
                }
            }
        }

        static Transform Pivot(Transform parent, string name, float x, float y, float z)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = new Vector3(x, y, z);
            return t;
        }

        /// <summary>A leg that hangs down from a hip pivot named "Leg" (animated by LimbAnimator).</summary>
        static void Leg(Transform root, float x, float hipY, float z, float width, Color c)
        {
            var pivot = Pivot(root, "Leg", x, hipY, z);
            P(Cyl, pivot, 0, -hipY / 2f, 0, width, hipY / 2f, width, c);
        }

        static void AddNet(Transform parent, Vector3 at)
        {
            var pivot = Pivot(parent, "ToolPivot", at.x, at.y, at.z);
            P(Cyl, pivot, 0, 0, 0.55f, 0.04f, 0.55f, 0.04f, Palette.Wood, 90f);
            P(Cyl, pivot, 0, 0, 1.15f, 0.42f, 0.02f, 0.42f, new Color(0.85f, 0.85f, 0.8f), 90f);
        }

        // ---------------- Products on the ground ----------------

        public static Transform BuildProduct(Product p, Transform parent)
        {
            var model = Visuals.TryModel(p.ToString(), parent);
            if (model != null) return model;
            var root = Visuals.NewRoot(parent);
            switch (p)
            {
                case Product.Egg:
                    P(Sphere, root, 0, 0.12f, 0, 0.16f, 0.21f, 0.16f, new Color(0.96f, 0.92f, 0.8f));
                    break;
                case Product.Wool:
                    P(Sphere, root, 0, 0.2f, 0, 0.38f, 0.3f, 0.38f, new Color(0.97f, 0.97f, 0.95f));
                    P(Sphere, root, 0.12f, 0.28f, 0.05f, 0.22f, 0.2f, 0.22f, new Color(0.97f, 0.97f, 0.95f));
                    break;
                case Product.Milk:
                case Product.GoatMilk:
                    P(Cyl, root, 0, 0.17f, 0, 0.26f, 0.17f, 0.26f, new Color(0.7f, 0.72f, 0.75f));
                    P(Cyl, root, 0, 0.345f, 0, 0.23f, 0.01f, 0.23f, p == Product.Milk ? Palette.White : new Color(0.98f, 0.95f, 0.85f));
                    break;
                case Product.Truffle:
                    P(Sphere, root, 0, 0.1f, 0, 0.2f, 0.16f, 0.18f, new Color(0.25f, 0.17f, 0.11f));
                    break;
                case Product.Feather:
                    P(Cube, root, 0, 0.05f, 0, 0.08f, 0.02f, 0.38f, Palette.White, 0f, 30f, 0f);
                    P(Cube, root, 0, 0.05f, 0, 0.02f, 0.03f, 0.4f, new Color(0.55f, 0.36f, 0.18f), 0f, 30f, 0f);
                    break;
                case Product.LlamaWool:
                    P(Sphere, root, 0, 0.2f, 0, 0.4f, 0.3f, 0.4f, new Color(0.9f, 0.84f, 0.7f));
                    P(Sphere, root, 0.12f, 0.28f, 0.05f, 0.22f, 0.2f, 0.22f, new Color(0.9f, 0.84f, 0.7f));
                    break;
                default:
                    P(Cube, root, 0, 0.15f, 0, 0.3f, 0.3f, 0.3f, Palette.Wood);
                    break;
            }
            root.localScale = Vector3.one * 1.6f; // a bit bigger than life so it is easy to spot
            return root;
        }

        // ---------------- Props ----------------

        public static Transform BuildSign(Transform parent, string label)
        {
            var root = Visuals.NewRoot(parent);
            P(Cube, root, -0.55f, 0.7f, 0, 0.1f, 1.4f, 0.1f, Palette.DarkWood);
            P(Cube, root, 0.55f, 0.7f, 0, 0.1f, 1.4f, 0.1f, Palette.DarkWood);
            P(Cube, root, 0, 1.15f, 0.06f, 1.5f, 0.7f, 0.06f, Palette.Wood);
            P(Cube, root, 0, 1.15f, 0.1f, 1.2f, 0.12f, 0.02f, new Color(0.95f, 0.9f, 0.75f));
            P(Cube, root, 0, 1.6f, 0.06f, 0.5f, 0.25f, 0.05f, Palette.Red);
            return root;
        }

        public static Transform BuildOrderBoard(Transform parent)
        {
            var root = Visuals.NewRoot(parent);
            P(Cube, root, -0.9f, 1.1f, 0, 0.14f, 2.2f, 0.14f, Palette.DarkWood);
            P(Cube, root, 0.9f, 1.1f, 0, 0.14f, 2.2f, 0.14f, Palette.DarkWood);
            P(Cube, root, 0, 1.45f, 0, 2.0f, 1.2f, 0.08f, Palette.Wood);
            P(Cube, root, 0, 2.2f, 0, 2.3f, 0.12f, 0.4f, Palette.Roof);
            Color[] paper = { new Color(0.97f, 0.95f, 0.85f), new Color(0.95f, 0.9f, 0.7f), new Color(0.98f, 0.97f, 0.92f) };
            for (int i = 0; i < 3; i++)
                P(Cube, root, -0.6f + i * 0.6f, 1.45f + (i % 2) * 0.1f, 0.05f, 0.45f, 0.6f, 0.02f, paper[i], 0f, 0f, (i - 1) * 5f);
            return root;
        }

        public static Transform BuildTrap(Transform parent)
        {
            var root = Visuals.NewRoot(parent);
            Color metal = new Color(0.4f, 0.4f, 0.42f);
            P(Cyl, root, 0, 0.03f, 0, 0.8f, 0.03f, 0.8f, new Color(0.3f, 0.3f, 0.32f));
            P(Cube, root, 0, 0.12f, 0.3f, 0.8f, 0.15f, 0.04f, metal, -30f);
            P(Cube, root, 0, 0.12f, -0.3f, 0.8f, 0.15f, 0.04f, metal, 30f);
            P(Cube, root, 0, 0.07f, 0, 0.12f, 0.04f, 0.12f, Palette.Gold);
            return root;
        }
    }
}
