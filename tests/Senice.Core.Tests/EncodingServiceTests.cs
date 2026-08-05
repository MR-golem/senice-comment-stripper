using System.Text;
using Senice.Core.Encodings;

namespace Senice.Core.Tests;

public class EncodingServiceTests
{
    private readonly EncodingService _service = new();

    private static byte[] B(params byte[] bytes) => bytes;

    [Fact]
    public void Decode_Utf8Bom_StripsBom()
    {
        var data = B(0xEF, 0xBB, 0xBF).Concat(Encoding.UTF8.GetBytes("hello")).ToArray();
        var result = _service.Decode(data);

        Assert.Equal("hello", result.Text);
        Assert.True(result.Encoding.HasBom);
    }

    [Fact]
    public void Decode_Utf8NoBom_PreservesText()
    {
        var data = Encoding.UTF8.GetBytes("hello");
        var result = _service.Decode(data);

        Assert.Equal("hello", result.Text);
        Assert.False(result.Encoding.HasBom);
    }

    [Fact]
    public void Decode_Utf16LeBom_DetectsEndianness()
    {
        var data = new byte[] { 0xFF, 0xFE }.Concat(Encoding.Unicode.GetBytes("hello")).ToArray();
        var result = _service.Decode(data);

        Assert.Equal("hello", result.Text);
        Assert.True(result.Encoding.HasBom);
    }

    [Fact]
    public void Decode_Utf16BeBom_DetectsEndianness()
    {
        var data = new byte[] { 0xFE, 0xFF }.Concat(Encoding.BigEndianUnicode.GetBytes("hello")).ToArray();
        var result = _service.Decode(data);

        Assert.Equal("hello", result.Text);
    }

    [Fact]
    public void Decode_Utf32Bom_Detects()
    {
        var data = new byte[] { 0xFF, 0xFE, 0x00, 0x00 }.Concat(Encoding.UTF32.GetBytes("hello")).ToArray();
        var result = _service.Decode(data);

        Assert.Equal("hello", result.Text);
    }

    [Fact]
    public void Decode_InvalidUtf8_FallsBackToCodePage()
    {
        var data = B(0xE9); // invalid as a standalone UTF-8 lead byte
        var result = _service.Decode(data);

        Assert.Equal("\u00E9", result.Text);
    }

    [Fact]
    public void RoundTrip_Utf8Bom_IsByteIdentical()
    {
        var original = B(0xEF, 0xBB, 0xBF).Concat(Encoding.UTF8.GetBytes("int x = 1; // note")).ToArray();
        var decoded = _service.Decode(original);
        var reencoded = _service.Encode(decoded.Text, decoded.Encoding);

        Assert.Equal(original, reencoded);
    }

    [Fact]
    public void RoundTrip_Utf16LeBom_IsByteIdentical()
    {
        var original = new byte[] { 0xFF, 0xFE }.Concat(Encoding.Unicode.GetBytes("int x = 1; // note")).ToArray();
        var decoded = _service.Decode(original);
        var reencoded = _service.Encode(decoded.Text, decoded.Encoding);

        Assert.Equal(original, reencoded);
    }

    [Fact]
    public void RoundTrip_Utf16BeBom_IsByteIdentical()
    {
        var original = new byte[] { 0xFE, 0xFF }.Concat(Encoding.BigEndianUnicode.GetBytes("int x = 1; // note")).ToArray();
        var decoded = _service.Decode(original);
        var reencoded = _service.Encode(decoded.Text, decoded.Encoding);

        Assert.Equal(original, reencoded);
    }

    [Fact]
    public void RoundTrip_Utf8NoBom_IsByteIdentical()
    {
        var original = Encoding.UTF8.GetBytes("int x = 1; // note\n");
        var decoded = _service.Decode(original);
        var reencoded = _service.Encode(decoded.Text, decoded.Encoding);

        Assert.Equal(original, reencoded);
    }

    [Fact]
    public void RoundTrip_Utf32LeBom_IsByteIdentical()
    {
        var original = new byte[] { 0xFF, 0xFE, 0x00, 0x00 }.Concat(Encoding.UTF32.GetBytes("int x = 1; // note")).ToArray();
        var decoded = _service.Decode(original);
        var reencoded = _service.Encode(decoded.Text, decoded.Encoding);

        Assert.Equal(original, reencoded);
    }
}
