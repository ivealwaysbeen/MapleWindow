using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace MapleWindow.App;

public partial class App : System.Windows.Application
{
    private IHost? _host;
    private Mutex? _singleInstanceMutex;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _singleInstanceMutex = new Mutex(initiallyOwned: true, name: "Global\\MapleWindow-SingleInstance", createdNew: out var createdNew);
        if (!createdNew)
        {
            Shutdown();
            return;
        }

        _host = Host.CreateApplicationBuilder().ConfigureMapleWindowServices().Build();

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        await _host.StartAsync();

        var bootstrapper = _host.Services.GetRequiredService<AppBootstrapper>();
        await bootstrapper.RunAsync();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unhandled UI-thread exception");
        e.Handled = true; // keep the tray-resident app alive after logging
    }

    private static void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Log.Error(e.ExceptionObject as Exception, "Unhandled AppDomain exception (IsTerminating={IsTerminating})", e.IsTerminating);
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unobserved task exception");
        e.SetObserved();
    }
}
