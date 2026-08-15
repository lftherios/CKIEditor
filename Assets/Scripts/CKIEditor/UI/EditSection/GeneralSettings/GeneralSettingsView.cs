using strange.extensions.mediation.impl;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CKIEditor.UI.EditSection.General
{
    public class GeneralSettingsView : View
    {
        public TMP_InputField InstrumentNameInput;
        public TMP_Dropdown MidiPortDropdown;
        public TMP_Dropdown MidiChannelDropdown;
        public TMP_Dropdown DefaultNoteDropdown;
        public TMP_Dropdown DefaultNoteOctaveDropdown;
        public TMP_Dropdown DefaultPatternDropdown;
        public Toggle MultiToggle;
        public Toggle PolySpreadToggle;
        public Toggle NoTransposeToggle;
        public Toggle NoFtsToggle;

        //CirkOS 1.18 - 1.22 instrument settings.
        //Created at runtime by cloning the NoFts row, so the prefab doesn't need to be edited.
        public Toggle NoBankMToggle;
        public Toggle NoBankLToggle;
        public Toggle ShowNoteNumsToggle;
        public Toggle NoThruToggle;
        public Toggle PresendPgmToggle;

        protected override void Awake()
        {
            if (NoBankMToggle == null)
                NoBankMToggle = CreateToggleRow("NoBankM", "CC0 = bankM");
            if (NoBankLToggle == null)
                NoBankLToggle = CreateToggleRow("NoBankL", "CC32 = bankL");
            if (ShowNoteNumsToggle == null)
                ShowNoteNumsToggle = CreateToggleRow("ShowNoteNums", "Note Nums");
            if (NoThruToggle == null)
                NoThruToggle = CreateToggleRow("NoThru", "No Edtrk Thru");
            if (PresendPgmToggle == null)
                PresendPgmToggle = CreateToggleRow("PresendPgm", "Pre-send Pgm");

            base.Awake();
        }

        private Toggle CreateToggleRow(string rowName, string label)
        {
            var templateRow = NoFtsToggle.transform.parent;
            var row = Instantiate(templateRow.gameObject, templateRow.parent);
            row.name = rowName;

            var labelText = row.GetComponentInChildren<TMP_Text>();
            if (labelText != null)
                labelText.text = label;

            var toggle = row.GetComponentInChildren<Toggle>();
            toggle.onValueChanged.RemoveAllListeners();
            toggle.isOn = false;

            return toggle;
        }
    }
}
