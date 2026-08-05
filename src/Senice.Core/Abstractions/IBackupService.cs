namespace Senice.Core.Abstractions;

public interface IBackupService
{
    Task<string> BackupAsync(string filePath, CancellationToken cancellationToken = default);
}
