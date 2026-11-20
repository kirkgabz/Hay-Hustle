using UnityEngine;

namespace HayHustle
{
    /// <summary>"For sale" sign on a field you don't own yet. Each field is the home of one kind of animal.</summary>
    public class FieldSign : Interactable
    {
        public int FieldIndex;

        FieldDef Def => GameData.Fields[FieldIndex];
        string Animals => GameData.Animals[Def.Kind].Plural;

        public override string Prompt
        {
            get
            {
                var gm = GameManager.I;
                if (gm.Rank < Def.UnlockRank) return $"{Def.Name} (for {Animals}) - unlocks at Farm Rank {Def.UnlockRank}";
                return $"Buy the {Def.Name} for {Def.Price} coins - home for up to {Def.Capacity} {Animals}";
            }
        }

        public override bool CanInteract => GameManager.I.Rank >= Def.UnlockRank;

        public override void Interact(PlayerController player)
        {
            var gm = GameManager.I;
            player.PlayAnim(ActAnim.Use, 0.5f);
            HUD.I.Confirm($"Buy the {Def.Name}?",
                $"Pay {Def.Price} coins for a fenced field with {Def.Cols * Def.Rows} grass plots. It is the home for up to {Def.Capacity} {Animals}.",
                "Buy field", () => gm.BuyField(FieldIndex));
        }
    }
}
