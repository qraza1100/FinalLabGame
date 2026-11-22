using UnityEngine;

namespace DrownedSanctum
{
    /// <summary>Just beyond the entrance gate. Reaching it while holding the Heart wins the game.</summary>
    public class ExitZone : MonoBehaviour
    {
        void OnTriggerEnter(Collider other) { Check(other); }
        void OnTriggerStay(Collider other) { Check(other); }

        void Check(Collider other)
        {
            var gm = GameManager.I;
            if (gm == null || gm.State != GameState.Playing || !gm.HasHeart) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;
            gm.Win();
        }
    }
}
