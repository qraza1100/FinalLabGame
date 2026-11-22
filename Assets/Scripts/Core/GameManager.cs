using UnityEngine;
using UnityEngine.SceneManagement;

namespace DrownedSanctum
{
    public enum GameState { Intro, Playing, Paused, Won, Lost }

    /// <summary>
    /// Runs one dive: oxygen, score, sigils, the Heart, the escape timer, winning and losing.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }
        public static readonly string[] SigilNames = { "Moon", "Sun", "Star" };

        [Header("Setup (filled in by the builder)")]
        public GameSettings settings;
        public PlayerController player;
        public CameraFollow cameraFollow;
        public GameHUD hud;
        public EscapeSequence escape;
        public Material particleMaterial;

        [Header("Objectives")]
        public Pickup[] sigilPickups = new Pickup[3];
        public Gate[] sigilGates = new Gate[3];
        public Pickup heartPickup;
        public Gate exitGate;

        public GameState State { get; private set; } = GameState.Intro;
        public float Oxygen { get; private set; }
        public float MaxOxygen => settings.maxOxygen;
        public int Score { get; private set; }
        public int Pearls { get; private set; }
        public int PearlsTotal { get; private set; }
        public int Hits { get; private set; }
        public bool HasHeart { get; private set; }
        public bool EscapeActive { get; private set; }
        public float EscapeTimeLeft { get; private set; }
        public float Elapsed { get; private set; }
        public int TimeBonus { get; private set; }
        public bool Refilling => refillTimer > 0f;
        public float SonarCooldownLeft => Mathf.Max(0f, sonarTimer);

        readonly bool[] sigils = new bool[3];
        float invuln, refillTimer, sonarTimer, lowOxygenBeep;

        public float DifficultyMultiplier
        {
            get
            {
                var m = settings.difficultyMultipliers;
                if (m == null || m.Length == 0) return 1f;
                return m[Mathf.Clamp(SaveSystem.Difficulty, 0, m.Length - 1)];
            }
        }

        public bool HasSigil(int i) => i >= 0 && i < sigils.Length && sigils[i];

        public int SigilCount
        {
            get { int c = 0; foreach (var s in sigils) if (s) c++; return c; }
        }

        void Awake()
        {
            I = this;
            Time.timeScale = 1f;
            FxUtil.ParticleMat = particleMaterial;
        }

        void Start()
        {
            Oxygen = settings.maxOxygen;
            foreach (var p in FindObjectsByType<Pickup>(FindObjectsSortMode.None))
                if (p.type == PickupType.Pearl) PearlsTotal++;
            State = GameState.Intro;
            if (hud) hud.ShowIntro(true);
        }

        public void BeginPlay()
        {
            if (State != GameState.Intro) return;
            State = GameState.Playing;
            if (hud)
            {
                hud.ShowIntro(false);
                hud.Toast("Find the Moon Sigil. Press SPACE for sonar if you get lost.", GameHUD.Cyan);
            }
        }

        void Update()
        {
            if (State == GameState.Intro)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    BeginPlay();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                if (State == GameState.Playing) Pause();
                else if (State == GameState.Paused) Resume();
            }
            if (State != GameState.Playing) return;

            float dt = Time.deltaTime;
            Elapsed += dt;
            invuln -= dt;
            refillTimer -= dt;
            sonarTimer -= dt;

            float drain = settings.oxygenDrainPerSecond * DifficultyMultiplier;
            if (player && player.Boosting) drain *= settings.boostDrainMultiplier;
            if (EscapeActive) drain *= settings.escapeDrainMultiplier;
            if (!Refilling) Oxygen -= drain * dt;

            if (Oxygen <= 0f) { Oxygen = 0f; Lose("Your oxygen ran out."); return; }

            if (Oxygen < settings.maxOxygen * 0.25f)
            {
                lowOxygenBeep -= dt;
                if (lowOxygenBeep <= 0f) { lowOxygenBeep = 1.2f; SFX.Play(s => s.tick, 0.6f); }
            }

