using System.Collections.Generic;
using System.Text;

namespace CKIEditor.Validation
{
    /// <summary>
    /// Suggests Cirklon-sized (6 character) labels from full control names,
    /// e.g. "Filter Cutoff" -> "FltCut", "Feedback Level" -> "FdbkLv".
    /// </summary>
    public static class LabelAbbreviator
    {
        public const int MAX_LENGTH = 6;

        private static readonly Dictionary<string, string> Known = new Dictionary<string, string>
        {
            {"filter", "Flt"}, {"cutoff", "Cut"}, {"resonance", "Res"}, {"drive", "Drv"},
            {"envelope", "Env"}, {"attack", "Atk"}, {"decay", "Dec"}, {"sustain", "Sus"},
            {"release", "Rel"}, {"amount", "Amt"}, {"oscillator", "Osc"}, {"osc", "Osc"},
            {"level", "Lvl"}, {"volume", "Vol"}, {"feedback", "Fdbk"}, {"frequency", "Frq"},
            {"wave", "Wav"}, {"waveform", "Wav"}, {"noise", "Noiz"}, {"glide", "Gld"},
            {"portamento", "Port"}, {"keyboard", "Kb"}, {"tracking", "Trk"}, {"track", "Trk"},
            {"modulation", "Mod"}, {"mod", "Mod"}, {"wheel", "Whl"}, {"pitch", "Ptch"},
            {"bend", "Bnd"}, {"delay", "Dly"}, {"reverb", "Rev"}, {"depth", "Dpth"},
            {"rate", "Rate"}, {"pan", "Pan"}, {"width", "Wdth"}, {"detune", "Dtun"},
            {"sync", "Sync"}, {"octave", "Oct"}, {"velocity", "Velo"}, {"gate", "Gate"},
            {"low", "Lo"}, {"high", "Hi"}, {"select", "Sel"}, {"pole", "Pol"},
        };

        public static string Suggest(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "";

            var trimmed = name.Trim();
            if (trimmed.Length <= MAX_LENGTH)
                return trimmed;

            var words = trimmed.Split(' ', '_', '-', '/');
            var result = new StringBuilder();

            foreach (var word in words)
            {
                if (word.Length == 0)
                    continue;

                if (Known.TryGetValue(word.ToLowerInvariant(), out var mapped))
                    result.Append(mapped);
                else if (IsNumber(word))
                    result.Append(word);
                else
                    result.Append(StripVowels(word));
            }

            var suggestion = result.ToString();
            return suggestion.Length <= MAX_LENGTH ? suggestion : suggestion.Substring(0, MAX_LENGTH);
        }

        private static bool IsNumber(string word)
        {
            return int.TryParse(word, out _);
        }

        //first letter + following consonants, capped at 3 characters per word
        private static string StripVowels(string word)
        {
            var result = new StringBuilder();
            result.Append(char.ToUpperInvariant(word[0]));

            for (var i = 1; i < word.Length && result.Length < 3; i++)
            {
                if ("aeiouAEIOU".IndexOf(word[i]) < 0)
                    result.Append(word[i]);
            }

            return result.ToString();
        }
    }
}
