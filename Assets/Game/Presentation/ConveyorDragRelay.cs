using UnityEngine;
using UnityEngine.EventSystems;

namespace ColorFlow.Game.Presentation
{
    /// <summary>
    /// Relais de glisser-déposer sur un emplacement du tapis roulant.
    /// Le <see cref="ConveyorView"/> garde la logique visuelle (ghost, retour,
    /// surbrillance) et le contrôleur de scène décide de la validité du drop.
    /// </summary>
    public sealed class ConveyorDragRelay : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public ConveyorView View;
        public int Index;

        public void OnBeginDrag(PointerEventData eventData)
        {
            View?.BeginDrag(Index, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            View?.DragTo(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            View?.EndDrag(eventData);
        }
    }
}
