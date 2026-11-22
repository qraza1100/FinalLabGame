using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace DrownedSanctum
{
    /// <summary>In-game HUD plus the intro, pause, help and end-of-run panels.</summary>
    public class GameHUD : MonoBehaviour
    {
        public static readonly Color Cyan = new Color(0.24f, 0.96f, 0.88f);
        public static readonly Color Amber = new Color(1f, 0.71f, 0.23f);
        public static readonly Color Alarm = new Color(1f, 0.32f, 0.32f);
        public static readonly Color PearlWhite = new Color(0.95f, 0.94f, 0.9f);
        static readonly Color Dim = new Color(1f, 1f, 1f, 0.16f);

        [Header("HUD")]
        public Text playerNameText;
        public Text scoreText;
        public Text pearlsText;
        public Text oxygenText;
        public Image oxygenFill;
        public Text objectiveText;
        public GameObject timerPanel;
        public Text timerText;
        public Text sonarText;
        public Image[] sigilIcons = new Image[3];
        public Image heartIcon;
        public Text toastText;
        public CanvasGroup toastGroup;
        public CanvasGroup damageFlash;
        public RectTransform popupRoot;
        public Font popupFont;

        [Header("Panels")]
        public GameObject introPanel;
        public GameObject pausePanel;
        public GameObject helpPanel;
        public GameObject endPanel;
        public Text endTitle;
        public Text endReason;
        public Text endLabels;
        public Text endValues;
        public Text endBest;

        [Header("Buttons")]
        public Button introStartButton;
        public Button resumeButton;
        public Button pauseHelpButton;
        public Button restartButton;
        public Button pauseMenuButton;
        public Button helpCloseButton;
        public Button endRetryButton;
        public Button endMenuButton;

        float toastTimer, flash;

        void Start()
        {
            var gm = GameManager.I;
            if (introStartButton) introStartButton.onClick.AddListener(() => GameManager.I.BeginPlay());
            if (resumeButton) resumeButton.onClick.AddListener(() => GameManager.I.Resume());
            if (restartButton) restartButton.onClick.AddListener(() => GameManager.I.Restart());
            if (pauseMenuButton) pauseMenuButton.onClick.AddListener(() => GameManager.I.ToMenu());
            if (pauseHelpButton) pauseHelpButton.onClick.AddListener(() => helpPanel.SetActive(true));
            if (helpCloseButton) helpCloseButton.onClick.AddListener(() => helpPanel.SetActive(false));
            if (endRetryButton) endRetryButton.onClick.AddListener(() => GameManager.I.Restart());
            if (endMenuButton) endMenuButton.onClick.AddListener(() => GameManager.I.ToMenu());
            if (playerNameText) playerNameText.text = "EXPLORER  " + SaveSystem.CurrentPlayer.ToUpperInvariant();
            if (pausePanel) pausePanel.SetActive(false);
            if (helpPanel) helpPanel.SetActive(false);
            if (endPanel) endPanel.SetActive(false);
            if (timerPanel) timerPanel.SetActive(false);
            if (toastGroup) toastGroup.alpha = 0f;
            if (damageFlash) damageFlash.alpha = 0f;
            if (gm == null) enabled = false;
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null) return;

            if (scoreText) scoreText.text = gm.Score.ToString("N0");
            if (pearlsText) pearlsText.text = gm.Pearls + " / " + gm.PearlsTotal;

            float o = gm.MaxOxygen > 0f ? gm.Oxygen / gm.MaxOxygen : 0f;
            if (oxygenFill)
            {
                oxygenFill.fillAmount = o;
                oxygenFill.color = o < 0.25f
                    ? Color.Lerp(Alarm, Color.white, Mathf.PingPong(Time.unscaledTime * 3f, 1f) * 0.35f)
                    : Color.Lerp(Amber, Cyan, Mathf.InverseLerp(0.25f, 0.6f, o));
            }
            if (oxygenText) oxygenText.text = (gm.Refilling ? "REFILLING  " : "") + Mathf.CeilToInt(o * 100f) + "%";

            for (int i = 0; i < sigilIcons.Length; i++)
                if (sigilIcons[i]) sigilIcons[i].color = gm.HasSigil(i) ? Cyan : Dim;
            if (heartIcon) heartIcon.color = gm.HasHeart ? Amber : Dim;

            if (objectiveText) objectiveText.text = gm.CurrentObjective(out _);

            if (timerPanel)
            {
                bool showTimer = gm.EscapeActive && (gm.State == GameState.Playing || gm.State == GameState.Paused);
                if (timerPanel.activeSelf != showTimer) timerPanel.SetActive(showTimer);
                if (showTimer && timerText)
                {
                    timerText.text = "ESCAPE  " + SaveSystem.FormatTime(Mathf.Ceil(gm.EscapeTimeLeft));
                    float pulse = gm.EscapeTimeLeft < 20f ? Mathf.PingPong(Time.unscaledTime * 4f, 1f) : 0f;
                    timerText.color = Color.Lerp(new Color(1f, 0.55f, 0.4f), Color.white, pulse * 0.6f);
                }
            }

            if (sonarText)
                sonarText.text = gm.SonarCooldownLeft > 0f
                    ? "SONAR RECHARGING  " + Mathf.CeilToInt(gm.SonarCooldownLeft) + "s"
                    : "SPACE  SONAR READY";

            toastTimer -= Time.unscaledDeltaTime;
            if (toastGroup) toastGroup.alpha = Mathf.Clamp01(toastTimer / 0.5f);
            flash = Mathf.Max(0f, flash - Time.unscaledDeltaTime * 2.2f);
            if (damageFlash) damageFlash.alpha = flash;
        }

        public void ShowIntro(bool on) { if (introPanel) introPanel.SetActive(on); }

        public void ShowPause(bool on)
        {
            if (pausePanel) pausePanel.SetActive(on);
            if (helpPanel) helpPanel.SetActive(false);
        }

        public void Toast(string msg, Color c)
        {
            if (!toastText) return;
            toastText.text = msg;
            toastText.color = c;
            toastTimer = 3.8f;
        }

        public void FlashDamage() { flash = 0.85f; }

        public void Popup(string text, Vector3 worldPos, Color color)
        {
            if (!popupRoot || !popupFont || Camera.main == null) return;
            Vector3 sp = Camera.main.WorldToScreenPoint(worldPos + Vector3.up * 1.2f);
            if (sp.z < 0f) return;
            var go = new GameObject("Popup", typeof(RectTransform));
            go.transform.SetParent(popupRoot, false);
            var t = go.AddComponent<Text>();
            t.font = popupFont;
            t.fontSize = 34;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.text = text;
            t.color = color;
            var sh = go.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.8f);
            sh.effectDistance = new Vector2(2f, -2f);
            var rt = (RectTransform)go.transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(popupRoot, sp, null, out Vector2 local);
            rt.anchoredPosition = local;
            StartCoroutine(AnimatePopup(rt, t));
        }

        IEnumerator AnimatePopup(RectTransform rt, Text t)
        {
            float life = 1.1f, a = 0f;
            Vector2 start = rt.anchoredPosition;
            Color c = t.color;
            while (a < life)
            {
                a += Time.unscaledDeltaTime;
                float k = a / life;
                rt.anchoredPosition = start + Vector2.up * (70f * k);
                c.a = 1f - k * k;
                t.color = c;
                yield return null;
            }
            Destroy(rt.gameObject);
        }

        public void ShowEnd(bool won, string reason, RunRecord rec)
        {
            var gm = GameManager.I;
            var st = gm.settings;
            if (endPanel) endPanel.SetActive(true);
            if (timerPanel) timerPanel.SetActive(false);
            if (endTitle)
            {
                endTitle.text = won ? "ESCAPED!" : "GAME OVER";
                endTitle.color = won ? Cyan : Alarm;
            }
            if (endReason) endReason.text = reason;

            var labels = new StringBuilder();
            var values = new StringBuilder();
            labels.AppendLine("Pearls  x" + gm.Pearls);            values.AppendLine("+" + gm.Pearls * st.pearlScore);
            labels.AppendLine("Sigils  x" + gm.SigilCount);        values.AppendLine("+" + gm.SigilCount * st.sigilScore);
            labels.AppendLine("Heart of Eternity");                values.AppendLine(gm.HasHeart ? "+" + st.heartScore : "-");
            labels.AppendLine("Time bonus");                       values.AppendLine(won ? "+" + gm.TimeBonus : "-");
            labels.AppendLine("Hits taken  x" + gm.Hits);          values.AppendLine(gm.Hits > 0 ? "-" + gm.Hits * st.hitPenalty : "0");
            labels.AppendLine("Dive time");                        values.AppendLine(SaveSystem.FormatTime(gm.Elapsed));
            labels.Append("TOTAL SCORE");                          values.Append(gm.Score.ToString("N0"));
            if (endLabels) endLabels.text = labels.ToString();
            if (endValues) endValues.text = values.ToString();

            if (endBest && rec.profile != null)
            {
                string s = rec.profile.name + "'s best: " + rec.profile.bestScore.ToString("N0");
                if (rec.newBest) s += "   NEW PERSONAL BEST!";
                if (rec.rank > 0) s += "\nLeaderboard rank #" + rec.rank;
                if (won && rec.newBestTime) s += "   Fastest escape: " + SaveSystem.FormatTime(rec.profile.bestTime);
                endBest.text = s;
            }
        }
    }
}
