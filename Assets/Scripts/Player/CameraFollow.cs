using UnityEngine;

namespace DrownedSanctum
{
    /// <summary>Angled top-down camera that trails the sub, looks slightly ahead and can shake.</summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 15f, -8f);
        public float smooth = 5f;
        public float lookAhead = 1.5f;

        float shakeAmp, shakeTime, shakeDur;
        PlayerController pc;

        void Start()
        {
            if (target) pc = target.GetComponent<PlayerController>();
            Snap();
        }

        public void Snap()
        {
            if (!target) return;
            transform.position = target.position + offset;
            transform.rotation = Quaternion.LookRotation(-offset.normalized, Vector3.up);
        }

        void LateUpdate()
        {
            if (!target) return;
            Vector3 ahead = Vector3.zero;
            if (pc)
            {
                Vector3 v = pc.Velocity;
                v.y = 0f;
                ahead = Vector3.ClampMagnitude(v * 0.3f, lookAhead);
            }
            Vector3 desired = target.position + ahead + offset;
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-smooth * Time.deltaTime));
            transform.rotation = Quaternion.LookRotation(-offset.normalized, Vector3.up);
            if (shakeTime > 0f)
            {
                shakeTime -= Time.deltaTime;
                float k = shakeAmp * Mathf.Clamp01(shakeTime / Mathf.Max(0.01f, shakeDur));
                transform.position += Random.insideUnitSphere * k;
            }
        }

        public void Shake(float amplitude, float duration)
        {
            if (shakeTime <= 0f || amplitude >= shakeAmp * Mathf.Clamp01(shakeTime / Mathf.Max(0.01f, shakeDur)))
            {
                shakeAmp = amplitude;
                shakeDur = duration;
                shakeTime = duration;
            }
        }
    }
}
