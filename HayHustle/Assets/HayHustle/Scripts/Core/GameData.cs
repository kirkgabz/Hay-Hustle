using System.Collections.Generic;
using UnityEngine;

namespace HayHustle
{
    // NOTE: only ADD new values at the END of these enums - save files store them as numbers.
    public enum AnimalKind { Chicken, Sheep, Cow, Goat, Pig, Turkey, Llama }
    public enum Product { Egg, Wool, Milk, Cake, Yarn, Butter, GoatMilk, Cheese, Truffle, Feather, Pillow, LlamaWool, Coat, Wheat, Carrot, Corn, Pumpkin, Meal }
    public enum PredatorKind { Fox, Hawk, Wolf, Snake, Rat, Bear, Zombie }
    public enum CropKind { Wheat, Carrot, Corn, Pumpkin }

    /// <summary>Jobs for hired workers and survivors. Rest = off duty / healing in the Bunkhouse.</summary>
    public enum JobKind { Rest, Collector, WaterCarrier, FactoryHand, Driver, Guard, Builder, Farmer, Cook }

    public class CropDef
    {
        public CropKind Kind;
        public string Name;
        public Product Produces;
        public int SeedCost, Yield, UnlockRank;
        public float GrowTime;
        public Color Color;
    }
    public enum HelperKind { Cat, Dog }
    public enum WorkerKind { Collector, WaterCarrier, FactoryHand, Driver, Guard }

    public class WorkerDef
    {
        public WorkerKind Kind;
        public string Name, Job;
        public int HireCost, Wage, UnlockRank, Max;
        public Color Shirt, Cap;
    }

    public class FieldDef
    {
        public int Index;
        public string Name;
        public Vector3 Center;
        public AnimalKind Kind;   // each field is home to one kind of animal
        public int Cols, Rows, Price, UnlockRank, Capacity;
    }
    public enum Stage { Baby, Adult, Prime }
    public enum CharacterChoice { Boy, Girl }

    public class AnimalDef
    {
        public AnimalKind Kind;
        public string Name, Plural, BabyName, RareName, Perk;
        public int UnlockRank;
        public int BabyPrice, AdultPrice, SellBase;
        public Product Produces;
        public float ProduceTime;      // seconds per product when well fed
        public float GrowTime;         // seconds from baby to adult
        public float HungerPerSecond;  // how fast the hunger bar drops (1 = full bar)
        public float FoodValue;        // hunger restored per unit of grass eaten
        public float Speed;
    }

    public class ProductDef
    {
        public Product Product;
        public string Name;
        public int Price;
        public float SpoilTime;        // seconds before it spoils if left on the ground
        public int UnlockRank;
    }

    public class RecipeDef
    {
        public string Building;
        public Product Input;
        public int InputAmount;
        public Product Output;
        public float Time;
        public int UnlockRank;
        public Vector3 Position;
        public Color Wall, Roof;
    }

    public class PredatorDef
    {
        public PredatorKind Kind;
        public string Name;
        public int Hits;               // net hits needed to catch it
        public float Speed, FleeSpeed;
        public AnimalKind[] Targets;
        public bool Flying, NightOnly;
        public int Bounty;             // coins when caught (sold to the zoo)
        public int MinRank;
    }

    public class HelperDef
    {
        public HelperKind Kind;
        public string Name, Description;
        public int Price, UnlockRank, Max;
        public float Speed, HitInterval;
        public PredatorKind[] Hunts;
    }

    /// <summary>All tunable game numbers live here.</summary>
    public static class GameData
    {
        public const int MaxRank = 20;
        public const int StartCoins = 300;
        public const int BarnUnlockRank = 4;
        public static readonly int ProductCount = System.Enum.GetValues(typeof(Product)).Length;

