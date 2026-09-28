using SkyPilot.Core.Settings;

namespace SkyPilot.Core.Tests;

public class SettingsTests
{
    [Fact]
    public void Defaults_UseProductionServer()
    {
        var s = new AppSettings();
        Assert.Equal("SkyNetwork", s.SelectedServer);
        Assert.Equal("sky.network.npzy2.us", s.CurrentServer.Host);
        Assert.Equal(6809, s.CurrentServer.Port);
        Assert.Equal("https://sky.network.npzy2.us/", s.Website);
    }

    [Fact]
    public void Migrate_LocalhostServer_FixedToProduction()
    {
        var s = new AppSettings
        {
            Servers = [new ServerEntry { Name = "SKYNET", Host = "127.0.0.1", Port = 6809 }],
            SelectedServer = "SKYNET",
            Website = "http://127.0.0.1:8000/"
        };
        AppSettings.Migrate(s);
        Assert.Equal("SkyNetwork", s.Servers[0].Name);
        Assert.Equal("sky.network.npzy2.us", s.Servers[0].Host);
        Assert.Equal("https://sky.network.npzy2.us/", s.Website);
        Assert.Equal("SkyNetwork", s.SelectedServer);
    }

    [Fact]
    public void Migrate_LocalhostVariant_FixedToProduction()
    {
        var s = new AppSettings
        {
            Servers = [new ServerEntry { Name = "Local", Host = "localhost", Port = 6809 }],
            Website = "http://localhost:8000/"
        };
        AppSettings.Migrate(s);
        Assert.Equal("sky.network.npzy2.us", s.Servers[0].Host);
        Assert.Equal("https://sky.network.npzy2.us/", s.Website);
    }

    [Fact]
    public void Migrate_CustomServer_NotTouched()
    {
        var s = new AppSettings
        {
            Servers = [new ServerEntry { Name = "MyServer", Host = "custom.example.com", Port = 6810 }],
            Website = "https://custom.example.com/"
        };
        AppSettings.Migrate(s);
        Assert.Equal("custom.example.com", s.Servers[0].Host);
        Assert.Equal(6810, s.Servers[0].Port);
        Assert.Equal("https://custom.example.com/", s.Website);
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var s = AppSettings.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json"));
        Assert.Equal("sky.network.npzy2.us", s.CurrentServer.Host);
    }

    [Fact]
    public void Load_CorruptFile_ReturnsDefaults()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        File.WriteAllText(path, "not json {{{");
        var s = AppSettings.Load(path);
        Assert.Equal("sky.network.npzy2.us", s.CurrentServer.Host);
        File.Delete(path);
    }
}
