using CKIEditor.Controller;
using CKIEditor.Metadata;
using CKIEditor.Model;
using CKIEditor.Model.Defs;
using CKIEditor.UI.EditSection.General;

namespace CKIEditor.UI.EditSection.GeneralSettings
{
    public class GeneralSettingsMediator : EditInstrumentSectionMediator
    {
        [Inject] public IOptionsModel OptionsModel { get; set; }
        [Inject] public IMetadataModel MetadataModel { get; set; }

        [Inject] public InstrumentGeneralSettingsChangedSignal InstrumentGeneralSettingsChangedSignal { get; set; }

        [Inject] public GeneralSettingsView View { get; set; }

        public override void OnRegister()
        {
            View.MidiPortDropdown.options = OptionsModel.GetMidiPortOptions();
            View.MidiChannelDropdown.options = OptionsModel.GetMidiChannelOptions();
            View.DefaultNoteDropdown.options = OptionsModel.GetDefaultNoteOptions();
            View.DefaultNoteOctaveDropdown.options = OptionsModel.GetOctaveOptions();
            View.DefaultPatternDropdown.options = OptionsModel.GetPatternOptions();

            AddInputListener(View.InstrumentNameInput, InputHandler);

            AddDropdownListener(View.MidiPortDropdown, DropDownHandler);
            AddDropdownListener(View.MidiChannelDropdown, DropDownHandler);
            AddDropdownListener(View.DefaultNoteDropdown, DropDownHandler);
            AddDropdownListener(View.DefaultNoteOctaveDropdown, DropDownHandler);
            AddDropdownListener(View.DefaultPatternDropdown, DropDownHandler);

            AddToggleListener(View.MultiToggle, ToggleHandler);
            AddToggleListener(View.NoFtsToggle, ToggleHandler);
            AddToggleListener(View.NoTransposeToggle, ToggleHandler);
            AddToggleListener(View.PolySpreadToggle, ToggleHandler);
            AddToggleListener(View.NoBankMToggle, ToggleHandler);
            AddToggleListener(View.NoBankLToggle, ToggleHandler);
            AddToggleListener(View.ShowNoteNumsToggle, ToggleHandler);
            AddToggleListener(View.NoThruToggle, ToggleHandler);
            AddToggleListener(View.PresendPgmToggle, ToggleHandler);

            base.OnRegister();
        }

        protected override void ShowInstrumentData(InstrumentDef instrumentDef)
        {
            var inst = InstrumentsModel.GetEditedInstrument();
            if(inst == null)
                return;

            View.InstrumentNameInput.text = inst.Name;
            View.MidiPortDropdown.value = inst.MidiPort - 1;
            View.MidiChannelDropdown.value = inst.MidiChannel - 1;

            //note dropdown option 0 is "off", note options are shifted by 1
            if (inst.DefaultNote.HasValue)
            {
                View.DefaultNoteDropdown.value = inst.DefaultNote.Value.NoteIndex + 1;
                View.DefaultNoteOctaveDropdown.value = inst.DefaultNote.Value.OctaveIndex;
            }
            else
            {
                View.DefaultNoteDropdown.value = 0;
                View.DefaultNoteOctaveDropdown.value = 0;
            }

            View.DefaultPatternDropdown.value = (int)inst.DefaultPattern;
            View.MultiToggle.isOn = inst.Multi;
            View.PolySpreadToggle.isOn = inst.PolySpread >= CkiConsts.POLY_SPREAD_MIN;
            View.NoTransposeToggle.isOn = inst.NoXpose;
            View.NoFtsToggle.isOn = inst.NoFts;
            View.NoBankMToggle.isOn = inst.NoBankM;
            View.NoBankLToggle.isOn = inst.NoBankL;
            View.ShowNoteNumsToggle.isOn = inst.ShowNoteNums;
            View.NoThruToggle.isOn = inst.NoThru;
            View.PresendPgmToggle.isOn = inst.PresendPgm;

            if (inst.Name == InstrumentDef.DEFAULT_NAME)
                View.InstrumentNameInput.Select();
        }

        private void UpdateInstrument()
        {
            var inst = InstrumentsModel.GetEditedInstrument();
            if(inst == null)
                return;

            //keep sidecar documentation attached across renames
            var oldName = inst.Name;
            inst.Name = View.InstrumentNameInput.text;
            if (oldName != inst.Name)
                MetadataModel.Rename(oldName, inst.Name);

            inst.MidiPort = View.MidiPortDropdown.value + 1;
            inst.MidiChannel = View.MidiChannelDropdown.value + 1;

            //note dropdown option 0 is "off", note options are shifted by 1
            if (View.DefaultNoteDropdown.value == 0)
                inst.DefaultNote = null;
            else
                inst.DefaultNote = new Note(View.DefaultNoteDropdown.value - 1, View.DefaultNoteOctaveDropdown.value);

            inst.DefaultPattern =  (PatternType)View.DefaultPatternDropdown.value;

            inst.Multi = View.MultiToggle.isOn;

            //keep the spread channel count from an imported file when the toggle stays on
            if (!View.PolySpreadToggle.isOn)
                inst.PolySpread = CkiConsts.POLY_SPREAD_OFF;
            else if (inst.PolySpread < CkiConsts.POLY_SPREAD_MIN)
                inst.PolySpread = CkiConsts.POLY_SPREAD_MIN;

            inst.NoXpose = View.NoTransposeToggle.isOn;
            inst.NoFts  = View.NoFtsToggle.isOn;
            inst.NoBankM = View.NoBankMToggle.isOn;
            inst.NoBankL = View.NoBankLToggle.isOn;
            inst.ShowNoteNums = View.ShowNoteNumsToggle.isOn;
            inst.NoThru = View.NoThruToggle.isOn;
            inst.PresendPgm = View.PresendPgmToggle.isOn;

            InstrumentGeneralSettingsChangedSignal.Dispatch();
        }

        private void InputHandler(string value)
        {
            UpdateInstrument();
        }

        private void DropDownHandler(int value)
        {
            UpdateInstrument();
        }

        private void ToggleHandler(bool value)
        {
            UpdateInstrument();
        }
    }
}
