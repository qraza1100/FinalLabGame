using UnityEngine;

namespace DrownedSanctum
{
    /// <summary>A weakened wall that crumbles when the escape starts, opening a shortcut.</summary>
    public class CrackedWall : MonoBehaviour
    {
        public Renderer[] renderers;
        public Collider wallCollider;
        public Material debrisMaterial;
        public float cellSize = 3f;
        public float wallHeight = 2.6f;

        public bool Crumbled { get; private set; }

        public void Crumble()
        {
            if (Crumbled) return;
            Crumbled = true;
            if (wallCollider) wallCollider.enabled = false;
            foreach (var r in renderers) if (r) r.enabled = false;

            Collider playerCol = null;
            if (GameManager.I && GameManager.I.player) playerCol = GameManager.I.player.GetComponent<Collider>();

            for (int i = 0; i < 12; i++)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.name = "Debris";
                c.transform.position = transform.position + new Vector3(Random.Range(-1f, 1f) * cellSize * 0.4f,
                                                                         Random.Range(0.3f, wallHeight),
                                                                         Random.Range(-1f, 1f) * cellSize * 0.4f);
                c.transform.rotation = Random.rotation;
                c.transform.localScale = Vector3.one * Random.Range(0.25f, 0.6f);
                if (debrisMaterial) c.GetComponent<Renderer>().sharedMaterial = debrisMaterial;
                var col = c.GetComponent<Collider>();
                if (playerCol) Physics.IgnoreCollision(col, playerCol);
                var rb = c.AddComponent<Rigidbody>();
                rb.mass = 0.5f;
                rb.linearDamping = 1.5f;
                rb.angularDamping = 1f;
                rb.linearVelocity = new Vector3(Random.Range(-2f, 2f), Random.Range(0.5f, 2.5f), Random.Range(-2f, 2f));
                rb.angularVelocity = Random.insideUnitSphere * 4f;
                Object.Destroy(c, Random.Range(5f, 8f));
            }
            FxUtil.Burst(transform.position + Vector3.up * 1.2f, new Color(0.7f, 0.75f, 0.7f, 0.8f), 40, 3f, 0.6f, 1.6f);
        }
    }
}
