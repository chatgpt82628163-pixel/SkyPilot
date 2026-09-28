using System.Windows;
using SkyPilot.Core.Session;
using SkyPilot.Core.Settings;

namespace SkyPilot.App.Views;

public partial class ConnectWindow : Window
{
    private readonly AppSettings _settings;

    public ConnectWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        CallsignBox.Text = settings.LastCallsign;
        TypeBox.Text = settings.LastTypeCode;
        Loaded += (_, _) => CallsignBox.Focus();
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        string callsign = CallsignBox.Text.Trim().ToUpperInvariant();
        string type = TypeBox.Text.Trim().ToUpperInvariant();
        if (!NetworkSession.IsValidCallsign(callsign))
        {
            ErrorText.Text = "Callsign: 2–12 Latin letters and digits";
            return;
        }
        if (type.Length is < 2 or > 4)
        {
            ErrorText.Text = "Enter the ICAO aircraft type code (e.g. A20N)";
            return;
        }
        _settings.LastCallsign = callsign;
        _settings.LastTypeCode = type;
        DialogResult = true;
    }
}
