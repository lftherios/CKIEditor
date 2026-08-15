using System.Collections.Generic;
using System.IO;
using System.Linq;
using CKIEditor.Metadata;
using CKIEditor.Model;
using CKIEditor.Model.Defs;
using CKIEditor.Serialization;
using CKIEditor.UI.Preflight;
using CKIEditor.Validation;
using Crosstales.FB;
using Framewerk.Managers;
using strange.extensions.command.impl;
using strange.extensions.signal.impl;

namespace CKIEditor.Controller
{
    public class ExportInstrumentsSignal : Signal
    {

    }

    public class ExportInstrumentsCommand : Command
    {
        [Inject] public IInstrumentsModel InstrumentsModel { get; set; }
        [Inject] public IMetadataModel MetadataModel { get; set; }
        [Inject(BindingKeys.PARSER_CKI)] public IInstrumentsParser InstrumentsParser { get; set; }
        [Inject] public IPlayerPrefsManager PrefsManager { get; set; }

        private const string SAVE_DIRECTORY_KEY = "saveDirectory";

        public override void Execute()
        {
            var instruments = InstrumentsModel.GetAllInstruments();
            var findings = InstrumentValidator.ValidateLibrary(instruments);

            if (findings.Count == 0)
            {
                DoExport(instruments);
                return;
            }

            //preflight: errors block export until fixed, warnings ship knowingly
            Retain();
            PreflightDialog.Show(findings, BuildSummary(instruments),
                onExport: () =>
                {
                    DoExport(instruments);
                    Release();
                },
                onCancel: Release);
        }

        private void DoExport(List<InstrumentDef> instruments)
        {
            var loadDirectory = PrefsManager.GetUserString(SAVE_DIRECTORY_KEY, null);
            var path = FileBrowser.SaveFile("Export CKI file", loadDirectory, "Library", JsonKeys.FILE_EXTENSIONS);

            if (string.IsNullOrEmpty(path))
                return;

            PrefsManager.SetUserData(SAVE_DIRECTORY_KEY, Path.GetDirectoryName(path));

            var json = InstrumentsParser.SerializeInstruments(instruments);
            File.WriteAllText(path, json);

            WriteSidecar(path, instruments);
        }

        //full names and notes travel next to the .CKI so re-imports keep the documentation
        private void WriteSidecar(string ckiPath, List<InstrumentDef> instruments)
        {
            var exportedNames = new HashSet<string>(instruments.Select(i => i.Name));
            var metadata = MetadataModel.GetAll()
                .Where(pair => exportedNames.Contains(pair.Key) && pair.Value != null && !pair.Value.IsEmpty)
                .ToDictionary(pair => pair.Key, pair => pair.Value);

            if (metadata.Count == 0)
                return;

            File.WriteAllText(MetadataSerializer.SidecarPathFor(ckiPath), MetadataSerializer.Serialize(metadata));
        }

        private static string BuildSummary(List<InstrumentDef> instruments)
        {
            var ccDefs = instruments.Sum(i => i.CcDefs.Count);
            return $"Exporting {instruments.Count} instrument{(instruments.Count == 1 ? "" : "s")} · {ccDefs} CC defs";
        }
    }
}
