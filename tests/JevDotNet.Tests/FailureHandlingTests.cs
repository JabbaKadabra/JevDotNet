using System.Net;
using System.Text;
using System.Text.Json;

namespace JevDotNet.Tests;

public class FailureHandlingTests
{
    private const string ValidResponse = """{"model":"jev-latest","answers":{"noul":{"type":"noul","noul":0.5}},"usage":{"input_tokens":1,"output_tokens":2}}""";

    private static Jev Create(Func<RecordedRequest, HttpResponseMessage> factory) =>
        TestClient.Create(RecordingHandler.Responding(factory));

    [Fact]
    public async Task Unsuccessful_responses_throw_with_the_status_and_body()
    {
        const string body = """{"error":"invalid api key"}""";
        using var jev = Create(_ => FakeApi.Message(body, HttpStatusCode.Unauthorized));

        var act = () => jev.NoulAsync("ticket", "Is it urgent?");

        var exception = await act.Should().ThrowAsync<JevApiException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        exception.Which.ResponseBody.Should().Be(body);
        exception.Which.Message.Should().Contain("401").And.Contain(body);
    }

    [Theory]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    public async Task Every_unsuccessful_status_throws(HttpStatusCode statusCode)
    {
        using var jev = Create(_ => FakeApi.Message("{}", statusCode));

        var act = () => jev.NoulAsync("ticket", "Is it urgent?");

        var exception = await act.Should().ThrowAsync<JevApiException>();
        exception.Which.StatusCode.Should().Be(statusCode);
    }

    [Fact]
    public async Task Long_error_bodies_are_truncated_in_the_exception()
    {
        var body = new string('x', 10000);
        using var jev = Create(_ => FakeApi.Message(body, HttpStatusCode.BadGateway));

        var act = () => jev.NoulAsync("ticket", "Is it urgent?");
        var exception = await act.Should().ThrowAsync<JevApiException>();

        exception.Which.ResponseBody.Length.Should().BeLessThan(body.Length);
        exception.Which.ResponseBody.Should().Contain("characters truncated");
    }

    [Fact]
    public async Task Malformed_json_bodies_are_rejected()
    {
        using var jev = Create(_ => FakeApi.Message("{not json"));

        var act = () => jev.NoulAsync("ticket", "Is it urgent?");

        await act.Should().ThrowAsync<JevProtocolException>()
            .WithMessage("*malformed JSON*");
    }

    [Fact]
    public async Task Empty_bodies_are_rejected()
    {
        using var jev = Create(_ => FakeApi.Message(""));

        var act = () => jev.NoulAsync("ticket", "Is it urgent?");

        await act.Should().ThrowAsync<JevProtocolException>()
            .WithMessage("*empty response body*");
    }

    [Theory]
    [InlineData("""{"model":"jev-latest","usage":{"input_tokens":1,"output_tokens":2}}""", "answers")]
    [InlineData("""{"model":"jev-latest","answers":{},"usage":{"input_tokens":1,"output_tokens":2}}""", "missing an answer")]
    [InlineData("""{"answers":{"q":{"type":"noul","noul":0.5}},"usage":{"input_tokens":1,"output_tokens":2}}""", "model")]
    [InlineData("""{"model":"jev-latest","answers":{"q":{"type":"noul","noul":0.5}}}""", "usage")]
    [InlineData("""{"model":"jev-latest","answers":{"q":{"type":"noul","noul":0.5}},"usage":{}}""", "input_tokens")]
    [InlineData("""{"model":"jev-latest","answers":[],"usage":{"input_tokens":1,"output_tokens":2}}""", "answers")]
    public async Task Incomplete_responses_are_rejected(string body, string expectedMessagePart)
    {
        using var jev = Create(_ => FakeApi.Message(body));

        var act = () => jev.NoulAsync("ticket", "Is it urgent?");

        await act.Should().ThrowAsync<JevProtocolException>()
            .WithMessage($"*{expectedMessagePart}*");
    }

    [Fact]
    public async Task Mismatched_answer_types_are_rejected()
    {
        var body = FakeApi.Serialize(FakeApi.Envelope(new { noul = new { type = "score", score = 1.0 } }));
        using var jev = Create(_ => FakeApi.Message(body));

        var act = () => jev.NoulAsync("ticket", "Is it urgent?");

        await act.Should().ThrowAsync<JevProtocolException>()
            .WithMessage("*has type 'score' but 'noul' was expected*");
    }

