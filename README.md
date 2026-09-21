# SystemOneDotNet

A .NET Standard 2.0 client library for the [TypeSafe System One](https://docs.typesafe.ai) API. System One is the class of models that Jev belongs to; the default model alias is `jev-latest`.

`SystemOneDotNet` wraps the `POST https://api.typesafe.ai/v1/systemone` endpoint with asynchronous,
cancellable calls, strongly typed questions and answers, and local validation of choice limits.

## Install

```bash
dotnet add package SystemOneDotNet
```

Or reference the project directly:

```xml
<ProjectReference Include="path/to/src/SystemOneDotNet/SystemOneDotNet.csproj" />
```

## Quick start

```csharp
using SystemOneDotNet;
using SystemOneDotNet.Answers;

using ISystemOneClient systemOne = SystemOneClient.Create(apiKey);

string ticket = "Hi, I've been trying to connect my Stripe account for 3 days and it keeps failing. I'm losing sales. Please help ASAP.";

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

// Ask a yes/no question.
NoulAnswer urgent = await systemOne.NoulAsync(
    ticket,
    "Does this message convey urgency?");

Console.WriteLine(department.Choice);       // "billing"
Console.WriteLine(frustration.Score);       // 1.035
Console.WriteLine(urgent.Noul);             // 0.999
```

Every method accepts an optional `CancellationToken`:

```csharp
var answer = await systemOne.ChoiceAsync(ticket, "Which team?", teams, cancellationToken: ct);
```

## Reusable questions and batches

Questions are created with the `Question` factory, are reusable, carry no response state, and can be
sent together in one request.

```csharp
using SystemOneDotNet.Questions;

IChoiceQuestion<Team> department = Question.Choice(
    "department",
    "Which team should handle this?", teams);

INoulQuestion urgent = Question.Noul(
    "is_urgent",
    "Does this message convey urgency?");

IScoreQuestion frustration = Question.Score(
    "frustration",
    "How frustrated is the customer?",
    new[] { "Calm", "Frustrated", "Very Angry" });

ISystemOneResult result = await systemOne.Query(ticket)
    .Question(department)
    .Question(urgent)
    .Score(frustration)
    .SendAsync(ct);

Team selected = result.Get(department).Choice;          // the original Team instance
double confidence = result.Get(department).Confidence;
double score = result.Get(frustration).Score;
double probability = result.Get(urgent).Noul;

// Detailed single-question call:
ChoiceAnswer<Team> answer = await systemOne.AskAsync(ticket, department, ct);
```

`result.Model` and `result.Usage` (input and output tokens) are available for every batch.

### Answers

* `ChoiceAnswer<T>.Choice` is the original selected value; `.Options` is an ordered list of
  `ChoiceProbability<T>` pairs with the original values and their probabilities; `.Confidence`
  is the model's confidence.
* `ScoreAnswer.Score` is the probability-weighted value, `.Legend` is the ordered level list,
  `.Probabilities` is populated when the API supplies the distribution, and `.Confidence` is the
  model's confidence.
* `NoulAnswer.Noul` is the probability that the answer is yes, from 0 to 1. There is no automatic
  boolean conversion.

Values are mapped back to the original objects, so `Question.Choice<Team>` returns the exact `Team`
instance that was supplied, without requiring equality or dictionary-compatible keys.

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
  and disposes its internal `HttpClient`.

## Choice options and property limits

Generic choice options are materialized once and each value is sent as its JSON description.
Strings stay strings, enums are serialized by name, and POCOs are sent as structured objects:

```csharp
var teams = new[]
{
    new Team { Name = "Billing", Email = "billing@example.com" },
    new Team { Name = "Technical", Email = "technical@example.com" },
};

var answer = await systemOne.ChoiceAsync(ticket, "Which team should handle this?", teams);
```

The API receives the options as a criteria map with deterministic ids:

```json
{
  "type": "choice",
  "instructions": "Which team should handle this?",
  "criteria": {
    "0": { "Name": "Billing", "Email": "billing@example.com" },
    "1": { "Name": "Technical", "Email": "technical@example.com" }
  }
}
```

`MaxChoiceProperties` counts the properties in each option's serialized object tree, including
nested properties, dictionary entries, and properties inside array elements. JSON object/array
values (`JsonNode`, `JsonElement`) are counted exactly like their serialized form. Members marked
with `[JsonIgnore]` are not counted. The limit defaults to 20 and is validated before any HTTP
request is sent; options are never truncated. An oversized option produces an actionable error:

```text
Choice option 2 contains 54 serialized properties; the limit is 20.
Use a dedicated smaller POCO or increase SystemOneOptions.MaxChoiceProperties
when creating the client.
```

Named string options are also supported for the criteria dictionary from the TypeSafe quickstart:

```csharp
INamedChoiceQuestion department = Question.NamedChoice("department", "Which team should handle this?",
    new Dictionary<string, string?>
    {
        ["billing"] = "Payment or subscription issues",
        ["technical"] = "Bugs or integration problems",
        ["sales"] = null,
    });

var result = await systemOne.Query(ticket).Question(department).SendAsync();
string selected = result.Get(department).Choice;
```

## Error handling

All exceptions live in `SystemOneDotNet.Exceptions` and derive from `SystemOneException`.

* `SystemOneValidationException` — local validation failed and no request was sent: missing or duplicate
  question ids, empty batches, fewer than two score levels, more than 255 choice options, null or
  cyclic options, an exceeded property limit, or a question that was not created by the `Question` factory.
* `SystemOneApiException` — the API returned an unsuccessful status code. `StatusCode` and `ResponseBody`
  are exposed.
* `SystemOneProtocolException` — the API returned a successful response that is malformed, missing an
  answer, has a mismatched answer type, or contains an unknown option id.
* `OperationCanceledException` — cancellation was requested. The original `CancellationToken` is
  propagated unchanged.

The client performs no automatic retries, caching, or blocking calls. A state that cannot be
serialized to JSON (for example a cyclic object graph) is reported locally as
`SystemOneValidationException` before any request is sent. To control timeouts, inject an `HttpClient`
configured with your own `Timeout`.

## Runnable sample

[`samples/SystemOneDotNet.Sample`](samples/SystemOneDotNet.Sample/README.md) is a console app that walks through
the library against the live API: direct calls, structured options, batches, named options,
validation, and cancellation. It requires an API key:

```bash
export SYSTEMONE_API_KEY="your-key"
dotnet run --project samples/SystemOneDotNet.Sample            # every scenario
dotnet run --project samples/SystemOneDotNet.Sample -- batch   # one scenario
```

Set `SYSTEMONE_MODEL` to override the default model, or pass `--help` to list the scenarios.

## Conventions and scope

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
* Tests use xUnit, AwesomeAssertions, and NSubstitute.

## Building and verifying

```bash
dotnet build -c Release
dotnet test
dotnet run --project tests/SystemOneDotNet.Verification
dotnet pack src/SystemOneDotNet -c Release
```

* `tests/SystemOneDotNet.Tests` contains the unit tests, written with xUnit, AwesomeAssertions, and
  NSubstitute.
* `tests/SystemOneDotNet.Verification` is a small runnable verification program that exercises the library
  through a fake `HttpMessageHandler`, without a test-framework dependency.

## License

MIT. See [LICENSE](LICENSE).
