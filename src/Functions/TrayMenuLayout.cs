using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WindowsAutoPowerManager.Functions
{
    /// <summary>
    ///     Gives every tray menu row the same explicit size. ToolStripDropDownMenu sizes rows
    ///     from their text alone, which would leave the quick action strip wider than the rows
    ///     beneath it; fixing the sizes here keeps the hover highlight edge to edge.
    /// </summary>
    internal static class TrayMenuLayout
    {
        public const int MinWidth = 224;
        public const int RowHeight = 30;
        public const int SeparatorHeight = 13;
        public const int IconColumn = 36;
        public const int TextRightPadding = 12;
        public const int CornerRadius = 8;

        public static void Apply(ToolStrip menu)
        {
            float dpi = menu.DeviceDpi;
            int width = Scale(MinWidth, dpi);

            foreach (ToolStripItem item in menu.Items)
            {
                if (item is TrayMenuItem)
                {
                    int textWidth = TextRenderer.MeasureText(item.Text ?? string.Empty, menu.Font).Width;
                    width = Math.Max(width, textWidth + Scale(IconColumn + TextRightPadding, dpi));
                }
            }

            foreach (ToolStripItem item in menu.Items)
            {
                item.AutoSize = false;
                if (item is ToolStripSeparator)
                {
                    item.Size = new Size(width, Scale(SeparatorHeight, dpi));
                }
                else if (item is ToolStripControlHost)
                {
                    item.Size = new Size(width, Scale(TrayQuickActionStrip.DefaultHeight, dpi));
                }
                else
                {
                    item.Size = new Size(width, Scale(RowHeight, dpi));
                }
            }
        }

        /// <summary>
        ///     Clips the popup to the rounded border the renderer draws, so the corners do not
        ///     show the square window underneath.
        /// </summary>
        public static void ApplyRoundedCorners(ToolStripDropDown menu)
        {
            int radius = Scale(CornerRadius, menu.DeviceDpi);
            Region previous = menu.Region;
            using (GraphicsPath path = RoundedRect(new Rectangle(0, 0, menu.Width, menu.Height), radius))
            {
                menu.Region = new Region(path);
            }
            previous?.Dispose();
        }

        public static int Scale(int logicalPixels, float dpi)
        {
            return (int)Math.Round(logicalPixels * dpi / 96f);
        }

        public static GraphicsPath RoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = Math.Max(1, radius * 2);
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
