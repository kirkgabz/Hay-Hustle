using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HayHustle
{
    /// <summary>
    /// Sends predator waves from the forest. Waves get more frequent and bigger as the
    /// farm grows (Farm Rank, number of animals) and at night. Each wave is announced
    /// a few seconds early with edge-of-screen warnings.
    /// Special waves: rat swarms (when the warehouse has goods / spoiled food is lying around)
    /// and the bear (night boss).
    /// </summary>
    public class PredatorSpawner : MonoBehaviour
    {
        public class Warning
        {
            public PredatorKind Kind;
            public Vector3 Position;
            public float SpawnAt;
        }

        public readonly List<Warning> Warnings = new List<Warning>();
        float nextWave = 40f;

        public void ResetTimer()
        {
            nextWave = 40f; // short grace period after starting
            Warnings.Clear();
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || gm.Mode != GameMode.Playing) return;

            if (Tutorial.HoldsPredators) return; // no attacks while learning the basics
            nextWave -= Time.deltaTime;
            if (nextWave <= 0f)
            {
                ScheduleWave(gm);
                nextWave = NextInterval(gm);
            }

            for (int i = Warnings.Count - 1; i >= 0; i--)
            {
                if (Time.time < Warnings[i].SpawnAt) continue;
                Spawn(Warnings[i]);
                Warnings.RemoveAt(i);
            }
        }

        float NextInterval(GameManager gm)
        {
            float t = (gm.Rank - 1) / (float)(GameData.MaxRank - 1);
            float interval = Mathf.Lerp(85f, 40f, t);
            if (gm.CountWorkers(WorkerKind.Guard) == 0) interval *= 1.3f; // gentle until you have a guard
            interval *= Mathf.Lerp(1.15f, 0.8f, Mathf.Clamp01(gm.Animals.Count / 25f)); // bigger farm attracts more
            if (gm.DayNight.IsNight) interval *= 0.6f;
            return interval * Random.Range(0.8f, 1.2f);
        }

        static bool HasPrey(GameManager gm, PredatorDef d)
        {
            if (d.Kind == PredatorKind.Snake && ProductPickup.All.Any(p => p && p.Product == Product.Egg)) return true;
            foreach (var k in d.Targets) if (gm.CountAnimals(k) > 0) return true;
            return false;
        }

        void ScheduleWave(GameManager gm)
        {
            bool night = gm.DayNight.IsNight;
            bool guarded = gm.CountWorkers(WorkerKind.Guard) > 0;
            var plan = new List<PredatorKind>();

            // Normal hunters.
            var options = new List<PredatorDef>();
            foreach (var d in GameData.Predators.Values)
            {
                if (d.Kind == PredatorKind.Rat || d.Kind == PredatorKind.Bear) continue; // special waves below
                if (gm.Rank < d.MinRank || (d.NightOnly && !night)) continue;
                if (!guarded && d.Kind != PredatorKind.Fox) continue; // only foxes until you hire a guard
                if (HasPrey(gm, d)) options.Add(d);
            }
            int count = guarded ? Mathf.Min(2, 1 + (gm.Rank - 1) / 6 + (night ? 1 : 0)) : 1;
            for (int i = 0; i < count && options.Count > 0; i++)
                plan.Add(options[Random.Range(0, options.Count)].Kind);

            // Rat swarm: comes for the warehouse. Spoiled food left around attracts them.
            var rat = GameData.Predators[PredatorKind.Rat];
            if (guarded && gm.Rank >= rat.MinRank && gm.InventoryTotal > 0 && (gm.Mess > 0 || Random.value < 0.3f))
            {
                int rats = Random.Range(3, 6);
                for (int i = 0; i < rats; i++) plan.Add(PredatorKind.Rat);
                gm.Mess = Mathf.Max(0, gm.Mess - 3);
            }

            // Bear: rare night boss.
            var bear = GameData.Predators[PredatorKind.Bear];
            bool bearComing = guarded && gm.Rank >= bear.MinRank && night && HasPrey(gm, bear) && Random.value < 0.3f;
            if (bearComing) plan.Add(PredatorKind.Bear);

            if (plan.Count == 0) return;

            bool sheepAlarm = gm.CountAnimals(AnimalKind.Sheep, true) > 0;
            float t = 3f + (sheepAlarm ? 1.5f : 0f);
            float baseAngle = Random.Range(0f, 360f);
            foreach (var kind in plan)
            {
                float ang = (baseAngle + Random.Range(-40f, 40f)) * Mathf.Deg2Rad; // a wave comes from roughly one side
                Vector3 dir = new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang));
                Warnings.Add(new Warning { Kind = kind, Position = GameManager.PastureCenter + dir * GameData.SpawnRadius, SpawnAt = Time.time + t });
                t += kind == PredatorKind.Rat ? 0.4f : 1.5f;
            }

            string[] compass = { "north", "east", "south", "west" };
            string from = compass[Mathf.RoundToInt(baseAngle / 90f) % 4];
            string who = string.Join(", ", plan.Distinct().Select(k => GameData.Predators[k].Name.ToLower() + (k == PredatorKind.Rat ? "s" : "")));
            string early = sheepAlarm ? " (your sheep heard them early)" : "";
            FX.Play(Sfx.Warn, 0.55f);
            if (bearComing) gm.Notify($"DANGER: a BEAR is coming from the {from}!", Palette.UiRed, 7f);
            gm.Notify($"WARNING: {who} coming from the {from}!{early}", Palette.UiRed, 5f);
        }

        void Spawn(Warning w)
        {
            var go = new GameObject(GameData.Predators[w.Kind].Name);
            go.transform.position = w.Position;
            Vector3 look = GameManager.PastureCenter - w.Position;
            look.y = 0f;
            go.transform.rotation = Quaternion.LookRotation(look);
            go.AddComponent<Predator>().Init(w.Kind);
        }
    }
}
