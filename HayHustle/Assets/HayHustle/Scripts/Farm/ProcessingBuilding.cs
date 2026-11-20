using UnityEngine;

namespace HayHustle
{
    /// <summary>Bakery / Weaving Mill / Dairy / ...: turns raw goods into valuable goods. Used by the player or a Factory Hand.</summary>
    public class ProcessingBuilding : Interactable
    {
        public RecipeDef Recipe { get; private set; }
        float timer;
        bool working;
        int ready;
        Renderer lamp;
        Color lampColor;

        public void Init(RecipeDef r, Renderer indicator)
        {
            Recipe = r;
            lamp = indicator;
            Range = 4.2f;
            GameManager.I.Buildings.Add(this);
        }

        void OnDestroy()
        {
            if (GameManager.I) GameManager.I.Buildings.Remove(this);
        }

        string Out => GameData.ProductName(Recipe.Output);
        string In => GameData.ProductName(Recipe.Input);
        bool Locked => GameManager.I.Rank < Recipe.UnlockRank;

        public Vector3 FrontPosition => transform.position + transform.forward * 3.2f;

        /// <summary>True when a Factory Hand should come: something to collect, or it is idle and the goods are there.</summary>
        public bool NeedsWorker => !Locked && (ready > 0 || (!working && GameManager.I.HasProduct(Recipe.Input, Recipe.InputAmount)));

        public override string Prompt
        {
            get
            {
                if (Locked) return $"{Recipe.Building} - unlocks at Farm Rank {Recipe.UnlockRank}";
                if (ready > 0) return $"{Recipe.Building} - collect {ready} {Out}";
                if (working) return $"{Recipe.Building} - making {Out}... {Mathf.CeilToInt(timer)}s";
                return $"{Recipe.Building} - make {Out} (uses {Recipe.InputAmount} {In}, you have {GameManager.I.Inventory[(int)Recipe.Input]})";
            }
        }

        public override bool CanInteract => !Locked && !(working && ready == 0);

        public override void Interact(PlayerController player) => Operate(player);

        /// <summary>Collect what is ready, then start the next batch if possible. player is null for workers.</summary>
        public void Operate(PlayerController player)
        {
            if (Locked) return;
            var gm = GameManager.I;
            bool collected = false;
            if (ready > 0)
            {
                int got = 0;
                while (ready > 0 && gm.AddProduct(Recipe.Output)) { ready--; got++; gm.AddXp(5); }
                if (got > 0)
                {
                    FX.Text(transform.position + Vector3.up * 4.5f, $"+{got} {Out}", Palette.UiGold, 1.2f);
                    FX.Burst(transform.position + Vector3.up * 2f, Palette.Gold, 10, 0.1f, 3f);
                    FX.Play(Sfx.Coin, player ? 0.5f : 0.25f);
                    if (player) player.PlayAnim(ActAnim.Use, 0.5f);
                }
                if (ready > 0) { gm.Notify("Warehouse is full!", Palette.UiRed); return; }
                collected = true;
            }
            if (working) return;
            if (gm.HasProduct(Recipe.Input, Recipe.InputAmount))
            {
                gm.RemoveProduct(Recipe.Input, Recipe.InputAmount);
                working = true;
                timer = Recipe.Time;
                if (player) player.PlayAnim(ActAnim.Use, 0.6f);
                FX.Text(transform.position + Vector3.up * 4.5f, $"Making {Out}... {Recipe.Time:0}s", Color.white);
                FX.Burst(transform.position + Vector3.up * 3.5f, new Color(0.8f, 0.8f, 0.8f), 8, 0.15f, 1.5f);
                if (player) FX.Play(Sfx.Plant);
            }
            else if (!collected && player)
            {
                gm.Notify($"Need {Recipe.InputAmount} {In} in the warehouse.", Palette.UiRed);
                FX.Text(transform.position + Vector3.up * 4.5f, $"Need {Recipe.InputAmount} {In}", Palette.UiRed);
                FX.Play(Sfx.Error, 0.3f);
            }
        }

        void Update()
        {
            if (working)
            {
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    working = false;
                    ready++;
                    GameManager.I.Notify($"{Out} is ready at the {Recipe.Building}!", Palette.UiGreen);
                    FX.Text(transform.position + Vector3.up * 4.5f, $"{Out} ready!", Palette.UiGreen, 1.1f);
                    FX.Play(Sfx.Pickup, 0.35f);
                }
            }
            Color c = Locked ? new Color(0.4f, 0.4f, 0.4f) : ready > 0 ? new Color(0.3f, 1f, 0.3f) : working ? new Color(1f, 0.85f, 0.2f) : Color.white;
            if (lamp && c != lampColor)
            {
                lampColor = c;
                lamp.sharedMaterial = Visuals.Mat(c);
            }
        }
    }
}
