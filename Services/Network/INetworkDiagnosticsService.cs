using System.Net;

namespace CarelessWhisperV2.Services.Network;

public interface INetworkDiagnosticsService
{
    Task<NetworkDiagnosticsResult> RunDiagnosticsAsync();
    Task<bool> TestInternetConnectivityAsync();
    Task<bool> TestDnsResolutionAsync(string hostname);
    Task<bool> TestHttpsConnectivityAsync(string url);
    Task<ProxyInfo> GetProxyInformationAsync();
}

public class NetworkDiagnosticsResult
{
    public bool InternetConnectivity { get; set; }
    public bool DnsResolution { get; set; }
    public bool HttpsConnectivity { get; set; }
    public bool OpenRouterApiAccessible { get; set; }
    public ProxyInfo ProxyInfo { get; set; } = new();
    public List<string> Issues { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
    public string Summary { get; set; } = "";
}

public class ProxyInfo
{
    public bool ProxyDetected { get; set; }
    public string ProxyAddress { get; set; } = "";
    public bool UseSystemProxy { get; set; }
    public bool AuthenticationRequired { get; set; }
    public string ProxyType { get; set; } = "";
}
