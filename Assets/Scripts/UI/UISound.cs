using UnityEngine;
using UnityEngine.EventSystems;

namespace DrownedSanctum
{
    /// <summary>Hover and click sounds for UI buttons.</summary>
    public class UISound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        public void OnPointerEnter(PointerEventData e) { SFX.Play(s => s.hover, 0.35f); }
        public void OnPointerClick(PointerEventData e) { SFX.Play(s => s.click, 0.7f); }
    }
}
