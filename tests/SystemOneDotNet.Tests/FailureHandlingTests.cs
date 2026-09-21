using System.Net;
using System.Text;
using System.Text.Json;

namespace SystemOneDotNet.Tests;

public sealed class FailureHandlingTests
{
    private const string ValidResponse = """{"model":"jev-latest","answers":{"noul":{"type":"noul","noul":0.5}},"usage":{"input_tokens":1,"output_tokens":2}}""";

    private static SystemOne Create(Func<RecordedRequest, HttpResponseMessage> factory) =>
        TestClient.Create(RecordingHandler.Responding(factory));

    [Fact]
    public async Task SendAsync_UnsuccessfulResponse_ThrowsWithStatusAndBody()
    {
        const string body = """{"error":"invalid api key"}""";
        using var systemOne = Create(_ => FakeApi.Message(body, HttpStatusCode.Unauthorized));

        var act = () => systemOne.NoulAsync("ticket", "Is it urgent?");

        var exception = await act.Should().ThrowAsync<SystemOneApiException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        exception.Which.ResponseBody.Should().Be(body);
        exception.Which.Message.Should().Contain("401").And.Contain(body);
    }

    [Theory]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    public async Task SendAsync_UnsuccessfulStatus_ThrowsWithThatStatus(HttpStatusCode statusCode)
    {
        using var systemOne = Create(_ => FakeApi.Message("{}", statusCode));

        var act = () => systemOne.NoulAsync("ticket", "Is it urgent?");

        var exception = await act.Should().ThrowAsync<SystemOneApiException>();
        exception.Which.StatusCode.Should().Be(statusCode);
    }

    [Fact]
    public async Task SendAsync_LongErrorBody_TruncatesInException()
    {
        var body = new string('x', 10000);
        using var systemOne = Create(_ => FakeApi.Message(body, HttpStatusCode.BadGateway));

        var act = () => systemOne.NoulAsync("ticket", "Is it urgent?");
        var exception = await act.Should().ThrowAsync<SystemOneApiException>();

        exception.Which.ResponseBody.Length.Should().BeLessThan(body.Length);
        exception.Which.ResponseBody.Should().Contain("characters truncated");
    }

    [Fact]
    public async Task SendAsync_MalformedJson_ThrowsProtocol()
    {
        using var systemOne = Create(_ => FakeApi.Message("{not json"));

        var act = () => systemOne.NoulAsync("ticket", "Is it urgent?");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*malformed JSON*");
    }

    [Fact]
    public async Task SendAsync_EmptyBody_ThrowsProtocol()
    {
        using var systemOne = Create(_ => FakeApi.Message(""));

        var act = () => systemOne.NoulAsync("ticket", "Is it urgent?");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*empty response body*");
    }

    [Theory]
    [InlineData("""{"model":"jev-latest","usage":{"input_tokens":1,"output_tokens":2}}""", "answers")]
    [InlineData("""{"model":"jev-latest","answers":{},"usage":{"input_tokens":1,"output_tokens":2}}""", "missing an answer")]
    [InlineData("""{"answers":{"q":{"type":"noul","noul":0.5}},"usage":{"input_tokens":1,"output_tokens":2}}""", "model")]
    [InlineData("""{"model":"jev-latest","answers":{"q":{"type":"noul","noul":0.5}}}""", "usage")]
    [InlineData("""{"model":"jev-latest","answers":{"q":{"type":"noul","noul":0.5}},"usage":{}}""", "input_tokens")]
    [InlineData("""{"model":"jev-latest","answers":[],"usage":{"input_tokens":1,"output_tokens":2}}""", "answers")]
    public async Task SendAsync_IncompleteResponse_ThrowsProtocol(string body, string expectedMessagePart)
    {
        using var systemOne = Create(_ => FakeApi.Message(body));

        var act = () => systemOne.NoulAsync("ticket", "Is it urgent?");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage($"*{expectedMessagePart}*");
    }

    [Fact]
    public async Task SendAsync_MismatchedAnswerType_ThrowsProtocol()
    {
        var body = FakeApi.Serialize(FakeApi.Envelope(new { noul = new { type = "score", score = 1.0 } }));
        using var systemOne = Create(_ => FakeApi.Message(body));

        var act = () => systemOne.NoulAsync("ticket", "Is it urgent?");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*has type 'score' but 'noul' was expected*");
    }

