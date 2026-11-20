using UnityEngine;

namespace HayHustle
{
    /// <summary>Load goods, send the truck to town, get coins when it returns.</summary>
    public class Truck : Interactable
    {
        public const int Capacity = 20;
        public const float TripTime = 24f;

        public readonly int[] Cargo = new int[GameData.ProductCount];
        public bool Away { get; private set; }
        public int PendingPayment { get; private set; }

        float tripTimer;
        Vector3 home;
        Transform body;

        public void Init(Transform truckBody)
        {
            body = truckBody;
            home = body.position;
            Range = 4.2f;
        }

        public int CargoCount
        {
            get { int n = 0; foreach (var c in Cargo) n += c; return n; }
        }

        public int CargoValue
        {
            get
            {
                int v = 0;
                for (int i = 0; i < Cargo.Length; i++) v += Cargo[i] * GameManager.I.ProductPrice((Product)i);
                return v;
            }
        }

        public override string Prompt => Away
            ? $"Truck is delivering... back in {Mathf.CeilToInt(tripTimer)}s"
            : $"Truck - load goods and sell them in town ({CargoCount}/{Capacity})";

        public override bool CanInteract => !Away;
        public override void Interact(PlayerController player) => HUD.I.Open = HUD.Panel.Truck;

        public void Load(Product p, int n)
        {
            var gm = GameManager.I;
            n = Mathf.Min(n, gm.Inventory[(int)p], Capacity - CargoCount);
            if (n <= 0) return;
            gm.RemoveProduct(p, n);
            Cargo[(int)p] += n;
        }

        public void Unload(Product p)
        {
            var gm = GameManager.I;
            while (Cargo[(int)p] > 0 && gm.AddProduct(p)) Cargo[(int)p]--;
        }

        public void Send()
        {
            if (Away || CargoCount == 0) return;
            int items = CargoCount;
            PendingPayment = CargoValue;
            System.Array.Clear(Cargo, 0, Cargo.Length);
            Away = true;
            tripTimer = TripTime;
            GameManager.I.AddXp(items);
            GameManager.I.Notify($"The truck left with {items} goods worth {PendingPayment} coins.", Palette.UiGold);
            FX.Text(body.position + Vector3.up * 3f, $"Delivering {items} goods... +{PendingPayment} coins soon", Palette.UiGold);
            FX.Play(Sfx.Coin, 0.4f);
            Tutorial.Event("truck");
        }

        /// <summary>How many of this product the Driver keeps for your buildings/nest instead of selling.</summary>
        static int Reserve(GameManager gm, Product p)
        {
            if (p == Product.Egg) return 6; // nest + bakery
            foreach (var r in GameData.Recipes)
                if (r.Input == p && gm.Rank >= r.UnlockRank) return 6;
            return 0;
        }

        public int DriverSellableCount()
        {
            var gm = GameManager.I;
            int n = 0;
            for (int i = 0; i < gm.Inventory.Length; i++) n += Mathf.Max(0, gm.Inventory[i] - Reserve(gm, (Product)i));
            return n;
        }

        /// <summary>Driver: loads WANTED goods first, then the most valuable, and sends the truck. Returns items loaded.</summary>
        public int DriverLoadAndSend()
        {
            if (Away) return 0;
            var gm = GameManager.I;
            var order = new System.Collections.Generic.List<Product>();
            order.Add(gm.WantedProduct);
            var byPrice = new System.Collections.Generic.List<ProductDef>(GameData.Products.Values);
            byPrice.Sort((a, b) => b.Price.CompareTo(a.Price));
            foreach (var d in byPrice) if (d.Product != gm.WantedProduct) order.Add(d.Product);

            int loaded = 0;
            foreach (var p in order)
            {
                int spare = gm.Inventory[(int)p] - Reserve(gm, p);
                int n = Mathf.Min(spare, Capacity - CargoCount);
                if (n <= 0) continue;
                gm.RemoveProduct(p, n);
                Cargo[(int)p] += n;
                loaded += n;
            }
            if (CargoCount > 0) Send();
            return loaded;
        }

        public void ResetTruck()
        {
            System.Array.Clear(Cargo, 0, Cargo.Length);
            Away = false;
            PendingPayment = 0;
            if (body) { body.position = home; body.rotation = Quaternion.identity; }
        }

        void Update()
        {
            if (!Away) return;
            tripTimer -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(tripTimer / TripTime);
            float outAmount = t < 0.5f ? t * 2f : (1f - t) * 2f;
            body.position = new Vector3(home.x, home.y, Mathf.Lerp(home.z, home.z + 60f, Mathf.SmoothStep(0f, 1f, outAmount)));
            body.rotation = Quaternion.Euler(0f, t < 0.5f ? 0f : 180f, 0f);

            if (tripTimer <= 0f)
            {
                Away = false;
                body.position = home;
                body.rotation = Quaternion.identity;
                GameManager.I.AddCoins(PendingPayment);
                GameManager.I.Notify($"The truck is back: +{PendingPayment} coins!", Palette.UiGold, 5f);
                FX.Text(body.position + Vector3.up * 3f, $"+{PendingPayment} coins", Palette.UiGold, 1.5f);
                FX.Burst(body.position + Vector3.up * 2f, Palette.Gold, 18, 0.12f, 4f);
                FX.Play(Sfx.Coin, 0.7f);
                PendingPayment = 0;
            }
        }
    }
}
