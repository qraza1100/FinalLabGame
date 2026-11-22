using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DrownedSanctum.EditorTools
{
    public static partial class SanctumBuilder
    {
        static Font fTitle, fBody;
        static readonly Color UiDark = new Color(0.02f, 0.08f, 0.12f, 0.86f);
        static readonly Color UiDarker = new Color(0.01f, 0.04f, 0.07f, 0.94f);
        static readonly Color UiLine = new Color(0.24f, 0.96f, 0.88f, 0.45f);
        static readonly Color Pearl = new Color(0.95f, 0.94f, 0.9f);
        static readonly Color Muted = new Color(0.62f, 0.75f, 0.78f);

        static void LoadFonts()
        {
            fTitle = Load<Font>("Assets/Fonts/Cinzel.ttf");
            fBody = Load<Font>("Assets/Fonts/Exo2.ttf");
        }

        // ------------------------------------------------------------------ primitives
        static RectTransform UI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = 5; // UI
            return (RectTransform)go.transform;
        }

        static RectTransform Pos(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        static RectTransform Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        static Image Img(Transform parent, string name, string sprite, Color color, bool sliced = true)
        {
            var rt = UI(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite != null ? Spr(sprite) : null;
            img.type = sliced && sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static Image Panel(Transform parent, string name, Color fill, bool outline = true)
        {
            var img = Img(parent, name, "ui_panel", fill);
            img.raycastTarget = true;
            if (outline)
            {
                var o = Img(img.transform, "Outline", "ui_panel_outline", UiLine);
                Stretch(o.rectTransform);
            }
            return img;
        }

        static Text Txt(Transform parent, string name, string text, Font font, int size, Color color, TextAnchor align, FontStyle style = FontStyle.Normal, bool shadow = true)
        {
            var rt = UI(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.text = text;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            if (shadow)
            {
                var s = rt.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0f, 0f, 0f, 0.75f);
                s.effectDistance = new Vector2(2f, -2f);
            }
            return t;
        }

        static Button Btn(Transform parent, string name, string label, Vector2 size, bool primary = false, int fontSize = 26)
        {
            var img = Img(parent, name, "ui_pill", Color.white);
            img.raycastTarget = true;
            img.rectTransform.sizeDelta = size;
            var b = img.gameObject.AddComponent<Button>();
            var cb = b.colors;
            if (primary)
            {
                cb.normalColor = new Color(0.88f, 0.56f, 0.16f, 1f);
                cb.highlightedColor = new Color(1f, 0.74f, 0.3f, 1f);
                cb.pressedColor = new Color(0.7f, 0.42f, 0.1f, 1f);
            }
            else
            {
                cb.normalColor = new Color(0.06f, 0.26f, 0.32f, 0.92f);
                cb.highlightedColor = new Color(0.1f, 0.5f, 0.56f, 1f);
                cb.pressedColor = new Color(0.04f, 0.18f, 0.22f, 1f);
            }
            cb.selectedColor = cb.normalColor;
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.08f;
            b.colors = cb;
            var nav = b.navigation;
            nav.mode = Navigation.Mode.None;
            b.navigation = nav;
            var t = Txt(img.transform, "Label", label, fBody, fontSize, primary ? new Color(0.08f, 0.05f, 0.02f) : Pearl, TextAnchor.MiddleCenter, FontStyle.Bold, !primary);
            Stretch(t.rectTransform);
            img.gameObject.AddComponent<UISound>();
            return b;
        }

        static Slider MakeSlider(Transform parent, string name, Vector2 size)
        {
            var root = UI(name, parent);
            root.sizeDelta = size;
            var slider = root.gameObject.AddComponent<Slider>();
            var bg = Img(root, "Background", "ui_bar", new Color(0.02f, 0.1f, 0.14f, 1f));
            Stretch(bg.rectTransform);
            bg.rectTransform.offsetMin = new Vector2(0f, size.y * 0.3f);
            bg.rectTransform.offsetMax = new Vector2(0f, -size.y * 0.3f);
            var fillArea = UI("Fill Area", root);
            Stretch(fillArea);
            fillArea.offsetMin = new Vector2(4f, size.y * 0.3f);
            fillArea.offsetMax = new Vector2(-4f, -size.y * 0.3f);
            var fill = Img(fillArea, "Fill", "ui_bar", new Color(0.24f, 0.96f, 0.88f));
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.sizeDelta = Vector2.zero;
            var handleArea = UI("Handle Slide Area", root);
            Stretch(handleArea);
            handleArea.offsetMin = new Vector2(10f, 0f);
            handleArea.offsetMax = new Vector2(-10f, 0f);
            var handle = Img(handleArea, "Handle", "ui_circle", Pearl, false);
            handle.raycastTarget = true;
            handle.rectTransform.sizeDelta = new Vector2(size.y, 0f);
            handle.rectTransform.anchorMin = new Vector2(0f, 0f);
            handle.rectTransform.anchorMax = new Vector2(0f, 1f);
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;
            return slider;
        }

        static InputField MakeInput(Transform parent, string name, Vector2 size, string placeholder)
        {
            var img = Img(parent, name, "ui_pill", new Color(0.01f, 0.06f, 0.09f, 0.95f));
            img.raycastTarget = true;
            img.rectTransform.sizeDelta = size;
            var outline = Img(img.transform, "Outline", "ui_pill_outline", UiLine);
            Stretch(outline.rectTransform);
            var field = img.gameObject.AddComponent<InputField>();
            var text = Txt(img.transform, "Text", "", fBody, 30, Pearl, TextAnchor.MiddleLeft, FontStyle.Bold, false);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(28f, 4f);
            text.rectTransform.offsetMax = new Vector2(-28f, -4f);
            text.supportRichText = false;
            var ph = Txt(img.transform, "Placeholder", placeholder, fBody, 28, new Color(0.6f, 0.75f, 0.78f, 0.6f), TextAnchor.MiddleLeft, FontStyle.Italic, false);
            Stretch(ph.rectTransform);
            ph.rectTransform.offsetMin = new Vector2(28f, 4f);
            ph.rectTransform.offsetMax = new Vector2(-28f, -4f);
            field.textComponent = text;
            field.placeholder = ph;
            field.characterLimit = 16;
            field.caretColor = new Color(0.24f, 0.96f, 0.88f);
            field.selectionColor = new Color(0.24f, 0.96f, 0.88f, 0.35f);
            field.customCaretColor = true;
            return field;
        }

        static Canvas MakeCanvas(string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = 5;
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            var s = go.GetComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920f, 1080f);
            s.matchWidthOrHeight = 0.5f;
            return c;
        }

        static void MakeEventSystem()
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        static Text Bullets(Transform parent, string name, string text, int size)
        {
            var t = Txt(parent, name, text, fBody, size, Pearl, TextAnchor.UpperLeft);
            t.lineSpacing = 1.15f;
            return t;
        }

        const string HowToText =
            "<color=#3DF5E0><b>GOAL</b></color>   Find the <b>Moon, Sun and Star sigils</b>. Each one opens its matching gate. " +
            "Claim the <b><color=#FFB43A>Heart of Eternity</color></b>, then get back out through the entrance before the timer runs out.\n\n" +
            "<color=#3DF5E0><b>CONTROLS</b></color>   <b>WASD / Arrow keys</b> swim    <b>Shift</b> boost    <b>Space</b> sonar (points to your goal)    <b>Esc</b> pause\n\n" +
            "<color=#3DF5E0><b>OXYGEN</b></color>   Your air drains all the time and faster while boosting. Hover over glowing <b>air vents</b> to refill. At 0% you lose.\n\n" +
            "<color=#3DF5E0><b>DANGER</b></color>   <b>Urchins</b> and the <b>guardian shark</b> cost oxygen and points. <b>Currents</b> push you around.\n\n" +
            "<color=#3DF5E0><b>SCORE</b></color>   Pearl +10 · Sigil +50 · Heart +200 · every second left when you escape +5 · each hit -20";

        // ------------------------------------------------------------------ in-game HUD
        static GameHUD BuildHUD()
        {
            LoadFonts();
            MakeEventSystem();
            var canvas = MakeCanvas("HUD Canvas", 0);
            var root = canvas.transform;
            var hud = canvas.gameObject.AddComponent<GameHUD>();
            hud.popupFont = fBody;

            // damage flash (under everything)
            var flash = Img(root, "Damage Flash", "ui_vignette", new Color(1f, 0.1f, 0.1f, 0.9f), false);
            Stretch(flash.rectTransform);
            hud.damageFlash = flash.gameObject.AddComponent<CanvasGroup>();
            hud.damageFlash.blocksRaycasts = false;
            var popups = UI("Popups", root);
            Stretch(popups);
            hud.popupRoot = popups;

            // top-left: explorer, score, pearls
            var tl = Panel(root, "Score Panel", UiDark);
            Pos(tl.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -28), new Vector2(400, 150));
            hud.playerNameText = Txt(tl.transform, "Player", "EXPLORER", fBody, 20, GameHUD.Cyan, TextAnchor.UpperLeft, FontStyle.Bold);
            Pos(hud.playerNameText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -16), new Vector2(360, 28));
            var sl = Txt(tl.transform, "Score Label", "SCORE", fBody, 17, Muted, TextAnchor.UpperLeft, FontStyle.Bold);
            Pos(sl.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -50), new Vector2(150, 24));
            hud.scoreText = Txt(tl.transform, "Score", "0", fTitle, 46, Pearl, TextAnchor.UpperLeft, FontStyle.Bold);
            Pos(hud.scoreText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(22, -70), new Vector2(220, 60));
            var pIcon = Img(tl.transform, "Pearl Icon", "glyph_pearl", Pearl, false);
            Pos(pIcon.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(250, -62), new Vector2(40, 40));
            var pl = Txt(tl.transform, "Pearls Label", "PEARLS", fBody, 17, Muted, TextAnchor.UpperLeft, FontStyle.Bold);
            Pos(pl.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(296, -50), new Vector2(100, 24));
            hud.pearlsText = Txt(tl.transform, "Pearls", "0 / 0", fBody, 28, Pearl, TextAnchor.UpperLeft, FontStyle.Bold);
            Pos(hud.pearlsText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(296, -76), new Vector2(110, 40));

            // top-centre: objective
            var obj = Img(root, "Objective", "ui_pill", UiDark);
            Pos(obj.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(760, 58));
            var objLine = Img(obj.transform, "Outline", "ui_pill_outline", UiLine);
            Stretch(objLine.rectTransform);
            var dia = Img(obj.transform, "Diamond", "ui_diamond", GameHUD.Cyan, false);
            Pos(dia.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(26, 0), new Vector2(18, 18));
            hud.objectiveText = Txt(obj.transform, "Objective Text", "Find the Moon Sigil", fBody, 26, Pearl, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(hud.objectiveText.rectTransform);
            hud.objectiveText.rectTransform.offsetMin = new Vector2(50, 0);
            hud.objectiveText.rectTransform.offsetMax = new Vector2(-30, 0);

            // escape timer
            var tp = Img(root, "Escape Timer", "ui_pill", new Color(0.3f, 0.02f, 0.04f, 0.92f));
            Pos(tp.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -100), new Vector2(420, 66));
            var tpl = Img(tp.transform, "Outline", "ui_pill_outline", new Color(1f, 0.3f, 0.3f, 0.9f));
            Stretch(tpl.rectTransform);
            hud.timerPanel = tp.gameObject;
            hud.timerText = Txt(tp.transform, "Timer Text", "ESCAPE 01:30", fBody, 36, new Color(1f, 0.55f, 0.4f), TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(hud.timerText.rectTransform);

            // top-right: sigils + heart
            var tr = Panel(root, "Relics Panel", UiDark);
            Pos(tr.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-28, -28), new Vector2(360, 120));
            var rl = Txt(tr.transform, "Label", "SIGILS                      HEART", fBody, 17, Muted, TextAnchor.UpperLeft, FontStyle.Bold);
            Pos(rl.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -14), new Vector2(320, 24));
            string[] glyphs = { "glyph_moon", "glyph_sun", "glyph_star" };
            for (int i = 0; i < 3; i++)
            {
                var ic = Img(tr.transform, "Sigil " + i, glyphs[i], new Color(1, 1, 1, 0.16f), false);
                Pos(ic.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20 + i * 72, -42), new Vector2(64, 64));
                hud.sigilIcons[i] = ic;
            }
            hud.heartIcon = Img(tr.transform, "Heart", "glyph_heart", new Color(1, 1, 1, 0.16f), false);
            Pos(hud.heartIcon.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(262, -40), new Vector2(70, 70));

            // bottom-left: oxygen
            var ox = Panel(root, "Oxygen Panel", UiDark);
            Pos(ox.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(28, 28), new Vector2(600, 96));
            var o2 = Txt(ox.transform, "O2 Label", "O2", fTitle, 40, GameHUD.Cyan, TextAnchor.MiddleLeft, FontStyle.Bold);
            Pos(o2.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(80, 60));
            var barBg = Img(ox.transform, "Bar Background", "ui_bar", new Color(0.01f, 0.05f, 0.08f, 1f));
            Pos(barBg.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(104, 0), new Vector2(330, 28));
            var fill = Img(barBg.transform, "Fill", "ui_bar", GameHUD.Cyan, false);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            Stretch(fill.rectTransform, 3f);
            hud.oxygenFill = fill;
            hud.oxygenText = Txt(ox.transform, "O2 Percent", "100%", fBody, 26, Pearl, TextAnchor.MiddleLeft, FontStyle.Bold);
            Pos(hud.oxygenText.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(448, 0), new Vector2(150, 40));

            // bottom-right: sonar + controls hint
            hud.sonarText = Txt(root, "Sonar Hint", "SPACE  SONAR READY", fBody, 22, GameHUD.Cyan, TextAnchor.LowerRight, FontStyle.Bold);
            Pos(hud.sonarText.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-32, 64), new Vector2(600, 32));
            var ctl = Txt(root, "Controls Hint", "WASD swim  ·  SHIFT boost  ·  ESC pause", fBody, 19, Muted, TextAnchor.LowerRight);
            Pos(ctl.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-32, 32), new Vector2(600, 28));

            // toast
            hud.toastText = Txt(root, "Toast", "", fBody, 30, GameHUD.Cyan, TextAnchor.MiddleCenter, FontStyle.Bold);
            Pos(hud.toastText.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 170), new Vector2(1300, 80));
            var ol = hud.toastText.gameObject.AddComponent<Outline>();
            ol.effectColor = new Color(0f, 0f, 0f, 0.85f);
            ol.effectDistance = new Vector2(2f, -2f);
            hud.toastGroup = hud.toastText.gameObject.AddComponent<CanvasGroup>();
            hud.toastGroup.blocksRaycasts = false;

            // ---------- intro
            var intro = Img(root, "Intro", null, new Color(0f, 0.02f, 0.04f, 0.72f));
            intro.raycastTarget = true;
            Stretch(intro.rectTransform);
            hud.introPanel = intro.gameObject;
            var ip = Panel(intro.transform, "Card", UiDarker);
            Pos(ip.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1060, 600));
            var it = Txt(ip.transform, "Title", "THE DROWNED SANCTUM", fTitle, 64, Pearl, TextAnchor.UpperCenter, FontStyle.Bold);
            Pos(it.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(1000, 90));
            var story = Txt(ip.transform, "Story",
                "An old map led you to a temple beneath the sea.\nThe moment you swam inside, the gate sealed shut behind you.",
                fBody, 28, Muted, TextAnchor.UpperCenter, FontStyle.Italic);
            Pos(story.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -140), new Vector2(940, 90));
            var goal = Txt(ip.transform, "Goal",
                "Find the <color=#3DF5E0>three sigils</color>, claim the <color=#FFB43A>Heart of Eternity</color> and escape\nbefore the Sanctum seals itself forever. Watch your oxygen.",
                fBody, 30, Pearl, TextAnchor.UpperCenter, FontStyle.Bold);
            Pos(goal.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -250), new Vector2(980, 90));
            var keys = Txt(ip.transform, "Keys",
                "<color=#3DF5E0>WASD / ARROWS</color> swim     <color=#3DF5E0>SHIFT</color> boost     <color=#3DF5E0>SPACE</color> sonar     <color=#3DF5E0>ESC</color> pause",
                fBody, 24, Pearl, TextAnchor.UpperCenter, FontStyle.Bold);
            Pos(keys.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -360), new Vector2(980, 40));
            hud.introStartButton = Btn(ip.transform, "Dive Button", "DIVE IN  (ENTER)", new Vector2(420, 78), true, 30);
            Pos((RectTransform)hud.introStartButton.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(420, 78));

            // ---------- pause
            var pause = Img(root, "Pause", null, new Color(0f, 0.02f, 0.04f, 0.7f));
            pause.raycastTarget = true;
            Stretch(pause.rectTransform);
            hud.pausePanel = pause.gameObject;
            var pp = Panel(pause.transform, "Card", UiDarker);
            Pos(pp.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 560));
            var ptitle = Txt(pp.transform, "Title", "PAUSED", fTitle, 58, Pearl, TextAnchor.UpperCenter, FontStyle.Bold);
            Pos(ptitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(480, 80));
            hud.resumeButton = Btn(pp.transform, "Resume", "RESUME", new Vector2(380, 70), true);
            Pos((RectTransform)hud.resumeButton.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(380, 70));
            hud.pauseHelpButton = Btn(pp.transform, "How", "HOW TO PLAY", new Vector2(380, 70));
            Pos((RectTransform)hud.pauseHelpButton.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -240), new Vector2(380, 70));
            hud.restartButton = Btn(pp.transform, "Restart", "RESTART DIVE", new Vector2(380, 70));
            Pos((RectTransform)hud.restartButton.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -330), new Vector2(380, 70));
            hud.pauseMenuButton = Btn(pp.transform, "Menu", "MAIN MENU", new Vector2(380, 70));
            Pos((RectTransform)hud.pauseMenuButton.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -420), new Vector2(380, 70));

            // ---------- help (shown from pause)
            var help = Img(root, "Help", null, new Color(0f, 0.02f, 0.04f, 0.6f));
            help.raycastTarget = true;
            Stretch(help.rectTransform);
            hud.helpPanel = help.gameObject;
            var hp = Panel(help.transform, "Card", UiDarker);
            Pos(hp.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180, 700));
            var ht = Txt(hp.transform, "Title", "HOW TO PLAY", fTitle, 50, Pearl, TextAnchor.UpperCenter, FontStyle.Bold);
            Pos(ht.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(1100, 70));
            var hb = Bullets(hp.transform, "Body", HowToText, 25);
            Pos(hb.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(1080, 460));
            hud.helpCloseButton = Btn(hp.transform, "Close", "CLOSE", new Vector2(300, 66), true);
            Pos((RectTransform)hud.helpCloseButton.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 36), new Vector2(300, 66));

            // ---------- end of run
            var end = Img(root, "End", null, new Color(0f, 0.02f, 0.04f, 0.75f));
            end.raycastTarget = true;
            Stretch(end.rectTransform);
            hud.endPanel = end.gameObject;
            var ep = Panel(end.transform, "Card", UiDarker);
            Pos(ep.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780, 760));
            hud.endTitle = Txt(ep.transform, "Title", "ESCAPED!", fTitle, 76, GameHUD.Cyan, TextAnchor.UpperCenter, FontStyle.Bold);
            Pos(hud.endTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(740, 100));
            hud.endReason = Txt(ep.transform, "Reason", "", fBody, 26, Muted, TextAnchor.UpperCenter, FontStyle.Italic);
            Pos(hud.endReason.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -130), new Vector2(720, 40));
            hud.endLabels = Txt(ep.transform, "Labels", "", fBody, 28, Pearl, TextAnchor.UpperLeft, FontStyle.Bold);
            hud.endLabels.lineSpacing = 1.12f;
            Pos(hud.endLabels.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-120, -195), new Vector2(420, 320));
            hud.endValues = Txt(ep.transform, "Values", "", fBody, 28, GameHUD.Amber, TextAnchor.UpperRight, FontStyle.Bold);
            hud.endValues.lineSpacing = 1.12f;
            Pos(hud.endValues.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(200, -195), new Vector2(220, 320));
            hud.endBest = Txt(ep.transform, "Best", "", fBody, 23, GameHUD.Cyan, TextAnchor.UpperCenter, FontStyle.Bold);
            Pos(hud.endBest.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -530), new Vector2(720, 70));
            hud.endRetryButton = Btn(ep.transform, "Retry", "DIVE AGAIN", new Vector2(300, 72), true);
            Pos((RectTransform)hud.endRetryButton.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-165, 40), new Vector2(300, 72));
            hud.endMenuButton = Btn(ep.transform, "Menu", "MAIN MENU", new Vector2(300, 72));
            Pos((RectTransform)hud.endMenuButton.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(165, 40), new Vector2(300, 72));

            return hud;
        }

        // ------------------------------------------------------------------ main menu scene
        static void BuildMenuScene()
        {
            LoadFonts();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;
            RenderSettings.fog = false;
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.06f, 0.09f);
            camGo.AddComponent<AudioListener>();
            MakeEventSystem();

            var mgr = new GameObject("Menu");
            var menu = mgr.AddComponent<MainMenu>();
            mgr.AddComponent<DevAutoTest>();
            BuildAudio(mgr.transform, false);

            var canvas = MakeCanvas("Menu Canvas", 0);
            var root = canvas.transform;

            var bg = Img(root, "Background", "menu_bg", Color.white, false);
            Stretch(bg.rectTransform);
            bg.preserveAspect = false;
            var arf = bg.gameObject.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            arf.aspectRatio = 16f / 9f;
            var fade = Img(root, "Left Fade", "ui_fade_left", new Color(0.01f, 0.04f, 0.07f, 0.95f), false);
            fade.rectTransform.anchorMin = Vector2.zero;
            fade.rectTransform.anchorMax = new Vector2(0.62f, 1f);
            fade.rectTransform.offsetMin = fade.rectTransform.offsetMax = Vector2.zero;

            var titleGroup = UI("Title Group", root);
            Stretch(titleGroup);
            menu.titleGroup = titleGroup.gameObject;
            var kicker = Txt(titleGroup, "Kicker", "FINAL LAB GAME  ·  A TEMPLE ESCAPE BENEATH THE SEA", fBody, 22, GameHUD.Cyan, TextAnchor.UpperLeft, FontStyle.Bold);
            Pos(kicker.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(110, -78), new Vector2(900, 30));
            var title = Txt(titleGroup, "Title", "THE DROWNED\nSANCTUM", fTitle, 104, Pearl, TextAnchor.UpperLeft, FontStyle.Bold);
            title.lineSpacing = 0.85f;
            Pos(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(104, -110), new Vector2(1000, 220));
            var tOutline = title.gameObject.AddComponent<Outline>();
            tOutline.effectColor = new Color(0.02f, 0.25f, 0.28f, 0.8f);
            tOutline.effectDistance = new Vector2(2f, -2f);
            var tag = Img(titleGroup, "Tagline", "ui_pill", UiDark);
            Pos(tag.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(110, -352), new Vector2(560, 48));
            var tagLine = Img(tag.transform, "Outline", "ui_pill_outline", UiLine);
            Stretch(tagLine.rectTransform);
            var tagT = Txt(tag.transform, "Text", "Find the Heart  ·  Survive the Deep  ·  Escape", fBody, 22, Pearl, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(tagT.rectTransform);

            // ---------- main panel
            var main = UI("Main Panel", root);
            Pos(main, new Vector2(0, 1), new Vector2(0, 1), new Vector2(110, -430), new Vector2(560, 580));
            menu.mainPanel = main.gameObject;
            var nl = Txt(main, "Name Label", "EXPLORER NAME", fBody, 20, Muted, TextAnchor.UpperLeft, FontStyle.Bold);
            Pos(nl.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(6, 0), new Vector2(400, 28));
            menu.nameInput = MakeInput(main, "Name Input", new Vector2(520, 64), "Type your name...");
            Pos((RectTransform)menu.nameInput.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -30), new Vector2(520, 64));
            menu.welcomeText = Txt(main, "Welcome", "", fBody, 20, GameHUD.Cyan, TextAnchor.UpperLeft, FontStyle.Italic);
            Pos(menu.welcomeText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(8, -102), new Vector2(560, 30));
            float y = -146f;
            menu.playButton = Btn(main, "Play", "DIVE IN", new Vector2(520, 74), true, 32);
            Pos((RectTransform)menu.playButton.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, y), new Vector2(520, 74));
            y -= 88f;
            Button Side(string n, string label)
            {
                var b = Btn(main, n, label, new Vector2(520, 56));
                Pos((RectTransform)b.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, y), new Vector2(520, 56));
                y -= 64f;
                return b;
            }
            menu.howButton = Side("How", "HOW TO PLAY");
            menu.boardButton = Side("Board", "LEADERBOARD");
            menu.profileButton = Side("Profile", "PROFILE");
            menu.optionsButton = Side("Options", "OPTIONS");
            menu.quitButton = Side("Quit", "QUIT");

            var foot = Txt(root, "Footer", "v1.0  ·  Unity 6.3 + Blender 5.2  ·  Press ENTER to dive", fBody, 18, Muted, TextAnchor.LowerLeft);
            Pos(foot.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(110, 26), new Vector2(900, 26));

            var backs = new System.Collections.Generic.List<Button>();
            RectTransform Card(string name, string heading, out GameObject panelGo)
            {
                var p = Panel(root, name, UiDarker);
                Pos(p.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(170, -20), new Vector2(1180, 820));
                var h = Txt(p.transform, "Title", heading, fTitle, 54, Pearl, TextAnchor.UpperCenter, FontStyle.Bold);
                Pos(h.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -34), new Vector2(1100, 80));
                var back = Btn(p.transform, "Back", "BACK", new Vector2(280, 64), true);
                Pos((RectTransform)back.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 36), new Vector2(280, 64));
                backs.Add(back);
                panelGo = p.gameObject;
                return p.rectTransform;
            }

            // how to play
            var how = Card("How Panel", "HOW TO PLAY", out var howGo);
            menu.howPanel = howGo;
            var hb = Bullets(how, "Body", HowToText, 27);
            Pos(hb.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -140), new Vector2(1060, 560));

            // leaderboard
            var board = Card("Leaderboard Panel", "LEADERBOARD", out var boardGo);
            menu.boardPanel = boardGo;
            string[] heads = { "RANK", "EXPLORER", "SCORE", "TIME / RESULT" };
            float[] xs = { -470f, -330f, 70f, 270f };
            float[] ws = { 120f, 380f, 180f, 320f };
            TextAnchor[] al = { TextAnchor.UpperLeft, TextAnchor.UpperLeft, TextAnchor.UpperRight, TextAnchor.UpperLeft };
            var cols = new Text[4];
            for (int i = 0; i < 4; i++)
            {
                var h = Txt(board, "Head " + i, heads[i], fBody, 22, GameHUD.Cyan, al[i], FontStyle.Bold);
                Pos(h.rectTransform, new Vector2(0.5f, 1), new Vector2(0, 1), new Vector2(xs[i], -140), new Vector2(ws[i], 30));
                cols[i] = Txt(board, "Column " + i, "", fBody, 29, i == 2 ? GameHUD.Amber : Pearl, al[i], FontStyle.Bold);
                cols[i].lineSpacing = 1.2f;
                Pos(cols[i].rectTransform, new Vector2(0.5f, 1), new Vector2(0, 1), new Vector2(xs[i], -186), new Vector2(ws[i], 520));
            }
            var line = Img(board, "Divider", "ui_bar", UiLine);
            Pos(line.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -176), new Vector2(1000, 3));
            menu.boardRank = cols[0]; menu.boardName = cols[1]; menu.boardScore = cols[2]; menu.boardTime = cols[3];

            // profile
            var prof = Card("Profile Panel", "EXPLORER PROFILE", out var profGo);
            menu.profilePanel = profGo;
            menu.profileTitle = Txt(prof, "Name", "", fTitle, 40, GameHUD.Amber, TextAnchor.UpperLeft, FontStyle.Bold);
            Pos(menu.profileTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0, 1), new Vector2(-500, -140), new Vector2(460, 60));
            menu.profileStats = Txt(prof, "Stats", "", fBody, 26, Pearl, TextAnchor.UpperLeft, FontStyle.Bold);
            Pos(menu.profileStats.rectTransform, new Vector2(0.5f, 1), new Vector2(0, 1), new Vector2(-500, -210), new Vector2(440, 500));
            menu.profileOthers = Txt(prof, "Others", "", fBody, 23, Muted, TextAnchor.UpperLeft, FontStyle.Bold);
            menu.profileOthers.lineSpacing = 1.25f;
            Pos(menu.profileOthers.rectTransform, new Vector2(0.5f, 1), new Vector2(0, 1), new Vector2(-20, -150), new Vector2(540, 540));

            // options
            var opt = Card("Options Panel", "OPTIONS", out var optGo);
            menu.optionsPanel = optGo;
            var ml = Txt(opt, "Master Label", "MASTER VOLUME", fBody, 24, Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
            Pos(ml.rectTransform, new Vector2(0.5f, 1), new Vector2(0, 1), new Vector2(-400, -170), new Vector2(300, 40));
            menu.masterSlider = MakeSlider(opt, "Master Slider", new Vector2(460, 40));
            Pos((RectTransform)menu.masterSlider.transform, new Vector2(0.5f, 1), new Vector2(0, 1), new Vector2(-60, -170), new Vector2(460, 40));
            var mul = Txt(opt, "Music Label", "MUSIC VOLUME", fBody, 24, Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
            Pos(mul.rectTransform, new Vector2(0.5f, 1), new Vector2(0, 1), new Vector2(-400, -250), new Vector2(300, 40));
            menu.musicSlider = MakeSlider(opt, "Music Slider", new Vector2(460, 40));
            Pos((RectTransform)menu.musicSlider.transform, new Vector2(0.5f, 1), new Vector2(0, 1), new Vector2(-60, -250), new Vector2(460, 40));
            menu.difficultyButton = Btn(opt, "Difficulty", "DIFFICULTY: NORMAL", new Vector2(560, 66));
            Pos((RectTransform)menu.difficultyButton.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -350), new Vector2(560, 66));
            menu.difficultyText = menu.difficultyButton.GetComponentInChildren<Text>();
            var dh = Txt(opt, "Difficulty Hint", "Easy / Normal / Hard changes how fast oxygen drains and how much hazards hurt.", fBody, 20, Muted, TextAnchor.UpperCenter, FontStyle.Italic);
            Pos(dh.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -424), new Vector2(900, 30));
            menu.fullscreenButton = Btn(opt, "Fullscreen", "FULLSCREEN: ON", new Vector2(560, 66));
            Pos((RectTransform)menu.fullscreenButton.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -476), new Vector2(560, 66));
            menu.fullscreenText = menu.fullscreenButton.GetComponentInChildren<Text>();
            menu.resetButton = Btn(opt, "Reset", "RESET SCORES & PROFILES", new Vector2(560, 66));
            Pos((RectTransform)menu.resetButton.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -566), new Vector2(560, 66));
            menu.resetText = menu.resetButton.GetComponentInChildren<Text>();

            menu.backButtons = backs.ToArray();
            EditorSceneManager.SaveScene(scene, MenuScene);
        }
    }
}
