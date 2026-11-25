using Avalonia.Controls;
using Avalonia.Platform.Storage;
using HubLink.Services;

namespace HubLink.Views;

public sealed class AvaloniaFileSaveService : IFileSaveService
{
    public Window? Owner { get; set; }

    public async Task<string?> PickCsvPathAsync(string suggestedFileName)
    {
        if (Owner is null) return null;
        var file = await Owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export shipments to CSV",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "csv",
            FileTypeChoices = new[] { new FilePickerFileType("CSV file") { Patterns = new[] { "*.csv" } } }
        });
        return file?.TryGetLocalPath();
    }
}
