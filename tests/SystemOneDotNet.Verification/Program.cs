using System.Net;
using System.Text.Json;
using SystemOneDotNet;
using SystemOneDotNet.Verification;

var runner = new CheckRunner();
Console.WriteLine("SystemOneDotNet verification runner");
Console.WriteLine();

const string BatchPayload = """
{
  "model": "jev-latest",
  "answers": {
    "department": {
      "type": "choice",
      "choice": "1",
      "probabilities": { "0": 0.08, "1": 0.85, "2": 0.07 },
      "confidence": 0.82
    },
    "is_urgent": { "type": "noul", "noul": 0.92 },
    "frustration": {
      "type": "score",
      "score": 1.6,
      "legend": { "0": "Calm", "1": "Frustrated", "2": "Very angry" },
      "probabilities": { "0": 0.05, "1": 0.3, "2": 0.65 },
      "confidence": 0.78
    }
  },
  "usage": { "input_tokens": 312, "output_tokens": 48 }
}
""";

const string SingleChoicePayload = """
{
  "model": "jev-latest",
  "answers": {
    "choice": {
      "type": "choice",
      "choice": "0",
      "probabilities": { "0": 1.0 },
      "confidence": 1.0
    }
  },
  "usage": { "input_tokens": 1, "output_tokens": 1 }
}
""";

const string DirectPayload = """
{
  "model": "jev-latest",
  "answers": {
    "choice": {
      "type": "choice",
      "choice": "1",
      "probabilities": { "0": 0.08, "1": 0.85, "2": 0.07 },
      "confidence": 0.82
    },
    "noul": { "type": "noul", "noul": 0.92 },
    "score": {
      "type": "score",
      "score": 1.6,
      "legend": { "0": "Calm", "1": "Frustrated", "2": "Very angry" },
      "probabilities": { "0": 0.05, "1": 0.3, "2": 0.65 },
      "confidence": 0.78
    }
  },
  "usage": { "input_tokens": 312, "output_tokens": 48 }
}
""";

var teams = new[]
{
    new Team("Billing", "billing@example.com"),
    new Team("Technical", "technical@example.com"),
    new Team("Sales", "sales@example.com"),
};

// ---------------------------------------------------------------------------
// Direct and mixed requests, authentication, model selection, typed metadata.
// ---------------------------------------------------------------------------

await runner.CheckAsync("direct choice request sends auth, model, and question", async () =>
{
    var handler = FakeHandler.Json(DirectPayload);
    using var systemOne = new SystemOne("secret-key", null, new HttpClient(handler));

    var answer = await systemOne.ChoiceAsync("ticket", "Which team should handle this?", teams);

    Expect.Equal(teams[1], answer.Choice, "The selected team should be Technical.");
    Expect.Equal(0.82, answer.Confidence, "The confidence should be returned.");
    Expect.Equal(3, answer.Options.Count, "Every option should carry a probability.");
    Expect.Equal("Bearer secret-key", handler.Last.Authorization, "The API key should be sent as a bearer token.");
    Expect.Equal("jev-latest", handler.Last.Json.GetProperty("model").GetString(), "The default model should be sent.");
    Expect.Equal("Which team should handle this?", handler.Last.Question("choice").GetProperty("instructions").GetString(), "The instructions should be sent.");
    Expect.Equal("ticket", handler.Last.Json.GetProperty("state").GetString(), "The state should be sent.");
});

await runner.CheckAsync("mixed batch returns typed answers and metadata", async () =>
{
    var department = new Choice<Team>("department", "Which team should handle this?", teams);
    var urgent = new Noul("is_urgent", "Does this message convey urgency?");
    var frustration = new Score("frustration", "How frustrated is the customer?", new[] { "Calm", "Frustrated", "Very angry" });

    var handler = FakeHandler.Json(BatchPayload);
    using var systemOne = new SystemOne("secret-key", null, new HttpClient(handler));

    var result = await systemOne.Query("ticket")
        .Question(department)
        .Question(urgent)
        .Score(frustration)
        .SendAsync();

    var frustrationAnswer = result.Get(frustration);
    Expect.Equal(teams[1], result.Get(department).Choice, "The typed choice answer should be readable.");
    Expect.Equal(0.92, result.Get(urgent).Noul, "The typed noul answer should be readable.");
    Expect.Equal(1.6, frustrationAnswer.Score, "The typed score answer should be readable.");
    Expect.Equal("jev-latest", result.Model, "The model should be returned.");
    Expect.Equal(312, result.Usage.InputTokens, "The input token count should be returned.");
    Expect.Equal(48, result.Usage.OutputTokens, "The output token count should be returned.");
    Expect.Equal(3, frustrationAnswer.Probabilities?.Count, "Score probabilities should be returned.");
});

