using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpellboundAscent.UI
{
    public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public int HandIndex { get; set; }
        public Vector2 RestPosition { get; set; }
        public Quaternion RestRotation { get; set; }
        public int RestSiblingIndex { get; set; }
        public RectTransform Visual { get; set; }

        public Action<CardDragHandler> OnDragStarted;
        public Action<CardDragHandler, PointerEventData> OnDragging;
        public Action<CardDragHandler, PointerEventData> OnDragEnded;

        RectTransform _rt;
        CanvasGroup _canvasGroup;
        Image _highlight;
        bool _isDragging;
        Vector2 _dragOffset;

        const float HoverRaise = 80f;
        const float HoverScale = 1.3f;

        void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isDragging) return;
            _rt.SetAsLastSibling();
            if (Visual != null)
            {
                Visual.anchoredPosition = new Vector2(0, HoverRaise);
                Visual.localScale = Vector3.one * HoverScale;
                Visual.localRotation = Quaternion.Inverse(_rt.localRotation);
                var vc = Visual.GetComponent<Canvas>();
                if (vc == null) vc = Visual.gameObject.AddComponent<Canvas>();
                vc.overrideSorting = true;
                vc.sortingOrder = 25;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isDragging) return;
            _rt.SetSiblingIndex(RestSiblingIndex);
            ResetVisual();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
            ResetVisual();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rt.parent as RectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
            _dragOffset = _rt.anchoredPosition - localPoint;

            _canvasGroup.blocksRaycasts = false;
            _rt.SetAsLastSibling();
            _rt.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one * 1.05f;
            ShowHighlight(true);
            OnDragStarted?.Invoke(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rt.parent as RectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
            _rt.anchoredPosition = localPoint + _dragOffset;

            OnDragging?.Invoke(this, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
            _canvasGroup.blocksRaycasts = true;
            transform.localScale = Vector3.one;
            ShowHighlight(false);
            OnDragEnded?.Invoke(this, eventData);
        }

        public void ReturnToHand()
        {
            _isDragging = false;
            _canvasGroup.blocksRaycasts = true;
            _rt.SetSiblingIndex(RestSiblingIndex);
            _rt.localRotation = RestRotation;
            _rt.anchoredPosition = RestPosition;
            transform.localScale = Vector3.one;
            ResetVisual();
            ShowHighlight(false);
        }

        void ResetVisual()
        {
            if (Visual == null) return;
            Visual.anchoredPosition = Vector2.zero;
            Visual.localScale = Vector3.one;
            Visual.localRotation = Quaternion.identity;
            var vc = Visual.GetComponent<Canvas>();
            if (vc != null) vc.overrideSorting = false;
        }

        void ShowHighlight(bool show)
        {
            if (_highlight == null)
            {
                var border = new GameObject("Highlight", typeof(RectTransform));
                var brt = border.GetComponent<RectTransform>();
                brt.SetParent(_rt, false);
                brt.anchorMin = Vector2.zero;
                brt.anchorMax = Vector2.one;
                brt.offsetMin = new Vector2(-3, -3);
                brt.offsetMax = new Vector2(3, 3);
                _highlight = border.AddComponent<Image>();
                _highlight.color = new Color(1f, 0.85f, 0.2f, 0.9f);
                _highlight.raycastTarget = false;
                brt.SetAsFirstSibling();
            }
            _highlight.gameObject.SetActive(show);
        }
    }
}
