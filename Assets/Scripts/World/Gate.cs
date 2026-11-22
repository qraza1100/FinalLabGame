using UnityEngine;

namespace DrownedSanctum
{
    /// <summary>
    /// Portcullis gate. Opens when the explorer arrives carrying the right sigil (or the Heart, for the exit).
    /// </summary>
    public class Gate : MonoBehaviour
    {
        public string gateName = "Moon Gate";
        [Tooltip("-1 = no sigil needed, 0 = Moon, 1 = Sun, 2 = Star")]
        public int requiredSigil = -1;
        public bool requiresHeart;
        [Tooltip("Slams shut when the escape starts (forces the new route).")]
        public bool closeOnEscape;

        [Header("Parts")]
        public Transform bars;
        public Collider blocker;
        public Light glowLight;
        public float openDepth = 3.2f;
        public float openTime = 1.2f;

        public bool IsOpen { get; private set; }

        Vector3 closedPos;
        float t, deniedCooldown;

        void Awake()
        {
            if (bars) closedPos = bars.localPosition;
        }

        void Update()
        {
            float target = IsOpen ? 1f : 0f;
            float speed = IsOpen ? 1f / openTime : 1f / 0.3f;
            t = Mathf.MoveTowards(t, target, speed * Time.deltaTime);
            if (bars) bars.localPosition = closedPos + Vector3.down * openDepth * Mathf.SmoothStep(0f, 1f, t);
            if (glowLight) glowLight.intensity = Mathf.Lerp(glowLight.intensity, IsOpen ? 0.4f : 2.2f, Time.deltaTime * 2f);
            deniedCooldown -= Time.deltaTime;
        }

        public bool CanOpen(GameManager gm)
        {
            if (requiredSigil >= 0 && !gm.HasSigil(requiredSigil)) return false;
            if (requiresHeart && !gm.HasHeart) return false;
            if (closeOnEscape && gm.EscapeActive) return false;
            return true;
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            if (blocker) blocker.enabled = false;
            SFX.Play(s => s.gateOpen);
            if (GameManager.I) GameManager.I.OnGateOpened(this);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (blocker) blocker.enabled = true;
            SFX.Play(s => s.gateClose);
        }

        void OnTriggerEnter(Collider other) { TryPlayer(other); }
        void OnTriggerStay(Collider other) { TryPlayer(other); }

        void TryPlayer(Collider other)
        {
            var gm = GameManager.I;
            if (gm == null || gm.State != GameState.Playing || IsOpen) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;
            if (CanOpen(gm)) Open();
            else if (deniedCooldown <= 0f)
            {
                deniedCooldown = 3f;
                gm.GateDenied(this);
            }
        }
    }
}
