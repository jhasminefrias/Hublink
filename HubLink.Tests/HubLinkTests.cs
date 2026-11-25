using System.Net;
using System.Text;
using HubLink.Configuration;
using HubLink.Models;
using HubLink.Services;
using Xunit;

namespace HubLink.Tests;

public class ApiClientTests
{
    private const string TwoShipments = """
    {"branchCode":"BR-001","shipments":[
      {"trackingNumber":"HUB-1","branchCode":"BR-001","origin":"Cagayan de Oro","destination":"Valencia","status":"InTransit","lastUpdatedUtc":"2026-11-10T08:30:00Z"},
      {"trackingNumber":"HUB-2","branchCode":"BR-001","origin":"A, B","destination":"C \"D\"","status":"Delivered","lastUpdatedUtc":"2026-11-11T01:00:00Z"}]}
    """;

    private static ShipmentApiClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> h, int timeout = 5) =>
        new(new HttpClient(new StubHandler(h)) { Timeout = Timeout.InfiniteTimeSpan }, new ServiceSettings { TimeoutSeconds = timeout });

    private static Task<HttpResponseMessage> Json(HttpStatusCode code, string body) =>
        Task.FromResult(new HttpResponseMessage(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") });

    [Fact] // OR-01 / AC-01
    public async Task Valid_json_is_parsed_into_shipments()
    {
        var r = await Client((_, _) => Json(HttpStatusCode.OK, TwoShipments)).GetShipmentsAsync();
        Assert.True(r.IsSuccess);
        Assert.Equal(2, r.Shipments.Count);
        Assert.Equal("A, B", r.Shipments[1].Origin);
    }

    [Fact] // OR-07
    public async Task Empty_result_is_success_not_failure()
    {
        var r = await Client((_, _) => Json(HttpStatusCode.OK, """{"branchCode":"BR-999","shipments":[]}""")).GetShipmentsAsync();
        Assert.True(r.IsSuccess);
        Assert.Empty(r.Shipments);
    }

    [Fact] // AC-05: HTTP failure
    public async Task Http_500_is_reported_as_failure()
    {
        var r = await Client((_, _) => Json(HttpStatusCode.InternalServerError, """{"error":"Simulated server error."}""")).GetShipmentsAsync();
        Assert.False(r.IsSuccess);
        Assert.Equal(FetchFailureKind.HttpError, r.Failure);
        Assert.Contains("500", r.ErrorMessage);
    }

    [Fact]
    public async Task Http_400_is_reported_as_failure()
    {
        var r = await Client((_, _) => Json(HttpStatusCode.BadRequest, """{"error":"Simulated bad request."}""")).GetShipmentsAsync();
        Assert.Equal(FetchFailureKind.HttpError, r.Failure);
    }

    [Fact] // AC-05: invalid JSON
    public async Task Incomplete_json_is_reported_as_invalid_response()
    {
        var r = await Client((_, _) => Json(HttpStatusCode.OK, """{"branchCode":"BR-001","shipments":[{"trackingNumber":"HUB-1""")).GetShipmentsAsync();
        Assert.Equal(FetchFailureKind.InvalidResponse, r.Failure);
    }

    [Fact]
    public async Task Missing_required_field_is_invalid_response()
    {
        var r = await Client((_, _) => Json(HttpStatusCode.OK, """{"shipments":[{"trackingNumber":"HUB-1","origin":"A"}]}""")).GetShipmentsAsync();
        Assert.Equal(FetchFailureKind.InvalidResponse, r.Failure);
    }

    [Fact] // AC-05: unreachable
    public async Task Connection_failure_is_reported_as_unreachable()
    {
        var r = await Client((_, _) => throw new HttpRequestException("refused")).GetShipmentsAsync();
        Assert.Equal(FetchFailureKind.Unreachable, r.Failure);
    }

    [Fact] // AC-05: timeout
    public async Task Slow_service_is_reported_as_timeout()
    {
        var r = await Client(async (_, ct) => { await Task.Delay(Timeout.Infinite, ct); return new HttpResponseMessage(); }, timeout: 1).GetShipmentsAsync();
        Assert.Equal(FetchFailureKind.Timeout, r.Failure);
    }

    [Fact]
    public void Request_url_uses_configured_branch_and_extra_query()
    {
        var c = new ShipmentApiClient(new HttpClient(), new ServiceSettings
        { ServiceBaseUrl = "http://127.0.0.1:5081/", EndpointPath = "api/test/shipments", BranchCode = "BR-002", ExtraQuery = "mode=empty" });
        Assert.Equal("http://127.0.0.1:5081/api/test/shipments?branchCode=BR-002&mode=empty", c.RequestUrl);
    }
}

public class LogicTests
{
    private static readonly DateTimeOffset T = new(2026, 11, 10, 8, 30, 0, TimeSpan.Zero);
    private static readonly List<Shipment> Data = new()
    {
        new("H1","A","B","Pending",T), new("H2","A","B","InTransit",T), new("H3","A","B","InTransit",T),
        new("H4","A","B","Delivered",T), new("H5","A","B","Exception",T)
    };

    [Fact] // AC-03
    public void Summary_counts_match_data()
    {
        var s = ShipmentProcessor.Summarize(Data);
        Assert.Equal((5, 1, 2, 1, 1), (s.Total, s.PendingCount, s.InTransitCount, s.DeliveredCount, s.ExceptionCount));
    }

    [Fact] // AC-02
    public void Filter_returns_only_selected_status_and_All_returns_everything()
    {
        Assert.Equal(2, ShipmentProcessor.Filter(Data, "InTransit").Count());
        Assert.Equal(5, ShipmentProcessor.Filter(Data, "All").Count());
    }

    [Fact] // AC-06
    public void Csv_has_header_one_row_per_shipment_and_escapes_values()
    {
        var csv = CsvBuilder.Build(new[] { new Shipment("H1", "Valencia, Bukidnon", "The \"Hub\"", "Pending", T) });
        var lines = csv.TrimEnd('\r', '\n').Split("\r\n");
        Assert.Equal("TrackingNumber,Origin,Destination,Status,LastUpdatedUtc", lines[0]);
        Assert.Equal("H1,\"Valencia, Bukidnon\",\"The \"\"Hub\"\"\",Pending,2026-11-10T08:30:00Z", lines[1]);
        Assert.Equal(2, lines.Length);
    }

    [Fact] // AC-07
    public async Task Export_failure_is_reported_not_hidden()
    {
        var bad = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "missing-folder", "x.csv");
        var r = await new CsvExportService().ExportAsync(bad, Data);
        Assert.False(r.Success);
    }

    [Fact]
    public async Task Export_with_no_data_is_refused()
    {
        var r = await new CsvExportService().ExportAsync(Path.GetTempFileName(), new List<Shipment>());
        Assert.False(r.Success);
    }

    [Fact]
    public async Task Export_writes_file_that_matches_data()
    {
        var path = Path.Combine(Path.GetTempPath(), $"hublink_{Guid.NewGuid():N}.csv");
        try
        {
            var r = await new CsvExportService().ExportAsync(path, Data);
            Assert.True(r.Success);
            Assert.Equal(6, File.ReadAllLines(path).Length);   // header + 5
        }
        finally { File.Delete(path); }
    }
}

public sealed class StubHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _f;
    public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> f) => _f = f;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => _f(request, ct);
}
