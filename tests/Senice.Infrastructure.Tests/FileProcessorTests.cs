using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Senice.Core.Abstractions;
using Senice.Core.Encodings;
using Senice.Core.Languages;
using Senice.Core.Models;
using Senice.Core.Processing;
using Senice.Core.Scanning;
using Senice.Infrastructure.Backup;
using Senice.Infrastructure.Files;
using Senice.Infrastructure.Scanning;

namespace Senice.Infrastructure.Tests;

public class FileProcessorTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly FileProcessor _processor;
    private readonly IFileSystem _fileSystem;

    public FileProcessorTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "Senice.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);

        _fileSystem = new WindowsFileSystem(NullLogger<WindowsFileSystem>.Instance);
        var logger = NullLogger<FileProcessor>.Instance;

        _processor = new FileProcessor(
            new LanguageResolver(new LanguageCatalog()),
            new TreeSitterCommentLocator(new TreeSitterLanguageCatalog(), new LexicalCommentLocator(), NullLogger<TreeSitterCommentLocator>.Instance),
            new EncodingService(),
            _fileSystem,
            new BackupService(_fileSystem),
            logger);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, recursive: true);
    }

    private string WriteFile(string fileName, string content, Encoding? encoding = null, bool bom = false)
    {
        string path = Path.Combine(_tempDirectory, fileName);
        encoding ??= new UTF8Encoding(bom);
        var bytes = encoding.GetPreamble().Concat(encoding.GetBytes(content)).ToArray();
        File.WriteAllBytes(path, bytes);
        return path;
    }

    [Fact]
    public async Task ProcessAsync_RemovesComments()
    {
        string path = WriteFile("test.cs", "// header\nclass A { int x; } // tail\n");
        var options = new ProcessingOptions { BackupBeforeWrite = false };

        var result = await _processor.ProcessAsync(path, options, CancellationToken.None);

        Assert.Equal(FileStatus.Changed, result.Status);
        Assert.Equal("C#", result.Language);
        Assert.Equal(2, result.CommentsRemoved);
        Assert.DoesNotContain("//", await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task ProcessAsync_UnchangedFile_NotRewritten()
    {
        string path = WriteFile("test.cs", "class A { int x = 1; }\n");
        var before = File.GetLastWriteTimeUtc(path);

        var result = await _processor.ProcessAsync(path, new ProcessingOptions { BackupBeforeWrite = false }, CancellationToken.None);

        Assert.Equal(FileStatus.Unchanged, result.Status);
        Assert.Equal(0, result.CommentsRemoved);
    }

    [Fact]
    public async Task ProcessAsync_BackupBeforeWrite_CreatesBackup()
    {
        string path = WriteFile("test.cs", "// c\nclass A {}\n");
        var options = new ProcessingOptions { BackupBeforeWrite = true };

        var result = await _processor.ProcessAsync(path, options, CancellationToken.None);

        Assert.Equal(FileStatus.Changed, result.Status);
        Assert.True(File.Exists(path + ".senice.bak"));
        Assert.Equal(path + ".senice.bak", result.BackupPath);
    }

    [Fact]
    public async Task ProcessAsync_PreservesEncodingAndBom()
    {
        string path = WriteFile("test.cs", "// c\r\nclass A {}\r\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        var result = await _processor.ProcessAsync(path, new ProcessingOptions { BackupBeforeWrite = false }, CancellationToken.None);

        Assert.Equal(FileStatus.Changed, result.Status);
        var bytes = File.ReadAllBytes(path);
        Assert.Equal(0xEF, bytes[0]);
        Assert.Equal(0xBB, bytes[1]);
        Assert.Equal(0xBF, bytes[2]);
        var text = File.ReadAllText(path, new UTF8Encoding(true));
        Assert.Equal("\r\nclass A {}\r\n", text);
    }

    [Fact]
    public async Task ProcessAsync_PreservesCrlf()
    {
        string path = WriteFile("test.cs", "// c\r\nclass A {}\r\n");
        var result = await _processor.ProcessAsync(path, new ProcessingOptions { BackupBeforeWrite = false }, CancellationToken.None);

        Assert.Equal(FileStatus.Changed, result.Status);
        var text = await File.ReadAllTextAsync(path);
        Assert.Contains("\r\n", text);
        Assert.DoesNotContain("\n", text.Replace("\r\n", ""));
    }

    [Fact]
    public async Task ProcessAsync_PreservesShebang()
    {
        string path = WriteFile("script.py", "#!/usr/bin/env python3\n# comment\nprint(1)\n");

        var result = await _processor.ProcessAsync(path, new ProcessingOptions { BackupBeforeWrite = false }, CancellationToken.None);

        Assert.Equal(FileStatus.Changed, result.Status);
        var text = await File.ReadAllTextAsync(path);
        Assert.StartsWith("#!/usr/bin/env python3", text);
        Assert.DoesNotContain("# comment", text);
    }

    [Fact]
    public async Task ProcessAsync_EmojisInComments_Removed()
    {
        string path = WriteFile("test.cs", "// note \uD83D\uDE00 here\nclass A {}\n");
        var options = new ProcessingOptions { BackupBeforeWrite = false, RemoveComments = false, RemoveEmojisInComments = true };

        var result = await _processor.ProcessAsync(path, options, CancellationToken.None);

        Assert.Equal(FileStatus.Changed, result.Status);
        Assert.Equal(1, result.EmojisRemoved);
        var text = await File.ReadAllTextAsync(path);
        Assert.Contains("// note  here", text);
    }

    [Fact]
    public async Task ProcessAsync_UnrecognizedFile_Skipped()
    {
        string path = WriteFile("mystery.xyz", "// comment\n");
        var result = await _processor.ProcessAsync(path, new ProcessingOptions(), CancellationToken.None);

        Assert.Equal(FileStatus.Skipped, result.Status);
    }

    [Fact]
    public async Task ProcessAsync_EmojisInStrings_Optional()
    {
        string path = WriteFile("test.cs", "var s = \"hi \uD83D\uDE00\";\n");
        var options = new ProcessingOptions
        {
            BackupBeforeWrite = false,
            RemoveComments = false,
            RemoveEmojisInComments = false,
            RemoveEmojisInStrings = true,
        };

        var result = await _processor.ProcessAsync(path, options, CancellationToken.None);

        Assert.Equal(FileStatus.Changed, result.Status);
        Assert.Equal(1, result.EmojisRemoved);
        var text = await File.ReadAllTextAsync(path);
        Assert.Equal("var s = \"hi \";\n", text);
    }
}
