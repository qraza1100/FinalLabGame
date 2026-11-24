using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DrownedSanctum
{
    [Serializable]
    public class PlayerProfile
    {
        public string name;
        public int bestScore;
        public float bestTime = -1f;
        public int runs;
        public int escapes;
        public int totalPearls;
        public string lastPlayed;
    }

    [Serializable]
    public class ScoreEntry
    {
        public string name;
        public int score;
        public float time;
        public bool escaped;
        public string date;
    }

    [Serializable]
    public class SaveData
    {
        public string currentPlayer = "";
        public List<PlayerProfile> profiles = new List<PlayerProfile>();
        public List<ScoreEntry> leaderboard = new List<ScoreEntry>();
    }

    public struct RunRecord
    {
        public bool newBest;
        public bool newBestTime;
        public int rank;
        public PlayerProfile profile;
    }

    /// <summary>
    /// Player profiles, the leaderboard and options, saved as JSON in PlayerPrefs.
    /// </summary>
    public static class SaveSystem
    {
        // The automated playtest uses its own slot so it never touches real players' scores.
        public const string AutotestSaveKey = "DrownedSanctum.Save.autotest";
        static string SaveKey => DevAutoTest.Enabled || DevCapture.Enabled ? AutotestSaveKey : "DrownedSanctum.Save.v1";
        public const int LeaderboardSize = 10;
        public static readonly string[] DifficultyNames = { "Easy", "Normal", "Hard" };

        static SaveData data;

        public static SaveData Data
        {
            get
            {
                if (data == null) Load();
                return data;
            }
        }

        public static void Load()
        {
            data = null;
            string json = PlayerPrefs.GetString(SaveKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                try { data = JsonUtility.FromJson<SaveData>(json); }
                catch (Exception e) { Debug.LogWarning("Save data unreadable, starting fresh: " + e.Message); }
            }
            if (data == null) data = new SaveData();
            if (data.profiles == null) data.profiles = new List<PlayerProfile>();
            if (data.leaderboard == null) data.leaderboard = new List<ScoreEntry>();
        }

        public static void Save()
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        public static string Sanitize(string n)
        {
            n = (n ?? "").Trim();
            if (n.Length > 16) n = n.Substring(0, 16);
            return n.Length == 0 ? "Explorer" : n;
        }

        public static string CurrentPlayer
        {
            get { return string.IsNullOrWhiteSpace(Data.currentPlayer) ? "Explorer" : Data.currentPlayer; }
            set
            {
                Data.currentPlayer = Sanitize(value);
                GetOrCreate(Data.currentPlayer);
                Save();
            }
        }

        public static PlayerProfile Current => GetOrCreate(CurrentPlayer);

        public static PlayerProfile GetOrCreate(string name)
        {
            name = Sanitize(name);
            var p = Data.profiles.Find(x => string.Equals(x.name, name, StringComparison.OrdinalIgnoreCase));
            if (p == null)
            {
                p = new PlayerProfile { name = name, lastPlayed = "never" };
                Data.profiles.Add(p);
            }
            return p;
        }

        public static RunRecord RecordRun(int score, float time, bool escaped, int pearls)
        {
            var p = Current;
            var r = new RunRecord { profile = p };
            p.runs++;
            p.totalPearls += pearls;
            p.lastPlayed = DateTime.Now.ToString("dd MMM yyyy HH:mm");
            if (score > p.bestScore) { p.bestScore = score; r.newBest = true; }
            if (escaped)
            {
                p.escapes++;
                if (p.bestTime < 0f || time < p.bestTime) { p.bestTime = time; r.newBestTime = true; }
            }
            var e = new ScoreEntry { name = p.name, score = score, time = time, escaped = escaped, date = DateTime.Now.ToString("dd MMM") };
            Data.leaderboard.Add(e);
            Data.leaderboard = Data.leaderboard.OrderByDescending(x => x.score).ThenBy(x => x.time).Take(LeaderboardSize).ToList();
            r.rank = Data.leaderboard.IndexOf(e) + 1; // 0 = did not make the board
            Save();
            return r;
        }

        public static List<ScoreEntry> TopScores(int n) => Data.leaderboard.Take(n).ToList();

        public static void ResetAll()
        {
            string cur = CurrentPlayer;
            data = new SaveData { currentPlayer = cur };
            GetOrCreate(cur);
            Save();
        }

        // ---------------- options
        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat("DrownedSanctum.MasterVolume", 0.8f);
            set { PlayerPrefs.SetFloat("DrownedSanctum.MasterVolume", Mathf.Clamp01(value)); AudioListener.volume = Mathf.Clamp01(value); }
        }

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat("DrownedSanctum.MusicVolume", 0.6f);
            set => PlayerPrefs.SetFloat("DrownedSanctum.MusicVolume", Mathf.Clamp01(value));
        }

        public static int Difficulty
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt("DrownedSanctum.Difficulty", 1), 0, 2);
            set => PlayerPrefs.SetInt("DrownedSanctum.Difficulty", Mathf.Clamp(value, 0, 2));
        }

        public static string FormatTime(float t)
        {
            if (t < 0f) return "--:--";
            int m = (int)(t / 60f);
            int s = (int)(t % 60f);
            return m.ToString("00") + ":" + s.ToString("00");
        }
    }
}