    [Fact]
    public async Task Unknown_choice_ids_are_rejected()
    {
        var body = FakeApi.Serialize(FakeApi.Envelope(new
        {
            choice = FakeApi.ChoiceAnswer("9", 0.5, new Dictionary<string, double> { ["0"] = 0.5, ["1"] = 0.5 }),
        }));
        using var jev = Create(_ => FakeApi.Message(body));

        var act = () => jev.ChoiceAsync("ticket", "Which team?", new[] { "billing", "technical" });

        await act.Should().ThrowAsync<JevProtocolException>()
            .WithMessage("*unknown option id '9'*");
    }

    [Fact]
    public async Task Missing_probabilities_are_rejected()
    {
        var body = FakeApi.Serialize(FakeApi.Envelope(new
        {
            choice = FakeApi.ChoiceAnswer("0", 0.5, new Dictionary<string, double> { ["0"] = 1.0 }),
        }));
        using var jev = Create(_ => FakeApi.Message(body));

        var act = () => jev.ChoiceAsync("ticket", "Which team?", new[] { "billing", "technical" });

        await act.Should().ThrowAsync<JevProtocolException>()
            .WithMessage("*missing the probability for option id '1'*");
    }

    [Fact]
    public async Task Out_of_range_confidences_are_rejected()
    {
        var body = FakeApi.Serialize(FakeApi.Envelope(new
        {
            choice = FakeApi.ChoiceAnswer("0", 1.5, new Dictionary<string, double> { ["0"] = 0.5, ["1"] = 0.5 }),
        }));
        using var jev = Create(_ => FakeApi.Message(body));

        var act = () => jev.ChoiceAsync("ticket", "Which team?", new[] { "billing", "technical" });

        await act.Should().ThrowAsync<JevProtocolException>()
            .WithMessage("*must be between 0 and 1*");
    }

    [Fact]
    public async Task Missing_noul_values_are_rejected()
    {
        var body = FakeApi.Serialize(FakeApi.Envelope(new { noul = new { type = "noul" } }));
        using var jev = Create(_ => FakeApi.Message(body));

        var act = () => jev.NoulAsync("ticket", "Is it urgent?");

        await act.Should().ThrowAsync<JevProtocolException>()
            .WithMessage("*missing the 'noul' property*");
    }

    [Fact]
    public async Task Missing_legend_entries_are_rejected()
    {
        var body = FakeApi.Serialize(FakeApi.Envelope(new
        {
            score = FakeApi.ScoreAnswer(1.0, 0.5, new Dictionary<string, string> { ["0"] = "Calm" }),
        }));
        using var jev = Create(_ => FakeApi.Message(body));

        var act = () => jev.ScoreAsync("ticket", "How frustrated?", new[] { "Calm", "Angry" });

        await act.Should().ThrowAsync<JevProtocolException>()
            .WithMessage("*missing the legend entry for level id '1'*");
    }

    [Fact]
    public async Task Out_of_range_scores_are_rejected()
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
        using var jev = Create(_ => FakeApi.Message(body));

        var act = () => jev.ScoreAsync("ticket", "How frustrated?", new[] { "Calm", "Angry" });

