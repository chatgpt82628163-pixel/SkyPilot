using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using SkyPilot.Core.Model;
using SkyPilot.Core.Session;

namespace SkyPilot.Core.Web;

public enum PositionState { Online, Booked, Free }

public sealed record PositionEntry(
    string Callsign,
    string Facility,
    string Frequency,
    double[]? Location,
    PositionState State,
    string? Name,
    DateTime? OnlineSince,
    DateTime? NextBooking);

/// <summary>
/// Reads the ATC position catalog from the SkyNetwork positions API.
/// Use <see cref="Merge"/> to overlay live online data from the FSD session.
/// </summary>
public sealed class PositionsClient(HttpClient http, Uri baseUrl)
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(8);

    /// <param name="lat">Aircraft latitude (degrees), or null to skip the distance filter.</param>
    /// <param name="lon">Aircraft longitude (degrees), or null to skip the distance filter.</param>
    /// <param name="radiusNm">Radius in nautical miles (default 250).</param>
    public async Task<IReadOnlyList<PositionEntry>> FetchAsync(
        double? lat = null, double? lon = null, double radiusNm = 250,
        CancellationToken ct = default)
    {
        var query = lat.HasValue && lon.HasValue
            ? $"?lat={lat.Value:F6}&lon={lon.Value:F6}&radius={radiusNm:F0}"
            : "";
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(RequestTimeout);
        var url = new Uri(baseUrl, "api/v1/positions" + query);
        using var response = await http.GetAsync(url, cts.Token).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound) return [];
        response.EnsureSuccessStatusCode();
        var dtos = await response.Content
            .ReadFromJsonAsync<PositionDto[]>(cts.Token).ConfigureAwait(false);
        return dtos?.Select(d => d.ToEntry()).ToList() ?? [];
    }

    /// <summary>
    /// Merges a positions catalog with the live online list from the FSD session.
    /// Network wins for online state and frequency.  Online controllers not in
    /// the catalog are appended.  Result is sorted online → booked → free.
    /// </summary>
    public static IReadOnlyList<PositionEntry> Merge(
        IReadOnlyList<PositionEntry> catalog,
        IReadOnlyList<AtcStation> onlineStations)
    {
        var online = onlineStations.ToDictionary(
            s => s.Callsign,
            StringComparer.OrdinalIgnoreCase);

        var catalogCallsigns = catalog
            .Select(p => p.Callsign)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var extra = onlineStations
            .Where(s => !catalogCallsigns.Contains(s.Callsign))
            .Select(s => new PositionEntry(
                s.Callsign,
                s.FacilityText,
                Frequency.Format(s.FrequencyKhz),
                null,
                PositionState.Online,
                null,
                s.LastSeen,
                null));

        var merged = catalog
            .Select(p =>
            {
                if (!online.TryGetValue(p.Callsign, out var st)) return p;
                return p with
                {
                    State = PositionState.Online,
                    Frequency = Frequency.Format(st.FrequencyKhz),
                    OnlineSince = st.LastSeen,
                };
            })
            .Concat(extra)
            .ToList();

        // Sort: online first, then booked, then free; within each group: by callsign.
        merged.Sort((a, b) =>
        {
            int sa = a.State == PositionState.Online ? 0 : a.State == PositionState.Booked ? 1 : 2;
            int sb = b.State == PositionState.Online ? 0 : b.State == PositionState.Booked ? 1 : 2;
            return sa != sb ? sa.CompareTo(sb)
                : string.Compare(a.Callsign, b.Callsign, StringComparison.OrdinalIgnoreCase);
        });

        return merged;
    }

    internal sealed class PositionDto
    {
        [JsonPropertyName("callsign")]    public string?   Callsign    { get; set; }
        [JsonPropertyName("facility")]    public string?   Facility    { get; set; }
        [JsonPropertyName("frequency")]   public string?   Frequency   { get; set; }
        [JsonPropertyName("location")]    public double[]? Location    { get; set; }
        [JsonPropertyName("state")]       public string?   State       { get; set; }
        [JsonPropertyName("name")]        public string?   Name        { get; set; }
        [JsonPropertyName("onlineSince")] public DateTime? OnlineSince { get; set; }
        [JsonPropertyName("nextBooking")] public DateTime? NextBooking { get; set; }

        public PositionEntry ToEntry() => new(
            Callsign  ?? "",
            Facility  ?? "",
            Frequency ?? "",
            Location,
            State?.ToLowerInvariant() switch
            {
                "online" => PositionState.Online,
                "booked" => PositionState.Booked,
                _        => PositionState.Free,
            },
            Name,
            OnlineSince,
            NextBooking);
    }
}
