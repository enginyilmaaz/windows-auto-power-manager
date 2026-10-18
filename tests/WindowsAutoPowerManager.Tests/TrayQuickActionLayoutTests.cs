using System.Drawing;
using WindowsAutoPowerManager.Functions;
using Xunit;

namespace WindowsAutoPowerManager.Tests
{
    public class TrayQuickActionLayoutTests
    {
        [Fact]
        public void TileRects_FillTheWidthWithEqualTilesAndGaps()
        {
            Rectangle[] tiles = TrayQuickActionLayout.TileRects(224, 62, 7);

            Assert.Equal(3, tiles.Length);
            Assert.Equal(0, tiles[0].X);
            Assert.Equal(70, tiles[0].Width);
            Assert.Equal(77, tiles[1].X);
            Assert.Equal(70, tiles[1].Width);
            Assert.Equal(154, tiles[2].X);
            // The last tile absorbs the rounding so the strip ends exactly at its right edge.
            Assert.Equal(224, tiles[2].Right);
            Assert.All(tiles, t => Assert.Equal(62, t.Height));
        }

        [Fact]
        public void TileRects_NeverGoNegativeOnTinyWidths()
        {
            Rectangle[] tiles = TrayQuickActionLayout.TileRects(10, 62, 7);

            Assert.All(tiles, t => Assert.True(t.Width >= 0));
        }

        [Theory]
        [InlineData(10, 30, 0)]
        [InlineData(100, 5, 1)]
        [InlineData(223, 61, 2)]
        public void HitTest_FindsTheTileUnderThePoint(int x, int y, int expected)
        {
            Rectangle[] tiles = TrayQuickActionLayout.TileRects(224, 62, 7);

            Assert.Equal(expected, TrayQuickActionLayout.HitTest(tiles, new Point(x, y)));
        }

        [Theory]
        [InlineData(72, 30)]   // in the gap between the first two tiles
        [InlineData(10, 62)]   // just below the strip
        [InlineData(-1, 10)]
        public void HitTest_ReturnsMinusOneOutsideEveryTile(int x, int y)
        {
            Rectangle[] tiles = TrayQuickActionLayout.TileRects(224, 62, 7);

            Assert.Equal(-1, TrayQuickActionLayout.HitTest(tiles, new Point(x, y)));
        }

        /// <summary>
        ///     The strip used to reserve the countdown line at all times, which left a band of dead
        ///     space under the captions and read, on screen, as the menu being padded at the top.
        /// </summary>
        [Fact]
        public void PreferredHeight_OnlyReservesTheCountdownLineWhileItIsShown()
        {
            Assert.Equal(TrayQuickActionStrip.CompactHeight, TrayQuickActionLayout.PreferredHeight(false));
            Assert.Equal(TrayQuickActionStrip.DefaultHeight, TrayQuickActionLayout.PreferredHeight(true));
            Assert.True(
                TrayQuickActionStrip.CompactHeight < TrayQuickActionStrip.DefaultHeight,
                "The compact strip must be the shorter of the two.");
        }

        [Fact]
        public void ContentRows_CentreTheBlockAndLeaveEqualSpaceAboveAndBelow()
        {
            TrayQuickActionLayout.ContentRows(
                TrayQuickActionStrip.CompactHeight, 22, 14, 0, 3,
                out int glyphTop, out int captionTop, out int subTop);

            Assert.Equal(glyphTop, captionTop - 22 - 3);
            Assert.Equal(captionTop + 14, subTop);

            int below = TrayQuickActionStrip.CompactHeight - (captionTop + 14);
            Assert.InRange(below - glyphTop, -1, 1);
        }

        [Fact]
        public void ContentRows_KeepTheCaptionOnTheSameLineWithAndWithoutACountdown()
        {
            // Both states centre their own block, so the caption must not jump when the countdown
            // appears: the compact strip is exactly the countdown's height shorter.
            TrayQuickActionLayout.ContentRows(
                TrayQuickActionStrip.CompactHeight, 22, 14, 0, 3,
                out int idleGlyphTop, out int idleCaptionTop, out _);
            TrayQuickActionLayout.ContentRows(
                TrayQuickActionStrip.DefaultHeight, 22, 14, 12, 3,
                out int pausedGlyphTop, out int pausedCaptionTop, out _);

            Assert.Equal(idleGlyphTop, pausedGlyphTop);
            Assert.Equal(idleCaptionTop, pausedCaptionTop);
        }

        [Fact]
        public void ContentRows_NeverGoNegativeOnATileTooShortForItsContent()
        {
            TrayQuickActionLayout.ContentRows(10, 22, 14, 12, 3, out int glyphTop, out _, out _);

            Assert.Equal(0, glyphTop);
        }
    }
}
