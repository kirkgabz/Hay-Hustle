using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HayHustle
{
    /// <summary>
    /// All screens and the in-game HUD (Unity IMGUI, so it needs no scene setup).
    /// Layout uses a logical screen 900 units tall; text is rendered at the real pixel size so it stays sharp.
    /// This file: core, title, character select, on-screen info. HUD.Panels.cs: the windows.
    /// </summary>
    public partial class HUD : MonoBehaviour
    {
        public enum Panel { None, Shop, Truck, Rewards, Help, Warehouse, Orders, Pause, Camp, Seeds }

        CropPlot seedPlot;

        /// <summary>Open the seed picker for an empty crop plot.</summary>
        public void OpenSeeds(CropPlot plot)
        {
            seedPlot = plot; // kept for future per-plot use
            Open = Panel.Seeds;
            FX.Play(Sfx.Click, 0.2f);
        }

        public static HUD I { get; private set; }
        public Panel Open = Panel.None;
        public bool BlocksGameplay => Open != Panel.None || confirm != null;

        /// <summary>Layout height in logical units: smaller on phones, so everything is drawn bigger.</summary>
        static float LogicalHeight => Device.Touch ? 640f : 900f;
        float S = 1f, W = 1600f, H = 900f;
        int fontsForHeight = -1;
        Texture2D circleTex;

        GUIStyle title, h1, h2, h2c, label, labelC, labelR, small, smallC, wrap, wrapC, tiny, tinyWrap, button, bigButton, prompt, textField, popupStyle, barStyle, tagStyle;
        Vector3? guideTarget;
        string guideText;
        float guideUntil;

        /// <summary>Point the player to a place on the farm (used by the "Show" buttons).</summary>
        public void GuideTo(Vector3 target, string text)
        {
            guideTarget = target;
            guideText = text;
            guideUntil = Time.unscaledTime + 90f;
            FX.Play(Sfx.Click, 0.3f);
        }
        Texture2D panelTex, rowTex, whiteTex;
        Font uiFont;
        readonly List<Rect> uiRects = new List<Rect>();

        CharacterChoice choice = CharacterChoice.Boy;
        string farmNameInput = "Sunny Acres";

        class ConfirmBox
        {
            public string Title, Message, Ok;
            public System.Action OnOk;
        }
        ConfirmBox confirm;

        void Awake() => I = this;

        // ================= Confirm popup =================

        /// <summary>"Are you sure?" card. always = ask even if the player turned confirmations off.</summary>
        public void Confirm(string titleText, string message, string ok, System.Action onOk, bool always = false)
        {
            if (!always && !Settings.AskConfirm) { onOk(); return; }
            confirm = new ConfirmBox { Title = titleText, Message = message, Ok = ok, OnOk = onOk };
            FX.Play(Sfx.Click, 0.3f);
        }

        void DoConfirm()
        {
            var c = confirm;
            confirm = null;
            c?.OnOk?.Invoke();
        }

        void DrawConfirm()
        {
            Block(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.55f));
            float textH = CalcH(wrapC, confirm.Message, 480f);
            Rect r = CenterRect(560f, 170f + textH);
            PanelBg(r);
            Lbl(new Rect(r.x, r.y + 18, r.width, 40), confirm.Title, h2c);
            Lbl(new Rect(r.x + 40, r.y + 66, r.width - 80, textH + 6), confirm.Message, wrapC);
            float by = r.yMax - 70;
            if (Btn(new Rect(r.x + 40, by, 220, 50), confirm.Ok, bigButton)) DoConfirm();
            else if (Btn(new Rect(r.xMax - 260, by, 220, 50), "Cancel", bigButton)) { confirm = null; FX.Play(Sfx.Click, 0.2f); }
        }

        // ================= Update / input =================

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            WebInput.Poll();
            if (Backend.SignedIn && CloudSave.TriedUser != Backend.UserId && !CloudSave.Syncing && gm.Mode == GameMode.Title)
                CloudSave.Sync();   // after signing in: load the newest farm from the cloud
            if (gm.Mode == GameMode.CharacterSelect) WorldBuilder.TickStage(choice);
            if (gm.Mode != GameMode.Playing) return;

            float wantScale = Open == Panel.Pause ? 0f : 1f;
            if (Time.timeScale != wantScale) Time.timeScale = wantScale;

            var kb = Keyboard.current;
            if (kb == null || WebInput.IsOpen) return;
            if (confirm != null)
            {
                if (kb.escapeKey.wasPressedThisFrame) confirm = null;
                else if (kb.enterKey.wasPressedThisFrame) DoConfirm();
                return;
            }
            if (LampPlacer.Active)
            {
                if (kb.escapeKey.wasPressedThisFrame) { LampPlacer.Cancel(); gm.Notify("Lamp placement cancelled.", Palette.UiWhite); }
                return;
            }
            if (kb.bKey.wasPressedThisFrame) Toggle(Panel.Shop);
            if (kb.rKey.wasPressedThisFrame) Toggle(Panel.Rewards);
            if (kb.oKey.wasPressedThisFrame) Toggle(Panel.Orders);
            if (kb.cKey.wasPressedThisFrame) Toggle(Panel.Camp);
            if (kb.hKey.wasPressedThisFrame || kb.f1Key.wasPressedThisFrame) Toggle(Panel.Help);
            if (kb.escapeKey.wasPressedThisFrame) Open = Open == Panel.None ? Panel.Pause : Panel.None;
        }

        void Toggle(Panel p)
        {
            Open = Open == p ? Panel.None : p;
            FX.Play(Sfx.Click, 0.2f);
        }

        // ================= Drawing helpers (logical units -> pixels) =================

        Rect P(Rect r) => new Rect(r.x * S, r.y * S, r.width * S, r.height * S);
        int Px(float size) => Mathf.Max(10, Mathf.RoundToInt(size * S));
        void Lbl(Rect r, string t, GUIStyle st) => GUI.Label(P(r), t, st);
        bool Btn(Rect r, string t, GUIStyle st = null) => GUI.Button(P(r), t, st ?? button);
        bool Tgl(Rect r, bool v, string t, GUIStyle st = null) => GUI.Toggle(P(r), v, t, st ?? button);
        void Tex(Rect r, Texture t) => GUI.DrawTexture(P(r), t);

        void Block(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(P(r), whiteTex);
            GUI.color = old;
        }

        void Bar(Rect r, float t, Color fill)
        {
            Block(r, new Color(0f, 0f, 0f, 0.55f));
            Block(new Rect(r.x, r.y, r.width * Mathf.Clamp01(t), r.height), fill);
        }

        Vector2 BeginScroll(Rect area, Vector2 pos, float contentH) =>
            GUI.BeginScrollView(P(area), pos, P(new Rect(0, 0, area.width - 26, Mathf.Max(contentH, area.height))));

        float CalcH(GUIStyle st, string text, float width) => st.CalcHeight(new GUIContent(text), width * S) / S;
        float CalcW(GUIStyle st, string text) => st.CalcSize(new GUIContent(text)).x / S;

        void PanelBg(Rect r)
        {
            Tex(r, panelTex);
            Block(new Rect(r.x, r.y, r.width, 3), new Color(0.95f, 0.75f, 0.25f, 0.9f));
            if (Event.current.type == EventType.Repaint) uiRects.Add(r); // clicks here belong to the UI, not the farm
        }

        Rect CenterRect(float w, float h)
        {
            w = Mathf.Min(w, W - 30f);
            h = Mathf.Min(h, H - 30f);
            return new Rect((W - w) / 2f, (H - h) / 2f, w, h);
        }

        bool CloseButton(Rect panel) => Btn(new Rect(panel.xMax - 56, panel.y + 12, 44, 40), "X");

        void ShadowLabel(Rect r, string text, GUIStyle style, Color color)
        {
            var old = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, color.a * 0.9f);
            GUI.Label(P(new Rect(r.x + 1.6f, r.y + 1.6f, r.width, r.height)), text, style);
            style.normal.textColor = color;
            GUI.Label(P(r), text, style);
            style.normal.textColor = old;
        }

        bool ToGui(Camera cam, Vector3 world, out Vector2 g)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            g = new Vector2(sp.x / S, (Screen.height - sp.y) / S);
            return sp.z > 0f && g.x > -60 && g.x < W + 60 && g.y > -60 && g.y < H + 60;
        }

        /// <summary>True when the mouse is over a HUD panel (so clicks don't move the farmer).</summary>
        public bool PointerOverUI(Vector2 screenPos)
        {
            if (confirm != null) return true;
            Vector2 g = new Vector2(screenPos.x / S, (Screen.height - screenPos.y) / S);
            foreach (var r in uiRects) if (r.Contains(g)) return true;
            return false;
        }

        static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);

        // ================= Phone helpers =================

        /// <summary>
        /// A text box. In a phone browser it opens a web text box (so the phone keyboard appears);
        /// everywhere else it is a normal Unity text field.
        /// </summary>
        string TextBox(Rect r, string value, int max, int kind, string titleText, bool hide)
        {
            value = value ?? "";
            if (Device.Touch && WebInput.Available)
            {
                string shown = value.Length == 0 ? "<color=#8A8070>Tap to type</color>" : hide ? new string('*', value.Length) : value;
                var st = new GUIStyle(textField) { richText = true };
                if (GUI.Button(P(r), shown, st))
                {
                    string current = value;
                    int k = kind;
                    WebInput.Open(titleText, current, k, max, s => SetTextBoxValue(titleText, s));
                }
                return pendingText != null && pendingTextFor == titleText ? TakePendingText() : value;
            }
            if (hide) return GUI.PasswordField(P(r), value, '*', max, textField);
            return GUI.TextField(P(r), value, max, textField);
        }

        string pendingText, pendingTextFor;
        void SetTextBoxValue(string forBox, string s) { pendingText = s; pendingTextFor = forBox; }
        string TakePendingText() { string s = pendingText; pendingText = null; pendingTextFor = null; return s; }

        Texture2D Circle()
        {
            if (circleTex != null) return circleTex;
            const int n = 128;
            circleTex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = new Vector2(x - n / 2f + 0.5f, y - n / 2f + 0.5f).magnitude / (n / 2f);
                float a = Mathf.Clamp01((1f - d) * n / 2f);
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            circleTex.SetPixels32(px);
            circleTex.Apply();
            return circleTex;
        }

        /// <summary>Draws a circle given in screen pixels (y up), e.g. the joystick and the round buttons.</summary>
        void ScreenCircle(Vector2 centerPx, float radiusPx, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(new Rect(centerPx.x - radiusPx, Screen.height - centerPx.y - radiusPx, radiusPx * 2f, radiusPx * 2f), Circle());
            GUI.color = old;
        }

        void ScreenLabel(Vector2 centerPx, string text, GUIStyle st, Color c)
        {
            var old = st.normal.textColor;
            st.normal.textColor = c;
            GUI.Label(new Rect(centerPx.x - 200, Screen.height - centerPx.y - 30, 400, 60), text, st);
            st.normal.textColor = old;
        }

        void DrawTouchControls(GameManager gm)
        {
            var t = TouchControls.I;
            if (t == null) return;
            // Joystick
            ScreenCircle(t.JoyCenter, TouchControls.JoyRadius, new Color(1f, 1f, 1f, t.JoyActive ? 0.22f : 0.12f));
            ScreenCircle(t.JoyKnob, TouchControls.JoyRadius * 0.45f, new Color(1f, 1f, 1f, t.JoyActive ? 0.75f : 0.4f));
            if (!t.JoyActive) ScreenLabel(t.JoyCenter + Vector2.down * TouchControls.JoyRadius * 1.25f, "MOVE", tagStyle, new Color(1f, 1f, 1f, 0.6f));

            // HIT button
            ScreenCircle(TouchControls.HitCenter, TouchControls.HitRadius, t.HitHeld ? new Color(0.95f, 0.72f, 0.22f, 0.95f) : new Color(0.75f, 0.25f, 0.18f, 0.8f));
            ScreenLabel(TouchControls.HitCenter, "HIT", barStyle, Color.white);

            // USE button (only when there is something to use)
            var p = gm.Player;
            bool canUse = p && p.Focus != null;
            ScreenCircle(TouchControls.UseCenter, TouchControls.UseRadius,
                canUse ? (t.UseHeld ? new Color(0.95f, 0.72f, 0.22f, 0.95f) : new Color(0.28f, 0.5f, 0.2f, 0.85f)) : new Color(0.3f, 0.3f, 0.3f, 0.45f));
            ScreenLabel(TouchControls.UseCenter, "USE", barStyle, canUse ? Color.white : new Color(1f, 1f, 1f, 0.5f));
        }

        void DrawPlacementButtons(GameManager gm)
        {
            float bw = 220, bh = 54;
            float y = Device.Touch ? 170 : H - 230;
            Rect bar = new Rect(W / 2f - (Device.Touch ? bw + 12 : bw / 2f + 6), y, Device.Touch ? bw * 2 + 24 : bw + 12, bh + 12);
            PanelBg(bar);
            float x = bar.x + 6;
            if (Device.Touch)
            {
                GUI.enabled = LampPlacer.PendingValid;
                if (Btn(new Rect(x, y + 6, bw, bh), LampPlacer.Moving != null ? "Move here" : $"Build here ({GameData.LampPrice})", bigButton)) LampPlacer.ConfirmPending(gm);
                GUI.enabled = true;
                x += bw + 12;
            }
            if (Btn(new Rect(x, y + 6, bw, bh), "Cancel", bigButton)) { LampPlacer.Cancel(); FX.Play(Sfx.Click, 0.2f); }
        }

        void DrawRotatePhone()
        {
            Block(new Rect(0, 0, W, H), new Color(0.06f, 0.05f, 0.04f, 1f));
            Lbl(new Rect(20, H / 2f - 120, W - 40, 60), "Turn your phone sideways", h1);
            Lbl(new Rect(40, H / 2f - 40, W - 80, 120), "Hay Hustle plays in landscape. Rotate your phone (and turn off rotation lock if it is on).", wrapC);
        }

        // ================= Styles =================

        static Texture2D Tex1(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        static void SetTextColor(GUIStyle s, Color c)
        {
            s.normal.textColor = s.hover.textColor = s.active.textColor = s.focused.textColor = c;
            s.onNormal.textColor = s.onHover.textColor = s.onActive.textColor = s.onFocused.textColor = c;
        }

        void InitStyles()
        {
            if (label != null) return;
            panelTex = Tex1(new Color(0.06f, 0.05f, 0.04f, 0.9f));
            rowTex = Tex1(new Color(1f, 1f, 1f, 0.07f));
            whiteTex = Tex1(Color.white);

            label = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = false, alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Overflow };
            SetTextColor(label, new Color(0.97f, 0.97f, 0.95f));
            labelC = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
            labelR = new GUIStyle(label) { alignment = TextAnchor.MiddleRight };
            small = new GUIStyle(label);
            smallC = new GUIStyle(labelC);
            tiny = new GUIStyle(label);
            SetTextColor(tiny, new Color(0.85f, 0.85f, 0.8f));
            wrap = new GUIStyle(label) { wordWrap = true, alignment = TextAnchor.UpperLeft, clipping = TextClipping.Clip };
            wrapC = new GUIStyle(wrap) { alignment = TextAnchor.UpperCenter };
            tinyWrap = new GUIStyle(wrap);
            SetTextColor(tinyWrap, new Color(0.78f, 0.78f, 0.72f));
            h2 = new GUIStyle(label) { fontStyle = FontStyle.Bold };
            h2c = new GUIStyle(h2) { alignment = TextAnchor.MiddleCenter };
            h1 = new GUIStyle(h2c);
            title = new GUIStyle(h2c);
            prompt = new GUIStyle(labelC);
            popupStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = false, clipping = TextClipping.Overflow };
            barStyle = new GUIStyle(popupStyle);
            tagStyle = new GUIStyle(popupStyle);

            var bg = Tex1(new Color(0.28f, 0.4f, 0.19f, 0.97f));
            var hover = Tex1(new Color(0.37f, 0.53f, 0.25f, 1f));
            var down = Tex1(new Color(0.2f, 0.3f, 0.13f, 1f));
            var on = Tex1(new Color(0.88f, 0.64f, 0.16f, 1f));
            var onHover = Tex1(new Color(0.95f, 0.72f, 0.22f, 1f));
            button = new GUIStyle(GUI.skin.button) { richText = true, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false };
            button.normal.background = bg;
            button.hover.background = hover;
            button.active.background = down;
            button.focused.background = bg;
            button.onNormal.background = on;
            button.onHover.background = onHover;
            button.onActive.background = on;
            button.onFocused.background = on;
            button.border = new RectOffset(0, 0, 0, 0);
            SetTextColor(button, Color.white);
            button.onNormal.textColor = button.onHover.textColor = button.onActive.textColor = new Color(0.15f, 0.1f, 0.02f);
            bigButton = new GUIStyle(button);

            textField = new GUIStyle(GUI.skin.textField) { alignment = TextAnchor.MiddleLeft };
            textField.normal.background = Tex1(new Color(0.95f, 0.93f, 0.86f));
            textField.focused.background = Tex1(Color.white);
            textField.hover.background = textField.focused.background;
            SetTextColor(textField, new Color(0.1f, 0.08f, 0.05f));
            textField.padding = new RectOffset(10, 10, 4, 4);

            // Windows system font: much sharper than the default Unity font at small sizes.
