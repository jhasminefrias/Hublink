using System.Text.Json;
using HubLink.Configuration;
using HubLink.Models;

namespace HubLink.Services;

/// <summary>All HTTP + JSON work lives here. No UI code.</summary>
public sealed class ShipmentApiClient : IShipmentApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _http;
    private readonly ServiceSettings _settings;

    public ShipmentApiClient(HttpClient http, ServiceSettings settings)
    {
        _http = http;
        _settings = settings;
    }

    public string RequestUrl
    {
        get
        {
            var baseUrl = _settings.ServiceBaseUrl.TrimEnd('/');
            var path = "/" + _settings.EndpointPath.TrimStart('/');
            var url = $"{baseUrl}{path}?branchCode={Uri.EscapeDataString(_settings.BranchCode)}";
            var extra = _settings.ExtraQuery?.Trim().TrimStart('&', '?');
            return string.IsNullOrEmpty(extra) ? url : $"{url}&{extra}";
        }
    }

    public async Task<ShipmentFetchResult> GetShipmentsAsync(CancellationToken ct = default)
    {
        if (!Uri.TryCreate(RequestUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return ShipmentFetchResult.Failed(FetchFailureKind.Configuration,
                "The service address is not valid. Check ServiceBaseUrl in appsettings.json.");

        var seconds = Math.Max(1, _settings.TimeoutSeconds);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(seconds));

        try
        {
            using var response = await _http.GetAsync(uri, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var detail = await ReadErrorDetailAsync(response, cts.Token);
                return ShipmentFetchResult.Failed(FetchFailureKind.HttpError,
                    $"The service returned an error (HTTP {(int)response.StatusCode}).{detail}");
            }

            string body;
            try { body = await response.Content.ReadAsStringAsync(cts.Token); }
            catch (Exception ex) when (ex is HttpRequestException or IOException)
            {
                return ShipmentFetchResult.Failed(FetchFailureKind.InvalidResponse,
                    "The response from the service could not be read completely.");
            }
            return Parse(body);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return ShipmentFetchResult.Failed(FetchFailureKind.Timeout,
                $"The request timed out after {seconds} seconds.");
        }
        catch (HttpRequestException)
        {
            return ShipmentFetchResult.Failed(FetchFailureKind.Unreachable,
                "The shipment service could not be reached. Check that the service is running and the address is correct.");
        }
    }

    private static ShipmentFetchResult Parse(string body)
    {
        const string invalid = "The service responded, but the data could not be understood as shipment information.";
        ShipmentResponseDto? dto;
        try { dto = JsonSerializer.Deserialize<ShipmentResponseDto>(body, JsonOptions); }
        catch (JsonException) { return ShipmentFetchResult.Failed(FetchFailureKind.InvalidResponse, invalid); }

        if (dto?.Shipments is null)
            return ShipmentFetchResult.Failed(FetchFailureKind.InvalidResponse, invalid);

        var list = new List<Shipment>();
        foreach (var s in dto.Shipments)
        {
            if (s is null || Blank(s.TrackingNumber) || Blank(s.Origin) || Blank(s.Destination) || Blank(s.Status) || s.LastUpdatedUtc is null)
                return ShipmentFetchResult.Failed(FetchFailureKind.InvalidResponse, invalid);
            list.Add(new Shipment(s.TrackingNumber!, s.Origin!, s.Destination!, s.Status!, s.LastUpdatedUtc.Value));
        }
        return ShipmentFetchResult.Succeeded(list);   // an empty list is a valid, successful result (OR-07)
    }

    private static bool Blank(string? v) => string.IsNullOrWhiteSpace(v);

    private static async Task<string> ReadErrorDetailAsync(HttpResponseMessage r, CancellationToken ct)
    {
        try
        {
            var body = await r.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var e) && e.ValueKind == JsonValueKind.String)
                return " " + e.GetString();
        }
        catch (Exception ex) when (ex is not OperationCanceledException) { }
        return "";
    }
}
