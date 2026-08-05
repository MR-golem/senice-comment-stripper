using System.Text;
using Senice.Core.Abstractions;

namespace Senice.Core.Encodings;

public sealed class EncodingService : IEncodingService
{
    private static readonly UTF8Encoding Utf8Strict = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    static EncodingService()
    {
        System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public DecodedText Decode(ReadOnlyMemory<byte> data)
    {
        var bytes = data.Span;

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return new DecodedText(Utf8Strict.GetString(bytes[3..]), Utf8(true));

        if (bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00)
            return new DecodedText(System.Text.Encoding.UTF32.GetString(bytes[4..]), Utf32Le(true));

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return new DecodedText(System.Text.Encoding.Unicode.GetString(bytes[2..]), Utf16Le(true));

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return new DecodedText(System.Text.Encoding.BigEndianUnicode.GetString(bytes[2..]), Utf16Be(true));

        if (bytes.Length >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF)
            return new DecodedText(Utf32BeNoBom.GetString(bytes[4..]), Utf32Be(true));

        try
        {
            return new DecodedText(Utf8Strict.GetString(bytes), Utf8(false));
        }
        catch (DecoderFallbackException)
        {
            // Not valid UTF-8; fall through to other candidates.
        }

        if (LooksLikeUtf16(bytes))
        {
            try
            {
                return new DecodedText(System.Text.Encoding.Unicode.GetString(bytes), Utf16Le(false));
            }
            catch (DecoderFallbackException)
            {
                // Odd byte count or bad surrogate pairs; keep trying.
            }
        }

        var legacy = System.Text.Encoding.GetEncoding(1252);
        return new DecodedText(legacy.GetString(bytes), new TextEncoding(legacy, false));
    }

    public byte[] Encode(string text, TextEncoding encoding)
    {
        var body = encoding.Encoding.GetBytes(text);
        if (!encoding.HasBom)
            return body;

        var preamble = encoding.Encoding.GetPreamble();
        if (preamble.Length == 0)
            return body;

        var result = new byte[preamble.Length + body.Length];
        preamble.CopyTo(result, 0);
        body.CopyTo(result, preamble.Length);
        return result;
    }

    private static TextEncoding Utf8(bool bom) => new(new UTF8Encoding(bom), bom);

    private static TextEncoding Utf16Le(bool bom) => new(new UnicodeEncoding(bigEndian: false, byteOrderMark: bom), bom);

    private static TextEncoding Utf16Be(bool bom) => new(new UnicodeEncoding(bigEndian: true, byteOrderMark: bom), bom);

    private static TextEncoding Utf32Le(bool bom) => new(new UTF32Encoding(bigEndian: false, byteOrderMark: bom), bom);

    private static TextEncoding Utf32Be(bool bom) => new(new UTF32Encoding(bigEndian: true, byteOrderMark: bom), bom);

    private static UTF32Encoding Utf32BeNoBom => new(bigEndian: true, byteOrderMark: false);

    private static bool LooksLikeUtf16(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 4)
            return false;

        int limit = Math.Min(bytes.Length, 8192);
        int evenZeros = 0;
        int oddZeros = 0;

        for (int i = 0; i < limit; i++)
        {
            if (bytes[i] != 0)
                continue;
            if ((i & 1) == 0)
                evenZeros++;
            else
                oddZeros++;
        }

        return evenZeros > oddZeros * 3 && evenZeros > limit / 8;
    }
}