            if (EscapeActive)
            {
                EscapeTimeLeft -= dt;
                if (EscapeTimeLeft <= 0f) { EscapeTimeLeft = 0f; Lose("The Sanctum sealed itself forever."); }
            }
        }

        // ------------------------------------------------------------ events from the world
        public void Collect(Pickup p)
        {
            switch (p.type)
            {
                case PickupType.Pearl:
                    Pearls++;
                    AddScore(settings.pearlScore);
                    SFX.Play(s => s.pearl, 0.8f);
                    if (hud) hud.Popup("+" + settings.pearlScore, p.transform.position, GameHUD.PearlWhite);
                    break;
                case PickupType.Sigil:
                    sigils[Mathf.Clamp(p.sigilIndex, 0, 2)] = true;
                    AddScore(settings.sigilScore);
                    SFX.Play(s => s.sigil);
                    if (hud)
                    {
                        string n = SigilNames[Mathf.Clamp(p.sigilIndex, 0, 2)];
                        hud.Toast(n + " Sigil found! The " + n + " Gate will now open for you.", GameHUD.Cyan);
                        hud.Popup("+" + settings.sigilScore, p.transform.position, GameHUD.Cyan);
                    }
                    break;
                case PickupType.Heart:
                    HasHeart = true;
                    AddScore(settings.heartScore);
                    SFX.Play(s => s.heart);
                    if (hud) hud.Popup("+" + settings.heartScore, p.transform.position, GameHUD.Amber);
                    StartEscape();
                    break;
            }
        }

        void AddScore(int v) { Score = Mathf.Max(0, Score + v); }

        void StartEscape()
        {
            EscapeActive = true;
            EscapeTimeLeft = settings.escapeTime;
            if (hud) hud.Toast("The Sanctum is collapsing! A cracked wall gave way. ESCAPE!", GameHUD.Alarm);
            if (escape) escape.Begin();
            if (cameraFollow) cameraFollow.Shake(0.6f, 1.2f);
        }

        public void DamagePlayer(Vector3 source, float multiplier = 1f)
        {
            if (State != GameState.Playing || invuln > 0f) return;
            invuln = settings.hitInvulnerability;
            float dmg = settings.hazardOxygenDamage * multiplier * DifficultyMultiplier;
            Oxygen = Mathf.Max(0f, Oxygen - dmg);
            Hits++;
            AddScore(-settings.hitPenalty);
            Vector3 dir = player.transform.position - source;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.back;
            player.Knockback(dir.normalized * settings.knockbackForce);
            SFX.Play(s => s.hit);
            if (hud)
            {
                hud.FlashDamage();
                hud.Popup("-" + Mathf.RoundToInt(dmg) + " O2", player.transform.position, GameHUD.Alarm);
            }
            if (cameraFollow) cameraFollow.Shake(0.35f, 0.3f);
            if (Oxygen <= 0f) Lose("Your oxygen ran out.");
        }

        public void RefillOxygen(float dt)
        {
            if (State != GameState.Playing) return;
            if (!Refilling) SFX.Play(s => s.vent, 0.6f);
            refillTimer = 0.25f;
            Oxygen = Mathf.Min(settings.maxOxygen, Oxygen + settings.ventRefillPerSecond * dt);
        }

        public bool TrySonar()
        {
            if (State != GameState.Playing || sonarTimer > 0f) return false;
            sonarTimer = settings.sonarCooldown;
            Oxygen = Mathf.Max(1f, Oxygen - settings.sonarOxygenCost);
            SFX.Play(s => s.sonar, 0.7f);
            return true;
        }

        public void GateDenied(Gate g)
        {
            if (!hud) return;
            if (g.closeOnEscape && EscapeActive)
                hud.Toast("This way is sealed now. Find another way out!", GameHUD.Alarm);
            else
            {
                string need = g.requiresHeart ? "the Heart of Eternity"
                            : g.requiredSigil >= 0 ? "the " + SigilNames[g.requiredSigil] + " Sigil" : "a key";
                hud.Toast("The " + g.gateName + " is sealed. It needs " + need + ".", GameHUD.Amber);
            }
            SFX.Play(s => s.denied, 0.7f);
        }

        public void OnGateOpened(Gate g)
        {
            if (hud && State == GameState.Playing) hud.Toast("The " + g.gateName + " grinds open...", GameHUD.Cyan);
        }

        /// <summary>The next thing to do, and where it is (used by the HUD and the sonar arrow).</summary>
        public string CurrentObjective(out Vector3 target)
        {
            for (int i = 0; i < 3; i++)
            {
                if (!sigils[i])
                {
                    target = sigilPickups[i] ? sigilPickups[i].transform.position : Vector3.zero;
                    return "Find the " + SigilNames[i] + " Sigil";
                }
                bool gateDone = sigilGates[i] == null || sigilGates[i].IsOpen || (i == 2 && HasHeart);
                if (!gateDone)
                {
                    target = sigilGates[i].transform.position;
                    return "Open the " + SigilNames[i] + " Gate";
                }
            }
            if (!HasHeart)
            {
                target = heartPickup ? heartPickup.transform.position : Vector3.zero;
                return "Claim the Heart of Eternity";
            }
            target = exitGate ? exitGate.transform.position : Vector3.zero;
            return "ESCAPE! Get back to the entrance gate";
        }

        // ------------------------------------------------------------ end of run
        public void Win()
        {
            if (State != GameState.Playing) return;
            State = GameState.Won;
            TimeBonus = Mathf.CeilToInt(EscapeTimeLeft) * settings.timeBonusPerSecond;
            Score += TimeBonus;
            var rec = SaveSystem.RecordRun(Score, Elapsed, true, Pearls);
            SFX.Play(s => s.win);
            if (escape) escape.End();
            if (hud) hud.ShowEnd(true, "You escaped with the Heart of Eternity!", rec);
        }

        public void Lose(string reason)
        {
            if (State != GameState.Playing) return;
            State = GameState.Lost;
            TimeBonus = 0;
            var rec = SaveSystem.RecordRun(Score, Elapsed, false, Pearls);
            SFX.Play(s => s.lose);
            if (escape) escape.End();
            if (hud) hud.ShowEnd(false, reason, rec);
        }

        public void Pause()
        {
            if (State != GameState.Playing) return;
            State = GameState.Paused;
            Time.timeScale = 0f;
            if (hud) hud.ShowPause(true);
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            State = GameState.Playing;
            Time.timeScale = 1f;
            if (hud) hud.ShowPause(false);
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void ToMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        // used by the automated playtest only
        public void DebugSetOxygen(float v) { Oxygen = Mathf.Clamp(v, 0f, settings.maxOxygen); }
    }
}
