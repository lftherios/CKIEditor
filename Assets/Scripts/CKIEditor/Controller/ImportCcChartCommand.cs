using CKIEditor.Metadata;
using CKIEditor.Model;
using CKIEditor.Model.Defs;
using CKIEditor.Serialization;
using CKIEditor.UI;
using CKIEditor.Validation;
using strange.extensions.command.impl;
using strange.extensions.signal.impl;
using UnityEngine;

namespace CKIEditor.Controller
{
    public class ImportCcChartSignal : Signal
    {

    }

    /// <summary>
    /// "Paste from chart": reads MIDI-implementation-chart lines from the clipboard,
    /// creates or updates CC defs (labels auto-abbreviated from full names),
    /// and stores the full names in the instrument's sidecar documentation.
    /// </summary>
    public class ImportCcChartCommand : Command
    {
        [Inject] public IInstrumentsModel InstrumentsModel { get; set; }
        [Inject] public IMetadataModel MetadataModel { get; set; }
        [Inject] public InstrumentCcDefsChangedSignal InstrumentCcDefsChangedSignal { get; set; }

        public override void Execute()
        {
            var instrument = InstrumentsModel.GetEditedInstrument();
            if (instrument == null)
                return;

            var entries = ChartParser.Parse(GUIUtility.systemCopyBuffer);
            if (entries.Count == 0)
            {
                Toast.Show("No CC lines found in the clipboard — copy chart rows like “19 Filter Cutoff 0-127” first.");
                return;
            }

            int added = 0, updated = 0;
            foreach (var entry in entries)
            {
                var isNew = !instrument.CcDefs.TryGetValue(entry.CcNum, out var ccDef);
                if (isNew)
                {
                    ccDef = new CcDef(entry.CcNum);
                    instrument.CcDefs[entry.CcNum] = ccDef;
                    added++;
                }
                else
                {
                    updated++;
                }

                if (!string.IsNullOrEmpty(entry.FullName) && (isNew || string.IsNullOrEmpty(ccDef.Label)))
                    ccDef.SetLabel(LabelAbbreviator.Suggest(entry.FullName));

                //max before min so clamping can't fight the incoming range
                if (entry.Max.HasValue)
                    ccDef.SetMaxValue(entry.Max.Value);
                if (entry.Min.HasValue)
                    ccDef.SetMinValue(entry.Min.Value);
                if (entry.Start.HasValue)
                    ccDef.SetStartValue(entry.Start.Value);

                if (!string.IsNullOrEmpty(entry.FullName))
                    MetadataModel.GetOrCreate(instrument.Name).GetOrCreateCc(entry.CcNum).FullName = entry.FullName;
            }

            InstrumentCcDefsChangedSignal.Dispatch();

            var summary = updated > 0
                ? $"Added {added} CCs, updated {updated} from clipboard"
                : $"Added {added} CC{(added == 1 ? "" : "s")} from clipboard";
            Toast.Show(summary);
        }
    }
}
