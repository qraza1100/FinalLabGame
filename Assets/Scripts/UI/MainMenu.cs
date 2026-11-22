using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DrownedSanctum
{
    /// <summary>Main menu: explorer name (profile), play, how to play, leaderboard, profile and options.</summary>
    public class MainMenu : MonoBehaviour
    {
        public string gameScene = "Game";

        [Header("Main")]
        public InputField nameInput;
        public Text welcomeText;
        public Button playButton, howButton, boardButton, profileButton, optionsButton, quitButton;

        [Header("Panels")]
        public GameObject titleGroup;
        public GameObject mainPanel, howPanel, boardPanel, profilePanel, optionsPanel;
        public Button[] backButtons;

        [Header("Leaderboard columns")]
        public Text boardRank, boardName, boardScore, boardTime;

        [Header("Profile")]
        public Text profileTitle, profileStats, profileOthers;

        [Header("Options")]
        public Slider masterSlider, musicSlider;
        public Button difficultyButton;
        public Text difficultyText;
        public Button fullscreenButton;
        public Text fullscreenText;
        public Button resetButton;
        public Text resetText;

        bool resetArmed;

        void Start()
        {
            Time.timeScale = 1f;
            if (nameInput) nameInput.text = string.IsNullOrWhiteSpace(SaveSystem.Data.currentPlayer) ? "" : SaveSystem.Data.currentPlayer;
            if (playButton) playButton.onClick.AddListener(Play);
            if (howButton) howButton.onClick.AddListener(() => Show(howPanel));
            if (boardButton) boardButton.onClick.AddListener(() => Show(boardPanel));
            if (profileButton) profileButton.onClick.AddListener(() => { CommitName(); Show(profilePanel); });
            if (optionsButton) optionsButton.onClick.AddListener(() => Show(optionsPanel));
            if (quitButton) quitButton.onClick.AddListener(Quit);
            foreach (var b in backButtons) if (b) b.onClick.AddListener(() => Show(mainPanel));

            if (masterSlider)
            {
                masterSlider.value = SaveSystem.MasterVolume;
                masterSlider.onValueChanged.AddListener(v => SaveSystem.MasterVolume = v);
            }
            if (musicSlider)
            {
                musicSlider.value = SaveSystem.MusicVolume;
                musicSlider.onValueChanged.AddListener(v => { SaveSystem.MusicVolume = v; if (SFX.I) SFX.I.ApplyMusicVolume(); });
            }
            if (difficultyButton) difficultyButton.onClick.AddListener(() => { SaveSystem.Difficulty = (SaveSystem.Difficulty + 1) % 3; RefreshOptions(); });
            if (fullscreenButton) fullscreenButton.onClick.AddListener(() => { Screen.fullScreen = !Screen.fullScreen; Invoke(nameof(RefreshOptions), 0.1f); });
            if (resetButton) resetButton.onClick.AddListener(ResetData);
            if (nameInput) nameInput.onEndEdit.AddListener(_ => RefreshWelcome());

            Show(mainPanel);
            RefreshWelcome();
            RefreshOptions();
        }

        void Update()
        {
            if (mainPanel && mainPanel.activeSelf && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
                Play();
            if (Input.GetKeyDown(KeyCode.Escape) && mainPanel && !mainPanel.activeSelf)
                Show(mainPanel);
        }

        void CommitName()
        {
            if (nameInput) SaveSystem.CurrentPlayer = nameInput.text;
        }

        public void Play()
        {
            CommitName();
            SceneManager.LoadScene(gameScene);
        }

        public void PlayAs(string playerName)
        {
            if (nameInput) nameInput.text = playerName;
            Play();
        }

        void Quit()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        public void Show(GameObject panel)
        {
            foreach (var p in new[] { mainPanel, howPanel, boardPanel, profilePanel, optionsPanel })
                if (p) p.SetActive(p == panel);
            if (titleGroup) titleGroup.SetActive(panel == mainPanel);
            if (panel == boardPanel) FillBoard();
            if (panel == profilePanel) FillProfile();
            if (panel == optionsPanel) { resetArmed = false; RefreshOptions(); }
            if (panel == mainPanel) RefreshWelcome();
        }

        public void ShowBoard() { Show(boardPanel); }
        public void ShowHow() { Show(howPanel); }
        public void ShowProfile() { CommitName(); Show(profilePanel); }
        public void ShowOptions() { Show(optionsPanel); }

        void RefreshWelcome()
        {
            if (!welcomeText) return;
            string n = nameInput && !string.IsNullOrWhiteSpace(nameInput.text) ? SaveSystem.Sanitize(nameInput.text) : null;
            if (n == null) { welcomeText.text = "Type your explorer name to start a profile."; return; }
            var p = SaveSystem.Data.profiles.Find(x => string.Equals(x.name, n, System.StringComparison.OrdinalIgnoreCase));
            welcomeText.text = p == null || p.runs == 0
                ? "New explorer: " + n + ". Good luck down there."
                : "Welcome back, " + p.name + ".  Best score " + p.bestScore.ToString("N0") + "  ·  Escapes " + p.escapes + "/" + p.runs;
        }

        void FillBoard()
        {
            var top = SaveSystem.TopScores(SaveSystem.LeaderboardSize);
            var r = new StringBuilder(); var n = new StringBuilder(); var s = new StringBuilder(); var t = new StringBuilder();
            if (top.Count == 0)
            {
                n.Append("No dives recorded yet.\nBe the first to escape!");
            }
            for (int i = 0; i < top.Count; i++)
            {
                var e = top[i];
                r.AppendLine("#" + (i + 1));
                n.AppendLine(e.name);
                s.AppendLine(e.score.ToString("N0"));
                t.AppendLine(e.escaped ? SaveSystem.FormatTime(e.time) + "  ESCAPED" : SaveSystem.FormatTime(e.time) + "  lost");
            }
            if (boardRank) boardRank.text = r.ToString();
            if (boardName) boardName.text = n.ToString();
            if (boardScore) boardScore.text = s.ToString();
            if (boardTime) boardTime.text = t.ToString();
        }

        void FillProfile()
        {
            var p = SaveSystem.Current;
            if (profileTitle) profileTitle.text = p.name.ToUpperInvariant();
            if (profileStats)
            {
                profileStats.text =
                    "Best score\n" + p.bestScore.ToString("N0") + "\n\n" +
                    "Fastest escape\n" + SaveSystem.FormatTime(p.bestTime) + "\n\n" +
                    "Dives / escapes\n" + p.runs + " / " + p.escapes + "\n\n" +
                    "Pearls collected\n" + p.totalPearls + "\n\n" +
                    "Last dive\n" + (string.IsNullOrEmpty(p.lastPlayed) ? "never" : p.lastPlayed);
            }
            if (profileOthers)
            {
                var others = SaveSystem.Data.profiles.OrderByDescending(x => x.bestScore).Take(8).ToList();
                var sb = new StringBuilder("ALL EXPLORERS\n");
                foreach (var o in others)
                    sb.AppendLine(o.name + "  ·  best " + o.bestScore.ToString("N0") + "  ·  " + o.escapes + "/" + o.runs + " escapes");
                profileOthers.text = sb.ToString();
            }
        }

        void RefreshOptions()
        {
            if (difficultyText) difficultyText.text = "DIFFICULTY:  " + SaveSystem.DifficultyNames[SaveSystem.Difficulty].ToUpperInvariant();
            if (fullscreenText) fullscreenText.text = Screen.fullScreen ? "FULLSCREEN:  ON" : "FULLSCREEN:  OFF";
            if (resetText) resetText.text = resetArmed ? "CLICK AGAIN TO ERASE ALL DATA" : "RESET SCORES & PROFILES";
        }

        void ResetData()
        {
            if (!resetArmed) { resetArmed = true; RefreshOptions(); return; }
            SaveSystem.ResetAll();
            resetArmed = false;
            RefreshOptions();
            RefreshWelcome();
        }
    }
}
