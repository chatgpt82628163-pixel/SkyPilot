using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SkyPilot.App.ViewModels;
using SkyPilot.App.Views;
using SkyPilot.Core.Session;
using SkyPilot.Core.Settings;

namespace SkyPilot.App.Services;

/// <summary>
/// Renders each window to PNG files for visual inspection without a running simulator or network.
/// Activated by the --render-previews &lt;folder&gt; command-line switch.
/// </summary>
internal static class PreviewRenderer
{
    public static void Run(string outputFolder)
    {
        Directory.CreateDirectory(outputFolder);

        // Build fake settings — never touches the user's settings file.
        var settings = new AppSettings
        {
            Cid = 1234567,
            RealName = "Ivan Petrov",
            HomeAirport = "UWWW",
            LastCallsign = "AFL123",
            LastTypeCode = "A20N",
        };

        // English previews
        SetCulture("en");
        TryRender(() => RenderMainWindow(outputFolder, settings, "en"),     "main-window-en");
        TryRender(() => RenderConnectWindow(outputFolder, settings, "en"),  "connect-dialog-en");
        TryRender(() => RenderSettingsWindow(outputFolder, settings, "en"), "settings-en");
        TryRender(() => RenderFirstRunWindow(outputFolder, settings, "en"), "first-run-en");

        // Russian previews
        SetCulture("ru");
        TryRender(() => RenderMainWindow(outputFolder, settings, "ru"),     "main-window-ru");
        TryRender(() => RenderConnectWindow(outputFolder, settings, "ru"),  "connect-dialog-ru");
        TryRender(() => RenderSettingsWindow(outputFolder, settings, "ru"), "settings-ru");
        TryRender(() => RenderFirstRunWindow(outputFolder, settings, "ru"), "first-run-ru");
        SetCulture("en");
    }

    private static void SetCulture(string lang)
    {
        var ci = new CultureInfo(lang);
        Thread.CurrentThread.CurrentCulture   = ci;
        Thread.CurrentThread.CurrentUICulture = ci;
        CultureInfo.DefaultThreadCurrentCulture   = ci;
        CultureInfo.DefaultThreadCurrentUICulture = ci;
    }

    private static void TryRender(Action action, string name)
    {
        try { action(); }
        catch (Exception ex) { Console.Error.WriteLine($"[preview] {name} failed: {ex.Message}"); }
    }

    // -----------------------------------------------------------------------

    private static void RenderMainWindow(string folder, AppSettings settings, string lang)
    {
        var vm = new MainViewModel();
        vm.NetConnected = true;
        vm.Callsign = "AFL123";
        vm.Com1 = "119.400";
        vm.Com2 = "121.500";
        vm.Squawk = "2547";
        vm.ModeC = true;
        vm.Com1Rx = true;
        vm.Com1Tx = true;
        vm.AtcVisible = true;

        // Chat lines
        var green = new SolidColorBrush(Color.FromRgb(0x4C, 0xC3, 0x8A));
        var white = new SolidColorBrush(Color.FromRgb(0xE6, 0xE6, 0xE6));
        var gray  = new SolidColorBrush(Color.FromRgb(0xA8, 0xA8, 0xA8));
        green.Freeze(); white.Freeze(); gray.Freeze();
        vm.RadioTab.Lines.Add(new ChatLine("13:41:22", "Server", "Connected. Welcome!", green));
        vm.RadioTab.Lines.Add(new ChatLine("13:42:01", "UWWW_APP [119.400]", "AFL123, radar identified, climb FL350, direct GIMLI", white));
        vm.RadioTab.Lines.Add(new ChatLine("13:42:15", "AFL123", "Climbing FL350, direct GIMLI, AFL123", gray));

        // Private message tab
        var blue = new SolidColorBrush(Color.FromRgb(0x6F, 0xA8, 0xFF)); blue.Freeze();
        var priv = vm.GetPrivateTab("SUP1");
        priv.Lines.Add(new ChatLine("13:43:00", "SUP1", "Hello, need any help?", blue));

        // ATC list: facility int codes — 5=APP, 4=TWR, 7=ATIS.
        // COM1 is 119.400 = 119400 kHz which matches UWWW_APP, so the station name appears under COM 1.
        vm.SetControllers(
        [
            new SkyPilot.Core.Session.AtcStation("UWWW_APP",  119400, 5, DateTime.UtcNow),
            new SkyPilot.Core.Session.AtcStation("UWWW_TWR",  118100, 4, DateTime.UtcNow),
            new SkyPilot.Core.Session.AtcStation("UWWW_ATIS", 126950, 7, DateTime.UtcNow),
        ],
        new Dictionary<string, SkyPilot.Core.Session.AtisInfo>());
        vm.SetFrequencies(119400, 121500);

        Save(new MainWindow(vm), 1060, 500,
            Path.Combine(folder, $"main-connected-{lang}.png"), 1);
    }

    private static void RenderConnectWindow(string folder, AppSettings settings, string lang)
    {
        var win = new ConnectWindow(settings);
        Save(win, 380, 310, Path.Combine(folder, $"connect-dialog-{lang}.png"), 1);
    }

    private static void RenderSettingsWindow(string folder, AppSettings settings, string lang)
    {
        var win = new SettingsWindow(settings);
        Save(win, 660, 800, Path.Combine(folder, $"settings-{lang}.png"), 1);
    }

    private static void RenderFirstRunWindow(string folder, AppSettings settings, string lang)
    {
        // Show first-run for a fresh account (Cid = 0 triggers the wizard in real usage).
        var fresh = new AppSettings { RealName = "", HomeAirport = "", Cid = 0 };
        var win = new FirstRunWindow(fresh);
        Save(win, 480, 640, Path.Combine(folder, $"first-run-{lang}.png"), 1);
    }

    // -----------------------------------------------------------------------

    private static void Save(Window win, double w, double h, string path, int scale)
    {
        win.Width = w;
        win.Height = h;
        win.WindowStyle = WindowStyle.None;
        win.ShowInTaskbar = false;
        win.AllowsTransparency = false;

        // Show the window off-screen so WPF materializes all templates.
        win.Left = -9999;
        win.Top = -9999;
        win.Show();
        win.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);

        win.Measure(new Size(w, h));
        win.Arrange(new Rect(0, 0, w, h));
        win.UpdateLayout();
        win.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);

        double dpi = 96.0 * scale;
        var rtb = new RenderTargetBitmap(
            (int)(w * scale), (int)(h * scale),
            dpi, dpi,
            PixelFormats.Pbgra32);
        rtb.Render(win);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));
        using var fs = File.OpenWrite(path);
        encoder.Save(fs);

        win.Close();
    }
}
