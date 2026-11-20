using UnityEngine;

namespace HayHustle
{
    /// <summary>A plot in the pasture. Plant grass with water; animals eat it down.</summary>
    public class GrassPatch : Interactable
    {
        public float Amount { get; private set; }
        public int Field;

        static int seedCounter;
        Transform grass;
        Renderer[] grassRenderers;
        bool wasLush;

        public void Init()
        {
            Range = 1.6f;
            Visuals.Skin(Visuals.Prim(PrimitiveType.Cube, transform, new Vector3(0f, 0.02f, 0f), new Vector3(2.3f, 0.04f, 2.3f), Palette.Soil), Palette.Soil, Surface.Soil, Vector2.one);

            // Grass = a low carpet plus tufts of blades. The whole group grows/shrinks with Amount.
            grass = new GameObject("Grass").transform;
            grass.SetParent(transform, false);
            Visuals.Skin(Visuals.Prim(PrimitiveType.Cube, grass, new Vector3(0f, 0.06f, 0f), new Vector3(2.05f, 0.12f, 2.05f), Palette.GrassLush), Palette.GrassLush, Surface.Grass, Vector2.one);
            var rng = new System.Random(1000 + seedCounter++);
            for (int i = 0; i < 10; i++)
            {
                float x = (float)(rng.NextDouble() * 1.7 - 0.85);
                float z = (float)(rng.NextDouble() * 1.7 - 0.85);
                float h = 0.35f + (float)rng.NextDouble() * 0.3f;
                float yaw = (float)rng.NextDouble() * 360f;
                float tilt = (float)(rng.NextDouble() * 20.0 - 10.0);
                Visuals.Prim(PrimitiveType.Cube, grass, new Vector3(x, h / 2f, z), new Vector3(0.14f, h, 0.14f), Palette.GrassLush, false, new Vector3(tilt, yaw, 0f));
            }
            grassRenderers = grass.GetComponentsInChildren<Renderer>();
            wasLush = true;
            Refresh();
        }

        public override bool Selectable => Amount < 0.35f;
        public override bool CanInteract => GameManager.I.Water > 0;
        public override string Prompt => GameManager.I.Water > 0 ? "Plant grass (uses 1 water)" : "Bucket is empty - refill it at the well";

        public override void Interact(PlayerController player)
        {
            var gm = GameManager.I;
            if (gm.Water <= 0) return;
            gm.Water--;
            // Chicken perk: Bug Eater - thicker grass near chickens.
            float bonus = gm.AnyAnimalNear(AnimalKind.Chicken, transform.position, 5f) ? 0.25f : 0f;
            SetAmount(1f + bonus);
            gm.AddXp(1);

            player.PlayAnim(ActAnim.Plant, 0.6f);
            player.SetBusy(0.4f, "Planting...");
            FX.Text(transform.position + Vector3.up * 1.3f, $"Grass planted  (water {gm.Water}/{gm.WaterMax})", new Color(0.6f, 0.95f, 0.5f), 0.85f);
            FX.Burst(transform.position + Vector3.up * 0.3f, Palette.GrassLush, 12, 0.1f, 2.5f);
            FX.Play(Sfx.Plant);
            Tutorial.Event("plant");
        }

        /// <summary>Planted by the Water Carrier (uses the worker's own bucket).</summary>
        public void PlantByWorker()
        {
            float bonus = GameManager.I.AnyAnimalNear(AnimalKind.Chicken, transform.position, 5f) ? 0.25f : 0f;
            SetAmount(1f + bonus);
            FX.Burst(transform.position + Vector3.up * 0.3f, Palette.GrassLush, 8, 0.08f, 2f);
        }

        public void SetAmount(float a)
        {
            Amount = Mathf.Clamp(a, 0f, 1.25f);
            Refresh();
        }

        public float Eat(float want)
        {
            float taken = Mathf.Min(want, Amount);
            Amount -= taken;
            Refresh();
            return taken;
        }

        public void Grow(float a)
        {
            if (Amount < 1f) SetAmount(Mathf.Min(1f, Amount + a));
        }

        void Refresh()
        {
            if (!grass) return;
            bool has = Amount > 0.02f;
            if (grass.gameObject.activeSelf != has) grass.gameObject.SetActive(has);
            if (!has) return;
            grass.localScale = new Vector3(1f, 0.2f + 0.8f * Amount, 1f);
            bool lush = Amount > 0.5f;
            if (lush == wasLush) return;
            wasLush = lush;
            var mat = Visuals.Mat(lush ? Palette.GrassLush : Palette.GrassShort);
            foreach (var r in grassRenderers) r.sharedMaterial = mat;
        }
    }
}
