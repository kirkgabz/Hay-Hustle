using UnityEngine;
using UnityEngine.InputSystem;

namespace HayHustle
{
    /// <summary>
    /// The farmer.
    /// Mouse: click anything (predator, goods, well, plot, building, ground) and the farmer goes there and does it.
    /// Keyboard: WASD/arrows move, Shift runs, E uses the nearest thing, F/Space swings the net.
    /// The body is animated in code: walking, running, net swing, cranking the well, planting, picking up.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public float WalkSpeed = 5.5f, RunSpeed = 8.5f, SwingRange = 2.6f;

        public Interactable Focus { get; private set; }
        public bool Busy => Time.time < busyUntil;
        public string BusyLabel { get; private set; }
        public string HoverLabel { get; private set; }
        /// <summary>Text for a long action like repairing a gate (shown at the bottom of the screen).</summary>
        public string Activity { get; private set; }

        FieldGate repairing;
        float repairAnimAt;

        /// <summary>Start hammering on a gate. Keeps going (100 health per minute, uses wood) until you move away.</summary>
        public void StartRepair(FieldGate gate)
        {
            repairing = gate;
            task = TaskType.None;
            FaceTowards(gate.transform.position);
            FX.Text(gate.transform.position + Vector3.up * 2f, "Repairing... (walk away to stop)", Palette.UiGreen, 0.9f);
        }

        void StopRepair(string why)
        {
            repairing = null;
            Activity = null;
            if (!string.IsNullOrEmpty(why)) GameManager.I.Notify(why, Palette.UiWhite);
        }

        void UpdateRepair(float dt, bool moved)
        {
            if (!repairing) { Activity = null; return; }
            if (moved || task != TaskType.None) { StopRepair("You stopped repairing the gate."); return; }
            if (GameManager.FlatDistance(transform.position, repairing.transform.position) > 3.6f) { StopRepair(""); return; }
            if (!repairing.RepairWork(dt))
            {
                StopRepair(GameManager.I.Wood <= 0.01f ? "Out of wood! Chop a tree to keep repairing." : "The gate is fully repaired.");
                return;
            }
            if (Time.time >= repairAnimAt) { repairAnimAt = Time.time + 0.9f; PlayAnim(ActAnim.Hammer, 1f); }
            Activity = $"Repairing the {repairing.FieldName} gate {repairing.Hp:0}/{GameData.GateMaxHp:0}  -  wood {GameManager.I.Wood:0.0}  -  +100 per minute  (walk to stop)";
        }

        enum TaskType { None, Move, Interact, Pickup, Hunt }

        struct Pick
        {
            public TaskType Type;
            public Predator Predator;
            public ProductPickup Pickup;
            public Interactable Target;
            public Vector3 Point;
            public string Label;
        }

        CharacterController cc;
        Transform visual;
        FarmerRig rig;
        Light lantern;
        float vy, busyUntil, nextSwing, stuckTimer;

        TaskType task;
        Vector3 taskPos, lastPos;
        Interactable taskTarget;
        ProductPickup taskPickup;
        Predator taskPredator;
        bool swingPending;

        void Awake() => cc = GetComponent<CharacterController>();

        public void Setup(Transform visualRoot, Light lanternLight)
        {
            visual = visualRoot;
            rig = visualRoot.gameObject.AddComponent<FarmerRig>();
            rig.RunSpeed = RunSpeed;
            lantern = lanternLight;
        }

        public void SetBusy(float seconds, string label)
        {
            busyUntil = Time.time + seconds;
            BusyLabel = label;
        }

        /// <summary>A zombie shoved the farmer: pushed back and stunned for a moment (no damage).</summary>
        public void Knockback(Vector3 from)
        {
            Vector3 d = transform.position - from;
            d.y = 0f;
            if (d.sqrMagnitude < 0.01f) d = -transform.forward;
            cc.Move(d.normalized * 1.8f);
            task = TaskType.None;
            repairing = null;
            Activity = null;
            SetBusy(0.7f, "A zombie knocked you back!");
            PlayAnim(ActAnim.Pickup, 0.6f);
            FX.Text(transform.position + Vector3.up * 2.4f, "Ouch!", Palette.UiRed, 1.1f);
            FX.Play(Sfx.Hit, 0.6f);
        }

        public void PlayAnim(ActAnim a, float duration)
        {
            if (rig) rig.Play(a, duration);
        }

