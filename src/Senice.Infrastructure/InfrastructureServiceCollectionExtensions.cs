using Microsoft.Extensions.DependencyInjection;
using Senice.Core.Abstractions;
using Senice.Core.Diffing;
using Senice.Core.Encodings;
using Senice.Core.Languages;
using Senice.Core.Processing;
using Senice.Core.Scanning;
using Senice.Infrastructure.Backup;
using Senice.Infrastructure.Files;
using Senice.Infrastructure.Scanning;

namespace Senice.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddSeniceInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<WindowsFileSystem>();
        services.AddSingleton<IFileSystem>(provider => provider.GetRequiredService<WindowsFileSystem>());

        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<IEncodingService, EncodingService>();
        services.AddSingleton<IDiffEngine, MyersDiffEngine>();
        services.AddSingleton<LanguageCatalog>();
        services.AddSingleton<ILanguageResolver, LanguageResolver>();

        services.AddSingleton<TreeSitterLanguageCatalog>();
        services.AddSingleton<LexicalCommentLocator>();
        services.AddSingleton<TreeSitterCommentLocator>();
        services.AddSingleton<ICommentLocator>(provider => new CommentLocatorSelector(
        [
            provider.GetRequiredService<TreeSitterCommentLocator>(),
            provider.GetRequiredService<LexicalCommentLocator>(),
        ]));

        services.AddSingleton<FileProcessor>();
        services.AddSingleton<ProcessingService>();

        return services;
    }
}
