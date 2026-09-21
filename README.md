# SystemOneDotNet

[![NuGet version](https://img.shields.io/nuget/v/SystemOneDotNet.svg)](https://www.nuget.org/packages/SystemOneDotNet)
[![NuGet downloads](https://img.shields.io/nuget/dt/SystemOneDotNet.svg)](https://www.nuget.org/packages/SystemOneDotNet)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

**Structured AI decisions for .NET — no prompt engineering, no output parsing.**

`SystemOneDotNet` is the .NET client for [TypeSafe System One](https://docs.typesafe.ai), the class of models that Jev belongs to. System One models do not write prose. They evaluate a **state** against typed **questions** and return decisions, probabilities, and calibrated confidence that your code can branch on directly:

| Ask the model | Get back | C# answer type |
| --- | --- | --- |
| **Choice** — pick one option | the chosen option, every option's probability, overall confidence | `ChoiceAnswer<T>` |
| **Score** — rate the state on an ordered scale | a probability-weighted score, the legend, probabilities, confidence | `ScoreAnswer` |
| **Noul** — is the statement true? | the probability that the answer is yes, from `0` to `1` | `NoulAnswer` |

You declare the answer space in C# and get C# values back. One request can carry several questions, and every question is evaluated in parallel and in isolation against the same state — adding questions barely changes the response time. System One is a natural fit for triage, routing, moderation, lead scoring, and any decision you would otherwise hand-roll with regex or a generic LLM prompt. The default model alias is `jev-latest`.

## Install

```bash
dotnet add package SystemOneDotNet
```

That is the whole setup. The package targets **.NET Standard 2.0**, so it runs on .NET Framework 4.6.2+, .NET Core 2.0+, and every modern .NET release. Its only dependency is `System.Text.Json`.

Grab an API key from the [TypeSafe console](https://console.typesafe.ai/keys). If you want to see the model answer before writing any code, try the [Playground](https://console.typesafe.ai/playground) first. Then make your first call:

```csharp
using SystemOneDotNet;
using SystemOneDotNet.Answers;

string apiKey = Environment.GetEnvironmentVariable("SYSTEMONE_API_KEY")
    ?? throw new InvalidOperationException("Set SYSTEMONE_API_KEY.");

using ISystemOneClient systemOne = SystemOneClient.Create(apiKey);

string ticket =
    "Hi, I've been trying to connect my Stripe account for 3 days and it keeps failing. " +
    "I'm losing sales. Please help ASAP.";

// Choose one option.
ChoiceAnswer<string> department = await systemOne.ChoiceAsync(
    ticket,
    "Which team should handle this?",
    new[] { "billing", "technical", "sales" });

// Rate the state along an ordered scale.
ScoreAnswer frustration = await systemOne.ScoreAsync(
    ticket,
    "How frustrated is the customer?",
    new[] { "Calm", "Frustrated", "Very angry" });

// Ask a yes/no question. The answer is a probability, never a bool.
NoulAnswer urgent = await systemOne.NoulAsync(
    ticket,
    "Does this message convey urgency?");

Console.WriteLine(department.Choice);       // e.g. "technical"
Console.WriteLine(department.Confidence);   // e.g. 0.78
Console.WriteLine(frustration.Score);       // e.g. 1.0
Console.WriteLine(urgent.Noul);             // e.g. 0.98
```

Every method accepts an optional `CancellationToken`:

```csharp
var answer = await systemOne.ChoiceAsync(ticket, "Which team?", teams, cancellationToken: ct);
```

## One request, many answers

Questions are created with the `Question` factory. They are immutable, validated once, carry no response
state, and can be reused across any number of requests. Send as many as you like in a single request — they
are evaluated in parallel against the same state, so adding questions barely changes the response time:

```csharp
using SystemOneDotNet.Questions;

IChoiceQuestion<Team> department = Question.Choice(
    "department",
    "Which team should handle this?",
    teams);

INoulQuestion urgent = Question.Noul(
    "is_urgent",
    "Does this message convey urgency?",
    trueDescription: "The customer is blocked or losing money.",
    falseDescription: "The request can wait until tomorrow.");

IScoreQuestion frustration = Question.Score(
    "frustration",
    "How frustrated is the customer?",
    new[] { "Calm", "Frustrated", "Very angry" });

ISystemOneResult result = await systemOne.Query(ticket)
    .Question(department)
    .Question(urgent)
    .Score(frustration)
    .SendAsync(ct);

// Read the typed answers back with the same question handles.
ChoiceAnswer<Team> routing = result.Get(department);
double routed = routing.Confidence;
double score = result.Get(frustration).Score;
double probability = result.Get(urgent).Noul;

// Every batch reports the model and the token usage.
Console.WriteLine(result.Model);          // "jev-1.x.y"
Console.WriteLine(result.Usage.InputTokens);
Console.WriteLine(result.Usage.OutputTokens);
```

Questions can be defined once next to your domain types and shared by every request. `AskAsync` is the
single-question shortcut for `Query(...).Question(...).SendAsync()`:

```csharp
ChoiceAnswer<Team> answer = await systemOne.AskAsync(ticket, department, ct);
```

## Use your own types as options

Choice options do not have to be strings. Pass any values — POCOs, enums, or records — and each option is
sent to the model as its JSON description, so the model reasons about the fields instead of opaque labels:

```csharp
using System.Text.Json.Serialization;

public sealed record Team
{
    public string Name { get; init; }
    public string Email { get; init; }

    [JsonIgnore]
    public string SlackChannel { get; init; }   // never sent to the model
}

var teams = new[]
{
    new Team { Name = "Billing",   Email = "billing@example.com", SlackChannel = "#billing" },
    new Team { Name = "Technical", Email = "tech@example.com",    SlackChannel = "#technical" },
    new Team { Name = "Sales",     Email = "sales@example.com",   SlackChannel = "#sales" },
};

ChoiceAnswer<Team> routing = await systemOne.ChoiceAsync(
    ticket,
    "Which team should handle this?",
    teams);

Console.WriteLine(routing.Choice.Name);          // "Technical"
Console.WriteLine(routing.Choice.SlackChannel);  // "#technical" — your original instance
```

The API receives the options as a criteria map with deterministic ids:

```json
{
  "type": "choice",
  "instructions": "Which team should handle this?",
  "criteria": {
    "0": { "Name": "Billing",   "Email": "billing@example.com" },
    "1": { "Name": "Technical", "Email": "tech@example.com" },
    "2": { "Name": "Sales",     "Email": "sales@example.com" }
  }
}
```

Answers map back to the original values, so `routing.Choice` is the exact `Team` instance that was
supplied — including members that were never sent to the model. No equality overrides or
dictionary-compatible keys required. Strings stay strings, and enums are serialized by name.

### Named options

The named criteria dictionary from the TypeSafe quickstart is supported too, with optional descriptions:

```csharp
INamedChoiceQuestion department = Question.NamedChoice(
    "department",
    "Which team should handle this?",
    new Dictionary<string, string?>
    {
        ["billing"] = "Payment or subscription issues",
        ["technical"] = "Bugs or integration problems",
        ["sales"] = null,
    });

var result = await systemOne.Query(ticket).Question(department).SendAsync();
string selected = result.Get(department).Choice;
```

## Answers you can act on

Every answer carries more than the decision itself, so your code can decide whether to act:

* `ChoiceAnswer<T>.Choice` is the original selected value; `.Options` is an ordered list of
  `ChoiceProbability<T>` pairs with the original values and their probabilities; `.Confidence`
  is the model's confidence.
* `ScoreAnswer.Score` is the probability-weighted value, `.Legend` is the ordered level list,
  `.Probabilities` is populated when the API supplies the distribution, and `.Confidence` is the
  model's confidence.
* `NoulAnswer.Noul` is the probability that the answer is yes, from 0 to 1. There is no automatic
  boolean conversion, so you choose the threshold.

Confidence turns a decision into a policy. Act automatically when the model is sure, and escalate when
it is not:

```csharp
ChoiceAnswer<Team> routing = await systemOne.ChoiceAsync(ticket, "Which team?", teams);

if (routing.Confidence < 0.9)
{
    return EscalateToHuman(ticket);   // not confident enough to route blindly
}

Notify(routing.Choice.SlackChannel);
```

The full distributions are there for ranking and analytics:

```csharp
foreach (var option in routing.Options)
{
    Console.WriteLine($"{option.Probability:P1}  {option.Option.Name}");
}

if (frustration.Probabilities is not null)
{
    foreach (var level in frustration.Probabilities)
    {
        Console.WriteLine($"{level.Probability:P1}  {level.Level}");
    }
}
```

Answers live in `SystemOneDotNet.Answers` and are plain records with public constructors, so a test double
for `ISystemOneClient` can return `new NoulAnswer(0.9)` directly.

## Options

```csharp
var options = new SystemOneOptions
{
    Endpoint = "https://api.typesafe.ai/v1/systemone", // default
    Model = "jev-latest",                              // default
    MaxChoiceProperties = 20,                          // default
};

using ISystemOneClient systemOne = SystemOneClient.Create(apiKey, options, httpClient);
```

* `Endpoint` and `Model` override the API target.
* `MaxChoiceProperties` limits how many serialized properties a single choice option may contain.
* `SystemOneOptions` is immutable: the values are validated once when the client is constructed. Use `with`
  to derive a modified copy, for example `options with { MaxChoiceProperties = 10 }`.
* A supplied `HttpClient` is reused and never disposed by the client; when none is supplied, the client owns
  and disposes its internal `HttpClient`. Use this to configure timeouts or proxies.

### Choice limits

`MaxChoiceProperties` counts the properties in each option's serialized object tree, including nested
properties, dictionary entries, and properties inside array elements. JSON object/array values
(`JsonNode`, `JsonElement`) are counted exactly like their serialized form. Members marked with
`[JsonIgnore]` are not counted.

The limit defaults to 20 and is validated before any HTTP request is sent; options are never truncated.
An oversized option produces an actionable error:

```text
Choice option 2 contains 54 serialized properties; the limit is 20.
Use a dedicated smaller POCO or increase SystemOneOptions.MaxChoiceProperties
when creating the client.
```

## Error handling

Failures surface as exceptions in `SystemOneDotNet.Exceptions`, all deriving from `SystemOneException` (cancellation surfaces as `OperationCanceledException`):

```csharp
try
{
    var answer = await systemOne.ChoiceAsync(ticket, "Which team?", teams);
}
catch (SystemOneValidationException ex) { /* local validation failed; no request was sent */ }
catch (SystemOneApiException ex)        { /* non-success status; ex.StatusCode, ex.ResponseBody */ }
catch (SystemOneProtocolException ex)   { /* malformed or mismatched answer */ }
```

* `SystemOneValidationException` — local validation failed and no request was sent: missing or duplicate
  question ids, empty batches, fewer than two score levels, more than 255 choice options, null or
  cyclic options, an exceeded property limit, or a question that was not created by the `Question` factory.
* `SystemOneApiException` — the API returned an unsuccessful status code. `StatusCode` and `ResponseBody`
  are exposed.
* `SystemOneProtocolException` — the API returned a successful response that is malformed, missing an
  answer, has a mismatched answer type, or contains an unknown option id.
* `OperationCanceledException` — cancellation was requested. The original `CancellationToken` is
  propagated unchanged.

The client performs no automatic retries, caching, or blocking calls. A state that cannot be serialized
to JSON (for example a cyclic object graph) is reported locally as `SystemOneValidationException` before
any request is sent. To control timeouts, inject an `HttpClient` configured with your own `Timeout`.

## Runnable sample

[`samples/SystemOneDotNet.Sample`](samples/SystemOneDotNet.Sample/README.md) is a console app that walks
through the library against the live API: direct calls, structured options, batches, named options,
validation, and cancellation. Clone the repository, set an API key, and run it:

```bash
export SYSTEMONE_API_KEY="your-key"
dotnet run --project samples/SystemOneDotNet.Sample            # every scenario
dotnet run --project samples/SystemOneDotNet.Sample -- batch   # one scenario
```

Set `SYSTEMONE_MODEL` to override the default model, or pass `--help` to list the scenarios.

## Design notes

`SystemOneDotNet` is a standalone client library, not a hosted service, so a few deliberate choices differ
from service-oriented .NET conventions:

* It targets `netstandard2.0` so older runtimes can consume it. `src/SystemOneDotNet/Internal/IsExternalInit.cs`
  supplies the marker type that C# records and `init` accessors require on that target.
* The public surface is interfaces and records only. `ISystemOneClient`, `ISystemOneQuery`,
  `ISystemOneResult`, and the `IQuestion` family are interfaces with internal implementations; answers,
  token usage, and `SystemOneOptions` are immutable records. The two static factories,
  `SystemOneClient.Create` and `Question`, are the only entry points into the internals, and the
  exception hierarchy is the only other set of public classes.
* Namespaces group the surface by role: `SystemOneDotNet` holds the client, batch, result, and options;
  `SystemOneDotNet.Questions` the question interfaces and factory; `SystemOneDotNet.Answers` the answer
  records; `SystemOneDotNet.Exceptions` the exception hierarchy. Everything under
  `SystemOneDotNet.Internal` is an implementation detail.
* Callers depend on `ISystemOneClient` and can substitute it in tests. There is no DI container, hosted
  service, or `ILogger` dependency. `HttpClient` is supplied through `SystemOneClient.Create` instead of
  `IHttpClientFactory`, and the client disposes only the instance it created. Failures surface as the
  `SystemOneException` hierarchy and callers decide how to log them.
* Tests use xUnit, AwesomeAssertions, and NSubstitute; coverage is collected with coverlet.

## Building and verifying

```bash
dotnet build -c Release
dotnet test
dotnet run --project tests/SystemOneDotNet.Verification
dotnet pack src/SystemOneDotNet -c Release
```

Collect coverage locally and enforce the same thresholds the CI pipeline uses:

```bash
dotnet test tests/SystemOneDotNet.Tests -c Release \
  -p:CollectCoverage=true \
  -p:CoverletOutputFormat=cobertura \
  -p:CoverletOutput=./artifacts/coverage/ \
  -p:Threshold=100%2C99 \
  -p:ThresholdType=line%2Cbranch \
  -p:ThresholdStat=total
```

`%2C` escapes the comma inside the MSBuild property values; the threshold order matches `ThresholdType`
(line, then branch). The report is written to `artifacts/coverage/coverage.cobertura.xml`. The unit tests
currently reach 100% line and 100% branch coverage, and CI fails when line coverage drops below 100% or
branch coverage drops below 99%.

* `tests/SystemOneDotNet.Tests` contains the unit tests, written with xUnit, AwesomeAssertions, and
  NSubstitute.
* `tests/SystemOneDotNet.Verification` is a small runnable verification program that exercises the library
  through a fake `HttpMessageHandler`, without a test-framework dependency.
* `.github/workflows/ci.yml` builds the solution, runs the unit tests with the coverage thresholds, runs
  the verification runner, and validates that the library packs on every push to `main` and every pull
  request. The coverage report is uploaded as a workflow artifact and summarized in the run page.

## License

MIT. See [LICENSE](LICENSE).
