using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using HubLink.Configuration;
using HubLink.Services;
using HubLink.ViewModels;
using HubLink.Views;
using Microsoft.Extensions.DependencyInjection;

namespace HubLink;

public partial class App : Application
{
    private ServiceProvider? _provider;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var fileSave = new AvaloniaFileSaveService();

        var services = new ServiceCollection();
        services.AddSingleton(ServiceSettingsLoader.Load());
        services.AddSingleton(new HttpClient { Timeout = Timeout.InfiniteTimeSpan });   // timeout handled per request in ShipmentApiClient
        services.AddSingleton<IShipmentApiClient, ShipmentApiClient>();
        services.AddSingleton<ICsvExportService, CsvExportService>();
        services.AddSingleton<IFileSaveService>(fileSave);
        services.AddSingleton<MainWindowViewModel>();
        _provider = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = _provider.GetRequiredService<MainWindowViewModel>();
            var window = new MainWindow { DataContext = vm };
            fileSave.Owner = window;
            desktop.MainWindow = window;
            _ = vm.InitializeAsync();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
