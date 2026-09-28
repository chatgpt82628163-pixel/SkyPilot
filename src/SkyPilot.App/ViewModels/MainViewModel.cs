using System.Collections.ObjectModel;
using SkyNetwork.Voice;
using SkyPilot.App.Resources;
using SkyPilot.Core.Model;
using SkyPilot.Core.Session;
using SkyPilot.Core.Web;

namespace SkyPilot.App.ViewModels;

/// <param name="Letter">Current ATIS letter of an ATIS station, once known; otherwise empty.</param>
/// <param name="AtisText">Last ATIS / controller information received from the station, or null (row tooltip).</param>
public sealed record AtcRow(string Callsign, string Frequency, string Facility, int FrequencyKhz,
    bool IsAtis = false, string Letter = "", string? AtisText = null)
{
    public bool HasLetter => Letter.Length > 0;
}

/// <summary>One row in the ATC positions panel.</summary>
public sealed record PositionRow(
    string Callsign,
    string Frequency,
    string Facility,
    PositionState State,
    string? Name,
    string? NextBookingText,
    int FrequencyKhz)
{
    /// <summary>Dot color: green = online, amber = booked, gray = free.</summary>
    public string DotColor => State switch
    {
        PositionState.Online => "#3FA34D",
        PositionState.Booked => "#E0A030",
        _                    => "#7A7A7A",
    };

    public bool IsOnline   => State == PositionState.Online;
    public bool IsBooked   => State == PositionState.Booked;
    public bool ShowName   => IsOnline && !string.IsNullOrEmpty(Name);
}

public sealed class MainViewModel : Observable
{
    private bool _simConnected;
    private string? _simError;
    private bool _netConnected;
    private bool _netConnecting;
    private string _callsign = "";
    private string _pttKey = "";
    private int _com1Khz, _com2Khz;
    private string _com1 = "---.---";
    private string _com2 = "---.---";
    private string _squawk = "----";
    private bool _modeC;
    private bool _identing;
    private bool _com1Rx = true, _com2Rx = true;
    private int _txRadio = 1;
    private int _trafficCount;
    private bool _atcVisible = true;
    private bool _topmost;
    private FlightPlan? _flightPlan;
    private string _utcTime = "";
    private ChatTab? _selectedTab;
    private List<AtcStation> _stations = [];
    private VoiceState _voiceState;
    private bool _voiceFailing;
    private bool _transmitting;
    private string _com1Heard = "", _com2Heard = "";
    private string _positionFilter = "";
    private bool _positionsOffline;
    private List<PositionRow> _allPositions = [];

    public MainViewModel()
    {
        RadioTab = new ChatTab("Radio", null);
        Tabs.Add(RadioTab);
        _selectedTab = RadioTab;
    }

    public ChatTab RadioTab { get; }
    public ObservableCollection<ChatTab> Tabs { get; } = [];
    public ObservableCollection<AtcRow> Controllers { get; } = [];
    public ObservableCollection<PositionRow> Positions { get; } = [];

