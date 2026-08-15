using System.Linq;
using CKIEditor.Controller;
using CKIEditor.Model;
using CKIEditor.Model.Defs;
using Framewerk.UI.List;

namespace CKIEditor.UI.TrackValues
{
    public class TrackValueListMediator : ListMediator<TrackValueListView, TrackValueDataProvider>
    {
        [Inject] public IInstrumentsModel InstrumentsModel{ get; set; }

        [Inject] public EditedInstrumentChangedSignal EditedInstrumentChangedSignal{ get; set; }
        [Inject] public TrackValuesChangedSignal TrackValuesChangedSignal{ get; set; }

        public override void OnRegister()
        {
            base.OnRegister();

            var instrument = InstrumentsModel.GetEditedInstrument();
            UpdateInstrument(instrument);

            EditedInstrumentChangedSignal.AddListener(UpdateInstrument);
            TrackValuesChangedSignal.AddListener(TrackValuesChangedHandler);
        }

        public override void OnRemove()
        {
            base.OnRemove();

            EditedInstrumentChangedSignal.RemoveListener(UpdateInstrument);
            TrackValuesChangedSignal.RemoveListener(TrackValuesChangedHandler);
        }

        private void TrackValuesChangedHandler()
        {
            UpdateInstrument(InstrumentsModel.GetEditedInstrument());
        }

        private void UpdateInstrument(InstrumentDef instrument)
        {
            if (instrument == null)
                return;

            //slots ordered by index so the rows-of-six grid matches hardware numbering
            var listData = instrument.TrackValues
                .OrderBy(pair => pair.Key)
                .Select(pair => new TrackValueDataProvider(pair.Value))
                .ToList();
            SetData(listData);
        }
    }

    public class TrackValueDataProvider : IListItemDataProvider
    {
        public TrackValueDef TrackValue { get; private set; }

        public TrackValueDataProvider(TrackValueDef trackValue)
        {
            TrackValue = trackValue;
        }
    }
}