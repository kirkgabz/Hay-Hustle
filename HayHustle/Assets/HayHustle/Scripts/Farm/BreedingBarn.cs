using UnityEngine;

namespace HayHustle
{
    /// <summary>Put 2 well-fed adult sheep or cows inside to get a baby.</summary>
    public class BreedingBarn : Interactable
    {
        public const float BreedTime = 40f;
        Animal a, b;
        float timer;

        bool Locked => GameManager.I.Rank < GameData.BarnUnlockRank;
        bool Busy => a != null;

        public override string Prompt
        {
            get
            {
                if (Locked) return $"Breeding Barn - unlocks at Farm Rank {GameData.BarnUnlockRank}";
                if (Busy) return $"Breeding Barn - {a.Def.Plural} are breeding... {Mathf.CeilToInt(timer)}s";
                if (FindPair(out var p, out _)) return $"Breeding Barn - breed 2 {p.Def.Plural}";
                var gm = GameManager.I;
                if (FindPair(out var q, out _, true) && gm.FieldFull(q.Kind))
                    return $"Breeding Barn - the {gm.FieldFor(q.Kind).Def.Name} is full, no room for a baby";
                return "Breeding Barn - needs 2 well-fed adult sheep or cows";
            }
        }

        public override bool CanInteract => !Locked && !Busy && FindPair(out _, out _);

        bool FindPair(out Animal first, out Animal second, bool ignoreRoom = false)
        {
            foreach (var kind in new[] { AnimalKind.Sheep, AnimalKind.Cow })
            {
                first = second = null;
                if (!ignoreRoom && GameManager.I.FieldFull(kind)) continue; // no room for a baby
                foreach (var an in GameManager.I.Animals)
                {
                    if (!an || an.Kind != kind || !an.CanBreed) continue;
                    if (first == null) first = an;
                    else { second = an; return true; }
                }
            }
            first = second = null;
            return false;
        }

        public override void Interact(PlayerController player)
        {
            var gm = GameManager.I;
            if (!FindPair(out a, out b)) return;
            a.EnterBarn();
            b.EnterBarn();
            timer = BreedTime;
            gm.Notify($"Two {a.Def.Plural} went into the barn.", Palette.UiGreen);
            player.PlayAnim(ActAnim.Use, 0.6f);
            FX.Text(transform.position + Vector3.up * 6f, $"Breeding... {BreedTime:0}s", Color.white);
            FX.Play(Sfx.Plant);
        }

        void Update()
        {
            if (!Busy) return;
            if (b == null) { a.ExitBarn(Door); a = null; return; }
            timer -= Time.deltaTime;
            if (timer > 0f) return;

            var gm = GameManager.I;
            bool bothPrime = a.Stage == Stage.Prime && b.Stage == Stage.Prime;
            bool rare = Random.value < (bothPrime ? 0.1f : 0.02f);
            var kind = a.Kind;
            int field = a.Field;
            a.ExitBarn(Door + transform.right * 1.2f);
            b.ExitBarn(Door - transform.right * 1.2f);
            a = b = null;
            if (gm.FieldFull(kind))
            {
                // The field filled up while they were breeding: sell the baby instead.
                var def = GameData.Animals[kind];
                int coins = Mathf.RoundToInt(def.SellBase * 0.4f * (rare ? 5f : 1f));
                gm.AddCoins(coins);
                gm.Notify($"A baby {def.BabyName.ToLower()} was born, but the {gm.FieldFor(kind).Def.Name} is full - sold for {coins} coins.", Palette.UiGold, 6f);
                FX.Text(Door + Vector3.up * 1.5f, $"Field full: +{coins} coins", Palette.UiGold);
                FX.Play(Sfx.Coin, 0.5f);
                return;
            }
            var baby = gm.SpawnAnimal(kind, Stage.Baby, Door, rare, field);
            gm.AddXp(10);
            FX.Text(Door + Vector3.up * 1.5f, rare ? $"RARE {baby.Def.RareName}!" : $"Baby {baby.Def.BabyName.ToLower()} born!", rare ? Palette.UiGold : Palette.UiGreen, 1.2f);
            FX.Burst(Door + Vector3.up * 0.5f, rare ? Palette.Gold : Palette.Pink, 14, 0.1f, 3f);
            FX.Play(Sfx.Catch, 0.5f);
            gm.Notify(rare ? $"A RARE {baby.Def.RareName} was born!" : $"A baby {baby.Def.BabyName.ToLower()} was born!",
                rare ? Palette.UiGold : Palette.UiGreen, 5f);
        }

        Vector3 Door => transform.position + transform.forward * 4.5f;
    }
}
