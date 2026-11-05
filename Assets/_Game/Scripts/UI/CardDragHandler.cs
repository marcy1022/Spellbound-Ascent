using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpellboundAscent.UI
{
    public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public int HandIndex { get; set; }
        public Action<CardDragHandler> OnDragStarted;
        public Action<CardDragHandler, PointerEventData> OnDragging;
        public Action<CardDragHandler, PointerEventData> OnDragEnded;

        RectTransform _rt;
        Vector2 _originalAnchorMin;
        Vector2 _originalAnchorMax;
        Vector2 _originalSize;
        CanvasGroup _canvasGroup;
        Image _highlight;
        bool _isDragging;

        void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
            _originalAnchorMin = _rt.anchorMin;
            _originalAnchorMax = _rt.anchorMax;
            _originalSize = new Vector2(_rt.rect.width, _rt.rect.height);

            _rt.SetAsLastSibling();
            _canvasGroup.blocksRaycasts = false;

            transform.localScale = Vector3.one * 1.05f;
            ShowHighlight(true);

            OnDragStarted?.Invoke(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;

            _rt.anchorMin = new Vector2(0.5f, 0.5f);
            _rt.anchorMax = new Vector2(0.5f, 0.5f);
            _rt.sizeDelta = _originalSize;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rt.parent as RectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
            _rt.anchoredPosition = localPoint;

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
            _rt.anchorMin = _originalAnchorMin;
            _rt.anchorMax = _originalAnchorMax;
            _rt.anchoredPosition = Vector2.zero;
            _rt.sizeDelta = Vector2.zero;
            transform.localScale = Vector3.one;
            ShowHighlight(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isDragging)
                transform.localScale = Vector3.one * 1.05f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isDragging)
                transform.localScale = Vector3.one;
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
