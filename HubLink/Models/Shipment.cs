namespace HubLink.Models;

public static class ShipmentStatus
{
    public const string All = "All";
    public const string Pending = "Pending";
    public const string InTransit = "InTransit";
    public const string Delivered = "Delivered";
    public const string Exception = "Exception";
}

public sealed record Shipment(string TrackingNumber, string Origin, string Destination, string Status, DateTimeOffset LastUpdatedUtc);

public sealed record ShipmentSummary(int Total, int PendingCount, int InTransitCount, int DeliveredCount, int ExceptionCount)
{
    public static ShipmentSummary Empty { get; } = new(0, 0, 0, 0, 0);
}
