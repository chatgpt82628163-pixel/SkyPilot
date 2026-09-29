using System.Collections.ObjectModel;
using SkyNetwork.Voice;
using SkyPilot.Core.Model;
using SkyPilot.Core.Session;

namespace SkyPilot.App.ViewModels;

/// <summary>A controller position in the ATC list, grouped by facility (Center, Approach, Tower…).</summary>
public sealed class AtcRow : Observable
{
    private string _distance = "";
    private bool _tuned;

    public AtcRow(AtcStation station, AtisInfo? atis)
    {
        Station = station;
        Callsign = station.Callsign;
        FrequencyKhz = station.FrequencyKhz;
        Frequency = Core.Model.Frequency.Format(station.FrequencyKhz);
        IsAtis = station.IsAtis;
        (GroupOrder, Group) = GroupOf(station.FacilityText);
        Letter = IsAtis && atis?.Letter is { } l ? l.ToString() : "";
        AtisText = atis is { Lines.Count: > 0 } ? $"{atis.Text}\n\nReceived at {atis.ReceivedAt.ToLocalTime():HH:mm}" : null;
    }

    public AtcStation Station { get; }
    public string Callsign { get; }
    public string Frequency { get; }
    public int FrequencyKhz { get; }
    public bool IsAtis { get; }
    /// <summary>"Center", "Approach", "Tower", "Ground", "Delivery", "ATIS", "Flight service".</summary>
    public string Group { get; }
    public int GroupOrder { get; }
    /// <summary>Current ATIS letter of an ATIS station, once known; otherwise empty.</summary>
    public string Letter { get; }
    public bool HasLetter => Letter.Length > 0;
    /// <summary>Last ATIS / controller information received from the station, or null (row tooltip).</summary>
    public string? AtisText { get; }

    /// <summary>"42 nm" from the own aircraft, or empty while either position is unknown.</summary>
    public string Distance { get => _distance; set => Set(ref _distance, value); }
    public double? DistanceNm { get; private set; }

    /// <summary>The station is on COM1 or COM2.</summary>
    public bool Tuned { get => _tuned; set => Set(ref _tuned, value); }

    public void UpdateDistance(double? latitude, double? longitude)
    {
        DistanceNm = latitude is { } lat && longitude is { } lon ? Station.DistanceNm(lat, lon) : null;
        Distance = DistanceNm is { } d ? $"{d:0} nm" : "";
    }

    /// <summary>Same order as the facility ladder: the widest area first.</summary>
    private static (int, string) GroupOf(string facility) => facility switch
    {
        "CTR" => (0, "Center"),
        "APP" => (1, "Approach"),
        "TWR" => (2, "Tower"),
        "GND" => (3, "Ground"),
        "DEL" => (4, "Delivery"),
        "ATIS" => (5, "ATIS"),
        "FSS" => (6, "Flight service"),
        _ => (7, "Other"),
    };

    /// <summary>Observers and supervisors without a frequency are not positions a pilot can call.</summary>
    public static bool IsPosition(AtcStation s) =>
        s.FacilityText != "OBS" && s.FrequencyKhz is > 118000 and < 137000;
}

public sealed class MainViewModel : Observable
{
    private bool _simConnected;
    private string? _simError;
    private bool _netConnected;
    private bool _connecting;
    private string _callsign = "";
    private int _com1Khz, _com2Khz;
    private string _com1 = "---.---";
    private string _com2 = "---.---";
    private string _squawk = "----";
    private bool _modeC;
    private bool _identing;
    private bool _com1Rx = true, _com2Rx = true;
    private int _txRadio = 1;
    private int _trafficCount;
    private bool _topmost;
    private FlightPlan? _flightPlan;
    private string _utcTime = "";
    private ChatTab? _selectedTab;
    private List<AtcStation> _stations = [];
    private VoiceState _voiceState;
    private bool _voiceFailing;
    private bool _transmitting;
    private string _com1Heard = "", _com2Heard = "";
    private double? _ownLat, _ownLon;

    public MainViewModel()
    {
        RadioTab = new ChatTab("Radio", null);
        Tabs.Add(RadioTab);
        _selectedTab = RadioTab;
    }

