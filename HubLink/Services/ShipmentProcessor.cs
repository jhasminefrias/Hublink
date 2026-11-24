using HubLink.Models;

namespace HubLink.Services;

/// <summary>Pure filtering + summary logic (OR-04). Works only on retrieved data.</summary>
public static class ShipmentProcessor
{
    public static IEnumerable<Shipment> Filter(IEnumerable<Shipment> shipments, string? status) =>
        string.IsNullOrEmpty(status) || status == ShipmentStatus.All
            ? shipments
            : shipments.Where(s => s.Status == status);

    public static ShipmentSummary Summarize(IReadOnlyCollection<Shipment> shipments) => new(
        shipments.Count,
        shipments.Count(s => s.Status == ShipmentStatus.Pending),
        shipments.Count(s => s.Status == ShipmentStatus.InTransit),
        shipments.Count(s => s.Status == ShipmentStatus.Delivered),
        shipments.Count(s => s.Status == ShipmentStatus.Exception));
}
