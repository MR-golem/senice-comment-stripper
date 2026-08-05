namespace Senice.Core.Models;

public sealed class ProcessingOptions
{
    public bool RemoveComments { get; set; } = true;

    public bool RemoveEmojisInComments { get; set; } = true;

    public bool RemoveEmojisInStrings { get; set; }

    public bool PreserveShebang { get; set; } = true;

    public bool BackupBeforeWrite { get; set; } = true;

    public bool SkipUncertainFiles { get; set; } = true;

    public int? MaxDegreeOfParallelism { get; set; }

    public long MaxFileSize { get; set; } = 20 * 1024 * 1024;

    public bool MatchAnyFile { get; set; }

    public IReadOnlyList<string>? Extensions { get; set; }

    public IReadOnlyList<string>? ExcludedDirectories { get; set; }

    public bool IncludeHiddenFiles { get; set; }

    public int MaxDepth { get; set; } = 64;
}
