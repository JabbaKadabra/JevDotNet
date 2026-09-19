# Jev .NET Standard 2.0 wrapper

## Public API

Create a `JevDotNet` library targeting `netstandard2.0`, with asynchronous calls and optional `CancellationToken` parameters throughout.

```csharp
using JevDotNet;

using var jev = new Jev(apiKey);

Team team = await jev.ChoiceAsync(
    ticket, "Which team should handle this?", teams,
    cancellationToken: ct);

double frustration = await jev.ScoreAsync(
    ticket, "How frustrated is the customer?",
    new[] { "Calm", "Frustrated", "Very angry" },
    cancellationToken: ct);

double urgency = await jev.NoulAsync(
    ticket, "Does this message convey urgency?",
    cancellationToken: ct);
```

Reusable questions provide detailed answers and typed batch retrieval:

```csharp
var department = new Choice<Team>(
    "department",
    "Which team should handle this?", teams);

var urgent = new Noul(
    "is_urgent",
    "Does this message convey urgency?");

var frustration = new Score(
    "frustration",
    "How frustrated is the customer?", ["Calm", "Frustrated", "Very Angry"]);

var result = await jev.Query(ticket)
    .Question(department)
    .Question(urgent)
    .Score(frustration)
    .SendAsync(ct);

Team selected = result.Get(department).Choice;
double confidence = result.Get(department).Confidence;
double score = result.Get(frustration).Score;
double probability = result.Get(urgent).Noul;

// Detailed single-question call:
var answer = await jev.AskAsync(ticket, department, ct);
```

## Questions and results

- Define `IQuestion` and `IQuestion<TAnswer>` with concrete `ChoiceQuestion<T>`, `ScoreQuestion`, and `NoulQuestion` types.
- `Get<TAnswer>(IQuestion<TAnswer>)` infers the answer type. Reject duplicate question IDs and repeated handles within one batch.
- `ChoiceAnswer<T>` exposes the original selected value, confidence, and an ordered list of typed option/probability pairs. This avoids requiring unique object equality or dictionary-compatible keys.
- `ScoreAnswer` exposes the fractional score, legend, confidence, and probabilities when supplied. `NoulAnswer` exposes a `double` probability without automatic boolean conversion.
- Batch results retain model and token usage. These shapes follow the [API reference](https://docs.typesafe.ai/api); tolerate absent Score probabilities because the [quickstart](https://docs.typesafe.ai/introduction/quickstart) omits them.
- Include a non-generic `ChoiceQuestion` for named string options with descriptions, matching the quickstart’s criteria dictionary.

## Serialization and choice limits

- Accept string, POCO, or array state. Use strings for instructions and Score levels in v1; Noul supports optional yes/no descriptions.
- Materialize generic choices once, assign deterministic internal option IDs, and serialize each value as its description. Preserve strings as strings and serialize enums by name.
- Send POCO descriptions as JSON objects, supported by Jev’s [structured criteria](https://docs.typesafe.ai/primitives/advanced). Map responses to original values rather than deserializing `T`.
- Use System.Text.Serialization over Newtonsoft
- Default `JevOptions.MaxChoiceProperties` to **20**, counted separately across each choice’s serialized object tree. Count nested properties, dictionary entries, and properties within array elements; ignored members do not count. This is a property limit, not a text-length limit.
- Reject excessive choices before HTTP submission; never truncate. The exception identifies the option, count, limit, and both remedies:

  ```text
  Choice option 2 contains 54 serialized properties; the limit is 20.
  Use a dedicated smaller POCO or increase JevOptions.MaxChoiceProperties
  when constructing Jev.
  ```

- Require a positive override. Reject null choices, cyclic graphs, empty choice collections, and more than the documented [255 options](https://docs.typesafe.ai/primitives/choice).

## Transport and failure handling

- Default to `https://api.typesafe.ai/v1/systemone` and `jev-latest`; allow endpoint/model overrides through `JevOptions`.
- Support `new Jev(apiKey, options: ..., httpClient: ...)`. Reuse one client; dispose only internally owned clients. Set bearer authentication per request.
- Route direct methods and batches through one request/response implementation. Pass cancellation through HTTP transmission and response buffering; propagate cancellation unchanged.
- Validate required inputs, nonempty batches, and at least two Score levels before sending.
- Throw `JevApiException` with HTTP status and response body for unsuccessful responses. Reject malformed successful responses, missing answers, mismatched answer types, and unknown choice IDs explicitly.
- Keep question definitions free of response state. Snapshot inputs at send time; builders are not thread-safe.
- No blocking methods, automatic retries, caching, or custom transport abstraction in v1.

## Verification and deliverables

Deliver the library, README examples, and one small runnable verification project using a fake `HttpMessageHandler`, without a test-framework dependency.
Also make sure to write unit-tests. use awesomeassertions and nsubstitute.

Verify:

- Direct and mixed requests, authentication, model selection, and typed metadata.
- Returned `Team` reference identity and independent concurrent requests.
- Property limits at 10 and 11, nested objects/arrays, ignored properties, overrides, and actionable errors.
- Cancellation before and during HTTP work, unsuccessful responses, malformed answers, and client ownership.
- Local validation failures send no request.
- Release build, verification runner, and local NuGet pack succeed.

No live API key or package publication is required for acceptance.
