namespace Senice.Core.Abstractions;

public sealed record TextEncoding(System.Text.Encoding Encoding, bool HasBom)
{
    public string DisplayName => Encoding.WebName + (HasBom ? " (BOM)" : string.Empty);
}

public sealed record DecodedText(string Text, TextEncoding Encoding);

public interface IEncodingService
{
    DecodedText Decode(ReadOnlyMemory<byte> data);

    byte[] Encode(string text, TextEncoding encoding);
}
