namespace MosaicShell.Core.Services.Kde
{
    /// <summary>
    /// The value of <c>_KDE_NET_WM_BLUR_BEHIND_REGION</c> for a rounded card: CARDINAL quads of x, y, width, height in
    /// window pixels, which KWin's blur effect unites into the area it blurs. The Win32 round region does the same job
    /// on Windows. Kept pure so the shape is tested on every OS.
    /// </summary>
    /// <remarks>
    /// A pixel is in the region when its centre lies inside the rounded rectangle. Each corner band takes one
    /// rectangle per run of rows with the same inset, and the straight middle takes one. KWin reads an empty property
    /// as "blur the whole window", so an empty result means "write nothing" (delete the property), never "write empty".
    /// </remarks>
    public static class KWinBlurRegion
    {
        public static uint[] FromRoundedRect(int width, int height, int radius)
        {
            if (width <= 0 || height <= 0)
            {
                return [];
            }

            int r = Math.Clamp(radius, 0, Math.Min(width, height) / 2);
            if (r == 0)
            {
                return [0u, 0u, (uint)width, (uint)height];
            }

            List<uint> top = [];
            List<uint> bottom = [];
            int runStart = 0;
            int runInset = InsetForRow(0, r);
            for (int y = 1; y <= r; y++)
            {
                int inset = y < r ? InsetForRow(y, r) : -1;
                if (inset == runInset)
                {
                    continue;
                }

                AddRun(top, bottom, runStart, y - runStart, runInset, width, height);
                runStart = y;
                runInset = inset;
            }

            List<uint> region = [.. top];
            int middle = height - (2 * r);
            if (middle > 0)
            {
                region.AddRange([0u, (uint)r, (uint)width, (uint)middle]);
            }

            region.AddRange(bottom);
            return [.. region];
        }

        /// <summary>
        /// The first column whose pixel centre is inside the corner circle, for a row in the top corner band. The
        /// circle's centre is at (r, r); the row's centre is at y + 0.5.
        /// </summary>
        private static int InsetForRow(int y, int r)
        {
            double dy = r - (y + 0.5);
            double half = Math.Sqrt(Math.Max(0, ((double)r * r) - (dy * dy)));
            return Math.Max(0, (int)Math.Ceiling(r - 0.5 - half));
        }

        private static void AddRun(List<uint> top, List<uint> bottom, int y, int rows, int inset, int width, int height)
        {
            int runWidth = width - (2 * inset);
            if (rows <= 0 || runWidth <= 0)
            {
                return;
            }

            top.AddRange([(uint)inset, (uint)y, (uint)runWidth, (uint)rows]);

            // The bottom band mirrors the top: rows counted up from the last row. Inserted at the front so the bottom
            // band reads top to bottom.
            bottom.InsertRange(0, [(uint)inset, (uint)(height - y - rows), (uint)runWidth, (uint)rows]);
        }
    }
}
