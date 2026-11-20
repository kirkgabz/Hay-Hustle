using UnityEngine;

namespace HayHustle
{
    /// <summary>
    /// One plot in the Crop Field. Click an empty plot to choose seeds, click again to water it,
    /// wait until it is ripe, then click to harvest. Farmer survivors do all of this by themselves.
    /// </summary>
    public class CropPlot : Interactable
    {
        public CropKind? Crop { get; private set; }
        public float Growth { get; private set; }
        public bool Watered { get; private set; }

        public bool Empty => !Crop.HasValue;
        public bool Ripe => Crop.HasValue && Growth >= 1f;
        public bool NeedsWater => Crop.HasValue && !Watered && !Ripe;
        public string CropName => Crop.HasValue ? GameData.Crops[Crop.Value].Name : "";

        Renderer soil;
        Transform plant;
        int shownStage = -1;
        CropKind shownCrop;

        public void Init()
        {
            Range = 1.7f;
            soil = Visuals.Skin(Visuals.Prim(PrimitiveType.Cube, transform, new Vector3(0f, 0.03f, 0f), new Vector3(2.3f, 0.06f, 2.3f), Palette.Soil),
                Palette.Soil, Surface.Soil, Vector2.one).GetComponent<Renderer>();
            GameManager.I.CropPlots.Add(this);
            Refresh();
        }

        void OnDestroy()
        {
            if (GameManager.I) GameManager.I.CropPlots.Remove(this);
        }

        int Stage => Empty ? 0 : Ripe ? 3 : Growth >= 0.5f ? 2 : 1;

        public override string Prompt
        {
            get
            {
                var gm = GameManager.I;
                if (Empty) return "Plant a crop (choose seeds)";
                if (Ripe) return $"Harvest {CropName} (+{GameData.Crops[Crop.Value].Yield})";
                if (NeedsWater) return gm.Water > 0 ? $"Water the {CropName} (uses 1 water)" : "Bucket is empty - refill it at the well";
                return $"{CropName} growing... {Growth * 100f:0}%";
            }
        }

        public override bool CanInteract => Empty || Ripe || (NeedsWater && GameManager.I.Water > 0);

        public override void Interact(PlayerController player)
        {
            var gm = GameManager.I;
            if (Empty) { HUD.I.OpenSeeds(this); return; }
            if (Ripe) { Harvest(player); return; }
            if (NeedsWater && gm.Water > 0)
            {
                gm.Water--;
                player.PlayAnim(ActAnim.Plant, 0.6f);
                player.SetBusy(0.4f, "Watering...");
                WaterFree();
            }
        }

        /// <summary>Plant seeds (pays the seed cost). Returns false if there are not enough coins.</summary>
        public bool Plant(CropKind kind)
        {
            var def = GameData.Crops[kind];
            if (!Empty || !GameManager.I.Spend(def.SeedCost)) return false;
            Crop = kind;
            Growth = 0f;
            Watered = false;
            FX.Text(transform.position + Vector3.up * 1.4f, $"{def.Name} planted - now water it", new Color(0.6f, 0.95f, 0.5f), 0.85f);
            FX.Burst(transform.position + Vector3.up * 0.3f, Palette.Soil, 8, 0.08f, 2f);
            FX.Play(Sfx.Plant);
            Refresh();
            return true;
        }

        public void WaterFree()
        {
            if (!NeedsWater) return;
            Watered = true;
            FX.Burst(transform.position + Vector3.up * 0.5f, new Color(0.35f, 0.6f, 0.95f), 10, 0.07f, 2f);
            FX.Play(Sfx.Water, 0.3f);
            Refresh();
        }

        public void Harvest(PlayerController player)
        {
            if (!Ripe) return;
            var gm = GameManager.I;
            var def = GameData.Crops[Crop.Value];
            int got = 0;
            for (int i = 0; i < def.Yield; i++) if (gm.AddProduct(def.Produces)) got++;
            if (got == 0)
            {
                gm.Notify("Storage is full - make room before harvesting.", Palette.UiRed);
                FX.Play(Sfx.Error, 0.3f);
                return;
            }
            gm.AddXp(2);
            FX.Text(transform.position + Vector3.up * 1.5f, $"+{got} {def.Name}", Palette.UiGold, 1.1f);
            FX.Burst(transform.position + Vector3.up * 0.5f, def.Color, 12, 0.09f, 2.5f);
            FX.Play(Sfx.Pickup, 0.5f);
            if (player) player.PlayAnim(ActAnim.Pickup, 0.5f);
            Crop = null;
            Growth = 0f;
            Watered = false;
            Refresh();
        }

        /// <summary>Restore from a save.</summary>
        public void SetState(int crop, float growth, bool watered)
        {
            Crop = crop >= 0 && crop < System.Enum.GetValues(typeof(CropKind)).Length ? (CropKind?)crop : null;
            Growth = Crop.HasValue ? Mathf.Clamp01(growth) : 0f;
            Watered = Crop.HasValue && watered;
            Refresh();
        }

        void Update()
        {
            if (!Crop.HasValue || !Watered || Ripe) return;
            Growth = Mathf.Min(1f, Growth + Time.deltaTime / GameData.Crops[Crop.Value].GrowTime);
            if (Stage != shownStage) Refresh();
            if (Ripe) FX.Text(transform.position + Vector3.up * 1.3f, $"{CropName} is ripe!", Palette.UiGold, 0.8f);
        }

        void Refresh()
        {
            if (!soil) return;
            soil.sharedMaterial = Visuals.MatTex(Watered ? Palette.Soil * 0.7f + Color.black * 0.3f : Palette.Soil, Surface.Soil, Vector2.one);
            int stage = Stage;
            if (stage == shownStage && (!Crop.HasValue || Crop.Value == shownCrop)) return;
            shownStage = stage;
            if (plant) Destroy(plant.gameObject);
            plant = null;
            if (Crop.HasValue && stage > 0)
            {
                shownCrop = Crop.Value;
                plant = Models.BuildCrop(Crop.Value, stage, transform);
            }
        }
    }
}
