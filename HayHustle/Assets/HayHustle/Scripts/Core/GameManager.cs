using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HayHustle
{
    public enum GameMode { Title, CharacterSelect, Playing }

    public class Note
    {
        public string Text;
        public Color Color;
        public float Until;
    }

    /// <summary>
    /// Central game state: economy, farm rank, fields, animals, workers, orders, saving.
    /// It creates itself automatically when you press Play in any scene.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            new GameObject("Hay Hustle").AddComponent<GameManager>();
        }

        /// <summary>Middle of the farm. Predators come from the forest around it.</summary>
        public static readonly Vector3 PastureCenter = new Vector3(0f, 0f, -7f);

        // ---- State ----
        public GameMode Mode { get; private set; } = GameMode.Title;
        public string FarmName = "My Farm";
        public CharacterChoice Character;
        public int Coins, Water = 10, WaterMax = 10, Xp;
        public int WarehouseCapacity = 40;
        public int[] Inventory = new int[GameData.ProductCount];
        public int PredatorsCaught, AnimalsLost;
        public string LastRewardDate = "";
        public int RewardStreak;
        public Product WantedProduct = Product.Egg;
        public int Mess;                       // spoiled food lying around - attracts rats
        public int FenceLevel, Traps, Lamps;
        public int BunkLevel = 1, BarnLevel;
        public float Wood;              // material for gate repairs (chop trees)
        public int GuardWeaponLevel;    // see GameData.GuardWeapons
        public CropKind FarmerCrop = CropKind.Wheat;
        /// <summary>Seeds bought in the Shop, per crop (does not use storage space).</summary>
        public int[] Seeds = new int[System.Enum.GetValues(typeof(CropKind)).Length];
        int nextSurvivorId = 1;
        float arrivalTimer = 150f, visitorTimeout;
        public bool TutorialDone;

        public int Rank => GameData.RankFromXp(Xp);
        public int AnimalCapacity => Fields.Where(f => f.Owned).Sum(f => f.Def.Capacity);
        public int StorageCapacity => WarehouseCapacity + BarnLevel * GameData.BarnStoragePerLevel;
        public int Beds => BunkLevel * GameData.BedsPerLevel;

        /// <summary>Food points in storage (see GameData.FoodValue).</summary>
        public int FoodPoints
        {
            get { int f = 0; foreach (var kv in GameData.FoodValue) f += Inventory[(int)kv.Key] * kv.Value; return f; }
        }
        public int FoodNeededPerDay => Survivors.Count * GameData.FoodPerSurvivor;

        // ---- World ----
        public readonly List<FarmField> Fields = new List<FarmField>();
        public readonly List<Animal> Animals = new List<Animal>();
        public readonly List<Predator> Predators = new List<Predator>();
        public readonly List<Helper> Helpers = new List<Helper>();
        public readonly List<Worker> Workers = new List<Worker>();
        public readonly List<ProcessingBuilding> Buildings = new List<ProcessingBuilding>();
        public readonly List<Trap> TrapObjects = new List<Trap>();
        public readonly List<Lamp> LampObjects = new List<Lamp>();
        public readonly List<Survivor> Survivors = new List<Survivor>();
        public readonly List<FieldGate> Gates = new List<FieldGate>();
        public readonly List<CropPlot> CropPlots = new List<CropPlot>();
        public readonly List<ChoppableTree> Trees = new List<ChoppableTree>();
        public Survivor Visitor { get; private set; }
        public FieldGate FarmGate;   // the one gate in the wall around the whole farm
        public Vector3 CropFieldCenter, BunkhouseDoor;
        public readonly List<GrassPatch> Grass = new List<GrassPatch>();
        public readonly List<Note> Notes = new List<Note>();
        public readonly OrderBook Orders = new OrderBook();

        public Vector3 WarehouseDoor;          // where rats go to steal
        public Vector3 WellPosition;
        public Transform WarehouseTransform, OrderBoardTransform;
        public PlayerController Player { get; private set; }
        public DayNightCycle DayNight { get; private set; }
        public PredatorSpawner Spawner { get; private set; }
        public Truck Truck;
        public CameraFollow CameraRig;

        float autosave = 30f;
        bool wasNight;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            Application.targetFrameRate = 60;

            gameObject.AddComponent<FX>();
            WorldBuilder.Build(this);
            DayNight = gameObject.AddComponent<DayNightCycle>();
            DayNight.Sun = WorldBuilder.Sun;
            DayNight.OnNewDay += OnNewDay;
            Spawner = gameObject.AddComponent<PredatorSpawner>();
            gameObject.AddComponent<HUD>();
            gameObject.AddComponent<TouchControls>();
            Device.Load();
            Settings.Load();

            Time.timeScale = 0f; // the world is paused on the menus
            GoToTitle(false);   // front screen: sign in / sign up, then Continue or New Farm
            if (Backend.HasSavedSession)
                Backend.Resume((ok, err) => { if (!ok && !string.IsNullOrEmpty(err)) Notify(err, Palette.UiRed, 6f); });
        }

        void Update()
        {
            Notes.RemoveAll(n => n.Until < Time.unscaledTime);
            CloudSave.Tick();
            if (Mode != GameMode.Playing) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            bool night = DayNight.IsNight;
            if (night != wasNight)
            {
                wasNight = night;
                Notify(night ? "Night has fallen - predators are bolder in the dark!" : "Morning! The sun is up on the farm.",
                    night ? Palette.UiRed : Palette.UiGold, 5f);
            }

            if (!Tutorial.HoldsPredators)
            {
                Orders.Tick(this, dt);
                TickArrivals(dt);
            }

            autosave -= dt;
            if (autosave <= 0f) { autosave = 30f; SaveGame(); }
        }

        void OnApplicationQuit() { SaveGame(); CloudSave.Flush(); }
        void OnApplicationPause(bool paused) { if (paused) { SaveGame(); CloudSave.Flush(); } }
        // In a browser this happens when you switch tabs/apps or lock the phone.
        void OnApplicationFocus(bool focus) { if (!focus) { SaveGame(); CloudSave.Flush(); } }

        void OnNewDay()
        {
            Notify($"Day {DayNight.Day} begins on {FarmName}.", Palette.UiGold, 5f);
            RerollWanted();
            PayWages();
            FeedSurvivors();
        }

        // ================= Screens =================

        public void GoToTitle(bool save = true)
        {
            if (save) { SaveGame(); CloudSave.Flush(); }
            ClearWorld();
            Tutorial.Stop();
            Mode = GameMode.Title;
            Time.timeScale = 0f;
            WorldBuilder.ShowStage(false);
            CameraRig.Target = null;
            CameraRig.ClearShowcase();
            if (HUD.I) HUD.I.Open = HUD.Panel.None;
        }

        public void EnterCharacterSelect()
        {
            Mode = GameMode.CharacterSelect;
            Time.timeScale = 0f;
            WorldBuilder.ShowStage(true);
            CameraRig.Target = null;
            CameraRig.Showcase(WorldBuilder.StageCameraPos, WorldBuilder.StageLookAt);
        }

        // ================= Starting / loading =================

        public void StartNewGame(CharacterChoice choice, string farmName)
        {
            SaveSystem.Backup(); // keep the old farm, just in case
            ClearWorld();
            WorldBuilder.ShowStage(false);
            FarmName = string.IsNullOrWhiteSpace(farmName) ? "My Farm" : farmName.Trim();
            Character = choice;
            Coins = GameData.StartCoins;
            Water = WaterMax = 10;
            Xp = 0;
            WarehouseCapacity = 40;
            System.Array.Clear(Inventory, 0, Inventory.Length);
            PredatorsCaught = AnimalsLost = 0;
            LastRewardDate = "";
            RewardStreak = 0;
            Mess = 0;
            FenceLevel = 0;
            Traps = 0;
            Lamps = 0;
            BunkLevel = 1;
            BarnLevel = 0;
            Wood = 10f;
            GuardWeaponLevel = 0;
            FarmerCrop = CropKind.Wheat;
            System.Array.Clear(Seeds, 0, Seeds.Length);
            Seeds[(int)CropKind.Wheat] = 8; // a starter pack
            nextSurvivorId = 1;
            arrivalTimer = 150f;
            foreach (var p in CropPlots) if (p) p.SetState(-1, 0f, false);
            foreach (var g in Gates) if (g) g.SetHp(GameData.GateMaxHp);
            TutorialDone = false;
            DayNight.TimeOfDay = 0.3f;
            DayNight.Day = 1;

            WorldBuilder.ResetFields(this);
            WorldBuilder.ApplyFenceLevel(this);
            var home = Fields[0].Grass;
            for (int i = 0; i < home.Count; i++) home[i].SetAmount(i % 3 == 0 ? 1f : 0f);
            for (int i = 0; i < 3; i++) SpawnAnimal(AnimalKind.Chicken, Stage.Adult, Fields[0].RandomPoint(), false, 0);

            RerollWanted();
            BeginPlay();
            Tutorial.Begin();
        }

        public void ContinueGame()
        {
            SaveSystem.Backup(); // a copy from the start of every play session
            var d = SaveSystem.Load();
            if (d == null) { EnterCharacterSelect(); return; }

            ClearWorld();
            FarmName = d.farmName;
            Character = (CharacterChoice)d.character;
            Coins = d.coins;
            WaterMax = Mathf.Max(10, d.waterMax);
            Water = Mathf.Clamp(d.water, 0, WaterMax);
            Xp = d.xp;
            WarehouseCapacity = Mathf.Max(40, d.warehouseCapacity);
            System.Array.Clear(Inventory, 0, Inventory.Length);
            if (d.inventory != null)
                for (int i = 0; i < Mathf.Min(Inventory.Length, d.inventory.Length); i++) Inventory[i] = d.inventory[i];

            // Fields (older saves only had the home pasture in 'grass')
            WorldBuilder.ResetFields(this);
            if (d.fields != null && d.fields.Count > 0)
            {
                foreach (var fs in d.fields)
                {
                    if (fs.index < 0 || fs.index >= Fields.Count) continue;
                    var f = Fields[fs.index];
                    if (fs.owned && !f.Owned) WorldBuilder.BuildField(this, f);
                    if (fs.grass != null)
                        for (int i = 0; i < Mathf.Min(f.Grass.Count, fs.grass.Length); i++) f.Grass[i].SetAmount(fs.grass[i]);
                }
            }
            else if (d.grass != null)
            {
                var home = Fields[0].Grass;
                for (int i = 0; i < Mathf.Min(home.Count, d.grass.Length); i++) home[i].SetAmount(d.grass[i]);
            }

            foreach (var s in d.animals)
            {
                // Each kind lives in its own field. Older saves kept everything in the first field.
                var home = FieldFor((AnimalKind)s.kind);
                int field = home != null && home.Owned ? home.Index
                    : s.field >= 0 && s.field < Fields.Count && Fields[s.field].Owned ? s.field : 0;
                var a = SpawnAnimal((AnimalKind)s.kind, (Stage)s.stage, Fields[field].Clamp(new Vector3(s.x, 0f, s.z)), s.rare, field);
                a.Age = s.age;
                a.Hunger = s.hunger;
                a.WellFedTime = s.wellFed;
            }
            DayNight.TimeOfDay = d.timeOfDay;
            DayNight.Day = Mathf.Max(1, d.day);
            PredatorsCaught = d.predatorsCaught;
            AnimalsLost = d.animalsLost;
            LastRewardDate = d.lastRewardDate ?? "";
            RewardStreak = d.rewardStreak;
            WantedProduct = (Product)Mathf.Clamp(d.wanted, 0, GameData.ProductCount - 1);
            Mess = d.mess;
            FenceLevel = Mathf.Clamp(d.fenceLevel, 0, GameData.FenceSlow.Length - 1);
            WorldBuilder.ApplyFenceLevel(this);
            Traps = 0;
            for (int i = 0; i < Mathf.Min(d.traps, GameData.MaxTraps); i++) { Traps++; PlaceTrap(); }
            Lamps = 0;
            if (d.lampXZ != null && d.lampXZ.Count >= 2)
                for (int i = 0; i + 1 < d.lampXZ.Count && Lamps < GameData.MaxLamps; i += 2) { Lamps++; SpawnLamp(new Vector3(d.lampXZ[i], 0f, d.lampXZ[i + 1])); }
            else
                for (int i = 0; i < Mathf.Min(d.lamps, GameData.MaxLamps); i++) { Lamps++; PlaceLamp(); }   // older saves
            for (int i = 0; i < d.cats; i++) SpawnHelper(HelperKind.Cat);
            for (int i = 0; i < d.dogs; i++) SpawnHelper(HelperKind.Dog);
            if (d.workers != null)
                for (int i = 0; i < d.workers.Count; i++)
                {
                    var w = SpawnWorker((WorkerKind)d.workers[i]);
                    if (d.workerFields != null && i < d.workerFields.Count) w.AssignedField = d.workerFields[i];
                }
            TutorialDone = d.tutorialDone || d.version < 2;

            // Survival
            BunkLevel = Mathf.Clamp(d.bunkLevel, 1, GameData.BunkUpgradeCost.Length);
            BarnLevel = Mathf.Clamp(d.barnLevel, 0, GameData.BarnUpgradeCost.Length);
            FarmerCrop = (CropKind)Mathf.Clamp(d.farmerCrop, 0, System.Enum.GetValues(typeof(CropKind)).Length - 1);
            nextSurvivorId = Mathf.Max(1, d.nextSurvivorId);
            Wood = d.wood < 0f ? 10f : d.wood;
            GuardWeaponLevel = Mathf.Clamp(d.guardWeapon, 0, GameData.GuardWeapons.Length - 1);
            System.Array.Clear(Seeds, 0, Seeds.Length);
            if (d.seeds != null) for (int i = 0; i < Mathf.Min(Seeds.Length, d.seeds.Length); i++) Seeds[i] = d.seeds[i];
            else Seeds[(int)CropKind.Wheat] = 8; // older saves get a starter pack
            if (FarmGate) FarmGate.SetHp(GameData.GateMaxHp - d.farmGateDamage);
            for (int i = 0; i < CropPlots.Count; i++)
            {
                var c = d.crops != null && i < d.crops.Count ? d.crops[i] : null;
                if (c != null) CropPlots[i].SetState(c.crop, c.growth, c.watered);
                else CropPlots[i].SetState(-1, 0f, false);
            }
            if (d.survivors != null)
                foreach (var ss in d.survivors)
                {
                    var s = SpawnSurvivor(ss.id, ss.name, (JobKind)ss.skill, BunkhouseDoor + new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(1f, 3f)));
                    s.Join((JobKind)ss.job);
                    s.AssignedField = ss.field;
                    s.Health = Mathf.Clamp(ss.health, 1f, 100f);
                    s.Morale = Mathf.Clamp(ss.morale, 1f, 100f);
                }
            BeginPlay();
        }

        void BeginPlay()
        {
            if (Player) Destroy(Player.gameObject);
            Player = WorldBuilder.SpawnPlayer(Character);
            CameraRig.ClearShowcase();
            CameraRig.Target = Player.transform;
            CameraRig.Yaw = 180f;

            Mode = GameMode.Playing;
            Time.timeScale = 1f;
            wasNight = DayNight.IsNight;
            Spawner.ResetTimer();
            Orders.Clear();
            if (DailyRewards.CanClaim(this) && TutorialDone) HUD.I.Open = HUD.Panel.Rewards;
            Notify($"Welcome to {FarmName}!", Palette.UiGold, 5f);
            SaveGame();
        }

        void ClearWorld()
        {
            foreach (var a in Animals.ToArray()) if (a) Destroy(a.gameObject);
            Animals.Clear();
            foreach (var p in Predators.ToArray()) if (p) Destroy(p.gameObject);
            Predators.Clear();
            foreach (var h in Helpers.ToArray()) if (h) Destroy(h.gameObject);
            Helpers.Clear();
            foreach (var w in Workers.ToArray()) if (w) Destroy(w.gameObject);
            Workers.Clear();
            foreach (var t in TrapObjects.ToArray()) if (t) Destroy(t.gameObject);
            TrapObjects.Clear();
            foreach (var l in LampObjects.ToArray()) if (l) Destroy(l.gameObject);
            LampObjects.Clear();
            foreach (var s in Survivors.ToArray()) if (s) Destroy(s.gameObject);
            Survivors.Clear();
            if (Visitor) Destroy(Visitor.gameObject);
            Visitor = null;
            foreach (var pk in ProductPickup.All.ToArray()) if (pk) Destroy(pk.gameObject);
            if (Player) Destroy(Player.gameObject);
            Player = null;
            Spawner.ResetTimer();
            Orders.Clear();
            if (Truck) Truck.ResetTruck();
        }

        public void SaveGame()
        {
            if (Mode != GameMode.Playing) return;
            var d = new SaveData
            {
                version = 4,
                savedAt = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ownerId = Backend.SignedIn ? Backend.UserId : "",
                farmName = FarmName,
                character = (int)Character,
                coins = Coins + (Truck ? Truck.PendingPayment : 0),
                water = Water,
                waterMax = WaterMax,
                xp = Xp,
                warehouseCapacity = WarehouseCapacity,
                timeOfDay = DayNight.TimeOfDay,
                day = DayNight.Day,
                predatorsCaught = PredatorsCaught,
                animalsLost = AnimalsLost,
                lastRewardDate = LastRewardDate,
                rewardStreak = RewardStreak,
                wanted = (int)WantedProduct,
                mess = Mess,
                cats = CountHelpers(HelperKind.Cat),
                dogs = CountHelpers(HelperKind.Dog),
                fenceLevel = FenceLevel,
                traps = Traps,
                lamps = Lamps,
                lampXZ = LampObjects.Where(l => l).SelectMany(l => new[] { l.transform.position.x, l.transform.position.z }).ToList(),
                bunkLevel = BunkLevel,
                barnLevel = BarnLevel,
                farmerCrop = (int)FarmerCrop,
                nextSurvivorId = nextSurvivorId,
                farmGateDamage = FarmGate ? GameData.GateMaxHp - FarmGate.Hp : 0f,
                seeds = (int[])Seeds.Clone(),
                wood = Wood,
                guardWeapon = GuardWeaponLevel,
                tutorialDone = TutorialDone || !Tutorial.Active,
                inventory = (int[])Inventory.Clone(),
            };
            if (Truck) for (int i = 0; i < d.inventory.Length; i++) d.inventory[i] += Truck.Cargo[i]; // unsent cargo goes back
            foreach (var f in Fields)
            {
                var fs = new FieldSave { index = f.Index, owned = f.Owned, grass = new float[f.Grass.Count] };
                for (int i = 0; i < f.Grass.Count; i++) fs.grass[i] = f.Grass[i].Amount;
                d.fields.Add(fs);
            }
            foreach (var s in Survivors)
                if (s) d.survivors.Add(new SurvivorSave { id = s.Id, name = s.PersonName, skill = (int)s.Skill, job = (int)s.AssignedJob, field = s.AssignedField, health = s.Health, morale = s.Morale });
            foreach (var p in CropPlots)
                d.crops.Add(p ? new CropSave { crop = p.Crop.HasValue ? (int)p.Crop.Value : -1, growth = p.Growth, watered = p.Watered } : new CropSave());
            foreach (var w in Workers)
            {
                if (!w) continue;
                d.workers.Add((int)w.Kind);
                d.workerFields.Add(w.AssignedField);
            }
            foreach (var a in Animals)
            {
                if (!a) continue;
                var p = a.transform.position;
                d.animals.Add(new AnimalSave
                {
                    kind = (int)a.Kind, stage = (int)a.Stage, age = a.Age, hunger = a.Hunger,
                    wellFed = a.WellFedTime, rare = a.Rare, x = p.x, z = p.z, field = a.Field
                });
            }
            SaveSystem.Save(d);
            CloudSave.Queue(JsonUtility.ToJson(d));
        }

        // ================= Economy =================

        public bool Spend(int amount)
        {
            if (Coins < amount) { Notify("Not enough coins!", Palette.UiRed); FX.Play(Sfx.Error, 0.4f); return false; }
            Coins -= amount;
            return true;
        }

        public void AddCoins(int amount) => Coins += amount;

        public int InventoryTotal
        {
            get { int t = 0; foreach (var n in Inventory) t += n; return t; }
        }

        public bool AddProduct(Product p, int n = 1)
        {
            if (InventoryTotal + n > StorageCapacity) return false;
            Inventory[(int)p] += n;
            return true;
        }

        public bool HasProduct(Product p, int n) => Inventory[(int)p] >= n;
        public void RemoveProduct(Product p, int n) => Inventory[(int)p] = Mathf.Max(0, Inventory[(int)p] - n);
        public int ProductPrice(Product p) => GameData.Products[p].Price * (p == WantedProduct ? 2 : 1);
        public int QuickSellPrice(Product p) => Mathf.Max(1, Mathf.RoundToInt(ProductPrice(p) * GameData.QuickSellRate));

        /// <summary>Instant sale from the warehouse at 70% of the truck price.</summary>
        public void QuickSell(Product p, int n)
        {
            n = Mathf.Min(n, Inventory[(int)p]);
            if (n <= 0) return;
            int coins = n * QuickSellPrice(p);
            RemoveProduct(p, n);
            AddCoins(coins);
            AddXp(Mathf.Max(1, n / 2));
            FX.Play(Sfx.Coin, 0.6f);
            if (WarehouseTransform) FX.Text(WarehouseTransform.position + Vector3.up * 5f, $"Sold {n} {GameData.ProductName(p)}: +{coins} coins", Palette.UiGold, 1.1f);
            Notify($"Quick sold {n} {GameData.ProductName(p)} for {coins} coins.", Palette.UiGold);
        }

        public void RerollWanted()
        {
            var options = GameData.Products.Values.Where(d => d.UnlockRank <= Rank && Obtainable(d.Product)).Select(d => d.Product).ToList();
            if (options.Count == 0) options.Add(Product.Egg);
            WantedProduct = options[Random.Range(0, options.Count)];
            Notify($"Market news: {GameData.ProductName(WantedProduct)} sells for DOUBLE today!", Palette.UiGold, 6f);
        }

        public void AddXp(int amount)
        {
            int before = Rank;
            Xp += amount;
            int after = Rank;
            for (int r = before + 1; r <= after; r++) AnnounceRank(r);
        }

        void AnnounceRank(int r)
        {
            Notify($"FARM RANK {r}!", Palette.UiGold, 6f);
            FX.Play(Sfx.RankUp, 0.6f);
            if (Player)
            {
                FX.Text(Player.transform.position + Vector3.up * 2.8f, $"FARM RANK {r}!", Palette.UiGold, 1.6f);
                FX.Burst(Player.transform.position + Vector3.up * 1.5f, Palette.Gold, 20, 0.12f, 4f);
                Player.PlayAnim(ActAnim.Cheer, 1.2f);
            }
            foreach (var a in GameData.Animals.Values)
                if (a.UnlockRank == r) Notify($"Unlocked: {a.Plural} in the shop", Palette.UiGreen, 6f);
            foreach (var rec in GameData.Recipes)
                if (rec.UnlockRank == r) Notify($"Unlocked: {rec.Building} ({GameData.ProductName(rec.Input)} -> {GameData.ProductName(rec.Output)})", Palette.UiGreen, 6f);
            if (r == GameData.BarnUnlockRank) Notify("Unlocked: Breeding Barn", Palette.UiGreen, 6f);
            foreach (var h in GameData.Helpers.Values)
                if (h.UnlockRank == r) Notify($"Unlocked: {h.Name} helper in the shop", Palette.UiGreen, 6f);
            foreach (var w in GameData.Workers.Values)
                if (w.UnlockRank == r) Notify($"Unlocked: hire a {w.Name} in the shop (Workers)", Palette.UiGreen, 6f);
            foreach (var f in GameData.Fields)
                if (f.UnlockRank == r && f.Price > 0) Notify($"Unlocked: buy the {f.Name} to keep {GameData.Animals[f.Kind].Plural}", Palette.UiGreen, 6f);
            foreach (var p in GameData.Predators.Values)
                if (p.MinRank == r && r > 1) Notify($"Watch out: {p.Name.ToLower()}s now hunt your farm{(p.NightOnly ? " at night" : "")}!", Palette.UiRed, 6f);
        }

        // ================= Fields =================

        public FarmField FieldAt(Vector3 pos)
        {
            foreach (var f in Fields) if (f.Owned && f.Contains(pos)) return f;
            return null;
        }

        public bool InOwnedField(Vector3 pos) => FieldAt(pos) != null;

        public int CountInField(int field) => Animals.Count(a => a && a.Field == field);

        /// <summary>The field that is home to this kind of animal.</summary>
        public FarmField FieldFor(AnimalKind kind) => Fields.FirstOrDefault(f => f.Def.Kind == kind);
        public bool HasFieldFor(AnimalKind kind) { var f = FieldFor(kind); return f != null && f.Owned; }
        public bool FieldFull(AnimalKind kind) { var f = FieldFor(kind); return f == null || CountInField(f.Index) >= f.Def.Capacity; }
        public Vector3 PointFor(AnimalKind kind) { var f = FieldFor(kind); return f != null && f.Owned ? f.RandomPoint() : Fields[0].RandomPoint(); }

        /// <summary>Can the player make this product with what they own now?</summary>
        public bool Obtainable(Product p)
        {
            var crop = GameData.Crops.Values.FirstOrDefault(c => c.Produces == p);
            if (crop != null) return Rank >= crop.UnlockRank;
            var maker = GameData.Animals.Values.FirstOrDefault(a => a.Produces == p);
            if (maker != null) return Rank >= maker.UnlockRank && HasFieldFor(maker.Kind);
            var r = GameData.Recipes.FirstOrDefault(x => x.Output == p);
            return r != null && Rank >= r.UnlockRank && Obtainable(r.Input);
        }

        /// <summary>Explains where a product comes from, and where to go (for the "Show" button).</summary>
        public string HowToGet(Product p, out Vector3? target)
        {
            target = null;
            var crop = GameData.Crops.Values.FirstOrDefault(c => c.Produces == p);
            if (crop != null)
            {
                if (Rank < crop.UnlockRank) return $"A crop - {crop.Name} seeds unlock at Farm Rank {crop.UnlockRank}.";
                target = CropFieldCenter;
                return $"Plant {crop.Name} in the Crop Field (click an empty plot, choose {crop.Name}, then water it).";
            }
            var r = GameData.Recipes.FirstOrDefault(x => x.Output == p);
            if (r != null)
            {
                string inp = $"{r.InputAmount} {GameData.ProductName(r.Input)}";
                if (Rank < r.UnlockRank) return $"Made at the {r.Building} (unlocks at Farm Rank {r.UnlockRank}) from {inp}.";
                var b = Buildings.FirstOrDefault(x => x && x.Recipe == r);
                if (b) target = b.FrontPosition;
                string need = HasProduct(r.Input, r.InputAmount) ? "" : $" First get {GameData.ProductName(r.Input)}: " + HowToGet(r.Input, out _);
                return $"Click the {r.Building} to make it from {inp}.{need}";
            }
            var maker = GameData.Animals.Values.FirstOrDefault(a => a.Produces == p);
            if (maker == null) return "";
            var field = FieldFor(maker.Kind);
            if (Rank < maker.UnlockRank) return $"Comes from {maker.Plural} (unlock at Farm Rank {maker.UnlockRank}).";
            if (!field.Owned)
            {
                if (field.Sign) target = field.Sign.transform.position;
                return $"Comes from {maker.Plural}. Buy the {field.Def.Name} ({field.Def.Price} coins), then buy {maker.Plural} in the Shop.";
            }
            target = field.Center;
            if (CountAnimals(maker.Kind) == 0) return $"Buy {maker.Plural} in the Shop - they live in the {field.Def.Name} and make {GameData.ProductName(p)}.";
            return $"Your {maker.Plural} in the {field.Def.Name} make it. Keep them fed and click the {GameData.ProductName(p)} to collect.";
        }

        public void BuyField(int index)
        {
            var f = Fields[index];
            if (f.Owned || Rank < f.Def.UnlockRank) return;
            if (!Spend(f.Def.Price)) return;
            WorldBuilder.BuildField(this, f);
            WorldBuilder.ApplyFenceLevel(this);
            foreach (var g in f.Grass) g.SetAmount(0.6f);
            AddXp(25);
            Notify($"You bought the {f.Def.Name}! Now you can keep up to {f.Def.Capacity} {GameData.Animals[f.Def.Kind].Plural} (Shop > Animals).", Palette.UiGold, 6f);
            FX.Play(Sfx.RankUp, 0.6f);
            FX.Text(f.Gate + Vector3.up * 2.5f, $"{f.Def.Name} is yours!", Palette.UiGold, 1.4f);
            FX.Burst(f.Gate + Vector3.up * 1f, Palette.Gold, 24, 0.14f, 5f);
        }

        // ================= Fences & traps =================

        public void UpgradeFence()
        {
            if (FenceLevel >= GameData.FenceUpgradeCost.Length) return;
            if (!Spend(GameData.FenceUpgradeCost[FenceLevel])) return;
            FenceLevel++;
            WorldBuilder.ApplyFenceLevel(this);
            AddXp(15);
            Notify($"Fences upgraded to {GameData.FenceNames[FenceLevel]}: predators are {GameData.FenceSlow[FenceLevel] * 100f:0}% slower inside your fields.", Palette.UiGreen, 6f);
            FX.Play(Sfx.RankUp, 0.5f);
        }

        public void BuyTrap()
        {
            if (Traps >= GameData.MaxTraps) { Notify($"You already have {GameData.MaxTraps} traps set.", Palette.UiRed); return; }
            if (!Spend(GameData.TrapPrice)) return;
            Traps++;
            var t = PlaceTrap();
            FX.Play(Sfx.Coin, 0.5f);
            FX.Text(t.position + Vector3.up * 1.5f, "Trap set!", Palette.UiGreen);
            Notify("A trap was set at the edge of your farm.", Palette.UiGreen);
        }

        Transform PlaceTrap()
        {
            Vector3 pos;
            if (Random.value < 0.3f) pos = WarehouseDoor + new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(1.5f, 3f));
            else
            {
                var owned = Fields.Where(f => f.Owned).ToList();
                var f = owned[Random.Range(0, owned.Count)];
                int side = Random.Range(0, 4);
                float along = Random.Range(-0.8f, 0.8f);
                float ox = side < 2 ? along * f.HalfX : (side == 2 ? -1f : 1f) * (f.HalfX + 2f);
                float oz = side < 2 ? (side == 0 ? -1f : 1f) * (f.HalfZ + 2f) : along * f.HalfZ;
                pos = f.Center + new Vector3(ox, 0f, oz);
            }
            var go = new GameObject("Trap");
            go.transform.position = new Vector3(pos.x, 0f, pos.z);
            go.AddComponent<Trap>();
            return go.transform;
        }

        // ================= Survivors =================

        public Survivor SpawnSurvivor(int id, string name, JobKind skill, Vector3 pos)
        {
            var go = new GameObject("Survivor " + name);
            go.transform.position = new Vector3(pos.x, 0f, pos.z);
            var s = go.AddComponent<Survivor>();
            s.Init(id, name, skill);
            return s;
        }

        void TickArrivals(float dt)
        {
            if (Visitor)
            {
                visitorTimeout -= dt;
                if (visitorTimeout <= 0f) DeclineVisitor(true);
                return;
            }
            arrivalTimer -= dt;
            if (arrivalTimer > 0f) return;
            arrivalTimer = DayNight.DayLengthSeconds * Random.Range(0.45f, 0.9f);
            if (Survivors.Count >= Beds)
            {
                Notify("A survivor walked past, but you have no free bed. Upgrade the Bunkhouse (Camp).", Palette.UiWhite, 6f);
                return;
            }
            JobKind[] skills = { JobKind.Guard, JobKind.Builder, JobKind.Farmer, JobKind.Collector, JobKind.Cook };
            string name = GameData.SurvivorNames[Random.Range(0, GameData.SurvivorNames.Length)];
            var v = SpawnSurvivor(nextSurvivorId++, name, skills[Random.Range(0, skills.Length)], new Vector3(Random.Range(-1f, 1f), 0f, 62f));
            v.ArriveAt(new Vector3(2.5f, 0f, GameData.WallMaxZ + 3.5f)); // waits outside the farm gate
            Visitor = v;
            visitorTimeout = 90f;
            Notify($"A survivor named {name} is walking up the road!", Palette.UiGold, 6f);
            FX.Play(Sfx.Warn, 0.35f);
        }

        public void AcceptVisitor()
        {
            if (!Visitor) return;
            if (Survivors.Count >= Beds) { Notify("No free bed - upgrade the Bunkhouse first.", Palette.UiRed); FX.Play(Sfx.Error, 0.4f); return; }
            var s = Visitor;
            Visitor = null;
            s.Join(s.Skill);
            AddXp(10);
            Notify($"{s.PersonName} joined your farm as a {GameData.JobName(s.Skill)}! They eat {GameData.FoodPerSurvivor} food every morning.", Palette.UiGreen, 7f);
            FX.Play(Sfx.Catch, 0.5f);
            FX.Text(s.transform.position + Vector3.up * 2.5f, "Joined!", Palette.UiGreen, 1.2f);
        }

        public void DeclineVisitor(bool timedOut = false)
        {
            if (!Visitor) return;
            var s = Visitor;
            Visitor = null;
            Notify(timedOut ? $"{s.PersonName} gave up waiting and moved on." : $"{s.PersonName} moves on down the road.", Palette.UiWhite);
            s.LeaveFarm();
        }

        public void DismissSurvivor(Survivor s)
        {
            if (!s) return;
            Notify($"{s.PersonName} leaves the farm.", Palette.UiWhite);
            s.LeaveFarm();
        }

        /// <summary>Every morning each survivor eats. Hungry survivors lose morale; at 0 morale they leave.</summary>
        void FeedSurvivors()
        {
            if (Survivors.Count == 0) return;
            Product[] order = { Product.Meal, Product.Pumpkin, Product.Corn, Product.Carrot, Product.Wheat, Product.Egg, Product.Milk, Product.GoatMilk };
            int fed = 0, hungry = 0;
            foreach (var s in Survivors.ToArray())
            {
                if (!s) continue;
                int need = GameData.FoodPerSurvivor;
                foreach (var p in order)
                    while (need > 0 && Inventory[(int)p] > 0)
                    {
                        Inventory[(int)p]--;
                        need -= GameData.FoodValue[p];
                    }
                if (need <= 0)
                {
                    fed++;
                    s.Morale = Mathf.Min(100f, s.Morale + 8f);
                }
                else
                {
                    hungry++;
                    s.Morale -= 30f;
                    if (s.Morale <= 0f)
                    {
                        Notify($"{s.PersonName} was too hungry and left the farm.", Palette.UiRed, 7f);
                        s.LeaveFarm();
                    }
                }
            }
            if (hungry > 0) Notify($"Not enough food! {hungry} survivor(s) went hungry - grow crops or cook meals.", Palette.UiRed, 7f);
            else Notify($"Breakfast: {fed} survivor(s) ate. Food left: {FoodPoints}.", Palette.UiWhite, 5f);
        }

        // ================= Seeds & planting =================

        public void BuySeeds(CropKind kind, int amount)
        {
            var d = GameData.Crops[kind];
            if (Rank < d.UnlockRank || amount <= 0) return;
            if (!Spend(d.SeedCost * amount)) return;
            Seeds[(int)kind] += amount;
            FX.Play(Sfx.Coin, 0.4f);
            Notify($"Bought {amount} {d.Name} seeds (you have {Seeds[(int)kind]}).", Palette.UiGreen);
        }

        public int EmptyCropPlots => CropPlots.Count(p => p && p.Empty);
        public int ThirstyCropPlots => CropPlots.Count(p => p && p.NeedsWater);

        /// <summary>Plants every empty plot with this seed, as far as the seed stock goes. Returns how many were planted.</summary>
        public int PlantAll(CropKind kind)
        {
            int planted = 0;
            foreach (var p in CropPlots)
            {
                if (!p || !p.Empty) continue;
                if (Seeds[(int)kind] <= 0) break;
                if (p.Plant(kind)) planted++;
            }
            var d = GameData.Crops[kind];
            if (planted > 0)
            {
                AddXp(planted);
                Notify($"Planted {planted} {d.Name}. Now water them (Water all).", Palette.UiGreen);
                if (Player) Player.PlayAnim(ActAnim.Plant, 0.6f);
            }
            else if (Seeds[(int)kind] <= 0) { Notify($"No {d.Name} seeds - buy them in Shop > Seeds.", Palette.UiRed); FX.Play(Sfx.Error, 0.4f); }
            else Notify("There are no empty plots.", Palette.UiWhite);
            return planted;
        }

        /// <summary>Waters every planted plot that needs it, using water from the bucket.</summary>
        public int WaterAll()
        {
            int watered = 0;
            foreach (var p in CropPlots)
            {
                if (!p || !p.NeedsWater) continue;
                if (Water <= 0) break;
                Water--;
                p.WaterFree();
                watered++;
            }
            if (watered > 0)
            {
                Notify($"Watered {watered} plot(s). Water left in the bucket: {Water}.", new Color(0.5f, 0.8f, 1f));
                if (Player) Player.PlayAnim(ActAnim.Fill, 0.8f);
            }
            else if (Water <= 0) { Notify("Your bucket is empty - refill it at the well.", Palette.UiRed); FX.Play(Sfx.Error, 0.4f); }
            return watered;
        }

        // ================= Guards =================

        /// <summary>Inside the wall around the farm (margin &gt; 0 also counts that far outside it).</summary>
        public static bool InsideFarm(Vector3 pos, float margin = 0f) =>
            pos.x > GameData.WallMinX - margin && pos.x < GameData.WallMaxX + margin &&
            pos.z > GameData.WallMinZ - margin && pos.z < GameData.WallMaxZ + margin;

        /// <summary>Where guards defend: the whole farm, from the middle out to 2 plots beyond the wall and its gate.</summary>
        public bool InGuardZone(Vector3 pos) => InsideFarm(pos, GameData.GuardZoneMargin);

        /// <summary>A hurt guard eats one food item from storage to heal. Returns its food points (0 = no food).</summary>
        public int EatForHealing()
        {
            Product[] order = { Product.Wheat, Product.Egg, Product.Milk, Product.GoatMilk, Product.Carrot, Product.Corn, Product.Pumpkin, Product.Meal };
            foreach (var p in order)
                if (Inventory[(int)p] > 0)
                {
                    Inventory[(int)p]--;
                    return GameData.FoodValue[p];
                }
            return 0;
        }

        public void UpgradeGuardWeapons()
        {
            if (GuardWeaponLevel >= GameData.GuardWeapons.Length - 1) return;
            var next = GameData.GuardWeapons[GuardWeaponLevel + 1];
            if (!Spend(next.Cost)) return;
            GuardWeaponLevel++;
            AddXp(20);
            foreach (var w in Workers) if (w) w.RefreshTool();
            foreach (var s in Survivors) if (s) s.RefreshTool();
            Notify($"Guards now carry a {next.Name}: {next.Damage} damage per hit.", Palette.UiGreen, 6f);
            FX.Play(Sfx.RankUp, 0.5f);
        }

        public void UpgradeBunkhouse()
        {
            if (BunkLevel >= GameData.BunkUpgradeCost.Length) return;
            if (!Spend(GameData.BunkUpgradeCost[BunkLevel])) return;
            BunkLevel++;
            AddXp(15);
            Notify($"Bunkhouse upgraded: {Beds} beds.", Palette.UiGreen, 5f);
            FX.Play(Sfx.Coin, 0.5f);
            FX.Text(BunkhouseDoor + Vector3.up * 4f, $"{Beds} beds!", Palette.UiGreen, 1.2f);
        }

        public void UpgradeBarn()
        {
            if (BarnLevel >= GameData.BarnUpgradeCost.Length) return;
            if (!Spend(GameData.BarnUpgradeCost[BarnLevel])) return;
            BarnLevel++;
            AddXp(15);
            Notify($"Barn storage upgraded: +{GameData.BarnStoragePerLevel} space (total storage {StorageCapacity}).", Palette.UiGreen, 5f);
            FX.Play(Sfx.Coin, 0.5f);
        }

        // ================= Lamp posts =================

        /// <summary>Lamp spots in order: around the Chicken Run, the yard, then around your other fields.</summary>
        List<Vector3> LampSpots()
        {
            var spots = new List<Vector3>();
            void Corners(FarmField f)
            {
                foreach (var cz in new[] { 1f, -1f })
                foreach (var cx in new[] { -1f, 1f })
                    spots.Add(f.Center + new Vector3(cx * (f.HalfX + 1.3f), 0f, cz * (f.HalfZ + 1.3f)));
            }
            Corners(Fields[0]);
            spots.Add(new Vector3(-7f, 0f, 11.5f));
            spots.Add(new Vector3(7f, 0f, 11.5f));
            foreach (var f in Fields) if (f.Index > 0 && f.Owned) Corners(f);
            return spots;
        }

        /// <summary>Shop button: the player then clicks where the lamp goes (paid when it is placed).</summary>
        public void BuyLamp()
        {
            if (Lamps >= GameData.MaxLamps) { Notify($"You already have {GameData.MaxLamps} lamp posts.", Palette.UiRed); return; }
            if (Coins < GameData.LampPrice) { Notify($"A lamp post costs {GameData.LampPrice} coins.", Palette.UiRed); FX.Play(Sfx.Error, 0.4f); return; }
            HUD.I.Open = HUD.Panel.None;
            LampPlacer.BeginNew();
            Notify("Click where you want the lamp post (inside the farm wall). Esc cancels.", Palette.UiGold, 6f);
        }

        public void BuildLampAt(Vector3 spot)
        {
            if (Lamps >= GameData.MaxLamps) return;
            if (!Spend(GameData.LampPrice)) return;
            Lamps++;
            var t = SpawnLamp(spot);
            FX.Play(Sfx.Coin, 0.5f);
            FX.Text(t.position + Vector3.up * 4f, "Lamp post built!", Palette.UiGreen);
            Notify("Lamp post built. It turns on at 18:00 and off at 06:00. Click it any time to move it.", Palette.UiGreen, 6f);
        }

        Transform SpawnLamp(Vector3 spot)
        {
            var go = new GameObject("Lamp Post");
            go.transform.position = new Vector3(spot.x, 0f, spot.z);
            var lamp = go.AddComponent<Lamp>();
            LampObjects.Add(lamp);
            return go.transform;
        }

        /// <summary>Older saves only stored how many lamps there were: put them at the old automatic spots.</summary>
        Transform PlaceLamp()
        {
            foreach (var spot in LampSpots())
            {
                if (LampObjects.Any(l => l && FlatDistance(l.transform.position, spot) < 1f)) continue;
                return SpawnLamp(spot);
            }
            return null;
        }

        /// <summary>True while the lamps are on (18:00-06:00) near a lamp post (predators avoid these spots).</summary>
        public bool NearLitLamp(Vector3 pos)
        {
            if (!DayNight.LightsOn) return false;
            foreach (var l in LampObjects)
                if (l && FlatDistance(l.transform.position, pos) <= GameData.LampRadius) return true;
            return false;
        }

        // ================= Animals =================

        public Animal SpawnAnimal(AnimalKind kind, Stage stage, Vector3 pos, bool rare = false, int field = -1)
        {
            if (field < 0 || field >= Fields.Count || !Fields[field].Owned)
            {
                var home = FieldFor(kind);
                field = home != null && home.Owned ? home.Index : 0;
            }
            var go = new GameObject(GameData.Animals[kind].Name);
            go.transform.position = new Vector3(pos.x, 0f, pos.z);
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            var a = go.AddComponent<Animal>();
            a.Field = field;
            a.Init(kind, stage, rare);
            Animals.Add(a);
            return a;
        }

        public void BuyAnimal(AnimalKind kind, Stage stage)
        {
            var d = GameData.Animals[kind];
            if (Rank < d.UnlockRank) return;
            var home = FieldFor(kind);
            if (!home.Owned) { Notify($"Buy the {home.Def.Name} first - {d.Plural} need their own field.", Palette.UiRed); FX.Play(Sfx.Error, 0.4f); return; }
            if (FieldFull(kind)) { Notify($"The {home.Def.Name} is full ({home.Def.Capacity} {d.Plural}).", Palette.UiRed); FX.Play(Sfx.Error, 0.4f); return; }
            if (!Spend(stage == Stage.Baby ? d.BabyPrice : d.AdultPrice)) return;
            var bought = SpawnAnimal(kind, stage, home.RandomPoint(), false, home.Index);
            AddXp(2);
            FX.Text(bought.transform.position + Vector3.up * 1.5f, "New!", Palette.UiGreen, 1.1f);
            FX.Burst(bought.transform.position + Vector3.up * 0.5f, Palette.UiGreen, 10, 0.08f, 2.5f);
            FX.Play(Sfx.Coin, 0.5f);
            Notify($"Bought a {(stage == Stage.Baby ? d.BabyName.ToLower() : d.Name.ToLower())} ({Fields[bought.Field].Def.Name}).", Palette.UiGreen);
        }

        public void SellAnimal(Animal a)
        {
            if (!a) return;
            int price = GameData.SellPrice(a);
            AddCoins(price);
            Notify($"Sold a {a.DisplayName.ToLower()} for {price} coins.", Palette.UiGold);
            FX.Text(a.transform.position + Vector3.up * 1.5f, $"+{price} coins", Palette.UiGold, 1.2f);
            FX.Play(Sfx.Coin, 0.6f);
            RemoveAnimal(a);
        }

        public void RemoveAnimal(Animal a)
        {
            Animals.Remove(a);
            if (a) Destroy(a.gameObject);
        }

        public void OnAnimalLost(Animal a, string reason)
        {
            AnimalsLost++;
            Notify($"You lost a {a.DisplayName.ToLower()} - {reason}.", Palette.UiRed, 5f);
            FX.Text(a.transform.position + Vector3.up * 1.5f, "Lost!", Palette.UiRed, 1.2f);
            FX.Play(Sfx.Lost, 0.6f);
            RemoveAnimal(a);
        }

        public int CountAnimals(AnimalKind k, bool adultsOnly = false) =>
            Animals.Count(a => a && a.Kind == k && (!adultsOnly || a.Stage != Stage.Baby));

        public bool AnyAnimalNear(AnimalKind k, Vector3 pos, float radius, bool adultsOnly = false)
        {
            foreach (var a in Animals)
                if (a && a.Kind == k && (!adultsOnly || a.Stage != Stage.Baby) && FlatDistance(a.transform.position, pos) <= radius) return true;
            return false;
        }

        // ================= Helpers (cats & dogs) =================

        public int CountHelpers(HelperKind k) => Helpers.Count(h => h && h.Kind == k);

        public bool AnyHelperNear(HelperKind k, Vector3 pos, float radius)
        {
            foreach (var h in Helpers)
                if (h && h.Kind == k && FlatDistance(h.transform.position, pos) <= radius) return true;
            return false;
        }

        public Helper SpawnHelper(HelperKind kind)
        {
            Vector3 pos = kind == HelperKind.Cat ? WarehouseDoor : RandomPasturePoint();
            var go = new GameObject(GameData.Helpers[kind].Name);
            go.transform.position = new Vector3(pos.x, 0f, pos.z);
            var h = go.AddComponent<Helper>();
            h.Init(kind);
            return h;
        }

        public void BuyHelper(HelperKind kind)
        {
            var d = GameData.Helpers[kind];
            if (Rank < d.UnlockRank) return;
            if (CountHelpers(kind) >= d.Max) { Notify($"You already have {d.Max} of those.", Palette.UiRed); return; }
            if (!Spend(d.Price)) return;
            SpawnHelper(kind);
            AddXp(5);
            Notify($"A {d.Name.ToLower()} joined your farm!", Palette.UiGreen);
            FX.Play(Sfx.Coin, 0.5f);
        }

        // ================= Workers =================

        public int CountWorkers(WorkerKind k) => Workers.Count(w => w && w.Kind == k);
        public int TotalWages => Workers.Where(w => w).Sum(w => w.Def.Wage);

        Worker SpawnWorker(WorkerKind kind)
        {
            var go = new GameObject(GameData.Workers[kind].Name);
            go.transform.position = new Vector3(Random.Range(-4f, 4f), 0f, Random.Range(11f, 14f));
            var w = go.AddComponent<Worker>();
            w.Init(kind);
            return w;
        }

        public void HireWorker(WorkerKind kind)
        {
            var d = GameData.Workers[kind];
            if (Rank < d.UnlockRank) return;
            if (CountWorkers(kind) >= d.Max) { Notify($"You already have {d.Max} {d.Name}(s).", Palette.UiRed); return; }
            if (!Spend(d.HireCost)) return;
            bool firstGuard = kind == WorkerKind.Guard && CountWorkers(WorkerKind.Guard) == 0;
            var w = SpawnWorker(kind);
            if (firstGuard)
                Notify("With a Guard on duty, other predators may visit too - but they will be weak and easy to catch.", Palette.UiGold, 7f);
            AddXp(5);
            Notify($"You hired a {d.Name}! Wage: {d.Wage} coins per day.", Palette.UiGreen, 5f);
            FX.Play(Sfx.Coin, 0.5f);
            FX.Text(w.transform.position + Vector3.up * 2.5f, "Hired!", Palette.UiGreen, 1.2f);
        }

        public void FireWorker(WorkerKind kind)
        {
            var w = Workers.FirstOrDefault(x => x && x.Kind == kind);
            if (w == null) return;
            Notify($"Your {w.Def.Name} left the farm.", Palette.UiWhite);
            Workers.Remove(w);
            Destroy(w.gameObject);
        }

        void PayWages()
        {
            int paid = 0;
            foreach (var w in Workers.ToArray())
            {
                if (!w) continue;
                if (Coins >= w.Def.Wage) { Coins -= w.Def.Wage; paid += w.Def.Wage; }
                else
                {
                    Notify($"You couldn't pay your {w.Def.Name} - they quit!", Palette.UiRed, 6f);
                    Workers.Remove(w);
                    Destroy(w.gameObject);
                }
            }
            if (paid > 0) Notify($"Paid {paid} coins in worker wages.", Palette.UiWhite, 5f);
        }

        // ================= World queries =================

        public Vector3 RandomPasturePoint()
        {
            var owned = Fields.Where(f => f.Owned).ToList();
            return owned[Random.Range(0, owned.Count)].RandomPoint();
        }

        public static Vector3 ClampToPasture(Vector3 p) => I.Fields[0].Clamp(p);

        public static float FlatDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        public Predator NearestPredator(Vector3 pos, float radius)
        {
            Predator best = null;
            float bestD = radius;
            foreach (var p in Predators)
            {
                if (!p) continue;
                float d = FlatDistance(p.transform.position, pos);
                if (d <= bestD) { bestD = d; best = p; }
            }
            return best;
        }

        public GrassPatch NearestGrassWithFood(Vector3 pos, int field)
        {
            var list = field >= 0 && field < Fields.Count ? Fields[field].Grass : Grass;
            GrassPatch best = null;
            float bestD = float.MaxValue;
            foreach (var g in list)
            {
                if (!g || g.Amount <= 0.1f) continue;
                float d = FlatDistance(g.transform.position, pos);
                if (d < bestD) { bestD = d; best = g; }
            }
            return best;
        }

        public GrassPatch GrassNear(Vector3 pos, float radius)
        {
            foreach (var g in Grass)
                if (g && FlatDistance(g.transform.position, pos) <= radius) return g;
            return null;
        }

        // ================= Messages =================

        public void Notify(string text, Color? color = null, float seconds = 4f)
        {
            Notes.Add(new Note { Text = text, Color = color ?? Palette.UiWhite, Until = Time.unscaledTime + seconds });
            if (Notes.Count > 8) Notes.RemoveAt(0);
        }
    }
}
