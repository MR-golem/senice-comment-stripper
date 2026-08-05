using Senice.Core.Models;

namespace Senice.Core.Emoji;

public sealed class EmojiScanner
{
    private static readonly (int Start, int End)[] Ranges =
    [
        (0x00A9, 0x00A9), (0x00AE, 0x00AE),
        (0x203C, 0x2049), (0x2122, 0x2139),
        (0x2194, 0x2199), (0x21A9, 0x21AA),
        (0x231A, 0x2328), (0x23CF, 0x23CF), (0x23E9, 0x23F3), (0x23F8, 0x23FA),
        (0x24C2, 0x24C2),
        (0x25AA, 0x25AB), (0x25B6, 0x25C0), (0x25FB, 0x25FE),
        (0x2600, 0x2604), (0x260E, 0x2618), (0x261D, 0x2623), (0x2626, 0x2626),
        (0x262A, 0x263A), (0x2640, 0x2640), (0x2642, 0x2642), (0x2648, 0x2653),
        (0x265F, 0x2668), (0x267B, 0x267B), (0x267E, 0x267F),
        (0x2692, 0x269C), (0x26A0, 0x26B1), (0x26BD, 0x26CF), (0x26D1, 0x26D4),
        (0x26E9, 0x26FA), (0x26FD, 0x26FD),
        (0x2702, 0x2705), (0x2708, 0x2712), (0x2714, 0x2728), (0x2733, 0x274E),
        (0x2753, 0x2764), (0x2795, 0x27B0), (0x27BF, 0x27BF),
        (0x2934, 0x2935),
        (0x2B05, 0x2B07), (0x2B1B, 0x2B1C), (0x2B50, 0x2B55),
        (0x3030, 0x3030), (0x303D, 0x303D), (0x3297, 0x3299),
        (0x1F000, 0x1F0FF), (0x1F10D, 0x1F10F), (0x1F12F, 0x1F12F),
        (0x1F16C, 0x1F171), (0x1F17E, 0x1F17F), (0x1F18E, 0x1F18E),
        (0x1F191, 0x1F19A), (0x1F1AD, 0x1F1FF),
        (0x1F201, 0x1F202), (0x1F21A, 0x1F21A), (0x1F22F, 0x1F23A),
        (0x1F250, 0x1F251), (0x1F260, 0x1F265),
        (0x1F300, 0x1F64F), (0x1F680, 0x1F6FF), (0x1F700, 0x1F773),
        (0x1F780, 0x1F7D8), (0x1F7E0, 0x1F7EB), (0x1F800, 0x1F80B),
        (0x1F810, 0x1F847), (0x1F850, 0x1F859), (0x1F860, 0x1F887),
        (0x1F890, 0x1F8AD), (0x1F900, 0x1F9FF), (0x1FA60, 0x1FA6D),
        (0x1FA70, 0x1FA74), (0x1FA78, 0x1FA7A), (0x1FA80, 0x1FA86),
        (0x1FA90, 0x1FAA8), (0x1FAB0, 0x1FAB6), (0x1FAC0, 0x1FAC2),
        (0x1FAD0, 0x1FAD6), (0x1FAE0, 0x1FAE7), (0x1FAF0, 0x1FAF6),
    ];

    public IReadOnlyList<TextRange> ScanInRange(string source, TextRange range)
    {
        var result = new List<TextRange>();
        int limit = Math.Min(range.End, source.Length);
        int i = range.Start;

        while (i < limit)
        {
            if (IsEmojiStart(source, i))
            {
                int start = i;
                i = ConsumeCluster(source, i, limit);
                result.Add(new TextRange(start, i));
            }
            else
            {
                i = NextIndex(source, i);
            }
        }

        return result;
    }

    private static int ConsumeCluster(string source, int i, int limit)
    {
        while (i < limit)
        {
            int codePoint = CodePointAt(source, i);
            if (codePoint is 0xFE0F or 0xFE0E or 0x20E3 or 0x200D)
            {
                i = NextIndex(source, i);
                continue;
            }

            if (IsEmojiStart(source, i))
            {
                i = NextIndex(source, i);
                continue;
            }

            break;
        }

        return i;
    }

    private static bool IsEmojiStart(string source, int index)
    {
        int codePoint = CodePointAt(source, index);
        foreach (var range in Ranges)
        {
            if (codePoint < range.Start)
                return false;
            if (codePoint <= range.End)
                return true;
        }

        return false;
    }

    private static int CodePointAt(string source, int index) =>
        char.IsHighSurrogate(source[index]) && index + 1 < source.Length && char.IsLowSurrogate(source[index + 1])
            ? char.ConvertToUtf32(source[index], source[index + 1])
            : source[index];

    private static int NextIndex(string source, int index) =>
        char.IsHighSurrogate(source[index]) && index + 1 < source.Length && char.IsLowSurrogate(source[index + 1])
            ? index + 2
            : index + 1;
}
