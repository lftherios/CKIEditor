namespace CKIEditor.Serialization
{
    public static class JsonKeys
    {
        public static string[] FILE_EXTENSIONS = {"CKI", "cki", "txt"};
        public static string PYRAMID_EXTENSION = "txt";
        public const string INSTRUMENT_DATA = "instrument_data";
        
        //global
        public const string MIDI_PORT = "midi_port";
        public const string MIDI_CHAN = "midi_chan";
        public const string DEFAULT_NOTE = "default_note";
        public const string DEFAULT_PATT = "default_patt";
        public const string MULTI = "multi";
        public const string POLY_SPREAD = "poly_spread";
        public const string NO_XPOSE = "no_xpose";
        public const string NO_FTS = "no_fts";
        //global - added in CirkOS 1.18 - 1.22
        public const string NO_THRU = "no_thru";
        public const string NO_BANK_M = "no_bankM";
        public const string NO_BANK_L = "no_bankL";
        public const string SHOW_NOTE_NUMS = "show_note_nums";
        public const string PRESEND_PGM = "presend_pgm";

        public const string OFF = "off";
        
        //track values
        public const string TRACK_VALUES = "track_values";
        public const string MIDI_CC = "MIDI_CC";
        public const string TRACK_CONTROL = "track_control";
        
        //cc defs
        public const string CC_DEFS = "CC_defs";
        
        public const string MIN_VAL = "min_val";
        public const string MAX_VAL = "max_val";
        public const string START_VAL = "start_val";
        
        //row defs
        public const string ROW_DEFS = "row_defs";
        public const string ALWAYS_SHOW = "always_show";
        
        //general
        public const string LABEL = "label";
    }
}