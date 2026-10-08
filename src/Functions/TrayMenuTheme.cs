using System;
using System.Drawing;

namespace WindowsAutoPowerManager.Functions
{
    /// <summary>
    ///     Colour tokens of the tray menu, copied from the web view's Style.css so the native
    ///     menu reads as part of the same product. Shared by the renderer and the quick action
    ///     strip so the two cannot drift apart.
    /// </summary>
    internal sealed class TrayMenuTheme
    {
        public bool IsDark { get; private set; }
        public Color Panel { get; private set; }        // --bg-panel
        public Color Border { get; private set; }       // --border-input
        public Color Text { get; private set; }         // --text-primary
        public Color Muted { get; private set; }        // --text-muted
        public Color Chip { get; private set; }         // --bg-btn-secondary
        public Color ChipHover { get; private set; }    // --bg-btn-secondary-hover
        public Color Accent { get; private set; }       // --accent-blue
        public Color Danger { get; private set; }       // --color-danger-text
        public Color DangerHover { get; private set; }  // --color-danger-bg-hover, flattened
        public Color Success { get; private set; }      // --color-success

        private static readonly TrayMenuTheme Dark = new TrayMenuTheme
        {
            IsDark = true,
            Panel = Color.FromArgb(30, 31, 54),
            Border = Color.FromArgb(58, 59, 85),
            Text = Color.FromArgb(224, 224, 239),
            Muted = Color.FromArgb(154, 154, 184),
            Chip = Color.FromArgb(45, 46, 72),
            ChipHover = Color.FromArgb(58, 59, 85),
            Accent = Color.FromArgb(91, 154, 255),
            Danger = Color.FromArgb(255, 107, 122),
            DangerHover = Color.FromArgb(53, 35, 57),
            Success = Color.FromArgb(46, 204, 113)
        };

        private static readonly TrayMenuTheme Light = new TrayMenuTheme
        {
            IsDark = false,
            Panel = Color.FromArgb(255, 255, 255),
            Border = Color.FromArgb(222, 226, 230),
            Text = Color.FromArgb(33, 37, 41),
            Muted = Color.FromArgb(108, 117, 125),
            Chip = Color.FromArgb(233, 236, 239),
            ChipHover = Color.FromArgb(222, 226, 230),
            Accent = Color.FromArgb(59, 130, 246),
            Danger = Color.FromArgb(220, 38, 38),
            DangerHover = Color.FromArgb(252, 238, 238),
            Success = Color.FromArgb(22, 163, 74)
        };

        public static TrayMenuTheme For(bool isDark)
        {
            return isDark ? Dark : Light;
        }
    }

    /// <summary>
    ///     Icon glyphs of the tray menu. Windows 11 ships Segoe Fluent Icons; Windows 10 only
    ///     has the older Segoe MDL2 Assets, which carries the same code points, so the menu
    ///     looks native on both without shipping a font.
    /// </summary>
    internal static class TrayMenuGlyphs
    {
        public const string Add = "";
        public const string Pause = "";
        public const string Play = "";
        public const string Settings = "";
        public const string List = "";
        public const string Help = "";
        public const string Info = "";
        public const string Power = "";

        private static readonly string[] Candidates = { "Segoe Fluent Icons", "Segoe MDL2 Assets" };
        private static string _familyName;

        public static string FamilyName
        {
            get
            {
                if (_familyName == null)
                {
                    _familyName = Resolve();
                }
                return _familyName;
            }
        }

        public static Font Create(float pointSize)
        {
            return new Font(FamilyName, pointSize, FontStyle.Regular, GraphicsUnit.Point);
        }

        private static string Resolve()
        {
            foreach (string name in Candidates)
            {
                // GDI+ silently substitutes a default family when the requested one is missing;
                // Name then reports the substitute while OriginalFontName keeps the request.
                using (var probe = new Font(name, 10f))
                {
                    if (string.Equals(probe.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return name;
                    }
                }
            }
            return Candidates[Candidates.Length - 1];
        }
    }
}
