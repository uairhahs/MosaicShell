using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using MosaicShell.Host.Tiles.Tessera;

namespace MosaicShell.Host.Tests
{
    /// <summary>
    /// Title blank after a skip, second cause (found 2026-10-04 from the A2 marquee trace): a title
    /// that scrolls hides the full-text block and shows the trimmed copy. The next title was then
    /// measured while the full-text block was still hidden, and Avalonia measures a hidden control as
    /// 0 wide, so the block got <c>Width = 0</c> and the title rendered blank. The trace showed
    /// <c>text=0 distance=0</c> on every restart that followed a scrolling title.
    /// </summary>
    public sealed class MarqueeTextTests : HeadlessHostTest
    {
        private const string LongTitle = "A title long enough that it cannot fit and has to scroll across the viewport";
        // Headless glyphs are about 15 dip wide, so this must stay well under the 120 dip cap.
        private const string ShortTitle = "Hi";

        private static (Window Window, TextBlock Title) Host(double maxWidth = 120)
        {
            TextBlock title = new() { Text = "", FontSize = 15 };
            Window window = new()
            {
                Width = 400,
                Height = 100,
                Content = new StackPanel { Children = { TesseraMarqueeText.Wrap(title, maxWidth) } },
            };
            window.Show();
            HeadlessPump.For(100);
            return (window, title);
        }

        [AvaloniaFact]
        public void A_title_after_a_scrolling_title_is_measured_at_its_real_width()
        {
            (Window window, TextBlock title) = Host();

            title.Text = LongTitle;
            HeadlessPump.For(200);
            _ = title.Width.Should().BeGreaterThan(120, "premise: the long title overflows and scrolls");

            title.Text = ShortTitle;
            HeadlessPump.For(200);

            _ = title.Width.Should().BeGreaterThan(0, "a hidden block measures as 0; the title must not collapse");
            _ = title.IsVisible.Should().BeTrue("a title that fits shows the full-text block");
            window.Close();
        }

        [AvaloniaFact]
        public void Two_scrolling_titles_in_a_row_both_measure_their_full_width()
        {
            (Window window, TextBlock title) = Host();

            title.Text = LongTitle;
            HeadlessPump.For(200);
            double first = title.Width;

            title.Text = LongTitle + " and then some more words";
            HeadlessPump.For(200);

            _ = title.Width.Should().BeGreaterThan(first);
            window.Close();
        }
    }
}
