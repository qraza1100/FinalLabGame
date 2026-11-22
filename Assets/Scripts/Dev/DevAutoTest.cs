using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DrownedSanctum
{
    /// <summary>
    /// Automated smoke test. Does nothing unless the game is started with "-autotest".
    /// It walks through the whole dive (teleporting between objectives), logs each step,
    /// saves screenshots and quits. Example:
    ///   DrownedSanctum.exe -autotest -autotestOut C:\temp\shots -screen-width 1600 -screen-height 900
    /// Add "-autotestlose" to test the losing path instead.
    /// </summary>
    public class DevAutoTest : MonoBehaviour
    {
        [Tooltip("Named test positions, filled in by the builder.")]
        public string[] pointNames = new string[0];
        public Vector3[] points = new Vector3[0];

        static bool visitedGame;
        static string[] Args => Environment.GetCommandLineArgs();
        public static bool Enabled => Args.Contains("-autotest");
        static bool LoseMode => Args.Contains("-autotestlose");

        static string OutDir
        {
            get
            {
                int i = Array.IndexOf(Args, "-autotestOut");
                if (i >= 0 && i + 1 < Args.Length) return Args[i + 1];
                return Path.Combine(Application.persistentDataPath, "autotest");
            }
        }

        float frameTimeSum;
        int frameCount;

        IEnumerator Start()
        {
            if (!Enabled) yield break;
            Directory.CreateDirectory(OutDir);
            string scene = SceneManager.GetActiveScene().name;
            Log("=== scene " + scene + "  " + Screen.width + "x" + Screen.height);
            if (scene == "MainMenu") yield return MenuRoutine();
            else yield return GameRoutine();
        }

        void Update()
        {
            if (!Enabled) return;
            frameTimeSum += Time.unscaledDeltaTime;
            frameCount++;
        }

        IEnumerator MenuRoutine()
        {
            var mm = FindAnyObjectByType<MainMenu>();
            if (!visitedGame)
            {
                PlayerPrefs.DeleteKey(SaveSystem.AutotestSaveKey);
                SaveSystem.Load();
                yield return Wait(1f);
                LogFps("menu loading");
                yield return Wait(3f);
                LogFps("menu idle");
                yield return Shot("01_menu");
                mm.ShowHow(); yield return Wait(0.4f); yield return Shot("02_howto");
                mm.ShowOptions(); yield return Wait(0.4f); yield return Shot("03_options");
                mm.ShowBoard(); yield return Wait(0.4f); yield return Shot("04_board_before");
                LogFps("menu");
                mm.PlayAs("AutoTester");
            }
            else
            {
                yield return Wait(2f);
                mm.ShowBoard(); yield return Wait(0.5f); yield return Shot("40_board_after");
                mm.ShowProfile(); yield return Wait(0.5f); yield return Shot("41_profile");
                Log("profile: best=" + SaveSystem.Current.bestScore + " runs=" + SaveSystem.Current.runs + " escapes=" + SaveSystem.Current.escapes);
                Log("=== AUTOTEST DONE");
                yield return Wait(0.5f);
                Application.Quit();
            }
        }

        IEnumerator GameRoutine()
        {
            visitedGame = true;
            var gm = GameManager.I;
            var pl = gm.player;
            yield return Wait(2f);
            yield return Shot("10_intro");
            gm.BeginPlay();
            LogFps("game loading");
            yield return Wait(3f);
            LogFps("game idle");
            yield return Shot("11_start");

            if (LoseMode)
            {
                gm.DebugSetOxygen(2f);
                yield return Wait(3.5f);
                Log("lose test: state=" + gm.State);
                yield return Shot("30_lose");
                Log("=== AUTOTEST DONE (lose)");
                Application.Quit();
                yield break;
            }

            if (gm.TrySonar()) pl.ShowSonar();
            yield return Wait(0.5f);
            yield return Shot("12_sonar");

            var pearl = FindObjectsByType<Pickup>(FindObjectsSortMode.None)
                .Where(p => p.type == PickupType.Pearl)
                .OrderBy(p => (p.transform.position - pl.transform.position).sqrMagnitude)
                .FirstOrDefault();
            if (pearl)
            {
                pl.Teleport(pearl.transform.position);
                yield return Wait(0.6f);
                Log("pearl collected: pearls=" + gm.Pearls + "/" + gm.PearlsTotal + " score=" + gm.Score);
            }

            for (int i = 0; i < 3; i++)
            {
                gm.DebugSetOxygen(100f);
                if (i == 1) { Go(pl, "west_view"); yield return Wait(1.2f); yield return Shot("14_west_wing"); }
                if (i == 2) { Go(pl, "shark_lane"); yield return Wait(1.6f); yield return Shot("16_shark"); Log("shark: hits=" + gm.Hits + " oxygen=" + gm.Oxygen.ToString("0")); }
                pl.Teleport(gm.sigilPickups[i].transform.position);
                yield return Wait(0.6f);
                Log("sigil " + i + " collected: " + gm.HasSigil(i) + "  score=" + gm.Score);
                Go(pl, "gate" + i + "_near");
                yield return Wait(2f);
                Log("gate " + i + " open: " + gm.sigilGates[i].IsOpen);
                yield return Shot("1" + (i == 0 ? "3" : i == 1 ? "5" : "7") + "_gate" + i);
            }

            Go(pl, "heart_near");
            yield return Wait(1f);
            yield return Shot("18_heart_chamber");
            pl.Teleport(gm.heartPickup.transform.position);
            yield return Wait(1.6f);
            Log("heart taken: " + gm.HasHeart + "  escape=" + gm.EscapeActive + "  timer=" + gm.EscapeTimeLeft.ToString("0.0") + "  starGateOpen=" + gm.sigilGates[2].IsOpen);
            yield return Shot("19_escape");

            Go(pl, "shaft");
            yield return Wait(1.2f);
            yield return Shot("20_shaft");

            gm.DebugSetOxygen(40f);
            Go(pl, "vent");
            yield return Wait(1.5f);
            Log("vent refill: 40 -> " + gm.Oxygen.ToString("0.0"));

            int hits = gm.Hits;
            Go(pl, "urchin");
            yield return Wait(0.8f);
            Log("urchin hit: hits " + hits + " -> " + gm.Hits + "  oxygen=" + gm.Oxygen.ToString("0.0"));

            gm.DebugSetOxygen(100f);
            Go(pl, "exit_near");
            yield return Wait(2f);
            Log("exit gate open: " + gm.exitGate.IsOpen);
            Go(pl, "exit_out");
            yield return Wait(1.5f);
            Log("final state=" + gm.State + " score=" + gm.Score + " timeBonus=" + gm.TimeBonus + " elapsed=" + gm.Elapsed.ToString("0.0"));
            yield return Shot("21_win");
            LogFps("game end");
            yield return Wait(1f);
            gm.ToMenu();
        }

        void Go(PlayerController pl, string name)
        {
            int i = Array.IndexOf(pointNames, name);
            if (i < 0 || i >= points.Length) { Log("missing test point " + name); return; }
            pl.Teleport(points[i]);
            var cf = GameManager.I.cameraFollow;
            if (cf) cf.Snap();
        }

        static IEnumerator Wait(float s) { yield return new WaitForSecondsRealtime(s); }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(OutDir, name + ".png"), tex.EncodeToPNG());
            Destroy(tex);
            Log("shot " + name);
        }

        void LogFps(string label)
        {
            if (frameCount > 0) Log("fps (" + label + "): " + (frameCount / frameTimeSum).ToString("0.0"));
            frameCount = 0;
            frameTimeSum = 0f;
        }

        static void Log(string msg)
        {
            Debug.Log("[AUTOTEST] " + msg);
            try { File.AppendAllText(Path.Combine(OutDir, "log.txt"), DateTime.Now.ToString("HH:mm:ss") + "  " + msg + "\n"); }
            catch { }
        }
    }
}
