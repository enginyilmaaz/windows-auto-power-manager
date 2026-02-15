using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WindowsShutdownHelper.functions
{
    public class ModernMenuRenderer : ToolStripProfessionalRenderer
    {
        private readonly Color _bgColor;
        private readonly Color _textColor;
        private readonly Color _hoverBg;
        private readonly Color _borderColor;
        private readonly Color _separatorColor;

        public ModernMenuRenderer(bool isDark)
            : base(new ModernMenuColorTable(isDark))
        {
            if (isDark)
            {
                _bgColor = Color.FromArgb(30, 31, 54);
                _textColor = Color.FromArgb(192, 192, 216);
                _hoverBg = Color.FromArgb(45, 46, 72);
                _borderColor = Color.FromArgb(58, 59, 85);
                _separatorColor = Color.FromArgb(45, 46, 72);
            }
            else
            {
                _bgColor = Color.FromArgb(255, 255, 255);
                _textColor = Color.FromArgb(33, 37, 41);
                _hoverBg = Color.FromArgb(233, 236, 239);
                _borderColor = Color.FromArgb(222, 226, 230);
                _separatorColor = Color.FromArgb(233, 236, 239);
            }
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var rect = new Rectangle(2, 0, e.Item.Size.Width - 4, e.Item.Size.Height);
            if (e.Item.Selected)
            {
                using (var brush = new SolidBrush(_hoverBg))
                {
                    var path = CreateRoundedRect(rect, 4);
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(brush, path);
                }
            }
            else
            {
                using (var brush = new SolidBrush(_bgColor))
                {
                    e.Graphics.FillRectangle(brush, rect);
                }
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = _textColor;
            e.TextFont = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            base.OnRenderItemText(e);
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (var brush = new SolidBrush(_bgColor))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (var pen = new Pen(_borderColor))
            {
                var rect = new Rectangle(0, 0, e.AffectedBounds.Width - 1, e.AffectedBounds.Height - 1);
                var path = CreateRoundedRect(rect, 6);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawPath(pen, path);
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using (var pen = new Pen(_separatorColor))
            {
                int y = e.Item.Height / 2;
                e.Graphics.DrawLine(pen, 8, y, e.Item.Width - 8, y);
            }
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            using (var brush = new SolidBrush(_bgColor))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }
        }

        private static GraphicsPath CreateRoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class ModernMenuColorTable : ProfessionalColorTable
    {
        private readonly bool _isDark;

        public ModernMenuColorTable(bool isDark)
        {
            _isDark = isDark;
            UseSystemColors = false;
        }

        public override Color MenuBorder => _isDark
            ? Color.FromArgb(58, 59, 85)
            : Color.FromArgb(222, 226, 230);

        public override Color MenuItemSelected => _isDark
            ? Color.FromArgb(45, 46, 72)
            : Color.FromArgb(233, 236, 239);

        public override Color ToolStripDropDownBackground => _isDark
            ? Color.FromArgb(30, 31, 54)
            : Color.FromArgb(255, 255, 255);

        public override Color ImageMarginGradientBegin => _isDark
            ? Color.FromArgb(30, 31, 54)
            : Color.FromArgb(255, 255, 255);

        public override Color ImageMarginGradientMiddle => _isDark
            ? Color.FromArgb(30, 31, 54)
            : Color.FromArgb(255, 255, 255);

        public override Color ImageMarginGradientEnd => _isDark
            ? Color.FromArgb(30, 31, 54)
            : Color.FromArgb(255, 255, 255);

        public override Color SeparatorDark => _isDark
            ? Color.FromArgb(45, 46, 72)
            : Color.FromArgb(233, 236, 239);

        public override Color SeparatorLight => _isDark
            ? Color.FromArgb(45, 46, 72)
            : Color.FromArgb(245, 245, 245);
    }
}
