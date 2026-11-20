using UnityEngine;

namespace HayHustle
{
    /// <summary>A predator trap. Catches a fox, wolf, snake or rat that steps on it (bears just get hurt). Used once.</summary>
    public class Trap : MonoBehaviour
    {
        void Start()
        {
            Models.BuildTrap(transform);
            GameManager.I.TrapObjects.Add(this);
        }

        void OnDestroy()
        {
            if (GameManager.I) GameManager.I.TrapObjects.Remove(this);
        }

        void Update()
        {
            var gm = GameManager.I;
            foreach (var p in gm.Predators.ToArray())
            {
                if (!p || p.Def == null || p.Def.Flying || !p.CanBeHit) continue;
                if (GameManager.FlatDistance(p.transform.position, transform.position) > 1.0f) continue;
                FX.Play(Sfx.Hit, 0.8f);
                FX.Text(transform.position + Vector3.up * 1.5f, "SNAP! Trap sprung", Palette.UiGold, 1.2f);
                FX.Burst(transform.position + Vector3.up * 0.3f, new Color(0.5f, 0.5f, 0.55f), 12, 0.1f, 3f);
                p.TrapHit();
                gm.Traps = Mathf.Max(0, gm.Traps - 1);
                Destroy(gameObject);
                return;
            }
        }
    }
}
