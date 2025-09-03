using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace CarelessWhisperV2.Services.Network;

public class NetworkDiagnosticsService : INetworkDiagnosticsService
{
    private readonly ILogger<NetworkDiagnosticsService> _logger;
    private readonly HttpClient _httpClient;

    public NetworkDiagnosticsService(ILogger<NetworkDiagnosticsService> logger)
    {
        _logger = logger;
        
        var handler = new HttpClientHandler()
        {
            UseProxy = true,
            UseDefaultCredentials = true,
        };

        if (System.Net.WebRequest.DefaultWebProxy != null)
        {
            handler.Proxy = System.Net.WebRequest.DefaultWebProxy;
        }

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CarelessWhisperV3-Diagnostics/1.0");
    }

    public async Task<NetworkDiagnosticsResult> RunDiagnosticsAsync()
    {
        var result = new NetworkDiagnosticsResult();
        
        _logger.LogInformation("Starting network diagnostics...");

        try
        {
            // Test 1: Basic internet connectivity
            result.InternetConnectivity = await TestInternetConnectivityAsync();
            if (!result.InternetConnectivity)
            {
                result.Issues.Add("No internet connectivity detected");
                result.Suggestions.Add("Check your network connection and try again");
            }

            // Test 2: DNS resolution for OpenRouter
            result.DnsResolution = await TestDnsResolutionAsync("openrouter.ai");
            if (!result.DnsResolution)
            {
                result.Issues.Add("Cannot resolve openrouter.ai domain");
                result.Suggestions.Add("Check your DNS settings or try using a different DNS server (like 8.8.8.8)");
            }

            // Test 3: HTTPS connectivity to OpenRouter
            result.HttpsConnectivity = await TestHttpsConnectivityAsync("https://openrouter.ai");
            if (!result.HttpsConnectivity)
            {
                result.Issues.Add("Cannot establish HTTPS connection to OpenRouter");
                result.Suggestions.Add("This may indicate firewall restrictions or proxy configuration issues");
            }

            // Test 4: OpenRouter API specific test
            result.OpenRouterApiAccessible = await TestHttpsConnectivityAsync("https://openrouter.ai/api/v1/models");
            if (!result.OpenRouterApiAccessible)
            {
                result.Issues.Add("OpenRouter API endpoint is not accessible");
                result.Suggestions.Add("Your network may be blocking API access. Contact your network administrator.");
            }

            // Test 5: Proxy information
            result.ProxyInfo = await GetProxyInformationAsync();
            if (result.ProxyInfo.ProxyDetected && !result.OpenRouterApiAccessible)
            {
                result.Suggestions.Add("Proxy detected but API not accessible. The proxy may need configuration for HTTPS traffic.");
            }

            // Generate summary
            result.Summary = GenerateSummary(result);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during network diagnostics");
            result.Issues.Add($"Diagnostics error: {ex.Message}");
        }

        _logger.LogInformation($"Network diagnostics completed. Summary: {result.Summary}");
        return result;
    }

    public async Task<bool> TestInternetConnectivityAsync()
    {
        try
        {
            // Test connectivity to multiple reliable endpoints
            var testEndpoints = new[]
            {
                "https://www.google.com",
                "https://www.cloudflare.com",
                "https://httpbin.org/get"
            };

            foreach (var endpoint in testEndpoints)
            {
                try
                {
                    using var response = await _httpClient.GetAsync(endpoint);
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogDebug($"Internet connectivity confirmed via {endpoint}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug($"Failed to reach {endpoint}: {ex.Message}");
                }
            }

            // Fallback: Try ping to Google DNS
            using var ping = new Ping();
            var reply = await ping.SendPingAsync("8.8.8.8", 5000);
            var isReachable = reply.Status == IPStatus.Success;
            
            if (isReachable)
            {
                _logger.LogDebug("Internet connectivity confirmed via ping to 8.8.8.8");
            }
            
            return isReachable;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing internet connectivity");
            return false;
        }
    }

    public async Task<bool> TestDnsResolutionAsync(string hostname)
    {
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(hostname);
            var resolved = addresses.Length > 0;
            
            if (resolved)
            {
                _logger.LogDebug($"DNS resolution successful for {hostname}: {string.Join(", ", addresses.Select(a => a.ToString()))}");
            }
            
            return resolved;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"DNS resolution failed for {hostname}");
            return false;
        }
    }

    public async Task<bool> TestHttpsConnectivityAsync(string url)
    {
        try
        {
            using var response = await _httpClient.GetAsync(url);
            var isAccessible = response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Unauthorized; // 401 means we reached the server
            
            _logger.LogDebug($"HTTPS test for {url}: Status={response.StatusCode}, Accessible={isAccessible}");
            return isAccessible;
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, $"HTTP request failed for {url}: {httpEx.Message}");
            return false;
        }
        catch (TaskCanceledException tcEx)
        {
            _logger.LogError(tcEx, $"Request to {url} timed out");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error testing {url}");
            return false;
        }
    }

    public async Task<ProxyInfo> GetProxyInformationAsync()
    {
        var proxyInfo = new ProxyInfo();

        try
        {
            var systemProxy = System.Net.WebRequest.DefaultWebProxy;
            if (systemProxy != null)
            {
                var testUri = new Uri("https://openrouter.ai");
                var proxyUri = systemProxy.GetProxy(testUri);
                
                if (proxyUri != testUri) // Different URI means proxy is being used
                {
                    proxyInfo.ProxyDetected = true;
                    proxyInfo.ProxyAddress = proxyUri.ToString();
                    proxyInfo.UseSystemProxy = true;
                    proxyInfo.ProxyType = "System";
                    
                    // Check if proxy requires authentication
                    proxyInfo.AuthenticationRequired = systemProxy.Credentials != null;
                }
            }

            // Also check environment variables for proxy settings
            var httpProxy = System.Environment.GetEnvironmentVariable("HTTP_PROXY") ?? System.Environment.GetEnvironmentVariable("http_proxy");
            var httpsProxy = System.Environment.GetEnvironmentVariable("HTTPS_PROXY") ?? System.Environment.GetEnvironmentVariable("https_proxy");
            
            if (!string.IsNullOrEmpty(httpsProxy) || !string.IsNullOrEmpty(httpProxy))
            {
                proxyInfo.ProxyDetected = true;
                proxyInfo.ProxyAddress = httpsProxy ?? httpProxy ?? "";
                proxyInfo.ProxyType = "Environment";
            }

            _logger.LogInformation($"Proxy detection: Detected={proxyInfo.ProxyDetected}, Address={proxyInfo.ProxyAddress}, Type={proxyInfo.ProxyType}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting proxy information");
        }

        return proxyInfo;
    }

    private string GenerateSummary(NetworkDiagnosticsResult result)
    {
        if (result.OpenRouterApiAccessible)
        {
            return "✓ All connectivity tests passed. OpenRouter API should be accessible.";
        }
        
        if (!result.InternetConnectivity)
        {
            return "✗ No internet connection detected. Check your network connection.";
        }
        
        if (!result.DnsResolution)
        {
            return "✗ DNS resolution failed for openrouter.ai. Check DNS settings.";
        }
        
        if (!result.HttpsConnectivity)
        {
            return "✗ Cannot reach OpenRouter website. Firewall or proxy may be blocking access.";
        }
        
        return "✗ OpenRouter API is not accessible. Network restrictions likely in place.";
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
