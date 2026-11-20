using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace HayHustle
{
    /// <summary>
    /// Phone controls:
    ///  - left side: a joystick appears where your thumb touches - push to walk, push all the way to run;
    ///  - tap anything on the farm to act (like a mouse click);
    ///  - drag with one finger to turn the camera, pinch with two to zoom;
    ///  - round buttons on the right: HIT (swing) and USE (the thing you stand next to).
    /// The HUD draws them; the player and camera read the results each frame.
    /// </summary>
    public class TouchControls : MonoBehaviour
    {
        public static TouchControls I { get; private set; }

        public Vector2 Move { get; private set; }
        public bool Run => Move.magnitude > 0.85f;
        public Vector2 LookDelta { get; private set; }   // pixels this frame
        public float Pinch { get; private set; }         // + = fingers apart (zoom in), in screen heights
        public bool HitPressed { get; private set; }
        public bool UsePressed { get; private set; }
        public bool HitHeld { get; private set; }
        public bool UseHeld { get; private set; }

        /// <summary>Where the joystick is (screen pixels, y up) and whether a thumb is on it.</summary>
        public Vector2 JoyCenter { get; private set; }
        public Vector2 JoyKnob { get; private set; }
        public bool JoyActive => joyId >= 0;

        class Info
        {
            public Vector2 Start;
            public float StartTime, MaxMove;
            public bool Ignored, Look, Multi;
        }

        readonly Dictionary<int, Info> touches = new Dictionary<int, Info>();
        int joyId = -1;
        Vector2 joyOrigin;
        float lastPinchDist = -1f;
        Vector2? tap;

        // ---- Layout (screen pixels, origin bottom-left) ----
        static float U => Screen.height / 100f;
        public static float JoyRadius => 11f * U;
        public static Vector2 JoyHome => new Vector2(18f * U, 26f * U);
        public static Vector2 HitCenter => new Vector2(Screen.width - 13f * U, 34f * U);
        public static float HitRadius => 9f * U;
        public static Vector2 UseCenter => new Vector2(Screen.width - 32f * U, 27f * U);
        public static float UseRadius => 7.5f * U;

        void Awake()
        {
            I = this;
            EnhancedTouchSupport.Enable();
        }

        void OnDestroy()
        {
            if (I == this) I = null;
        }

        /// <summary>A short tap on the farm since last asked (screen pixels, y up).</summary>
        public bool TakeTap(out Vector2 pos)
        {
            pos = tap ?? Vector2.zero;
            bool had = tap.HasValue;
            tap = null;
            return had;
        }

        void Update()
        {
            Move = Vector2.zero;
            LookDelta = Vector2.zero;
            Pinch = 0f;
            HitPressed = UsePressed = false;
            HitHeld = UseHeld = false;
            JoyCenter = JoyHome;
            JoyKnob = JoyHome;

            var gm = GameManager.I;
            var active = ETouch.activeTouches;
            if (active.Count > 0) Device.NoteTouch();
            if (!Device.Touch || gm == null || gm.Mode != GameMode.Playing)
            {
                touches.Clear();
                joyId = -1;
                tap = null;
                return;
            }
            bool blocked = HUD.I == null || HUD.I.BlocksGameplay || Device.Portrait;

            var looking = new List<ETouch>();
            foreach (var t in active)
            {
                int id = t.touchId;
                Vector2 pos = t.screenPosition;

                if (t.phase == TouchPhase.Began || !touches.ContainsKey(id))
                {
                    var info = new Info { Start = pos, StartTime = Time.unscaledTime };
                    touches[id] = info;
                    if (blocked || (HUD.I != null && HUD.I.PointerOverUI(pos))) info.Ignored = true;
                    else if ((pos - HitCenter).magnitude <= HitRadius * 1.1f) { info.Ignored = true; HitPressed = true; }
                    else if ((pos - UseCenter).magnitude <= UseRadius * 1.1f) { info.Ignored = true; UsePressed = true; }
                    else if (joyId < 0 && pos.x < Screen.width * 0.42f && pos.y < Screen.height * 0.72f)
                    {
                        joyId = id;
                        joyOrigin = pos;
                        info.Ignored = true;
                    }
                    else info.Look = true;
                }

                var inf = touches[id];
                inf.MaxMove = Mathf.Max(inf.MaxMove, (pos - inf.Start).magnitude);
                bool ended = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;

                if (id == joyId)
                {
                    if (ended) joyId = -1;
                    else
                    {
                        Vector2 off = pos - joyOrigin;
                        float r = JoyRadius;
                        if (off.magnitude > r * 1.3f) joyOrigin = pos - off.normalized * r * 1.3f; // the stick follows the thumb
                        off = pos - joyOrigin;
                        Move = Vector2.ClampMagnitude(off / r, 1f);
                        JoyCenter = joyOrigin;
                        JoyKnob = joyOrigin + Vector2.ClampMagnitude(off, r);
                    }
                }
                else if (!inf.Ignored && inf.Look)
                {
                    if (ended)
                    {
                        bool quick = Time.unscaledTime - inf.StartTime < 0.4f;
                        if (quick && !inf.Multi && inf.MaxMove < Screen.height * 0.035f) tap = pos;
                    }
                    else looking.Add(t);
                }
                else if (!ended && inf.Ignored)
                {
                    if ((inf.Start - HitCenter).magnitude <= HitRadius * 1.1f) HitHeld = true;
                    if ((inf.Start - UseCenter).magnitude <= UseRadius * 1.1f) UseHeld = true;
                }

                if (ended) touches.Remove(id);
            }

            // Camera: one finger drags, two fingers pinch.
            if (looking.Count >= 2)
            {
                foreach (var t in looking) touches[t.touchId].Multi = true;
                float d = (looking[0].screenPosition - looking[1].screenPosition).magnitude;
                if (lastPinchDist > 0f) Pinch = (d - lastPinchDist) / Screen.height;
                lastPinchDist = d;
            }
            else
            {
                lastPinchDist = -1f;
                if (looking.Count == 1)
                {
                    var t = looking[0];
                    var inf = touches[t.touchId];
                    if (inf.MaxMove > Screen.height * 0.02f || inf.Multi) LookDelta = t.delta;
                }
            }
        }
    }
}
