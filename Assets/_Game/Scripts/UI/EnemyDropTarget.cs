using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SpellboundAscent.UI
{
    public class EnemyDropTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public int EnemyIndex { get; set; }
        public Action<int> OnEnemyHovered;
        public Action<int> OnEnemyUnhovered;

        public void OnPointerEnter(PointerEventData eventData)
        {
            OnEnemyHovered?.Invoke(EnemyIndex);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnEnemyUnhovered?.Invoke(EnemyIndex);
        }
    }
}