        public static readonly Dictionary<AnimalKind, AnimalDef> Animals = new Dictionary<AnimalKind, AnimalDef>
        {
            [AnimalKind.Chicken] = new AnimalDef
            {
                Kind = AnimalKind.Chicken, Name = "Chicken", Plural = "chickens", BabyName = "Chick", RareName = "Golden Hen",
                Perk = "Bug Eater - grass planted near chickens grows thicker.",
                UnlockRank = 1, BabyPrice = 40, AdultPrice = 100, SellBase = 60,
                Produces = Product.Egg, ProduceTime = 20f, GrowTime = 60f,
                HungerPerSecond = 1f / 90f, FoodValue = 3f, Speed = 1.6f
            },
            [AnimalKind.Sheep] = new AnimalDef
            {
                Kind = AnimalKind.Sheep, Name = "Sheep", Plural = "sheep", BabyName = "Lamb", RareName = "Cloud Sheep",
                Perk = "Flock Alarm - adult sheep hear predators early (+1.5s warning).",
                UnlockRank = 3, BabyPrice = 150, AdultPrice = 350, SellBase = 220,
                Produces = Product.Wool, ProduceTime = 30f, GrowTime = 90f,
                HungerPerSecond = 1f / 75f, FoodValue = 1.5f, Speed = 1.4f
            },
            [AnimalKind.Cow] = new AnimalDef
            {
                Kind = AnimalKind.Cow, Name = "Cow", Plural = "cows", BabyName = "Calf", RareName = "Brave Cow",
                Perk = "Fertilizer - grass regrows wherever cows stand.",
                UnlockRank = 5, BabyPrice = 400, AdultPrice = 900, SellBase = 600,
                Produces = Product.Milk, ProduceTime = 35f, GrowTime = 120f,
                HungerPerSecond = 1f / 60f, FoodValue = 1f, Speed = 1.2f
            },
            [AnimalKind.Goat] = new AnimalDef
            {
                Kind = AnimalKind.Goat, Name = "Goat", Plural = "goats", BabyName = "Kid", RareName = "Mountain Goat",
                Perk = "Headbutt - knocks away snakes and rats that come close.",
                UnlockRank = 7, BabyPrice = 300, AdultPrice = 650, SellBase = 420,
                Produces = Product.GoatMilk, ProduceTime = 30f, GrowTime = 100f,
                HungerPerSecond = 1f / 70f, FoodValue = 1.3f, Speed = 1.5f
            },
            [AnimalKind.Pig] = new AnimalDef
            {
                Kind = AnimalKind.Pig, Name = "Pig", Plural = "pigs", BabyName = "Piglet", RareName = "Truffle King",
                Perk = "Scrap Eater - cleans up spoiled goods, so fewer rats come.",
                UnlockRank = 9, BabyPrice = 450, AdultPrice = 1000, SellBase = 700,
                Produces = Product.Truffle, ProduceTime = 45f, GrowTime = 120f,
                HungerPerSecond = 1f / 55f, FoodValue = 1f, Speed = 1.1f
            },
            [AnimalKind.Turkey] = new AnimalDef
            {
                Kind = AnimalKind.Turkey, Name = "Turkey", Plural = "turkeys", BabyName = "Poult", RareName = "Royal Turkey",
                Perk = "Gobble Scare - hawks won't dive near adult turkeys.",
                UnlockRank = 11, BabyPrice = 500, AdultPrice = 1100, SellBase = 750,
                Produces = Product.Feather, ProduceTime = 35f, GrowTime = 110f,
                HungerPerSecond = 1f / 65f, FoodValue = 1.4f, Speed = 1.3f
            },
            [AnimalKind.Llama] = new AnimalDef
            {
                Kind = AnimalKind.Llama, Name = "Llama", Plural = "llamas", BabyName = "Cria", RareName = "Snow Llama",
                Perk = "Guardian - kicks foxes, wolves and bears that come near.",
                UnlockRank = 14, BabyPrice = 1200, AdultPrice = 2500, SellBase = 1800,
                Produces = Product.LlamaWool, ProduceTime = 50f, GrowTime = 150f,
                HungerPerSecond = 1f / 60f, FoodValue = 1f, Speed = 1.3f
            },
        };

