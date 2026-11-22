using UnityEngine;

namespace DrownedSanctum
{
    public enum PickupType { Pearl, Sigil, Heart }

    /// <summary>A pearl, a sigil stone or the Heart of Eternity. Swim into it to collect it.</summary>
    public class Pickup : MonoBehaviour
    {
        public PickupType type = PickupType.Pearl;
        [Tooltip("0 = Moon, 1 = Sun, 2 = Star (sigils only)")]
        [Range(0, 2)] public int sigilIndex;
        public Color fxColor = new Color(0.9f, 1f, 1f);
        [Tooltip("Children that stay visible after collecting (e.g. the Heart's altar).")]
        public GameObject[] keepOnCollect = new GameObject[0];

        public bool Taken { get; private set; }

        void OnTriggerEnter(Collider other)
        {
            if (Taken) return;
            var gm = GameManager.I;
            if (gm == null || gm.State != GameState.Playing) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;
            Taken = true;
            bool big = type != PickupType.Pearl;
            FxUtil.Burst(transform.position + Vector3.up * 0.8f, fxColor, big ? 50 : 16, big ? 5f : 2.5f, big ? 0.4f : 0.22f, big ? 1.2f : 0.7f);
            gm.Collect(this);
            if (keepOnCollect != null && keepOnCollect.Length > 0)
            {
                foreach (var c in GetComponents<Collider>()) c.enabled = false;
                foreach (Transform child in transform)
                    if (System.Array.IndexOf(keepOnCollect, child.gameObject) < 0) child.gameObject.SetActive(false);
            }
            else gameObject.SetActive(false);
        }
    }
}
