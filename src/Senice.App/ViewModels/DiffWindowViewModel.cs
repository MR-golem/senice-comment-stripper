using System.Collections.ObjectModel;
using System.IO;
using Senice.Core.Abstractions;
using Senice.Core.Encodings;
using Senice.Core.Models;

namespace Senice.App.ViewModels;

public enum DiffRowKind
{
    Equal,
    Removed,
    Added,
}

public sealed record DiffRow(int? BeforeNumber, string BeforeText, int? AfterNumber, string AfterText, DiffRowKind Kind);

public sealed class DiffWindowViewModel
{
    private DiffWindowViewModel(string fileName, string language, IReadOnlyList<DiffRow> rows, int added, int removed)
    {
        FileName = fileName;
        Language = language;
        Rows = new ObservableCollection<DiffRow>(rows);
        Added = added;
        Removed = removed;
    }

    public string FileName { get; }

    public string Language { get; }

    public ObservableCollection<DiffRow> Rows { get; }

    public int Added { get; }

    public int Removed { get; }

    public static async Task<DiffWindowViewModel?> CreateAsync(
        FileProcessingResult result,
        IFileSystem fileSystem,
        IEncodingService encoding,
        IDiffEngine diffEngine)
    {
        if (string.IsNullOrEmpty(result.BackupPath) || !fileSystem.FileExists(result.BackupPath))
            return null;

        byte[] originalBytes;
        byte[] modifiedBytes;
        try
        {
            originalBytes = await fileSystem.ReadAllBytesAsync(result.BackupPath);
            modifiedBytes = await fileSystem.ReadAllBytesAsync(result.Path);
        }
        catch
        {
            return null;
        }

        var original = encoding.Decode(originalBytes).Text;
        var modified = encoding.Decode(modifiedBytes).Text;

        var originalLines = original.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var modifiedLines = modified.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        var edits = diffEngine.Compute(originalLines, modifiedLines);

        var rows = new List<DiffRow>();
        int beforeNumber = 0;
        int afterNumber = 0;
        int added = 0;
        int removed = 0;

        foreach (var edit in edits)
        {
            switch (edit.Operation)
            {
                case DiffOperation.Equal:
                    beforeNumber++;
                    afterNumber++;
                    rows.Add(new DiffRow(beforeNumber, edit.Text, afterNumber, edit.Text, DiffRowKind.Equal));
                    break;

                case DiffOperation.Delete:
                    beforeNumber++;
                    removed++;
                    rows.Add(new DiffRow(beforeNumber, edit.Text, null, string.Empty, DiffRowKind.Removed));
                    break;

                case DiffOperation.Insert:
                    afterNumber++;
                    added++;
                    rows.Add(new DiffRow(null, string.Empty, afterNumber, edit.Text, DiffRowKind.Added));
                    break;
            }
        }

        return new DiffWindowViewModel(
            Path.GetFileName(result.Path),
            result.Language ?? string.Empty,
            rows,
            added,
            removed);
    }
}
