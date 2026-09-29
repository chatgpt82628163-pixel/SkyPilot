using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using SkyNetwork.Voice;
using SkyPilot.Core.Settings;

namespace SkyPilot.App.Views;

public partial class SettingsWindow : Window
{
    private const string DefaultDevice = "Default";

    private readonly AppSettings _settings;
    private readonly ISecretProtector _protector;
    private readonly Func<float> _micLevel;
    private readonly DispatcherTimer _meter = new() { Interval = TimeSpan.FromMilliseconds(50) };
    private PttBinding _ptt;
    private CancellationTokenSource? _capture;

    public SettingsWindow(AppSettings settings, ISecretProtector protector, Func<float> micLevel)
    {
        InitializeComponent();
        SourceInitialized += (_, _) => Services.DarkTitleBar.Apply(this);
        _settings = settings;
        _protector = protector;
        _micLevel = micLevel;
        CidBox.Text = settings.Cid > 0 ? settings.Cid.ToString() : "";
        PasswordBox.Password = protector.Unprotect(settings.ProtectedPassword);
        NameBox.Text = settings.RealName;
        SimbriefBox.Text = settings.SimbriefUser;
        SimulatorBox.ItemsSource = SkyPilot.Core.Simulation.SimulatorKind.All.Select(k => new { k.Id, k.Title }).ToList();
        SimulatorBox.SelectedValue = settings.Simulator;
        if (SimulatorBox.SelectedIndex < 0) SimulatorBox.SelectedIndex = 0;
        P3dDllBox.Text = settings.P3dSimConnectPath;
        SoundBox.IsChecked = settings.PlaySoundOnPrivateMessage;
        TopmostBox.IsChecked = settings.KeepWindowOnTop;
        UpdatesBox.IsChecked = settings.CheckForUpdates;

        var (inputs, outputs) = VoiceClient.Devices();
        FillDevices(InputBox, inputs, settings.InputDevice);
        FillDevices(OutputBox, outputs, settings.OutputDevice);
        MicGainSlider.Value = Math.Round(settings.MicGain * 100);
        VolumeSlider.Value = Math.Round(settings.OutputVolume * 100);
        RadioNoiseBox.IsChecked = settings.RadioNoise;
        OnSliderChanged(this, new RoutedPropertyChangedEventArgs<double>(0, 0));
        _ptt = PttBinding.Parse(settings.PttKey);
        PttBox.Text = Describe(_ptt);

        _meter.Tick += (_, _) => MicMeter.Value = _micLevel();
        _meter.Start();
        // While waiting for the PTT key, keys must not press buttons (Enter = save, Esc = cancel).
        PreviewKeyDown += (_, e) => e.Handled |= _capture != null;
        Closed += (_, _) =>
        {
            _meter.Stop();
            _capture?.Cancel();
        };
    }

    private void OnBrowseP3dClick(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Title = "Prepar3D SimConnect.dll", Filter = "SimConnect.dll|SimConnect.dll|DLL files (*.dll)|*.dll" };
        if (dialog.ShowDialog(this) == true) P3dDllBox.Text = dialog.FileName;
    }

    /// <summary>"Default" and the devices; a saved device that is unplugged stays in the list.</summary>
    private static void FillDevices(ComboBox box, IReadOnlyList<string> devices, string selected)
    {
        box.Items.Add(DefaultDevice);
        foreach (var d in devices) box.Items.Add(d);
        if (selected.Length > 0 && !devices.Contains(selected)) box.Items.Add(selected);
        box.SelectedIndex = selected.Length > 0 ? box.Items.IndexOf(selected) : 0;
    }

    private static string SelectedDevice(ComboBox box) =>
        box.SelectedIndex > 0 && box.SelectedItem is string name ? name : "";

    /// <summary>The PTT control in English ("not assigned", "Joystick 1, button 5", or the key name).</summary>
    private static string Describe(PttBinding b) => b.Kind switch
    {
        PttKind.None => "not assigned",
        PttKind.Joystick => $"Joystick {b.Device + 1}, button {b.Code + 1}",
        _ => b.Code switch
        {
            0x04 => "Middle mouse button",
            0x05 => "Mouse button 4",
            0x06 => "Mouse button 5",
            _ => b.Describe(),
        },
    };

    private void OnSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MicGainText != null) MicGainText.Text = $"{MicGainSlider.Value:0} %";
        if (VolumeText != null) VolumeText.Text = $"{VolumeSlider.Value:0} %";
    }

    private async void OnPttCaptureClick(object sender, RoutedEventArgs e)
    {
        if (_capture != null)
        {
            _capture.Cancel();
            return;
        }
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        _capture = cts;
        PttCaptureButton.Content = "Cancel";
        PttBox.Text = "Press a key or joystick button…";
        try
        {
            // Polled off the UI thread so the window stays responsive.
            var binding = await Task.Run(() => PushToTalk.CaptureAsync(cts.Token));
            const int escape = 0x1B;
            if (binding.Kind != PttKind.None && binding != new PttBinding(PttKind.Keyboard, escape)) _ptt = binding;
        }
        catch (OperationCanceledException)
        {
            // Cancelled, timed out or the window was closed: keep the old key.
        }
        finally
        {
            _capture = null;
            PttCaptureButton.Content = "Assign";
            PttBox.Text = Describe(_ptt);
        }
    }

    private void OnPttClearClick(object sender, RoutedEventArgs e)
    {
        _capture?.Cancel();
        _ptt = PttBinding.None;
        PttBox.Text = Describe(_ptt);
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(CidBox.Text.Trim(), out var cid) || cid <= 0)
        {
            ErrorText.Text = "CID must be a positive number";
            return;
        }
        _settings.Cid = cid;
        _settings.SimbriefUser = SimbriefBox.Text.Trim();
        _settings.ProtectedPassword = _protector.Protect(PasswordBox.Password);
        _settings.RealName = NameBox.Text.Trim();
        _settings.Simulator = SimulatorBox.SelectedValue as string ?? SkyPilot.Core.Simulation.SimulatorKind.Auto;
        _settings.P3dSimConnectPath = P3dDllBox.Text.Trim();
        _settings.PlaySoundOnPrivateMessage = SoundBox.IsChecked == true;
        _settings.KeepWindowOnTop = TopmostBox.IsChecked == true;
        _settings.CheckForUpdates = UpdatesBox.IsChecked == true;
        _settings.InputDevice = SelectedDevice(InputBox);
        _settings.OutputDevice = SelectedDevice(OutputBox);
        _settings.MicGain = MicGainSlider.Value / 100;
        _settings.OutputVolume = VolumeSlider.Value / 100;
        _settings.RadioNoise = RadioNoiseBox.IsChecked == true;
        _settings.PttKey = _ptt.ToString();
        DialogResult = true;
    }
}
