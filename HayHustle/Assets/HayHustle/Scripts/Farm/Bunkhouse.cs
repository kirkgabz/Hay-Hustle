namespace HayHustle
{
    /// <summary>Where survivors sleep and heal. Click it to open the Camp window.</summary>
    public class Bunkhouse : Interactable
    {
        public override string Prompt
        {
            get
            {
                var gm = GameManager.I;
                return $"Bunkhouse - {gm.Survivors.Count}/{gm.Beds} beds - manage survivors";
            }
        }

        public override void Interact(PlayerController player)
        {
            player.PlayAnim(ActAnim.Use, 0.4f);
            HUD.I.Open = HUD.Panel.Camp;
        }
    }
}
