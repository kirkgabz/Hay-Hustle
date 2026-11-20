using UnityEngine;

namespace HayHustle
{
    /// <summary>
    /// Lets the player choose where a lamp post goes: a see-through lamp follows the mouse (green = OK, red = not here),
    /// click to build it there, Esc to cancel. Also used to move a lamp post that is already built.
    /// A new lamp is only paid for when it is placed.
    /// </summary>
    public static class LampPlacer
    {
        public static bool Active { get; private set; }
        public static Lamp Moving { get; private set; }
        static Transform ghost;
        static Renderer[] ghostParts;
        static bool valid;
        /// <summary>Phone: the spot chosen by tapping, waiting for "Build here".</summary>
        public static bool HasPending { get; private set; }
        public static bool PendingValid => HasPending && valid;
        static Vector3 pending;
        static Material okMat, badMat;

        public static string Hint => Device.Touch
            ? (Moving != null ? "Moving a lamp post: tap a spot inside the farm wall, then Build here." : $"Placing a lamp post ({GameData.LampPrice} coins): tap a spot inside the farm wall, then Build here.")
            : Moving != null
            ? "Moving a lamp post: click a spot inside the farm wall.   Esc = cancel"
            : $"Placing a lamp post ({GameData.LampPrice} coins): click a spot inside the farm wall.   Esc = cancel";

        public static void BeginNew() => Begin(null);
        public static void BeginMove(Lamp lamp) => Begin(lamp);

        static void Begin(Lamp lamp)
        {
            Moving = lamp;
            Active = true;
            HasPending = false;
            if (!ghost) BuildGhost();
            ghost.gameObject.SetActive(false);
            FX.Play(Sfx.Click, 0.3f);
        }

        public static void Cancel()
        {
            Active = false;
            Moving = null;
            HasPending = false;
            if (ghost) ghost.gameObject.SetActive(false);
        }

        /// <summary>Called every frame by the player while placing. Returns nothing; builds/moves on click.</summary>
        public static void Tick(GameManager gm, Camera cam, Vector2 mouse, bool click)
        {
            if (!ghost) BuildGhost();
            var ray = cam.ScreenPointToRay(mouse);
            if (Mathf.Abs(ray.direction.y) < 0.0001f) { ghost.gameObject.SetActive(false); return; }
            float t = -ray.origin.y / ray.direction.y;
            if (t <= 0f) { ghost.gameObject.SetActive(false); return; }
            Vector3 p = ray.origin + ray.direction * t;
            p.y = 0f;
            ghost.gameObject.SetActive(true);
            ghost.position = p;

            valid = SpotOk(gm, p, out string why);
            foreach (var r in ghostParts) r.sharedMaterial = valid ? okMat : badMat;
            if (!click) return;
            if (!valid)
            {
                gm.Notify(why, Palette.UiRed);
                FX.Play(Sfx.Error, 0.4f);
                return;
            }
            if (Moving != null)
            {
                Moving.transform.position = p;
                FX.Text(p + Vector3.up * 4f, "Lamp post moved!", Palette.UiGreen);
                FX.Play(Sfx.Plant, 0.4f);
                Cancel();
            }
            else
            {
                gm.BuildLampAt(p);
                Cancel();
            }
        }

        /// <summary>Phone: a tap shows the lamp there (green/red); the HUD then offers "Build here".</summary>
        public static void TouchTap(GameManager gm, Camera cam, Vector2 screenPos)
        {
            if (!ghost) BuildGhost();
            var ray = cam.ScreenPointToRay(screenPos);
            if (ray.direction.y > -0.0001f) return;
            float t = -ray.origin.y / ray.direction.y;
            pending = ray.origin + ray.direction * t;
            pending.y = 0f;
            HasPending = true;
            ghost.gameObject.SetActive(true);
            ghost.position = pending;
            valid = SpotOk(gm, pending, out string why);
            foreach (var r in ghostParts) r.sharedMaterial = valid ? okMat : badMat;
            if (!valid) gm.Notify(why, Palette.UiRed);
        }

        /// <summary>Phone: the "Build here" button.</summary>
        public static void ConfirmPending(GameManager gm)
        {
            if (!PendingValid) return;
            if (Moving != null)
            {
                Moving.transform.position = pending;
                FX.Text(pending + Vector3.up * 4f, "Lamp post moved!", Palette.UiGreen);
                FX.Play(Sfx.Plant, 0.4f);
            }
            else gm.BuildLampAt(pending);
            Cancel();
        }

        static bool SpotOk(GameManager gm, Vector3 p, out string why)
        {
            why = null;
            if (!GameManager.InsideFarm(p, -0.8f)) { why = "Lamp posts go inside the farm wall."; return false; }
            if (Mathf.Abs(p.x) < GameData.FarmGateHalfWidth + 0.5f && p.z > GameData.WallMaxZ - 2.5f) { why = "That would block the farm gate."; return false; }
            foreach (var l in gm.LampObjects)
                if (l && l != Moving && GameManager.FlatDistance(l.transform.position, p) < 2f) { why = "Too close to another lamp post."; return false; }
            return true;
        }

        static void BuildGhost()
        {
            ghost = new GameObject("Lamp Ghost").transform;
            okMat = Visuals.Mat(new Color(0.35f, 0.95f, 0.4f));
            badMat = Visuals.Mat(new Color(0.95f, 0.3f, 0.25f));
            Visuals.Prim(PrimitiveType.Cylinder, ghost, new Vector3(0f, 0.15f, 0f), new Vector3(0.4f, 0.15f, 0.4f), Color.white);
            Visuals.Prim(PrimitiveType.Cylinder, ghost, new Vector3(0f, 1.6f, 0f), new Vector3(0.12f, 1.5f, 0.12f), Color.white);
            Visuals.Prim(PrimitiveType.Cube, ghost, new Vector3(0f, 3.3f, 0f), new Vector3(0.45f, 0.5f, 0.45f), Color.white);
            // a ring on the ground showing how far the light reaches
            for (int i = 0; i < 24; i++)
            {
                float a = i / 24f * Mathf.PI * 2f;
                Visuals.Prim(PrimitiveType.Cube, ghost, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * GameData.LampRadius + Vector3.up * 0.05f,
                    new Vector3(0.25f, 0.06f, 0.9f), Color.white, false, new Vector3(0f, -a * Mathf.Rad2Deg, 0f));
            }
            ghostParts = ghost.GetComponentsInChildren<Renderer>();
            foreach (var r in ghostParts) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ghost.gameObject.SetActive(false);
        }
    }
}
