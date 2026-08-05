using Senice.Core.Emoji;
using Senice.Core.Models;

namespace Senice.Core.Tests;

public class EmojiScannerTests
{
    private readonly EmojiScanner _scanner = new();

    private List<TextRange> Scan(string source) => _scanner.ScanInRange(source, new TextRange(0, source.Length)).ToList();

    [Fact]
    public void SingleEmoji_IsDetected()
    {
        const string source = "😀";
        Assert.Single(Scan(source));
    }

    [Fact]
    public void EmojiWithSkinTone_IsOneCluster()
    {
        const string source = "👍🏽";
        Assert.Single(Scan(source));
    }

    [Fact]
    public void ZwjFamily_IsOneCluster()
    {
        const string source = "👨‍👩‍👧";
        Assert.Single(Scan(source));
    }

    [Fact]
    public void EmojiBetweenText_IsDetectedAtCorrectOffset()
    {
        const string source = "A😀B";
        var ranges = Scan(source);

        Assert.Single(ranges);
        Assert.Equal(1, ranges[0].Start);
        Assert.Equal(3, ranges[0].End);
    }

    [Fact]
    public void Ascii_IsNotDetected()
    {
        Assert.Empty(Scan("plain text 123"));
    }

    [Fact]
    public void SymbolsAndArrows_AreDetected()
    {
        Assert.Single(Scan("©"));
        Assert.Single(Scan("❤"));
        Assert.Single(Scan("✔"));
    }

    [Fact]
    public void MultipleEmojis_AllDetected()
    {
        const string source = "a😀b🚀c";
        Assert.Equal(2, Scan(source).Count);
    }

    [Fact]
    public void RangeLimits_Respected()
    {
        const string source = "😀😀";
        var ranges = _scanner.ScanInRange(source, new TextRange(0, 2)).ToList();

        Assert.Single(ranges);
    }
}
