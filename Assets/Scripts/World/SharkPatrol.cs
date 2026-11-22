using UnityEngine;

namespace DrownedSanctum
{
    /// <summary>
    /// The guardian shark swims back and forth along a straight corridor and charges
    /// along it when the explorer enters its lane.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class SharkPatrol : MonoBehaviour
    {
        public Vector3 pointA;
        public Vector3 pointB;
        public Animator animator;
        public float laneHalfWidth = 2.2f;
        public float damageMultiplier = 1.3f;

        float pos;
        int dir = 1;
        bool chasing;

        public bool Chasing => chasing;

        void Start()
        {
            var rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            transform.position = pointA;
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            bool active = gm.State == GameState.Playing;
            if (animator) animator.speed = active ? (chasing ? 1.9f : 1f) : 0.25f;
            if (!active || gm.player == null) return;

            var st = gm.settings;
            Vector3 ab = pointB - pointA;
            ab.y = 0f;
            float len = ab.magnitude;
            if (len < 0.01f) return;
            Vector3 lane = ab / len;

            Vector3 pp = gm.player.transform.position;
            Vector3 rel = pp - pointA;
            rel.y = 0f;
            float proj = Vector3.Dot(rel, lane);
            Vector3 closest = pointA + lane * Mathf.Clamp(proj, 0f, len);
            closest.y = pp.y;
            float lateral = Vector3.Distance(pp, closest);

            chasing = lateral < laneHalfWidth && proj > -1.5f && proj < len + 1.5f && Mathf.Abs(proj - pos) < st.sharkDetectRange;

            float target, speed;
            if (chasing)
            {
                target = Mathf.Clamp(proj, 0f, len);
                speed = st.sharkChaseSpeed;
            }
            else
            {
                target = dir > 0 ? len : 0f;
                speed = st.sharkPatrolSpeed;
                if (Mathf.Abs(pos - target) < 0.05f) dir = -dir;
            }

            float next = Mathf.MoveTowards(pos, target, speed * Time.deltaTime);
            float moved = next - pos;
            pos = next;
            Vector3 p = pointA + lane * pos;
            p.y = pointA.y + Mathf.Sin(Time.time * 1.7f) * 0.12f;
            transform.position = p;
            if (Mathf.Abs(moved) > 0.0005f)
            {
                Quaternion face = Quaternion.LookRotation(lane * Mathf.Sign(moved), Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, face, 1f - Mathf.Exp(-7f * Time.deltaTime));
            }
        }

        void OnTriggerEnter(Collider other) { Bite(other); }
        void OnTriggerStay(Collider other) { Bite(other); }

        void Bite(Collider other)
        {
            var gm = GameManager.I;
            if (gm == null || gm.State != GameState.Playing) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;
            gm.DamagePlayer(transform.position, damageMultiplier);
        }
    }
}
