// Auto-generated — do not edit. Add keys in Strings.resx / Strings.ru.resx.
#nullable enable
using System.Globalization;
using System.Resources;

namespace SkyPilot.App.Resources;

internal static class Strings
{
    private static ResourceManager? _rm;
    private static ResourceManager Rm =>
        _rm ??= new ResourceManager("SkyPilot.App.Resources.Strings", typeof(Strings).Assembly);

    private static string Get(string key) =>
        Rm.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    public static string ConnectButton     => Get(nameof(ConnectButton));
    public static string DisconnectButton  => Get(nameof(DisconnectButton));
    public static string ConnectingButton  => Get(nameof(ConnectingButton));
    public static string FlightPlanFiled   => Get(nameof(FlightPlanFiled));
    public static string FlightPlanNone    => Get(nameof(FlightPlanNone));
    public static string AtcPanel          => Get(nameof(AtcPanel));
    public static string SendButton        => Get(nameof(SendButton));
    public static string InputHint         => Get(nameof(InputHint));
    public static string HelpHint          => Get(nameof(HelpHint));
    public static string StatusDisconnected => Get(nameof(StatusDisconnected));
    public static string StatusConnecting  => Get(nameof(StatusConnecting));
    public static string StatusConnected   => Get(nameof(StatusConnected));
    public static string SimWaiting        => Get(nameof(SimWaiting));
    public static string SimConnected      => Get(nameof(SimConnected));
    public static string PttNotAssigned    => Get(nameof(PttNotAssigned));
    public static string RegisterLink      => Get(nameof(RegisterLink));
    public static string PttButton         => Get(nameof(PttButton));
    public static string SettingsTitle     => Get(nameof(SettingsTitle));
    public static string ConnectWindowTitle => Get(nameof(ConnectWindowTitle));
    public static string CallsignLabel     => Get(nameof(CallsignLabel));
    public static string AircraftTypeLabel => Get(nameof(AircraftTypeLabel));
    public static string ServerLabel       => Get(nameof(ServerLabel));
    public static string ConnectOkButton   => Get(nameof(ConnectOkButton));
    public static string CancelButton      => Get(nameof(CancelButton));
    public static string CallsignError     => Get(nameof(CallsignError));
    public static string TypeError         => Get(nameof(TypeError));
    public static string SaveButton        => Get(nameof(SaveButton));
    public static string RefreshButton     => Get(nameof(RefreshButton));
    public static string SimbriefButton    => Get(nameof(SimbriefButton));
    public static string AtcToggle         => Get(nameof(AtcToggle));
    public static string NetworkCaption    => Get(nameof(NetworkCaption));
    public static string FirstRunSubtitle  => Get(nameof(FirstRunSubtitle));
    public static string Step1Header       => Get(nameof(Step1Header));
    public static string Step2Header       => Get(nameof(Step2Header));
    public static string Step3Header       => Get(nameof(Step3Header));
    public static string CidLabel          => Get(nameof(CidLabel));
    public static string PasswordLabel     => Get(nameof(PasswordLabel));
    public static string DisplayNameLabel  => Get(nameof(DisplayNameLabel));
    public static string HomeAirportLabel  => Get(nameof(HomeAirportLabel));
    public static string MicLabel          => Get(nameof(MicLabel));
    public static string SpeakersLabel     => Get(nameof(SpeakersLabel));
    public static string PttLabel          => Get(nameof(PttLabel));
    public static string AssignButton      => Get(nameof(AssignButton));
    public static string SimLabel          => Get(nameof(SimLabel));
    public static string AutoDetectButton  => Get(nameof(AutoDetectButton));
    public static string SaveContinueButton => Get(nameof(SaveContinueButton));
    public static string SectionAccount    => Get(nameof(SectionAccount));
    public static string SectionSimulator  => Get(nameof(SectionSimulator));
    public static string SectionAudio      => Get(nameof(SectionAudio));
    public static string SectionRadio      => Get(nameof(SectionRadio));
    public static string SectionApplication => Get(nameof(SectionApplication));
    public static string SimbriefUsernameLabel => Get(nameof(SimbriefUsernameLabel));
    public static string P3dDllLabel       => Get(nameof(P3dDllLabel));
    public static string MicGainLabel      => Get(nameof(MicGainLabel));
    public static string RxVolumeLabel     => Get(nameof(RxVolumeLabel));
    public static string RadioNoiseLabel   => Get(nameof(RadioNoiseLabel));
    public static string VoicePortLabel    => Get(nameof(VoicePortLabel));
    public static string SoundLabel        => Get(nameof(SoundLabel));
    public static string AlwaysOnTopLabel  => Get(nameof(AlwaysOnTopLabel));
    public static string UpdatesLabel      => Get(nameof(UpdatesLabel));
    public static string LanguageLabel     => Get(nameof(LanguageLabel));
    public static string AdvancedLabel     => Get(nameof(AdvancedLabel));
    public static string ServerAddressLabel => Get(nameof(ServerAddressLabel));
    public static string WebsiteLabel      => Get(nameof(WebsiteLabel));
    public static string PinButton         => Get(nameof(PinButton));
    public static string AlwaysOnTopTip    => Get(nameof(AlwaysOnTopTip));
}
