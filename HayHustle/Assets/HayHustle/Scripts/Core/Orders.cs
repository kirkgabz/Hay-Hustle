using System.Collections.Generic;
using UnityEngine;

namespace HayHustle
{
    public class OrderItem
    {
        public Product Product;
        public int Amount;
    }

    public class Order
    {
        public string Customer;
        public readonly List<OrderItem> Items = new List<OrderItem>();
        public int Reward, Xp;
        public float TimeLeft;
    }

    /// <summary>Villager orders on the Order Board: deliver the goods before time runs out for bonus coins.</summary>
    public class OrderBook
    {
        public const int MaxOrders = 3;
        public readonly List<Order> Orders = new List<Order>();
        float refill = 5f;

        static readonly string[] Customers = { "Baker Rosa", "Old Tom", "Mayor Lina", "Chef Marco", "Granny Pia", "Teacher Ben", "Tailor Ana", "Innkeeper Sol" };

        public void Clear()
        {
            Orders.Clear();
            refill = 5f;
        }

        public void Tick(GameManager gm, float dt)
        {
            for (int i = Orders.Count - 1; i >= 0; i--)
            {
                Orders[i].TimeLeft -= dt;
                if (Orders[i].TimeLeft <= 0f)
                {
                    gm.Notify($"{Orders[i].Customer}'s order expired.", Palette.UiRed);
                    Orders.RemoveAt(i);
                }
            }
            if (Orders.Count >= MaxOrders) return;
            refill -= dt;
            if (refill > 0f) return;
            refill = 25f;
            Orders.Add(Generate(gm));
            gm.Notify("New order on the Order Board!", Palette.UiGold);
        }

        static Order Generate(GameManager gm)
        {
            var unlocked = new List<ProductDef>();
            foreach (var d in GameData.Products.Values) if (d.UnlockRank <= gm.Rank && gm.Obtainable(d.Product)) unlocked.Add(d);
            if (unlocked.Count == 0) unlocked.Add(GameData.Products[Product.Egg]);

            var o = new Order { Customer = Customers[Random.Range(0, Customers.Length)] };
            int count = Mathf.Min(unlocked.Count, Random.Range(1, gm.Rank >= 3 ? 4 : 3));
            int value = 0;
            for (int i = 0; i < count; i++)
            {
                var d = unlocked[Random.Range(0, unlocked.Count)];
                if (o.Items.Exists(x => x.Product == d.Product)) continue;
                int amount = d.Price < 50 ? Random.Range(3, 8) : Random.Range(1, 4);
                o.Items.Add(new OrderItem { Product = d.Product, Amount = amount });
                value += d.Price * amount;
            }
            o.Reward = Mathf.RoundToInt(value * 1.6f / 5f) * 5 + 20;
            o.Xp = 10 + o.Items.Count * 5;
            o.TimeLeft = 180f + 60f * o.Items.Count;
            return o;
        }

        public bool CanDeliver(GameManager gm, Order o)
        {
            foreach (var it in o.Items) if (!gm.HasProduct(it.Product, it.Amount)) return false;
            return true;
        }

        public void Deliver(GameManager gm, Order o)
        {
            if (!Orders.Contains(o) || !CanDeliver(gm, o)) return;
            foreach (var it in o.Items) gm.RemoveProduct(it.Product, it.Amount);
            Orders.Remove(o);
            gm.AddCoins(o.Reward);
            gm.AddXp(o.Xp);
            gm.Notify($"Order delivered to {o.Customer}: +{o.Reward} coins!", Palette.UiGold, 5f);
            FX.Play(Sfx.Coin, 0.7f);
            if (gm.OrderBoardTransform)
            {
                FX.Text(gm.OrderBoardTransform.position + Vector3.up * 3f, $"+{o.Reward} coins", Palette.UiGold, 1.4f);
                FX.Burst(gm.OrderBoardTransform.position + Vector3.up * 2f, Palette.Gold, 18, 0.12f, 4f);
            }
            refill = Mathf.Min(refill, 15f);
        }

        public void Discard(Order o)
        {
            Orders.Remove(o);
            refill = Mathf.Max(refill, 30f);
        }
    }
}
