namespace Senice.Core.Processing;

public readonly record struct ProcessingProgress(int Processed, int Total, string CurrentFile);

public enum ProcessingEventLevel
{
    Info,
    Warning,
    Error,
}

public readonly record struct ProcessingEvent(
    string File,
    string Message,
    ProcessingEventLevel Level,
    DateTime Timestamp);
