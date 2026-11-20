using UnityEngine;

namespace HayHustle
{
    /// <summary>Guided first steps for new players. Can be skipped and replayed from the pause menu.</summary>
    public static class Tutorial
    {
        class StepDef
        {
            public string Text;
            public string WaitFor;   // event that completes the step, or null = "Next" button
        }

        static readonly StepDef[] Steps =
        {
            new StepDef { Text = "Welcome! First, click the WELL (stone well in the yard) to fill your water bucket.", WaitFor = "well" },
            new StepDef { Text = "Now click an empty brown plot in the pasture to plant grass. Your animals walk to it and eat.", WaitFor = "plant" },
            new StepDef { Text = "Plant a few more plots. Soon a chicken lays an egg - click the EGG to collect it.", WaitFor = "collect" },
            new StepDef { Text = "Sell your goods: click the blue TRUCK, press +All next to Egg, then Send truck.", WaitFor = "truck" },
            new StepDef { Text = "Predators come from the forest! Click a predator to swing your net. The red bar shows how many hits are left - each click is one hit.", WaitFor = null },
            new StepDef { Text = "The buttons at the bottom right open the Shop (animals, helpers, workers, fields), Orders, Rewards and the Manual. Have fun!", WaitFor = null },
        };

        public static bool Active { get; private set; }
        public static int Step { get; private set; }
        public static int Count => Steps.Length;
        public static string Text => Active ? Steps[Step].Text : "";
        public static bool NeedsNextButton => Active && Steps[Step].WaitFor == null;

        /// <summary>No predator waves until the player has learned the basics.</summary>
        public static bool HoldsPredators => Active && Step < 4;

        public static void Begin()
        {
            Active = true;
            Step = 0;
        }

        public static void Event(string e)
        {
            if (!Active || Steps[Step].WaitFor != e) return;
            Next();
        }

        public static void Next()
        {
            Step++;
            FX.Play(Sfx.Pickup, 0.4f);
            if (Step >= Steps.Length) Finish();
        }

        /// <summary>Stops quietly (e.g. when leaving to the title screen).</summary>
        public static void Stop()
        {
            Active = false;
            Step = 0;
        }

        public static void Finish()
        {
            Active = false;
            Step = 0;
            var gm = GameManager.I;
            if (gm)
            {
                gm.TutorialDone = true;
                gm.Notify("Tutorial finished - good luck on your farm!", Palette.UiGold, 5f);
            }
        }

        /// <summary>Where the tutorial arrow points.</summary>
        public static Vector3? Target(GameManager gm)
        {
            if (!Active) return null;
            switch (Step)
            {
                case 0: return gm.WellPosition + Vector3.up * 2.8f;
                case 1:
                {
                    var p = gm.Player ? gm.Player.transform.position : Vector3.zero;
                    GrassPatch best = null;
                    float bd = float.MaxValue;
                    foreach (var g in gm.Fields[0].Grass)
                    {
                        if (!g.Selectable) continue;
                        float d = GameManager.FlatDistance(g.transform.position, p);
                        if (d < bd) { bd = d; best = g; }
                    }
                    return best ? best.transform.position + Vector3.up * 1.2f : (Vector3?)null;
                }
                case 2:
                    foreach (var pk in ProductPickup.All)
                        if (pk && pk.Product == Product.Egg) return pk.transform.position + Vector3.up * 1.4f;
                    return null;
                case 3: return gm.Truck ? gm.Truck.transform.position + Vector3.up * 3.2f : (Vector3?)null;
                default: return null;
            }
        }
    }
}
