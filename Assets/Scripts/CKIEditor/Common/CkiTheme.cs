using UnityEngine;

namespace CKIEditor
{
    /// <summary>
    /// Design tokens from the CKI Editor design study - studio-console dark theme.
    /// Powder-coat graphite grounds, silkscreen ink, one LED-amber accent.
    /// </summary>
    public static class CkiTheme
    {
        public static readonly Color Ground = Hex("141619");
        public static readonly Color Panel = Hex("1C1F24");
        public static readonly Color Panel2 = Hex("23272D");
        public static readonly Color Well = Hex("101214");
        public static readonly Color Line = Hex("2E333A");

        public static readonly Color Ink = Hex("E9E7E2");
        public static readonly Color Dim = Hex("A3A8AE");
        public static readonly Color Faint = Hex("6E747C");

        public static readonly Color Accent = Hex("F0A63C");
        public static readonly Color AccentInk = Hex("1A1204");

        public static readonly Color Ok = Hex("63C68F");
        public static readonly Color Error = Hex("E4685A");
        public static readonly Color Warning = Hex("D9B84A");
        public static readonly Color Info = Hex("7FB2D9");

        public static readonly Color Overlay = new Color(0f, 0f, 0f, 0.62f);

        public static Color Soft(Color color, float alpha = 0.14f)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        private static Color Hex(string hex)
        {
            var r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            var g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            var b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color32(r, g, b, 255);
        }
    }
}
