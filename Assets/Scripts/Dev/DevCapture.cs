using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DrownedSanctum
{
    /// <summary>
    /// Trailer capture. Does nothing unless the game is started with
    ///   -capture &lt;shotlist.json&gt; -captureOut &lt;folder&gt;
    /// It plays a scripted run (autopilot swimming along real maze paths, scripted camera moves),
    /// locks game time to 30 fps and saves every frame as a JPG, one folder per shot.
    /// The game's own rules run untouched: pickups, gates, hazards and the escape all happen for real.
    /// </summary>
    public class DevCapture : MonoBehaviour
    {
        [Serializable]
        class ShotList { public int fps = 30; public int quality = 92; public Shot[] shots; }

        [Serializable]
        class Shot
        {
            public string name = "shot";
            public string action = "swim";      // menu_type, menu_dive, intro, swim, restart, to_menu, board, profile, wait
            public string text = "";            // name to type for menu_type / menu_dive
            public float[] path = new float[0]; // world x,z pairs; first pair is the start
            public float duration = 3f;         // max seconds recorded
            public float tail = 0.6f;           // seconds kept after the path ends
            public bool record = true;
            public bool boost;
            public float sonarAt = -1f;
            public float oxygen = -1f;
            public bool hud = true;
            public bool beginPlay;
            public float[] camA = new float[0]; // camera offset at shot start (x,y,z)
            public float[] camB = new float[0]; // camera offset at shot end (eased)
        }

        static DevCapture instance;
        static string[] Args => Environment.GetCommandLineArgs();
        public static bool Enabled => Args.Contains("-capture");

        static string Arg(string key)
        {
            int i = Array.IndexOf(Args, key);
            return i >= 0 && i + 1 < Args.Length ? Args[i + 1] : null;
        }

        ShotList list;
        string outDir;
        static readonly Vector3 DefaultOffset = new Vector3(0f, 15f, -8f);

        public static void Boot()
        {
            if (instance) return;
            var go = new GameObject("DevCapture");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<DevCapture>();
        }

        IEnumerator Start()
        {
            outDir = Arg("-captureOut") ?? Path.Combine(Application.persistentDataPath, "capture");
            Directory.CreateDirectory(outDir);
            string json = File.ReadAllText(Arg("-capture"));
            list = JsonUtility.FromJson<ShotList>(json);
            PlayerPrefs.DeleteKey(SaveSystem.AutotestSaveKey);
            SaveSystem.Load();
            Time.captureFramerate = list.fps;
            Log("capture start: " + list.shots.Length + " shots -> " + outDir);
            yield return null;
            foreach (var s in list.shots)
            {
                Log("shot " + s.name + " (" + s.action + ")");
                yield return RunShot(s);
            }
            PlayerController.BotActive = false;
            Log("=== CAPTURE DONE");
            Application.Quit();
        }

        IEnumerator RunShot(Shot s)
        {
            switch (s.action)
            {
                case "menu_type":
                {
                    var mm = FindAnyObjectByType<MainMenu>();
                    if (mm && mm.nameInput) mm.nameInput.text = "";
                    int frames = Frames(s.duration);
                    int typeStart = Frames(0.5f), perChar = Frames(0.13f);
                    for (int f = 0; f < frames; f++)
                    {
                        int chars = f < typeStart ? 0 : Mathf.Min(s.text.Length, 1 + (f - typeStart) / Mathf.Max(1, perChar));
                        if (mm && mm.nameInput) mm.nameInput.text = s.text.Substring(0, chars);
                        yield return Capture(s, f);
                    }
                    break;
                }
                case "menu_dive":
                {
                    var mm = FindAnyObjectByType<MainMenu>();
                    mm.PlayAs(s.text);
                    yield return WaitForGame();
                    break;
                }
                case "intro":
                {
                    var gm = GameManager.I;
                    SetHud(s.hud);
                    for (int f = 0; f < Frames(s.duration); f++) yield return Capture(s, f);
                    if (s.beginPlay) gm.BeginPlay();
                    break;
                }
                case "swim":
                    yield return Swim(s);
                    break;
                case "restart":
                    GameManager.I.Restart();
                    yield return WaitForGame();
                    break;
                case "to_menu":
                    GameManager.I.ToMenu();
                    yield return WaitForScene("MainMenu");
                    break;
                case "board":
                case "profile":
                {
                    var mm = FindAnyObjectByType<MainMenu>();
                    if (s.action == "board") mm.ShowBoard(); else mm.ShowProfile();
                    for (int f = 0; f < Frames(s.duration); f++) yield return Capture(s, f);
                    break;
                }
                default: // "wait"
                    for (int f = 0; f < Frames(s.duration); f++) yield return Capture(s, f);
                    break;
            }
        }

        IEnumerator Swim(Shot s)
        {
            var gm = GameManager.I;
            var pl = gm.player;
            var cf = gm.cameraFollow;
            if (gm.State == GameState.Intro) gm.BeginPlay();
            SetHud(s.hud);
            if (s.oxygen >= 0f) gm.DebugSetOxygen(s.oxygen);

            var pts = new Vector3[s.path.Length / 2];
            for (int i = 0; i < pts.Length; i++) pts[i] = new Vector3(s.path[i * 2], pl.transform.position.y, s.path[i * 2 + 1]);
            if (pts.Length > 0) pl.Teleport(pts[0]);

            Vector3 camA = V(s.camA, DefaultOffset), camB = V(s.camB, camA);
            if (cf) { cf.offset = camA; cf.Snap(); }

            PlayerController.BotActive = true;
            PlayerController.BotBoost = s.boost;
            int wp = Mathf.Min(1, pts.Length - 1);
            int frames = Frames(s.duration);
            int tailFrames = Frames(s.tail), sinceEnd = -1;
            bool sonarDone = s.sonarAt < 0f;
            for (int f = 0; f < frames; f++)
            {
                float t = frames > 1 ? f / (float)(frames - 1) : 1f;
                if (cf) cf.offset = Vector3.Lerp(camA, camB, t * t * (3f - 2f * t));
                if (!sonarDone && f >= Frames(s.sonarAt))
                {
                    sonarDone = true;
                    if (gm.TrySonar()) pl.ShowSonar();
                }

                Vector3 move = Vector3.zero;
                if (pts.Length > 1 && wp < pts.Length)
                {
                    Vector3 d = pts[wp] - pl.transform.position;
                    d.y = 0f;
                    if (d.magnitude < 0.55f) wp++;
                    else move = d.normalized;
                }
                PlayerController.BotMove = move;
                bool done = pts.Length <= 1 || wp >= pts.Length;
                if (done) sinceEnd++;
                if (done && sinceEnd >= tailFrames) { yield return Capture(s, f); break; }
                yield return Capture(s, f);
            }
            PlayerController.BotMove = Vector3.zero;
            PlayerController.BotBoost = false;
            if (cf) cf.offset = DefaultOffset;
        }

        IEnumerator Capture(Shot s, int f)
        {
            yield return new WaitForEndOfFrame();
            if (!s.record) yield break;
            string dir = Path.Combine(outDir, s.name);
            if (f == 0) Directory.CreateDirectory(dir);
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(dir, "f_" + f.ToString("00000") + ".jpg"), tex.EncodeToJPG(list.quality));
            Destroy(tex);
        }

        IEnumerator WaitForGame()
        {
            yield return WaitForScene("Game");
            while (GameManager.I == null) yield return null;
            yield return null;
            yield return null;
        }

        static IEnumerator WaitForScene(string name)
        {
            while (SceneManager.GetActiveScene().name != name) yield return null;
            yield return null;
        }

        static void SetHud(bool on)
        {
            var gm = GameManager.I;
            if (gm && gm.hud)
            {
                var c = gm.hud.GetComponent<Canvas>();
                if (c) c.enabled = on;
            }
        }

        int Frames(float seconds) => Mathf.Max(1, Mathf.RoundToInt(seconds * list.fps));
        static Vector3 V(float[] a, Vector3 fallback) => a != null && a.Length == 3 ? new Vector3(a[0], a[1], a[2]) : fallback;

        void Log(string msg)
        {
            Debug.Log("[CAPTURE] " + msg);
            try { File.AppendAllText(Path.Combine(outDir, "capture_log.txt"), msg + "\n"); } catch { }
        }
    }
}
