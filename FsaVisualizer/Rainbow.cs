using System.Drawing;

namespace Jibini.CommonUtil.Algorithms;

public static class Rainbow
{
    /// <summary>
    /// Generates evenly distributed colors in the rainbow; every other color is
    /// optionally offset in phase to increase contrast between groups.
    /// </summary>
    public static IEnumerable<Color> GenerateNColors(int n, bool alternatePhases = false, bool washOut = false)
    {
        var period = 2.0 * Math.PI / Math.Max(1, n);
        var offset = 2.0 * Math.PI / 3;

        // Offsets phase of every other color in the rainbow progression
        double highContrastOffset(int i) => alternatePhases ? (i % 2 == 0 ? 0.0 : Math.PI) : 0;

        for (int i = 0; i < n; i++)
        {
            var r = Math.Cos(period * i + highContrastOffset(i) + 4.5);
            var g = Math.Cos(period * i + offset * 2 + highContrastOffset(i) + .5);
            var b = Math.Cos(period * i + offset + highContrastOffset(i) + .5);

            // Takes positive/negative doubles and maps them to fairly light
            // 8-bit color values
            int adapt(double amt) => (int)Math.Clamp(Math.Round(washOut ? (amt * 64 + 150) : amt * 255), 0, 255);

            yield return Color.FromArgb(255, adapt(r), adapt(g), adapt(b));
        }
    }

    /// <summary>
    /// Generates evenly distributed colors in the rainbow; every other color is
    /// optionally offset in phase to increase contrast between groups.
    /// </summary>
    public static IEnumerable<(T item, Color color)> AssignColors<T>(this IList<T> source, bool alternatePhases = false, bool washOut = false)
    {
        // Yield items so the count always matches the size of the list, even if
        // the size of the list changes after this enumeration is first created
        foreach (var itemColor in source.Zip(GenerateNColors(source.Count, alternatePhases, washOut)))
        {
            yield return itemColor;
        }
    }
}
