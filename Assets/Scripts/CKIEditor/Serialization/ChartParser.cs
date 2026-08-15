using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CKIEditor.Serialization
{
    public class ChartEntry
    {
        public int CcNum;
        public string FullName;
        public int? Min;
        public int? Max;
        public int? Start;
    }

    /// <summary>
    /// Parses MIDI implementation charts pasted straight from a synth manual into
    /// CC entries. Tolerant of the usual shapes:
    ///   19 Filter Cutoff 0-127
    ///   74: Cutoff
    ///   CC 21 | Filter Resonance | 0-127 | 64
    ///   109 Filter Pole Select (0-3) 3
    /// Lines without a leading CC number (headers, prose) are skipped.
    /// </summary>
    public static class ChartParser
    {
        private static readonly Regex RangePattern = new Regex(@"^\(?(\d+)\s*(?:-|–|—|\.\.|to)\s*(\d+)\)?$", RegexOptions.IgnoreCase);
        private static readonly Regex IntPattern = new Regex(@"^\d+$");
        private static readonly Regex LeadingCc = new Regex(@"^(?:cc#?|#)?\s*(\d+)\s*[:.\-]?$", RegexOptions.IgnoreCase);

        public static List<ChartEntry> Parse(string text)
        {
            var entries = new List<ChartEntry>();
            if (string.IsNullOrEmpty(text))
                return entries;

            foreach (var rawLine in text.Split('\n'))
            {
                var line = rawLine.Trim().TrimEnd('\r');
                if (line.Length == 0)
                    continue;

                var entry = line.IndexOfAny(new[] {'|', '\t'}) >= 0
                    ? ParseStructured(line)
                    : ParsePlain(line);

                if (entry != null && entry.CcNum >= 0 && entry.CcNum <= 127)
                    entries.Add(entry);
            }

            return entries;
        }

        //fields separated by pipes or tabs: cc | name | range | start
        private static ChartEntry ParseStructured(string line)
        {
            var fields = line.Split('|', '\t')
                .Select(f => f.Trim())
                .Where(f => f.Length > 0)
                .ToList();

            if (fields.Count < 2)
                return null;

            var ccNum = ExtractCcNumber(fields[0]);
            if (ccNum < 0)
                return null;

            var entry = new ChartEntry { CcNum = ccNum, FullName = fields[1] };

            for (var i = 2; i < fields.Count; i++)
            {
                var range = RangePattern.Match(fields[i]);
                if (range.Success)
                {
                    entry.Min = int.Parse(range.Groups[1].Value);
                    entry.Max = int.Parse(range.Groups[2].Value);
                }
                else if (IntPattern.IsMatch(fields[i]))
                {
                    entry.Start = int.Parse(fields[i]);
                }
            }

            return entry;
        }

        //whitespace-separated: [CC] <num> <name words...> [range] [start]
        private static ChartEntry ParsePlain(string line)
        {
            var tokens = line.Split(' ').Where(t => t.Length > 0).ToList();
            if (tokens.Count == 0)
                return null;

            //optional "CC" / "CC#" prefix before the number
            if (tokens.Count > 1 && Regex.IsMatch(tokens[0], @"^(?:cc#?|#)$", RegexOptions.IgnoreCase))
                tokens.RemoveAt(0);

            var ccNum = ExtractCcNumber(tokens[0]);
            if (ccNum < 0)
                return null;

            tokens.RemoveAt(0);

            var entry = new ChartEntry { CcNum = ccNum };

            //pull range (and a start value after it) off the end;
            //a bare trailing number is only a start value when a range precedes it,
            //so names like "Osc 2" survive intact
            if (tokens.Count >= 2 && IntPattern.IsMatch(tokens[tokens.Count - 1])
                                  && RangePattern.IsMatch(tokens[tokens.Count - 2]))
            {
                entry.Start = int.Parse(tokens[tokens.Count - 1]);
                ApplyRange(entry, tokens[tokens.Count - 2]);
                tokens.RemoveRange(tokens.Count - 2, 2);
            }
            else if (tokens.Count >= 1 && RangePattern.IsMatch(tokens[tokens.Count - 1]))
            {
                ApplyRange(entry, tokens[tokens.Count - 1]);
                tokens.RemoveAt(tokens.Count - 1);
            }

            entry.FullName = string.Join(" ", tokens).Trim();
            return entry;
        }

        private static void ApplyRange(ChartEntry entry, string token)
        {
            var range = RangePattern.Match(token);
            entry.Min = int.Parse(range.Groups[1].Value);
            entry.Max = int.Parse(range.Groups[2].Value);
        }

        private static int ExtractCcNumber(string token)
        {
            var match = LeadingCc.Match(token);
            if (!match.Success)
                return -1;

            return int.Parse(match.Groups[1].Value);
        }
    }
}
