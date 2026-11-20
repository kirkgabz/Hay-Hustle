using System.Collections.Generic;
using UnityEngine;

namespace HayHustle
{
    /// <summary>Eggs, wool and milk lying on the ground. Walk over them to collect; they spoil if left too long.</summary>
    public class ProductPickup : MonoBehaviour
    {
        public static readonly List<ProductPickup> All = new List<ProductPickup>();

        public Product Product { get; private set; }
        float life, nextFullWarning;
        Transform visual;

        public static ProductPickup Spawn(Product p, Vector3 pos)
        {
            var go = new GameObject(GameData.ProductName(p));
            go.transform.position = new Vector3(pos.x, 0f, pos.z);
            var pk = go.AddComponent<ProductPickup>();
            pk.Product = p;
            pk.life = GameData.Products[p].SpoilTime;
            pk.visual = Models.BuildProduct(p, go.transform);
            return pk;
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Update()
        {
            var gm = GameManager.I;
            float dt = Time.deltaTime;
            life -= dt;

            if (visual)
            {
                visual.localPosition = new Vector3(0f, 0.05f + Mathf.Abs(Mathf.Sin(Time.time * 3f)) * 0.08f, 0f);
                visual.Rotate(0f, 60f * dt, 0f);
                bool blinkOff = life < 10f && Mathf.Repeat(Time.time * 4f, 1f) < 0.35f; // about to spoil
                visual.gameObject.SetActive(!blinkOff);

            }

            if (life <= 0f)
            {
                gm.Notify($"{GameData.ProductName(Product)} spoiled on the ground! (spoiled food attracts rats)", Palette.UiRed);
                FX.Text(transform.position + Vector3.up, "Spoiled!", Palette.UiRed);
                FX.Burst(transform.position, new Color(0.4f, 0.32f, 0.2f), 6, 0.08f, 1.5f);
                gm.Mess++;
                Destroy(gameObject);
                return;
            }

            var player = gm.Player;
            if (player && GameManager.FlatDistance(player.transform.position, transform.position) < 1.8f)
            {
                if (gm.AddProduct(Product))
                {
                    gm.AddXp(1);
                    FX.Text(transform.position + Vector3.up * 1.2f, $"+1 {GameData.ProductName(Product)}", Color.white);
                    FX.Burst(transform.position + Vector3.up * 0.3f, Palette.Gold, 8, 0.07f, 2f);
                    FX.Play(Sfx.Pickup);
                    player.PlayAnim(ActAnim.Pickup, 0.35f);
                    Tutorial.Event("collect");
                    Destroy(gameObject);
                }
                else if (Time.time > nextFullWarning)
                {
                    nextFullWarning = Time.time + 4f;
                    gm.Notify("Warehouse is full! Sell goods or upgrade it.", Palette.UiRed);
                }
            }
        }
    }
}
