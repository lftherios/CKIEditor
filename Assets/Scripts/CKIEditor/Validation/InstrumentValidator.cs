using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CKIEditor.Model;
using CKIEditor.Model.Defs;

namespace CKIEditor.Validation
{
    /// <summary>
    /// Preflight checks over instrument definitions. Pure C# - no Unity dependencies -
    /// so it runs in unit tests and at import time as well as before export.
    /// Errors describe files the Cirklon would mangle; warnings describe things it
    /// will silently change; infos are worth knowing, nothing more.
    /// </summary>
    public static class InstrumentValidator
    {
        //characters the Cirklon accepts in labels
        private static readonly Regex LegalLabel = new Regex(@"^[\-A-Za-z0-9()#. $@!&~%/+]*$");

        private const int MAX_NAME = 9;
        private const int MAX_LABEL = 6;
        private const int MAX_NOTE_ID = 127; // G10 ("G X")
        private const int KNOWN_PORT_MAX = 11; // MIDI 1-5, USB 1-6

        public static List<ValidationFinding> ValidateLibrary(List<InstrumentDef> instruments)
        {
            var findings = new List<ValidationFinding>();

            foreach (var instrument in instruments)
                ValidateInstrument(instrument, findings);

            CheckSharedRouting(instruments, findings);
            CheckDuplicateNames(instruments, findings);

            //errors first, then warnings, then infos
            return findings.OrderBy(f => f.Severity).ToList();
        }

        public static void ValidateInstrument(InstrumentDef inst, List<ValidationFinding> findings)
        {
            CheckName(inst, findings);
            CheckRouting(inst, findings);
            CheckTrackValues(inst, findings);
            CheckCcDefs(inst, findings);
            CheckNoteRows(inst, findings);
        }

        public static bool HasErrors(List<ValidationFinding> findings)
        {
            return findings.Any(f => f.Severity == FindingSeverity.Error && !f.IsFixed);
        }

        /// <summary>One-line pass summary for the preflight footer.</summary>
        public static string Summarize(InstrumentDef inst)
        {
            var placed = inst.TrackValues.Values.Count(tv => tv.Type != TrackValueType.Empty);
            var slots = CkiConsts.TRACK_VALUES_PER_SCREEN * CkiConsts.TRACK_VALUE_ROWS;
            return $"{inst.Name}: name {inst.Name?.Length ?? 0}/{MAX_NAME} · " +
                   $"{inst.CcDefs.Count} CC defs · {placed} of {slots} slots · {inst.NoteRowDefs.Count} note rows";
        }

        // ---------------------------------------------------------------- checks

        private static void CheckName(InstrumentDef inst, List<ValidationFinding> findings)
        {
            if (string.IsNullOrWhiteSpace(inst.Name))
            {
                findings.Add(new ValidationFinding
                {
                    Severity = FindingSeverity.Error,
                    InstrumentName = inst.Name,
                    Title = "Instrument has no name",
                    Detail = "The Cirklon needs a name to list it. Give it one before exporting.",
                });
                return;
            }

            if (inst.Name.Length > MAX_NAME)
            {
                var truncated = inst.Name.Substring(0, MAX_NAME);
                findings.Add(new ValidationFinding
                {
                    Severity = FindingSeverity.Error,
                    InstrumentName = inst.Name,
                    Title = $"Name “{inst.Name}” won't fit",
                    Detail = $"Instrument names are {MAX_NAME} characters on the hardware.",
                    FixLabel = $"Use “{truncated}”",
                    Fix = () => inst.Name = truncated,
                });
            }
        }

