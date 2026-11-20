using UnityEngine;

namespace HayHustle
{
    /// <summary>
    /// The gate of a fenced field (10,000 health). Zombies, foxes, wolves and bears hit it continuously to get in.
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
        float nextHitFx, nextRepairFx;

        public void Init(string fieldName)
        {
            FieldName = fieldName;
            Range = 2.8f;
            left = Board(-1f);
            right = Board(1f);
            GameManager.I.Gates.Add(this);
            Refresh();
        }

        Transform Board(float side)
        {
            var hinge = new GameObject("Gate Half").transform;
            hinge.SetParent(transform, false);
            hinge.localPosition = new Vector3(2f * side, 0f, 0f);
            foreach (float h in new[] { 0.3f, 0.6f, 0.9f })
                Visuals.Prim(PrimitiveType.Cube, hinge, new Vector3(-1f * side, h, 0f), new Vector3(1.95f, 0.12f, 0.08f), Palette.Wood);
            Visuals.Prim(PrimitiveType.Cube, hinge, new Vector3(-1f * side, 0.6f, 0f), new Vector3(0.1f, 0.75f, 0.06f), Palette.DarkWood, false, new Vector3(0f, 0f, 55f * side));
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
                GameManager.I.Notify("No wood! Chop a tree first (click a tree near the fields).", Palette.UiRed);
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
                GameManager.I.Notify($"The {FieldName} gate was BROKEN! Guards, defend it - then repair it with wood.", Palette.UiRed, 7f);
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
            left.localRotation = Quaternion.Euler(0f, 0f, -damage * 12f);
            right.localRotation = Quaternion.Euler(0f, 0f, damage * 9f);
        }
    }
}
