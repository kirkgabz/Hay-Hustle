using UnityEngine;

namespace HayHustle
{
    /// <summary>Stores collected goods. Click it to open the warehouse window (Quick Sell and upgrades).</summary>
    public class Warehouse : Interactable
    {
        public static int UpgradeCost => 150 + (GameManager.I.WarehouseCapacity - 40) / 20 * 150;

        public override string Prompt
        {
            get
            {
                var gm = GameManager.I;
                return $"Warehouse {gm.InventoryTotal}/{gm.WarehouseCapacity} - quick sell goods or upgrade";
            }
        }

        public override void Interact(PlayerController player)
        {
            player.PlayAnim(ActAnim.Use, 0.4f);
            HUD.I.Open = HUD.Panel.Warehouse;
        }

        public static void Upgrade()
        {
            var gm = GameManager.I;
            int cost = UpgradeCost;
            if (!gm.Spend(cost)) return;
            gm.WarehouseCapacity += 20;
            gm.AddXp(5);
            gm.Notify($"Warehouse upgraded! Space: {gm.WarehouseCapacity}", Palette.UiGreen);
            if (gm.WarehouseTransform)
            {
                FX.Text(gm.WarehouseTransform.position + Vector3.up * 5f, $"+20 space ({gm.WarehouseCapacity})", Palette.UiGreen, 1.2f);
                FX.Burst(gm.WarehouseTransform.position + Vector3.up * 3f, Palette.Gold, 12, 0.12f, 3f);
            }
            FX.Play(Sfx.Coin);
        }
    }
}