        await act.Should().ThrowAsync<JevProtocolException>()
            .WithMessage("*must be between 0 and 1*");
    }

    [Fact]
    public async Task Alias_option_ids_are_rejected()
    {
        var body = FakeApi.Serialize(FakeApi.Envelope(new
        {
            choice = FakeApi.ChoiceAnswer("0", 1.0, new Dictionary<string, double> { ["01"] = 1.0 }),
        }));
        using var jev = Create(_ => FakeApi.Message(body));

        var act = () => jev.ChoiceAsync("ticket", "Which team?", new[] { "billing", "technical" });

        await act.Should().ThrowAsync<JevProtocolException>()
            .WithMessage("*unknown option id '01'*");
    }

    [Fact]
    public async Task Negative_usage_counts_are_rejected()
    {
        var body = FakeApi.Serialize(FakeApi.Envelope(new { noul = FakeApi.NoulAnswer(0.5) }, inputTokens: -1));
        using var jev = Create(_ => FakeApi.Message(body));

        var act = () => jev.NoulAsync("ticket", "Is it urgent?");

        await act.Should().ThrowAsync<JevProtocolException>()
            .WithMessage("*usage.input_tokens*");
    }

    [Fact]
    public async Task State_serialization_failures_are_reported_as_local_validation_errors()
    {
        var handler = RecordingHandler.Json(ValidResponse);
        using var jev = TestClient.Create(handler);

        var cyclicState = new CyclicNode();
        cyclicState.Next = cyclicState;

        var act = () => jev.NoulAsync(cyclicState, "Is it urgent?");

        await act.Should().ThrowAsync<JevValidationException>()
            .WithMessage("*could not be serialized to JSON*");
        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Cancellation_before_sending_is_propagated_and_no_request_is_sent()
    {
        var handler = RecordingHandler.Json(ValidResponse);
        using var jev = TestClient.Create(handler);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var act = () => jev.NoulAsync("ticket", "Is it urgent?", cancellation.Token);

        var exception = await act.Should().ThrowAsync<OperationCanceledException>();
        exception.Which.CancellationToken.Should().Be(cancellation.Token);
        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Cancellation_during_http_work_is_propagated_unchanged()
    {
        using var cancellation = new CancellationTokenSource();
        var handler = RecordingHandler.RespondingAsync(async request =>
        {
            cancellation.Cancel();
            var content = new StringContent(ValidResponse, Encoding.UTF8, "application/json");
            await Task.Delay(Timeout.Infinite, cancellation.Token);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content, RequestMessage = request.Message };
        });
        using var jev = TestClient.Create(handler);

        var act = () => jev.NoulAsync("ticket", "Is it urgent?", cancellation.Token);

        var exception = await act.Should().ThrowAsync<OperationCanceledException>();
        exception.Which.CancellationToken.Should().Be(cancellation.Token);
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Cancellation_after_the_response_is_received_is_propagated_during_body_reading()
    {
        using var cancellation = new CancellationTokenSource();
        var handler = RecordingHandler.RespondingAsync(request =>
        {
            cancellation.Cancel();
            var content = new StreamContent(new CancellingStream(Encoding.UTF8.GetBytes(ValidResponse), cancellation.Token));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content, RequestMessage = request.Message });
        });
        using var jev = TestClient.Create(handler);

        var act = () => jev.NoulAsync("ticket", "Is it urgent?", cancellation.Token);

        var exception = await act.Should().ThrowAsync<OperationCanceledException>();
        exception.Which.CancellationToken.Should().Be(cancellation.Token);
    }

    [Fact]
    public async Task Local_validation_failures_send_no_request()
    {
        var handler = RecordingHandler.Json(ValidResponse);
        using var jev = TestClient.Create(handler);

        var actions = new Func<Task>[]
        {
            () => jev.ChoiceAsync("ticket", "Which team?", Array.Empty<string>()),
            () => jev.ScoreAsync("ticket", "How frustrated?", new[] { "Only one level" }),
            () => jev.Query(null!).Noul(new Noul("q", "Is it urgent?")).SendAsync(),
            () => jev.Query("ticket").Question<ChoiceAnswer<string>>(null!).SendAsync(),
            () => jev.ChoiceAsync("ticket", "Which team?", new[] { "a", "a" }.AsEnumerable().Concat(new[] { (string)null! })),
        };

        foreach (var action in actions)
        {
            await action.Should().ThrowAsync<Exception>();
        }

        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task The_client_can_be_used_again_after_a_failed_request()
    {
        var call = 0;
        using var jev = Create(_ => call++ == 0
            ? FakeApi.Message("{}", HttpStatusCode.BadGateway)
            : FakeApi.Message(ValidResponse));

        var act = () => jev.NoulAsync("ticket", "Is it urgent?");
        await act.Should().ThrowAsync<JevApiException>();

        var answer = await jev.NoulAsync("ticket", "Is it urgent?");

        answer.Noul.Should().Be(0.5);
        call.Should().Be(2);
    }

    [Fact]
    public async Task Disposed_clients_throw_on_use()
    {
        var handler = RecordingHandler.Json(ValidResponse);
        var jev = TestClient.Create(handler);
        jev.Dispose();

        var act = () => jev.NoulAsync("ticket", "Is it urgent?");

        await act.Should().ThrowAsync<ObjectDisposedException>();
        handler.CallCount.Should().Be(0);
    }

    private sealed class CancellingStream : MemoryStream
    {
        private readonly CancellationToken _cancellationToken;

        public CancellingStream(byte[] buffer, CancellationToken cancellationToken)
            : base(buffer)
        {
            _cancellationToken = cancellationToken;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            _cancellationToken.ThrowIfCancellationRequested();
            return await base.ReadAsync(buffer, cancellationToken);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            return base.ReadAsync(buffer, offset, count, cancellationToken);
        }
    }
}