#if UNITY_WEBGL && !UNITY_EDITOR
            uiFont = null;   // browsers can't use the computer's fonts - keep Unity's built-in font
#else
            uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Segoe UI Semibold", "Arial" }, 20);
#endif
            if (uiFont != null)
                foreach (var st in new[] { title, h1, h2, h2c, label, labelC, labelR, small, smallC, wrap, wrapC, tiny, tinyWrap, button, bigButton, prompt, textField, popupStyle, barStyle, tagStyle })
                    st.font = uiFont;
        }

        void ApplyFontSizes()
        {
            title.fontSize = Px(Device.Touch ? 64 : 92);
            h1.fontSize = Px(34);
            h2.fontSize = h2c.fontSize = Px(25);
            label.fontSize = labelC.fontSize = labelR.fontSize = Px(20);
            small.fontSize = smallC.fontSize = wrap.fontSize = wrapC.fontSize = Px(17);
            tiny.fontSize = tinyWrap.fontSize = Px(14);
            button.fontSize = Px(17);
            bigButton.fontSize = Px(22);
            prompt.fontSize = Px(20);
            textField.fontSize = Px(22);
            barStyle.fontSize = Px(15);
            tagStyle.fontSize = Px(13);
        }

        // ================= Main =================

        void OnGUI()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            InitStyles();
            S = Mathf.Max(0.45f, Screen.height / LogicalHeight);
            W = Screen.width / S;
            H = Screen.height / S;
            int fontKey = Screen.height * 2 + (Device.Touch ? 1 : 0);
            if (fontsForHeight != fontKey) { fontsForHeight = fontKey; ApplyFontSizes(); }
            if (Event.current.type == EventType.Repaint) uiRects.Clear();
            if (Device.Portrait) { DrawRotatePhone(); return; }
            if (WebInput.IsOpen) GUI.enabled = false;   // the web text box is on top

            bool modal = confirm != null;
            GUI.enabled = !modal;
            switch (gm.Mode)
            {
                case GameMode.Title: DrawTitle(gm); break;
                case GameMode.CharacterSelect: DrawCharacterSelect(gm); break;
                case GameMode.Playing: DrawPlaying(gm); break;
            }
            DrawNotes(gm);
            GUI.enabled = true;
            if (modal) DrawConfirm();
        }

        // ================= Title & character select =================

        // ================= Sign in / Sign up =================

        int authTab;                       // 0 = sign in, 1 = sign up
        string authEmail, authPass = "", authPass2 = "", authError = "";
        bool showPass, rememberMe = true;

        void DrawAuth(GameManager gm)
        {
            if (authEmail == null) authEmail = Backend.SavedEmail;
            Block(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.3f));
            bool signUp = authTab == 1;
            float bw = 460, x = (W - bw) / 2f;
            float boxTop = 180;
            if (Device.Touch)
            {
                // Phone (sideways): the name on the left, the form in the middle from the top.
                x = Mathf.Max(W * 0.5f - bw / 2f, Mathf.Min(W - bw - 50f, W * 0.62f - bw / 2f));
                boxTop = Mathf.Max(10f, (H - (signUp ? 560 : 480)) / 2f);
                ShadowLabel(new Rect(10, H / 2f - 60, x - 50, 120), "HAY\nHUSTLE", title, new Color(1f, 0.84f, 0.32f));
            }
            else ShadowLabel(new Rect(0, 40, W, 120), "HAY HUSTLE", title, new Color(1f, 0.84f, 0.32f));
            Rect box = new Rect(x - 30, boxTop, bw + 60, signUp ? 560 : 480);
            PanelBg(box);
            float y = box.y + 24;

            if (Backend.Busy && Backend.HasSavedSession && !Backend.SignedIn && string.IsNullOrEmpty(authPass))
            {
                Lbl(new Rect(box.x, y + 120, box.width, 40), "Signing you in...", h2c);
                return;
            }

            if (Tgl(new Rect(x, y, bw / 2f - 4, 46), !signUp, "Sign in") && signUp) { authTab = 0; authError = ""; }
            if (Tgl(new Rect(x + bw / 2f + 4, y, bw / 2f - 4, 46), signUp, "Sign up") && !signUp) { authTab = 1; authError = ""; }
            y += 64;

            Lbl(new Rect(x, y, bw, 26), "Email", small);
            y += 28;
            authEmail = TextBox(new Rect(x, y, bw, 46), authEmail ?? "", 120, WebInput.Email, "Email", false);
            y += 58;
            Lbl(new Rect(x, y, bw, 26), "Password" + (signUp ? "  (at least 6 characters)" : ""), small);
            y += 28;
            authPass = TextBox(new Rect(x, y, bw, 46), authPass, 72, WebInput.Password, "Password", !showPass);
            y += 58;
            if (signUp)
            {
                Lbl(new Rect(x, y, bw, 26), "Confirm password", small);
                y += 28;
                authPass2 = TextBox(new Rect(x, y, bw, 46), authPass2, 72, WebInput.Password, "Confirm password", !showPass);
                y += 58;
            }

            showPass = Tgl(new Rect(x, y, bw / 2f - 4, 38), showPass, showPass ? "Hide password" : "Show password");
            rememberMe = Tgl(new Rect(x + bw / 2f + 4, y, bw / 2f - 4, 38), rememberMe, rememberMe ? "Stay signed in: ON" : "Stay signed in: OFF");
            y += 52;

            if (!string.IsNullOrEmpty(authError))
            {
                float eh = CalcH(wrap, authError, bw);
                Lbl(new Rect(x, y, bw, eh), $"<color=#{Hex(Palette.UiRed)}>{authError}</color>", wrap);
                y += eh + 8;
            }

            bool enter = Event.current.type == EventType.KeyDown &&
                         (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);
            GUI.enabled = !Backend.Busy && confirm == null;
            string label = Backend.Busy ? "Please wait..." : signUp ? "Create account" : "Sign in";
            if ((Btn(new Rect(x, y, bw, 56), label, bigButton) || (enter && GUI.enabled)) && !Backend.Busy) SubmitAuth(gm, signUp);
            GUI.enabled = confirm == null;
            y += 70;

            if (Btn(new Rect(x + bw / 2f - 110, y, 220, 40), "Play offline"))
            {
                Backend.Offline = true;
                authError = "";
            }
            Lbl(new Rect(16, H - 36, 400, 28), "Hay Hustle  v0.5", tiny);
        }

        void SubmitAuth(GameManager gm, bool signUp)
        {
            string email = (authEmail ?? "").Trim();
            if (!email.Contains("@") || !email.Contains(".")) { authError = "Please enter a valid email address."; return; }
            if (authPass.Length < 6) { authError = "Password must be at least 6 characters."; return; }
            if (signUp && authPass != authPass2) { authError = "The two passwords do not match."; return; }
            authError = "";
            System.Action<bool, string> done = (ok, err) =>
            {
                if (ok)
                {
                    authPass = authPass2 = "";
                    gm.Notify(signUp ? $"Welcome! Account created for {Backend.Email}." : $"Signed in as {Backend.Email}.", Palette.UiGreen, 5f);
                    FX.Play(Sfx.Coin, 0.5f);
                }
                else
                {
                    authError = err;
                    FX.Play(Sfx.Error, 0.3f);
                }
            };
            if (signUp) Backend.SignUp(email, authPass, rememberMe, done);
            else Backend.SignIn(email, authPass, rememberMe, done);
        }

        void DrawTitle(GameManager gm)
        {
            if (!Backend.SignedIn && !Backend.Offline) { DrawAuth(gm); return; }
            Block(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.25f));
            float top = Device.Touch ? 4f : 90f;
            ShadowLabel(new Rect(0, top, W, Device.Touch ? 90 : 130), "HAY HUSTLE", title, new Color(1f, 0.84f, 0.32f));
            ShadowLabel(new Rect(0, top + (Device.Touch ? 84 : 125), W, 44), "Grow it.  Guard it.  Hustle it.", h2c, Color.white);

            if (showBackups) { DrawBackups(gm); return; }
            if (CloudSave.Syncing)
            {
                Rect sb = CenterRect(560, 120);
                PanelBg(sb);
                Lbl(new Rect(sb.x, sb.y + 20, sb.width, 40), "Loading your farm from the cloud...", h2c);
                Lbl(new Rect(sb.x, sb.y + 66, sb.width, 30), "One moment", smallC);
                return;
            }
            bool hasSave = SaveSystem.Exists;
            bool hasBackups = SaveSystem.ListBackups().Count > 0;
            bool canQuit = !Device.IsWeb;   // a browser tab can't be quit from inside
            float bw = 360, x = (W - bw) / 2f;
            float rowH = Device.Touch ? 72 : 80;
            Rect box = new Rect(x - 30, Device.Touch ? 140 : 300, bw + 60, 60 + rowH * ((hasSave ? 1 : 0) + 1 + (hasBackups ? 1 : 0) + (canQuit ? 1 : 0)) - 16);
            PanelBg(box);
            float y = box.y + 30;
            if (hasSave)
            {
                if (Btn(new Rect(x, y, bw, 64), "Continue", bigButton)) gm.ContinueGame();
                y += rowH;
            }
            if (Btn(new Rect(x, y, bw, 64), "New Farm", bigButton))
            {
                if (hasSave) Confirm("Start a new farm?", "Your current farm will be replaced by a brand new one. This cannot be undone.", "New farm", gm.EnterCharacterSelect, true);
                else gm.EnterCharacterSelect();
            }
            y += rowH;
            if (hasBackups)
            {
                if (Btn(new Rect(x, y, bw, 64), "Restore backup", bigButton)) showBackups = true;
                y += rowH;
            }
            if (canQuit && Btn(new Rect(x, y, bw, 64), "Quit", bigButton))
                Confirm("Quit Hay Hustle?", "Your farm is saved automatically.", "Quit", Application.Quit, true);
            Lbl(new Rect(16, H - 36, 400, 28), "Hay Hustle  v0.5", tiny);

            // Account
            Rect acc = new Rect(W - 470, H - 74, 456, 60);
            if (Backend.SignedIn)
            {
                // Cloud status above the account box
                bool cloudOk = CloudSave.SyncedUser == Backend.UserId;
                string cs = cloudOk ? CloudSave.Status : "Cloud not reached - playing the farm on this device.";
                if (!string.IsNullOrEmpty(cs))
                {
                    float cw = Mathf.Min(W - 28f, CalcW(small, cs) + 40f + (cloudOk ? 0 : 130));
                    Rect cr = new Rect(W - 14 - cw, acc.y - 52, cw, 44);
                    PanelBg(cr);
                    Lbl(new Rect(cr.x + 14, cr.y, cw - 28, cr.height), $"<color=#{Hex(cloudOk ? Palette.UiGreen : Palette.UiRed)}>{cs}</color>", small);
                    if (!cloudOk && Btn(new Rect(cr.xMax - 124, cr.y + 6, 112, 32), "Try again")) CloudSave.Sync();
                }
            }
            PanelBg(acc);
            if (Backend.SignedIn)
            {
                Lbl(new Rect(acc.x + 14, acc.y, 300, acc.height), $"Signed in as <b>{Backend.Email}</b>", small);
                if (Btn(new Rect(acc.xMax - 130, acc.y + 10, 116, 40), "Sign out"))
                    Confirm("Sign out?", "You will go back to the sign-in screen. Your farm stays saved on this device and in the cloud.", "Sign out", () => { Backend.SignOut(); authTab = 0; }, true);
            }
            else
            {
                Lbl(new Rect(acc.x + 14, acc.y, 300, acc.height), "Playing offline", small);
                if (Btn(new Rect(acc.xMax - 130, acc.y + 10, 116, 40), "Sign in")) Backend.Offline = false;
            }
        }

        bool showBackups;

        void DrawBackups(GameManager gm)
        {
            var list = SaveSystem.ListBackups();
            Rect r = CenterRect(900, 200 + list.Count * 96);
            PanelBg(r);
            Lbl(new Rect(r.x + 26, r.y + 16, r.width - 52, 40), "Restore a backup", h2);
            Lbl(new Rect(r.x + 26, r.y + 58, r.width - 52, 28), "A backup is made every time you click Continue, and before starting a New Farm. Newest first.", small);
            float y = r.y + 100;
            foreach (var b in list)
            {
                Rect row = new Rect(r.x + 26, y, r.width - 52, 84);
                Row(row);
                Lbl(new Rect(row.x + 16, row.y + 6, row.width - 260, 34), $"<b>Backup {b.Index}</b>   <color=#AAAAAA>{b.Time:MMM d, yyyy  h:mm tt}</color>", label);
                Lbl(new Rect(row.x + 16, row.y + 42, row.width - 260, 32), b.Summary, small);
                int idx = b.Index;
                string when = b.Time.ToString("MMM d, h:mm tt");
                if (Btn(new Rect(row.xMax - 216, row.y + 18, 200, 48), "Restore"))
                    Confirm("Restore this backup?", $"Your farm will go back to how it was on {when}. Your current save is kept aside first, so it is not lost.", "Restore", () =>
                    {
                        if (SaveSystem.Restore(idx))
                        {
                            gm.Notify("Backup restored. Click Continue to play it.", Palette.UiGreen, 6f);
                            FX.Play(Sfx.Coin, 0.5f);
                        }
                        else gm.Notify("Could not restore that backup.", Palette.UiRed, 6f);
                        showBackups = false;
                    }, true);
                y += 96;
            }
            if (Btn(new Rect(r.x + 26, r.yMax - 66, 200, 48), "Back")) showBackups = false;
        }

        void DrawCharacterSelect(GameManager gm)
        {
            Rect head = new Rect(W / 2f - 330, 18, 660, 92);
            PanelBg(head);
            Lbl(new Rect(head.x, head.y + 8, head.width, 46), "Choose your farmer", h1);
            Lbl(new Rect(head.x, head.y + 54, head.width, 30), "Click a farmer or a name card below", smallC);

            var cam = Camera.main;
            if (cam)
            {
                foreach (var c in new[] { CharacterChoice.Boy, CharacterChoice.Girl })
                {
                    Vector3 feet = WorldBuilder.StagePosition(c);
                    if (!ToGui(cam, feet, out var g)) continue;
                    bool sel = choice == c;
                    Rect card = new Rect(g.x - 130, g.y + 14, 260, 86);
                    if (Tgl(card, sel, "", button) && !sel) { choice = c; WorldBuilder.WaveNow(c); FX.Play(Sfx.Click, 0.3f); }
                    var st = new GUIStyle(h2c);
                    if (sel) SetTextColor(st, new Color(0.15f, 0.1f, 0.02f));
                    Lbl(new Rect(card.x, card.y + 6, card.width, 36), c == CharacterChoice.Boy ? "Boy Farmer" : "Girl Farmer", st);
                    var sm = new GUIStyle(smallC);
                    if (sel) SetTextColor(sm, new Color(0.2f, 0.14f, 0.04f));
                    Lbl(new Rect(card.x, card.y + 44, card.width, 30), c == CharacterChoice.Boy ? "Straw hat, plaid shirt" : "Sun hat, braid, overalls", sm);

                    // Clicking the 3D farmer selects them too
                    if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && GUI.enabled && ToGui(cam, feet + Vector3.up, out var body))
                    {
                        Vector2 m = Event.current.mousePosition / S;
                        if (Mathf.Abs(m.x - body.x) < 90f && Mathf.Abs(m.y - body.y) < 150f && choice != c)
                        {
                            choice = c;
                            WorldBuilder.WaveNow(c);
                            FX.Play(Sfx.Click, 0.3f);
                            Event.current.Use();
                        }
                    }
                }
            }

            Rect bottom = new Rect(W / 2f - 340, H - 150, 680, 132);
            PanelBg(bottom);
            Lbl(new Rect(bottom.x + 28, bottom.y + 18, 160, 46), "Farm name:", label);
            farmNameInput = TextBox(new Rect(bottom.x + 180, bottom.y + 18, 470, 46), farmNameInput, 24, WebInput.Text, "Farm name", false);
            if (Btn(new Rect(bottom.x + 180, bottom.y + 74, 470, 50), "Start Farming!", bigButton)) gm.StartNewGame(choice, farmNameInput);
            if (SaveSystem.Exists && Btn(new Rect(20, 20, 140, 46), "Back")) gm.GoToTitle(false);
        }

        // ================= In-game HUD =================

        void DrawPlaying(GameManager gm)
        {
            var cam = Camera.main;
            if (cam)
            {
                DrawFieldLabels(gm, cam);
                DrawHungerIcons(gm, cam);
                DrawWorkerTags(gm, cam);
                DrawGateBars(gm, cam);
                DrawPredatorBars(gm, cam);
                DrawPopups(cam);
                DrawTutorialArrow(gm, cam);
                DrawGuide(gm, cam);
                DrawWarnings(gm, cam);
            }
            DrawStats(gm);
            DrawClock(gm);
            DrawInventory(gm);
            DrawPrompt(gm);
            DrawMenuButtons(gm);
            DrawTutorialCard(gm);
            DrawArrivalCard(gm);
            if (Device.Touch)
            {
                if (Open == Panel.None && confirm == null) DrawTouchControls(gm);
            }
            else
            {
                Lbl(new Rect(16, H - 30, W - 700, 24), "Click anything to act  -  WASD move  -  Shift run  -  E use  -  F/Space swing  -  Right-drag camera  -  Scroll zoom  -  Esc menu", tiny);
                if (Open == Panel.None && confirm == null) DrawHover(gm);
            }
            if (LampPlacer.Active && Open == Panel.None) DrawPlacementButtons(gm);

            switch (Open)
            {
                case Panel.Shop: DrawShop(gm); break;
                case Panel.Truck: DrawTruck(gm); break;
                case Panel.Rewards: DrawRewards(gm); break;
                case Panel.Help: DrawHelp(); break;
                case Panel.Warehouse: DrawWarehouse(gm); break;
                case Panel.Orders: DrawOrders(gm); break;
                case Panel.Pause: DrawPause(gm); break;
                case Panel.Camp: DrawCamp(gm); break;
                case Panel.Seeds: DrawSeeds(gm); break;
            }
        }

        void DrawStats(GameManager gm)
        {
            Rect r = new Rect(14, 14, 350, 222);
            PanelBg(r);
            float x = r.x + 16, y = r.y + 10;
            Lbl(new Rect(x, y, 320, 34), gm.FarmName, h2); y += 38;
            Lbl(new Rect(x, y, 320, 28), $"<color=#{Hex(Palette.UiGold)}>Coins</color>   <b>{gm.Coins}</b>", label); y += 30;
            Lbl(new Rect(x, y, 150, 28), $"<color=#7EC8FF>Water</color>   <b>{gm.Water}/{gm.WaterMax}</b>", label);
            Bar(new Rect(x + 170, y + 10, 148, 9), gm.Water / (float)gm.WaterMax, new Color(0.35f, 0.65f, 1f)); y += 30;

            int rank = gm.Rank;
            bool max = rank >= GameData.MaxRank;
            int cur = gm.Xp - GameData.XpToReach(rank);
            int need = max ? 1 : GameData.XpToReach(rank + 1) - GameData.XpToReach(rank);
            Lbl(new Rect(x, y, 320, 28), $"<color=#{Hex(Palette.UiGreen)}>Farm Rank {rank}</color>   {(max ? "MAX" : cur + "/" + need + " XP")}", label); y += 30;
            Bar(new Rect(x, y, 318, 9), max ? 1f : cur / (float)need, Palette.UiGreen); y += 16;
            Lbl(new Rect(x, y, 320, 26), $"Storage {gm.InventoryTotal}/{gm.StorageCapacity}     Animals {gm.Animals.Count}/{gm.AnimalCapacity}", small);
            y += 26;
            bool lowFood = gm.FoodPoints < gm.FoodNeededPerDay;
            string food = (gm.Survivors.Count == 0 ? $"Food {gm.FoodPoints}" : $"Food {gm.FoodPoints} (eat {gm.FoodNeededPerDay}/day)") + $"   Wood {gm.Wood:0}";
            Lbl(new Rect(x, y, 320, 26), lowFood ? $"<color=#{Hex(Palette.UiRed)}>{food}</color>" : $"<color=#C8F08C>{food}</color>", small);
        }

        void DrawClock(GameManager gm)
        {
            Rect r = new Rect(W - 314, 14, 300, 142);
            PanelBg(r);
            float x = r.x + 16, y = r.y + 10;
            var dn = gm.DayNight;
            Lbl(new Rect(x, y, 270, 34), $"Day {dn.Day}    {dn.Clock}", h2); y += 36;
            Lbl(new Rect(x, y, 270, 26), dn.IsNight ? $"<color=#{Hex(Palette.UiRed)}>Night - stay alert!</color>" : $"<color=#{Hex(Palette.UiGold)}>Daytime</color>", label); y += 28;
            Lbl(new Rect(x, y, 270, 24), $"Wanted: <color=#{Hex(Palette.UiGold)}><b>{GameData.ProductName(gm.WantedProduct)}</b> x2</color>", small); y += 24;
            Lbl(new Rect(x, y, 270, 24), $"Caught {gm.PredatorsCaught}     Lost {gm.AnimalsLost}", small);
        }

        void DrawInventory(GameManager gm)
        {
            var lines = new List<string>();
            foreach (var pd in GameData.Products.Values)
            {
                int n = gm.Inventory[(int)pd.Product];
                if (n > 0) lines.Add($"{pd.Name}  <b>x{n}</b>");
            }
            if (Device.Touch && lines.Count > 5)   // phones: keep the joystick area free
            {
                int more = lines.Count - 4;
                lines.RemoveRange(4, lines.Count - 4);
                lines.Add($"<color=#AAAAAA>+{more} more (Storage)</color>");
            }
            Rect r = new Rect(14, 246, 240, 44 + Mathf.Max(1, lines.Count) * 25);
            PanelBg(r);
            Lbl(new Rect(r.x + 14, r.y + 8, 220, 28), "<b>Warehouse</b>", label);
            if (lines.Count == 0) Lbl(new Rect(r.x + 14, r.y + 38, 220, 24), "<color=#AAAAAA>empty</color>", small);
            for (int i = 0; i < lines.Count; i++) Lbl(new Rect(r.x + 14, r.y + 38 + i * 25, 220, 24), lines[i], small);
        }

        void DrawPrompt(GameManager gm)
        {
            var p = gm.Player;
            if (!p || Open != Panel.None) return;
            string text = null;
            if (LampPlacer.Active) text = $"<color=#{Hex(Palette.UiGold)}>{LampPlacer.Hint}</color>";
            else if (p.Busy) text = p.BusyLabel;
            else if (!string.IsNullOrEmpty(p.Activity)) text = $"<color=#{Hex(Palette.UiGreen)}>{p.Activity}</color>";
            else if (p.Focus != null)
                text = p.Focus.CanInteract
                    ? $"<color=#{Hex(Palette.UiGold)}>[E]</color>  {p.Focus.Prompt}"
                    : $"<color=#BBBBBB>{p.Focus.Prompt}</color>";
            if (text == null) return;
            float pw = Device.Touch ? Mathf.Clamp(W - 800f, 420f, 940f) : 940f;
            Rect r = new Rect(W / 2f - pw / 2f, H - (Device.Touch ? 150 : 160), pw, 46);
            if (Device.Touch && CalcW(prompt, text) > pw - 20f)
            {
                float th = CalcH(wrapC, text, pw - 20f);
                r = new Rect(r.x, r.yMax - th - 16f, pw, th + 16f);
                PanelBg(r);
                Lbl(new Rect(r.x + 10, r.y + 8, pw - 20, th), text, wrapC);
                return;
            }
            PanelBg(r);
            Lbl(r, text, prompt);
        }

        void DrawMenuButtons(GameManager gm)
        {
            float bw = 112f, bh = 48f, gap = 8f;
            int n = 6;
            Rect bar = new Rect(W - (bw * n + gap * (n + 1)) - 14, H - 96, bw * n + gap * (n + 1), bh + gap * 2);
            PanelBg(bar);
            float x = bar.x + gap, y = bar.y + gap;
            if (Btn(new Rect(x, y, bw, bh), "Shop")) Toggle(Panel.Shop);
            x += bw + gap;
            int ready = 0;
            foreach (var o in gm.Orders.Orders) if (gm.Orders.CanDeliver(gm, o)) ready++;
            if (Btn(new Rect(x, y, bw, bh), ready > 0 ? $"Orders {ready}!" : "Orders")) Toggle(Panel.Orders);
            x += bw + gap;
            bool claim = DailyRewards.CanClaim(gm);
            if (Btn(new Rect(x, y, bw, bh), claim ? "<color=#FFE066>Reward!</color>" : "Rewards")) Toggle(Panel.Rewards);
            x += bw + gap;
            if (Btn(new Rect(x, y, bw, bh), gm.Visitor ? "<color=#FFE066>Camp!</color>" : "Camp")) Toggle(Panel.Camp);
            x += bw + gap;
            if (Btn(new Rect(x, y, bw, bh), "Manual")) Toggle(Panel.Help);
            x += bw + gap;
            if (Btn(new Rect(x, y, bw, bh), "Menu")) Toggle(Panel.Pause);
        }

        // ================= World overlays =================

        void DrawPopups(Camera cam)
        {
            if (FX.I == null) return;
            foreach (var p in FX.I.Popups)
            {
                float age = (Time.unscaledTime - p.Born) / p.Life;
                if (!ToGui(cam, p.World + Vector3.up * age * 1.2f, out var g)) continue;
                float alpha = 1f - Mathf.Clamp01((age - 0.6f) / 0.4f);
                float pop = age < 0.1f ? Mathf.Lerp(0.6f, 1.1f, age / 0.1f) : 1f;
                popupStyle.fontSize = Px(23f * p.Size * pop);
                var c = p.Color;
                c.a = alpha;
                ShadowLabel(new Rect(g.x - 320, g.y - 25, 640, 50), p.Text, popupStyle, c);
            }
        }

        void DrawPredatorBars(GameManager gm, Camera cam)
        {
            foreach (var p in gm.Predators)
            {
                if (!p || p.Hidden || p.Def == null) continue;
                if (!ToGui(cam, p.transform.position + Vector3.up * p.BarHeight, out var g)) continue;
                int max = Mathf.Max(1, p.MaxHits);
                int left = Mathf.Clamp(p.HitsLeft, 0, max);
                float w = Mathf.Clamp(max * 22f, 70f, 160f), h = 13f;
                Rect bar = new Rect(g.x - w / 2f, g.y, w, h);
                Block(new Rect(bar.x - 3, bar.y - 3, w + 6, h + 6), new Color(0f, 0f, 0f, 0.7f));
                float seg = w / max;
                for (int i = 0; i < max; i++)
                    Block(new Rect(bar.x + i * seg + 1, bar.y, seg - 2, h), i < left ? new Color(0.92f, 0.2f, 0.12f) : new Color(0.25f, 0.25f, 0.25f));
                string text = p.Def.Name + "  -  " + left + (left == 1 ? " hit left" : " hits left");
                if (p.IsCarrying) text = "THIEF!  " + text;
                if (p.Def.Flying && !p.CanBeHit) text += "  (hit it when it dives)";
                ShadowLabel(new Rect(g.x - 260, g.y - 28, 520, 26), text, barStyle, Color.white);
            }
        }

        void DrawHungerIcons(GameManager gm, Camera cam)
        {
            foreach (var a in gm.Animals)
            {
                if (!a || a.IsCarried || a.InBarn || a.Hunger >= 0.45f) continue;
                float hgt = a.Kind == AnimalKind.Chicken ? 1.0f : a.Kind == AnimalKind.Cow ? 2.0f : a.Kind == AnimalKind.Llama ? 2.6f : 1.5f;
                if (a.Stage == Stage.Baby) hgt *= 0.65f;
                if (!ToGui(cam, a.transform.position + Vector3.up * hgt, out var g)) continue;
                bool starving = a.Hunger < 0.15f;
                Color c = starving ? new Color(0.95f, 0.2f, 0.15f) : new Color(1f, 0.78f, 0.15f);
                float pulse = starving ? 1f + Mathf.PingPong(Time.unscaledTime * 3f, 0.25f) : 1f;
                Rect icon = new Rect(g.x - 13 * pulse, g.y - 26 * pulse, 26 * pulse, 26 * pulse);
                Block(new Rect(icon.x - 2, icon.y - 2, icon.width + 4, icon.height + 4), new Color(0f, 0f, 0f, 0.7f));
                Block(icon, c);
                ShadowLabel(icon, "!", barStyle, Color.white);
                Bar(new Rect(g.x - 20, g.y + 4, 40, 6), a.Hunger / 0.45f, c);
            }
        }

        /// <summary>Name and animal count above each owned field's entrance, e.g. "Chicken Run  12/30".</summary>
        void DrawFieldLabels(GameManager gm, Camera cam)
        {
            foreach (var f in gm.Fields)
            {
                if (!f.Owned || !ToGui(cam, f.Gate + Vector3.up * 2.2f, out var g)) continue;
                int n = gm.CountInField(f.Index), cap = f.Def.Capacity;
                bool full = n >= cap;
                string text = $"{f.Def.Name}   {n}/{cap}" + (full ? "  FULL" : "");
                float w = CalcW(barStyle, text) + 24f;
                Rect r = new Rect(g.x - w / 2f, g.y - 14, w, 28);
                Block(r, full ? new Color(0.55f, 0.1f, 0.08f, 0.8f) : new Color(0f, 0f, 0f, 0.55f));
                ShadowLabel(r, text, barStyle, full ? new Color(1f, 0.85f, 0.8f) : Color.white);
            }
        }

        void DrawWorkerTags(GameManager gm, Camera cam)
        {
            foreach (var w in gm.Workers)
            {
                if (!w || !ToGui(cam, w.transform.position + Vector3.up * 2.35f, out var g)) continue;
                ShadowLabel(new Rect(g.x - 150, g.y - 22, 300, 20), w.Def.Name, tagStyle, new Color(0.75f, 1f, 0.7f));
                ShadowLabel(new Rect(g.x - 150, g.y - 4, 300, 20), w.Status, tagStyle, new Color(0.9f, 0.9f, 0.88f));
                if (w.Health < 99f)
                    Bar(new Rect(g.x - 25, g.y + 16, 50, 6), w.Health / 100f, w.Health < GameData.GuardRetreatHealth ? Palette.UiRed : Palette.UiGreen);
            }
            var people = new List<Survivor>(gm.Survivors);
            if (gm.Visitor) people.Add(gm.Visitor);
            foreach (var s in people)
            {
                if (!s || !ToGui(cam, s.transform.position + Vector3.up * 2.35f, out var g)) continue;
                string role = s.Joined ? GameData.JobName(s.Job == JobKind.Rest && !s.Injured ? s.AssignedJob : s.Job) : "Visitor";
                ShadowLabel(new Rect(g.x - 150, g.y - 22, 300, 20), $"{s.PersonName}  ({role})", tagStyle, new Color(1f, 0.88f, 0.55f));
                ShadowLabel(new Rect(g.x - 150, g.y - 4, 300, 20), s.Status, tagStyle, new Color(0.9f, 0.9f, 0.88f));
                if (s.Joined && s.Health < 99f)
                    Bar(new Rect(g.x - 25, g.y + 16, 50, 6), s.Health / 100f, s.Injured ? Palette.UiRed : Palette.UiGreen);
            }
        }

        /// <summary>Health bar over gates that are damaged.</summary>
        void DrawGateBars(GameManager gm, Camera cam)
        {
            foreach (var gate in gm.Gates)
            {
                if (!gate || gate.Hp >= GameData.GateMaxHp) continue;
                if (!ToGui(cam, gate.transform.position + Vector3.up * 1.6f, out var g)) continue;
                bool broken = !gate.Intact;
                string text = broken ? "FARM GATE BROKEN - click to repair" : $"Farm gate {gate.Hp:0}/{GameData.GateMaxHp:0}";
                ShadowLabel(new Rect(g.x - 160, g.y - 26, 320, 22), text, barStyle, broken ? Palette.UiRed : Color.white);
                Bar(new Rect(g.x - 40, g.y, 80, 9), gate.Hp / GameData.GateMaxHp, broken ? Palette.UiRed : new Color(0.95f, 0.7f, 0.2f));
            }
        }

        /// <summary>A survivor at the gate: Accept or Decline.</summary>
        void DrawArrivalCard(GameManager gm)
        {
            var v = gm.Visitor;
            if (!v || Open != Panel.None) return;
            float w = 560f;
            float top = (Tutorial.Active ? 150f : 14f);
            Rect r = new Rect(W / 2f - w / 2f, top, w, 132);
            PanelBg(r);
            Lbl(new Rect(r.x + 18, r.y + 8, w - 36, 30), $"<color=#{Hex(Palette.UiGold)}><b>A survivor arrived: {v.PersonName}</b></color>", label);
            Lbl(new Rect(r.x + 18, r.y + 38, w - 36, 44),
                $"Skilled {GameData.JobName(v.Skill)}. Needs a bed ({gm.Survivors.Count}/{gm.Beds} used) and eats {GameData.FoodPerSurvivor} food every morning (you have {gm.FoodPoints}).", wrap);
            bool bed = gm.Survivors.Count < gm.Beds;
            GUI.enabled = bed && confirm == null;
            if (Btn(new Rect(r.x + 18, r.yMax - 46, 250, 38), bed ? "Accept" : "No free bed")) gm.AcceptVisitor();
            GUI.enabled = confirm == null;
            if (Btn(new Rect(r.xMax - 268, r.yMax - 46, 250, 38), "Decline")) gm.DeclineVisitor();
        }

        void DrawWarnings(GameManager gm, Camera cam)
        {
            foreach (var w in gm.Spawner.Warnings)
                Marker(cam, w.Position, "! " + GameData.Predators[w.Kind].Name.ToUpper());
            foreach (var p in gm.Predators)
                if (p && !p.Hidden) Marker(cam, p.transform.position + Vector3.up * 1.6f, p.IsCarrying ? "! THIEF" : "! " + p.Def.Name.ToUpper());
        }

        /// <summary>Red marker at the screen edge pointing at something off-screen.</summary>
        void Marker(Camera cam, Vector3 world, string text) => Marker(cam, world, text, new Color(0.85f, 0.1f, 0.08f));

        void Marker(Camera cam, Vector3 world, string text, Color color)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            bool behind = sp.z < 0f;
            Vector2 p = new Vector2(sp.x / S, (Screen.height - sp.y) / S);
            bool onScreen = !behind && p.x > 40 && p.x < W - 40 && p.y > 40 && p.y < H - 40;
            if (onScreen) return; // on screen the health bar above it is enough

            Vector2 c = new Vector2(W / 2f, H / 2f);
            Vector2 d = p - c;
            if (behind) d = -d;
            if (d.sqrMagnitude < 0.01f) d = Vector2.up;
            float k = Mathf.Min((W / 2f - 110f) / Mathf.Max(0.001f, Mathf.Abs(d.x)), (H / 2f - 90f) / Mathf.Max(0.001f, Mathf.Abs(d.y)));
            p = c + d * k;

            float blink = Mathf.PingPong(Time.unscaledTime * 3f, 1f);
            Rect r = new Rect(p.x - 80, p.y - 20, 160, 40);
            Block(r, new Color(color.r, color.g, color.b, 0.55f + blink * 0.4f));
            ShadowLabel(r, text, barStyle, Color.white);
        }

        void DrawHover(GameManager gm)
        {
            var p = gm.Player;
            var mouse = Mouse.current;
            if (!p || mouse == null || string.IsNullOrEmpty(p.HoverLabel)) return;
            Vector2 mp = mouse.position.ReadValue();
            string text = "Click: " + p.HoverLabel;
            float w = CalcW(small, text) + 24f;
            Rect r = new Rect(mp.x / S + 22f, (Screen.height - mp.y) / S + 18f, w, 32f);
            if (r.xMax > W) r.x = W - r.width - 4f;
            if (r.yMax > H) r.y = H - r.height - 4f;
            Tex(r, panelTex); // not registered as UI, so it never blocks clicks
            Lbl(new Rect(r.x + 12f, r.y, w, 32f), text, small);
        }

        void DrawNotes(GameManager gm)
        {
            float y = gm.Mode == GameMode.Playing ? 166 : 16;
            int shown = 0;
            int maxShown = Device.Touch ? 3 : 6;   // phones: keep the right side free for the buttons
            for (int i = gm.Notes.Count - 1; i >= 0 && shown < maxShown; i--, shown++)
            {
                var n = gm.Notes[i];
                float w = Mathf.Min(560f, CalcW(small, n.Text) + 30f);
                Rect r = new Rect(W - 14 - w, y, w, 34);
                PanelBg(r);
                Lbl(new Rect(r.x + 14, r.y, r.width - 20, r.height), $"<color=#{Hex(n.Color)}>{n.Text}</color>", small);
                y += 40;
            }
        }

        // ================= Tutorial =================

        void DrawTutorialCard(GameManager gm)
        {
            if (!Tutorial.Active || Open != Panel.None) return;
            float w = Mathf.Min(760f, W - 760f);
            if (w < 420f) w = 420f;
            float textH = CalcH(wrap, Tutorial.Text, w - 40f);
            Rect r = new Rect(W / 2f - w / 2f, 14, w, textH + 96);
            PanelBg(r);
            Lbl(new Rect(r.x + 20, r.y + 8, w - 40, 28), $"<color=#{Hex(Palette.UiGold)}><b>TUTORIAL  {Tutorial.Step + 1}/{Tutorial.Count}</b></color>", small);
            Lbl(new Rect(r.x + 20, r.y + 38, w - 40, textH + 4), Tutorial.Text, wrap);
            float by = r.yMax - 46;
            if (Tutorial.NeedsNextButton)
            {
                bool last = Tutorial.Step == Tutorial.Count - 1;
                if (Btn(new Rect(r.xMax - 160, by, 140, 36), last ? "Finish" : "Next")) Tutorial.Next();
            }
            if (Btn(new Rect(r.x + 20, by, 160, 36), "Skip tutorial"))
                Confirm("Skip the tutorial?", "You can replay it any time from the Menu (Esc).", "Skip", Tutorial.Finish, true);
        }

        void DrawGuide(GameManager gm, Camera cam)
        {
            if (guideTarget == null) return;
            var player = gm.Player;
            if (Time.unscaledTime > guideUntil || (player && GameManager.FlatDistance(player.transform.position, guideTarget.Value) < 6f))
            {
                if (player && Time.unscaledTime <= guideUntil) FX.Text(player.transform.position + Vector3.up * 2.6f, "You are here!", Palette.UiGreen);
                guideTarget = null;
                return;
            }
            if (Open != Panel.None) return;
            Color c = new Color(0.4f, 1f, 0.45f);
            DrawArrow(cam, guideTarget.Value + Vector3.up * 3f, "GO HERE", c);
            Marker(cam, guideTarget.Value, "GO HERE", new Color(0.15f, 0.6f, 0.2f));

            float w = Mathf.Min(720f, W - 760f);
            if (w < 420f) w = 420f;
            float textH = CalcH(wrap, guideText, w - 170f);
            float top = Tutorial.Active ? 150f : 14f;
            Rect r = new Rect(W / 2f - w / 2f, top, w, textH + 24f);
            PanelBg(r);
            Lbl(new Rect(r.x + 16, r.y + 12, w - 170, textH), $"<color=#7CFF8A><b>GUIDE</b></color>  {guideText}", wrap);
            if (Btn(new Rect(r.xMax - 130, r.y + (r.height - 36f) / 2f, 114, 36), "Stop")) guideTarget = null;
        }

        void DrawTutorialArrow(GameManager gm, Camera cam)
        {
            var target = Tutorial.Target(gm);
            if (target == null || Open != Panel.None) return;
            DrawArrow(cam, target.Value, "CLICK HERE", new Color(1f, 0.85f, 0.2f));
        }

        void DrawArrow(Camera cam, Vector3 world, string text, Color c)
        {
            float bounce = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) * 14f;
            if (!ToGui(cam, world, out var g)) return;
            float tipY = g.y - bounce;
            for (int i = 0; i < 6; i++)
            {
                float w = 6f + i * 6f;
                Block(new Rect(g.x - w / 2f, tipY - 6 - i * 6, w, 6), c); // triangle pointing down
            }
            Block(new Rect(g.x - 6, tipY - 72, 12, 36), c);
            ShadowLabel(new Rect(g.x - 120, tipY - 104, 240, 30), text, barStyle, c);
        }
    }
}
