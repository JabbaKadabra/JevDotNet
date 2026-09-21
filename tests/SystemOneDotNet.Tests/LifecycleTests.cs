using System.Net;
using System.Net.Http;
using NSubstitute;

namespace SystemOneDotNet.Tests;

public sealed class LifecycleTests
{
    private const string ValidResponse = """{"model":"jev-latest","answers":{"noul":{"type":"noul","noul":0.5}},"usage":{"input_tokens":1,"output_tokens":2}}""";

    [Fact]
    public async Task SystemOne_SuppliedHttpClient_IsUsedAndNotDisposed()
    {
        var handler = RecordingHandler.Json(ValidResponse);
        using var inner = new HttpClient(handler);
        var httpClient = Substitute.For<HttpClient>();
        httpClient.SendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(call => inner.SendAsync(call.Arg<HttpRequestMessage>(), call.Arg<CancellationToken>()));

        var systemOne = new SystemOne("test-key", null, httpClient);

        var answer = await systemOne.NoulAsync("ticket", "Is it urgent?");
        answer.Noul.Should().Be(0.5);
        handler.CallCount.Should().Be(1);

        systemOne.Dispose();

        httpClient.DidNotReceive().Dispose();
    }

    [Fact]
    public async Task SystemOne_OwnedHttpClientDisposed_ThrowsOnNextUse()
    {
        var handler = RecordingHandler.Json(ValidResponse);
        var systemOne = new SystemOne("test-key", null, new HttpClient(handler));

        await systemOne.NoulAsync("ticket", "Is it urgent?");
        systemOne.Dispose();

        var act = () => systemOne.NoulAsync("ticket", "Is it urgent?");
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public void SystemOne_DisposeTwice_DoesNotThrow()
    {
        var systemOne = TestClient.Create(RecordingHandler.Json(ValidResponse));

        systemOne.Dispose();
        var act = () => systemOne.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public async Task SystemOne_ConcurrentRequests_AreIsolated()
    {
        var handler = RecordingHandler.RespondingAsync(async request =>
        {
            var body = request.Body;
            var id = System.Text.Json.JsonDocument.Parse(body).RootElement
                .GetProperty("questions").EnumerateObject().Single().Name;
            await Task.Delay(20);
            return FakeApi.Message(FakeApi.Serialize(FakeApi.Envelope(new
            {
                answer = FakeApi.NoulAnswer(id == "first" ? 0.1 : 0.9),
            })).Replace("\"answer\"", $"\"{id}\""));
        });
        using var systemOne = TestClient.Create(handler);

        var first = systemOne.Query("first").Noul(new Noul("first", "Is it urgent?")).SendAsync();
        var second = systemOne.Query("second").Noul(new Noul("second", "Is it urgent?")).SendAsync();
        var results = await Task.WhenAll(first, second);

        results[0].Get(new Noul("first", "Is it urgent?")).Noul.Should().Be(0.1);
        results[1].Get(new Noul("second", "Is it urgent?")).Noul.Should().Be(0.9);
        handler.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task SystemOne_MultipleRequests_ReusesClient()
    {
        var handler = RecordingHandler.Json(ValidResponse);
        using var systemOne = TestClient.Create(handler);

        for (var index = 0; index < 5; index++)
        {
            await systemOne.NoulAsync("ticket", "Is it urgent?");
        }

        handler.CallCount.Should().Be(5);
    }

    [Fact]
    public async Task SystemOne_EveryRequest_SetsBearerAuthentication()
    {
        var handler = RecordingHandler.Json(ValidResponse);
        using var inner = new HttpClient(handler);
        var httpClient = Substitute.For<HttpClient>();
        var authorizationHeaders = new List<string?>();
        httpClient.SendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var message = call.Arg<HttpRequestMessage>();
                authorizationHeaders.Add(
                    message.Headers.TryGetValues("Authorization", out var values) ? values.FirstOrDefault() : null);
                return inner.SendAsync(message, call.Arg<CancellationToken>());
            });

        using var systemOne = new SystemOne("secret-key", null, httpClient);

        await systemOne.NoulAsync("ticket", "Is it urgent?");
        await systemOne.NoulAsync("ticket", "Is it urgent?");

        authorizationHeaders.Should().Equal("Bearer secret-key", "Bearer secret-key");
        await httpClient.Received(2).SendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SystemOne_UnsuccessfulResponse_DoesNotRetry()
    {
        var handler = RecordingHandler.Responding(_ => FakeApi.Message("{}", HttpStatusCode.TooManyRequests));
        using var systemOne = TestClient.Create(handler);

        var act = () => systemOne.NoulAsync("ticket", "Is it urgent?");
        await act.Should().ThrowAsync<SystemOneApiException>();

        handler.CallCount.Should().Be(1);
    }
}
