# HubLink - Shipment Monitor (ITSD81 Performance Task 5)

Avalonia / MVVM desktop client (C# / .NET 10) that retrieves shipments for one branch from the instructor-provided HubLink API, shows them, filters by status, summarizes counts, and exports the displayed list to CSV.

**Team:** `<Member 1>` and `<Member 2>`

## Requirements
- .NET 10 SDK (or the SDK your instructor specifies)
- The instructor's local service package (Windows x64)

## 1. Start the API service
1. Extract the **entire** HubLink-PT5-Local-Service ZIP to a normal folder.
2. Double-click `Start-HubLink.cmd` and keep the window open (use `Start-HubLink-5081.cmd` if port 5080 is busy).
3. Check in a browser: `http://127.0.0.1:5080/api/shipments?branchCode=BR-001` (should show 5 shipments).

## 2. Configure the client
Edit `HubLink/appsettings.json` (no code changes needed):

| Setting | Default | Purpose |
|---|---|---|
| `ServiceBaseUrl` | `http://127.0.0.1:5080` | Service address (use `5081` if you started that script) |
| `EndpointPath` | `/api/shipments` | Normal endpoint; use `/api/test/shipments` for test modes |
| `BranchCode` | `BR-001` | Branch to retrieve (`BR-001`, `BR-002`, `BR-003`, `BR-999` = empty) |
| `ExtraQuery` | *(empty)* | e.g. `mode=server-error` with the test endpoint |
| `TimeoutSeconds` | `8` | Client timeout (keep below 10 to test `mode=delay`) |

Optional environment variables `HUBLINK_BASEURL` and `HUBLINK_BRANCH` override the file.

## 3. Build and run
```powershell
dotnet restore
dotnet build
dotnet run --project HubLink
dotnet test
```
No database or EF Core migrations are used in this task.

## Project layout
| Folder | Responsibility |
|---|---|
| `Configuration` | Settings loading |
| `Models` | `Shipment`, `ShipmentSummary`, status constants |
| `Services` | `ShipmentApiClient` (HTTP + JSON + error handling), `ShipmentProcessor` (filter + summary), `CsvBuilder`/`CsvExportService` |
| `ViewModels` / `Views` | MVVM + XAML (no HTTP or file logic in Views) |

Docs: `docs/VerificationChecklist.md`. Sample export: `samples/`.

## Acknowledgments
AI assistance (Claude) was used to generate the initial project structure and code. We reviewed, ran, and understand the submitted work. `<Add anything else you used or changed.>`
