using UnityEngine;
using UnityEngine.Rendering;

namespace DrownedSanctum
{
    /// <summary>
    /// Plays the collapse after the Heart is taken: cracked walls fall, the Star Gate slams,
    /// red alarm lighting pulses and the camera rumbles.
    /// </summary>
    public class EscapeSequence : MonoBehaviour
    {
        public CrackedWall[] crackedWalls;
        public Gate[] gatesToClose;
        public Light alarmLight;
        public Volume alarmVolume;
        public Camera mainCamera;
        public AudioSource music;
        public Color alarmAmbient = new Color(0.24f, 0.07f, 0.07f);
        public Color alarmFog = new Color(0.10f, 0.02f, 0.03f);

        bool active, stopped;
        Color baseAmbient, baseFog;
        float t, rumble = 2f;

        void Start()
        {
            baseAmbient = RenderSettings.ambientLight;
            baseFog = RenderSettings.fogColor;
            if (alarmLight) alarmLight.intensity = 0f;
            if (alarmVolume) alarmVolume.weight = 0f;
        }

        public void Begin()
        {
            if (active) return;
            active = true;
            foreach (var w in crackedWalls) if (w) w.Crumble();
            foreach (var g in gatesToClose) if (g) g.Close();
            SFX.Play(s => s.collapse);
            if (music) music.pitch = 1.12f;
        }

        public void End()
        {
            stopped = true;
            if (music) music.pitch = 1f;
        }

        void Update()
        {
            if (!active) return;
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / 1.5f);
            if (stopped) k *= 0.5f;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f);
            RenderSettings.ambientLight = Color.Lerp(baseAmbient, alarmAmbient, k * (0.55f + 0.45f * pulse));
            RenderSettings.fogColor = Color.Lerp(baseFog, alarmFog, k);
            if (mainCamera) mainCamera.backgroundColor = RenderSettings.fogColor;
            if (alarmLight) alarmLight.intensity = k * (0.2f + 0.8f * pulse);
            if (alarmVolume) alarmVolume.weight = k * (0.65f + 0.35f * pulse);

            var gm = GameManager.I;
            if (stopped || gm == null || gm.State != GameState.Playing) return;
            rumble -= Time.deltaTime;
            if (rumble <= 0f)
            {
                rumble = Random.Range(2.5f, 4.5f);
                if (gm.cameraFollow) gm.cameraFollow.Shake(0.22f, 0.8f);
                SFX.Play(s => s.collapse, 0.3f);
            }
        }
    }
}
