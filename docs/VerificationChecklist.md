# HubLink - Verification Checklist

Fill in **Status** (Pass / Fail / Not done), **Date** and **Verified by** only after you actually ran the check.
"Test" = `dotnet test`. "Manual" = run the app with the settings shown (edit `appsettings.json`, restart the app).

| ID | Scenario | How verified | Status | Date | By |
|---|---|---|---|---|---|
| Retrieve | BR-001 loads 5 shipments from the real API | Manual: defaults. Test: `Valid_json_is_parsed_into_shipments` | | | |
| Filter | Status filter works without a new request | Manual: choose each status, then All. Test: `Filter_returns_only_selected_status...` | | | |
| Summary | Counts: Total 5, Pending 1, InTransit 2, Delivered 1, Exception 1 | Manual: BR-001. Test: `Summary_counts_match_data` | | | |
| Empty | BR-999 shows a "no shipments" message (not an error) | Manual: `BranchCode=BR-999`. Test: `Empty_result_is_success_not_failure` | | | |
| HTTP error | 400 and 500 shown clearly | Manual: `EndpointPath=/api/test/shipments`, `ExtraQuery=mode=bad-request` / `mode=server-error` | | | |
| Invalid JSON | Understandable message, no crash | Manual: `mode=invalid-json`. Test: `Incomplete_json_...` | | | |
| Timeout | Message after ~8 s, UI stays responsive | Manual: `mode=delay` | | | |
| Unreachable | Clear message when service stopped | Manual: close the service window, click Refresh | | | |
| Retry / stale data | Retry works; old data labelled as previously loaded | Manual: load OK, stop service, Refresh (orange notice), restart service, Retry | | | |
| No overlap | Refresh disabled while loading | Manual: `mode=delay`, observe the button | | | |
| CSV | Columns/order correct, commas and quotes escaped, matches filtered list | Manual: export, open in Notepad + Excel. Test: `Csv_has_header_...` | | | |
| Export failure | Failure shown, not reported as success | Manual: save to a locked/protected location. Test: `Export_failure_is_reported_not_hidden` | | | |
| Export empty | Clear message, no file | Manual: BR-999 or a status with 0 rows | | | |
| Config | Base URL / branch changed without code edits | Manual: change `appsettings.json` | | | |

Incomplete items / notes:
- 