        public static readonly Dictionary<Product, ProductDef> Products = new Dictionary<Product, ProductDef>
        {
            [Product.Egg]       = new ProductDef { Product = Product.Egg,       Name = "Egg",        Price = 10,  SpoilTime = 45f,  UnlockRank = 1 },
            [Product.Cake]      = new ProductDef { Product = Product.Cake,      Name = "Cake",       Price = 60,  SpoilTime = 60f,  UnlockRank = 2 },
            [Product.Wool]      = new ProductDef { Product = Product.Wool,      Name = "Wool",       Price = 30,  SpoilTime = 90f,  UnlockRank = 3 },
            [Product.Yarn]      = new ProductDef { Product = Product.Yarn,      Name = "Yarn",       Price = 110, SpoilTime = 60f,  UnlockRank = 3 },
            [Product.Milk]      = new ProductDef { Product = Product.Milk,      Name = "Milk",       Price = 45,  SpoilTime = 45f,  UnlockRank = 5 },
            [Product.Butter]    = new ProductDef { Product = Product.Butter,    Name = "Butter",     Price = 160, SpoilTime = 60f,  UnlockRank = 5 },
            [Product.GoatMilk]  = new ProductDef { Product = Product.GoatMilk,  Name = "Goat Milk",  Price = 55,  SpoilTime = 45f,  UnlockRank = 7 },
            [Product.Cheese]    = new ProductDef { Product = Product.Cheese,    Name = "Cheese",     Price = 200, SpoilTime = 60f,  UnlockRank = 7 },
            [Product.Truffle]   = new ProductDef { Product = Product.Truffle,   Name = "Truffle",    Price = 150, SpoilTime = 60f,  UnlockRank = 9 },
            [Product.Feather]   = new ProductDef { Product = Product.Feather,   Name = "Feather",    Price = 40,  SpoilTime = 90f,  UnlockRank = 11 },
            [Product.Pillow]    = new ProductDef { Product = Product.Pillow,    Name = "Pillow",     Price = 220, SpoilTime = 60f,  UnlockRank = 11 },
            [Product.LlamaWool] = new ProductDef { Product = Product.LlamaWool, Name = "Llama Wool", Price = 120, SpoilTime = 120f, UnlockRank = 14 },
            [Product.Coat]      = new ProductDef { Product = Product.Coat,      Name = "Luxury Coat",Price = 500, SpoilTime = 60f,  UnlockRank = 14 },
            [Product.Wheat]     = new ProductDef { Product = Product.Wheat,     Name = "Wheat",      Price = 6,   SpoilTime = 90f,  UnlockRank = 1 },
            [Product.Carrot]    = new ProductDef { Product = Product.Carrot,    Name = "Carrot",     Price = 10,  SpoilTime = 90f,  UnlockRank = 2 },
            [Product.Corn]      = new ProductDef { Product = Product.Corn,      Name = "Corn",       Price = 12,  SpoilTime = 90f,  UnlockRank = 3 },
            [Product.Pumpkin]   = new ProductDef { Product = Product.Pumpkin,   Name = "Pumpkin",    Price = 25,  SpoilTime = 120f, UnlockRank = 5 },
            [Product.Meal]      = new ProductDef { Product = Product.Meal,      Name = "Meal",       Price = 30,  SpoilTime = 90f,  UnlockRank = 1 },
        };

        public static readonly List<RecipeDef> Recipes = new List<RecipeDef>
        {
            new RecipeDef { Building = "Bakery", Input = Product.Egg, InputAmount = 2, Output = Product.Cake, Time = 15f, UnlockRank = 2,
                Position = new Vector3(-12f, 0f, 14f), Wall = new Color(0.93f, 0.85f, 0.7f), Roof = new Color(0.55f, 0.22f, 0.15f) },
            new RecipeDef { Building = "Weaving Mill", Input = Product.Wool, InputAmount = 1, Output = Product.Yarn, Time = 20f, UnlockRank = 3,
                Position = new Vector3(12f, 0f, 14f), Wall = new Color(0.75f, 0.82f, 0.88f), Roof = new Color(0.25f, 0.3f, 0.4f) },
            new RecipeDef { Building = "Dairy", Input = Product.Milk, InputAmount = 1, Output = Product.Butter, Time = 25f, UnlockRank = 5,
                Position = new Vector3(-19f, 0f, -4f), Wall = new Color(0.96f, 0.96f, 0.94f), Roof = new Color(0.2f, 0.4f, 0.65f) },
            new RecipeDef { Building = "Cheese Maker", Input = Product.GoatMilk, InputAmount = 2, Output = Product.Cheese, Time = 30f, UnlockRank = 7,
                Position = new Vector3(-20f, 0f, 6f), Wall = new Color(0.98f, 0.9f, 0.55f), Roof = new Color(0.45f, 0.3f, 0.15f) },
            new RecipeDef { Building = "Pillow Factory", Input = Product.Feather, InputAmount = 3, Output = Product.Pillow, Time = 30f, UnlockRank = 11,
                Position = new Vector3(-19f, 0f, -14f), Wall = new Color(0.9f, 0.8f, 0.9f), Roof = new Color(0.4f, 0.25f, 0.45f) },
            new RecipeDef { Building = "Kitchen", Input = Product.Wheat, InputAmount = 2, Output = Product.Meal, Time = 20f, UnlockRank = 1,
                Position = new Vector3(-12f, 0f, 25f), Wall = new Color(0.85f, 0.75f, 0.6f), Roof = new Color(0.45f, 0.18f, 0.12f) },
            new RecipeDef { Building = "Tailor", Input = Product.LlamaWool, InputAmount = 2, Output = Product.Coat, Time = 45f, UnlockRank = 14,
                Position = new Vector3(20f, 0f, -17f), Wall = new Color(0.82f, 0.7f, 0.55f), Roof = new Color(0.2f, 0.2f, 0.22f) },
        };