    public ChatTab RadioTab { get; }
    public ObservableCollection<ChatTab> Tabs { get; } = [];

    /// <summary>Controller positions, sorted by group, then distance, then callsign (the view groups them).</summary>
    public ObservableCollection<AtcRow> Controllers { get; } = [];
    public bool HasControllers => Controllers.Count > 0;

    public ChatTab? SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (Set(ref _selectedTab, value) && value != null) value.Unread = false;
            RaisePropertyChanged(nameof(InputHint));
        }
    }

    // ---- connection -----------------------------------------------------------------

    private string _simName = "Simulator";
    public bool SimConnected { get => _simConnected; set { if (Set(ref _simConnected, value)) OnStatusChanged(); } }

    /// <summary>Why the simulator cannot be reached (e.g. SimConnect.dll missing), or null.</summary>
    public string? SimError { get => _simError; set { if (Set(ref _simError, value)) OnStatusChanged(); } }

    public bool NetConnected { get => _netConnected; set { if (Set(ref _netConnected, value)) OnStatusChanged(); } }
    public bool Connecting { get => _connecting; set { if (Set(ref _connecting, value)) OnStatusChanged(); } }
    public string Callsign { get => _callsign; set { if (Set(ref _callsign, value)) OnStatusChanged(); } }

    /// <summary>The connected simulator ("Prepar3D", "X-Plane 12"…).</summary>
    public string SimName { get => _simName; set { if (Set(ref _simName, value)) OnStatusChanged(); } }

    public string SimStatus => SimConnected ? SimName
        : SimError != null ? "Simulator not reachable"
        : "Waiting for the simulator";

    public string ConnectButtonText => NetConnected ? "Disconnect" : Connecting ? "Connecting…" : "Connect";

    /// <summary>Next to the Connect button: the callsign when online.</summary>
    public string NetStatus => NetConnected ? Callsign : Connecting ? "Connecting to SkyNetwork…" : "Offline";

    public string WindowTitle => NetConnected ? $"SkyPilot — {Callsign}" : "SkyPilot";

    private void OnStatusChanged()
    {
        RaisePropertyChanged(nameof(SimStatus));
        RaisePropertyChanged(nameof(ConnectButtonText));
        RaisePropertyChanged(nameof(NetStatus));
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
            RaisePropertyChanged(nameof(InputHint));
        }
    }

    public bool Com1Tx { get => TxRadio == 1; set { if (value) TxRadio = 1; else RaisePropertyChanged(nameof(Com1Tx)); } }
    public bool Com2Tx { get => TxRadio == 2; set { if (value) TxRadio = 2; else RaisePropertyChanged(nameof(Com2Tx)); } }

    /// <summary>The frequency text goes out on.</summary>
    public string TxFrequency => TxRadio == 2 ? Com2 : Com1;

    /// <summary>Grey hint in the empty message box.</summary>
    public string InputHint => SelectedTab?.Peer is { } peer
        ? $"Private message to {peer}"
        : $"Message on {TxFrequency}   ·   commands start with a dot, .help lists them";

    /// <summary>ATC station tuned on each radio, or empty.</summary>
    public string Com1Station => StationOn(_com1Khz);
    public string Com2Station => StationOn(_com2Khz);

    private string StationOn(int khz) =>
        _stations.FirstOrDefault(s => Frequency.SameChannel(s.FrequencyKhz, khz))?.Callsign ?? "";

    public void UpdateRadios(OwnAircraftData own)
    {
        bool tuningChanged = own.Com1Khz != _com1Khz || own.Com2Khz != _com2Khz;
        _com1Khz = own.Com1Khz;
        _com2Khz = own.Com2Khz;
        Com1 = Frequency.Format(own.Com1Khz);
        Com2 = Frequency.Format(own.Com2Khz);
        Squawk = own.TransponderCode.ToString("0000");
        RaisePropertyChanged(nameof(TxFrequency));
        RaisePropertyChanged(nameof(InputHint));
        if (tuningChanged)
        {
            RaisePropertyChanged(nameof(Com1Station));
            RaisePropertyChanged(nameof(Com2Station));
            MarkTuned();
        }
        UpdatePosition(own.State.Latitude, own.State.Longitude);
    }

    public int TrafficCount { get => _trafficCount; set => Set(ref _trafficCount, value); }

    // ---- voice ---------------------------------------------------------------------------

    public VoiceState VoiceState
    {
        get => _voiceState;
        set { if (Set(ref _voiceState, value)) OnVoiceChanged(); }
    }

    public bool VoiceFailing { get => _voiceFailing; set { if (Set(ref _voiceFailing, value)) OnVoiceChanged(); } }
    public bool VoiceConnected => VoiceState == VoiceState.Connected;

    public string VoiceText =>
        VoiceConnected ? "Voice connected"
        : VoiceFailing ? "Voice: no connection"
        : VoiceState == VoiceState.Connecting ? "Voice connecting…"
        : "Voice off";

    public string VoiceTip =>
        VoiceConnected ? "Connected to the voice server. Click to reconnect"
        : VoiceFailing ? "No connection to the voice server. Click to reconnect"
        : VoiceState == VoiceState.Connecting ? "Connecting to the voice server…"
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

    public string PttText => Transmitting ? "TRANSMITTING" : "PUSH TO TALK";

    /// <summary>The callsign heard on each radio right now, otherwise empty.</summary>
    public string Com1Heard { get => _com1Heard; private set => Set(ref _com1Heard, value); }
    public string Com2Heard { get => _com2Heard; private set => Set(ref _com2Heard, value); }

    public void SetHeard(string com1, string com2)
    {
        Com1Heard = com1;
        Com2Heard = com2;
    }

    // ---- flight plan -------------------------------------------------------------------

    public FlightPlan? FlightPlan
    {
        get => _flightPlan;
        set
        {
            if (!Set(ref _flightPlan, value)) return;
            RaisePropertyChanged(nameof(HasFlightPlan));
            RaisePropertyChanged(nameof(FlightPlanRoute));
            RaisePropertyChanged(nameof(FlightPlanDetails));
        }
    }

    public bool HasFlightPlan => FlightPlan != null;
    public string FlightPlanRoute => FlightPlan is { } p ? $"{p.Departure} → {p.Destination}" : "Not filed";

    public string FlightPlanDetails => FlightPlan is { } p
        ? string.Join("  ·  ", new[] { p.AircraftType, p.CruiseAltitude }.Where(s => !string.IsNullOrWhiteSpace(s)))
        : "File it on the website or load it from SimBrief";

    // ---- misc ----------------------------------------------------------------------------

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
        _stations = stations.Where(AtcRow.IsPosition).ToList();
        var rows = _stations.Select(s => new AtcRow(s, atis.GetValueOrDefault(s.Callsign))).ToList();
        foreach (var r in rows) r.UpdateDistance(_ownLat, _ownLon);
        Controllers.Clear();
        foreach (var r in Sorted(rows)) Controllers.Add(r);
        MarkTuned();
        RaisePropertyChanged(nameof(HasControllers));
        RaisePropertyChanged(nameof(Com1Station));
        RaisePropertyChanged(nameof(Com2Station));
    }

    private static IEnumerable<AtcRow> Sorted(IEnumerable<AtcRow> rows) =>
        rows.OrderBy(r => r.GroupOrder).ThenBy(r => r.DistanceNm ?? double.MaxValue).ThenBy(r => r.Callsign);

    /// <summary>Distances follow the own aircraft; the list is re-sorted only when the order actually changes.</summary>
    private void UpdatePosition(double lat, double lon)
    {
        _ownLat = lat;
        _ownLon = lon;
        if (Controllers.Count == 0) return;
        foreach (var r in Controllers) r.UpdateDistance(lat, lon);
        var order = Sorted(Controllers).ToList();
        if (order.SequenceEqual(Controllers)) return;
        for (int i = 0; i < order.Count; i++)
        {
            int from = Controllers.IndexOf(order[i]);
            if (from != i) Controllers.Move(from, i);
        }
    }

    private void MarkTuned()
    {
        foreach (var r in Controllers)
            r.Tuned = Frequency.SameChannel(r.FrequencyKhz, _com1Khz) || Frequency.SameChannel(r.FrequencyKhz, _com2Khz);
    }
}
