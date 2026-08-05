using System.Runtime.InteropServices;
using TreeSitter;

namespace Senice.Infrastructure.Scanning;

public sealed class TreeSitterLanguageCatalog
{
    private readonly Dictionary<string, Language> _languages = new(StringComparer.Ordinal);

    public TreeSitterLanguageCatalog()
    {
        string? nativeDirectory = FindNativeDirectory();
        if (nativeDirectory is null)
            return;

        foreach (var library in Directory.EnumerateFiles(nativeDirectory, "*.dll"))
        {
            string fileName = Path.GetFileNameWithoutExtension(library);
            if (fileName == "tree-sitter")
                continue;

            if (!fileName.StartsWith("tree-sitter-", StringComparison.OrdinalIgnoreCase))
                continue;

            string grammar = fileName["tree-sitter-".Length..];
            string function = "tree_sitter_" + grammar.Replace('-', '_');

            try
            {
                var language = new Language(library, function);
                _languages[grammar] = language;
            }
            catch (Exception)
            {
                // Grammar unavailable or ABI-incompatible; it will be skipped.
            }
        }
    }

    public IReadOnlyDictionary<string, Language> Languages => _languages;

    public bool TryGet(string grammarName, out Language language)
        => _languages.TryGetValue(grammarName, out language!);

    public bool IsCommentNode(string nodeType)
    {
        if (nodeType == "comment" || nodeType == "line_comment" || nodeType == "block_comment")
            return true;

        return nodeType.Contains("comment", StringComparison.OrdinalIgnoreCase)
            || nodeType.EndsWith("_comment", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsStringNode(string nodeType)
    {
        return nodeType is "string" or "string_literal" or "template_string" or "regex"
            or "regex_literal" or "character" or "char" or "raw_string_literal"
            or "interpolated_string" or "verbatim_string_literal" or "text"
            or "attribute_value" or "node_content"
            || nodeType.EndsWith("string", StringComparison.OrdinalIgnoreCase);
    }

    private static string? FindNativeDirectory()
    {
        string? baseDirectory = AppContext.BaseDirectory;
        string rid = BuildRid();
        string candidate = Path.Combine(baseDirectory, "runtimes", rid, "native");
        if (Directory.Exists(candidate))
            return candidate;

        string runtimesRoot = Path.Combine(baseDirectory, "runtimes");
        if (!Directory.Exists(runtimesRoot))
            return null;

        foreach (var directory in Directory.EnumerateDirectories(runtimesRoot, rid + "*"))
        {
            string native = Path.Combine(directory, "native");
            if (Directory.Exists(native))
                return native;
        }

        foreach (var directory in Directory.EnumerateDirectories(runtimesRoot))
        {
            string native = Path.Combine(directory, "native");
            if (Directory.Exists(native) && Directory.EnumerateFiles(native, "tree-sitter-*.dll").Any())
                return native;
        }

        string? extractionDirectory = FindSelfExtractDirectory();
        if (extractionDirectory is not null)
            return extractionDirectory;

        return null;
    }

    private static string? FindSelfExtractDirectory()
    {
        foreach (var candidate in EnumerateSearchDirectories())
        {
            if (Directory.Exists(candidate) && Directory.EnumerateFiles(candidate, "tree-sitter-*.dll").Any())
                return candidate;
        }

        return null;
    }

    private static IEnumerable<string> EnumerateSearchDirectories()
    {
        object? data = AppContext.GetData("NATIVE_DLL_SEARCH_DIRECTORIES");

        if (data is string[] array)
            return array;

        if (data is string value)
            return value.Split(';', StringSplitOptions.RemoveEmptyEntries);

        return [];
    }

    private static string BuildRid()
    {
        string os = OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsLinux() ? "linux" : "osx";
        string arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm",
            _ => "x64",
        };

        return os + "-" + arch;
    }
}
