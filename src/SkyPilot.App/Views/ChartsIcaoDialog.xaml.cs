using System.Windows;
using System.Windows.Input;

namespace SkyPilot.App.Views;

public partial class ChartsIcaoDialog : Window
{
    public string Icao { get; private set; } = "";

    public ChartsIcaoDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => IcaoBox.Focus();
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        Icao = IcaoBox.Text.Trim().ToUpperInvariant();
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) OnOk(sender, e);
        else if (e.Key == Key.Escape) OnCancel(sender, e);
    }
}
