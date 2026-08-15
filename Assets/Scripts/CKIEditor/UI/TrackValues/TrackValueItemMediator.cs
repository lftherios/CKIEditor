using System;
using CKIEditor.Controller;
using CKIEditor.Model;
using CKIEditor.Model.Defs;
using Framewerk.UI.List;

namespace CKIEditor.UI.TrackValues
{
    public class TrackValueItemMediator : ListItemMediator<TrackValueItemView, TrackValueDataProvider>
    {
        [Inject] public IOptionsModel OptionsModel { get; set; }
        [Inject] public IInstrumentsModel InstrumentsModel { get; set; }
        [Inject] public InstrumentCcDefsChangedSignal InstrumentCcDefsChangedSignal { get; set; }
        [Inject] public MoveTrackValueSignal MoveTrackValueSignal { get; set; }

        public override void OnRegister()
        {
            base.OnRegister();

            AddListeners();

            InstrumentCcDefsChangedSignal.AddListener(EditedInstrumentChangedHandler);
            View.DroppedOnSlotSignal.AddListener(DroppedOnSlotHandler);
        }

        public override void OnRemove()
        {
            InstrumentCcDefsChangedSignal.RemoveListener(EditedInstrumentChangedHandler);
            View.DroppedOnSlotSignal.RemoveListener(DroppedOnSlotHandler);
            base.OnRemove();
        }

        private void DroppedOnSlotHandler(int fromSlot, int toSlot)
        {
            MoveTrackValueSignal.Dispatch(new SlotMove(fromSlot, toSlot));
        }
        
        private void AddListeners()
        {
            AddDropdownListener(View.TrackValueTypeDropdown, TrackValueTypeDropdownChanged);
            AddDropdownListener(View.TrackControlTypeDropdown, TrackControlTypeDropdownChanged);
            AddDropdownListener(View.CcSelectionDropdown, CcSelectionDropdownChanged);   
        }

        private void EditedInstrumentChangedHandler()
        {
            View.CcSelectionDropdown.options = OptionsModel.GetCcOptions();  
            //TODO: select previously selected cc (in case we renamed / moved it)
        }

        private void TrackValueTypeDropdownChanged(int value)
        {
            RemoveListeners();

            DataProvider.TrackValue.Type = (TrackValueType) value;

            //when switching to a CC slot, default to the instrument's first CC def (if it has any)
            if (DataProvider.TrackValue.Type == TrackValueType.MidiCC)
            {
                var ccId = OptionsModel.GetCCnumberByOptionId(0);
                if (ccId >= 0)
                {
                    var instrument = InstrumentsModel.GetEditedInstrument();
                    DataProvider.TrackValue.MidiCC = ccId;
                    DataProvider.TrackValue.Label = instrument.CcDefs[ccId].Label;
                }
            }

            UpdateView();

            AddListeners();
        }

        private void TrackControlTypeDropdownChanged(int value)
        {
            RemoveListeners();
            //dropdown options are generated straight from the enum, so index == value
            DataProvider.TrackValue.TrackControl = (TrackControlType) value;
            UpdateView();
            AddListeners();
        }
        
        private void CcSelectionDropdownChanged(int value)
        {
            var ccId = OptionsModel.GetCCnumberByOptionId(value);
            if (ccId < 0)
                return;

            DataProvider.TrackValue.MidiCC = ccId;
            DataProvider.TrackValue.Label = InstrumentsModel.GetEditedInstrument().CcDefs[ccId].Label;
            RefreshDragContext();
        }

        public override void SetData(TrackValueDataProvider dataProvider, int index)
        {
            base.SetData(dataProvider, index);

            RemoveListeners();

            //drag-handle context: which slot this is and what's in it
            View.SlotIndex = dataProvider.TrackValue.SlotIndex;
            View.SlotIsEmpty = dataProvider.TrackValue.Type == TrackValueType.Empty;
            View.SlotDescription = DescribeSlot(dataProvider.TrackValue);

            View.TrackValueTypeDropdown.options = OptionsModel.GetTrackValueOptions();
            View.TrackControlTypeDropdown.options = OptionsModel.GetTrackControlOptions();
            View.CcSelectionDropdown.options = OptionsModel.GetCcOptions();

            View.TrackValueTypeDropdown.value = (int) dataProvider.TrackValue.Type;
            View.TrackControlTypeDropdown.value = (int) dataProvider.TrackValue.TrackControl;
            
            UpdateView();

            AddListeners();
        }

        private static string DescribeSlot(TrackValueDef trackValue)
        {
            switch (trackValue.Type)
            {
                case TrackValueType.MidiCC:
                    return string.IsNullOrEmpty(trackValue.Label)
                        ? $"cc# {trackValue.MidiCC}"
                        : trackValue.Label;
                case TrackValueType.TrackControl:
                    return trackValue.TrackControl.ToDefString();
                default:
                    return "";
            }
        }

        //every code path that mutates the track value must keep the drag handle's
        //picture of the slot current, not just SetData
        private void RefreshDragContext()
        {
            View.SlotIsEmpty = DataProvider.TrackValue.Type == TrackValueType.Empty;
            View.SlotDescription = DescribeSlot(DataProvider.TrackValue);
        }

        private void UpdateView()
        {
            RefreshDragContext();

            View.CcSelectionDropdown.gameObject.SetActive(DataProvider.TrackValue.Type == TrackValueType.MidiCC);
            View.TrackControlTypeDropdown.gameObject.SetActive(DataProvider.TrackValue.Type == TrackValueType.TrackControl);

            switch (DataProvider.TrackValue.Type)
            {
                case TrackValueType.Empty:
                    break;
                case TrackValueType.MidiCC:
                    //dropdown wants the option index, not the raw CC number
                    var optionId = OptionsModel.GetOptionIdByCcNumber(DataProvider.TrackValue.MidiCC);
                    if (optionId >= 0)
                        View.CcSelectionDropdown.value = optionId;
                    break;
                case TrackValueType.TrackControl:
                    View.TrackControlTypeDropdown.value = (int)DataProvider.TrackValue.TrackControl;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }    
        }
    }
}