await runner.CheckAsync("a score answer without probabilities is tolerated", async () =>
{
    const string payload = """
    {
      "model": "jev-latest",
      "answers": {
        "score": {
          "type": "score",
          "score": 1.035,
          "legend": { "0": "Calm", "1": "Frustrated", "2": "Very angry" },
          "confidence": 0.842
        }
      },
      "usage": { "input_tokens": 1, "output_tokens": 1 }
    }
    """;

    var handler = FakeHandler.Json(payload);
    using var systemOne = new SystemOne("secret-key", null, new HttpClient(handler));

    var answer = await systemOne.ScoreAsync("ticket", "How frustrated?", new[] { "Calm", "Frustrated", "Very angry" });

    Expect.Equal(1.035, answer.Score, "The score should be returned.");
    Expect.True(answer.Probabilities is null, "The probabilities should stay absent.");
});

await runner.CheckAsync("the endpoint and model can be overridden", async () =>
{
    var handler = FakeHandler.Json(DirectPayload);
    using var systemOne = new SystemOne("secret-key", new SystemOneOptions
    {
        Endpoint = "https://example.test/systemOne",
        Model = "systemOne-2026-01",
    }, new HttpClient(handler));

    await systemOne.NoulAsync("ticket", "Is it urgent?");

    Expect.Equal("https://example.test/systemOne", handler.Last.Message.RequestUri?.ToString(), "The endpoint override should be used.");
    Expect.Equal("systemOne-2026-01", handler.Last.Json.GetProperty("model").GetString(), "The model override should be used.");
});

// ---------------------------------------------------------------------------
// Reference identity and independent concurrent requests.
// ---------------------------------------------------------------------------

await runner.CheckAsync("returned options keep their reference identity", async () =>
{
    var handler = FakeHandler.Json(DirectPayload);
    using var systemOne = new SystemOne("secret-key", null, new HttpClient(handler));

    var answer = await systemOne.ChoiceAsync("ticket", "Which team?", teams);

    Expect.Same(teams[1], answer.Choice, "The selected value should be the original instance.");
    Expect.Same(teams[0], answer.Options[0].Option, "Every option should map back to the original instance.");
    Expect.Same(teams[2], answer.Options[2].Option, "Every option should map back to the original instance.");
});

await runner.CheckAsync("independent concurrent requests are isolated", async () =>
{
    var handler = new FakeHandler(async (_, body, _) =>
    {
        var id = JsonDocument.Parse(body).RootElement.GetProperty("questions").EnumerateObject().Single().Name;
        await Task.Delay(20);
        return FakeHandler.Message($$"""
        {
          "model": "jev-latest",
          "answers": { "{{id}}": { "type": "noul", "noul": {{(id == "first" ? "0.1" : "0.9")}} } },
          "usage": { "input_tokens": 1, "output_tokens": 1 }
        }
        """);
    });
    using var systemOne = new SystemOne("secret-key", null, new HttpClient(handler));

    var first = systemOne.Query("first").Noul(new Noul("first", "Is it urgent?")).SendAsync();
    var second = systemOne.Query("second").Noul(new Noul("second", "Is it urgent?")).SendAsync();
    var results = await Task.WhenAll(first, second);

    Expect.Equal(0.1, results[0].Get(new Noul("first", "Is it urgent?")).Noul, "The first request should get its own answer.");
    Expect.Equal(0.9, results[1].Get(new Noul("second", "Is it urgent?")).Noul, "The second request should get its own answer.");
    Expect.Equal(2, handler.CallCount, "Both requests should reach the API.");
});

// ---------------------------------------------------------------------------
// Property limits.
// ---------------------------------------------------------------------------

