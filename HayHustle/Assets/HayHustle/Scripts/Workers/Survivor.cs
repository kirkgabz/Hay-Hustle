using UnityEngine;

namespace HayHustle
{
    /// <summary>
    /// A survivor who joined the farm. Works for food instead of coins (eats every morning), needs a bed in the Bunkhouse,
    /// can be given any job, and is faster at the job they are skilled in. Zombies can injure them; injured survivors
    /// rest in the Bunkhouse until they are healed. If they go hungry their morale drops and they may leave.
    /// </summary>
    public class Survivor : Laborer
    {
        public int Id { get; private set; }
        public string PersonName { get; private set; } = "Survivor";
        public JobKind Skill { get; private set; }
        public JobKind AssignedJob { get; private set; } = JobKind.Rest;
        public float Morale = 80f;
        public bool Injured { get; private set; }
        public bool Joined { get; private set; }

        public override string DisplayName => PersonName;
        public override JobKind Job => !Joined ? JobKind.Rest : Injured ? JobKind.Rest : AssignedJob;
        protected override float SpeedBonus => Joined && !Injured && AssignedJob == Skill ? 1.4f : 1f;


        public void Init(int id, string name, JobKind skill)
        {
            Id = id;
            PersonName = name;
            Skill = skill;
            var visual = Models.BuildSurvivor(id * 7919 + 13, transform);
            var arm = FindChild(visual, "ArmR");
            if (arm != null)
            {
                var pivot = new GameObject("ToolPivot").transform;   // weapon or axe goes here
                pivot.SetParent(arm, false);
                pivot.localPosition = new Vector3(0f, -0.62f, 0.08f);
            }
            SetupBody(visual);
        }

        /// <summary>Called when the player accepts them (or when a save is loaded).</summary>
        public void Join(JobKind job)
        {
            Joined = true;
            SetJob(job);
            if (!GameManager.I.Survivors.Contains(this)) GameManager.I.Survivors.Add(this);
        }

        public void SetJob(JobKind job)
        {
            AssignedJob = job;
            StopTrip();
            RefreshTool();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (GameManager.I) GameManager.I.Survivors.Remove(this);
        }

        protected override void OnTick(float dt)
        {
            if (!Joined) return;
            bool atBunk = GameManager.FlatDistance(transform.position, GameManager.I.BunkhouseDoor) < 6f;
            if (Injured)
            {
                Health = Mathf.Min(100f, Health + (atBunk ? 1.5f : 0.4f) * dt);
                Status = atBunk ? $"Resting - healing ({Health:0}%)" : "Injured - going to rest";
                if (Health >= 80f)
                {
                    Injured = false;
                    StopTrip();
                    GameManager.I.Notify($"{PersonName} has healed and is back to work.", Palette.UiGreen);
                }
            }
            else if (Health < 100f) Health = Mathf.Min(100f, Health + 0.1f * dt);
        }

        /// <summary>Zombie attack. Survivors are never killed - at low health they go and rest.</summary>
        public override void Damage(float amount)
        {
            if (!Joined) return;
            base.Damage(amount);
            if (!Injured && Health <= 25f)
            {
                Injured = true;
                StopTrip();
                GameManager.I.Notify($"{PersonName} is injured and goes to the Bunkhouse to rest.", Palette.UiRed, 6f);
                FX.Play(Sfx.Lost, 0.4f);
            }
        }

        protected override void ThinkRest(GameManager gm)
        {
            if (!Joined) { Status = "Waiting at the gate"; return; }
            if (!Injured) Status = "Off duty - resting";
            Idle(gm.BunkhouseDoor);
        }

        /// <summary>A declined or departing survivor walks away down the road.</summary>
        public void LeaveFarm()
        {
            Joined = false;
            StopTrip();
            GameManager.I.Survivors.Remove(this);
            GoTo(new Vector3(0f, 0f, 70f), "Leaving", () => Destroy(gameObject));
        }

        /// <summary>A new arrival walks up the road to the farm.</summary>
        public void ArriveAt(Vector3 spot) => GoTo(spot, "Walking to your farm", null);
    }
}
