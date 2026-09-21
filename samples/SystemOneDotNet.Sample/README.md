# SystemOneDotNet.Sample

A runnable console app that shows how to use the `SystemOneDotNet` client against the live
TypeSafe System One API.

## Run

The sample makes real API calls, so it needs a real key:

```bash
export SYSTEMONE_API_KEY="your-key"
dotnet run --project samples/SystemOneDotNet.Sample
```

Optional environment variables:

| Variable | Purpose |
| --- | --- |
| `SYSTEMONE_API_KEY` | Required. Sent as a bearer token on every request. |
| `SYSTEMONE_MODEL` | Optional. Model that handles requests. Defaults to `jev-latest`. |

Pass a scenario name to run only that part:

```bash
dotnet run --project samples/SystemOneDotNet.Sample -- quickstart
dotnet run --project samples/SystemOneDotNet.Sample -- --help
```

## Scenarios

| Scenario | Shows |
| --- | --- |
| `quickstart` | `ChoiceAsync`, `ScoreAsync`, and `NoulAsync`, plus answer confidences and probability distributions. |
| `structured-options` | POCO choice options sent as structured criteria, and reading members that were never sent to the model. |
| `batch` | Reusable `Question.Choice<T>`, `Question.Score`, and `Question.Noul` questions sent in one request, with typed answers, `Model`, and token usage. |
| `named-options` | `Question.NamedChoice` with named options and descriptions, matching the TypeSafe quickstart. |
| `errors` | Local validation without a request, cancellation, and the API exception types. |

## Files

| File | Contains |
| --- | --- |
| `Program.cs` | Key/model setup, Ctrl+C handling, scenario dispatch, and top-level error handling. |
| `Scenarios.cs` | The demonstrations and the scenario catalog. |
| `Models.cs` | The example `Ticket` state and structured `Team` options. |
| `Scenario.cs` | The scenario record. |
| `Output.cs` | Console formatting and usage text. |
