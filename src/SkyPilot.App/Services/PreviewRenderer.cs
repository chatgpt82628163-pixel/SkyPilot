using System.IO;
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

        RenderMainWindow(outputFolder, settings);
        RenderConnectWindow(outputFolder, settings);
        RenderSettingsWindow(outputFolder, settings);
    }

    // -----------------------------------------------------------------------

    private static void RenderMainWindow(string folder, AppSettings settings)
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
        var white = new SolidColorBrush(Color.FromRgb(0xE6, 0xE9, 0xED));
        var gray  = new SolidColorBrush(Color.FromRgb(0x8B, 0x95, 0xA1));
        green.Freeze(); white.Freeze(); gray.Freeze();
        vm.RadioTab.Lines.Add(new ChatLine("13:41:22", "Server", "Connected. Welcome!", green));
        vm.RadioTab.Lines.Add(new ChatLine("13:42:01", "UWWW_APP [119.400]", "AFL123, radar identified, climb FL350, direct GIMLI", white));
        vm.RadioTab.Lines.Add(new ChatLine("13:42:15", "AFL123", "Climbing FL350, direct GIMLI, AFL123", gray));

        // Private message tab
        var priv = vm.GetPrivateTab("SUP1");
        priv.Lines.Add(new ChatLine("13:43:00", "SUP1", "Hello, need any help?", new SolidColorBrush(Color.FromRgb(0x3F, 0xA9, 0xF5))));

        // ATC list
        vm.Controllers.Add(new AtcRow("UWWW_APP", "119.400", "Ufa Approach", 119400));
        vm.Controllers.Add(new AtcRow("UWWW_TWR", "118.100", "Ufa Tower", 118100));
        vm.Controllers.Add(new AtcRow("UWWW_ATIS", "126.950", "Ufa ATIS", 126950, IsAtis: true, Letter: "C"));

        var win = new MainWindow(vm);
        Save(win, 1060, 500, System.IO.Path.Combine(folder, "main-connected-1x.png"), 1);
        Save(win, 1060, 500, System.IO.Path.Combine(folder, "main-connected-2x.png"), 2);
    }

    private static void RenderConnectWindow(string folder, AppSettings settings)
    {
        var win = new ConnectWindow(settings);
        Save(win, 360, 320, System.IO.Path.Combine(folder, "connect-dialog-1x.png"), 1);
    }

    private static void RenderSettingsWindow(string folder, AppSettings settings)
    {
        var win = new SettingsWindow(settings);
        Save(win, 720, 600, System.IO.Path.Combine(folder, "settings-1x.png"), 1);
    }

    // -----------------------------------------------------------------------

    private static void Save(Window win, double w, double h, string path, int scale)
    {
        win.Width = w;
        win.Height = h;
        win.WindowStyle = WindowStyle.None;
        win.ShowInTaskbar = false;
        win.AllowsTransparency = false;

        win.Measure(new Size(w, h));
        win.Arrange(new Rect(0, 0, w, h));
        win.UpdateLayout();

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
    }
}
