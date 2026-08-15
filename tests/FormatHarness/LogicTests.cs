using System;
using System.Collections.Generic;
using System.Linq;
using CKIEditor.Metadata;
using CKIEditor.Model;
using CKIEditor.Model.Defs;
using CKIEditor.Validation;

public static class LogicTests
{
    public static void Run(Action<bool, string> check)
    {
        // ---- abbreviator ----
        check(LabelAbbreviator.Suggest("Filter Cutoff") == "FltCut", "abbrev: Filter Cutoff -> FltCut");
        check(LabelAbbreviator.Suggest("Feedback") == "Fdbk", "abbrev: Feedback -> Fdbk");
        check(LabelAbbreviator.Suggest("Feedback Level") == "FdbkLv", "abbrev: Feedback Level -> FdbkLv");
        check(LabelAbbreviator.Suggest("Glide") == "Glide", "abbrev: short name passes through");
        check(LabelAbbreviator.Suggest("Osc 2 Frequency") == "Osc2Fr", "abbrev: Osc 2 Frequency -> Osc2Fr");
        check(LabelAbbreviator.Suggest("").Length == 0, "abbrev: empty ok");

        // ---- validator: clean instrument yields no findings ----
        var clean = MakeInstrument("Sub 37", 3, 1);
        clean.TrackValues[1].Type = TrackValueType.MidiCC;
        clean.TrackValues[1].MidiCC = 19;
        clean.TrackValues[1].Label = "FltCut";
        var cleanFindings = InstrumentValidator.ValidateLibrary(new List<InstrumentDef> { clean });
        check(cleanFindings.Count == 0, "validator: clean instrument has no findings");

        // ---- duplicate track-value CC ----
        var dup = MakeInstrument("DupCC", 1, 1);
        SetCc(dup, 2, 74, "OscOct");
        SetCc(dup, 5, 74, "CutMW");
        var dupFindings = InstrumentValidator.ValidateLibrary(new List<InstrumentDef> { dup });
        var dupErr = dupFindings.FirstOrDefault(f => f.Severity == FindingSeverity.Error);
        check(dupErr != null && dupErr.Title.Contains("CC 74"), "validator: duplicate CC flagged as error");
        check(dupErr.CanFix, "validator: duplicate CC has a fix");
        dupErr.ApplyFix();
        check(dup.TrackValues[5].Type == TrackValueType.Empty, "validator: fix clears the later slot");
        check(InstrumentValidator.ValidateLibrary(new List<InstrumentDef> { dup }).Count == 0,
            "validator: re-validation clean after fix");

        // ---- long label ----
        var longLbl = MakeInstrument("LongLbl", 1, 1);
        SetCc(longLbl, 1, 118, "Feedback");
        var lblFindings = InstrumentValidator.ValidateLibrary(new List<InstrumentDef> { longLbl });
        var lblWarn = lblFindings.FirstOrDefault(f => f.Severity == FindingSeverity.Warning);
        check(lblWarn != null && lblWarn.FixLabel.Contains("Fdbk"), "validator: long label warns with suggestion");
        lblWarn.ApplyFix();
        check(longLbl.TrackValues[1].Label == "Fdbk", "validator: label fix applies suggestion");

        // ---- name too long ----
        var longName = MakeInstrument("Grandmother!", 1, 2);
        var nameFindings = InstrumentValidator.ValidateLibrary(new List<InstrumentDef> { longName });
        var nameErr = nameFindings.FirstOrDefault(f => f.Severity == FindingSeverity.Error);
        check(nameErr != null && nameErr.CanFix, "validator: 12-char name is an error with fix");
        nameErr.ApplyFix();
        check(longName.Name == "Grandmoth", "validator: name fix truncates to 9");

        // ---- poly spread past channel 16 ----
        var spread = MakeInstrument("Spread", 1, 14);
        spread.PolySpread = 4;
        var spreadFindings = InstrumentValidator.ValidateLibrary(new List<InstrumentDef> { spread });
        var spreadErr = spreadFindings.FirstOrDefault(f => f.Severity == FindingSeverity.Error);
        check(spreadErr != null && spreadErr.Title.Contains("channel 16"), "validator: spread overflow is an error");
        spreadErr.ApplyFix();
        check(spread.PolySpread == 3, "validator: spread fix reduces to fit (14+3-1=16)");

        // ---- shared routing info + duplicate names ----
        var a = MakeInstrument("SameName", 3, 1);
        var b = MakeInstrument("SameName", 3, 1);
        var libFindings = InstrumentValidator.ValidateLibrary(new List<InstrumentDef> { a, b });
        check(libFindings.Any(f => f.Severity == FindingSeverity.Info && f.Title.Contains("shared")),
            "validator: shared port/channel is an info");
        check(libFindings.Any(f => f.Severity == FindingSeverity.Error && f.Title.Contains("named")),
            "validator: duplicate names are an error");

        // ---- illegal characters ----
        var weird = MakeInstrument("Weird", 1, 3);
        SetCc(weird, 1, 10, "Cut\"?*");
        var weirdFindings = InstrumentValidator.ValidateLibrary(new List<InstrumentDef> { weird });
        var charWarn = weirdFindings.FirstOrDefault(f => f.Severity == FindingSeverity.Warning);
        check(charWarn != null && charWarn.CanFix, "validator: illegal characters warn with fix");
        charWarn.ApplyFix();
        check(weird.TrackValues[1].Label == "Cut", "validator: illegal characters stripped");

        // ---- metadata: sidecar round-trip ----
        var model = new MetadataModel();
        var sub = model.GetOrCreate("Sub 37");
        sub.Notes = "Live rig, channel 1.";
        var cc19 = sub.GetOrCreateCc(19);
        cc19.FullName = "Filter Cutoff";
        cc19.Description = "Main sweep, 20 Hz - 20 kHz.";
        cc19.Group = "Filter";

        var json = MetadataSerializer.Serialize(model.GetAll());
        var parsed = MetadataSerializer.Parse(json);
        check(parsed.Count == 1
              && parsed["Sub 37"].Notes == "Live rig, channel 1."
              && parsed["Sub 37"].CcMeta[19].FullName == "Filter Cutoff"
              && parsed["Sub 37"].CcMeta[19].Group == "Filter", "sidecar: serialize/parse round-trip");

        // ---- metadata: merge never blanks existing fields ----
        var incoming = new Dictionary<string, InstrumentMetadata>
        {
            ["Sub 37"] = new InstrumentMetadata
            {
                CcMeta = { [19] = new CcMetadata { FullName = "Cutoff Frequency" } }
            }
        };
        model.Merge(incoming);
        var merged = model.Get("Sub 37").CcMeta[19];
        check(merged.FullName == "Cutoff Frequency" && merged.Description == "Main sweep, 20 Hz - 20 kHz.",
            "sidecar: merge updates name, keeps description");

        // ---- metadata: rename keeps documentation ----
        model.Rename("Sub 37", "Sub37 v2");
        check(model.Get("Sub 37") == null && model.Get("Sub37 v2").CcMeta[19].Description.Contains("20 kHz"),
            "sidecar: rename carries metadata");

        // ---- summarize ----
        check(InstrumentValidator.Summarize(clean).Contains("1 of 180 slots"), "summarize mentions slot usage");
    }

    private static InstrumentDef MakeInstrument(string name, int port, int channel)
    {
        return new InstrumentDef { Name = name, MidiPort = port, MidiChannel = channel };
    }

    private static void SetCc(InstrumentDef inst, int slot, int cc, string label)
    {
        inst.TrackValues[slot].Type = TrackValueType.MidiCC;
        inst.TrackValues[slot].MidiCC = cc;
        inst.TrackValues[slot].Label = label;
    }
}
