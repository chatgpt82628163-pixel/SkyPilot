namespace SkyPilot.Core;

/// <summary>Where SkyNetwork lives. Built into the client: pilots never type a server address.</summary>
public static class SkyNetworkAddress
{
    public const string Host = "sky-network.online";
    /// <summary>FSD server (TCP).</summary>
    public const int FsdPort = 6809;
    /// <summary>Voice server (UDP), on the same host.</summary>
    public const int VoicePort = 3782;
    /// <summary>The website: sign-in, flight plans, releases.</summary>
    public const string Website = "https://" + Host + "/";
}
