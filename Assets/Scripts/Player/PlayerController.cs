using UnityEngine;

namespace DrownedSanctum
{
    /// <summary>
    /// Top-down swimming for the explorer sub: WASD/arrows to move, Shift to boost, Space/Q for sonar.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Parts (set by the builder)")]
        public Transform visual;
        public Transform propeller;
        public ParticleSystem bubbles;
        public AudioSource engineAudio;
        public Transform sonarArrow;
        public Transform sonarRing;
        public float visualHeight = 0f;

        Rigidbody rb;
        Vector3 moveInput, external, knock;
        float yaw, prevYaw, bank, sonarShow, ringT = 1f;
        Material arrowMat, ringMat;

        // Autopilot used only by the trailer capture (DevCapture); normal play never sets these.
        public static bool BotActive;
        public static Vector3 BotMove;
        public static bool BotBoost;

        public bool Boosting { get; private set; }
        public Vector3 Velocity => rb ? rb.linearVelocity : Vector3.zero;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            yaw = prevYaw = visual ? visual.eulerAngles.y : 0f;
            if (sonarArrow)
            {
                var r = sonarArrow.GetComponentInChildren<Renderer>();
                if (r) arrowMat = r.material;
                sonarArrow.gameObject.SetActive(false);
            }
            if (sonarRing)
            {
                var r = sonarRing.GetComponentInChildren<Renderer>();
                if (r) ringMat = r.material;
                sonarRing.gameObject.SetActive(false);
            }
        }

        void Update()
        {
            var gm = GameManager.I;
            bool playing = gm != null && gm.State == GameState.Playing;
            if (playing)
            {
                float h = Input.GetAxisRaw("Horizontal");
                float v = Input.GetAxisRaw("Vertical");
                moveInput = BotActive ? Vector3.ClampMagnitude(BotMove, 1f) : Vector3.ClampMagnitude(new Vector3(h, 0f, v), 1f);
                bool was = Boosting;
                bool wantBoost = BotActive ? BotBoost : (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
                Boosting = wantBoost && moveInput.sqrMagnitude > 0.05f;
                if (Boosting && !was) SFX.Play(s => s.boost, 0.3f);
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Q))
                    if (gm.TrySonar()) ShowSonar();
            }
            else
            {
                moveInput = Vector3.zero;
                Boosting = false;
            }
            AnimateVisual();
            UpdateSonar();
        }

        void FixedUpdate()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            var st = gm.settings;
            Vector3 target = moveInput * st.moveSpeed * (Boosting ? st.boostMultiplier : 1f);
            Vector3 v = rb.linearVelocity;
            v.y = 0f;
            v = Vector3.Lerp(v, target, 1f - Mathf.Exp(-st.acceleration * Time.fixedDeltaTime));
            v += external * Time.fixedDeltaTime;
            external = Vector3.zero;
            if (knock.sqrMagnitude > 0.0001f) { v += knock; knock = Vector3.zero; }
            rb.linearVelocity = v;
        }

        public void AddCurrent(Vector3 force) { external += force; }
        public void Knockback(Vector3 impulse) { knock += impulse; }

        public void Teleport(Vector3 pos)
        {
            pos.y = transform.position.y;
            rb.position = pos;
            transform.position = pos;
            rb.linearVelocity = Vector3.zero;
            Physics.SyncTransforms();
        }

        void AnimateVisual()
        {
            if (!visual) return;
            Vector3 v = Velocity;
            v.y = 0f;
            float spd = v.magnitude;
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            prevYaw = yaw;
            if (spd > 0.3f)
            {
                float targetYaw = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
                yaw = Mathf.LerpAngle(yaw, targetYaw, 1f - Mathf.Exp(-8f * dt));
            }
            float turn = Mathf.DeltaAngle(prevYaw, yaw) / dt;
            bank = Mathf.Lerp(bank, Mathf.Clamp(-turn * 0.12f, -28f, 28f), 1f - Mathf.Exp(-6f * dt));
            float pitch = Mathf.Clamp(spd * 1.4f, 0f, 9f);
            float bob = Mathf.Sin(Time.time * 2.1f) * 0.08f;
            visual.localPosition = new Vector3(0f, visualHeight + bob, 0f);
            visual.rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(pitch, 0f, bank);

            if (propeller) propeller.Rotate(visual.forward, (180f + spd * 260f) * dt, Space.World);
            if (engineAudio)
            {
                engineAudio.pitch = 0.75f + spd * 0.07f;
                engineAudio.volume = 0.06f + spd * 0.022f;
            }
            if (bubbles)
            {
                var em = bubbles.emission;
                em.rateOverTime = 3f + spd * 5f;
            }
        }

        public void ShowSonar()
        {
            var gm = GameManager.I;
            sonarShow = gm ? gm.settings.sonarDuration : 3f;
            ringT = 0f;
        }

        void UpdateSonar()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            if (sonarArrow)
            {
                bool on = sonarShow > 0f && gm.State == GameState.Playing;
                sonarShow -= Time.deltaTime;
                if (sonarArrow.gameObject.activeSelf != on) sonarArrow.gameObject.SetActive(on);
                if (on)
                {
                    gm.CurrentObjective(out Vector3 t);
                    Vector3 d = t - transform.position;
                    d.y = 0f;
                    if (d.sqrMagnitude > 0.01f)
                    {
                        float a = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                        float pulse = 1.9f + Mathf.Sin(Time.time * 6f) * 0.15f;
                        sonarArrow.position = transform.position + d.normalized * pulse + Vector3.down * 0.55f;
                        sonarArrow.rotation = Quaternion.Euler(90f, a, 0f);
                    }
                    if (arrowMat)
                    {
                        var c = arrowMat.color;
                        c.a = Mathf.Clamp01(sonarShow);
                        arrowMat.color = c;
                    }
                }
            }
            if (sonarRing)
            {
                if (ringT < 1f)
                {
                    ringT += Time.deltaTime / 1.1f;
                    if (!sonarRing.gameObject.activeSelf) sonarRing.gameObject.SetActive(true);
                    float s = Mathf.Lerp(1f, 18f, ringT);
                    sonarRing.localScale = new Vector3(s, s, s);
                    sonarRing.position = transform.position + Vector3.down * 0.6f;
                    sonarRing.rotation = Quaternion.Euler(90f, 0f, 0f);
                    if (ringMat)
                    {
                        var c = ringMat.color;
                        c.a = 1f - ringT;
                        ringMat.color = c;
                    }
                }
                else if (sonarRing.gameObject.activeSelf) sonarRing.gameObject.SetActive(false);
            }
        }
    }
}
