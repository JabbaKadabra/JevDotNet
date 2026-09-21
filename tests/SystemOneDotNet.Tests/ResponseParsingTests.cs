namespace SystemOneDotNet.Tests;

public sealed class ResponseParsingTests
{
    [Theory]
    [InlineData("[]")]
    [InlineData("\"text\"")]
    [InlineData("42")]
    [InlineData("null")]
    [InlineData("true")]
    public async Task SendAsync_ResponseRootNotObject_ThrowsProtocol(string body)
    {
        var act = () => AskAsync(body);

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*response body must be a JSON object*");
    }

    [Theory]
    [InlineData("""{"model":"jev-latest","answers":[],"usage":{"input_tokens":1,"output_tokens":2}}""")]
    [InlineData("""{"model":"jev-latest","answers":5,"usage":{"input_tokens":1,"output_tokens":2}}""")]
    public async Task SendAsync_AnswersNotObject_ThrowsProtocol(string body)
    {
        var act = () => AskAsync(body);

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*'answers' property of the response must be a JSON object*");
    }

    [Theory]
    [InlineData("""{"model":"jev-latest","answers":{"q":{"type":"noul","noul":0.5}},"usage":[]}""")]
    [InlineData("""{"model":"jev-latest","answers":{"q":{"type":"noul","noul":0.5}},"usage":"none"}""")]
    public async Task SendAsync_UsageNotObject_ThrowsProtocol(string body)
    {
        var act = () => AskAsync(body);

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*'usage' property of the response must be a JSON object*");
    }

    [Fact]
    public async Task SendAsync_ModelNotString_ThrowsProtocol()
    {
        var act = () => AskAsync("""{"model":5,"answers":{"q":{"type":"noul","noul":0.5}},"usage":{"input_tokens":1,"output_tokens":2}}""");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*'model' property of the answer for question 'response' must be a string*");
    }

    [Theory]
    [InlineData("\"one\"")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("[1]")]
    public async Task SendAsync_TokenCountNotNumber_ThrowsProtocol(string token)
    {
        var act = () => AskAsync(Usage($"\"input_tokens\":{token}"));

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*'usage.input_tokens' property of the response must be a number*");
    }

    [Theory]
    [InlineData("1.0", 1)]
    [InlineData("1e2", 100)]
    [InlineData("2147483647.0", int.MaxValue)]
    public async Task SendAsync_DoubleTokenCount_IsAccepted(string token, int expected)
    {
        using var systemOne = TestClient.Create(RecordingHandler.Json(Usage($"\"input_tokens\":{token}")));

        var result = await systemOne.Query("ticket").Noul(Question.Noul("q", "Is it urgent?")).SendAsync();

        result.Usage.InputTokens.Should().Be(expected);
        result.Usage.OutputTokens.Should().Be(2);
        result.Get(Question.Noul("q", "Is it urgent?")).Noul.Should().Be(0.5);
    }

    [Theory]
    [InlineData("1.5")]
    [InlineData("-1.0")]
    [InlineData("2147483648.0")]
    [InlineData("9999999999")]
    [InlineData("9223372036854775807")]
    public async Task SendAsync_InvalidTokenCount_ThrowsProtocol(string token)
    {
        var act = () => AskAsync(Usage($"\"input_tokens\":{token}"));

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*'usage.input_tokens' property of the response is not a valid token count*");
    }

    [Fact]
    public async Task SendAsync_ExtraAnswersAndUsageProperties_AreIgnored()
    {
        const string body = """
        {
          "model": "jev-latest",
          "answers": {
            "noul": { "type": "noul", "noul": 0.5 },
            "other": { "type": "noul", "noul": 0.9 }
          },
          "usage": { "input_tokens": 1, "output_tokens": 2, "total_tokens": 3 }
        }
        """;

        var answer = await AskAsync(body);

        answer.Noul.Should().Be(0.5);
    }

    private static string Usage(string body) =>
        """{"model":"jev-latest","answers":{"q":{"type":"noul","noul":0.5}},"usage":{""" + body + ""","output_tokens":2}}""";

    private static async Task<NoulAnswer> AskAsync(string body)
    {
        using var systemOne = TestClient.Create(RecordingHandler.Json(body));
        return await systemOne.NoulAsync("ticket", "Is it urgent?");
    }
}
