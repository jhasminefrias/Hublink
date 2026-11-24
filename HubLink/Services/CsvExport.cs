using System.Globalization;
using System.Text;
using HubLink.Models;

namespace HubLink.Services;

public static class CsvBuilder
{
    public const string Header = "TrackingNumber,Origin,Destination,Status,LastUpdatedUtc";

    public static string Build(IEnumerable<Shipment> shipments)
    {
        var sb = new StringBuilder();
        sb.Append(Header).Append("\r\n");
        foreach (var s in shipments)
        {
            sb.Append(Escape(s.TrackingNumber)).Append(',')
              .Append(Escape(s.Origin)).Append(',')
              .Append(Escape(s.Destination)).Append(',')
              .Append(Escape(s.Status)).Append(',')
              .Append(s.LastUpdatedUtc.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture))
              .Append("\r\n");
        }
        return sb.ToString();
    }

    // RFC 4180: quote fields containing comma, quote or line breaks; double any quotes inside.
    public static string Escape(string value) =>
        value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
}

public sealed class CsvExportService : ICsvExportService
{
    public async Task<ExportResult> ExportAsync(string path, IReadOnlyList<Shipment> shipments, CancellationToken ct = default)
    {
        if (shipments.Count == 0)
            return new ExportResult(false, "There are no shipments to export.");
        try
        {
            await File.WriteAllTextAsync(path, CsvBuilder.Build(shipments), new UTF8Encoding(false), ct);
            return new ExportResult(true, $"Exported {shipments.Count} shipment(s) to {path}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException
                                      or NotSupportedException or System.Security.SecurityException)
        {
            return new ExportResult(false, $"The CSV file could not be saved: {ex.Message}");
        }
    }
}