        private static void CheckRouting(InstrumentDef inst, List<ValidationFinding> findings)
        {
            if (inst.MidiChannel < 1 || inst.MidiChannel > 16)
            {
                var clamped = Math.Min(Math.Max(inst.MidiChannel, 1), 16);
                findings.Add(new ValidationFinding
                {
                    Severity = FindingSeverity.Error,
                    InstrumentName = inst.Name,
                    Title = $"MIDI channel {inst.MidiChannel} doesn't exist",
                    Detail = "Channels run 1–16.",
                    FixLabel = $"Set channel {clamped}",
                    Fix = () => inst.MidiChannel = clamped,
                });
            }

            if (inst.MidiPort < 1 || inst.MidiPort > KNOWN_PORT_MAX)
            {
                findings.Add(new ValidationFinding
                {
                    Severity = FindingSeverity.Warning,
                    InstrumentName = inst.Name,
                    Title = $"Port {inst.MidiPort} is outside the editor's known range",
                    Detail = "MIDI 1–5 and USB 1–6 map to ports 1–11. Higher numbers may be CV or USB-host ports - the editor can't verify them.",
                });
            }

            if (inst.PolySpread >= CkiConsts.POLY_SPREAD_MIN)
            {
                var top = inst.MidiChannel + inst.PolySpread - 1;
                if (top > 16)
                {
                    var maxSpread = 16 - inst.MidiChannel + 1;
                    findings.Add(new ValidationFinding
                    {
                        Severity = FindingSeverity.Error,
                        InstrumentName = inst.Name,
                        Title = $"Poly spread runs past channel 16",
                        Detail = $"{inst.PolySpread} voices up from channel {inst.MidiChannel} would need channel {top}.",
                        FixLabel = maxSpread >= CkiConsts.POLY_SPREAD_MIN ? $"Reduce spread to {maxSpread}" : "Turn spread off",
                        Fix = () => inst.PolySpread = maxSpread >= CkiConsts.POLY_SPREAD_MIN ? maxSpread : CkiConsts.POLY_SPREAD_OFF,
                    });
                }
            }
        }

        private static void CheckTrackValues(InstrumentDef inst, List<ValidationFinding> findings)
        {
            var seenCc = new Dictionary<int, TrackValueDef>();

            foreach (var pair in inst.TrackValues.OrderBy(p => p.Key))
            {
                var tv = pair.Value;
                if (tv.Type != TrackValueType.MidiCC)
                    continue;

                if (tv.MidiCC < 0 || tv.MidiCC > 127)
                {
                    var slot = tv;
                    findings.Add(new ValidationFinding
                    {
                        Severity = FindingSeverity.Error,
                        InstrumentName = inst.Name,
                        Title = $"CC {tv.MidiCC} doesn't exist (slot {tv.SlotIndex})",
                        Detail = "CC numbers run 0–127.",
                        FixLabel = "Clear this slot",
                        Fix = () => slot.Type = TrackValueType.Empty,
                    });
                    continue;
                }

                if (seenCc.TryGetValue(tv.MidiCC, out var first))
                {
                    var duplicate = tv;
                    findings.Add(new ValidationFinding
                    {
                        Severity = FindingSeverity.Error,
                        InstrumentName = inst.Name,
                        Title = $"CC {tv.MidiCC} is mapped twice",
                        Detail = $"Slots {first.SlotIndex} ({DescribeLabel(first)}) and {tv.SlotIndex} ({DescribeLabel(tv)}) both claim it - " +
                                 "the Cirklon will show two controls fighting over one parameter.",
                        FixLabel = $"Keep slot {first.SlotIndex}, clear slot {tv.SlotIndex}",
                        Fix = () => duplicate.Type = TrackValueType.Empty,
                    });
                }
                else
                {
                    seenCc[tv.MidiCC] = tv;
                }

                CheckLabel(inst, tv.Label, $"CC {tv.MidiCC}", newLabel => tv.Label = newLabel, findings);
            }
        }

        private static void CheckCcDefs(InstrumentDef inst, List<ValidationFinding> findings)
        {
            foreach (var ccDef in inst.CcDefs.Values)
            {
                if (ccDef.MinValue > ccDef.MaxValue)
                {
                    var def = ccDef;
                    findings.Add(new ValidationFinding
                    {
                        Severity = FindingSeverity.Error,
                        InstrumentName = inst.Name,
                        Title = $"CC {ccDef.CcNum} range is inverted ({ccDef.MinValue}–{ccDef.MaxValue})",
                        Detail = "Minimum must not exceed maximum.",
                        FixLabel = "Swap min and max",
                        Fix = () => { var min = def.MinValue; def.SetMinValue(def.MaxValue); def.SetMaxValue(min); },
                    });
                }
                else if (ccDef.StartValue < ccDef.MinValue || ccDef.StartValue > ccDef.MaxValue)
                {
                    var def = ccDef;
                    var clamped = Math.Min(Math.Max(def.StartValue, def.MinValue), def.MaxValue);
                    findings.Add(new ValidationFinding
                    {
                        Severity = FindingSeverity.Error,
                        InstrumentName = inst.Name,
                        Title = $"Start value {ccDef.StartValue} is outside range {ccDef.MinValue}–{ccDef.MaxValue} on CC {ccDef.CcNum}",
                        Detail = "The hardware clamps silently.",
                        FixLabel = $"Set start to {clamped}",
                        Fix = () => def.SetStartValue(clamped),
                    });
                }

                if (string.IsNullOrEmpty(ccDef.Label))
                {
                    findings.Add(new ValidationFinding
                    {
                        Severity = FindingSeverity.Warning,
                        InstrumentName = inst.Name,
                        Title = $"CC {ccDef.CcNum} has no label",
                        Detail = $"It will show as “cc# {ccDef.CcNum}” on the hardware.",
                    });
                }
            }
        }