await runner.CheckAsync("a choice option with 10 properties is accepted and 11 is rejected", async () =>
{
    var handler = FakeHandler.Json(SingleChoicePayload);
    using var systemOne = new SystemOne("secret-key", new SystemOneOptions { MaxChoiceProperties = 10 }, new HttpClient(handler));

    await systemOne.ChoiceAsync("ticket", "Which option?", new[] { new TenProperties() });
    Expect.Equal(1, handler.CallCount, "The accepted option should be sent.");

    await Expect.ThrowsAsync<SystemOneValidationException>(
        () => systemOne.ChoiceAsync("ticket", "Which option?", new[] { new ElevenProperties() }),
        "The oversized option should be rejected locally.");
    Expect.Equal(1, handler.CallCount, "The rejected option should never reach the API.");
});

await runner.CheckAsync("the property limit error is actionable", async () =>
{
    var handler = FakeHandler.Json(DirectPayload);
    using var systemOne = new SystemOne("secret-key", null, new HttpClient(handler));

    try
    {
        await systemOne.ChoiceAsync<object>("ticket", "Which option?", new object[] { new Point(), new TwentyOneProperties() });
        throw new InvalidOperationException("The oversized option should have been rejected.");
    }
    catch (SystemOneValidationException exception)
    {
        Expect.Equal(
            "Choice option 1 contains 21 serialized properties; the limit is 20.\n" +
            "Use a dedicated smaller POCO or increase SystemOneOptions.MaxChoiceProperties\n" +
            "when constructing SystemOne.",
            exception.Message,
            "The error should identify the option, the count, the limit, and both remedies.");
    }
});

await runner.CheckAsync("nested objects and arrays are counted and ignored properties are not", async () =>
{
    var handler = FakeHandler.Json(SingleChoicePayload);
    using var systemOne = new SystemOne("secret-key", new SystemOneOptions { MaxChoiceProperties = 10 }, new HttpClient(handler));

    await systemOne.ChoiceAsync("ticket", "Which option?", new[] { new WithIgnoredProperties() });

    await Expect.ThrowsAsync<SystemOneValidationException>(
        () => systemOne.ChoiceAsync("ticket", "Which option?", new[] { new NestedTen() }),
        "Nested properties should be counted.");
    await Expect.ThrowsAsync<SystemOneValidationException>(
        () => systemOne.ChoiceAsync("ticket", "Which option?", new[] { new WithArray { Points = Enumerable.Repeat(new Point(), 11).ToArray() } }),
        "Array elements should be counted.");
    await Expect.ThrowsAsync<SystemOneValidationException>(
        () => systemOne.ChoiceAsync("ticket", "Which option?", new[] { new WithDictionary { Points = Enumerable.Range(0, 11).ToDictionary(index => "p" + index, _ => new Point()) } }),
        "Dictionary entries should be counted.");
    await Expect.ThrowsAsync<SystemOneValidationException>(
        () => systemOne.ChoiceAsync("ticket", "Which option?", new[] { Cyclic() }),
        "Cyclic graphs should be rejected.");
    Expect.Equal(1, handler.CallCount, "Only the accepted option should be sent.");
});

// ---------------------------------------------------------------------------
// Cancellation and failure handling.
// ---------------------------------------------------------------------------

await runner.CheckAsync("cancellation before the request is propagated and sends nothing", async () =>
{
    var handler = FakeHandler.Json(DirectPayload);
    using var systemOne = new SystemOne("secret-key", null, new HttpClient(handler));
    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();

    await Expect.ThrowsAsync<OperationCanceledException>(
        () => systemOne.NoulAsync("ticket", "Is it urgent?", cancellation.Token),
        "The pre-cancelled token should be observed.");
    Expect.Equal(0, handler.CallCount, "No request should be sent.");
});

await runner.CheckAsync("cancellation during HTTP work is propagated unchanged", async () =>
{
    using var cancellation = new CancellationTokenSource();
    var handler = new FakeHandler(async (_, _, _) =>
    {
        cancellation.Cancel();
        await Task.Delay(Timeout.Infinite, cancellation.Token);
        return FakeHandler.Message("{}");
    });
    using var systemOne = new SystemOne("secret-key", null, new HttpClient(handler));

    try
    {
        await systemOne.NoulAsync("ticket", "Is it urgent?", cancellation.Token);
        throw new InvalidOperationException("The request should have been cancelled.");
    }
    catch (OperationCanceledException exception)
    {
        Expect.Equal(cancellation.Token, exception.CancellationToken, "The original token should be propagated.");
    }
});

