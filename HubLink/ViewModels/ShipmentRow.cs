using HubLink.Models;

namespace HubLink.ViewModels;

public sealed record ShipmentRow(string TrackingNumber, string Origin, string Destination, string Status, string LastUpdated)
{
    public static ShipmentRow From(Shipment s) => new(
        s.TrackingNumber, s.Origin, s.Destination, s.Status,
        s.LastUpdatedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
}