        public static readonly Dictionary<PredatorKind, PredatorDef> Predators = new Dictionary<PredatorKind, PredatorDef>
        {
            [PredatorKind.Fox] = new PredatorDef { Kind = PredatorKind.Fox, Name = "Fox", Hits = 3, Speed = 4.2f, FleeSpeed = 5f,
                Targets = new[] { AnimalKind.Chicken, AnimalKind.Turkey }, Bounty = 40, MinRank = 1 },
            [PredatorKind.Hawk] = new PredatorDef { Kind = PredatorKind.Hawk, Name = "Hawk", Hits = 2, Speed = 5f, FleeSpeed = 6f,
                Targets = new[] { AnimalKind.Chicken }, Flying = true, Bounty = 50, MinRank = 2 },
            [PredatorKind.Wolf] = new PredatorDef { Kind = PredatorKind.Wolf, Name = "Wolf", Hits = 6, Speed = 3.6f, FleeSpeed = 4.2f,
                Targets = new[] { AnimalKind.Sheep, AnimalKind.Cow, AnimalKind.Goat }, NightOnly = true, Bounty = 120, MinRank = 3 },
            [PredatorKind.Snake] = new PredatorDef { Kind = PredatorKind.Snake, Name = "Snake", Hits = 2, Speed = 1.8f, FleeSpeed = 2.4f,
                Targets = new[] { AnimalKind.Chicken }, Bounty = 60, MinRank = 6 },
            [PredatorKind.Rat] = new PredatorDef { Kind = PredatorKind.Rat, Name = "Rat", Hits = 1, Speed = 3.2f, FleeSpeed = 4f,
                Targets = new AnimalKind[0], Bounty = 15, MinRank = 10 },
            [PredatorKind.Bear] = new PredatorDef { Kind = PredatorKind.Bear, Name = "Bear", Hits = 12, Speed = 3.0f, FleeSpeed = 3.5f,
                Targets = new[] { AnimalKind.Cow, AnimalKind.Pig, AnimalKind.Goat, AnimalKind.Sheep }, NightOnly = true, Bounty = 400, MinRank = 15 },
            [PredatorKind.Zombie] = new PredatorDef { Kind = PredatorKind.Zombie, Name = "Zombie", Hits = 5, Speed = 1.8f, FleeSpeed = 2.2f,
                Targets = new[] { AnimalKind.Chicken, AnimalKind.Sheep, AnimalKind.Cow, AnimalKind.Goat, AnimalKind.Pig, AnimalKind.Turkey, AnimalKind.Llama },
                Bounty = 30, MinRank = 1 },
        };

        public static readonly Dictionary<HelperKind, HelperDef> Helpers = new Dictionary<HelperKind, HelperDef>
        {
            [HelperKind.Cat] = new HelperDef { Kind = HelperKind.Cat, Name = "Cat", Price = 300, UnlockRank = 6, Max = 2,
                Description = "Guards the warehouse. Catches rats and finds snakes hiding in the grass.",
                Speed = 6f, HitInterval = 0.6f, Hunts = new[] { PredatorKind.Rat, PredatorKind.Snake } },
            [HelperKind.Dog] = new HelperDef { Kind = HelperKind.Dog, Name = "Guard Dog", Price = 900, UnlockRank = 8, Max = 2,
                Description = "Patrols the pasture. Chases and bites foxes, wolves and bears.",
                Speed = 6.5f, HitInterval = 0.8f, Hunts = new[] { PredatorKind.Fox, PredatorKind.Wolf, PredatorKind.Bear, PredatorKind.Zombie } },
        };

