using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WindowsAutoPowerManager.Functions
{
    /// <summary>
    ///     Draws the tray menu in the app's own theme: panel surface, rounded border, pill
    ///     hover, Segoe icon glyphs tinted per state and a danger-coloured exit row.
    /// </summary>
    public class ModernMenuRenderer : ToolStripProfessionalRenderer
    {
        private const int HoverRadius = 5;
        private const int GlyphLeft = 10;
        private const int GlyphSize = 16;
        private const int SeparatorInset = 8;

        private const TextFormatFlags GlyphFormat =
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;

        // 12 pt is 16 px at 96 dpi; shared because renderers are recreated on every theme change.
        private static readonly Font GlyphFont = TrayMenuGlyphs.Create(12f);

        private readonly TrayMenuTheme _theme;

        public ModernMenuRenderer(bool isDark)
            : base(new ModernMenuColorTable(isDark))
        {
            _theme = TrayMenuTheme.For(isDark);
            RoundedEdges = false;
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var rect = new Rectangle(Point.Empty, e.Item.Size);
            using (var brush = new SolidBrush(_theme.Panel))
            {
                e.Graphics.FillRectangle(brush, rect);
            }

            if (!e.Item.Selected || !e.Item.Enabled) return;

            bool danger = (e.Item as TrayMenuItem)?.IsDanger == true;
            rect.Width -= 1;
            rect.Height -= 1;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var brush = new SolidBrush(danger ? _theme.DangerHover : _theme.Chip))
            using (GraphicsPath path = TrayMenuLayout.RoundedRect(rect, Scale(HoverRadius, e.Graphics)))
            {
                e.Graphics.FillPath(brush, path);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            var item = e.Item as TrayMenuItem;
            bool danger = item != null && item.IsDanger;

            if (item != null && !string.IsNullOrEmpty(item.Glyph))
            {
                Color glyphColor = danger ? _theme.Danger : e.Item.Selected ? _theme.Accent : _theme.Muted;
                var glyphRect = new Rectangle(Scale(GlyphLeft, e.Graphics), 0, Scale(GlyphSize, e.Graphics), e.Item.Height);
                TextRenderer.DrawText(e.Graphics, item.Glyph, GlyphFont, glyphRect, glyphColor, GlyphFormat);
            }

            // The menu lays text out for an image margin we hide; place it after the glyph column.
            int textLeft = Scale(TrayMenuLayout.IconColumn, e.Graphics);
            int textWidth = Math.Max(0, e.Item.Width - textLeft - Scale(TrayMenuLayout.TextRightPadding, e.Graphics));
            e.TextRectangle = new Rectangle(textLeft, 0, textWidth, e.Item.Height);
            e.TextFormat |= TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;
            e.TextColor = danger ? _theme.Danger : _theme.Text;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (var brush = new SolidBrush(_theme.Panel))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            var rect = new Rectangle(0, 0, e.AffectedBounds.Width - 1, e.AffectedBounds.Height - 1);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(_theme.Border))
            using (GraphicsPath path = TrayMenuLayout.RoundedRect(rect, Scale(TrayMenuLayout.CornerRadius, e.Graphics)))
            {
                e.Graphics.DrawPath(pen, path);
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int inset = Scale(SeparatorInset, e.Graphics);
            int y = e.Item.Height / 2;
            using (var pen = new Pen(_theme.Border))
            {
                e.Graphics.DrawLine(pen, inset, y, e.Item.Width - inset, y);
            }
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            using (var brush = new SolidBrush(_theme.Panel))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }
        }

        private static int Scale(int logicalPixels, Graphics graphics)
        {
            return TrayMenuLayout.Scale(logicalPixels, graphics.DpiX);
        }
    }

    public class ModernMenuColorTable : ProfessionalColorTable
    {
        private readonly TrayMenuTheme _theme;

        public ModernMenuColorTable(bool isDark)
        {
            _theme = TrayMenuTheme.For(isDark);
            UseSystemColors = false;
        }

        public override Color MenuBorder => _theme.Border;
        public override Color MenuItemBorder => _theme.Chip;
        public override Color MenuItemSelected => _theme.Chip;
        public override Color ToolStripDropDownBackground => _theme.Panel;
        public override Color ImageMarginGradientBegin => _theme.Panel;
        public override Color ImageMarginGradientMiddle => _theme.Panel;
        public override Color ImageMarginGradientEnd => _theme.Panel;
        public override Color SeparatorDark => _theme.Border;
        public override Color SeparatorLight => _theme.Border;
    }
}
