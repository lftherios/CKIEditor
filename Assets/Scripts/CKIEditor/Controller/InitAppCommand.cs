using System.IO;
using CKIEditor.Model;
using CKIEditor.Serialization;
using CKIEditor.UI;
using Framewerk.Managers;
using strange.extensions.command.impl;
using UnityEngine;

namespace CKIEditor.Controller
{
    public class InitAppCommand : Command
    {
        [Inject] public IUiManager UiManager { get; set; }
        [Inject] public IInstrumentsModel InstrumentsModel { get; set; }
        [Inject(BindingKeys.PARSER_CKI)] public IInstrumentsParser InstrumentsParser { get; set; }
        
        [Inject] public InstrumentsImportedSignal InstrumentsImportedSignal { get; set; }
        [Inject] public EditedInstrumentChangedSignal EditedInstrumentChangedSignal { get; set; }
        
        //optional dev convenience - instruments at this path are loaded on startup when the file exists
        private const string STARTUP_CKI_PATH = "/TEMP/CKI_EDITOR/TEST-INS.CKI";

        public override void Execute()
        {
            if (File.Exists(STARTUP_CKI_PATH))
            {
                var jsonString = File.ReadAllText(STARTUP_CKI_PATH);
                var instruments = InstrumentsParser.ParseInstruments(jsonString);
                InstrumentsModel.AddInstruments(instruments);

                InstrumentsModel.SelectEditedInstrument(0);
                EditedInstrumentChangedSignal.Dispatch(InstrumentsModel.GetEditedInstrument());
                InstrumentsImportedSignal.Dispatch();
            }

            Screen.fullScreen = false;

            UiManager.InstantiateView<EditorScreenView>();
        }
    }
}