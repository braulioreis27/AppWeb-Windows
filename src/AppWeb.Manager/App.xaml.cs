using AppWeb.Core.Models;
using AppWeb.Core.Services;
using AppWeb.Manager.Services;
using AppWeb.Manager.Views;
using Microsoft.UI.Xaml;

namespace AppWeb.Manager;

public partial class App : Application
{
    private AppServices? _services;
    private Window? _window;
    private Mutex? _webAppMutex;
    private EventWaitHandle? _activationEvent;
    private CancellationTokenSource? _activationCancellation;

    public static Window? MainWindow { get; private set; }

    public App()
    {
        InitializeComponent();
        UnhandledException += (_, args) =>
        {
            WindowInterop.ShowError(args.Exception.ToString());
            args.Handled = true;
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var commandLine = Environment.GetCommandLineArgs();
        if (TryGetWebAppId(commandLine, out var webAppId))
        {
            _ = LaunchWebAppAsync(webAppId);
            return;
        }

        _ = LaunchManagerAsync();
    }

    private async Task LaunchManagerAsync()
    {
        try
        {
            WindowInterop.SetAppUserModelId(AppIdentity.ManagerAumid);
            _services = AppServices.Create();
            _window = new MainWindow(_services);
            MainWindow = _window;
            _window.Closed += (_, _) =>
            {
                if (ReferenceEquals(MainWindow, _window))
                {
                    MainWindow = null;
                }

                _services?.Dispose();
            };
            _window.Activate();
        }
        catch (Exception exception)
        {
            WindowInterop.ShowError(exception.Message);
            Exit();
        }

        await Task.CompletedTask;
    }

    private async Task LaunchWebAppAsync(Guid id)
    {
        try
        {
            _services = AppServices.Create();
            var app = (await _services.WebApps.GetAllAsync()).FirstOrDefault(candidate => candidate.Id == id);
            if (app is null)
            {
                WindowInterop.ShowError("Este Web App não existe mais.");
                Exit();
                return;
            }

            var browser = _services.Browsers.GetAvailable().FirstOrDefault(candidate =>
                string.Equals(candidate.Id, app.BrowserId, StringComparison.OrdinalIgnoreCase));

            if (browser is null || !browser.IsAvailable)
            {
                WindowInterop.ShowError("O navegador configurado para este Web App não está disponível.");
                Exit();
                return;
            }

            if (browser.Kind != BrowserKind.WebView2)
            {
                await _services.Launcher.LaunchAsync(app);
                _services.Dispose();
                Exit();
                return;
            }

            if (!TryAcquireSingleInstance(app.Id))
            {
                _services.Dispose();
                Exit();
                return;
            }

            WindowInterop.SetAppUserModelId(AppIdentity.GetWebAppAumid(app.Id));
            _window = new WebAppWindow(_services, app);
            _window.Closed += (_, _) =>
            {
                _window = null;
                StopSingleInstance();
                _services?.Dispose();
            };
            _window.Activate();
        }
        catch (Exception exception)
        {
            WindowInterop.ShowError(exception.ToString());
            Exit();
        }
    }

    private bool TryAcquireSingleInstance(Guid id)
    {
        var mutexName = $@"Local\BraulioReis.AppWeb.Host.{id:N}";
        var eventName = $@"Local\BraulioReis.AppWeb.Activate.{id:N}";

        _webAppMutex = new Mutex(initiallyOwned: true, mutexName, out var createdNew);
        _activationEvent = new EventWaitHandle(false, EventResetMode.AutoReset, eventName);

        if (!createdNew)
        {
            _activationEvent.Set();
            _activationEvent.Dispose();
            _activationEvent = null;
            _webAppMutex.Dispose();
            _webAppMutex = null;
            return false;
        }

        _activationCancellation = new CancellationTokenSource();
        var activationEvent = _activationEvent;
        var cancellationHandle = _activationCancellation.Token.WaitHandle;

        _ = Task.Run(() => WaitForActivation(activationEvent, cancellationHandle));
        return true;
    }

    private void WaitForActivation(EventWaitHandle activationEvent, WaitHandle cancellationHandle)
    {
        WaitHandle[] handles = [activationEvent, cancellationHandle];

        while (WaitHandle.WaitAny(handles) == 0)
        {
            _window?.DispatcherQueue.TryEnqueue(() =>
            {
                if (_window is null)
                {
                    return;
                }

                _window.Activate();
                WindowInterop.BringToForeground(_window);
            });
        }
    }

    private void StopSingleInstance()
    {
        _activationCancellation?.Cancel();
        _activationEvent?.Set();

        if (_webAppMutex is null)
        {
            return;
        }

        try
        {
            _webAppMutex.ReleaseMutex();
        }
        catch (Exception exception) when (
            exception is ApplicationException or
            ObjectDisposedException or
            SynchronizationLockException)
        {
        }

        _webAppMutex = null;
    }

    private static bool TryGetWebAppId(string[] commandLine, out Guid id)
    {
        id = Guid.Empty;

        for (var index = 0; index < commandLine.Length - 1; index++)
        {
            if (string.Equals(commandLine[index], "--webapp", StringComparison.OrdinalIgnoreCase) &&
                Guid.TryParse(commandLine[index + 1], out id))
            {
                return true;
            }
        }

        return false;
    }
}
