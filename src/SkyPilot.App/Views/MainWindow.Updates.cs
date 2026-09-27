using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using SkyPilot.Core.Web;

namespace SkyPilot.App.Views;

/// <summary>
/// Updates, like vPilot: at start a newer release on GitHub is offered; on yes its installer is downloaded, checked,
/// and run silently, SkyPilot closes, and the installer starts the new version.
/// </summary>
public partial class MainWindow
{
    /// <summary>For the installer download, which takes longer than an API call.</summary>
    private static readonly HttpClient UpdateHttp = new() { Timeout = TimeSpan.FromMinutes(10) };

    private async void CheckForUpdatesAtStart()
    {
        if (!_settings.CheckForUpdates) return;
        try
        {
            // Let the window and the simulator connection come up first.
            await Task.Delay(TimeSpan.FromSeconds(3));
            await CheckForUpdatesAsync();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException or UnauthorizedAccessException
                                       or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            Error("Update check failed: " + ex.Message);
        }
    }

    private async Task CheckForUpdatesAsync()
    {
        var current = typeof(MainWindow).Assembly.GetName().Version ?? new Version(0, 0, 0);
        var checker = new UpdateChecker(UpdateHttp);
        var release = await checker.CheckAsync(current, _settings.UpdateInstalledTag);
        if (release == null) return;
        string version = release.Version.ToString(3);
        if (release.SetupUrl == null)
        {
            Info($"SkyPilot {version} is available: {release.Page}");
            return;
        }
        string connected = _session.IsConnected ? "\n\nYou are connected to the network: the connection will be closed." : "";
        var answer = MessageBox.Show(this,
            $"SkyPilot {version} is available (you have {UpdateChecker.Normalize(current).ToString(3)}).\n\n" +
            $"Install it now? SkyPilot will close, the update installs by itself and SkyPilot starts again.{connected}",
            "SkyPilot update", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes)
        {
            Info($"SkyPilot {version} is waiting: {release.Page}. It will be offered again at the next start.");
            return;
        }
        await InstallUpdateAsync(checker, release);
    }

    /// <summary>Downloads the installer and runs it silently; the installer starts the new version itself.</summary>
    private async Task InstallUpdateAsync(UpdateChecker checker, ReleaseInfo release)
    {
        string version = release.Version.ToString(3);
        string directory = Path.Combine(Path.GetTempPath(), "SkyPilot-update");
        Info($"Downloading SkyPilot {version}…");
        string setup;
        try
        {
            int lastQuarter = -1;
            var progress = new Progress<double>(p =>
            {
                int quarter = (int)(p * 4);
                if (quarter <= lastQuarter || quarter >= 4) return;
                lastQuarter = quarter;
                if (quarter > 0) Info($"Downloading the update: {quarter * 25} %");
            });
            setup = await checker.DownloadSetupAsync(release, directory, progress);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException or UnauthorizedAccessException)
        {
            Error("Could not download the update: " + ex.Message);
            Info($"You can get it from {release.Page}");
            return;
        }
        Process.Start(new ProcessStartInfo(setup, UpdateChecker.SilentArguments) { UseShellExecute = true });
        // Remembered, so a release whose installer carries an older version number is not offered again once installed.
        _settings.UpdateInstalledTag = release.Tag;
        // A normal exit: the settings are saved and the network connection signs off; the installer waits for it.
        Close();
        Application.Current.Shutdown();
    }
}