        private static void CheckNoteRows(InstrumentDef inst, List<ValidationFinding> findings)
        {
            foreach (var row in inst.NoteRowDefs.Values)
            {
                if (row.Note.Id < 0 || row.Note.Id > MAX_NOTE_ID)
                {
                    findings.Add(new ValidationFinding
                    {
                        Severity = FindingSeverity.Error,
                        InstrumentName = inst.Name,
                        Title = $"Note row {row.Note.Name} is outside C0–G10",
                        Detail = "The Cirklon's note range ends at G10 (“G X”). Remove or re-pitch this row.",
                    });
                }
            }
        }

        private static void CheckLabel(InstrumentDef inst, string label, string owner,
            Action<string> setLabel, List<ValidationFinding> findings)
        {
            if (string.IsNullOrEmpty(label))
                return;

            if (label.Length > MAX_LABEL)
            {
                var suggestion = LabelAbbreviator.Suggest(label);
                findings.Add(new ValidationFinding
                {
                    Severity = FindingSeverity.Warning,
                    InstrumentName = inst.Name,
                    Title = $"“{label}” won't fit ({owner})",
                    Detail = $"Labels are {MAX_LABEL} characters on the hardware - this ships as “{label.Substring(0, MAX_LABEL)}”.",
                    FixLabel = $"Use “{suggestion}”",
                    Fix = () => setLabel(suggestion),
                });
            }
            else if (!LegalLabel.IsMatch(label))
            {
                var cleaned = new string(label.Where(c => LegalLabel.IsMatch(c.ToString())).ToArray());
                findings.Add(new ValidationFinding
                {
                    Severity = FindingSeverity.Warning,
                    InstrumentName = inst.Name,
                    Title = $"“{label}” has characters the Cirklon can't show ({owner})",
                    Detail = "The hardware character set is letters, digits and -()#.$@!&~%/+",
                    FixLabel = $"Use “{cleaned}”",
                    Fix = () => setLabel(cleaned),
                });
            }
        }

        private static void CheckSharedRouting(List<InstrumentDef> instruments, List<ValidationFinding> findings)
        {
            var byRoute = instruments
                .GroupBy(i => (i.MidiPort, i.MidiChannel))
                .Where(g => g.Count() > 1);

            foreach (var group in byRoute)
            {
                var names = string.Join("”, “", group.Select(i => i.Name));
                findings.Add(new ValidationFinding
                {
                    Severity = FindingSeverity.Info,
                    InstrumentName = group.First().Name,
                    Title = $"Port {group.Key.MidiPort} · ch {group.Key.MidiChannel} is shared",
                    Detail = $"“{names}” all send here. Fine if intentional - consider Multi-timbral if it's one box.",
                });
            }
        }

        private static void CheckDuplicateNames(List<InstrumentDef> instruments, List<ValidationFinding> findings)
        {
            var dupes = instruments
                .Where(i => !string.IsNullOrEmpty(i.Name))
                .GroupBy(i => i.Name)
                .Where(g => g.Count() > 1);

            foreach (var group in dupes)
            {
                findings.Add(new ValidationFinding
                {
                    Severity = FindingSeverity.Error,
                    InstrumentName = group.Key,
                    Title = $"Two instruments are named “{group.Key}”",
                    Detail = "The Cirklon looks instruments up by name - the second one would replace the first on load.",
                });
            }
        }

        private static string DescribeLabel(TrackValueDef tv)
        {
            return string.IsNullOrEmpty(tv.Label) ? "unlabeled" : "“" + tv.Label + "”";
        }
    }
}
