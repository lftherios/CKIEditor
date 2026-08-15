using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CKIEditor.UI.TrackValues
{
    /// <summary>
    /// Small grip in a track-value slot's corner. Drag it onto another slot to
    /// move or swap the value - a floating ghost label follows the pointer.
    /// Built and attached at runtime by TrackValueItemView; reports drops
    /// through the owner view's DroppedOnSlotSignal.
    /// </summary>
    public class SlotDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private static readonly List<RaycastResult> Hits = new List<RaycastResult>();

        private TrackValueItemView _owner;
        private Canvas _canvas;
        private RectTransform _ghost;
        private bool _dragging;

        public void Init(TrackValueItemView owner)
        {
            _owner = owner;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragging = false;
            if (_owner == null || _owner.SlotIsEmpty)
                return;

            _canvas = GetComponentInParent<Canvas>();
            if (_canvas == null)
                return;

            _dragging = true;
            BuildGhost();
            MoveGhost(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_dragging)
                MoveGhost(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging)
                return;

            _dragging = false;
            if (_ghost != null)
                Destroy(_ghost.gameObject);

            var target = FindSlotUnderPointer(eventData);
            if (target != null && target != _owner)
                _owner.DroppedOnSlotSignal.Dispatch(_owner.SlotIndex, target.SlotIndex);
        }

        private TrackValueItemView FindSlotUnderPointer(PointerEventData eventData)
        {
            Hits.Clear();
            EventSystem.current.RaycastAll(eventData, Hits);

            foreach (var hit in Hits)
            {
                var view = hit.gameObject.GetComponentInParent<TrackValueItemView>();
                if (view != null && view != _owner)
                    return view;
            }

            return null;
        }

        private void BuildGhost()
        {
            var go = new GameObject("SlotDragGhost", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            _ghost = (RectTransform)go.transform;
            _ghost.SetParent(_canvas.transform, false);
            _ghost.SetAsLastSibling();
            _ghost.sizeDelta = new Vector2(110, 28);

            go.GetComponent<Image>().color = CkiTheme.Accent;
            //never intercept the raycast we do on drop
            go.GetComponent<CanvasGroup>().blocksRaycasts = false;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            var textRect = (RectTransform)textGo.transform;
            textRect.SetParent(_ghost, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;

            var text = textGo.GetComponent<TextMeshProUGUI>();
            text.text = _owner.SlotDescription;
            text.fontSize = 13;
            text.color = CkiTheme.AccentInk;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
        }

        private void MoveGhost(PointerEventData eventData)
        {
            if (_ghost == null)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)_canvas.transform, eventData.position,
                _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
                out var localPoint);
            _ghost.anchoredPosition = localPoint + new Vector2(12, -12);
        }
    }
}
