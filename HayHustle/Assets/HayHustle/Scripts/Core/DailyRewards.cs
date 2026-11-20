using System.Globalization;
using UnityEngine;

namespace HayHustle
{
    /// <summary>
    /// 7-day login calendar. Missing a day resets the streak to Day 1.
    /// NOTE: this uses the device clock for now. When the Supabase backend is connected,
    /// claiming moves to the server function claim_daily_reward() which uses server time,
    /// so players cannot cheat by changing their phone/PC date.
    /// </summary>
    public static class DailyRewards
    {
        public class Reward
        {
            public string Label;
            public System.Action<GameManager> Give;
        }

        public static readonly Reward[] Days =
        {
            new Reward { Label = "100 coins", Give = g => g.AddCoins(100) },
            new Reward { Label = "Full water + 50 coins", Give = g => { g.Water = g.WaterMax; g.AddCoins(50); } },
            new Reward { Label = "Free chick", Give = g => g.SpawnAnimal(AnimalKind.Chicken, Stage.Baby, g.RandomPasturePoint()) },
            new Reward { Label = "250 coins", Give = g => g.AddCoins(250) },
            new Reward { Label = "6 eggs", Give = g => { if (!g.AddProduct(Product.Egg, 6)) g.AddCoins(60); } },
            new Reward { Label = "Free adult sheep", Give = g => g.SpawnAnimal(AnimalKind.Sheep, Stage.Adult, g.RandomPasturePoint()) },
            new Reward { Label = "Mystery chest", Give = OpenChest },
        };

        static void OpenChest(GameManager g)
        {
            float r = Random.value;
            if (r < 0.25f)
            {
                g.SpawnAnimal(AnimalKind.Chicken, Stage.Adult, g.RandomPasturePoint(), true);
                g.Notify("MYSTERY CHEST: a rare Golden Hen joined your farm!", Palette.UiGold, 6f);
            }
            else if (r < 0.6f)
            {
                g.AddCoins(600);
                g.Notify("MYSTERY CHEST: 600 coins!", Palette.UiGold, 6f);
            }
            else
            {
                g.AddCoins(300);
                g.Water = g.WaterMax;
                g.Notify("MYSTERY CHEST: 300 coins and a full bucket!", Palette.UiGold, 6f);
            }
        }

        static string Today => System.DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        public static bool CanClaim(GameManager g) => g.LastRewardDate != Today;

        /// <summary>0-based index of the next reward to claim.</summary>
        public static int NextIndex(GameManager g)
        {
            if (string.IsNullOrEmpty(g.LastRewardDate)) return 0;
            if (!System.DateTime.TryParseExact(g.LastRewardDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var last)) return 0;
            int gap = (System.DateTime.Now.Date - last.Date).Days;
            if (gap <= 1) return g.RewardStreak % 7;
            return 0; // missed a day: streak resets
        }

        public static void Claim(GameManager g)
        {
            if (!CanClaim(g)) return;
            int i = NextIndex(g);
            Days[i].Give(g);
            g.RewardStreak = i + 1;
            g.LastRewardDate = Today;
            g.Notify($"Day {i + 1} reward: {Days[i].Label}!", Palette.UiGold, 5f);
            FX.Play(Sfx.RankUp, 0.6f);
            if (g.Player) FX.Burst(g.Player.transform.position + Vector3.up * 1.5f, Palette.Gold, 20, 0.12f, 4f);
            g.SaveGame();
        }
    }
}
