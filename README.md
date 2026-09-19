# JevDotNet

A .NET Standard 2.0 client library for the [Jev / TypeSafe System One](https://docs.typesafe.ai) API.

`JevDotNet` wraps the `POST https://api.typesafe.ai/v1/systemone` endpoint with asynchronous,
cancellable calls, strongly typed questions and answers, and local validation of choice limits.

## Install

```bash
dotnet add package JevDotNet
```

Or reference the project directly:

```xml
<ProjectReference Include="path/to/src/JevDotNet/JevDotNet.csproj" />
```

## Quick start

```csharp
using JevDotNet;

using var jev = new Jev(apiKey);

string ticket = "Hi, I've been trying to connect my Stripe account for 3 days and it keeps failing. I'm losing sales. Please help ASAP.";

// Choose one option.
ChoiceAnswer<string> department = await jev.ChoiceAsync(
    ticket,
    "Which team should handle this?",
    new[] { "billing", "technical", "sales" });

// Rate the state along an ordered scale.
ScoreAnswer frustration = await jev.ScoreAsync(
    ticket,
    "How frustrated is the customer?",
    new[] { "Calm", "Frustrated", "Very angry" });

// Ask a yes/no question.
NoulAnswer urgent = await jev.NoulAsync(
    ticket,
    "Does this message convey urgency?");

Console.WriteLine(department.Choice);       // "billing"
Console.WriteLine(frustration.Score);       // 1.035
Console.WriteLine(urgent.Noul);             // 0.999
```

Every method accepts an optional `CancellationToken`:

```csharp
var answer = await jev.ChoiceAsync(ticket, "Which team?", teams, cancellationToken: ct);
```

## Reusable questions and batches

Questions are reusable, carry no response state, and can be sent together in one request.

```csharp
var department = new Choice<Team>(
    "department",
    "Which team should handle this?", teams);

var urgent = new Noul(
    "is_urgent",
    "Does this message convey urgency?");

var frustration = new Score(
    "frustration",
    "How frustrated is the customer?",
    new[] { "Calm", "Frustrated", "Very Angry" });

JevResult result = await jev.Query(ticket)
    .Question(department)
    .Question(urgent)
    .Score(frustration)
    .SendAsync(ct);

Team selected = result.Get(department).Choice;          // the original Team instance
double confidence = result.Get(department).Confidence;
double score = result.Get(frustration).Score;
double probability = result.Get(urgent).Noul;

// Detailed single-question call:
ChoiceAnswer<Team> answer = await jev.AskAsync(ticket, department, ct);
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

Values are mapped back to the original objects, so `Choice<Team>` returns the exact `Team` instance
that was supplied, without requiring equality or dictionary-compatible keys.

## Options

```csharp
var options = new JevOptions
{
    Endpoint = "https://api.typesafe.ai/v1/systemone", // default
    Model = "jev-latest",                              // default
    MaxChoiceProperties = 20,                          // default
};

using var jev = new Jev(apiKey, options, httpClient);
```

* `Endpoint` and `Model` override the API target.
* `MaxChoiceProperties` limits how many serialized properties a single choice option may contain.
* A supplied `HttpClient` is reused and never disposed by `Jev`; when none is supplied, `Jev` owns
  and disposes its internal client.

## Choice options and property limits

Generic choice options are materialized once and each value is sent as its JSON description.
Strings stay strings, enums are serialized by name, and POCOs are sent as structured objects:

```csharp
var teams = new[]
{
    new Team { Name = "Billing", Email = "billing@example.com" },
    new Team { Name = "Technical", Email = "technical@example.com" },
};

var answer = await jev.ChoiceAsync(ticket, "Which team should handle this?", teams);
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
nested properties, dictionary entries, and properties inside array elements. Members marked with
`[JsonIgnore]` are not counted. The limit defaults to 20 and is validated before any HTTP request is
sent; options are never truncated. An oversized option produces an actionable error:

```text
Choice option 2 contains 54 serialized properties; the limit is 20.
Use a dedicated smaller POCO or increase JevOptions.MaxChoiceProperties
when constructing Jev.
```

Named string options are also supported for the criteria dictionary from the TypeSafe quickstart:

```csharp
var department = new ChoiceQuestion("department", "Which team should handle this?",
    new Dictionary<string, string?>
    {
        ["billing"] = "Payment or subscription issues",
        ["technical"] = "Bugs or integration problems",
        ["sales"] = null,
    });

var result = await jev.Query(ticket).Question(department).SendAsync();
string selected = result.Get(department).Choice;
```

## Error handling

* `JevValidationException` — local validation failed and no request was sent: missing or duplicate
  question ids, empty batches, fewer than two score levels, more than 255 choice options, null or
  cyclic options, or an exceeded property limit.
* `JevApiException` — the API returned an unsuccessful status code. `StatusCode` and `ResponseBody`
  are exposed.
* `JevProtocolException` — the API returned a successful response that is malformed, missing an
  answer, has a mismatched answer type, or contains an unknown option id.
* `OperationCanceledException` — cancellation was requested. The original `CancellationToken` is
  propagated unchanged.

The client performs no automatic retries, caching, or blocking calls.

## Building and verifying

```bash
dotnet build -c Release
dotnet test
dotnet run --project tests/JevDotNet.Verification
dotnet pack src/JevDotNet -c Release
```

* `tests/JevDotNet.Tests` contains the unit tests, written with xUnit, AwesomeAssertions, and
  NSubstitute.
* `tests/JevDotNet.Verification` is a small runnable verification program that exercises the library
  through a fake `HttpMessageHandler`, without a test-framework dependency.

## License

MIT. See [LICENSE](LICENSE).