    public ChatTab? SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (Set(ref _selectedTab, value) && value != null) value.Unread = false;
        }
    }

    // ---- connection -----------------------------------------------------------------

    private string _simName = "Simulator";
    public bool SimConnected { get => _simConnected; set { if (Set(ref _simConnected, value)) OnStatusChanged(); } }

    /// <summary>Why the simulator cannot be reached (e.g. SimConnect.dll missing), or null.</summary>
    public string? SimError { get => _simError; set { if (Set(ref _simError, value)) OnStatusChanged(); } }

    public bool NetConnected { get => _netConnected; set { if (Set(ref _netConnected, value)) OnStatusChanged(); } }
    public bool NetConnecting { get => _netConnecting; set { if (Set(ref _netConnecting, value)) OnStatusChanged(); } }
    public string Callsign { get => _callsign; set { if (Set(ref _callsign, value)) OnStatusChanged(); } }

    /// <summary>The connected simulator ("Prepar3D", "X-Plane 12"…).</summary>
    public string SimName { get => _simName; set { if (Set(ref _simName, value)) OnStatusChanged(); } }

    /// <summary>Short display name of the assigned PTT key, or empty.</summary>
    public string PttKey { get => _pttKey; set { if (Set(ref _pttKey, value)) OnStatusChanged(); } }

    public string SimStatus => SimConnected ? $"{SimName} connected"
        : SimError != null ? "Simulator not reachable"
        : "Waiting for the simulator…";

    public string ConnectButtonText => NetConnecting ? Strings.ConnectingButton
        : NetConnected ? Strings.DisconnectButton : Strings.ConnectButton;

    /// <summary>Status bar: network state.</summary>
    public string NetStatusText => NetConnected ? $"● {string.Format(Strings.StatusConnected, Callsign)}"
        : NetConnecting ? $"● {Strings.StatusConnecting}"
        : $"● {Strings.StatusDisconnected}";

    /// <summary>Status bar: simulator state.</summary>
    public string SimStatusText => SimConnected
        ? $"✈ {string.Format(Strings.SimConnected, SimName)}"
        : $"✈ {Strings.SimWaiting}";

    /// <summary>Status bar: PTT state.</summary>
    public string PttStatusText => PttKey.Length > 0 ? $"🎙 {PttKey}" : $"🎙 {Strings.PttNotAssigned}";

    public string WindowTitle => NetConnected ? $"SkyPilot — {Callsign}" : "SkyPilot";

    private void OnStatusChanged()
    {
        RaisePropertyChanged(nameof(SimStatus));
        RaisePropertyChanged(nameof(ConnectButtonText));
        RaisePropertyChanged(nameof(NetStatusText));
        RaisePropertyChanged(nameof(SimStatusText));
        RaisePropertyChanged(nameof(PttStatusText));
        RaisePropertyChanged(nameof(WindowTitle));
    }

    // ---- radios -----------------------------------------------------------------------

    public string Com1 { get => _com1; set => Set(ref _com1, value); }
    public string Com2 { get => _com2; set => Set(ref _com2, value); }
    public string Squawk { get => _squawk; set => Set(ref _squawk, value); }
    public bool ModeC { get => _modeC; set { if (Set(ref _modeC, value)) RaisePropertyChanged(nameof(ModeCText)); } }
    public string ModeCText => ModeC ? "MODE C" : "STBY";
    public bool Identing { get => _identing; set => Set(ref _identing, value); }

    public bool Com1Rx { get => _com1Rx; set => Set(ref _com1Rx, value); }
    public bool Com2Rx { get => _com2Rx; set => Set(ref _com2Rx, value); }

    public int TxRadio
    {
        get => _txRadio;
        set
        {
            if (!Set(ref _txRadio, value)) return;
            RaisePropertyChanged(nameof(Com1Tx));
            RaisePropertyChanged(nameof(Com2Tx));
            RaisePropertyChanged(nameof(TxFrequency));
        }
    }

    public bool Com1Tx { get => TxRadio == 1; set { if (value) TxRadio = 1; else RaisePropertyChanged(nameof(Com1Tx)); } }
    public bool Com2Tx { get => TxRadio == 2; set { if (value) TxRadio = 2; else RaisePropertyChanged(nameof(Com2Tx)); } }

    /// <summary>The frequency text goes out on (shown above the chat).</summary>
    public string TxFrequency => TxRadio == 2 ? Com2 : Com1;

    /// <summary>ATC station tuned on each radio, or "-".</summary>
    public string Com1Station => StationOn(_com1Khz);
    public string Com2Station => StationOn(_com2Khz);

    private string StationOn(int khz)
    {
        var s = _stations.FirstOrDefault(st => Frequency.SameChannel(st.FrequencyKhz, khz));
        if (s is null) return "";
        return string.IsNullOrEmpty(s.FacilityText) ? s.Callsign : $"{s.Callsign}  {s.FacilityText}";
    }

    /// <summary>Set frequencies in kHz for preview and testing without a full OwnAircraftData object.</summary>
    public void SetFrequencies(int com1Khz, int com2Khz)
    {
        _com1Khz = com1Khz;
        _com2Khz = com2Khz;
        RaisePropertyChanged(nameof(Com1Station));
        RaisePropertyChanged(nameof(Com2Station));
    }

    public void UpdateRadios(OwnAircraftData own)
    {
        _com1Khz = own.Com1Khz;
        _com2Khz = own.Com2Khz;
        Com1 = Frequency.Format(own.Com1Khz);
        Com2 = Frequency.Format(own.Com2Khz);
        Squawk = own.TransponderCode.ToString("0000");
        RaisePropertyChanged(nameof(TxFrequency));
        RaisePropertyChanged(nameof(Com1Station));
        RaisePropertyChanged(nameof(Com2Station));
    }

    public int TrafficCount { get => _trafficCount; set => Set(ref _trafficCount, value); }

    /// <summary>Number of ATC stations currently in the list (matches what the ATC panel shows).</summary>
    public int ControllerCount => Controllers.Count;

    // ---- voice ---------------------------------------------------------------------------

    public VoiceState VoiceState
    {
        get => _voiceState;
        set { if (Set(ref _voiceState, value)) OnVoiceChanged(); }
    }

    public bool VoiceFailing { get => _voiceFailing; set { if (Set(ref _voiceFailing, value)) OnVoiceChanged(); } }
    public bool VoiceConnected => VoiceState == VoiceState.Connected;
    public string VoiceText => VoiceState == VoiceState.Connecting ? "VOICE…" : "VOICE";

    public string VoiceTip =>
        VoiceConnected ? "Voice: connected. Click to reconnect"
        : VoiceFailing ? "Voice: no connection to the voice server. Click to reconnect"
        : VoiceState == VoiceState.Connecting ? "Voice: connecting…"
        : "Voice connects when you connect to the network";

    private void OnVoiceChanged()
    {
        RaisePropertyChanged(nameof(VoiceConnected));
        RaisePropertyChanged(nameof(VoiceText));
        RaisePropertyChanged(nameof(VoiceTip));
    }

    public bool Transmitting
    {
        get => _transmitting;
        set { if (Set(ref _transmitting, value)) RaisePropertyChanged(nameof(PttText)); }
    }

    public string PttText => Transmitting ? "TX" : "PTT";

    /// <summary>"RX AFL123" while someone is heard on the radio, otherwise empty.</summary>
    public string Com1Heard { get => _com1Heard; private set => Set(ref _com1Heard, value); }
    public string Com2Heard { get => _com2Heard; private set => Set(ref _com2Heard, value); }

    public void SetHeard(string com1, string com2)
    {
        Com1Heard = com1.Length > 0 ? "RX " + com1 : "";
        Com2Heard = com2.Length > 0 ? "RX " + com2 : "";
    }

    // ---- flight plan -------------------------------------------------------------------

    public FlightPlan? FlightPlan
    {
        get => _flightPlan;
        set
        {
            if (!Set(ref _flightPlan, value)) return;
            RaisePropertyChanged(nameof(HasFlightPlan));
            RaisePropertyChanged(nameof(FlightPlanText));
            RaisePropertyChanged(nameof(DepartureIcao));
            RaisePropertyChanged(nameof(DestinationIcao));
            RaisePropertyChanged(nameof(HasDepartureIcao));
            RaisePropertyChanged(nameof(HasDestinationIcao));
        }
    }

    public bool HasFlightPlan => FlightPlan != null;

    public string FlightPlanText => FlightPlan is { } p
        ? $"{p.Departure} → {p.Destination}   {p.AircraftType}   {p.CruiseAltitude}"
        : Strings.FlightPlanNone;

    /// <summary>Departure ICAO from flight plan, or empty.</summary>
    public string DepartureIcao    => FlightPlan?.Departure ?? "";
    /// <summary>Destination ICAO from flight plan, or empty.</summary>
    public string DestinationIcao  => FlightPlan?.Destination ?? "";
    public bool HasDepartureIcao   => DepartureIcao.Length >= 3;
    public bool HasDestinationIcao => DestinationIcao.Length >= 3;

    // ---- positions panel ----------------------------------------------------------------

    /// <summary>Filter text box content (case-insensitive substring match).</summary>
    public string PositionFilter
    {
        get => _positionFilter;
        set { if (Set(ref _positionFilter, value)) ApplyPositionFilter(); }
    }

    /// <summary>True when the last refresh returned an error; shows an offline hint.</summary>
    public bool PositionsOffline
    {
        get => _positionsOffline;
        set => Set(ref _positionsOffline, value);
    }

    /// <summary>Replace the positions list and apply the current filter.</summary>
    public void UpdatePositions(IReadOnlyList<PositionEntry> entries)
    {
        _allPositions = entries.Select(ToRow).ToList();
        ApplyPositionFilter();
    }

    private static PositionRow ToRow(PositionEntry e)
    {
        string? nextText = e.NextBooking is { } nb
            ? nb.ToLocalTime().ToString("HH:mm")
            : null;

        // Try to parse frequency to kHz for tuning.
        int khz = 0;
        if (e.Frequency.Length > 0)
            SkyPilot.Core.Model.Frequency.TryParse(e.Frequency, out khz);

        return new PositionRow(e.Callsign, e.Frequency, e.Facility, e.State, e.Name, nextText, khz);
    }

    private void ApplyPositionFilter()
    {
        var filter = _positionFilter.Trim();
        var filtered = string.IsNullOrEmpty(filter)
            ? _allPositions
            : _allPositions.Where(r =>
                r.Callsign.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                r.Facility.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                r.Frequency.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

        Positions.Clear();
        foreach (var row in filtered)
            Positions.Add(row);
    }

    // ---- misc ----------------------------------------------------------------------------

    public bool AtcVisible { get => _atcVisible; set => Set(ref _atcVisible, value); }
    public bool Topmost { get => _topmost; set => Set(ref _topmost, value); }
    public string UtcTime { get => _utcTime; set => Set(ref _utcTime, value); }

    public ChatTab GetPrivateTab(string peer)
    {
        var tab = Tabs.FirstOrDefault(t => t.Peer == peer);
        if (tab == null)
        {
            tab = new ChatTab(peer, peer);
            Tabs.Add(tab);
        }
        return tab;
    }

    public void SetControllers(IEnumerable<AtcStation> stations, IReadOnlyDictionary<string, AtisInfo> atis)
    {
        _stations = stations.ToList();
        Controllers.Clear();
        foreach (var s in _stations)
        {
            atis.TryGetValue(s.Callsign, out var info);
            string letter = s.IsAtis && info?.Letter is { } l ? l.ToString() : "";
            string? text = info is { Lines.Count: > 0 }
                ? $"{info.Text}\n\nReceived at {info.ReceivedAt.ToLocalTime():HH:mm}"
                : null;
            Controllers.Add(new AtcRow(s.Callsign, Frequency.Format(s.FrequencyKhz), s.FacilityText, s.FrequencyKhz,
                s.IsAtis, letter, text));
        }
        RaisePropertyChanged(nameof(Com1Station));
        RaisePropertyChanged(nameof(Com2Station));
        RaisePropertyChanged(nameof(ControllerCount));
    }
}
