using UnityEngine;

namespace HayHustle
{
    /// <summary>Hatch an egg into a chick (chicken breeding).</summary>
    public class Nest : Interactable
    {
        public const float HatchTime = 25f;
        float timer;
        bool hatching;
        Transform eggVisual;

        public void Init(Transform egg)
        {
            eggVisual = egg;
            eggVisual.gameObject.SetActive(false);
            Range = 2.5f;
        }

        public override string Prompt => hatching
            ? $"Nest - egg hatching... {Mathf.CeilToInt(timer)}s"
            : $"Nest - hatch an egg into a chick (have {GameManager.I.Inventory[(int)Product.Egg]} eggs)";

        public override bool CanInteract => !hatching;

        public override void Interact(PlayerController player)
        {
            var gm = GameManager.I;
            if (gm.FieldFull(AnimalKind.Chicken)) { gm.Notify("The Chicken Run is full!", Palette.UiRed); return; }
            if (!gm.HasProduct(Product.Egg, 1)) { gm.Notify("You need 1 egg in the warehouse.", Palette.UiRed); return; }
            gm.RemoveProduct(Product.Egg, 1);
            hatching = true;
            timer = HatchTime;
            eggVisual.gameObject.SetActive(true);
            player.PlayAnim(ActAnim.Pickup, 0.4f);
            FX.Text(transform.position + Vector3.up * 1.4f, $"Egg in the nest - hatching in {HatchTime:0}s", Palette.UiGold, 0.9f);
            FX.Play(Sfx.Plant);
        }

        void Update()
        {
            if (!hatching) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            hatching = false;
            eggVisual.gameObject.SetActive(false);
            var gm = GameManager.I;
            bool golden = Random.value < 0.03f;
            if (gm.FieldFull(AnimalKind.Chicken))
            {
                gm.AddProduct(Product.Egg);
                gm.Notify("The Chicken Run is full - the egg went back to the warehouse.", Palette.UiRed, 5f);
                FX.Text(transform.position + Vector3.up * 1.4f, "Chicken Run full!", Palette.UiRed);
                return;
            }
            gm.SpawnAnimal(AnimalKind.Chicken, Stage.Baby, transform.position + new Vector3(0f, 0f, -1.5f), golden);
            gm.AddXp(5);
            gm.Notify(golden ? "WOW! A rare Golden Hen chick hatched!" : "A chick hatched!", golden ? Palette.UiGold : Palette.UiGreen, 5f);
            FX.Text(transform.position + Vector3.up * 1.4f, golden ? "GOLDEN CHICK!" : "Chick hatched!", golden ? Palette.UiGold : Palette.UiGreen, 1.2f);
            FX.Burst(transform.position + Vector3.up * 0.4f, new Color(1f, 0.9f, 0.4f), 12, 0.08f, 2.5f);
            FX.Play(Sfx.Catch, 0.5f);
        }
    }
}
