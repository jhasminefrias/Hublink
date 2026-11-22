using System.Text.Json;

namespace HubLink.Configuration;

public sealed class ServiceSettings
{
    public string ServiceBaseUrl { get; set; } = "http://127.0.0.1:5080";
    public string EndpointPath { get; set; } = "/api/shipments";
    public string BranchCode { get; set; } = "BR-001";
    public string ExtraQuery { get; set; } = "";      // e.g. "mode=server-error" for the controlled test endpoint
    public int TimeoutSeconds { get; set; } = 8;      // must be shorter than 10 to test the "delay" mode
}

public static class ServiceSettingsLoader
{
    // Order: defaults -> appsettings.json (next to the .exe) -> environment variables.
    public static ServiceSettings Load()
    {
        var settings = new ServiceSettings();
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (File.Exists(path))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                if (doc.RootElement.TryGetProperty("HubLink", out var section))
                    settings = section.Deserialize<ServiceSettings>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? settings;
            }
        }
        catch (Exception) { /* unreadable file: keep defaults */ }

        var url = Environment.GetEnvironmentVariable("HUBLINK_BASEURL");
        var branch = Environment.GetEnvironmentVariable("HUBLINK_BRANCH");
        if (!string.IsNullOrWhiteSpace(url)) settings.ServiceBaseUrl = url;
        if (!string.IsNullOrWhiteSpace(branch)) settings.BranchCode = branch;
        return settings;
    }
}
