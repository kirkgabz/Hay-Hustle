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
    /// Central game state: economy, farm rank, animals, saving.
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

        // ---- Farm pasture area (animals stay inside) ----
        public static readonly Vector3 PastureCenter = new Vector3(0f, 0f, -7f);
        public const float PastureMinX = -11f, PastureMaxX = 11f, PastureMinZ = -17f, PastureMaxZ = 3f;

        // ---- State ----
        public GameMode Mode { get; set; } = GameMode.Title;
        public string FarmName = "My Farm";
        public CharacterChoice Character;
        public int Coins, Water = 10, WaterMax = 10, Xp;
        public int WarehouseCapacity = 40, AnimalCapacity = 30;
        public int[] Inventory = new int[GameData.ProductCount];
        public int PredatorsCaught, AnimalsLost;
        public string LastRewardDate = "";
        public int RewardStreak;
        public Product WantedProduct = Product.Egg;
        public int Mess;                       // spoiled food lying around - attracts rats
        public Vector3 WarehouseDoor;          // where rats go to steal

        public int Rank => GameData.RankFromXp(Xp);

        public readonly List<Animal> Animals = new List<Animal>();
        public readonly List<Predator> Predators = new List<Predator>();
        public readonly List<Helper> Helpers = new List<Helper>();
        public readonly List<GrassPatch> Grass = new List<GrassPatch>();
        public readonly List<Note> Notes = new List<Note>();

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

            WorldBuilder.Build(this);
            DayNight = gameObject.AddComponent<DayNightCycle>();
            DayNight.Sun = WorldBuilder.Sun;
            DayNight.OnNewDay += () =>
            {
                Notify($"Day {DayNight.Day} begins on {FarmName}.", Palette.UiGold, 5f);
                RerollWanted();
            };
            Spawner = gameObject.AddComponent<PredatorSpawner>();
            gameObject.AddComponent<FX>();
            gameObject.AddComponent<HUD>();

            Mode = SaveSystem.Exists ? GameMode.Title : GameMode.CharacterSelect;
            Time.timeScale = 0f; // world is paused on the menus
        }

        void Update()
        {
            Notes.RemoveAll(n => n.Until < Time.unscaledTime);
            if (Mode != GameMode.Playing) return;

            bool night = DayNight.IsNight;
            if (night != wasNight)
            {
                wasNight = night;
                Notify(night ? "Night has fallen - predators are bolder in the dark!" : "Morning! The sun is up on the farm.",
                    night ? Palette.UiRed : Palette.UiGold, 5f);
            }

            autosave -= Time.deltaTime;
            if (autosave <= 0f) { autosave = 30f; SaveGame(); }
        }

        void OnApplicationQuit() => SaveGame();
        void OnApplicationPause(bool paused) { if (paused) SaveGame(); }

        // ================= Starting / loading =================

        public void StartNewGame(CharacterChoice choice, string farmName)
        {
            ClearWorld();
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
            DayNight.TimeOfDay = 0.3f;
            DayNight.Day = 1;

            for (int i = 0; i < Grass.Count; i++) Grass[i].SetAmount(i % 3 == 0 ? 1f : 0f);
            for (int i = 0; i < 3; i++) SpawnAnimal(AnimalKind.Chicken, Stage.Adult, RandomPasturePoint());

            RerollWanted();
            BeginPlay();
            Notify("Tip: plant grass (E) so your animals can eat. Press H for help.", Palette.UiWhite, 8f);
        }

        public void ContinueGame()
        {
            var d = SaveSystem.Load();
            if (d == null) { Mode = GameMode.CharacterSelect; return; }

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
            if (d.grass != null)
                for (int i = 0; i < Mathf.Min(Grass.Count, d.grass.Length); i++) Grass[i].SetAmount(d.grass[i]);
            foreach (var s in d.animals)
            {
                var a = SpawnAnimal((AnimalKind)s.kind, (Stage)s.stage, new Vector3(s.x, 0f, s.z), s.rare);
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
            for (int i = 0; i < d.cats; i++) SpawnHelper(HelperKind.Cat);
            for (int i = 0; i < d.dogs; i++) SpawnHelper(HelperKind.Dog);
            BeginPlay();
        }

        void BeginPlay()
        {
            if (Player) Destroy(Player.gameObject);
            Player = WorldBuilder.SpawnPlayer(Character);
            CameraRig.Target = Player.transform;
            CameraRig.Yaw = 180f;

            Mode = GameMode.Playing;
            Time.timeScale = 1f;
            wasNight = DayNight.IsNight;
            Spawner.ResetTimer();
            if (DailyRewards.CanClaim(this)) HUD.I.Open = HUD.Panel.Rewards;
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
            foreach (var pk in ProductPickup.All.ToArray()) if (pk) Destroy(pk.gameObject);
            Spawner.ResetTimer();
            if (Truck) Truck.ResetTruck();
        }

        public void SaveGame()
        {
            if (Mode != GameMode.Playing) return;
            var d = new SaveData
            {
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
                inventory = (int[])Inventory.Clone(),
                grass = new float[Grass.Count],
            };
            if (Truck) for (int i = 0; i < d.inventory.Length; i++) d.inventory[i] += Truck.Cargo[i]; // unsent cargo goes back
            for (int i = 0; i < Grass.Count; i++) d.grass[i] = Grass[i].Amount;
            foreach (var a in Animals)
            {
                if (!a) continue;
                var p = a.transform.position;
                d.animals.Add(new AnimalSave
                {
                    kind = (int)a.Kind, stage = (int)a.Stage, age = a.Age, hunger = a.Hunger,
                    wellFed = a.WellFedTime, rare = a.Rare, x = p.x, z = p.z
                });
            }
            SaveSystem.Save(d);
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
            if (InventoryTotal + n > WarehouseCapacity) return false;
            Inventory[(int)p] += n;
            return true;
        }

        public bool HasProduct(Product p, int n) => Inventory[(int)p] >= n;
        public void RemoveProduct(Product p, int n) => Inventory[(int)p] = Mathf.Max(0, Inventory[(int)p] - n);
        public int ProductPrice(Product p) => GameData.Products[p].Price * (p == WantedProduct ? 2 : 1);

        public void RerollWanted()
        {
            var options = GameData.Products.Values.Where(d => d.UnlockRank <= Rank).Select(d => d.Product).ToList();
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
            }
            foreach (var a in GameData.Animals.Values)
                if (a.UnlockRank == r) Notify($"Unlocked: {a.Plural} in the shop (press B)", Palette.UiGreen, 6f);
            foreach (var rec in GameData.Recipes)
                if (rec.UnlockRank == r) Notify($"Unlocked: {rec.Building} ({GameData.ProductName(rec.Input)} -> {GameData.ProductName(rec.Output)})", Palette.UiGreen, 6f);
            if (r == GameData.BarnUnlockRank) Notify("Unlocked: Breeding Barn", Palette.UiGreen, 6f);
            foreach (var h in GameData.Helpers.Values)
                if (h.UnlockRank == r) Notify($"Unlocked: {h.Name} helper in the shop (press B)", Palette.UiGreen, 6f);
            foreach (var p in GameData.Predators.Values)
                if (p.MinRank == r && r > 1) Notify($"Watch out: {p.Name.ToLower()}s now hunt your farm{(p.NightOnly ? " at night" : "")}!", Palette.UiRed, 6f);
        }

        // ================= Animals =================

        public Animal SpawnAnimal(AnimalKind kind, Stage stage, Vector3 pos, bool rare = false)
        {
            var go = new GameObject(GameData.Animals[kind].Name);
            go.transform.position = new Vector3(pos.x, 0f, pos.z);
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            var a = go.AddComponent<Animal>();
            a.Init(kind, stage, rare);
            Animals.Add(a);
            return a;
        }

        public void BuyAnimal(AnimalKind kind, Stage stage)
        {
            var d = GameData.Animals[kind];
            if (Rank < d.UnlockRank) return;
            if (Animals.Count >= AnimalCapacity) { Notify("Your pasture is full!", Palette.UiRed); return; }
            if (!Spend(stage == Stage.Baby ? d.BabyPrice : d.AdultPrice)) return;
            var bought = SpawnAnimal(kind, stage, RandomPasturePoint());
            AddXp(2);
            FX.Text(bought.transform.position + Vector3.up * 1.5f, "New!", Palette.UiGreen, 1.1f);
            FX.Burst(bought.transform.position + Vector3.up * 0.5f, Palette.UiGreen, 10, 0.08f, 2.5f);
            FX.Play(Sfx.Coin, 0.5f);
            Notify($"Bought a {(stage == Stage.Baby ? d.BabyName.ToLower() : d.Name.ToLower())}.", Palette.UiGreen);
        }

        public void SellAnimal(Animal a)
        {
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

        // ================= World queries =================

        public Vector3 RandomPasturePoint() =>
            new Vector3(Random.Range(PastureMinX + 1f, PastureMaxX - 1f), 0f, Random.Range(PastureMinZ + 1f, PastureMaxZ - 1f));

        public static Vector3 ClampToPasture(Vector3 p) =>
            new Vector3(Mathf.Clamp(p.x, PastureMinX, PastureMaxX), 0f, Mathf.Clamp(p.z, PastureMinZ, PastureMaxZ));

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

        public GrassPatch NearestGrassWithFood(Vector3 pos)
        {
            GrassPatch best = null;
            float bestD = float.MaxValue;
            foreach (var g in Grass)
            {
                if (g.Amount <= 0.1f) continue;
                float d = FlatDistance(g.transform.position, pos);
                if (d < bestD) { bestD = d; best = g; }
            }
            return best;
        }

        public GrassPatch GrassNear(Vector3 pos, float radius)
        {
            foreach (var g in Grass)
                if (FlatDistance(g.transform.position, pos) <= radius) return g;
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
