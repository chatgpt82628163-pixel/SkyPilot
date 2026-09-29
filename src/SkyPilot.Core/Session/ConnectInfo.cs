namespace SkyPilot.Core.Session;

public sealed record ConnectInfo(
    string Host,
    int Port,
    int Cid,
    string Password,
    string Callsign,
    string TypeCode,
    string RealName);

/// <param name="Latitude">Where the controller sits (from the position packet); 0/0 when unknown.</param>
public sealed record AtcStation(string Callsign, int FrequencyKhz, int Facility, DateTime LastSeen,
    double Latitude = 0, double Longitude = 0)
{
    public bool HasPosition => Latitude != 0 || Longitude != 0;

    /// <summary>Great-circle distance in nautical miles to a point, or null when the station's position is unknown.</summary>
    public double? DistanceNm(double latitude, double longitude)
    {
        if (!HasPosition) return null;
        const double EarthRadiusNm = 3440.065;
        double rad = Math.PI / 180;
        double dLat = (latitude - Latitude) * rad, dLon = (longitude - Longitude) * rad;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(Latitude * rad) * Math.Cos(latitude * rad) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return 2 * EarthRadiusNm * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    /// <summary>ATIS stations connect as separate clients named like "UUEE_ATIS".</summary>
    public bool IsAtis => IsAtisCallsign(Callsign);

    /// <summary>"ATIS", "TWR", "OBS", …</summary>
    public string FacilityText => FacilityName(Callsign, Facility);

    public static bool IsAtisCallsign(string callsign) => callsign.EndsWith("_ATIS", StringComparison.OrdinalIgnoreCase);

    /// <summary>Like <see cref="FacilityName(int)"/>, but ATIS stations are "ATIS" whatever facility they report.</summary>
    public static string FacilityName(string callsign, int facility) =>
        IsAtisCallsign(callsign) ? "ATIS" : FacilityName(facility);

    public static string FacilityName(int facility) => facility switch
    {
        1 => "FSS",
        2 => "DEL",
        3 => "GND",
        4 => "TWR",
        5 => "APP",
        6 => "CTR",
        _ => "OBS",
    };
}
