using UnityEngine;

namespace DrownedSanctum
{
    /// <summary>Spiky urchin (or any harmful thing): touching it costs oxygen and points.</summary>
    public class Hazard : MonoBehaviour
    {
        public float damageMultiplier = 1f;

        void OnTriggerEnter(Collider other) { Hit(other); }
        void OnTriggerStay(Collider other) { Hit(other); }

        void Hit(Collider other)
        {
            var gm = GameManager.I;
            if (gm == null || gm.State != GameState.Playing) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;
            gm.DamagePlayer(transform.position, damageMultiplier);
        }
    }
}
