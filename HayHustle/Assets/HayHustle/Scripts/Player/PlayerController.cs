using UnityEngine;
using UnityEngine.InputSystem;

namespace HayHustle
{
    public enum ActAnim { None, Swing, Fill, Plant, Pickup, Use }

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
        Transform visual, toolPivot, upper, legL, legR, armL, armR;
        Light lantern;
        float vy, busyUntil, nextSwing, walkPhase, actStart, actDuration, stuckTimer;
        ActAnim act;

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
            toolPivot = FindDeep(visualRoot, "ToolPivot");
            upper = FindDeep(visualRoot, "Upper");
            legL = FindDeep(visualRoot, "LegL");
            legR = FindDeep(visualRoot, "LegR");
            armL = FindDeep(visualRoot, "ArmL");
            armR = FindDeep(visualRoot, "ArmR");
            lantern = lanternLight;
        }

        public void SetBusy(float seconds, string label)
        {
            busyUntil = Time.time + seconds;
            BusyLabel = label;
        }

        public void PlayAnim(ActAnim a, float duration)
        {
            act = a;
            actStart = Time.time;
            actDuration = Mathf.Max(0.05f, duration);
        }

        void Update()
        {
            var gm = GameManager.I;
            float dt = Time.deltaTime;
            if (lantern) lantern.enabled = gm.DayNight.IsNight;

            bool menu = gm.Mode != GameMode.Playing || HUD.I.BlocksGameplay;
            bool blocked = menu || Busy;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            var cam = Camera.main;

            // ---- Mouse: hover label + click to act ----
            HoverLabel = null;
            if (!menu && mouse != null && cam != null)
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
            if (input.sqrMagnitude > 0.01f) task = TaskType.None; // manual control cancels click orders

            Vector3 fwd = cam ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = cam ? Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized : Vector3.right;
            Vector3 move = fwd * input.y + right * input.x;
            if (move.sqrMagnitude > 1f) move.Normalize();
            float speed = kb != null && kb.leftShiftKey.isPressed ? RunSpeed : WalkSpeed;

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

            Animate(dt, move.magnitude * speed);
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

        // ================= Animation =================

        void Animate(float dt, float speed)
        {
            if (!visual) return;
            bool moving = speed > 0.1f;
            if (moving) walkPhase += dt * speed * 1.6f;
            float amount = moving ? Mathf.Clamp01(speed / RunSpeed) * 40f + 12f : 0f;
            float sin = Mathf.Sin(walkPhase);

            float legLx = sin * amount, legRx = -sin * amount;
            float armLx = -sin * amount * 0.8f, armRx = sin * amount * 0.8f;
            float upperX = moving ? 4f + amount * 0.1f : Mathf.Sin(Time.time * 2f) * 1.5f;
            float bodyY = moving ? Mathf.Abs(sin) * 0.06f : 0f;
            float toolX = 40f;
            float k = 1f - Mathf.Exp(-18f * dt);

            float t = (Time.time - actStart) / actDuration;
            if (act != ActAnim.None && t < 1f)
            {
                float w = Mathf.Sin(Mathf.PI * t); // 0 -> 1 -> 0
                float wiggle = Mathf.Sin(Time.time * 14f);
                switch (act)
                {
                    case ActAnim.Swing:
                        armRx = t < 0.35f ? Mathf.Lerp(0f, -170f, t / 0.35f) : Mathf.Lerp(-170f, -20f, (t - 0.35f) / 0.65f);
                        upperX = 12f * w;
                        toolX = 10f;
                        k = 1f;
                        break;
                    case ActAnim.Fill:
                        upperX = 25f * w;
                        armRx = (-75f + wiggle * 20f) * w;
                        armLx = (-75f - wiggle * 20f) * w;
                        break;
                    case ActAnim.Plant:
                        upperX = 55f * w;
                        bodyY = -0.25f * w;
                        armRx = (-45f + wiggle * 15f) * w;
                        armLx = -35f * w;
                        legLx = -20f * w;
                        legRx = 25f * w;
                        break;
                    case ActAnim.Pickup:
                        upperX = 50f * w;
                        bodyY = -0.15f * w;
                        armRx = -60f * w;
                        armLx = -30f * w;
                        break;
                    case ActAnim.Use:
                        upperX = 10f * w;
                        armRx = (-85f + wiggle * 20f) * w;
                        break;
                }
            }
            else act = ActAnim.None;

            visual.localPosition = new Vector3(0f, bodyY, 0f);
            Rot(upper, upperX, k);
            Rot(legL, legLx, k);
            Rot(legR, legRx, k);
            Rot(armL, armLx, k);
            Rot(armR, armRx, k);
            Rot(toolPivot, toolX, k);
        }

        static void Rot(Transform t, float x, float k)
        {
            if (t) t.localRotation = Quaternion.Slerp(t.localRotation, Quaternion.Euler(x, 0f, 0f), k);
        }

        void FaceTowards(Vector3 p)
        {
            Vector3 d = p - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(d);
        }

        void KeepInBounds()
        {
            Vector3 p = transform.position;
            Vector2 flat = new Vector2(p.x, p.z);
            if (flat.magnitude <= 46f) return;
            flat = flat.normalized * 46f;
            cc.enabled = false;
            transform.position = new Vector3(flat.x, p.y, flat.y);
            cc.enabled = true;
        }

        static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t)
            {
                var r = FindDeep(c, name);
                if (r) return r;
            }
            return null;
        }
    }
}
