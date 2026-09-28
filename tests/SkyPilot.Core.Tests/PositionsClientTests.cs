using System.Net;
using System.Text;
using SkyPilot.Core.Session;
using SkyPilot.Core.Web;

namespace SkyPilot.Core.Tests;

public class PositionsClientTests
{
    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public Uri? LastUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }

    private static PositionsClient Make(string json, out StubHandler handler,
        HttpStatusCode status = HttpStatusCode.OK)
    {
        handler = new StubHandler(status, json);
        return new PositionsClient(new HttpClient(handler), new Uri("https://sky.test/"));
    }

    [Fact]
    public async Task ParsesOnlinePosition()
    {
        var client = Make("""
            [{"callsign":"UWWW_APP","facility":"APP","frequency":"119.400",
              "state":"online","name":"Ivan Petrov","onlineSince":"2025-06-01T12:00:00Z"}]
            """, out _);
        var list = await client.FetchAsync();
        Assert.Single(list);
        var p = list[0];
        Assert.Equal("UWWW_APP", p.Callsign);
        Assert.Equal(PositionState.Online, p.State);
        Assert.Equal("Ivan Petrov", p.Name);
        Assert.Equal("119.400", p.Frequency);
    }

    [Fact]
    public async Task ParsesBookedPosition()
    {
        var client = Make("""
            [{"callsign":"UUEE_TWR","facility":"TWR","frequency":"118.100",
              "state":"booked","nextBooking":"2025-06-01T14:00:00Z"}]
            """, out _);
        var list = await client.FetchAsync();
        Assert.Single(list);
        var p = list[0];
        Assert.Equal(PositionState.Booked, p.State);
        Assert.NotNull(p.NextBooking);
    }

    [Fact]
    public async Task ParsesFreePosition()
    {
        var client = Make("""[{"callsign":"UUDD_GND","facility":"GND","frequency":"121.900","state":"free"}]""", out _);
        var list = await client.FetchAsync();
        Assert.Equal(PositionState.Free, list[0].State);
    }

    [Fact]
    public async Task IncludesLatLonInUrl()
    {
        var client = Make("[]", out var handler);
        await client.FetchAsync(lat: 55.97, lon: 37.41, radiusNm: 200);
        Assert.Contains("lat=55", handler.LastUri!.Query);
        Assert.Contains("lon=37", handler.LastUri.Query);
        Assert.Contains("radius=200", handler.LastUri.Query);
    }

    [Fact]
    public async Task ReturnsEmptyOnNotFound()
    {
        var client = Make("", out _, HttpStatusCode.NotFound);
        var list = await client.FetchAsync();
        Assert.Empty(list);
    }

    [Fact]
    public void MergeNetworkWinsFrequency()
    {
        var catalog = new List<PositionEntry>
        {
            new("UWWW_APP", "APP", "119.000", null, PositionState.Free, null, null, null),
        };
        var online = new List<AtcStation> { new("UWWW_APP", 119400, 5, DateTime.UtcNow) };

        var merged = PositionsClient.Merge(catalog, online);

        var row = Assert.Single(merged, r => r.Callsign == "UWWW_APP");
        Assert.Equal(PositionState.Online, row.State);
        Assert.Equal("119.400", row.Frequency);
    }

    [Fact]
    public void MergeInjectsExtraOnlineStations()
    {
        var catalog = new List<PositionEntry>();
        var online  = new List<AtcStation> { new("ULLI_TWR", 118400, 4, DateTime.UtcNow) };

        var merged = PositionsClient.Merge(catalog, online);

        var row = Assert.Single(merged);
        Assert.Equal("ULLI_TWR", row.Callsign);
        Assert.Equal(PositionState.Online, row.State);
    }

    [Fact]
    public void MergeSortOrder_OnlineFirst()
    {
        var now = DateTime.UtcNow;
        var catalog = new List<PositionEntry>
        {
            new("UWWW_GND", "GND", "121.700", null, PositionState.Free,   null, null, null),
            new("UWWW_TWR", "TWR", "118.100", null, PositionState.Booked, null, null, now),
            new("UWWW_APP", "APP", "119.400", null, PositionState.Free,   null, null, null),
        };
        var online = new List<AtcStation> { new("UWWW_TWR", 118100, 4, now) };

        var merged = PositionsClient.Merge(catalog, online);

        Assert.Equal(PositionState.Online, merged[0].State);
        Assert.Equal("UWWW_TWR", merged[0].Callsign);
    }
}
