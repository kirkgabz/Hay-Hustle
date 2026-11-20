namespace HayHustle
{
    /// <summary>Notice board in the yard with villager orders.</summary>
    public class OrderBoard : Interactable
    {
        public override string Prompt
        {
            get
            {
                var book = GameManager.I.Orders;
                int ready = 0;
                foreach (var o in book.Orders) if (book.CanDeliver(GameManager.I, o)) ready++;
                return $"Order Board - {book.Orders.Count} order{(book.Orders.Count == 1 ? "" : "s")}" + (ready > 0 ? $", {ready} ready to deliver!" : "");
            }
        }

        public override void Interact(PlayerController player)
        {
            player.PlayAnim(ActAnim.Use, 0.4f);
            HUD.I.Open = HUD.Panel.Orders;
        }
    }
}
