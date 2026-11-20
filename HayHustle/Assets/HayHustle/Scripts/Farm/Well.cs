using UnityEngine;

namespace HayHustle
{
    /// <summary>Refills the water bucket used for planting grass.</summary>
    public class Well : Interactable
    {
        public override string Prompt
        {
            get
            {
                var gm = GameManager.I;
                return gm.Water >= gm.WaterMax
                    ? $"Well - your bucket is full ({gm.Water}/{gm.WaterMax})"
                    : $"Refill water bucket ({gm.Water}/{gm.WaterMax})";
            }
        }

        public override bool CanInteract => GameManager.I.Water < GameManager.I.WaterMax;

        public override void Interact(PlayerController player)
        {
            var gm = GameManager.I;
            int added = gm.WaterMax - gm.Water;
            player.SetBusy(1.2f, "Filling the bucket...");
            player.PlayAnim(ActAnim.Fill, 1.2f);
            gm.Water = gm.WaterMax;
            FX.Text(transform.position + Vector3.up * 2.8f, $"+{added} water  (bucket {gm.Water}/{gm.WaterMax})", new Color(0.5f, 0.8f, 1f), 1.1f);
            FX.Burst(transform.position + Vector3.up * 1f, new Color(0.35f, 0.6f, 0.95f), 14, 0.09f, 2.5f);
            FX.Play(Sfx.Water, 0.7f);
        }
    }
}
