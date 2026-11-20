using UnityEngine;

namespace HayHustle
{
    /// <summary>Stores collected goods. Upgrade for more space.</summary>
    public class Warehouse : Interactable
    {
        int UpgradeCost => 150 + (GameManager.I.WarehouseCapacity - 40) / 20 * 150;

        public override string Prompt
        {
            get
            {
                var gm = GameManager.I;
                return $"Warehouse {gm.InventoryTotal}/{gm.WarehouseCapacity} - upgrade +20 space for {UpgradeCost} coins";
            }
        }

        public override void Interact(PlayerController player)
        {
            var gm = GameManager.I;
            if (!gm.Spend(UpgradeCost)) return;
            gm.WarehouseCapacity += 20;
            gm.AddXp(5);
            gm.Notify($"Warehouse upgraded! Space: {gm.WarehouseCapacity}", Palette.UiGreen);
            player.PlayAnim(ActAnim.Use, 0.6f);
            FX.Text(transform.position + Vector3.up * 5f, $"+20 space ({gm.WarehouseCapacity})", Palette.UiGreen, 1.2f);
            FX.Burst(transform.position + Vector3.up * 3f, Palette.Gold, 12, 0.12f, 3f);
            FX.Play(Sfx.Coin);
        }
    }
}
