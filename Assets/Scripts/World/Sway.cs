using UnityEngine;

namespace DrownedSanctum
{
    /// <summary>Slow underwater sway for seaweed, plus an optional pulse in scale (urchins).</summary>
    public class Sway : MonoBehaviour
    {
        public float angle = 8f;
        public float speed = 0.9f;
        public float pulseAmount = 0f;
        public float pulseSpeed = 2f;

        Quaternion baseRot;
        Vector3 baseScale;
        float phase;

        void Start()
        {
            baseRot = transform.localRotation;
            baseScale = transform.localScale;
            phase = Random.value * 6.28f;
        }

        void Update()
        {
            float t = Time.time * speed + phase;
            if (angle != 0f)
                transform.localRotation = baseRot * Quaternion.Euler(Mathf.Sin(t) * angle, 0f, Mathf.Cos(t * 0.8f) * angle * 0.6f);
            if (pulseAmount != 0f)
                transform.localScale = baseScale * (1f + Mathf.Sin(Time.time * pulseSpeed + phase) * pulseAmount);
        }
    }
}