    [Fact]
    public async Task SendAsync_UnknownChoiceId_ThrowsProtocol()
    {
        var body = FakeApi.Serialize(FakeApi.Envelope(new
        {
            choice = FakeApi.ChoiceAnswer("9", 0.5, new Dictionary<string, double> { ["0"] = 0.5, ["1"] = 0.5 }),
        }));
        using var systemOne = Create(_ => FakeApi.Message(body));

        var act = () => systemOne.ChoiceAsync("ticket", "Which team?", new[] { "billing", "technical" });

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*unknown option id '9'*");
    }

    [Fact]
    public async Task SendAsync_MissingProbabilities_ThrowsProtocol()
    {
        var body = FakeApi.Serialize(FakeApi.Envelope(new
        {
            choice = FakeApi.ChoiceAnswer("0", 0.5, new Dictionary<string, double> { ["0"] = 1.0 }),
        }));
        using var systemOne = Create(_ => FakeApi.Message(body));

        var act = () => systemOne.ChoiceAsync("ticket", "Which team?", new[] { "billing", "technical" });

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*missing the probability for option id '1'*");
    }

    [Fact]
    public async Task SendAsync_ConfidenceOutOfRange_ThrowsProtocol()
    {
        var body = FakeApi.Serialize(FakeApi.Envelope(new
        {
            choice = FakeApi.ChoiceAnswer("0", 1.5, new Dictionary<string, double> { ["0"] = 0.5, ["1"] = 0.5 }),
        }));
        using var systemOne = Create(_ => FakeApi.Message(body));

        var act = () => systemOne.ChoiceAsync("ticket", "Which team?", new[] { "billing", "technical" });

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*must be between 0 and 1*");
    }

    [Fact]
    public async Task SendAsync_MissingNoulValue_ThrowsProtocol()
    {
        var body = FakeApi.Serialize(FakeApi.Envelope(new { noul = new { type = "noul" } }));
        using var systemOne = Create(_ => FakeApi.Message(body));

        var act = () => systemOne.NoulAsync("ticket", "Is it urgent?");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*missing the 'noul' property*");
    }

    [Fact]
    public async Task SendAsync_MissingLegendEntry_ThrowsProtocol()
    {
        var body = FakeApi.Serialize(FakeApi.Envelope(new
        {
            score = FakeApi.ScoreAnswer(1.0, 0.5, new Dictionary<string, string> { ["0"] = "Calm" }),
        }));
        using var systemOne = Create(_ => FakeApi.Message(body));

        var act = () => systemOne.ScoreAsync("ticket", "How frustrated?", new[] { "Calm", "Angry" });

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*missing the legend entry for level id '1'*");
    }

    [Fact]
    public async Task SendAsync_ScoreOutOfRange_ThrowsProtocol()
    {
        var body = FakeApi.Serialize(FakeApi.Envelope(new
        {
            score = new
            {
                type = "score",
                score = 2.5,
                legend = new Dictionary<string, string> { ["0"] = "Calm", ["1"] = "Angry" },
                confidence = 0.5,
            },
        }));
        using var systemOne = Create(_ => FakeApi.Message(body));

        var act = () => systemOne.ScoreAsync("ticket", "How frustrated?", new[] { "Calm", "Angry" });

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*must be between 0 and 1*");
    }

    [Fact]
    public async Task SendAsync_AliasOptionId_ThrowsProtocol()
    {
        var body = FakeApi.Serialize(FakeApi.Envelope(new
        {
            choice = FakeApi.ChoiceAnswer("0", 1.0, new Dictionary<string, double> { ["01"] = 1.0 }),
        }));
        using var systemOne = Create(_ => FakeApi.Message(body));

        var act = () => systemOne.ChoiceAsync("ticket", "Which team?", new[] { "billing", "technical" });

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*unknown option id '01'*");
    }

    [Fact]
    public async Task SendAsync_NegativeUsageCount_ThrowsProtocol()
    {
        var body = FakeApi.Serialize(FakeApi.Envelope(new { noul = FakeApi.NoulAnswer(0.5) }, inputTokens: -1));
        using var systemOne = Create(_ => FakeApi.Message(body));

        var act = () => systemOne.NoulAsync("ticket", "Is it urgent?");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*usage.input_tokens*");
    }

