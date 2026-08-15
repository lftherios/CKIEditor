using Framewerk.UI.List;
using strange.extensions.signal.impl;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CKIEditor.UI.TrackValues
{
    public class TrackValueItemView : ListItemView
    {
        public TMP_Dropdown TrackValueTypeDropdown;
        public TMP_Dropdown TrackControlTypeDropdown;
        public TMP_Dropdown CcSelectionDropdown;

        //set by the mediator on SetData; read by the drag handle on drop
        [HideInInspector] public int SlotIndex;
        [HideInInspector] public bool SlotIsEmpty = true;
        [HideInInspector] public string SlotDescription = "";

        //(fromSlot, toSlot) - dispatched when this slot's handle is dropped on another slot
        public Signal<int, int> DroppedOnSlotSignal = new Signal<int, int>();

        private const float HANDLE_SIZE = 16f;

        protected override void Awake()
        {
            BuildDragHandle();
            base.Awake();
        }

        private void BuildDragHandle()
        {
            var go = new GameObject("DragHandle", typeof(RectTransform), typeof(Image), typeof(SlotDragHandle));
            var rect = (RectTransform)go.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-2, -2);
            rect.sizeDelta = new Vector2(HANDLE_SIZE, HANDLE_SIZE);

            go.GetComponent<Image>().color = CkiTheme.Soft(CkiTheme.Accent, 0.55f);
            go.GetComponent<SlotDragHandle>().Init(this);
        }
    }
}
