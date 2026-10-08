using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WindowsAutoPowerManager.Functions
{
    /// <summary>
    ///     Pure geometry of the quick action strip, kept apart from the control so it can be
    ///     unit tested without a window.
    /// </summary>
    internal static class TrayQuickActionLayout
    {
        public const int TileCount = 3;

        /// <summary>Three equal tiles across the width; the last one absorbs the rounding.</summary>
        public static Rectangle[] TileRects(int width, int height, int gap)
        {
            var rects = new Rectangle[TileCount];
            int tileWidth = Math.Max(0, (width - gap * (TileCount - 1)) / TileCount);
            for (int i = 0; i < TileCount; i++)
            {
                int x = i * (tileWidth + gap);
                int w = i == TileCount - 1 ? Math.Max(0, width - x) : tileWidth;
                rects[i] = new Rectangle(x, 0, w, height);
            }
            return rects;
        }

        public static int HitTest(Rectangle[] tiles, Point point)
        {
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i].Contains(point)) return i;
            }
            return -1;
        }
    }

    /// <summary>
    ///     The three large targets at the top of the tray menu: new action, pause / resume and
    ///     settings. Owner drawn so hover, pressed, focus and the paused countdown follow the
    ///     tray menu theme instead of the system button look.
    /// </summary>
    internal sealed class TrayQuickActionStrip : Control
    {
        public const int DefaultWidth = TrayMenuLayout.MinWidth;
        public const int DefaultHeight = 62;

        private const int Gap = 7;
        private const int Radius = 7;
        private const int GlyphTop = 9;
        private const int GlyphHeight = 22;
        private const int CaptionTop = 35;
        private const int CaptionHeight = 14;
        private const int SubTop = 48;
        private const int SubHeight = 12;
        private const int ChevronSize = 10;
        private const int ChevronInset = 5;

        private const int NewActionTile = 0;
        private const int PauseTile = 1;
        private const int SettingsTile = 2;

        public event EventHandler NewActionRequested;
        public event EventHandler PauseToggleRequested;
        public event EventHandler SettingsRequested;

        private readonly Font _captionFont = new Font("Segoe UI", 8.25f, FontStyle.Regular, GraphicsUnit.Point);
        private readonly Font _subFont = new Font("Segoe UI", 7.5f, FontStyle.Regular, GraphicsUnit.Point);
        private readonly Font _glyphFont = TrayMenuGlyphs.Create(15f);
        private readonly Font _chevronFont = TrayMenuGlyphs.Create(7.5f);

        private TrayMenuTheme _theme = TrayMenuTheme.For(true);
        private readonly string[] _glyphs = { TrayMenuGlyphs.Add, TrayMenuGlyphs.Pause, TrayMenuGlyphs.Settings };
        private readonly string[] _captions = { "New Action", "Pause", "Settings" };
        private readonly string[] _subs = { null, null, null };

        private string _pauseCaption = "Pause";
        private string _resumeCaption = "Resume";
        private bool _paused;

        /// <summary>True while the pause duration list is open above the tile.</summary>
        private bool _expanded;

        private int _hover = -1;
        private int _pressed = -1;
        private int _focus = -1;

        public TrayQuickActionStrip()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            TabStop = true;
            Size = new Size(DefaultWidth, DefaultHeight);
            BackColor = _theme.Panel;
        }

        public void ApplyTheme(TrayMenuTheme theme)
        {
            _theme = theme;
            BackColor = theme.Panel;
            Invalidate();
        }

        public void SetCaptions(string newAction, string pause, string resume, string settings)
        {
            _captions[NewActionTile] = newAction;
            _captions[SettingsTile] = settings;
            _pauseCaption = pause;
            _resumeCaption = resume;
            ApplyPauseTile(_paused, _subs[PauseTile]);
        }

        /// <param name="remaining">Countdown shown under "Resume" while paused; ignored otherwise.</param>
        public void SetPauseState(bool paused, string remaining)
        {
            ApplyPauseTile(paused, remaining);
        }

        /// <summary>Keeps the pause tile highlighted while its duration list is open.</summary>
        public void SetExpanded(bool expanded)
        {
            if (_expanded == expanded) return;
            _expanded = expanded;
            Invalidate();
        }

        /// <summary>Where the duration list is anchored, in this control's coordinates.</summary>
        public Rectangle PauseTileBounds
        {
            get { return Tiles()[PauseTile]; }
        }

        private void ApplyPauseTile(bool paused, string remaining)
        {
            _paused = paused;
            _glyphs[PauseTile] = paused ? TrayMenuGlyphs.Play : TrayMenuGlyphs.Pause;
            _captions[PauseTile] = paused ? _resumeCaption : _pauseCaption;
            _subs[PauseTile] = paused ? remaining : null;
            Invalidate();
        }

        private Rectangle[] Tiles()
        {
            return TrayQuickActionLayout.TileRects(Width, Height, Scale(Gap));
        }

        private int Scale(int logicalPixels)
        {
            return TrayMenuLayout.Scale(logicalPixels, DeviceDpi);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(_theme.Panel);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle[] tiles = Tiles();
            for (int i = 0; i < tiles.Length; i++)
            {
                PaintTile(g, i, tiles[i]);
            }
        }

        private void PaintTile(Graphics g, int index, Rectangle rect)
        {
            if (rect.Width <= 0 || rect.Height <= 0) return;

            bool pauseTile = index == PauseTile;
            bool hot = index == _hover || index == _pressed || (Focused && index == _focus) || (pauseTile && _expanded);
            bool pausedTile = _paused && pauseTile;

            Rectangle fill = rect;
            if (index == _pressed)
            {
                // Pressed tiles shrink by a pixel on each side: the "97 %" of the design.
                fill.Inflate(-1, -1);
            }

            using (var brush = new SolidBrush(hot ? _theme.ChipHover : _theme.Chip))
            using (GraphicsPath path = TrayMenuLayout.RoundedRect(fill, Scale(Radius)))
            {
                g.FillPath(brush, path);
            }

            if (Focused && index == _focus)
            {
                Rectangle ring = fill;
                ring.Inflate(-1, -1);
                using (var pen = new Pen(_theme.Accent, 2f))
                using (GraphicsPath path = TrayMenuLayout.RoundedRect(ring, Scale(Radius) - 1))
                {
                    g.DrawPath(pen, path);
                }
            }

            Color glyphColor = pausedTile ? _theme.Success : hot ? _theme.Accent : _theme.Muted;
            var glyphRect = new Rectangle(fill.X, fill.Y + Scale(GlyphTop), fill.Width, Scale(GlyphHeight));
            TextRenderer.DrawText(g, _glyphs[index], _glyphFont, glyphRect, glyphColor, CenteredText);

            var captionRect = new Rectangle(fill.X, fill.Y + Scale(CaptionTop), fill.Width, Scale(CaptionHeight));
            TextRenderer.DrawText(g, _captions[index] ?? string.Empty, _captionFont, captionRect, _theme.Text,
                CenteredText | TextFormatFlags.EndEllipsis);

            string sub = _subs[index];
            if (!string.IsNullOrEmpty(sub))
            {
                var subRect = new Rectangle(fill.X, fill.Y + Scale(SubTop), fill.Width, Scale(SubHeight));
                TextRenderer.DrawText(g, sub, _subFont, subRect, pausedTile ? _theme.Success : _theme.Muted,
                    CenteredText | TextFormatFlags.EndEllipsis);
            }

            if (pauseTile && !pausedTile)
            {
                // The list opens upward, so the hint points the same way.
                int size = Scale(ChevronSize);
                int inset = Scale(ChevronInset);
                var chevronRect = new Rectangle(fill.Right - inset - size, fill.Y + inset, size, size);
                TextRenderer.DrawText(g, TrayMenuGlyphs.ChevronUp, _chevronFont, chevronRect,
                    hot ? _theme.Accent : _theme.Muted, CenteredText);
            }
        }

        private const TextFormatFlags CenteredText =
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;

        // ---- mouse ----

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int index = TrayQuickActionLayout.HitTest(Tiles(), e.Location);
            if (index != _hover)
            {
                _hover = index;
                Cursor = index >= 0 ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hover != -1 || _pressed != -1)
            {
                _hover = -1;
                _pressed = -1;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            _pressed = TrayQuickActionLayout.HitTest(Tiles(), e.Location);
            if (_pressed >= 0)
            {
                _focus = _pressed;
                Focus();
            }
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) return;
            int released = TrayQuickActionLayout.HitTest(Tiles(), e.Location);
            int pressed = _pressed;
            _pressed = -1;
            Invalidate();
            if (pressed >= 0 && pressed == released)
            {
                Raise(pressed);
            }
        }

        // ---- keyboard ----

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData & Keys.KeyCode)
            {
                case Keys.Left:
                case Keys.Right:
                case Keys.Enter:
                case Keys.Space:
                    return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            switch (e.KeyCode)
            {
                case Keys.Left:
                    _focus = (_focus + TrayQuickActionLayout.TileCount - 1) % TrayQuickActionLayout.TileCount;
                    e.Handled = true;
                    Invalidate();
                    break;
                case Keys.Right:
                    _focus = (_focus + 1) % TrayQuickActionLayout.TileCount;
                    e.Handled = true;
                    Invalidate();
                    break;
                case Keys.Enter:
                case Keys.Space:
                    if (_focus >= 0)
                    {
                        e.Handled = true;
                        Raise(_focus);
                    }
                    break;
            }
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            if (_focus < 0) _focus = 0;
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        private void Raise(int index)
        {
            switch (index)
            {
                case NewActionTile:
                    NewActionRequested?.Invoke(this, EventArgs.Empty);
                    break;
                case PauseTile:
                    PauseToggleRequested?.Invoke(this, EventArgs.Empty);
                    break;
                case SettingsTile:
                    SettingsRequested?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _captionFont.Dispose();
                _subFont.Dispose();
                _glyphFont.Dispose();
                _chevronFont.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>Places the strip inside the tray ContextMenuStrip as its first item.</summary>
    internal sealed class TrayQuickActionHost : ToolStripControlHost
    {
        public TrayQuickActionHost()
            : base(new TrayQuickActionStrip())
        {
            AutoSize = false;
            Margin = Padding.Empty;
            Padding = Padding.Empty;
            Size = new Size(TrayQuickActionStrip.DefaultWidth, TrayQuickActionStrip.DefaultHeight);
        }

        public TrayQuickActionStrip Strip
        {
            get { return (TrayQuickActionStrip)Control; }
        }
    }
}
