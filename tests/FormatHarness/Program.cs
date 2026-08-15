using System;
using System.IO;
using System.Linq;
using CKIEditor;
using CKIEditor.Model;
using CKIEditor.Model.Defs;
using CKIEditor.Serialization;

public static class Program
{
    private static int _failures;

    private static void Check(bool condition, string label)
    {
        Console.WriteLine((condition ? "PASS " : "FAIL ") + label);
        if (!condition) _failures++;
    }

    public static int Main(string[] args)
    {
        var parser = new CkiInstrumentsParser();

        // --- 1. CirkOS 1.22 / Cirklon 2 style file with all new keys ---
        const string modernJson = @"{
  ""instrument_data"": {
    ""Templt"": {
      ""midi_port"": 3,
      ""midi_chan"": 2,
      ""multi"": true,
      ""presend_pgm"": true,
      ""default_note"": ""off"",
      ""default_patt"": ""P3"",
      ""poly_spread"": 4,
      ""no_bankL"": true,
      ""no_bankM"": true,
      ""no_xpose"": true,
      ""no_fts"": true,
      ""show_note_nums"": true,
      ""no_thru"": true,
      ""track_values"": {
        ""slot_1"": { ""track_control"": ""pgm"" },
        ""slot_2"": { ""MIDI_CC"": 74, ""label"": ""cutoff"" },
        ""slot_180"": { ""track_control"": ""reich"" }
      },
      ""CC_defs"": {
        ""CC_74"": { ""label"": ""cutoff"", ""min_val"": 5, ""max_val"": 100, ""start_val"": 42 }
      },
      ""row_defs"": {
        ""C 3"": { ""label"": ""kick"", ""always_show"": true },
        ""D X"": { ""label"": ""high"", ""always_show"": false }
      }
    }
  }
}";
        var insts = parser.ParseInstruments(modernJson);
        Check(insts.Count == 1, "modern file parses one instrument");
        var inst = insts[0];
        Check(inst.MidiPort == 3 && inst.MidiChannel == 2, "port/channel");
        Check(inst.Multi, "multi");
        Check(inst.PresendPgm, "presend_pgm parsed");
        Check(inst.DefaultNote == null, "default_note off -> null");
        Check(inst.PolySpread == 4, "poly_spread number parsed");
        Check(inst.NoBankL && inst.NoBankM, "no_bankL/no_bankM parsed");
        Check(inst.NoXpose && inst.NoFts, "no_xpose/no_fts parsed");
        Check(inst.ShowNoteNums, "show_note_nums parsed");
        Check(inst.NoThru, "no_thru parsed");
        Check(inst.TrackValues.Count == CkiConsts.TRACK_VALUES_PER_SCREEN * CkiConsts.TRACK_VALUE_ROWS, "180 track value slots");
        Check(inst.TrackValues[180].Type == TrackValueType.TrackControl, "slot_180 accepted");
        Check(inst.TrackValues[2].MidiCC == 74 && inst.TrackValues[2].Label == "cutoff", "CC track value");
        Check(inst.CcDefs[74].StartValue == 42 && inst.CcDefs[74].MinValue == 5 && inst.CcDefs[74].MaxValue == 100, "CC def min/max/start");
        var rowX = inst.NoteRowDefs.Values.FirstOrDefault(r => r.Label == "high");
        Check(rowX != null && rowX.Note.Id == 122 && rowX.Note.Name == "D X", "row_defs octave X parses to octave 10");

        // --- 2. Round-trip: serialize and re-parse ---
        var serialized = parser.SerializeInstruments(insts);
        Check(serialized.Contains("\"presend_pgm\""), "serialized contains presend_pgm");
        Check(serialized.Contains("\"no_thru\""), "serialized contains no_thru");
        Check(serialized.Contains("\"no_bankM\"") && serialized.Contains("\"no_bankL\""), "serialized contains bank keys");
        Check(serialized.Contains("\"show_note_nums\""), "serialized contains show_note_nums");
        Check(serialized.Contains("\"default_note\" : \"off\"") || serialized.Contains("\"default_note\":\"off\""), "default_note serialized as off");
        Check(serialized.Contains("\"poly_spread\" : 4") || serialized.Contains("\"poly_spread\":4"), "poly_spread serialized as number");

        var reparsed = parser.ParseInstruments(serialized)[0];
        Check(reparsed.PresendPgm == inst.PresendPgm
              && reparsed.NoThru == inst.NoThru
              && reparsed.NoBankM == inst.NoBankM
              && reparsed.NoBankL == inst.NoBankL
              && reparsed.ShowNoteNums == inst.ShowNoteNums
              && reparsed.PolySpread == inst.PolySpread
              && Nullable.Equals(reparsed.DefaultNote, inst.DefaultNote), "round-trip globals identical");
        Check(reparsed.CcDefs[74].StartValue == 42, "round-trip start_val preserved (bug fix)");
        Check(reparsed.TrackValues[180].TrackControl == TrackControlType.Reich, "round-trip slot 180");
        var rowX2 = reparsed.NoteRowDefs.Values.FirstOrDefault(r => r.Label == "high");
        Check(rowX2 != null && rowX2.Note.Id == 122, "round-trip octave X row");