        public static readonly Dictionary<WorkerKind, WorkerDef> Workers = new Dictionary<WorkerKind, WorkerDef>
        {
            [WorkerKind.Collector] = new WorkerDef { Kind = WorkerKind.Collector, Name = "Collector", HireCost = 400, Wage = 40, UnlockRank = 2, Max = 4,
                Job = "Picks up eggs, wool, milk and other goods before they spoil.", Shirt = new Color(0.25f, 0.55f, 0.3f), Cap = new Color(0.2f, 0.45f, 0.25f) },
            [WorkerKind.WaterCarrier] = new WorkerDef { Kind = WorkerKind.WaterCarrier, Name = "Water Carrier", HireCost = 500, Wage = 50, UnlockRank = 3, Max = 4,
                Job = "Fetches water from the well and replants empty grass plots in all your fields.", Shirt = new Color(0.25f, 0.45f, 0.8f), Cap = new Color(0.86f, 0.72f, 0.38f) },
            [WorkerKind.FactoryHand] = new WorkerDef { Kind = WorkerKind.FactoryHand, Name = "Factory Hand", HireCost = 800, Wage = 80, UnlockRank = 5, Max = 1,
                Job = "Runs your buildings: starts products and collects them when they are ready.", Shirt = new Color(0.92f, 0.92f, 0.9f), Cap = new Color(0.5f, 0.3f, 0.15f) },
            [WorkerKind.Driver] = new WorkerDef { Kind = WorkerKind.Driver, Name = "Driver", HireCost = 600, Wage = 60, UnlockRank = 6, Max = 1,
                Job = "Loads the truck (WANTED goods first) and sends it to town. Keeps enough raw goods for your buildings.", Shirt = new Color(0.45f, 0.45f, 0.5f), Cap = new Color(0.12f, 0.12f, 0.14f) },
            [WorkerKind.Guard] = new WorkerDef { Kind = WorkerKind.Guard, Name = "Guard", HireCost = 1200, Wage = 100, UnlockRank = 3, Max = 4,
                Job = "Patrols your fields with a pitchfork. Runs after any predator (fox, hawk, wolf, snake, rat, bear) and fights it until it is caught.", Shirt = new Color(0.55f, 0.15f, 0.12f), Cap = new Color(0.15f, 0.2f, 0.12f) },
        };

        /// <summary>One field per kind of animal. The Chicken Run is yours from the start; buy the others to keep those animals.</summary>
        public static readonly List<FieldDef> Fields = new List<FieldDef>
        {
            new FieldDef { Index = 0, Name = "Chicken Run", Kind = AnimalKind.Chicken, Center = new Vector3(-7f, 0f, -4f), Cols = 4, Rows = 4, Price = 0, UnlockRank = 1, Capacity = 5 },
            new FieldDef { Index = 1, Name = "Sheep Meadow", Kind = AnimalKind.Sheep, Center = new Vector3(7f, 0f, -4f), Cols = 4, Rows = 4, Price = 1000, UnlockRank = 3, Capacity = 5 },
            new FieldDef { Index = 2, Name = "Cow Pasture", Kind = AnimalKind.Cow, Center = new Vector3(-7f, 0f, -18f), Cols = 4, Rows = 4, Price = 2500, UnlockRank = 5, Capacity = 5 },
            new FieldDef { Index = 3, Name = "Goat Hill", Kind = AnimalKind.Goat, Center = new Vector3(7f, 0f, -18f), Cols = 4, Rows = 4, Price = 3500, UnlockRank = 7, Capacity = 5 },
            new FieldDef { Index = 4, Name = "Pig Pen", Kind = AnimalKind.Pig, Center = new Vector3(-7f, 0f, -32f), Cols = 4, Rows = 4, Price = 5000, UnlockRank = 9, Capacity = 5 },
            new FieldDef { Index = 5, Name = "Turkey Yard", Kind = AnimalKind.Turkey, Center = new Vector3(7f, 0f, -32f), Cols = 4, Rows = 4, Price = 6500, UnlockRank = 11, Capacity = 5 },
            new FieldDef { Index = 6, Name = "Llama Ridge", Kind = AnimalKind.Llama, Center = new Vector3(0f, 0f, -46f), Cols = 4, Rows = 4, Price = 9000, UnlockRank = 14, Capacity = 5 },
        };

