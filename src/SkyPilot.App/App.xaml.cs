using System.Windows;
using System.Windows.Threading;
using SkyPilot.App.Services;
using SkyPilot.App.Views;

namespace SkyPilot.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // --render-previews <folder>: render each window to PNG and exit without touching user data.
        var args = e.Args;
        int idx = Array.IndexOf(args, "--render-previews");
        if (idx >= 0 && idx + 1 < args.Length)
        {
            // Prevent WPF from shutting down when each preview window closes.
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            // Swallow async WPF-dispatcher errors so partial PNG output is still uploaded.
            DispatcherUnhandledException += (_, ex) =>
            {
                Console.Error.WriteLine("[preview] dispatcher: " + ex.Exception.Message);
                ex.Handled = true;
            };
            string folder = args[idx + 1];
            try
            {
                PreviewRenderer.Run(folder);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("preview-render failed: " + ex);
                Shutdown(1);
                return;
            }
            Shutdown(0);
            return;
        }

        DispatcherUnhandledException += OnUnhandled;
        var mainWindow = new MainWindow();
        mainWindow.Show();
    }

    private static void OnUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(e.Exception.Message, "SkyPilot", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