        // --- 3. poly_spread legacy/off forms ---
        const string offJson = @"{""instrument_data"":{""A"":{""midi_port"":1,""midi_chan"":1,""default_note"":""C 3"",""default_patt"":""CK"",""poly_spread"":""off""}}}";
        var offInst = parser.ParseInstruments(offJson)[0];
        Check(offInst.PolySpread == 0, "poly_spread off string -> 0");
        Check(offInst.DefaultNote.HasValue && offInst.DefaultNote.Value.Name == "C 3", "default_note C 3 parsed");
        var offSerialized = parser.SerializeInstruments(new System.Collections.Generic.List<InstrumentDef> { offInst });
        Check(offSerialized.Contains("\"poly_spread\" : \"off\"") || offSerialized.Contains("\"poly_spread\":\"off\""), "poly_spread 0 serialized as off");

        const string legacyJson = @"{""instrument_data"":{""B"":{""midi_port"":1,""midi_chan"":1,""default_note"":""C 3"",""default_patt"":""CK"",""poly_spread"":true}}}";
        Check(parser.ParseInstruments(legacyJson)[0].PolySpread == 2, "legacy poly_spread true -> 2");

        // --- 4. The full real INSTS.CKI sample (v1.17 era) ---
        var samplePath = args.Length > 0
            ? args[0]
            : new[] {"../../Assets/Resources/INSTS.CKI", "Assets/Resources/INSTS.CKI"}.FirstOrDefault(File.Exists);
        if (samplePath != null && File.Exists(samplePath))
        {
            var sample = parser.ParseInstruments(File.ReadAllText(samplePath));
            Check(sample.Count == 19, $"INSTS.CKI parses {sample.Count} instruments (expect 19)");
            var sampleSerialized = parser.SerializeInstruments(sample);
            var sampleReparsed = parser.ParseInstruments(sampleSerialized);
            Check(sampleReparsed.Count == sample.Count, "INSTS.CKI round-trip instrument count");
            for (var i = 0; i < sample.Count; i++)
            {
                var a = sample[i];
                var b = sampleReparsed[i];
                var same = a.Name == b.Name && a.MidiPort == b.MidiPort && a.MidiChannel == b.MidiChannel
                           && Nullable.Equals(a.DefaultNote, b.DefaultNote) && a.DefaultPattern == b.DefaultPattern
                           && a.CcDefs.Count == b.CcDefs.Count && a.NoteRowDefs.Count == b.NoteRowDefs.Count
                           && a.TrackValues.Values.Count(tv => tv.Type != TrackValueType.Empty)
                              == b.TrackValues.Values.Count(tv => tv.Type != TrackValueType.Empty)
                           && a.CcDefs.Keys.All(k => b.CcDefs.ContainsKey(k)
                                                     && a.CcDefs[k].StartValue == b.CcDefs[k].StartValue
                                                     && a.CcDefs[k].MinValue == b.CcDefs[k].MinValue
                                                     && a.CcDefs[k].MaxValue == b.CcDefs[k].MaxValue
                                                     && a.CcDefs[k].Label == b.CcDefs[k].Label)
                           && a.NoteRowDefs.Keys.All(k => b.NoteRowDefs.ContainsKey(k)
                                                          && a.NoteRowDefs[k].Label == b.NoteRowDefs[k].Label
                                                          && a.NoteRowDefs[k].AlwaysShow == b.NoteRowDefs[k].AlwaysShow);
                if (!same)
                {
                    Check(false, $"INSTS.CKI round-trip instrument '{a.Name}'");
                }
            }
            Check(true, "INSTS.CKI per-instrument round-trip complete");
        }

        // --- 5. Cirklon 2 template file (real-world key set) ---
        var templatePath = args.Length > 1 ? args[1] : null;
        if (templatePath != null && File.Exists(templatePath))
        {
            var template = parser.ParseInstruments(File.ReadAllText(templatePath))[0];
            Check(template.MidiPort == 1 && template.PolySpread == 0 && !template.NoThru, "Cirklon 2 template parses");
        }

        // --- 6. Note helper sanity ---
        Check(new Note(61).Name == "C#5", "Note(61) name (GetNoteName swap fix)");
        Check(new Note("C#5").Id == 61, "Note name -> id");
        Check(new Note(0, 10).Name == "C X", "octave 10 rendered as X");
        Check(new Note("C X").Id == 120, "octave X parsed as 10");

        // --- 7. Validation + sidecar metadata ---
        LogicTests.Run(Check);

        // --- 8. Paste-from-chart parser ---
        ChartTests.Run(Check);

        // --- 9. Track value arranger ---
        ArrangerTests.Run(Check);

        Console.WriteLine();
        Console.WriteLine(_failures == 0 ? "ALL TESTS PASSED" : $"{_failures} FAILURES");
        return _failures == 0 ? 0 : 1;
    }
}
