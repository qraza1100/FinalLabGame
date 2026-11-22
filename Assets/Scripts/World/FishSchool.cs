using UnityEngine;

namespace DrownedSanctum
{
    /// <summary>Decorative fish circling a point (they face +Z of their own transform).</summary>
    public class FishSchool : MonoBehaviour
    {
        public Transform[] fish;
        public float radius = 3f;
        public float speed = 0.5f;
        public float height = 1.8f;

        float[] phase;

        void Start()
        {
            phase = new float[fish.Length];
            for (int i = 0; i < fish.Length; i++)
                phase[i] = i * (Mathf.PI * 2f / Mathf.Max(1, fish.Length)) + Random.Range(-0.3f, 0.3f);
        }

        void Update()
        {
            for (int i = 0; i < fish.Length; i++)
            {
                if (!fish[i]) continue;
                float a = phase[i] + Time.time * speed;
                float r = radius * (0.8f + 0.2f * Mathf.Sin(a * 2f + i));
                fish[i].localPosition = new Vector3(Mathf.Cos(a) * r, height + Mathf.Sin(a * 1.7f + i) * 0.3f, Mathf.Sin(a) * r);
                Vector3 tangent = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
                fish[i].localRotation = Quaternion.LookRotation(tangent, Vector3.up);
            }
        }
    }
}
