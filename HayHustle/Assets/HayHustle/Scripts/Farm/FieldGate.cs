using UnityEngine;

namespace HayHustle
{
    /// <summary>
    /// The gate of a fenced field. Zombies smash it to get to the animals inside.
    /// Click it to repair it (or let a Builder survivor do it).
    /// </summary>
    public class FieldGate : Interactable
    {
        public float Hp { get; private set; } = GameData.GateMaxHp;
        public bool Intact => Hp > 0f;
        public string FieldName { get; private set; }

        /// <summary>The spot just outside the gate (where zombies hit it and builders repair it).</summary>
        public Vector3 RepairPoint => transform.position + transform.forward * 1.3f;

        Transform left, right;

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
            : Hp <= 0f ? $"{FieldName} gate is BROKEN - click to repair it"
            : $"Repair the {FieldName} gate ({Hp:0}/{GameData.GateMaxHp:0})";

        public override bool CanInteract => Hp < GameData.GateMaxHp;

        public override void Interact(PlayerController player)
        {
            player.PlayAnim(ActAnim.Use, 0.8f);
            player.SetBusy(0.8f, "Repairing the gate...");
            Repair(40f);
        }

        public void SetHp(float hp)
        {
            Hp = Mathf.Clamp(hp, 0f, GameData.GateMaxHp);
            Refresh();
        }

        public void Repair(float amount)
        {
            bool wasBroken = Hp <= 0f;
            Hp = Mathf.Min(GameData.GateMaxHp, Hp + amount);
            FX.Text(transform.position + Vector3.up * 2f, $"Gate {Hp:0}/{GameData.GateMaxHp:0}", Palette.UiGreen, 0.9f);
            FX.Burst(transform.position + Vector3.up * 0.6f, Palette.Wood, 6, 0.08f, 2f);
            FX.Play(Sfx.Plant, 0.4f);
            if (wasBroken) GameManager.I.Notify($"The {FieldName} gate is fixed.", Palette.UiGreen);
            Refresh();
        }

        public void Damage(float amount)
        {
            if (Hp <= 0f) return;
            Hp = Mathf.Max(0f, Hp - amount);
            FX.Burst(transform.position + Vector3.up * 0.6f, Palette.Wood, 5, 0.07f, 2.5f);
            FX.Play(Sfx.Hit, 0.35f);
            if (Hp <= 0f)
            {
                GameManager.I.Notify($"Zombies BROKE the {FieldName} gate! Repair it fast.", Palette.UiRed, 6f);
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
