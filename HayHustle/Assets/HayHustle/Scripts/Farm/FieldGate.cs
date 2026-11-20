using UnityEngine;

namespace HayHustle
{
    /// <summary>
    /// The main gate in the wall around the whole farm (10,000 health). Zombies, foxes, wolves and bears hit it
    /// continuously to get in. It swings open for the truck and your people, and shuts when danger is near.
    /// Repairing takes time and wood: 100 health per minute for each person working on it (they stack),
    /// using 1 wood per 100 health. Click it to repair it yourself, or let Builders do it.
    /// </summary>
    public class FieldGate : Interactable
    {
        public float Hp { get; private set; } = GameData.GateMaxHp;
        public bool Intact => Hp > 0f;
        public string FieldName { get; private set; }

        /// <summary>The spot just outside the gate (where attackers hit it and builders repair it).</summary>
        public Vector3 RepairPoint => transform.position + transform.forward * 1.3f;

        Transform left, right;
        float nextHitFx, nextRepairFx, halfWidth = 2f, open;

        public void Init(string fieldName, float halfWidth = 2f)
        {
            FieldName = fieldName;
            this.halfWidth = halfWidth;
            Range = 2.8f + (halfWidth - 2f);
            left = Board(-1f);
            right = Board(1f);
            GameManager.I.Gates.Add(this);
            Refresh();
        }

        Transform Board(float side)
        {
            var hinge = new GameObject("Gate Half").transform;
            hinge.SetParent(transform, false);
            float w = halfWidth;
            hinge.localPosition = new Vector3(w * side, 0f, 0f);
            foreach (float h in new[] { 0.35f, 0.8f, 1.25f, 1.7f })
                Visuals.Prim(PrimitiveType.Cube, hinge, new Vector3(-w / 2f * side, h, 0f), new Vector3(w - 0.05f, 0.16f, 0.1f), Palette.Wood);
            for (int i = 0; i < 3; i++)   // upright planks
                Visuals.Prim(PrimitiveType.Cube, hinge, new Vector3(-(0.15f + i * (w - 0.3f) / 2f) * side, 1.0f, 0.04f), new Vector3(0.14f, 1.9f, 0.06f), Palette.DarkWood);
            Visuals.Prim(PrimitiveType.Cube, hinge, new Vector3(-w / 2f * side, 1.0f, -0.05f), new Vector3(0.12f, 1.6f, 0.06f), Palette.DarkWood, false, new Vector3(0f, 0f, 45f * side));
            return hinge;
        }

        void OnDestroy()
        {
            if (GameManager.I) GameManager.I.Gates.Remove(this);
        }

        public override string Prompt =>
            Hp >= GameData.GateMaxHp ? $"{FieldName} gate - strong ({Hp:0}/{GameData.GateMaxHp:0})"
            : Hp <= 0f ? $"{FieldName} gate is BROKEN - click to repair it (uses wood)"
            : $"Repair the {FieldName} gate ({Hp:0}/{GameData.GateMaxHp:0}) - uses wood";

        public override bool CanInteract => Hp < GameData.GateMaxHp;

        public override void Interact(PlayerController player)
        {
            if (GameManager.I.Wood < 0.05f)
            {
                GameManager.I.Notify("No wood! Chop a tree first (click a tree in the groves beside the fields).", Palette.UiRed);
                FX.Play(Sfx.Error, 0.4f);
                return;
            }
            player.StartRepair(this);
        }

        public void SetHp(float hp)
        {
            Hp = Mathf.Clamp(hp, 0f, GameData.GateMaxHp);
            Refresh();
        }

