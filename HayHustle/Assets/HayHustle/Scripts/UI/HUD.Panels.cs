using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HayHustle
{
    /// <summary>The windows: Shop, Truck, Warehouse, Orders, Rewards, Manual, Pause/Settings.</summary>
    public partial class HUD
    {
        int shopTab, manualTab;
        Vector2 sellScroll, buyScroll, truckScroll, manualScroll, workerScroll, farmScroll, whScroll;

        static readonly string[] ShopTabs = { "Animals", "Seeds", "Helpers", "Workers", "Farm", "Lights", "Sell animals" };

        Rect Window(float w, float h, string heading)
        {
            Rect r = CenterRect(w, h);
            PanelBg(r);
            Lbl(new Rect(r.x + 26, r.y + 14, r.width - 120, 40), heading, h2);
            if (CloseButton(r)) { Open = Panel.None; FX.Play(Sfx.Click, 0.2f); }
            return r;
        }

        void Row(Rect r) => Tex(r, rowTex);

        // ================= Shop =================

        void DrawShop(GameManager gm)
        {
            Rect r = Window(1000, 700, "Shop");
            float tx = r.x + 26, ty = r.y + 62;
            for (int i = 0; i < ShopTabs.Length; i++)
            {
                float tabW = (r.width - 52f) / ShopTabs.Length;
                if (Tgl(new Rect(tx + i * tabW, ty, tabW - 6f, 42), shopTab == i, ShopTabs[i]) && shopTab != i) { shopTab = i; FX.Play(Sfx.Click, 0.2f); }
            }
            Lbl(new Rect(r.xMax - 260, r.y + 14, 190, 40), $"<color=#{Hex(Palette.UiGold)}>Coins {gm.Coins}</color>", labelR);
            Rect area = new Rect(r.x + 26, ty + 56, r.width - 52, r.yMax - ty - 74);
            switch (shopTab)
            {
                case 0: DrawBuy(gm, area); break;
                case 1: DrawSeedShop(gm, area); break;
                case 2: DrawHelpers(gm, area); break;
                case 3: DrawWorkers(gm, area); break;
                case 4: DrawFarm(gm, area); break;
                case 5: DrawLights(gm, area); break;
                default: DrawSell(gm, area); break;
            }
        }

        void DrawBuy(GameManager gm, Rect area)
        {
            Lbl(new Rect(area.x, area.y, area.width, 26), "Each kind of animal lives in its own field. Buy the field first (here, in Shop > Farm, or at the FOR SALE sign).", small);
            Rect list = new Rect(area.x, area.y + 32, area.width, area.height - 32);
            const float step = 126f;
            buyScroll = BeginScroll(list, buyScroll, GameData.Animals.Count * step);
            float y = 0, w = list.width - 26;
            foreach (var d in GameData.Animals.Values)
            {
                Row(new Rect(0, y, w, step - 8));
                var home = gm.FieldFor(d.Kind);
                string where = home.Owned ? $"{home.Def.Name} {gm.CountInField(home.Index)}/{home.Def.Capacity}" : $"lives in the {home.Def.Name}";
                Lbl(new Rect(16, y + 8, w - 330, 32), $"<b>{d.Name}</b>   <color=#AAAAAA>makes {GameData.ProductName(d.Produces)} every {d.ProduceTime:0}s  -  {where}</color>", label);
                Lbl(new Rect(16, y + 42, w - 330, 70), d.Perk, wrap);
                if (gm.Rank < d.UnlockRank)
                    Lbl(new Rect(w - 300, y + 40, 290, 36), $"<color=#AAAAAA>Unlocks at Farm Rank {d.UnlockRank}</color>", small);
                else if (!home.Owned)
                {
                    var hd = home.Def;
                    int idx = home.Index;
                    Lbl(new Rect(w - 300, y + 8, 290, 30), $"<color=#{Hex(Palette.UiRed)}>Needs the {hd.Name}</color>", small);
                    if (Btn(new Rect(w - 300, y + 44, 284, 48), $"Buy field - {hd.Price}"))
                        Confirm($"Buy the {hd.Name}?", $"Pay {hd.Price} coins for a fenced field with {hd.Cols * hd.Rows} grass plots. It is the home for up to {hd.Capacity} {d.Plural}.", "Buy field", () => gm.BuyField(idx));
                }
                else if (gm.FieldFull(d.Kind))
                {
                    Lbl(new Rect(w - 300, y + 8, 290, 30), $"<color=#{Hex(Palette.UiRed)}>{home.Def.Name} is full ({home.Def.Capacity}/{home.Def.Capacity})</color>", small);
                    Lbl(new Rect(w - 300, y + 40, 290, 60), "<color=#AAAAAA>Sell an animal (Sell animals tab) to make room.</color>", wrap);
                }
                else
                {
                    var def = d;
                    if (Btn(new Rect(w - 300, y + 12, 284, 44), $"Buy {d.BabyName.ToLower()} - {d.BabyPrice}"))
                        Confirm($"Buy a {def.BabyName.ToLower()}?", $"Pay {def.BabyPrice} coins for a baby {def.Name.ToLower()}. It needs time to grow before it produces.", "Buy", () => gm.BuyAnimal(def.Kind, Stage.Baby));
                    if (Btn(new Rect(w - 300, y + 62, 284, 44), $"Buy adult - {d.AdultPrice}"))
                        Confirm($"Buy an adult {def.Name.ToLower()}?", $"Pay {def.AdultPrice} coins. An adult produces {GameData.ProductName(def.Produces).ToLower()} right away.", "Buy", () => gm.BuyAnimal(def.Kind, Stage.Adult));
                }
                y += step;
            }
            GUI.EndScrollView();
        }

        void DrawSeedShop(GameManager gm, Rect area)
        {
            float y = area.y;
            Lbl(new Rect(area.x, y, area.width, 26), "Buy seeds here. Then click the Crop Field, pick a seed and press <b>Plant all</b>. Seeds don't use storage space.", small);
            y += 36;
            foreach (var d in GameData.Crops.Values)
            {
                Rect row = new Rect(area.x, y, area.width, 96);
                Row(row);
                int have = gm.Seeds[(int)d.Kind];
                Lbl(new Rect(row.x + 16, row.y + 8, row.width - 520, 32), $"<b>{d.Name} seeds</b>   <color=#AAAAAA>you have <b>{have}</b></color>", label);
                Lbl(new Rect(row.x + 16, row.y + 46, row.width - 520, 40), $"{d.SeedCost} coins each  -  grows in {d.GrowTime:0}s  -  harvest {d.Yield}  -  {GameData.FoodValue[d.Produces]} food each", small);
                var def = d;
                if (gm.Rank < d.UnlockRank)
                    Lbl(new Rect(row.xMax - 300, row.y + 30, 290, 36), $"<color=#AAAAAA>Unlocks at Farm Rank {d.UnlockRank}</color>", small);
                else
                {
                    int[] packs = { 1, 5, 10 };
                    for (int i = 0; i < packs.Length; i++)
                    {
                        int n = packs[i];
                        if (Btn(new Rect(row.xMax - 498 + i * 164, row.y + 24, 156, 48), $"Buy {n} - {d.SeedCost * n}"))
                            Confirm($"Buy {n} {def.Name} seeds?", $"Pay {def.SeedCost * n} coins for {n} {def.Name} seeds.", "Buy", () => gm.BuySeeds(def.Kind, n));
                    }
                }
                y += 106;
            }
        }

        void DrawHelpers(GameManager gm, Rect area)
        {
            float y = area.y;
            foreach (var d in GameData.Helpers.Values)
            {
                Rect row = new Rect(area.x, y, area.width, 130);
                Row(row);
                int have = gm.CountHelpers(d.Kind);
                Lbl(new Rect(row.x + 16, row.y + 8, 500, 32), $"<b>{d.Name}</b>   <color=#AAAAAA>({have}/{d.Max})</color>", label);
                Lbl(new Rect(row.x + 16, row.y + 44, row.width - 340, 76), d.Description, wrap);
                if (gm.Rank < d.UnlockRank)
                    Lbl(new Rect(row.xMax - 300, row.y + 44, 290, 36), $"<color=#AAAAAA>Unlocks at Farm Rank {d.UnlockRank}</color>", small);
                else if (have >= d.Max)
                    Lbl(new Rect(row.xMax - 300, row.y + 44, 290, 36), "<color=#AAAAAA>You have the maximum</color>", small);
                else
                {
                    var def = d;
                    if (Btn(new Rect(row.xMax - 300, row.y + 40, 284, 50), $"Buy - {d.Price} coins"))
                        Confirm($"Buy a {def.Name}?", $"Pay {def.Price} coins. {def.Description}", "Buy", () => gm.BuyHelper(def.Kind));
                }
                y += 142;
            }
        }

        void DrawWorkers(GameManager gm, Rect area)
        {
            Lbl(new Rect(area.x, area.y, area.width, 26), $"Wages are paid every morning: <color=#{Hex(Palette.UiGold)}>{gm.TotalWages} coins/day</color>. Click a worker's field button to choose where they work.", small);
            // Guard weapons (all guards use the current one)
            {
                var cur = GameData.GuardWeapons[gm.GuardWeaponLevel];
                Row(new Rect(area.x, (area.y + 32), area.width, 50));
                Lbl(new Rect(area.x + 14, (area.y + 32), area.width - 340, 50), $"<b>Guard weapon:</b> {cur.Name}  <color=#AAAAAA>({cur.Damage} damage per hit, {cur.Cooldown:0.0}s between hits)</color>", small);
                if (gm.GuardWeaponLevel < GameData.GuardWeapons.Length - 1)
                {
                    var next = GameData.GuardWeapons[gm.GuardWeaponLevel + 1];
                    if (Btn(new Rect(area.x + area.width - 320, (area.y + 32) + 6, 310, 38), $"Upgrade to {next.Name} - {next.Cost}"))
                        Confirm($"Upgrade to the {next.Name}?", $"Pay {next.Cost} coins. Every guard (hired and survivors) gets a {next.Name}: {next.Damage} damage per hit.", "Upgrade", gm.UpgradeGuardWeapons);
                }
                else Lbl(new Rect(area.x + area.width - 320, (area.y + 32), 310, 50), $"<color=#{Hex(Palette.UiGreen)}>Best weapon</color>", small);
            }
            Rect list = new Rect(area.x, area.y + 90, area.width, area.height - 90);
            const float step = 132f;
            float contentH = 0f;
            foreach (var wd in GameData.Workers.Values) contentH += step + (IsFieldJob(wd.Kind) ? gm.CountWorkers(wd.Kind) * 46f : 0f);
            workerScroll = BeginScroll(list, workerScroll, contentH);
            float y = 0, w = list.width - 26;
            foreach (var d in GameData.Workers.Values)
            {
                Row(new Rect(0, y, w, step - 8));
                int have = gm.CountWorkers(d.Kind);
                Lbl(new Rect(16, y + 8, 520, 32), $"<b>{d.Name}</b>   <color=#AAAAAA>({have}/{d.Max})   hire {d.HireCost}  -  wage {d.Wage}/day</color>", label);
                Lbl(new Rect(16, y + 44, w - 340, 72), d.Job, wrap);
                var def = d;
                if (gm.Rank < d.UnlockRank)
                    Lbl(new Rect(w - 300, y + 40, 290, 36), $"<color=#AAAAAA>Unlocks at Farm Rank {d.UnlockRank}</color>", small);
                else
                {
                    GUI.enabled = confirm == null && have < d.Max;
                    if (Btn(new Rect(w - 300, y + 12, 284, 46), $"Hire - {d.HireCost} coins"))
                        Confirm($"Hire a {def.Name}?", $"Pay {def.HireCost} coins now and {def.Wage} coins every morning. {def.Job}", "Hire", () => gm.HireWorker(def.Kind));
                    GUI.enabled = confirm == null && have > 0;
                    if (Btn(new Rect(w - 300, y + 66, 284, 42), "Dismiss one"))
                        Confirm($"Dismiss your {def.Name}?", "They leave the farm. The hiring fee is not refunded.", "Dismiss", () => gm.FireWorker(def.Kind), true);
                    GUI.enabled = confirm == null;
                }
                y += step;

                // Each hired worker: which field they look after
                if (!IsFieldJob(d.Kind)) continue;
                int n = 0;
                foreach (var worker in gm.Workers.Where(x => x && x.Kind == def.Kind).ToList())
                {
                    n++;
                    Row(new Rect(40, y - 6, w - 40, 40));
                    Lbl(new Rect(56, y - 6, 300, 40), $"{def.Name} #{n}  -  <color=#AAAAAA>{worker.Status}</color>", small);
                    if (Btn(new Rect(w - 300, y - 2, 284, 32), "Works in: " + worker.AreaName))
                    {
                        worker.AssignedField = NextField(gm, worker.AssignedField);
                        FX.Play(Sfx.Click, 0.25f);
                    }
                    y += 46;
                }
            }
            GUI.EndScrollView();
        }

        static bool IsFieldJob(WorkerKind k) => k == WorkerKind.Collector || k == WorkerKind.WaterCarrier || k == WorkerKind.Guard;

        /// <summary>Cycle: All fields (rotation) -> each owned field -> back to all.</summary>
        static int NextField(GameManager gm, int current)
        {
            var owned = gm.Fields.Where(f => f.Owned).Select(f => f.Index).ToList();
            int i = owned.IndexOf(current);
            if (current < 0) return owned.Count > 0 ? owned[0] : -1;
            return i >= 0 && i + 1 < owned.Count ? owned[i + 1] : -1;
        }

        void DrawFarm(GameManager gm, Rect area)
        {
            float contentH = gm.Fields.Count * 92f + 430f;
            farmScroll = BeginScroll(area, farmScroll, contentH);
            float y = 0, w = area.width - 26;

            Lbl(new Rect(0, y, w, 30), "<b>Fields</b>   <color=#AAAAAA>each kind of animal needs its own field</color>", label);
            y += 36;
            foreach (var f in gm.Fields)
            {
                Row(new Rect(0, y, w, 84));
                var d = f.Def;
                Lbl(new Rect(16, y + 6, 520, 30), $"<b>{d.Name}</b>   <color=#AAAAAA>for {GameData.Animals[d.Kind].Plural}  -  {d.Cols * d.Rows} plots  -  up to {d.Capacity}</color>", label);
                string status = f.Owned ? $"<color=#{Hex(Palette.UiGreen)}>Owned  -  {gm.CountInField(f.Index)} animals here</color>"
                    : gm.Rank < d.UnlockRank ? $"<color=#AAAAAA>Unlocks at Farm Rank {d.UnlockRank}</color>" : $"For sale: {d.Price} coins";
                Lbl(new Rect(16, y + 42, 520, 30), status, small);
                if (!f.Owned && gm.Rank >= d.UnlockRank)
                {
                    int idx = f.Index;
                    if (Btn(new Rect(w - 300, y + 18, 284, 48), $"Buy - {d.Price} coins"))
                        Confirm($"Buy {d.Name}?", $"Pay {d.Price} coins for a fenced field with {d.Cols * d.Rows} grass plots. It is the home for up to {d.Capacity} {GameData.Animals[d.Kind].Plural}.", "Buy field", () => gm.BuyField(idx));
                }
                y += 92;
            }

            y += 10;
            Lbl(new Rect(0, y, w, 30), "<b>Fences</b>", label);
            y += 34;
            Row(new Rect(0, y, w, 84));
            int lvl = gm.FenceLevel;
            Lbl(new Rect(16, y + 6, 560, 30), $"{GameData.FenceNames[lvl]}  <color=#AAAAAA>- predators {GameData.FenceSlow[lvl] * 100f:0}% slower inside your fields</color>", label);
            if (lvl < GameData.FenceUpgradeCost.Length)
            {
                int cost = GameData.FenceUpgradeCost[lvl];
                Lbl(new Rect(16, y + 42, 560, 30), $"Next: {GameData.FenceNames[lvl + 1]} ({GameData.FenceSlow[lvl + 1] * 100f:0}% slower)", small);
                if (Btn(new Rect(w - 300, y + 18, 284, 48), $"Upgrade - {cost} coins"))
                    Confirm("Upgrade your fences?", $"Pay {cost} coins for {GameData.FenceNames[lvl + 1]} on all your fields. Predators move {GameData.FenceSlow[lvl + 1] * 100f:0}% slower inside them.", "Upgrade", gm.UpgradeFence);
            }
            else Lbl(new Rect(16, y + 42, 560, 30), $"<color=#{Hex(Palette.UiGreen)}>Fully upgraded</color>", small);
            y += 96;

            Lbl(new Rect(0, y, w, 30), "<b>Barn storage</b>", label);
            y += 34;
            Row(new Rect(0, y, w, 84));
            Lbl(new Rect(16, y + 6, 600, 30), $"Barn level {gm.BarnLevel}/{GameData.BarnUpgradeCost.Length}  -  +{gm.BarnLevel * GameData.BarnStoragePerLevel} storage", label);
            Lbl(new Rect(16, y + 42, w - 340, 30), $"<color=#AAAAAA>Each level adds {GameData.BarnStoragePerLevel} storage space. The barn is reinforced - zombies can't break in.</color>", small);
            if (gm.BarnLevel < GameData.BarnUpgradeCost.Length)
            {
                int cost = GameData.BarnUpgradeCost[gm.BarnLevel];
                if (Btn(new Rect(w - 300, y + 18, 284, 48), $"Upgrade - {cost} coins"))
                    Confirm("Upgrade the barn?", $"Pay {cost} coins for +{GameData.BarnStoragePerLevel} storage space.", "Upgrade", gm.UpgradeBarn);
            }
            y += 96;

            Lbl(new Rect(0, y, w, 30), "<b>Traps</b>", label);
            y += 34;
            Row(new Rect(0, y, w, 84));
            Lbl(new Rect(16, y + 6, 600, 30), $"Traps set: {gm.Traps}/{GameData.MaxTraps}", label);
            Lbl(new Rect(16, y + 42, 600, 30), "<color=#AAAAAA>Placed at the edge of your fields. Catches a fox, wolf, snake or rat (bears get hurt). Used once.</color>", small);
            if (gm.Traps < GameData.MaxTraps && Btn(new Rect(w - 300, y + 18, 284, 48), $"Buy trap - {GameData.TrapPrice} coins"))
                Confirm("Buy a trap?", $"Pay {GameData.TrapPrice} coins. It is set automatically at the edge of your farm.", "Buy", gm.BuyTrap);
            GUI.EndScrollView();
        }

        void DrawLights(GameManager gm, Rect area)
        {
            float y = area.y;
            Lbl(new Rect(area.x, y, area.width, 30), "<b>Night lights</b>   <color=#AAAAAA>nights are dark and more predators come - light up your farm</color>", label);
            y += 40;
            Rect row = new Rect(area.x, y, area.width, 150);
            Row(row);
            Lbl(new Rect(row.x + 16, row.y + 10, 600, 32), $"<b>Lamp post</b>   <color=#AAAAAA>built: {gm.Lamps}/{GameData.MaxLamps}</color>", label);
            Lbl(new Rect(row.x + 16, row.y + 48, row.width - 340, 92),
                "Turns on by itself at 18:00 and off at 06:00. Predators move 40% slower near a lit lamp and leave animals in the light alone when they can. " +
                "You choose where it goes: click a spot on your farm. Click a lamp post later to move it.", wrap);
            if (gm.Lamps >= GameData.MaxLamps)
                Lbl(new Rect(row.xMax - 300, row.y + 50, 290, 36), "<color=#AAAAAA>All lamp spots are used</color>", small);
            else if (Btn(new Rect(row.xMax - 300, row.y + 46, 284, 54), $"Buy lamp - {GameData.LampPrice} coins", bigButton))
                Confirm("Buy a lamp post?", $"Next, click where you want it on your farm (inside the wall). You pay {GameData.LampPrice} coins when it is placed. Esc cancels.", "Choose a spot", gm.BuyLamp);
            y += 166;
            Lbl(new Rect(area.x, y, area.width, 60), "<color=#AAAAAA>Your farmer also carries a lantern at night. It switches on by itself.</color>", wrap);
        }

        void DrawSell(GameManager gm, Rect area)
        {
            var groups = new SortedDictionary<string, List<Animal>>();
            foreach (var a in gm.Animals)
            {
                if (!a || a.IsCarried || a.InBarn) continue;
                if (!groups.TryGetValue(a.DisplayName, out var list))
                {
                    list = new List<Animal>();
                    groups[a.DisplayName] = list;
                }
                list.Add(a);
            }
            if (groups.Count == 0)
            {
                Lbl(new Rect(area.x, area.y, area.width, 40), "You have no animals to sell.", label);
                return;
            }
            Lbl(new Rect(area.x, area.y, area.width, 26), "<color=#AAAAAA>Prime and rare animals sell for more. Babies and hungry animals sell for less.</color>", small);
            Rect list2 = new Rect(area.x, area.y + 32, area.width, area.height - 32);
            sellScroll = BeginScroll(list2, sellScroll, groups.Count * 64f);
            float y = 0, w = list2.width - 26;
            Animal toSell = null;
            foreach (var kv in groups)
            {
                var a = kv.Value[0];
                int price = GameData.SellPrice(a);
                Row(new Rect(0, y, w, 56));
                Lbl(new Rect(16, y, 520, 56), $"<b>{kv.Key}</b>   x{kv.Value.Count}", label);
                if (Btn(new Rect(w - 300, y + 6, 284, 44), $"Sell 1 - {price} coins")) toSell = a;
                y += 64;
            }
            GUI.EndScrollView();
            if (toSell != null)
            {
                var target = toSell;
                int price = GameData.SellPrice(target);
                Confirm($"Sell a {target.DisplayName.ToLower()}?", $"You get {price} coins. The animal leaves your farm.", "Sell", () => gm.SellAnimal(target));
            }
        }

        // ================= Truck =================

        void DrawTruck(GameManager gm)
        {
            var t = gm.Truck;
            Rect r = Window(1000, 700, "Delivery Truck");
            Lbl(new Rect(r.x + 26, r.y + 58, r.width - 52, 26), "Load goods, then send the truck to town. It comes back after 24 seconds with your coins.", small);

            var shown = new List<ProductDef>();
            foreach (var pd in GameData.Products.Values)
                if (gm.Rank >= pd.UnlockRank || gm.Inventory[(int)pd.Product] > 0 || t.Cargo[(int)pd.Product] > 0) shown.Add(pd);

            Rect list = new Rect(r.x + 26, r.y + 94, r.width - 52, r.height - 200);
            truckScroll = BeginScroll(list, truckScroll, shown.Count * 64f);
            float y = 0, w = list.width - 26;
            foreach (var pd in shown)
            {
                int have = gm.Inventory[(int)pd.Product], inTruck = t.Cargo[(int)pd.Product], price = gm.ProductPrice(pd.Product);
                Row(new Rect(0, y, w, 56));
                string wanted = pd.Product == gm.WantedProduct ? $"  <color=#{Hex(Palette.UiGold)}><b>WANTED x2</b></color>" : "";
                Lbl(new Rect(16, y, 560, 56), $"<b>{pd.Name}</b>   have {have}   in truck <b>{inTruck}</b>   {price} each{wanted}", label);
                if (Btn(new Rect(w - 372, y + 7, 110, 42), "+1")) t.Load(pd.Product, 1);
                if (Btn(new Rect(w - 254, y + 7, 110, 42), "+All")) t.Load(pd.Product, have);
                if (Btn(new Rect(w - 136, y + 7, 120, 42), "Return")) t.Unload(pd.Product);
                y += 64;
            }
            GUI.EndScrollView();

            float fy = r.yMax - 92;
            Lbl(new Rect(r.x + 26, fy + 10, 600, 40), $"Cargo <b>{t.CargoCount}/{Truck.Capacity}</b>     Value <color=#{Hex(Palette.UiGold)}><b>{t.CargoValue} coins</b></color>", label);
            bool canSend = t.CargoCount > 0 && confirm == null;
            GUI.enabled = canSend;
            if (Btn(new Rect(r.xMax - 326, fy, 300, 64), "Send truck", bigButton))
                Confirm("Send the truck?", $"The truck drives {t.CargoCount} goods to town and comes back with {t.CargoValue} coins.", "Send", () => { t.Send(); Open = Panel.None; });
            GUI.enabled = confirm == null;
        }

        // ================= Warehouse =================

        void DrawWarehouse(GameManager gm)
        {
            Rect r = Window(1000, 700, $"Storage   {gm.InventoryTotal}/{gm.StorageCapacity}   (warehouse {gm.WarehouseCapacity} + barn {gm.BarnLevel * GameData.BarnStoragePerLevel})");
            Lbl(new Rect(r.x + 26, r.y + 58, r.width - 360, 26), $"Quick Sell pays instantly, but only {GameData.QuickSellRate * 100f:0}% of the truck price.", small);
            int cost = Warehouse.UpgradeCost;
            if (Btn(new Rect(r.xMax - 330, r.y + 58, 300, 44), $"Upgrade +20 space - {cost}"))
                Confirm("Upgrade the warehouse?", $"Pay {cost} coins for 20 more storage space.", "Upgrade", Warehouse.Upgrade);

            var shown = new List<ProductDef>();
            foreach (var pd in GameData.Products.Values) if (gm.Inventory[(int)pd.Product] > 0) shown.Add(pd);
            Rect list = new Rect(r.x + 26, r.y + 116, r.width - 52, r.height - 140);
            if (shown.Count == 0)
            {
                Lbl(new Rect(list.x, list.y, list.width, 40), "The warehouse is empty. Collect goods from your animals first.", label);
                return;
            }
            whScroll = BeginScroll(list, whScroll, shown.Count * 64f);
            float y = 0, w = list.width - 26;
            foreach (var pd in shown)
            {
                int have = gm.Inventory[(int)pd.Product];
                int each = gm.QuickSellPrice(pd.Product);
                Row(new Rect(0, y, w, 56));
                Lbl(new Rect(16, y, 520, 56), $"<b>{pd.Name}</b>   x{have}   <color=#AAAAAA>quick sell {each} each (truck {gm.ProductPrice(pd.Product)})</color>", label);
                var p = pd.Product;
                if (Btn(new Rect(w - 372, y + 7, 150, 42), "Sell 1")) gm.QuickSell(p, 1);
                if (Btn(new Rect(w - 210, y + 7, 194, 42), $"Sell all ({have * each})"))
                    Confirm($"Quick sell all {pd.Name}?", $"Sell {have} {pd.Name} now for {have * each} coins. (The truck would pay {have * gm.ProductPrice(p)}.)", "Sell all", () => gm.QuickSell(p, have));
                y += 64;
            }
            GUI.EndScrollView();
        }

        // ================= Orders =================

        void DrawOrders(GameManager gm)
        {
            Rect r = Window(1040, 720, "Order Board");
            Lbl(new Rect(r.x + 26, r.y + 58, r.width - 52, 26), "Villagers pay extra for these orders. Deliver everything before the time runs out!", small);
            var book = gm.Orders;
            if (book.Orders.Count == 0)
            {
                Lbl(new Rect(r.x + 26, r.y + 110, r.width - 52, 40), Tutorial.HoldsPredators ? "Orders start after the tutorial." : "No orders right now - new ones arrive soon.", label);
                return;
            }
            float cardW = (r.width - 52 - 2 * 16) / 3f;
            for (int i = 0; i < book.Orders.Count; i++)
            {
                var o = book.Orders[i];
                Rect c = new Rect(r.x + 26 + i * (cardW + 16), r.y + 100, cardW, r.height - 130);
                Row(c);
                Lbl(new Rect(c.x + 14, c.y + 10, c.width - 28, 30), $"<b>{o.Customer}</b>", label);
                int secs = Mathf.CeilToInt(o.TimeLeft);
                Color tc = o.TimeLeft < 60f ? Palette.UiRed : new Color(0.85f, 0.85f, 0.8f);
                Lbl(new Rect(c.x + 14, c.y + 42, c.width - 28, 26), $"<color=#{Hex(tc)}>Time left {secs / 60}:{secs % 60:00}</color>", small);
                float y = c.y + 76;
                foreach (var it in o.Items)
                {
                    int have = gm.Inventory[(int)it.Product];
                    bool enough = have >= it.Amount;
                    Color ic = enough ? Palette.UiGreen : Palette.UiRed;
                    Lbl(new Rect(c.x + 14, y, c.width - 100, 30), $"{GameData.ProductName(it.Product)}   <color=#{Hex(ic)}><b>{Mathf.Min(have, it.Amount)}/{it.Amount}</b></color>", label);
                    if (enough) { y += 34; continue; }
                    string how = gm.HowToGet(it.Product, out var target);
                    if (target.HasValue)
                    {
                        Vector3 goal = target.Value;
                        string guide = GameData.ProductName(it.Product) + ": " + how;
                        if (Btn(new Rect(c.xMax - 86, y + 1, 72, 28), "Show")) { GuideTo(goal, guide); Open = Panel.None; }
                    }
                    float hh = Mathf.Min(CalcH(tinyWrap, how, c.width - 28), 66f);
                    Lbl(new Rect(c.x + 14, y + 30, c.width - 28, hh), how, tinyWrap);
                    y += 38 + hh;
                }
                Lbl(new Rect(c.x + 14, c.yMax - 150, c.width - 28, 30), $"Reward <color=#{Hex(Palette.UiGold)}><b>{o.Reward} coins</b></color>  +{o.Xp} XP", label);
                var order = o;
                bool can = book.CanDeliver(gm, o);
                GUI.enabled = can && confirm == null;
                if (Btn(new Rect(c.x + 14, c.yMax - 110, c.width - 28, 48), can ? "Deliver" : "Not enough goods", bigButton))
                    Confirm($"Deliver to {o.Customer}?", $"Hand over the goods for {o.Reward} coins.", "Deliver", () => book.Deliver(gm, order));
                GUI.enabled = confirm == null;
                if (Btn(new Rect(c.x + 14, c.yMax - 54, c.width - 28, 40), "Discard"))
                    Confirm("Discard this order?", $"{o.Customer}'s order will be removed.", "Discard", () => book.Discard(order));
            }
        }

        // ================= Daily rewards =================

        void DrawRewards(GameManager gm)
        {
            Rect r = Window(1180, 520, "Daily Rewards");
            bool can = DailyRewards.CanClaim(gm);
            int next = DailyRewards.NextIndex(gm);
            float bw = 148, gap = 10, x = r.x + (r.width - (7 * bw + 6 * gap)) / 2f, y = r.y + 76;
            for (int i = 0; i < 7; i++)
            {
                bool claimed = can ? i < next : i < gm.RewardStreak;
                bool today = can && i == next;
                Rect b = new Rect(x + i * (bw + gap), y, bw, 220);
                Color bg = today ? new Color(0.92f, 0.68f, 0.16f, 0.95f) : claimed ? new Color(0.22f, 0.48f, 0.22f, 0.95f) : new Color(1f, 1f, 1f, 0.1f);
                Block(b, bg);
                Lbl(new Rect(b.x, b.y + 10, bw, 34), $"Day {i + 1}", h2c);
                Lbl(new Rect(b.x + 8, b.y + 64, bw - 16, 96), DailyRewards.Days[i].Label, wrapC);
                Lbl(new Rect(b.x, b.y + 176, bw, 30), claimed ? "Claimed" : today ? "<b>TODAY</b>" : "", smallC);
            }
            if (can)
            {
                if (Btn(new Rect(r.x + (r.width - 420) / 2f, r.y + 318, 420, 64), $"Claim Day {next + 1} reward", bigButton))
                {
                    DailyRewards.Claim(gm);
                    Open = Panel.None;
                }
            }
            else Lbl(new Rect(r.x, r.y + 330, r.width, 40), "Come back tomorrow for your next reward!", labelC);
            Lbl(new Rect(r.x, r.y + 410, r.width, 30), "Log in every day to keep your streak. Missing a day resets it to Day 1.", smallC);
        }

        // ================= Manual =================

        void DrawHelp()
        {
            Rect r = Window(1260, 820, "User Manual - what to click");
            int n = Manual.Titles.Length;
            int cols = (r.height - 90f) / n - 4f < 34f ? 2 : 1;   // short screens (phones): two columns of tabs
            int rows = Mathf.CeilToInt(n / (float)cols);
            float colW = cols == 1 ? 220f : 172f;
            float tx = r.x + 26, ty = r.y + 66, tw = colW * cols + (cols - 1) * 4f, th = Mathf.Min(52f, (r.height - 90f) / rows - 4f);
            for (int i = 0; i < n; i++)
            {
                int c = i / rows, row = i % rows;
                if (Tgl(new Rect(tx + c * (colW + 4f), ty + row * (th + 4), colW, th), manualTab == i, Manual.Titles[i]) && manualTab != i)
                {
                    manualTab = i;
                    manualScroll = Vector2.zero;
                }
            }
            Rect area = new Rect(tx + tw + 24, ty, r.width - tw - 76, r.height - 90);
            Row(area);
            Rect inner = new Rect(area.x + 16, area.y + 12, area.width - 32, area.height - 24);
            string page = Manual.Pages[manualTab];
            float textH = CalcH(wrap, page, inner.width - 30);
            manualScroll = BeginScroll(inner, manualScroll, textH + 20);
            Lbl(new Rect(0, 0, inner.width - 30, textH + 10), page, wrap);
            GUI.EndScrollView();
        }

        // ================= Camp (survivors) =================

        Vector2 campScroll;
        int campFilter = -1;          // -1 = all, otherwise (int)JobKind
        int jobPickerFor = -1;        // survivor id whose job list is open
        static readonly JobKind[] SurvivorJobs = { JobKind.Guard, JobKind.Builder, JobKind.Farmer, JobKind.Collector, JobKind.Cook, JobKind.WaterCarrier, JobKind.Rest };

        void DrawCamp(GameManager gm)
        {
            Rect r = Window(1120, 780, $"Camp   -   survivors {gm.Survivors.Count}/{gm.Beds} beds");
            float x = r.x + 26, w = r.width - 52, y = r.y + 60;
            bool low = gm.FoodPoints < gm.FoodNeededPerDay;
            Lbl(new Rect(x, y, w - 320, 28), $"Food in storage: <b>{gm.FoodPoints}</b>   -   needed every morning: <b>{gm.FoodNeededPerDay}</b>" +
                (low ? $"   <color=#{Hex(Palette.UiRed)}>not enough! grow crops / cook meals</color>" : ""), small);
            if (gm.BunkLevel < GameData.BunkUpgradeCost.Length)
            {
                int cost = GameData.BunkUpgradeCost[gm.BunkLevel];
                if (Btn(new Rect(r.xMax - 330, y - 4, 300, 40), $"More beds (+{GameData.BedsPerLevel}) - {cost}"))
                    Confirm("Upgrade the Bunkhouse?", $"Pay {cost} coins for {GameData.BedsPerLevel} more beds, so more survivors can join.", "Upgrade", gm.UpgradeBunkhouse);
            }
            y += 44;
            // Guard weapons (all guards use the current one)
            {
                var cur = GameData.GuardWeapons[gm.GuardWeaponLevel];
                Row(new Rect(x, y, w, 50));
                Lbl(new Rect(x + 14, y, w - 340, 50), $"<b>Guard weapon:</b> {cur.Name}  <color=#AAAAAA>({cur.Damage} damage per hit, {cur.Cooldown:0.0}s between hits)</color>", small);
                if (gm.GuardWeaponLevel < GameData.GuardWeapons.Length - 1)
                {
                    var next = GameData.GuardWeapons[gm.GuardWeaponLevel + 1];
                    if (Btn(new Rect(x + w - 320, y + 6, 310, 38), $"Upgrade to {next.Name} - {next.Cost}"))
                        Confirm($"Upgrade to the {next.Name}?", $"Pay {next.Cost} coins. Every guard (hired and survivors) gets a {next.Name}: {next.Damage} damage per hit.", "Upgrade", gm.UpgradeGuardWeapons);
                }
                else Lbl(new Rect(x + w - 320, y, 310, 50), $"<color=#{Hex(Palette.UiGreen)}>Best weapon</color>", small);
            }
            y += 58;

            if (gm.Survivors.Count == 0)
            {
                Lbl(new Rect(x, y + 10, w, 60), "No survivors yet. Now and then someone walks up the road - click <b>Accept</b> on their card to let them join.", wrap);
                return;
            }

            // Filter: All + one button per job, with how many survivors do it
            var people = gm.Survivors.Where(s => s).OrderBy(s => s.PersonName).ToList();
            Lbl(new Rect(x, y + 6, 80, 30), "Show:", small);
            float fx = x + 70;
            int filters = SurvivorJobs.Length + 1;
            float fw = (w - 70f) / filters;
            for (int i = -1; i < SurvivorJobs.Length; i++)
            {
                int count = i < 0 ? people.Count : people.Count(s => s.AssignedJob == SurvivorJobs[i]);
                string name = i < 0 ? "All" : GameData.JobName(SurvivorJobs[i]);
                int value = i < 0 ? -1 : (int)SurvivorJobs[i];
                if (Tgl(new Rect(fx + (i + 1) * fw, y, fw - 6, 40), campFilter == value, $"{name} ({count})") && campFilter != value)
                {
                    campFilter = value;
                    campScroll = Vector2.zero;
                    FX.Play(Sfx.Click, 0.2f);
                }
            }
            y += 52;

            var shown = campFilter < 0 ? people : people.Where(s => (int)s.AssignedJob == campFilter).ToList();
            string heading = campFilter < 0 ? $"All survivors ({shown.Count})" : $"{GameData.JobName((JobKind)campFilter)}: {shown.Count}";
            if (shown.Count > 0) heading += "  -  " + string.Join(", ", shown.Select(s => s.PersonName));
            Lbl(new Rect(x, y, w, 28), $"<b>{heading}</b>", small);
            y += 34;

            const float rowH = 104f, pickerH = 56f;
            float contentH = 0f;
            foreach (var s in shown) contentH += rowH + (jobPickerFor == s.Id ? pickerH : 0f);
            Rect list = new Rect(x, y, w, r.yMax - y - 20);
            campScroll = BeginScroll(list, campScroll, contentH);
            float ry = 0, rw = list.width - 26;
            foreach (var s in shown)
            {
                bool open = jobPickerFor == s.Id;
                Row(new Rect(0, ry, rw, rowH - 8 + (open ? pickerH : 0f)));
                Lbl(new Rect(14, ry + 6, 360, 28), $"<b>{s.PersonName}</b>   <color=#AAAAAA>skill: {GameData.JobName(s.Skill)}</color>", label);
                Lbl(new Rect(14, ry + 36, 380, 24), s.Status, small);
                Lbl(new Rect(14, ry + 64, 70, 22), "Health", tiny);
                Bar(new Rect(80, ry + 70, 110, 9), s.Health / 100f, s.Injured ? Palette.UiRed : Palette.UiGreen);
                Lbl(new Rect(200, ry + 64, 70, 22), "Morale", tiny);
                Bar(new Rect(266, ry + 70, 110, 9), s.Morale / 100f, s.Morale < 35f ? Palette.UiRed : Palette.UiGold);

                var survivor = s;
                string jobText = (open ? "Close list" : "Job: " + GameData.JobName(s.AssignedJob)) + (s.AssignedJob == s.Skill && !open ? "  *" : "");
                if (Btn(new Rect(rw - 660, ry + 10, 240, 38), jobText))
                {
                    jobPickerFor = open ? -1 : s.Id;
                    FX.Play(Sfx.Click, 0.2f);
                }
                if (s.AssignedJob == JobKind.Collector || s.AssignedJob == JobKind.Guard || s.AssignedJob == JobKind.WaterCarrier)
                {
                    if (Btn(new Rect(rw - 410, ry + 10, 250, 38), "Works in: " + s.AreaName))
                    {
                        survivor.AssignedField = NextField(gm, survivor.AssignedField);
                        FX.Play(Sfx.Click, 0.2f);
                    }
                }
                if (Btn(new Rect(rw - 150, ry + 10, 136, 38), "Dismiss"))
                    Confirm($"Send {s.PersonName} away?", "They leave the farm for good.", "Send away", () => gm.DismissSurvivor(survivor), true);

                if (open)
                {
                    // The job list: pick one
                    float bw = (rw - 28f) / SurvivorJobs.Length;
                    for (int i = 0; i < SurvivorJobs.Length; i++)
                    {
                        var job = SurvivorJobs[i];
                        string label2 = GameData.JobName(job) + (job == s.Skill ? " *" : "");
                        if (Tgl(new Rect(14 + i * bw, ry + rowH - 2, bw - 6, 42), s.AssignedJob == job, label2) && s.AssignedJob != job)
                        {
                            survivor.SetJob(job);
                            jobPickerFor = -1;
                            FX.Play(Sfx.Click, 0.25f);
                        }
                    }
                }
                ry += rowH + (open ? pickerH : 0f);
            }
            GUI.EndScrollView();
            Lbl(new Rect(x, r.yMax - 22, w, 20), "<color=#AAAAAA>*  = the survivor's skill (40% faster at it)</color>", tiny);
        }

        // ================= Crop Field window =================

        void DrawSeeds(GameManager gm)
        {
            Rect r = Window(900, 640, "Crop Field");
            float x = r.x + 26, w = r.width - 52, y = r.y + 62;
            int empty = gm.EmptyCropPlots, thirsty = gm.ThirstyCropPlots;
            Lbl(new Rect(x, y, w, 28), $"Empty plots: <b>{empty}</b>   -   need water: <b>{thirsty}</b>   -   your bucket: <b>{gm.Water}/{gm.WaterMax}</b>", label);
            y += 38;
            foreach (var d in GameData.Crops.Values)
            {
                Row(new Rect(x, y, w, 78));
                int have = gm.Seeds[(int)d.Kind];
                Lbl(new Rect(x + 14, y + 6, w - 520, 32), $"<b>{d.Name}</b>   <color=#AAAAAA>seeds: <b>{have}</b></color>", label);
                Lbl(new Rect(x + 14, y + 42, w - 520, 26), $"grows {d.GrowTime:0}s  -  harvest {d.Yield}  -  {GameData.FoodValue[d.Produces]} food each", small);
                var def = d;
                if (gm.Rank < d.UnlockRank)
                    Lbl(new Rect(x + w - 300, y + 24, 290, 30), $"<color=#AAAAAA>Unlocks at Rank {d.UnlockRank}</color>", small);
                else
                {
                    GUI.enabled = confirm == null && have > 0 && empty > 0;
                    if (Btn(new Rect(x + w - 500, y + 16, 300, 46), have > 0 ? $"Plant all ({Mathf.Min(have, empty)} plots)" : "No seeds"))
                        gm.PlantAll(def.Kind);
                    GUI.enabled = confirm == null;
                    if (Btn(new Rect(x + w - 190, y + 16, 176, 46), $"Buy 5 - {d.SeedCost * 5}"))
                        Confirm($"Buy 5 {def.Name} seeds?", $"Pay {def.SeedCost * 5} coins for 5 {def.Name} seeds.", "Buy", () => gm.BuySeeds(def.Kind, 5));
                }
                y += 86;
            }
            y += 4;
            GUI.enabled = confirm == null && thirsty > 0;
            if (Btn(new Rect(x, y, 400, 52), thirsty > 0 ? $"Water all ({Mathf.Min(thirsty, gm.Water)} plots)" : "Nothing needs water", bigButton)) gm.WaterAll();
            GUI.enabled = confirm == null;
            Lbl(new Rect(x + 420, y, w - 420, 52), "Planted crops only grow after they are watered. Ripe crops: click a plot to harvest.", wrap);
            y += 66;
            Lbl(new Rect(x, y + 6, 300, 30), "Farmer survivors plant:", label);
            if (Btn(new Rect(x + 300, y, 260, 42), GameData.Crops[gm.FarmerCrop].Name))
            {
                var kinds = (CropKind[])System.Enum.GetValues(typeof(CropKind));
                int i = System.Array.IndexOf(kinds, gm.FarmerCrop);
                for (int k = 1; k <= kinds.Length; k++)
                {
                    var next = kinds[(i + k) % kinds.Length];
                    if (gm.Rank >= GameData.Crops[next].UnlockRank) { gm.FarmerCrop = next; break; }
                }
                FX.Play(Sfx.Click, 0.2f);
            }
        }

        // ================= Pause / settings =================

        void DrawPause(GameManager gm)
        {
            Rect r = Window(560, Device.IsWeb ? 640 : 700, "Paused");
            float x = r.x + 40, w = r.width - 80, y = r.y + 70;
            if (Btn(new Rect(x, y, w, 56), "Resume", bigButton)) Open = Panel.None;
            y += 76;

            Lbl(new Rect(x, y, w, 30), "<b>Settings</b>", label);
            y += 36;
            Lbl(new Rect(x, y, 140, 34), "Volume", label);
            float v = GUI.HorizontalSlider(P(new Rect(x + 150, y + 12, w - 220, 20)), Settings.Volume, 0f, 1f);
            if (Mathf.Abs(v - Settings.Volume) > 0.001f) Settings.SetVolume(v);
            Lbl(new Rect(x + w - 60, y, 60, 34), $"{Settings.Volume * 100f:0}%", labelR);
            y += 46;

            Lbl(new Rect(x, y, 140, 40), "Graphics", label);
            if (Tgl(new Rect(x + 150, y, (w - 160) / 2f, 40), !Settings.HighQuality, "Low (faster)") && Settings.HighQuality) Settings.SetHighQuality(false);
            if (Tgl(new Rect(x + 160 + (w - 160) / 2f, y, (w - 160) / 2f, 40), Settings.HighQuality, "High") && !Settings.HighQuality) Settings.SetHighQuality(true);
            y += 52;

            Lbl(new Rect(x, y, 140, 40), "Confirm buys", label);
            if (Tgl(new Rect(x + 150, y, (w - 160) / 2f, 40), Settings.AskConfirm, "Ask first") && !Settings.AskConfirm) Settings.SetAskConfirm(true);
            if (Tgl(new Rect(x + 160 + (w - 160) / 2f, y, (w - 160) / 2f, 40), !Settings.AskConfirm, "Don't ask") && Settings.AskConfirm) Settings.SetAskConfirm(false);
            y += 52;

            Lbl(new Rect(x, y, 140, 40), "Controls", label);
            float cw = (w - 150 - 16) / 3f;
            string[] modes = { "Auto", "Touch", "Mouse" };
            for (int i = 0; i < 3; i++)
                if (Tgl(new Rect(x + 150 + i * (cw + 8), y, cw, 40), Device.TouchMode == i, modes[i]) && Device.TouchMode != i) { Device.SetTouchMode(i); FX.Play(Sfx.Click, 0.2f); }
            y += 62;

            float half = Device.IsWeb ? (w - 10) / 2f : w;
            if (Btn(new Rect(x, y, half, 50), Tutorial.Active ? "Tutorial is running" : "Replay tutorial"))
            {
                if (!Tutorial.Active) { Tutorial.Begin(); Open = Panel.None; }
            }
            if (Device.IsWeb && Btn(new Rect(x + half + 10, y, half, 50), Screen.fullScreen ? "Exit full screen" : "Full screen"))
                Screen.fullScreen = !Screen.fullScreen;
            y += 62;
            if (Btn(new Rect(x, y, w, 50), "Save & quit to title"))
                Confirm("Quit to the title screen?", "Your farm will be saved.", "Save & quit", () => gm.GoToTitle(), true);
            y += 62;
            if (!Device.IsWeb && Btn(new Rect(x, y, w, 50), "Quit game"))
                Confirm("Quit Hay Hustle?", "Your farm will be saved.", "Quit", () => { gm.SaveGame(); Application.Quit(); }, true);
        }
    }
}
