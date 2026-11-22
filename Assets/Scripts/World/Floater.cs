using UnityEngine;

namespace DrownedSanctum
{
    /// <summary>Gentle spin and bob for pickups and floating props (pearls, sigils, the Heart).</summary>
    public class Floater : MonoBehaviour
    {
        public float spinSpeed = 45f;
        public float bobAmplitude = 0.15f;
        public float bobSpeed = 1.6f;

        Vector3 basePos;
        float phase;

        void Start()
        {
            basePos = transform.localPosition;
            phase = Random.value * 10f;
        }

        void Update()
        {
            transform.localPosition = basePos + Vector3.up * Mathf.Sin((Time.time + phase) * bobSpeed) * bobAmplitude;
            if (spinSpeed != 0f) transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }
    }
}
