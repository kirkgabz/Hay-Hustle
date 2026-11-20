using System.Collections;
using UnityEngine;

namespace HayHustle
{
    /// <summary>
    /// A tree on the farm that gives wood (the material for repairing gates).
    /// Click it to chop it yourself, or Builders chop it. The stump grows back after a few minutes.
    /// </summary>
    public class ChoppableTree : Interactable
    {
        public bool Available => !stump && !beingChopped;

        Transform tree, stumpObj;
        bool stump, beingChopped;
        float regrow;

        public void Init(float size)
        {
            Range = 2.6f;
            tree = new GameObject("Tree").transform;
            tree.SetParent(transform, false);
            Color bark = Palette.DarkWood, leaf = new Color(0.2f, 0.42f, 0.17f);
            Visuals.Skin(Visuals.Prim(PrimitiveType.Cylinder, tree, new Vector3(0f, 1.3f * size, 0f), new Vector3(0.4f, 1.3f, 0.4f) * size, bark), bark, Surface.Wood, Vector2.one);
            Visuals.Skin(Visuals.Prim(PrimitiveType.Sphere, tree, new Vector3(0f, 3.3f * size, 0f), new Vector3(2.8f, 2.4f, 2.8f) * size, leaf), leaf, Surface.Leaves, new Vector2(2f, 2f));
            Visuals.Skin(Visuals.Prim(PrimitiveType.Sphere, tree, new Vector3(0.5f, 2.7f * size, 0.3f), new Vector3(1.8f, 1.5f, 1.8f) * size, leaf), leaf, Surface.Leaves, new Vector2(2f, 2f));
            stumpObj = Visuals.Skin(Visuals.Prim(PrimitiveType.Cylinder, transform, new Vector3(0f, 0.2f, 0f), new Vector3(0.45f, 0.2f, 0.45f) * size, bark), bark, Surface.Wood, Vector2.one).transform;
            stumpObj.gameObject.SetActive(false);
            GameManager.I.Trees.Add(this);
        }

        void OnDestroy()
        {
            if (GameManager.I) GameManager.I.Trees.Remove(this);
        }

        public override bool Selectable => true;

        public override string Prompt =>
            stump ? $"Tree stump - grows back in {Mathf.CeilToInt(regrow)}s"
            : beingChopped ? "Someone is chopping this tree..."
            : $"Chop the tree (+{GameData.WoodPerTree} wood for gate repairs)";

        public override bool CanInteract => Available;

        public override void Interact(PlayerController player)
        {
            if (!Available) return;
            player.SetBusy(GameData.PlayerChopSeconds, "Chopping wood...");
            player.PlayAnim(ActAnim.Chop, GameData.PlayerChopSeconds);
            StartCoroutine(Chopping(GameData.PlayerChopSeconds));
        }

        /// <summary>Starts chopping (used by Builders). The wood is added when the tree falls.</summary>
        public void BeginChop(float seconds)
        {
            if (!Available) return;
            StartCoroutine(Chopping(seconds));
        }

        IEnumerator Chopping(float seconds)
        {
            beingChopped = true;
            float t = 0f, nextThunk = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                // the tree shakes with every hit
                tree.localRotation = Quaternion.Euler(Mathf.Sin(t * 22f) * 2.5f, 0f, Mathf.Cos(t * 17f) * 2f);
                if (t >= nextThunk)
                {
                    nextThunk = t + 0.7f;
                    FX.Play(Sfx.Hit, 0.25f);
                    FX.Burst(transform.position + Vector3.up * 1.2f, Palette.Wood, 4, 0.06f, 2f);
                }
                yield return null;
            }
            beingChopped = false;
            Fell();
        }

        void Fell()
        {
            var gm = GameManager.I;
            stump = true;
            regrow = GameData.TreeRegrowSeconds;
            tree.localRotation = Quaternion.identity;
            tree.gameObject.SetActive(false);
            stumpObj.gameObject.SetActive(true);
            gm.Wood += GameData.WoodPerTree;
            gm.AddXp(2);
            FX.Text(transform.position + Vector3.up * 2.2f, $"+{GameData.WoodPerTree} wood  (total {gm.Wood:0})", new Color(0.85f, 0.65f, 0.4f), 1.1f);
            FX.Burst(transform.position + Vector3.up * 1.5f, new Color(0.2f, 0.42f, 0.17f), 14, 0.1f, 3f);
            FX.Play(Sfx.Catch, 0.3f);
        }

        void Update()
        {
            if (!stump) return;
            regrow -= Time.deltaTime;
            if (regrow > 0f) return;
            stump = false;
            tree.gameObject.SetActive(true);
            stumpObj.gameObject.SetActive(false);
        }
    }
}