        void Update()
        {
            var gm = GameManager.I;
            float dt = Time.deltaTime;
            if (lantern) lantern.enabled = gm.DayNight.LightsOn;

            bool menu = gm.Mode != GameMode.Playing || HUD.I.BlocksGameplay;
            bool blocked = menu || Busy;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            var cam = Camera.main;

            // ---- Mouse: hover label + click to act ----
            HoverLabel = null;
            if (LampPlacer.Active && (gm.Mode != GameMode.Playing)) LampPlacer.Cancel();
            var touch = Device.Touch ? TouchControls.I : null;
            if (touch != null)
            {
                // Phone: tap to act (or to choose where a lamp goes).
                if (touch.TakeTap(out var tp) && !menu && cam != null)
                {
                    if (LampPlacer.Active) LampPlacer.TouchTap(gm, cam, tp);
                    else if (!Busy) Apply(PickAt(gm, cam, tp));
                }
            }
            else if (LampPlacer.Active && !menu && mouse != null && cam != null)
            {
                Vector2 mp = mouse.position.ReadValue();
                LampPlacer.Tick(gm, cam, mp, mouse.leftButton.wasPressedThisFrame && !HUD.I.PointerOverUI(mp));
            }
            else if (!menu && mouse != null && cam != null)
            {
                Vector2 mp = mouse.position.ReadValue();
                if (!HUD.I.PointerOverUI(mp))
                {
                    var pick = PickAt(gm, cam, mp);
                    HoverLabel = pick.Label;
                    if (mouse.leftButton.wasPressedThisFrame && !Busy) Apply(pick);
                }
            }

            // ---- Keyboard movement ----
            Vector2 input = Vector2.zero;
            if (!blocked && kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
            }
            bool touchRun = false;
            if (!blocked && touch != null && touch.Move.sqrMagnitude > 0.0025f)
            {
                input = touch.Move;
                touchRun = touch.Run;
            }
            if (input.sqrMagnitude > 0.01f) task = TaskType.None; // manual control cancels click orders
            UpdateRepair(dt, input.sqrMagnitude > 0.01f);

            Vector3 fwd = cam ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = cam ? Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized : Vector3.right;
            Vector3 move = fwd * input.y + right * input.x;
            if (move.sqrMagnitude > 1f) move.Normalize();
            float speed = (kb != null && kb.leftShiftKey.isPressed) || touchRun ? RunSpeed : WalkSpeed;

            if (!blocked && task != TaskType.None && move.sqrMagnitude < 0.01f) move = RunTask(ref speed);
            if (blocked) move = Vector3.zero;

            if (move.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), 14f * dt);

            vy = cc.isGrounded ? -2f : vy - 20f * dt;
            cc.Move((move * speed + Vector3.up * vy) * dt);
            KeepInBounds();

            // ---- Keyboard actions ----
            Focus = blocked ? null : Interactable.Nearest(transform.position);
            if (!blocked && kb != null && kb.eKey.wasPressedThisFrame && Focus != null) Use(Focus);
            if (!blocked && kb != null && (kb.fKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)) Swing();
            if (!blocked && touch != null)
            {
                if (touch.UsePressed && Focus != null) Use(Focus);
                if (touch.HitPressed) Swing();
            }