        /// <summary>The forest ring around the farm (predators come from here).</summary>
        public const float ForestInner = 63f, ForestOuter = 84f, SpawnRadius = 66f, EscapeRadius = 80f, PlayerRadius = 60f;

        public const int LampPrice = 250;
        public const int MaxLamps = 10;
        public const float LampRadius = 10f;   // matches the lit circle (about 8 plots across)

        // Fence upgrades slow predators inside your fields. Index = fence level.
        public static readonly int[] FenceUpgradeCost = { 1200, 3000 };
        public static readonly float[] FenceSlow = { 0f, 0.3f, 0.5f };
        public static readonly string[] FenceNames = { "Wooden Fence", "Sturdy Fence", "Iron Fence" };
        public const int TrapPrice = 150;
        public const int MaxTraps = 4;
        public const float QuickSellRate = 0.7f;

        // ================= Survival =================

        public static readonly Dictionary<CropKind, CropDef> Crops = new Dictionary<CropKind, CropDef>
        {
            [CropKind.Wheat]   = new CropDef { Kind = CropKind.Wheat,   Name = "Wheat",   Produces = Product.Wheat,   SeedCost = 5,  GrowTime = 60f,  Yield = 3, UnlockRank = 1, Color = new Color(0.92f, 0.78f, 0.35f) },
            [CropKind.Carrot]  = new CropDef { Kind = CropKind.Carrot,  Name = "Carrot",  Produces = Product.Carrot,  SeedCost = 10, GrowTime = 90f,  Yield = 3, UnlockRank = 2, Color = new Color(0.95f, 0.5f, 0.12f) },
            [CropKind.Corn]    = new CropDef { Kind = CropKind.Corn,    Name = "Corn",    Produces = Product.Corn,    SeedCost = 15, GrowTime = 120f, Yield = 4, UnlockRank = 3, Color = new Color(0.98f, 0.85f, 0.25f) },
            [CropKind.Pumpkin] = new CropDef { Kind = CropKind.Pumpkin, Name = "Pumpkin", Produces = Product.Pumpkin, SeedCost = 30, GrowTime = 180f, Yield = 2, UnlockRank = 5, Color = new Color(0.95f, 0.55f, 0.1f) },
        };

        /// <summary>How much food one item gives a survivor. Survivors need FoodPerSurvivor every morning.</summary>
        public static readonly Dictionary<Product, int> FoodValue = new Dictionary<Product, int>
        {
            [Product.Wheat] = 1, [Product.Egg] = 1, [Product.Milk] = 1, [Product.GoatMilk] = 1,
            [Product.Carrot] = 2, [Product.Corn] = 2, [Product.Pumpkin] = 4, [Product.Meal] = 5,
        };
        public const int FoodPerSurvivor = 3;

        public static readonly int[] BunkUpgradeCost = { 0, 800, 1600, 3200, 5000 };   // level 1..5, 2 beds per level
        public const int BedsPerLevel = 2;
        public static readonly int[] BarnUpgradeCost = { 600, 1200, 2400, 4000, 6000 }; // +30 storage per level
        public const int BarnStoragePerLevel = 30;
        // ================= Gates, wood and guards =================

        public const float GateMaxHp = 10000f;
        public const float GateRepairPerMinute = 100f;   // per person repairing (they stack)
        public const float WoodPer100Hp = 1f;            // repair material
        public const int WoodPerTree = 3;
        public const float TreeRegrowSeconds = 180f;
        public const float PlayerChopSeconds = 5f, WorkerChopSeconds = 8f;
        public const float GuardZoneMargin = 5f;         // 2 plots beyond the fence and gate
        public const float GateGiveUpSeconds = 45f;      // animals give up on a gate after this (zombies never do)

        /// <summary>Damage per second a predator does to a gate while attacking it.</summary>
        public static float GateDps(PredatorKind k) => k switch
        {
            PredatorKind.Zombie => 30f,
            PredatorKind.Bear => 50f,
            PredatorKind.Wolf => 20f,
            PredatorKind.Fox => 8f,
            _ => 0f,   // hawks fly over, snakes and rats slip through the fence
        };

        public class WeaponDef
        {
            public string Name;
            public int Damage, Cost;
            public float Cooldown;
        }

