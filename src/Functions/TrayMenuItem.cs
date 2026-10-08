using System.Windows.Forms;

namespace WindowsAutoPowerManager.Functions
{
    /// <summary>
    ///     A tray menu row that carries a Segoe icon glyph instead of a bitmap, so the renderer
    ///     can tint it with the hover and danger colours of the theme.
    /// </summary>
    internal class TrayMenuItem : ToolStripMenuItem
    {
        public string Glyph { get; set; }

        /// <summary>Drawn in the danger colour; used for the exit row.</summary>
        public bool IsDanger { get; set; }
    }
}
