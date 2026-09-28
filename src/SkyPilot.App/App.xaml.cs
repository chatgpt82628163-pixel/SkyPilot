using System.Windows;
using System.Windows.Threading;
using SkyPilot.App.Services;
using SkyPilot.App.Views;

namespace SkyPilot.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnUnhandled;
        base.OnStartup(e);

        // --render-previews <folder>: render each window to PNG and exit without touching user data.
        var args = e.Args;
        int idx = Array.IndexOf(args, "--render-previews");
        if (idx >= 0 && idx + 1 < args.Length)
        {
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

        var mainWindow = new MainWindow();
        mainWindow.Show();
    }

    private static void OnUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(e.Exception.Message, "SkyPilot", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
