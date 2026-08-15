using System.Collections.Generic;
using Framewerk.UI.List;

namespace CKIEditor.Model.Defs
{
    public class InstrumentDef : IListItemDataProvider
    {
        public static string DEFAULT_NAME = "New instrument";
        
        public int Id;
        public string Name = DEFAULT_NAME;
        public int MidiPort = 1; // 1 - 5 midi, 6 - 11 usb (usb1 - usb6)
        public int MidiChannel = 1;

        //null = "off" (default note follows the scene root note)
        public Note? DefaultNote = new Note("C 3");
        public PatternType DefaultPattern = PatternType.Sel;
        public bool Multi;
        //0 = off, otherwise number of spread channels (2 - 16)
        public int PolySpread;
        public bool NoXpose;
        public bool NoFts;
        public bool NoThru;
        public bool NoBankM;
        public bool NoBankL;
        public bool ShowNoteNums;
        public bool PresendPgm;
        public Dictionary<int, TrackValueDef> TrackValues = new Dictionary<int, TrackValueDef>();
        public Dictionary<int, CcDef> CcDefs = new Dictionary<int, CcDef>();
        public Dictionary<int, NoteRowDef> NoteRowDefs = new Dictionary<int, NoteRowDef>();

        public InstrumentDef()
        {
            for (var i = 1; i < CkiConsts.TRACK_VALUES_PER_SCREEN * CkiConsts.TRACK_VALUE_ROWS + 1; i++)
            {
                TrackValues[i] = new TrackValueDef {SlotIndex = i, Type = TrackValueType.Empty};
            }
        }
    }
}