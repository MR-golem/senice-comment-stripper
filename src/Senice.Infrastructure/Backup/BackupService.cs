using Microsoft.Extensions.Logging;
using Senice.Core.Abstractions;

namespace Senice.Infrastructure.Backup;

public sealed class BackupService : IBackupService
{
    private readonly IFileSystem _fs;

    public BackupService(IFileSystem fs)
    {
        _fs = fs;
    }

    public async Task<string> BackupAsync(string sourcePath, CancellationToken cancellationToken)
    {
        string? directory = Path.GetDirectoryName(sourcePath);
        string fileName = Path.GetFileName(sourcePath);

        string sideBySide = Path.Combine(directory ?? string.Empty, fileName + ".senice.bak");
        await _fs.CopyFileAsync(sourcePath, sideBySide, cancellationToken);

        string recoverable = Path.Combine(
            Path.GetTempPath(),
            "Senice",
            "Backups",
            Guid.NewGuid().ToString("N"),
            fileName);
        await _fs.CopyFileAsync(sourcePath, recoverable, cancellationToken);

        return sideBySide;
    }
}