        /// <summary>
        /// One person works on the gate for 'seconds' (speed = skill bonus). Uses wood.
        /// Returns false when there is no wood left or the gate is already whole.
        /// </summary>
        public bool RepairWork(float seconds, float speed = 1f)
        {
            var gm = GameManager.I;
            if (Hp >= GameData.GateMaxHp) return false;
            float hp = GameData.GateRepairPerMinute / 60f * seconds * speed;
            hp = Mathf.Min(hp, GameData.GateMaxHp - Hp);
            float wood = hp / 100f * GameData.WoodPer100Hp;
            if (gm.Wood < wood)
            {
                if (gm.Wood <= 0f) return false;
                hp *= gm.Wood / wood;   // use what is left
                wood = gm.Wood;
            }
            gm.Wood -= wood;
            bool wasBroken = Hp <= 0f;
            Hp = Mathf.Min(GameData.GateMaxHp, Hp + hp);
            if (Time.time > nextRepairFx)
            {
                nextRepairFx = Time.time + 1.2f;
                FX.Burst(transform.position + Vector3.up * 0.6f, Palette.Wood, 3, 0.06f, 1.5f);
                FX.Play(Sfx.Hit, 0.15f);
            }
            if (wasBroken && Hp > 0f) GameManager.I.Notify($"The {FieldName} gate is standing again.", Palette.UiGreen);
            if (Hp >= GameData.GateMaxHp) FX.Text(transform.position + Vector3.up * 2f, "Gate fully repaired!", Palette.UiGreen, 1.1f);
            Refresh();
            return true;
        }

        public void Damage(float amount)
        {
            if (Hp <= 0f) return;
            Hp = Mathf.Max(0f, Hp - amount);
            if (Time.time > nextHitFx)
            {
                nextHitFx = Time.time + 0.6f;
                FX.Burst(transform.position + Vector3.up * 0.6f, Palette.Wood, 5, 0.07f, 2.5f);
                FX.Play(Sfx.Hit, 0.3f);
            }
            if (Hp <= 0f)
            {
                GameManager.I.Notify($"The {FieldName} gate was BROKEN! Predators can walk into the farm - guards, defend it, then repair it with wood.", Palette.UiRed, 7f);
                FX.Text(transform.position + Vector3.up * 2f, "GATE BROKEN!", Palette.UiRed, 1.3f);
                FX.Play(Sfx.Grab, 0.6f);
            }
            Refresh();
        }

        void Refresh()
        {
            if (!left) return;
            bool up = Hp > 0f;
            left.gameObject.SetActive(up);
            right.gameObject.SetActive(up);
            float damage = 1f - Hp / GameData.GateMaxHp;
            float swing = open * 85f;   // opens inwards, into the farm
            left.localRotation = Quaternion.Euler(0f, swing, -damage * 12f);
            right.localRotation = Quaternion.Euler(0f, -swing, damage * 9f);
        }

        /// <summary>Open for the truck and your people; shut tight while a predator or zombie is close.</summary>
        void Update()
        {
            var gm = GameManager.I;
            if (!gm || !left || Hp <= 0f) return;
            Vector3 p = transform.position;
            bool danger = false;
            foreach (var pr in gm.Predators)
                if (pr && GameManager.FlatDistance(pr.transform.position, p) < 12f) { danger = true; break; }
            bool want = false;
            if (!danger)
            {
                if (gm.Truck && GameManager.FlatDistance(gm.Truck.BodyPosition, p) < 10f) want = true;
                else if (gm.Player && GameManager.FlatDistance(gm.Player.transform.position, p) < 3.5f && Hp >= GameData.GateMaxHp) want = true;
                else
                {
                    foreach (var s in gm.Survivors) if (s && GameManager.FlatDistance(s.transform.position, p) < 4f) { want = true; break; }
                    if (!want && gm.Visitor && GameManager.FlatDistance(gm.Visitor.transform.position, p) < 4f) want = true;
                    if (!want) foreach (var w in gm.Workers) if (w && GameManager.FlatDistance(w.transform.position, p) < 4f) { want = true; break; }
                }
            }
            float target = want ? 1f : 0f;
            if (Mathf.Approximately(open, target)) return;
            open = Mathf.MoveTowards(open, target, Time.deltaTime * (want ? 1.2f : 2.5f));
            Refresh();
        }
    }
}
