using System.Collections.ObjectModel;
using HubLink.Configuration;
using HubLink.Models;
using HubLink.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HubLink.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IShipmentApiClient _api;
    private readonly ICsvExportService _csv;
    private readonly IFileSaveService _fileSave;

    private List<Shipment> _all = new();       // everything successfully retrieved (drives the summary)
    private List<Shipment> _visible = new();   // after the status filter (drives the list AND the export)
    private bool _hasLoadedSuccessfully;
    private DateTime? _lastLoadedAt;

    public MainWindowViewModel(IShipmentApiClient api, ICsvExportService csv, IFileSaveService fileSave, ServiceSettings settings)
    {
        _api = api;
        _csv = csv;
        _fileSave = fileSave;
        BranchCode = settings.BranchCode;
        RequestUrl = api.RequestUrl;
    }

    public string BranchCode { get; }
    public string RequestUrl { get; }
    public IReadOnlyList<string> StatusOptions { get; } = new[]
    {
        ShipmentStatus.All, ShipmentStatus.Pending, ShipmentStatus.InTransit, ShipmentStatus.Delivered, ShipmentStatus.Exception
    };
    public ObservableCollection<ShipmentRow> Shipments { get; } = new();

    [ObservableProperty] private string _selectedStatus = ShipmentStatus.All;
    [ObservableProperty] private ShipmentSummary _summary = ShipmentSummary.Empty;
    [ObservableProperty] private string _statusMessage = "Ready.";
    [ObservableProperty] private bool _isError;

    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportCsvCommand))]
    [ObservableProperty] private bool _isLoading;

    [NotifyPropertyChangedFor(nameof(RefreshButtonText))]
    [ObservableProperty] private bool _lastRefreshFailed;

    [NotifyPropertyChangedFor(nameof(HasStaleNotice))]
    [ObservableProperty] private string _staleNotice = "";

    [NotifyPropertyChangedFor(nameof(HasEmptyListText))]
    [ObservableProperty] private string _emptyListText = "";

    public string RefreshButtonText => LastRefreshFailed ? "Retry" : "Refresh";
    public bool HasStaleNotice => !string.IsNullOrEmpty(StaleNotice);
    public bool HasEmptyListText => !string.IsNullOrEmpty(EmptyListText);

    partial void OnSelectedStatusChanged(string value) => ApplyFilter();   // local only, no HTTP request

    public Task InitializeAsync() => RefreshAsync();

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RefreshAsync()
    {
        if (IsLoading) return;   // never allow overlapping requests
        IsLoading = true;
        SetStatus("Retrieving shipments...", false);
        try
        {
            var result = await _api.GetShipmentsAsync();
            if (result.IsSuccess)
            {
                _all = result.Shipments.ToList();
                _lastLoadedAt = DateTime.Now;
                _hasLoadedSuccessfully = true;
                LastRefreshFailed = false;
                StaleNotice = "";
                Summary = ShipmentProcessor.Summarize(_all);
                ApplyFilter();
                SetStatus(_all.Count == 0
                    ? $"The service responded successfully, but returned no shipments for branch {BranchCode}."
                    : $"Retrieved {_all.Count} shipment(s) for branch {BranchCode} at {_lastLoadedAt:HH:mm:ss}.", false);
            }
            else
            {
                LastRefreshFailed = true;
                SetStatus($"{result.ErrorMessage} Use Retry to try again.", true);
                // Old data stays visible only if it is clearly labelled as old.
                StaleNotice = _all.Count > 0
                    ? $"Showing PREVIOUSLY LOADED data from {_lastLoadedAt:HH:mm:ss}. The latest refresh failed, so this may be out of date."
                    : "";
            }
        }
        finally { IsLoading = false; }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task ExportCsvAsync()
    {
        if (_visible.Count == 0)
        {
            SetStatus("There are no shipments to export. Retrieve data or change the status filter first.", true);
            return;
        }

        var name = $"shipments_{BranchCode}_{SelectedStatus}_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        var path = await _fileSave.PickCsvPathAsync(name);
        if (path is null) { SetStatus("Export cancelled.", false); return; }

        var result = await _csv.ExportAsync(path, _visible.ToList());   // exactly what is displayed (OR-08)
        SetStatus(result.Message, !result.Success);
    }

    private bool CanRun() => !IsLoading;

    private void ApplyFilter()
    {
        _visible = ShipmentProcessor.Filter(_all, SelectedStatus).ToList();
        Shipments.Clear();
        foreach (var s in _visible) Shipments.Add(ShipmentRow.From(s));

        EmptyListText = !_hasLoadedSuccessfully ? ""
            : _all.Count == 0 ? "The service returned no shipments for this branch."
            : _visible.Count == 0 ? $"No shipments with status \"{SelectedStatus}\"."
            : "";
    }

    private void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        IsError = isError;
    }
}
