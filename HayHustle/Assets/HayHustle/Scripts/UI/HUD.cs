using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HayHustle
{
    /// <summary>
    /// All screens and the in-game HUD (Unity IMGUI, so it needs no scene setup).
    /// Title, character select, stats, prompts, predator warnings, shop, truck,
    /// daily rewards and help.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        public enum Panel { None, Shop, Truck, Rewards, Help }

        public static HUD I { get; private set; }
        public Panel Open = Panel.None;
        public bool BlocksGameplay => Open != Panel.None;

        CharacterChoice choice = CharacterChoice.Boy;
        string farmNameInput = "Sunny Acres";
        bool confirmNewFarm;
        int shopTab;
        Vector2 sellScroll, buyScroll, truckScroll, manualScroll;
        int manualTab;

        GUIStyle title, h1, h2, h2c, label, labelC, small, smallC, wrap, wrapC, button, bigButton, prompt, textField;
        GUIStyle popupStyle, barStyle;
        Texture2D panelTex, rowTex, whiteTex;
        readonly List<Rect> uiRects = new List<Rect>();
        float W, H;

        void Awake() => I = this;

        void Update()
        {
            var gm = GameManager.I;
            var kb = Keyboard.current;
            if (gm == null || kb == null || gm.Mode != GameMode.Playing) return;
            if (kb.bKey.wasPressedThisFrame) Toggle(Panel.Shop);
            if (kb.rKey.wasPressedThisFrame) Toggle(Panel.Rewards);
            if (kb.hKey.wasPressedThisFrame || kb.f1Key.wasPressedThisFrame) Toggle(Panel.Help);
            if (kb.escapeKey.wasPressedThisFrame) Open = Panel.None;
        }

        void Toggle(Panel p) => Open = Open == p ? Panel.None : p;

        // ================= Styles =================

        void InitStyles()
        {
            if (label != null) return;
            panelTex = Tex(new Color(0.07f, 0.06f, 0.04f, 0.8f));
            rowTex = Tex(new Color(1f, 1f, 1f, 0.07f));
            whiteTex = Tex(Color.white);

            label = new GUIStyle(GUI.skin.label) { fontSize = 22, richText = true, wordWrap = false };
            label.normal.textColor = Color.white;
            labelC = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
            small = new GUIStyle(label) { fontSize = 18 };
            smallC = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter };
            wrap = new GUIStyle(small) { wordWrap = true };
            wrapC = new GUIStyle(wrap) { alignment = TextAnchor.UpperCenter };
            h2 = new GUIStyle(label) { fontSize = 28, fontStyle = FontStyle.Bold };
            h2c = new GUIStyle(h2) { alignment = TextAnchor.MiddleCenter };
            h1 = new GUIStyle(label) { fontSize = 42, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            title = new GUIStyle(label) { fontSize = 120, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            title.normal.textColor = new Color(1f, 0.84f, 0.32f);
            prompt = new GUIStyle(label) { fontSize = 26, alignment = TextAnchor.MiddleCenter };
            button = new GUIStyle(GUI.skin.button) { fontSize = 20, richText = true };
            bigButton = new GUIStyle(GUI.skin.button) { fontSize = 32, fontStyle = FontStyle.Bold };
            textField = new GUIStyle(GUI.skin.textField) { fontSize = 26, alignment = TextAnchor.MiddleLeft };
            popupStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = false };
            barStyle = new GUIStyle(popupStyle) { fontSize = 17 };
        }

        static Texture2D Tex(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        Rect CenterRect(float w, float h) => new Rect((W - w) / 2f, (H - h) / 2f, w, h);
        void PanelBg(Rect r)
        {
            GUI.DrawTexture(r, panelTex);
            if (Event.current.type == EventType.Repaint) uiRects.Add(r); // clicks here belong to the UI, not the farm
        }

        /// <summary>True when the mouse is over a HUD panel (so clicks don't move the farmer).</summary>
        public bool PointerOverUI(Vector2 screenPos)
        {
            float s = Screen.height / 1080f;
            Vector2 g = new Vector2(screenPos.x / s, (Screen.height - screenPos.y) / s);
            foreach (var r in uiRects) if (r.Contains(g)) return true;
            return false;
        }

        void ShadowLabel(Rect r, string text, GUIStyle style, Color color)
        {
            var old = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, color.a * 0.85f);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, style);
            style.normal.textColor = color;
            GUI.Label(r, text, style);
            style.normal.textColor = old;
        }

        bool ToGui(Camera cam, Vector3 world, out Vector2 g)
        {
            float s = Screen.height / 1080f;
            Vector3 sp = cam.WorldToScreenPoint(world);
            g = new Vector2(sp.x / s, (Screen.height - sp.y) / s);
            return sp.z > 0f && g.x > -60 && g.x < W + 60 && g.y > -60 && g.y < H + 60;
        }

        void Block(float x, float y, float w, float h, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(new Rect(x, y, w, h), whiteTex);
            GUI.color = Color.white;
        }

        void Bar(Rect r, float t, Color fill)
        {
            Block(r.x, r.y, r.width, r.height, new Color(0f, 0f, 0f, 0.5f));
            Block(r.x, r.y, r.width * Mathf.Clamp01(t), r.height, fill);
        }

        static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);

        bool CloseButton(Rect panel) => GUI.Button(new Rect(panel.xMax - 66, panel.y + 16, 50, 44), "X", button);

        // ================= Main =================

        void OnGUI()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            InitStyles();
            if (Event.current.type == EventType.Repaint) uiRects.Clear();
            float s = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(s, s, 1f));
            W = Screen.width / s;
            H = 1080f;

            switch (gm.Mode)
            {
                case GameMode.Title: DrawTitle(gm); break;
                case GameMode.CharacterSelect: DrawCharacterSelect(gm); break;
                case GameMode.Playing: DrawPlaying(gm); break;
            }
            DrawNotes(gm);
        }

        // ================= Title & character select =================

        void DrawTitle(GameManager gm)
        {
            GUI.Label(new Rect(0, 150, W, 150), "HAY HUSTLE", title);
            GUI.Label(new Rect(0, 300, W, 50), "Grow it. Guard it. Hustle it.", h2c);
            float bw = 420, x = (W - bw) / 2f, y = 450;
            if (SaveSystem.Exists)
            {
                if (GUI.Button(new Rect(x, y, bw, 80), "Continue", bigButton)) gm.ContinueGame();
                y += 100;
            }
            string newLabel = confirmNewFarm ? "Start over? Click again" : "New Farm";
            if (GUI.Button(new Rect(x, y, bw, 80), newLabel, bigButton))
            {
                if (!SaveSystem.Exists || confirmNewFarm) { confirmNewFarm = false; gm.Mode = GameMode.CharacterSelect; }
                else confirmNewFarm = true;
            }
            y += 100;
            if (GUI.Button(new Rect(x, y, bw, 80), "Quit", bigButton)) Application.Quit();
        }

        void DrawCharacterSelect(GameManager gm)
        {
            GUI.Label(new Rect(0, 50, W, 70), "Choose your farmer", h1);
            float cw = 380, ch = 540, gap = 80, x0 = (W - (cw * 2 + gap)) / 2f, y = 140;
            DrawFarmerCard(new Rect(x0, y, cw, ch), CharacterChoice.Boy, "Boy Farmer", "Straw hat, red plaid shirt, jeans and work boots.");
            DrawFarmerCard(new Rect(x0 + cw + gap, y, cw, ch), CharacterChoice.Girl, "Girl Farmer", "Sun hat, long braid, denim overalls and rubber boots.");

            float fy = y + ch + 40;
            GUI.Label(new Rect((W - 640) / 2f, fy + 6, 200, 40), "Farm name:", label);
            farmNameInput = GUI.TextField(new Rect((W - 640) / 2f + 170, fy, 470, 48), farmNameInput, 24, textField);
            if (GUI.Button(new Rect((W - 420) / 2f, fy + 80, 420, 80), "Start Farming!", bigButton))
                gm.StartNewGame(choice, farmNameInput);
            if (SaveSystem.Exists && GUI.Button(new Rect(30, 30, 160, 50), "Back", button)) gm.Mode = GameMode.Title;
        }

        void DrawFarmerCard(Rect r, CharacterChoice c, string name, string desc)
        {
            bool selected = choice == c;
            if (selected) Block(r.x - 6, r.y - 6, r.width + 12, r.height + 12, new Color(1f, 0.8f, 0.2f));
            PanelBg(r);

            // Simple portrait (the in-game farmer looks the same).
            bool boy = c == CharacterChoice.Boy;
            float cx = r.x + r.width / 2f, o = r.y + 40;
            Color hat = boy ? Palette.Straw : new Color(0.97f, 0.88f, 0.62f);
            Color shirt = boy ? new Color(0.7f, 0.15f, 0.13f) : new Color(0.95f, 0.95f, 0.92f);
            Color overalls = new Color(0.25f, 0.4f, 0.72f);
            Color boots = boy ? new Color(0.35f, 0.22f, 0.12f) : new Color(0.15f, 0.35f, 0.2f);
            Block(cx - 40, o, 80, 35, hat);
            Block(cx - (boy ? 95 : 120), o + 30, boy ? 190 : 240, 14, hat);
            Block(cx - 38, o + 44, 76, 76, Palette.Skin);
            Block(cx - 38, o + 44, 76, 16, Palette.Hair);
            Block(cx - 18, o + 76, 8, 8, Palette.Black);
            Block(cx + 10, o + 76, 8, 8, Palette.Black);
            if (!boy) Block(cx + 32, o + 70, 16, 100, Palette.Hair);
            Block(cx - 60, o + 124, 120, 120, boy ? shirt : overalls);
            if (boy)
            {
                Color plaid = new Color(0.35f, 0.07f, 0.06f);
                Block(cx - 60, o + 150, 120, 8, plaid);
                Block(cx - 60, o + 200, 120, 8, plaid);
                Block(cx - 25, o + 124, 8, 120, plaid);
                Block(cx + 17, o + 124, 8, 120, plaid);
            }
            else Block(cx - 60, o + 124, 120, 26, shirt);
            Block(cx - 88, o + 128, 26, 110, shirt);
            Block(cx + 62, o + 128, 26, 110, shirt);
            Block(cx - 88, o + 238, 26, 20, Palette.Skin);
            Block(cx + 62, o + 238, 26, 20, Palette.Skin);
            Block(cx - 55, o + 244, 50, 110, boy ? Palette.Jeans : overalls);
            Block(cx + 5, o + 244, 50, 110, boy ? Palette.Jeans : overalls);
            Block(cx - 58, o + 350, 56, 24, boots);
            Block(cx + 2, o + 350, 56, 24, boots);

            GUI.Label(new Rect(r.x, r.y + 430, r.width, 44), name, h2c);
            GUI.Label(new Rect(r.x + 20, r.y + 478, r.width - 40, 60), desc, wrapC);
            if (GUI.Button(r, GUIContent.none, GUIStyle.none)) choice = c;
        }

        // ================= In-game HUD =================

        void DrawPlaying(GameManager gm)
        {
            DrawStats(gm);
            DrawClock(gm);
            DrawInventory(gm);
            DrawWarnings(gm);
            var cam = Camera.main;
            if (cam)
            {
                DrawPredatorBars(gm, cam);
                DrawPopups(cam);
            }
            DrawPrompt(gm);
            if (Open == Panel.None) DrawHover(gm);
            GUI.Label(new Rect(20, H - 40, W - 40, 30),
                "Click anything to act   WASD move   Shift run   E use   F/Space swing net   B shop   R rewards   H help   Right-drag camera   Scroll zoom", small);

            if (DailyRewards.CanClaim(gm) && Open != Panel.Rewards && Mathf.Repeat(Time.unscaledTime, 1.2f) < 0.8f)
            {
                Rect r = new Rect(W / 2f - 230, 20, 460, 44);
                PanelBg(r);
                GUI.Label(r, $"<color=#{Hex(Palette.UiGold)}>Daily reward ready! Press R</color>", labelC);
            }

            DrawMenuButtons();

            switch (Open)
            {
                case Panel.Shop: DrawShop(gm); break;
                case Panel.Truck: DrawTruck(gm); break;
                case Panel.Rewards: DrawRewards(gm); break;
                case Panel.Help: DrawHelp(); break;
            }
        }

        void DrawStats(GameManager gm)
        {
            Rect r = new Rect(20, 20, 440, 236);
            PanelBg(r);
            float x = r.x + 18, y = r.y + 10;
            GUI.Label(new Rect(x, y, 410, 40), gm.FarmName, h2); y += 44;
            GUI.Label(new Rect(x, y, 410, 32), $"<color=#{Hex(Palette.UiGold)}>Coins</color>   {gm.Coins}", label); y += 32;
            GUI.Label(new Rect(x, y, 410, 32), $"<color=#7EC8FF>Water</color>   {gm.Water}/{gm.WaterMax}", label); y += 32;

            int rank = gm.Rank;
            bool max = rank >= GameData.MaxRank;
            int cur = gm.Xp - GameData.XpToReach(rank);
            int need = max ? 1 : GameData.XpToReach(rank + 1) - GameData.XpToReach(rank);
            string xpText = max ? "MAX" : cur + "/" + need + " XP";
            GUI.Label(new Rect(x, y, 410, 32), $"<color=#{Hex(Palette.UiGreen)}>Farm Rank {rank}</color>   {xpText}", label); y += 36;
            Bar(new Rect(x, y, 400, 12), max ? 1f : cur / (float)need, Palette.UiGreen); y += 22;
            GUI.Label(new Rect(x, y, 410, 30), $"Warehouse {gm.InventoryTotal}/{gm.WarehouseCapacity}     Animals {gm.Animals.Count}/{gm.AnimalCapacity}", small);
        }

        void DrawClock(GameManager gm)
        {
            Rect r = new Rect(W - 380, 20, 360, 168);
            PanelBg(r);
            float x = r.x + 18, y = r.y + 10;
            var dn = gm.DayNight;
            GUI.Label(new Rect(x, y, 330, 40), $"Day {dn.Day}   {dn.Clock}", h2); y += 42;
            GUI.Label(new Rect(x, y, 330, 30), dn.IsNight
                ? $"<color=#{Hex(Palette.UiRed)}>Night - stay alert!</color>"
                : $"<color=#{Hex(Palette.UiGold)}>Daytime</color>", label); y += 32;
            GUI.Label(new Rect(x, y, 330, 30), $"Wanted: <color=#{Hex(Palette.UiGold)}>{GameData.ProductName(gm.WantedProduct)} x2 price</color>", small); y += 28;
            GUI.Label(new Rect(x, y, 330, 30), $"Predators caught {gm.PredatorsCaught}    Lost {gm.AnimalsLost}", small);
        }

        void DrawInventory(GameManager gm)
        {
            var lines = new List<string>();
            foreach (var pd in GameData.Products.Values)
            {
                int n = gm.Inventory[(int)pd.Product];
                if (n > 0) lines.Add($"{pd.Name}  x{n}");
            }
            Rect r = new Rect(20, 270, 260, 48 + Mathf.Max(1, lines.Count) * 28);
            PanelBg(r);
            GUI.Label(new Rect(r.x + 16, r.y + 8, 240, 30), "Warehouse", label);
            if (lines.Count == 0) GUI.Label(new Rect(r.x + 16, r.y + 40, 240, 28), "<color=#AAAAAA>empty</color>", small);
            for (int i = 0; i < lines.Count; i++)
                GUI.Label(new Rect(r.x + 16, r.y + 40 + i * 28, 240, 28), lines[i], small);
        }

        void DrawPrompt(GameManager gm)
        {
            var p = gm.Player;
            if (!p) return;
            string text = null;
            if (p.Busy) text = p.BusyLabel;
            else if (p.Focus != null)
                text = p.Focus.CanInteract
                    ? $"<color=#{Hex(Palette.UiGold)}>[E]</color>  {p.Focus.Prompt}"
                    : $"<color=#BBBBBB>{p.Focus.Prompt}</color>";
            if (text == null) return;
            Rect r = new Rect(W / 2f - 560, H - 200, 1120, 56);
            PanelBg(r);
            GUI.Label(r, text, prompt);
        }

        void DrawWarnings(GameManager gm)
        {
            var cam = Camera.main;
            if (!cam) return;
            foreach (var w in gm.Spawner.Warnings)
                Marker(cam, w.Position, "! " + GameData.Predators[w.Kind].Name.ToUpper(), true);
            foreach (var p in gm.Predators)
                if (p && !p.Hidden) Marker(cam, p.transform.position + Vector3.up * 1.6f, p.IsCarrying ? "! THIEF" : "!", false);
        }

        void Marker(Camera cam, Vector3 world, string text, bool incoming)
        {
            float s = Screen.height / 1080f;
            Vector3 sp = cam.WorldToScreenPoint(world);
            bool behind = sp.z < 0f;
            Vector2 p = new Vector2(sp.x / s, (Screen.height - sp.y) / s);
            bool onScreen = !behind && p.x > 40 && p.x < W - 40 && p.y > 40 && p.y < H - 40;

            if (!onScreen)
            {
                // Push the marker to the screen edge in the predator's direction.
                Vector2 c = new Vector2(W / 2f, H / 2f);
                Vector2 d = p - c;
                if (behind) d = -d;
                if (d.sqrMagnitude < 0.01f) d = Vector2.up;
                float k = Mathf.Min((W / 2f - 110f) / Mathf.Max(0.001f, Mathf.Abs(d.x)), (H / 2f - 90f) / Mathf.Max(0.001f, Mathf.Abs(d.y)));
                p = c + d * k;
            }
            else return; // on screen: the health bar above the predator shows it

            float blink = Mathf.PingPong(Time.unscaledTime * 3f, 1f);
            float w = onScreen ? 60 : 170;
            Rect r = new Rect(p.x - w / 2f, p.y - 22, w, 44);
            Block(r.x, r.y, r.width, r.height, new Color(0.85f, 0.1f, 0.08f, 0.55f + blink * 0.4f));
            GUI.Label(r, $"<b>{text}</b>", labelC);
        }

        void DrawPopups(Camera cam)
        {
            if (FX.I == null) return;
            foreach (var p in FX.I.Popups)
            {
                float age = (Time.unscaledTime - p.Born) / p.Life;
                if (!ToGui(cam, p.World + Vector3.up * age * 1.2f, out var g)) continue;
                float alpha = 1f - Mathf.Clamp01((age - 0.6f) / 0.4f);
                float pop = age < 0.1f ? Mathf.Lerp(0.6f, 1.1f, age / 0.1f) : 1f;
                popupStyle.fontSize = Mathf.Max(10, Mathf.RoundToInt(26f * p.Size * pop));
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
                int max = Mathf.Max(1, p.Def.Hits);
                int left = Mathf.Clamp(p.HitsLeft, 0, max);
                float w = Mathf.Clamp(max * 24f, 72f, 170f), h = 14f;
                Rect bar = new Rect(g.x - w / 2f, g.y, w, h);
                Block(bar.x - 3, bar.y - 3, w + 6, h + 6, new Color(0f, 0f, 0f, 0.65f));
                float seg = w / max;
                for (int i = 0; i < max; i++)
                    Block(bar.x + i * seg + 1, bar.y, seg - 2, h, i < left ? new Color(0.92f, 0.2f, 0.12f) : new Color(0.25f, 0.25f, 0.25f));
                string label = p.Def.Name + "  -  " + left + (left == 1 ? " hit left" : " hits left");
                if (p.IsCarrying) label = "THIEF!  " + label;
                if (p.Def.Flying && !p.CanBeHit) label += "  (hit it when it dives)";
                ShadowLabel(new Rect(g.x - 260, g.y - 30, 520, 28), label, barStyle, Color.white);
            }
        }

        void DrawHover(GameManager gm)
        {
            var p = gm.Player;
            var mouse = Mouse.current;
            if (!p || mouse == null || string.IsNullOrEmpty(p.HoverLabel)) return;
            float s = Screen.height / 1080f;
            Vector2 mp = mouse.position.ReadValue();
            var content = new GUIContent("Click: " + p.HoverLabel);
            Vector2 size = small.CalcSize(content);
            Rect r = new Rect(mp.x / s + 22f, (Screen.height - mp.y) / s + 18f, size.x + 20f, 32f);
            if (r.xMax > W) r.x = W - r.width - 4f;
            if (r.yMax > H) r.y = H - r.height - 4f;
            GUI.DrawTexture(r, panelTex); // not registered as UI, so it never blocks clicks
            GUI.Label(new Rect(r.x + 10f, r.y + 3f, size.x + 4f, 28f), content, small);
        }

        void DrawNotes(GameManager gm)
        {
            float y = gm.Mode == GameMode.Playing ? 205 : 20;
            int shown = 0;
            for (int i = gm.Notes.Count - 1; i >= 0 && shown < 6; i--, shown++)
            {
                var n = gm.Notes[i];
                Rect r = new Rect(W - 680, y, 660, 40);
                PanelBg(r);
                GUI.Label(new Rect(r.x + 12, r.y + 6, r.width - 24, 30), $"<color=#{Hex(n.Color)}>{n.Text}</color>", small);
                y += 46;
            }
        }

        // ================= Shop =================

        void DrawShop(GameManager gm)
        {
            Rect r = CenterRect(940, 700);
            PanelBg(r);
            GUI.Label(new Rect(r.x + 30, r.y + 18, 500, 44), "Animal Shop", h2);
            if (CloseButton(r)) Open = Panel.None;
            if (GUI.Toggle(new Rect(r.x + 30, r.y + 74, 180, 44), shopTab == 0, "Buy animals", button)) shopTab = 0;
            if (GUI.Toggle(new Rect(r.x + 220, r.y + 74, 180, 44), shopTab == 2, "Helpers", button)) shopTab = 2;
            if (GUI.Toggle(new Rect(r.x + 410, r.y + 74, 180, 44), shopTab == 1, "Sell animals", button)) shopTab = 1;
            GUI.Label(new Rect(r.x + 610, r.y + 82, 320, 36), $"Coins {gm.Coins}   Animals {gm.Animals.Count}/{gm.AnimalCapacity}", small);
            Rect area = new Rect(r.x + 30, r.y + 136, r.width - 60, r.height - 160);
            if (shopTab == 0) DrawBuy(gm, area);
            else if (shopTab == 2) DrawHelpers(gm, area);
            else DrawSell(gm, area);
        }

        void DrawBuy(GameManager gm, Rect area)
        {
            const float rowH = 160f, step = 172f;
            Rect view = new Rect(0, 0, area.width - 24, GameData.Animals.Count * step);
            buyScroll = GUI.BeginScrollView(area, buyScroll, view);
            float y = 0;
            foreach (var d in GameData.Animals.Values)
            {
                Rect row = new Rect(0, y, view.width, rowH);
                GUI.DrawTexture(row, rowTex);
                GUI.Label(new Rect(row.x + 16, row.y + 10, 500, 36), d.Name, h2);
                GUI.Label(new Rect(row.x + 16, row.y + 50, 540, 30), $"Makes {GameData.ProductName(d.Produces)} every {d.ProduceTime:0}s  -  grows up in {d.GrowTime:0}s", small);
                GUI.Label(new Rect(row.x + 16, row.y + 82, 540, 70), "Perk: " + d.Perk, wrap);
                if (gm.Rank < d.UnlockRank)
                {
                    GUI.Label(new Rect(row.xMax - 300, row.y + 60, 290, 40), $"<color=#AAAAAA>Unlocks at Farm Rank {d.UnlockRank}</color>", small);
                }
                else
                {
                    if (GUI.Button(new Rect(row.xMax - 290, row.y + 28, 270, 48), $"Buy {d.BabyName.ToLower()} - {d.BabyPrice}", button)) gm.BuyAnimal(d.Kind, Stage.Baby);
                    if (GUI.Button(new Rect(row.xMax - 290, row.y + 86, 270, 48), $"Buy adult - {d.AdultPrice}", button)) gm.BuyAnimal(d.Kind, Stage.Adult);
                }
                y += step;
            }
            GUI.EndScrollView();
        }

        void DrawHelpers(GameManager gm, Rect area)
        {
            float y = area.y;
            foreach (var d in GameData.Helpers.Values)
            {
                Rect row = new Rect(area.x, y, area.width, 150);
                GUI.DrawTexture(row, rowTex);
                int have = gm.CountHelpers(d.Kind);
                GUI.Label(new Rect(row.x + 16, row.y + 10, 500, 36), $"{d.Name}   <color=#AAAAAA>({have}/{d.Max})</color>", h2);
                GUI.Label(new Rect(row.x + 16, row.y + 54, 540, 80), d.Description, wrap);
                if (gm.Rank < d.UnlockRank)
                    GUI.Label(new Rect(row.xMax - 300, row.y + 55, 290, 40), $"<color=#AAAAAA>Unlocks at Farm Rank {d.UnlockRank}</color>", small);
                else if (have >= d.Max)
                    GUI.Label(new Rect(row.xMax - 300, row.y + 55, 290, 40), "<color=#AAAAAA>You have the maximum</color>", small);
                else if (GUI.Button(new Rect(row.xMax - 290, row.y + 50, 270, 52), $"Buy - {d.Price} coins", button))
                    gm.BuyHelper(d.Kind);
                y += 162;
            }
        }

        void DrawSell(GameManager gm, Rect area)
        {
            var groups = new SortedDictionary<string, List<Animal>>();
            foreach (var a in gm.Animals)
            {
                if (!a || a.IsCarried || a.InBarn) continue;
                if (!groups.TryGetValue(a.DisplayName, out var list))
                {
                    list = new List<Animal>();
                    groups[a.DisplayName] = list;
                }
                list.Add(a);
            }
            if (groups.Count == 0)
            {
                GUI.Label(new Rect(area.x, area.y, area.width, 40), "You have no animals to sell.", label);
                return;
            }

            GUI.Label(new Rect(area.x, area.y, area.width, 30), "<color=#AAAAAA>Prime and rare animals sell for more. Hungry animals sell for less.</color>", small);
            Rect scrollArea = new Rect(area.x, area.y + 36, area.width, area.height - 36);
            Rect view = new Rect(0, 0, scrollArea.width - 24, groups.Count * 64);
            sellScroll = GUI.BeginScrollView(scrollArea, sellScroll, view);
            float y = 0;
            Animal toSell = null;
            foreach (var kv in groups)
            {
                var a = kv.Value[0];
                int price = GameData.SellPrice(a);
                GUI.DrawTexture(new Rect(0, y, view.width, 58), rowTex);
                GUI.Label(new Rect(14, y + 12, 520, 36), $"{kv.Key}   x{kv.Value.Count}", label);
                if (GUI.Button(new Rect(view.width - 260, y + 7, 246, 44), $"Sell 1 - {price} coins", button)) toSell = a;
                y += 64;
            }
            GUI.EndScrollView();
            if (toSell != null) gm.SellAnimal(toSell);
        }

        // ================= Truck =================

        void DrawTruck(GameManager gm)
        {
            var t = gm.Truck;
            Rect r = CenterRect(1060, 720);
            PanelBg(r);
            GUI.Label(new Rect(r.x + 30, r.y + 18, 500, 44), "Delivery Truck", h2);
            if (CloseButton(r)) Open = Panel.None;
            GUI.Label(new Rect(r.x + 30, r.y + 66, 1000, 30), "Load goods, then send the truck to town. It comes back with your coins.", small);

            var shown = new List<ProductDef>();
            foreach (var pd in GameData.Products.Values)
                if (gm.Rank >= pd.UnlockRank || gm.Inventory[(int)pd.Product] > 0 || t.Cargo[(int)pd.Product] > 0) shown.Add(pd);

            Rect listArea = new Rect(r.x + 30, r.y + 110, r.width - 60, r.height - 220);
            Rect view = new Rect(0, 0, listArea.width - 24, shown.Count * 70);
            truckScroll = GUI.BeginScrollView(listArea, truckScroll, view);
            float y = 0;
            foreach (var pd in shown)
            {
                int have = gm.Inventory[(int)pd.Product];
                int inTruck = t.Cargo[(int)pd.Product];
                int price = gm.ProductPrice(pd.Product);
                Rect row = new Rect(0, y, view.width, 62);
                GUI.DrawTexture(row, rowTex);
                string wanted = pd.Product == gm.WantedProduct ? $"   <color=#{Hex(Palette.UiGold)}>WANTED x2</color>" : "";
                GUI.Label(new Rect(row.x + 14, row.y + 14, 600, 36), $"<b>{pd.Name}</b>   have {have}   in truck {inTruck}   {price} each{wanted}", label);
                if (GUI.Button(new Rect(row.xMax - 380, row.y + 9, 110, 44), "+1", button)) t.Load(pd.Product, 1);
                if (GUI.Button(new Rect(row.xMax - 260, row.y + 9, 110, 44), "+All", button)) t.Load(pd.Product, have);
                if (GUI.Button(new Rect(row.xMax - 140, row.y + 9, 126, 44), "Return", button)) t.Unload(pd.Product);
                y += 70;
            }
            GUI.EndScrollView();

            float fy = r.yMax - 92;
            GUI.Label(new Rect(r.x + 30, fy + 18, 600, 40), $"Cargo {t.CargoCount}/{Truck.Capacity}     Value <color=#{Hex(Palette.UiGold)}>{t.CargoValue} coins</color>", label);
            GUI.enabled = t.CargoCount > 0;
            if (GUI.Button(new Rect(r.xMax - 330, fy, 300, 70), "Send truck", bigButton))
            {
                t.Send();
                Open = Panel.None;
            }
            GUI.enabled = true;
        }

        // ================= Daily rewards =================

        void DrawRewards(GameManager gm)
        {
            Rect r = CenterRect(1200, 540);
            PanelBg(r);
            GUI.Label(new Rect(r.x, r.y + 20, r.width, 56), "Daily Rewards", h1);
            if (CloseButton(r)) Open = Panel.None;

            bool can = DailyRewards.CanClaim(gm);
            int next = DailyRewards.NextIndex(gm);
            float bw = 150, gap = 12, x = r.x + (r.width - (7 * bw + 6 * gap)) / 2f, y = r.y + 100;
            for (int i = 0; i < 7; i++)
            {
                bool claimed = can ? i < next : i < gm.RewardStreak;
                bool today = can && i == next;
                Rect b = new Rect(x + i * (bw + gap), y, bw, 230);
                Color bg = today ? new Color(0.95f, 0.72f, 0.18f, 0.95f)
                    : claimed ? new Color(0.22f, 0.5f, 0.22f, 0.95f)
                    : new Color(1f, 1f, 1f, 0.12f);
                Block(b.x, b.y, b.width, b.height, bg);
                GUI.Label(new Rect(b.x, b.y + 14, bw, 36), $"Day {i + 1}", h2c);
                GUI.Label(new Rect(b.x + 8, b.y + 76, bw - 16, 90), DailyRewards.Days[i].Label, wrapC);
                GUI.Label(new Rect(b.x, b.y + 180, bw, 36), claimed ? "Claimed" : today ? "<b>TODAY</b>" : "", smallC);
            }

            if (can)
            {
                if (GUI.Button(new Rect(r.x + (r.width - 440) / 2f, r.y + 360, 440, 74), $"Claim Day {next + 1} reward", bigButton))
                {
                    DailyRewards.Claim(gm);
                    Open = Panel.None;
                }
            }
            else GUI.Label(new Rect(r.x, r.y + 370, r.width, 50), "Come back tomorrow for your next reward!", labelC);
            GUI.Label(new Rect(r.x, r.y + 470, r.width, 40), "Log in every day to keep your streak. Missing a day resets it to Day 1.", smallC);
        }

        // ================= Menu buttons & Manual =================

        void DrawMenuButtons()
        {
            float bw = 150f, bh = 54f, gap = 10f;
            Rect bar = new Rect(W - (bw * 3 + gap * 4), H - 250f, bw * 3 + gap * 4, bh + gap * 2);
            PanelBg(bar);
            float x = bar.x + gap, y = bar.y + gap;
            if (GUI.Button(new Rect(x, y, bw, bh), "Shop  (B)", button)) Toggle(Panel.Shop);
            x += bw + gap;
            if (GUI.Button(new Rect(x, y, bw, bh), "Rewards  (R)", button)) Toggle(Panel.Rewards);
            x += bw + gap;
            if (GUI.Button(new Rect(x, y, bw, bh), "Manual  (H)", button)) Toggle(Panel.Help);
        }

        void DrawHelp()
        {
            Rect r = CenterRect(Mathf.Min(1300f, W - 40f), 860f);
            PanelBg(r);
            GUI.Label(new Rect(r.x + 30, r.y + 18, 700, 44), "User Manual - what to click", h2);
            if (CloseButton(r)) Open = Panel.None;

            // Tabs on the left
            float tx = r.x + 30, ty = r.y + 80, tw = 230, th = 56;
            for (int i = 0; i < Manual.Titles.Length; i++)
            {
                bool on = GUI.Toggle(new Rect(tx, ty + i * (th + 8), tw, th), manualTab == i, Manual.Titles[i], button);
                if (on && manualTab != i) { manualTab = i; manualScroll = Vector2.zero; }
            }

            // Page on the right (scrolls if long)
            Rect area = new Rect(tx + tw + 30, r.y + 80, r.width - tw - 90, r.height - 110);
            GUI.DrawTexture(area, rowTex);
            Rect inner = new Rect(area.x + 16, area.y + 12, area.width - 32, area.height - 24);
            var content = new GUIContent(Manual.Pages[manualTab]);
            var pageStyle = new GUIStyle(wrap) { fontSize = 20 };
            float textH = pageStyle.CalcHeight(content, inner.width - 24);
            manualScroll = GUI.BeginScrollView(inner, manualScroll, new Rect(0, 0, inner.width - 24, Mathf.Max(textH, inner.height)));
            GUI.Label(new Rect(0, 0, inner.width - 24, textH), content, pageStyle);
            GUI.EndScrollView();
        }
    }
}