await runner.CheckAsync("unsuccessful responses throw with status and body", async () =>
{
    const string body = """{"error":"invalid api key"}""";
    var handler = FakeHandler.Json(body, HttpStatusCode.Unauthorized);
    using var systemOne = new SystemOne("bad-key", null, new HttpClient(handler));

    try
    {
        await systemOne.NoulAsync("ticket", "Is it urgent?");
        throw new InvalidOperationException("The failed response should throw.");
    }
    catch (SystemOneApiException exception)
    {
        Expect.Equal(HttpStatusCode.Unauthorized, exception.StatusCode, "The status code should be exposed.");
        Expect.Equal(body, exception.ResponseBody, "The response body should be exposed.");
    }
});

await runner.CheckAsync("malformed and incomplete responses are rejected", async () =>
{
    var malformed = FakeHandler.Json("{not json");
    using (var systemOne = new SystemOne("secret-key", null, new HttpClient(malformed)))
    {
        await Expect.ThrowsAsync<SystemOneProtocolException>(
            () => systemOne.NoulAsync("ticket", "Is it urgent?"),
            "Malformed JSON should be rejected.");
    }

    var wrongType = FakeHandler.Json("""{"model":"jev-latest","answers":{"noul":{"type":"score","score":1.0}},"usage":{"input_tokens":1,"output_tokens":1}}""");
    using (var systemOne = new SystemOne("secret-key", null, new HttpClient(wrongType)))
    {
        await Expect.ThrowsAsync<SystemOneProtocolException>(
            () => systemOne.NoulAsync("ticket", "Is it urgent?"),
            "A mismatched answer type should be rejected.");
    }

    var unknownChoice = FakeHandler.Json("""{"model":"jev-latest","answers":{"choice":{"type":"choice","choice":"9","probabilities":{"0":1.0},"confidence":1.0}},"usage":{"input_tokens":1,"output_tokens":1}}""");
    using (var systemOne = new SystemOne("secret-key", null, new HttpClient(unknownChoice)))
    {
        await Expect.ThrowsAsync<SystemOneProtocolException>(
            () => systemOne.ChoiceAsync("ticket", "Which team?", new[] { "billing" }),
            "An unknown choice id should be rejected.");
    }
});

await runner.CheckAsync("a supplied HTTP client is used and stays alive after disposal", async () =>
{
    var handler = FakeHandler.Json(DirectPayload);
    var httpClient = new HttpClient(handler);
    var systemOne = new SystemOne("secret-key", null, httpClient);

    await systemOne.NoulAsync("ticket", "Is it urgent?");
    systemOne.Dispose();

    using var response = await httpClient.GetAsync("https://example.test/still-alive");
    Expect.True(response.IsSuccessStatusCode, "The supplied client should not be disposed by SystemOne.");
});

// ---------------------------------------------------------------------------
// Local validation.
// ---------------------------------------------------------------------------

await runner.CheckAsync("local validation failures send no request", async () =>
{
    var handler = FakeHandler.Json(DirectPayload);
    using var systemOne = new SystemOne("secret-key", null, new HttpClient(handler));

    await Expect.ThrowsAsync<SystemOneValidationException>(
        () => systemOne.ChoiceAsync("ticket", "Which team?", Array.Empty<string>()),
        "An empty choice should be rejected.");
    await Expect.ThrowsAsync<SystemOneValidationException>(
        () => systemOne.ScoreAsync("ticket", "How frustrated?", new[] { "Only one level" }),
        "A single-level score should be rejected.");
    await Expect.ThrowsAsync<SystemOneValidationException>(
        () => systemOne.Query("ticket").SendAsync(),
        "An empty batch should be rejected.");
    await Expect.ThrowsAsync<SystemOneValidationException>(
        () => systemOne.Query("ticket").Noul(new Noul("same", "First?")).Noul(new Noul("same", "Second?")).SendAsync(),
        "Duplicate question ids should be rejected.");

    Expect.Equal(0, handler.CallCount, "No invalid request should reach the API.");
});

await runner.CheckAsync("a disposed client rejects new requests", async () =>
{
    var handler = FakeHandler.Json(DirectPayload);
    var systemOne = new SystemOne("secret-key", null, new HttpClient(handler));
    systemOne.Dispose();

    await Expect.ThrowsAsync<ObjectDisposedException>(
        () => systemOne.NoulAsync("ticket", "Is it urgent?"),
        "A disposed client should throw.");
});

return runner.Complete();

static object Cyclic()
{
    var node = new CyclicNode();
    node.Next = node;
    return node;
}