            if (rig) rig.Tick(move.magnitude * speed, dt);
        }

        // ================= Click to act =================

        Pick PickAt(GameManager gm, Camera cam, Vector2 mp)
        {
            float s = Screen.height / 1080f;

            // 1) Predators (biggest priority)
            Predator bestPred = null;
            float best = 80f * s;
            foreach (var p in gm.Predators)
            {
                if (!p || p.Hidden || p.Def == null) continue;
                if (!ScreenDist(cam, p.transform.position + Vector3.up * 0.5f, mp, out float d) || d > best) continue;
                best = d;
                bestPred = p;
            }
            if (bestPred != null)
                return new Pick { Type = TaskType.Hunt, Predator = bestPred, Label = $"Catch the {bestPred.Def.Name.ToLower()} - {bestPred.HitsLeft} hit{(bestPred.HitsLeft == 1 ? "" : "s")} left" };

            // 2) Goods on the ground
            ProductPickup bestGood = null;
            best = 55f * s;
            foreach (var pk in ProductPickup.All)
            {
                if (!pk) continue;
                if (!ScreenDist(cam, pk.transform.position + Vector3.up * 0.2f, mp, out float d) || d > best) continue;
                best = d;
                bestGood = pk;
            }
            if (bestGood != null)
                return new Pick { Type = TaskType.Pickup, Pickup = bestGood, Label = $"Collect {GameData.ProductName(bestGood.Product)}" };

            // 3) Buildings and stations
            Interactable bestIt = null;
            best = float.MaxValue;
            foreach (var it in Interactable.All)
            {
                if (!it || it is GrassPatch) continue;
                bool big = it.Range >= 4f;
                float radius = (big ? 120f : 70f) * s;
                if (!ScreenDist(cam, it.transform.position + Vector3.up * (big ? 2f : 0.8f), mp, out float d) || d > radius || d > best) continue;
                best = d;
                bestIt = it;
            }
            if (bestIt != null) return new Pick { Type = TaskType.Interact, Target = bestIt, Label = bestIt.Prompt };

            // 4) Ground: a grass plot, or just walk there
            Ray ray = cam.ScreenPointToRay(mp);
            if (ray.direction.y < -0.0001f)
            {
                float t = -ray.origin.y / ray.direction.y;
                Vector3 gp = ray.origin + ray.direction * t;
                foreach (var g in gm.Grass)
                {
                    Vector3 c = g.transform.position;
                    if (Mathf.Abs(c.x - gp.x) > 1.2f || Mathf.Abs(c.z - gp.z) > 1.2f) continue;
                    if (g.Selectable) return new Pick { Type = TaskType.Interact, Target = g, Label = g.Prompt };
                    break;
                }
                return new Pick { Type = TaskType.Move, Point = gp };
            }
            return default;
        }

        static bool ScreenDist(Camera cam, Vector3 world, Vector2 mp, out float d)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            d = 0f;
            if (sp.z < 0f) return false;
            d = Vector2.Distance(new Vector2(sp.x, sp.y), mp);
            return true;
        }

        void Apply(Pick p)
        {
            switch (p.Type)
            {
                case TaskType.Hunt:
                    task = TaskType.Hunt;
                    taskPredator = p.Predator;
                    FX.Ring(p.Predator.transform.position, Palette.UiRed);
                    if (InReach(p.Predator)) { FaceTowards(p.Predator.transform.position); Swing(); swingPending = false; }
                    else swingPending = true;
                    break;
                case TaskType.Pickup:
                    task = TaskType.Pickup;
                    taskPickup = p.Pickup;
                    FX.Ring(p.Pickup.transform.position, Color.white);
                    FX.Play(Sfx.Click, 0.25f);
                    break;
                case TaskType.Interact:
                    task = TaskType.Interact;
                    taskTarget = p.Target;
                    FX.Ring(p.Target.transform.position, Palette.UiGold);
                    FX.Play(Sfx.Click, 0.25f);
                    break;
                case TaskType.Move:
                    task = TaskType.Move;
                    taskPos = p.Point;
                    FX.Ring(p.Point, Color.white);
                    FX.Play(Sfx.Click, 0.2f);
                    break;
                default:
                    return;
            }
            stuckTimer = 0f;
            lastPos = transform.position;
        }

        bool InReach(Predator p) =>
            p && p.CanBeHit && GameManager.FlatDistance(p.transform.position, transform.position) <= SwingRange;

        Vector3 RunTask(ref float speed)
        {
            Vector3 target;
            float reach;
            switch (task)
            {
                case TaskType.Move:
                    target = taskPos; reach = 0.3f;
                    break;
                case TaskType.Pickup:
                    if (!taskPickup) { task = TaskType.None; return Vector3.zero; }
                    target = taskPickup.transform.position; reach = 0.4f;
                    break;
                case TaskType.Interact:
                    if (!taskTarget) { task = TaskType.None; return Vector3.zero; }
                    target = taskTarget.transform.position; reach = taskTarget.Range * 0.75f;
                    break;
                case TaskType.Hunt:
                    if (!taskPredator) { task = TaskType.None; return Vector3.zero; } // caught or escaped
                    target = taskPredator.transform.position; reach = SwingRange * 0.8f; speed = RunSpeed;
                    break;
                default:
                    return Vector3.zero;
            }

            Vector3 d = target - transform.position;
            d.y = 0f;
            float dist = d.magnitude;
            if (dist <= reach)
            {
                switch (task)
                {
                    case TaskType.Interact:
                        var it = taskTarget;
                        task = TaskType.None;
                        Use(it);
                        break;
                    case TaskType.Hunt:
                        FaceTowards(target);
                        if (swingPending && taskPredator.CanBeHit) { Swing(); swingPending = false; }
                        break; // keep following so the next click hits right away
                    default:
                        task = TaskType.None;
                        break;
                }
                stuckTimer = 0f;
                return Vector3.zero;
            }

            stuckTimer += Time.deltaTime;
            if (stuckTimer > 0.6f)
            {
                if ((transform.position - lastPos).sqrMagnitude < 0.05f)
                {
                    task = TaskType.None;
                    FX.Text(transform.position + Vector3.up * 2.4f, "Can't get there", new Color(0.85f, 0.85f, 0.85f), 0.8f);
                    return Vector3.zero;
                }
                stuckTimer = 0f;
                lastPos = transform.position;
            }
            return d / dist;
        }

        void Use(Interactable it)
        {
            FaceTowards(it.transform.position);
            if (it.CanInteract)
            {
                it.Interact(this);
            }
            else
            {
                FX.Text(it.transform.position + Vector3.up * 2f, it.Prompt, new Color(0.9f, 0.9f, 0.9f), 0.8f);
                FX.Play(Sfx.Error, 0.3f);
            }
        }

        // ================= Net swing =================

        void Swing()
        {
            if (Time.time < nextSwing) return;
            nextSwing = Time.time + 0.3f;
            PlayAnim(ActAnim.Swing, 0.3f);

            var gm = GameManager.I;

            // Auto-aim: turn toward the closest predator in reach.
            Predator closest = null;
            float best = SwingRange + 0.6f;
            foreach (var p in gm.Predators)
            {
                if (!p || !p.CanBeHit) continue;
                float d = GameManager.FlatDistance(p.transform.position, transform.position);
                if (d < best) { best = d; closest = p; }
            }
            if (closest != null) FaceTowards(closest.transform.position);

            bool hitAny = false;
            foreach (var p in gm.Predators.ToArray())
            {
                if (!p || !p.CanBeHit) continue;
                Vector3 d = p.transform.position - transform.position;
                float vertical = d.y;
                d.y = 0f;
                float dist = d.magnitude;
                if (dist > SwingRange || vertical > 3.5f) continue;
                if (dist > 1f && Vector3.Angle(transform.forward, d) > 75f) continue;
                p.Hit(transform.position);
                hitAny = true;
            }
            if (!hitAny) FX.Play(Sfx.Whoosh, 0.3f);
        }

        void FaceTowards(Vector3 p)
        {
            Vector3 d = p - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(d);
        }

        void KeepInBounds()
        {
            if (transform.position.y < -5f)
            {
                // Fell through the ground somehow: put the farmer back on it.
                cc.enabled = false;
                transform.position = new Vector3(transform.position.x, 0.1f, transform.position.z);
                cc.enabled = true;
                vy = 0f;
            }
            KeepWallSolid();
            Vector3 p = transform.position;
            Vector2 c = new Vector2(GameManager.PastureCenter.x, GameManager.PastureCenter.z);
            Vector2 flat = new Vector2(p.x, p.z) - c;
            if (flat.magnitude <= GameData.PlayerRadius) return;
            flat = c + flat.normalized * GameData.PlayerRadius;
            cc.enabled = false;
            transform.position = new Vector3(flat.x, p.y, flat.y);
            cc.enabled = true;
        }

        Vector3 wallLastPos;
        bool hasWallLastPos;

        /// <summary>You cannot walk through the farm wall - only through the gate.</summary>
        void KeepWallSolid()
        {
            Vector3 p = transform.position;
            if (hasWallLastPos)
            {
                bool wasIn = GameManager.InsideFarm(wallLastPos), isIn = GameManager.InsideFarm(p);
                bool atGate = Mathf.Abs(p.x) < GameData.FarmGateHalfWidth - 0.3f && Mathf.Abs(p.z - GameData.WallMaxZ) < 2f;
                bool teleported = (p - wallLastPos).sqrMagnitude > 9f;
                if (wasIn != isIn && !atGate && !teleported)
                {
                    cc.enabled = false;
                    transform.position = new Vector3(wallLastPos.x, p.y, wallLastPos.z);
                    cc.enabled = true;
                    p = transform.position;
                }
            }
            wallLastPos = p;
            hasWallLastPos = true;
        }

    }
}
