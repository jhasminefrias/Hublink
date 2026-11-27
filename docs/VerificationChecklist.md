# HubLink - Verification Checklist

Fill in **Status** (Pass / Fail / Not done) and **Date** only after you actually ran the check.
"Test" = `dotnet test`. "Manual" = run the app with the settings shown (edit `appsettings.json`, restart the app).

| ID | Scenario | How verified | Status | Date |
|---|---|---|---|---|
| Retrieve | BR-001 loads 5 shipments from the real API | Manual: defaults. Test: `Valid_json_is_parsed_into_shipments` | Pass | 2026-11-26 |
| Filter | Status filter works without a new request | Manual: choose each status, then All. Test: `Filter_returns_only_selected_status...` | Pass | 2026-11-25 |
| Summary | Counts: Total 5, Pending 1, InTransit 2, Delivered 1, Exception 1 | Manual: BR-001. Test: `Summary_counts_match_data` | Pass | 2026-11-25 |
| Empty | BR-999 shows a "no shipments" message (not an error) | Manual: `BranchCode=BR-999`. Test: `Empty_result_is_success_not_failure` | Pass | 2026-11-25 |
| HTTP error | 400 and 500 shown clearly | Manual: `EndpointPath=/api/test/shipments`, `ExtraQuery=mode=bad-request` / `mode=server-error` | Pass | 2026-11-25 |
| Invalid JSON | Understandable message, no crash | Manual: `mode=invalid-json`. Test: `Incomplete_json_...` | Pass | 2026-11-25 |
| Timeout | Message after ~8 s, UI stays responsive | Manual: `mode=delay` | Pass | 2026-11-25 |
| Unreachable | Clear message when service stopped | Manual: close the service window, click Refresh | Pass | 2026-11-25 |
| Retry / stale data | Retry works; old data labelled as previously loaded | Manual: load OK, stop service, Refresh (orange notice), restart service, Retry | Pass | 2026-11-25 |
| No overlap | Refresh disabled while loading | Manual: `mode=delay`, observe the button | Pass | 2026-11-25 |
| CSV | Columns/order correct, commas and quotes escaped, matches filtered list | Manual: export, open in Notepad + Excel. Test: `Csv_has_header_...` | Pass | 2026-11-26 |
| Export failure | Failure shown, not reported as success | Manual: save to a locked/protected location. Test: `Export_failure_is_reported_not_hidden` | Pass | 2026-11-25 |
| Export empty | Clear message, no file | Manual: BR-999 or a status with 0 rows | Pass | 2026-11-25 |
| Config | Base URL / branch changed without code edits | Manual: change `appsettings.json` | Pass | 2026-11-25 |

Incomplete items / notes:
- **Sample Export File**: `samples/EXPORTED_CSV.txt` was initially committed as an empty file (commit `ff10d7f`). The current uncommitted local file contains tab-delimited text (`\t`) instead of standard comma-separated RFC 4180 format with escaped quotes; it should be exported directly from the app or generated via `CsvExportService` and committed.
- **README Metadata**: `README.md` still contains placeholder values for team members (`**Team:** <Member 1> and <Member 2>`) and AI acknowledgments (`<Add anything else you used or changed.>`).
- **External Mock Service**: Automated unit tests (`dotnet test`) pass all 15 scenarios. Interactive manual UI checks (e.g., verifying button disabling during delay, observing the orange stale data banner during service restarts) require running the external instructor package (`Start-HubLink.cmd` / `Start-HubLink-5081.cmd`).
