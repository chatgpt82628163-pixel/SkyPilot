using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Threading;
using SkyNetwork.Voice;
using SkyPilot.Core.Settings;

namespace SkyPilot.App.Views;

public partial class FirstRunWindow : Window
{
    private readonly AppSettings _settings;
    private readonly ISecretProtector _protector;
    private PttBinding _ptt;
    private CancellationTokenSource? _capture;

    internal FirstRunWindow(AppSettings settings)
        : this(settings, new PlainTextProtector()) { }

    public FirstRunWindow(AppSettings settings, ISecretProtector protector)
    {
        InitializeComponent();
        _settings = settings;
        _protector = protector;
        _ptt = PttBinding.Parse(settings.PttKey);
        PttBox.Text = _ptt.Kind == PttKind.None ? "not assigned" : _ptt.Describe();

        SimulatorBox.ItemsSource = SkyPilot.Core.Simulation.SimulatorKind.All.Select(k => new { k.Id, k.Title }).ToList();
        SimulatorBox.SelectedValue = settings.Simulator;
        if (SimulatorBox.SelectedIndex < 0) SimulatorBox.SelectedIndex = 0;

        var (inputs, outputs) = VoiceClient.Devices();
        FillDevices(InputBox, inputs, settings.InputDevice);
        FillDevices(OutputBox, outputs, settings.OutputDevice);

        NameBox.Text = settings.RealName;
        HomeBox.Text = settings.HomeAirport;
        if (settings.Cid > 0) CidBox.Text = settings.Cid.ToString();

        PreviewKeyDown += (_, e) => e.Handled |= _capture != null;
        Closed += (_, _) => _capture?.Cancel();
    }

    private void OnRegisterLinkClick(object sender, RequestNavigateEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); }
        catch { /* ignore */ }
        e.Handled = true;
    }

    private static void FillDevices(ComboBox box, IReadOnlyList<string> devices, string selected)
    {
        box.Items.Add("Default");
        foreach (var d in devices) box.Items.Add(d);
        if (selected.Length > 0 && !devices.Contains(selected)) box.Items.Add(selected);
        box.SelectedIndex = selected.Length > 0 ? box.Items.IndexOf(selected) : 0;
    }

    private static string SelectedDevice(ComboBox box) =>
        box.SelectedIndex > 0 && box.SelectedItem is string name ? name : "";

    private async void OnPttCaptureClick(object sender, RoutedEventArgs e)
    {
        if (_capture != null) { _capture.Cancel(); return; }
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        _capture = cts;
        PttCaptureButton.Content = "CANCEL";
        PttBox.Text = "Press a key or joystick button…";
        try
        {
            var binding = await Task.Run(() => PushToTalk.CaptureAsync(cts.Token));
            const int escape = 0x1B;
            if (binding.Kind != PttKind.None && binding != new PttBinding(PttKind.Keyboard, escape))
                _ptt = binding;
        }
        catch (OperationCanceledException) { }
        finally
        {
            _capture = null;
            PttCaptureButton.Content = "ASSIGN";
            PttBox.Text = _ptt.Kind == PttKind.None ? "not assigned" : _ptt.Describe();
        }
    }

    private void OnPttClearClick(object sender, RoutedEventArgs e)
    {
        _capture?.Cancel();
        _ptt = PttBinding.None;
        PttBox.Text = "not assigned";
    }

    private void OnDetectSimClick(object sender, RoutedEventArgs e)
    {
        SimDetectText.Text = "Detecting…";
        // Auto-detect: just set simulator to Auto and let the hub find it.
        SimulatorBox.SelectedValue = SkyPilot.Core.Simulation.SimulatorKind.Auto;
        SimDetectText.Text = "Set to Automatic — will detect on connect.";
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(CidBox.Text.Trim(), out var cid) || cid <= 0)
        {
            ErrorText.Text = "Please enter your Pilot ID (CID)";
            return;
        }
        if (PasswordBox.Password.Length == 0)
        {
            ErrorText.Text = "Please enter your password";
            return;
        }
        _settings.Cid = cid;
        _settings.ProtectedPassword = _protector.Protect(PasswordBox.Password);
        _settings.RealName = NameBox.Text.Trim();
        _settings.HomeAirport = HomeBox.Text.Trim().ToUpperInvariant();
        _settings.InputDevice = SelectedDevice(InputBox);
        _settings.OutputDevice = SelectedDevice(OutputBox);
        _settings.PttKey = _ptt.ToString();
        _settings.Simulator = SimulatorBox.SelectedValue as string ?? SkyPilot.Core.Simulation.SimulatorKind.Auto;
        DialogResult = true;
    }
}
