using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Senice.App.ViewModels;
using Senice.App.Views;
using Senice.Infrastructure;

namespace Senice.App;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSeniceInfrastructure();
                services.AddSingleton<MainViewModel>();
            })
            .Build();

        await _host.StartAsync();

        var mainWindow = new MainWindow(_host.Services.GetRequiredService<MainViewModel>());
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
            await _host.StopAsync(TimeSpan.FromSeconds(2));
        base.OnExit(e);
    }
}