        /// <summary>Guard weapons (Shop > Workers or Camp). Every guard uses the current level.</summary>
        public static readonly WeaponDef[] GuardWeapons =
        {
            new WeaponDef { Name = "Pitchfork",     Damage = 1, Cooldown = 0.8f, Cost = 0 },
            new WeaponDef { Name = "Machete",       Damage = 1, Cooldown = 0.5f, Cost = 1500 },
            new WeaponDef { Name = "Spear",         Damage = 2, Cooldown = 0.7f, Cost = 3500 },
            new WeaponDef { Name = "Battle Axe",    Damage = 3, Cooldown = 0.8f, Cost = 6000 },
            new WeaponDef { Name = "Flaming Sword", Damage = 4, Cooldown = 0.6f, Cost = 10000 },
        };

        public static readonly string[] SurvivorNames =
        {
            "Maria", "Jun", "Rosa", "Ben", "Lito", "Ana", "Carlo", "Mika", "Tess", "Diego", "Nina", "Paolo",
            "Lea", "Marco", "Ivy", "Rafael", "Joy", "Sam", "Liza", "Tony", "Gina", "Andre", "Kim", "Noel",
        };

        public static string JobName(JobKind j) => j switch
        {
            JobKind.Rest => "Off duty",
            JobKind.WaterCarrier => "Water Carrier",
            JobKind.FactoryHand => "Factory Hand",
            _ => j.ToString(),
        };

        public static int XpToReach(int rank) => 40 * (rank - 1) * (rank - 1);

        public static int RankFromXp(int xp)
        {
            int r = 1;
            while (r < MaxRank && xp >= XpToReach(r + 1)) r++;
            return r;
        }

        public static string ProductName(Product p) => Products[p].Name;

        public static int SellPrice(Animal a)
        {
            var d = Animals[a.Kind];
            float m = a.Stage == Stage.Baby ? 0.4f : a.Stage == Stage.Prime ? 1.6f : 1f;
            if (a.Hunger < 0.35f) m *= 0.6f;   // hungry animals sell for less
            if (a.Rare) m *= 5f;
            return Mathf.Max(1, Mathf.RoundToInt(d.SellBase * m));
        }
    }

    public static class Palette
    {
        public static readonly Color Grass = new Color(0.34f, 0.54f, 0.22f);
        public static readonly Color GrassLush = new Color(0.27f, 0.6f, 0.18f);
        public static readonly Color GrassShort = new Color(0.55f, 0.62f, 0.28f);
        public static readonly Color Soil = new Color(0.4f, 0.29f, 0.18f);
        public static readonly Color Dirt = new Color(0.56f, 0.46f, 0.33f);
        public static readonly Color Road = new Color(0.45f, 0.41f, 0.36f);
        public static readonly Color Wood = new Color(0.52f, 0.36f, 0.2f);
        public static readonly Color DarkWood = new Color(0.32f, 0.21f, 0.12f);
        public static readonly Color BarnRed = new Color(0.62f, 0.13f, 0.1f);
        public static readonly Color Roof = new Color(0.28f, 0.25f, 0.24f);
        public static readonly Color Stone = new Color(0.56f, 0.55f, 0.52f);
        public static readonly Color White = new Color(0.95f, 0.94f, 0.9f);
        public static readonly Color Black = new Color(0.08f, 0.08f, 0.08f);
        public static readonly Color Red = new Color(0.85f, 0.12f, 0.1f);
        public static readonly Color Orange = new Color(0.95f, 0.55f, 0.1f);
        public static readonly Color Gold = new Color(1f, 0.8f, 0.2f);
        public static readonly Color Pink = new Color(0.95f, 0.66f, 0.66f);
        public static readonly Color Leaf = new Color(0.18f, 0.38f, 0.15f);
        public static readonly Color Straw = new Color(0.86f, 0.72f, 0.38f);
        public static readonly Color Skin = new Color(0.93f, 0.75f, 0.6f);
        public static readonly Color Jeans = new Color(0.22f, 0.32f, 0.55f);
        public static readonly Color Hair = new Color(0.35f, 0.2f, 0.1f);

        public static readonly Color UiGreen = new Color(0.5f, 0.95f, 0.45f);
        public static readonly Color UiRed = new Color(1f, 0.42f, 0.36f);
        public static readonly Color UiGold = new Color(1f, 0.84f, 0.3f);
        public static readonly Color UiWhite = new Color(0.95f, 0.95f, 0.95f);
    }
}
