using CKIEditor.Controller;
using CKIEditor.Metadata;
using CKIEditor.Model;
using CKIEditor.Model.Defs;
using CKIEditor.Validation;
using Framewerk.UI;

namespace CKIEditor.UI.EditSection.CcEditor
{
    public class CreateCcDefMediator : ExtendedMediator
    {
        [Inject] public IInstrumentsModel InstrumentsModel { get; set; }
        [Inject] public IMetadataModel MetadataModel { get; set; }

        [Inject] public AddCcDefSignal AddCcDefSignal { get; set; }
        [Inject] public ImportCcChartSignal ImportCcChartSignal { get; set; }

        [Inject] public CreateCcDefView View { get; set; }

        //when true, the label was auto-filled from the full name and may be overwritten
        private bool _labelAutoFilled = true;

        public override void OnRegister()
        {
            base.OnRegister();

            //the hardware keeps six characters - say so while typing, not after
            View.NameInput.characterLimit = CkiConsts.CC_NAME_CHARACTER_LIMIT;

            AddButtonListener(View.SaveButton, SaveButtonListener);
            AddButtonListener(View.PasteChartButton, () => ImportCcChartSignal.Dispatch());
            AddInputListener(View.FullNameInput, FullNameChangedHandler);
            AddInputListener(View.NameInput, LabelEditedHandler);
        }

        //typing a full name suggests the six-character label until the user edits it
        private void FullNameChangedHandler(string fullName)
        {
            if (!_labelAutoFilled && View.NameInput.text.Length > 0)
                return;

            View.NameInput.SetTextWithoutNotify(LabelAbbreviator.Suggest(fullName));
            _labelAutoFilled = true;
        }

        private void LabelEditedHandler(string label)
        {
            _labelAutoFilled = label.Length == 0;
        }

        private void SaveButtonListener()
        {
            if (!int.TryParse(View.CcInput.text, out var ccNum)
                || ccNum < CcDef.MIN_CC_VALUE || ccNum > CcDef.MAX_CC_VALUE)
            {
                Toast.Show("CC number must be 0–127.");
                return;
            }

            var ccDef = new CcDef(ccNum);
            ccDef.SetLabel(View.NameInput.text);
            if (int.TryParse(View.MaxInput.text, out var max))
                ccDef.SetMaxValue(max);
            if (int.TryParse(View.MinInput.text, out var min))
                ccDef.SetMinValue(min);
            if (int.TryParse(View.StartInput.text, out var start))
                ccDef.SetStartValue(start);

            AddCcDefSignal.Dispatch(ccDef);
            SaveMetadata(ccNum);
            ResetValues();
        }

        private void SaveMetadata(int ccNum)
        {
            var instrument = InstrumentsModel.GetEditedInstrument();
            if (instrument == null)
                return;

            var fullName = View.FullNameInput.text.Trim();
            var notes = View.NotesInput.text.Trim();
            if (fullName.Length == 0 && notes.Length == 0)
                return;

            var meta = MetadataModel.GetOrCreate(instrument.Name).GetOrCreateCc(ccNum);
            if (fullName.Length > 0)
                meta.FullName = fullName;
            if (notes.Length > 0)
                meta.Description = notes;
        }

        public void ResetValues()
        {
            View.CcInput.text = "";
            View.NameInput.text = "";
            View.FullNameInput.text = "";
            View.NotesInput.text = "";
            View.StartInput.text = CcDef.MIN_CC_VALUE.ToString();
            View.MinInput.text = CcDef.MIN_CC_VALUE.ToString();
            View.MaxInput.text = CcDef.MAX_CC_VALUE.ToString();
            _labelAutoFilled = true;
        }
    }
}
