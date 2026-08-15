using CKIEditor.Controller;
using CKIEditor.Metadata;
using CKIEditor.Model;
using CKIEditor.Model.Defs;
using CKIEditor.UI.TrackValues.CcList;
using Framewerk.UI.List;

namespace CKIEditor.UI.EditSection.CcEditor.CcList
{
    public class CcListItemMediator : ListItemMediator<CcListItemView, CcDef>
    {
        [Inject] public DeleteCcDefSignal DeleteCcDefSignal { get; set; }
        [Inject] public InstrumentCcDefsChangedSignal InstrumentCcDefsChangedSignal { get; set; }
        [Inject] public IInstrumentsModel InstrumentsModel { get; set; }
        [Inject] public IMetadataModel MetadataModel { get; set; }

        public override void OnRegister()
        {
            base.OnRegister();

            View.NameInput.characterLimit = CkiConsts.CC_NAME_CHARACTER_LIMIT;
            UpdateSelected();

            AddInputListener(View.NameInput, NameInputHandler);
            AddInputListener(View.CcInput, CcInputHandler);
            AddInputListener(View.StartInput, StartInputHandler);
            AddInputListener(View.MinInput, MinInputHandler);
            AddInputListener(View.MaxInput, MaxInputHandler);

            AddButtonListener(View.RemoveButton, RemoveButtonClickHandler);
        }

        public override void SetData(CcDef dataProvider, int index)
        {
            base.SetData(dataProvider, index);

            View.CcInput.text = dataProvider.CcNum.ToString();
            View.NameInput.text = dataProvider.Label;
            View.StartInput.text = dataProvider.StartValue.ToString();
            View.MinInput.text = dataProvider.MinValue.ToString();
            View.MaxInput.text = dataProvider.MaxValue.ToString();

            ShowDocumentationHint();
        }

        public override void SetSelected(bool selected)
        {
            base.SetSelected(selected);
            UpdateSelected();

            View.NameInput.text = DataProvider.Label;
            View.NameInput.Select();
        }

        private void CcInputHandler(string value)
        {
            if (!int.TryParse(value, out var newCcNum)
                || newCcNum < CcDef.MIN_CC_VALUE || newCcNum > CcDef.MAX_CC_VALUE)
                return;

            var oldCcNum = DataProvider.CcNum;
            if (newCcNum == oldCcNum)
                return;

            DataProvider.SetCcNum(newCcNum);
            MoveMetadata(oldCcNum, newCcNum);
            InstrumentCcDefsChangedSignal.Dispatch();
        }

        //sidecar documentation is keyed by CC number - follow the renumbering
        private void MoveMetadata(int oldCcNum, int newCcNum)
        {
            var instrument = InstrumentsModel.GetEditedInstrument();
            if (instrument == null)
                return;

            var meta = MetadataModel.Get(instrument.Name);
            if (meta == null || !meta.CcMeta.TryGetValue(oldCcNum, out var ccMeta))
                return;

            meta.CcMeta.Remove(oldCcNum);
            meta.CcMeta[newCcNum] = ccMeta;
        }

        private void NameInputHandler(string ccLabel)
        {
            DataProvider.SetLabel(ccLabel);
            InstrumentCcDefsChangedSignal.Dispatch();
        }

        private void StartInputHandler(string value)
        {
            if (!int.TryParse(value, out var start))
                return;

            DataProvider.SetStartValue(start);
            InstrumentCcDefsChangedSignal.Dispatch();
        }

        private void MinInputHandler(string value)
        {
            if (!int.TryParse(value, out var min))
                return;

            DataProvider.SetMinValue(min);
            InstrumentCcDefsChangedSignal.Dispatch();
        }

        private void MaxInputHandler(string value)
        {
            if (!int.TryParse(value, out var max))
                return;

            DataProvider.SetMaxValue(max);
            InstrumentCcDefsChangedSignal.Dispatch();
        }

        private void RemoveButtonClickHandler()
        {
            DeleteCcDefSignal.Dispatch(DataProvider.CcNum);
        }

        //surface the sidecar full name where the row has room for it:
        //as the label input's placeholder, visible whenever the label is empty.
        //always assigned - list items are reused, so a stale hint must be overwritten
        private void ShowDocumentationHint()
        {
            if (!(View.NameInput.placeholder is TMPro.TMP_Text placeholderText))
                return;

            var hint = "label";
            var instrument = InstrumentsModel.GetEditedInstrument();
            if (instrument != null)
            {
                var meta = MetadataModel.Get(instrument.Name);
                if (meta != null && meta.CcMeta.TryGetValue(DataProvider.CcNum, out var ccMeta)
                    && !string.IsNullOrEmpty(ccMeta.FullName))
                    hint = ccMeta.FullName;
            }

            placeholderText.text = hint;
        }

        private void UpdateSelected()
        {
            View.BackgroundImage.color = IsSelected ? View.SelectedColor : View.NormalColor;
        }
    }
}
