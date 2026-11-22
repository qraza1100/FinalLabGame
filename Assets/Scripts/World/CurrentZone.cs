using UnityEngine;

namespace DrownedSanctum
{
    /// <summary>A strong water current that pushes the sub along this object's forward (blue) axis.</summary>
    public class CurrentZone : MonoBehaviour
    {
        [Tooltip("Flow direction in local space. Rotate the object to change where the current pushes.")]
        public Vector3 direction = Vector3.forward;
        public Renderer arrowRenderer;
        public float scrollSpeed = 1.4f;

        Material mat;

        void Start()
        {
            if (arrowRenderer) mat = arrowRenderer.material;
        }

        void Update()
        {
            if (mat) mat.mainTextureOffset = new Vector2(0f, -Time.time * scrollSpeed);
        }

        void OnTriggerStay(Collider other)
        {
            var gm = GameManager.I;
            if (gm == null || gm.State != GameState.Playing) return;
            var p = other.GetComponentInParent<PlayerController>();
            if (p == null) return;
            Vector3 flow = transform.TransformDirection(direction);
            flow.y = 0f;
            p.AddCurrent(flow.normalized * gm.settings.currentForce);
        }
    }
}
