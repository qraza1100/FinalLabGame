using UnityEngine;

namespace DrownedSanctum
{
    /// <summary>Hover over an air vent to refill oxygen.</summary>
    public class AirVent : MonoBehaviour
    {
        void OnTriggerStay(Collider other)
        {
            var gm = GameManager.I;
            if (gm == null || gm.State != GameState.Playing) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;
            gm.RefillOxygen(Time.fixedDeltaTime);
        }
    }
}
