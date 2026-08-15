using System;
using System.Linq;
using CKIEditor.Serialization;

public static class ChartTests
{
    public static void Run(Action<bool, string> check)
    {
        var one = ChartParser.Parse("19 Filter Cutoff 0-127");
        check(one.Count == 1 && one[0].CcNum == 19 && one[0].FullName == "Filter Cutoff"
              && one[0].Min == 0 && one[0].Max == 127 && one[0].Start == null,
            "chart: plain line with range");

        var colon = ChartParser.Parse("74: Cutoff");
        check(colon.Count == 1 && colon[0].CcNum == 74 && colon[0].FullName == "Cutoff",
            "chart: colon after number");

        var piped = ChartParser.Parse("CC 21 | Filter Resonance | 0–127 | 64");
        check(piped.Count == 1 && piped[0].CcNum == 21 && piped[0].FullName == "Filter Resonance"
              && piped[0].Min == 0 && piped[0].Max == 127 && piped[0].Start == 64,
            "chart: piped fields with en-dash range and start");

        var tabbed = ChartParser.Parse("18\tFilter Drive\t0-127");
        check(tabbed.Count == 1 && tabbed[0].CcNum == 18 && tabbed[0].FullName == "Filter Drive"
              && tabbed[0].Max == 127,
            "chart: tab-separated");

        var parens = ChartParser.Parse("109 Filter Pole Select (0-3) 3");
        check(parens.Count == 1 && parens[0].Min == 0 && parens[0].Max == 3 && parens[0].Start == 3
              && parens[0].FullName == "Filter Pole Select",
            "chart: parenthesised range plus start");

        var header = ChartParser.Parse("CC Parameter Range");
        check(header.Count == 0, "chart: header line skipped");

        var osc2 = ChartParser.Parse("74 Osc 2");
        check(osc2.Count == 1 && osc2[0].FullName == "Osc 2" && osc2[0].Start == null,
            "chart: trailing digit stays in the name without a range");

        check(ChartParser.Parse("999 Nope").Count == 0, "chart: CC above 127 skipped");
        check(ChartParser.Parse("").Count == 0, "chart: empty input ok");
        check(ChartParser.Parse(null).Count == 0, "chart: null input ok");

        var dots = ChartParser.Parse("cc# 5 Glide Time 0..127");
        check(dots.Count == 1 && dots[0].CcNum == 5 && dots[0].FullName == "Glide Time"
              && dots[0].Min == 0 && dots[0].Max == 127,
            "chart: cc# prefix and dotted range");

        var multi = ChartParser.Parse("19 Filter Cutoff 0-127\r\n\r\nsome prose here\n74: Cutoff\n");
        check(multi.Count == 2 && multi[0].CcNum == 19 && multi[1].CcNum == 74,
            "chart: multiline with blanks and prose");

        var to = ChartParser.Parse("7 Volume 0 to 127");
        check(to.Count == 1 && to[0].FullName == "Volume 0 to 127" || to.Count == 1,
            "chart: 'x to y' line parses without crashing");
    }
}
