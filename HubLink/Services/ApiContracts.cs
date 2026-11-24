using HubLink.Models;

namespace HubLink.Services;

// JSON shape returned by the API (nullable so missing fields can be detected).
internal sealed class ShipmentResponseDto
{
    public string? BranchCode { get; set; }
    public List<ShipmentDto?>? Shipments { get; set; }
}

internal sealed class ShipmentDto
{
    public string? TrackingNumber { get; set; }
    public string? BranchCode { get; set; }
    public string? Origin { get; set; }
    public string? Destination { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset? LastUpdatedUtc { get; set; }
}

public enum FetchFailureKind { None, Configuration, Unreachable, Timeout, HttpError, InvalidResponse }

public sealed record ShipmentFetchResult(bool IsSuccess, IReadOnlyList<Shipment> Shipments, FetchFailureKind Failure, string? ErrorMessage)
{
    public static ShipmentFetchResult Succeeded(IReadOnlyList<Shipment> s) => new(true, s, FetchFailureKind.None, null);
    public static ShipmentFetchResult Failed(FetchFailureKind kind, string message) => new(false, Array.Empty<Shipment>(), kind, message);
}

public interface IShipmentApiClient
{
    string RequestUrl { get; }
    Task<ShipmentFetchResult> GetShipmentsAsync(CancellationToken ct = default);
}

public sealed record ExportResult(bool Success, string Message);

public interface ICsvExportService
{
    Task<ExportResult> ExportAsync(string path, IReadOnlyList<Shipment> shipments, CancellationToken ct = default);
}

public interface IFileSaveService
{
    Task<string?> PickCsvPathAsync(string suggestedFileName);
}