    [Fact]
    public async Task SendAsync_UnserializableState_ThrowsValidation()
    {
        var handler = RecordingHandler.Json(ValidResponse);
        using var systemOne = TestClient.Create(handler);

        var cyclicState = new CyclicNode();
        cyclicState.Next = cyclicState;

        var act = () => systemOne.NoulAsync(cyclicState, "Is it urgent?");

        await act.Should().ThrowAsync<SystemOneValidationException>()
            .WithMessage("*could not be serialized to JSON*");
        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task SendAsync_PreCancelledToken_PropagatesAndSendsNothing()
    {
        var handler = RecordingHandler.Json(ValidResponse);
        using var systemOne = TestClient.Create(handler);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var act = () => systemOne.NoulAsync("ticket", "Is it urgent?", cancellation.Token);

        var exception = await act.Should().ThrowAsync<OperationCanceledException>();
        exception.Which.CancellationToken.Should().Be(cancellation.Token);
        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task SendAsync_CancelledDuringHttpWork_PropagatesToken()
    {
        using var cancellation = new CancellationTokenSource();
        var handler = RecordingHandler.RespondingAsync(async request =>
        {
            cancellation.Cancel();
            var content = new StringContent(ValidResponse, Encoding.UTF8, "application/json");
            await Task.Delay(Timeout.Infinite, cancellation.Token);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content, RequestMessage = request.Message };
        });
        using var systemOne = TestClient.Create(handler);

        var act = () => systemOne.NoulAsync("ticket", "Is it urgent?", cancellation.Token);

        var exception = await act.Should().ThrowAsync<OperationCanceledException>();
        exception.Which.CancellationToken.Should().Be(cancellation.Token);
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task SendAsync_CancelledDuringBodyRead_PropagatesToken()
    {
        using var cancellation = new CancellationTokenSource();
        var handler = RecordingHandler.RespondingAsync(request =>
        {
            cancellation.Cancel();
            var content = new StreamContent(new CancellingStream(Encoding.UTF8.GetBytes(ValidResponse), cancellation.Token));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content, RequestMessage = request.Message });
        });
        using var systemOne = TestClient.Create(handler);

        var act = () => systemOne.NoulAsync("ticket", "Is it urgent?", cancellation.Token);

        var exception = await act.Should().ThrowAsync<OperationCanceledException>();
        exception.Which.CancellationToken.Should().Be(cancellation.Token);
    }

    [Fact]
    public async Task SendAsync_LocalValidationFailures_SendNothing()
    {
        var handler = RecordingHandler.Json(ValidResponse);
        using var systemOne = TestClient.Create(handler);

        // The null entries below intentionally exercise the guards with a null argument. The
        // null-forgiving operator marks that intent; it is the only use in this file.
        var actions = new Func<Task>[]
        {
            () => systemOne.ChoiceAsync("ticket", "Which team?", Array.Empty<string>()),
            () => systemOne.ScoreAsync("ticket", "How frustrated?", new[] { "Only one level" }),
            () => systemOne.Query(null!).Noul(new Noul("q", "Is it urgent?")).SendAsync(),
            () => systemOne.Query("ticket").Question<ChoiceAnswer<string>>(null!).SendAsync(),
            () => systemOne.ChoiceAsync("ticket", "Which team?", new[] { "a", "a" }.AsEnumerable().Concat(new[] { (string)null! })),
        };

        foreach (var action in actions)
        {
            await action.Should().ThrowAsync<Exception>();
        }

        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task SystemOne_AfterFailedRequest_ServesNextRequest()
    {
        var call = 0;
        using var systemOne = Create(_ => call++ == 0
            ? FakeApi.Message("{}", HttpStatusCode.BadGateway)
            : FakeApi.Message(ValidResponse));

        var act = () => systemOne.NoulAsync("ticket", "Is it urgent?");
        await act.Should().ThrowAsync<SystemOneApiException>();

        var answer = await systemOne.NoulAsync("ticket", "Is it urgent?");

        answer.Noul.Should().Be(0.5);
        call.Should().Be(2);
    }

    [Fact]
    public async Task SystemOne_Disposed_ThrowsObjectDisposed()
    {
        var handler = RecordingHandler.Json(ValidResponse);
        var systemOne = TestClient.Create(handler);
        systemOne.Dispose();

        var act = () => systemOne.NoulAsync("ticket", "Is it urgent?");

        await act.Should().ThrowAsync<ObjectDisposedException>();
        handler.CallCount.Should().Be(0);
    }

    private sealed class CancellingStream : MemoryStream
    {
        private readonly CancellationToken cancellingToken;

        public CancellingStream(byte[] buffer, CancellationToken cancellationToken)
            : base(buffer)
        {
            cancellingToken = cancellationToken;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellingToken.ThrowIfCancellationRequested();
            return await base.ReadAsync(buffer, cancellationToken);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellingToken.ThrowIfCancellationRequested();
            return base.ReadAsync(buffer, offset, count, cancellationToken);
        }
    }
}
