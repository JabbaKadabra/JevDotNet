namespace SystemOneDotNet.Tests;

public sealed class ScoreAnswerParsingTests
{
    private const string Legend = """{"0":"Calm","1":"Frustrated","2":"Angry"}""";

    [Fact]
    public void ScoreQuestion_ExposesIdInstructionsAndLevels()
    {
        var question = Question.Score("score", "How frustrated?", new[] { "Calm", "Angry" });

        question.Id.Should().Be("score");
        question.Instructions.Should().Be("How frustrated?");
        question.Levels.Should().Equal("Calm", "Angry");
    }

    [Fact]
    public async Task ScoreAsync_DuplicateLegendEntry_ThrowsProtocol()
    {
        var act = () => SendAsync($$"""{"type":"score","score":1.0,"legend":{"0":"Calm","0":"Calm","1":"Frustrated","2":"Angry"},"confidence":0.5}""");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*duplicate legend entries for level id '0'*");
    }

    [Fact]
    public async Task ScoreAsync_LegendEntryNotString_ThrowsProtocol()
    {
        var act = () => SendAsync("""{"type":"score","score":1.0,"legend":{"0":5,"1":"Frustrated","2":"Angry"},"confidence":0.5}""");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*legend entry '0' of question 'score' must be a string*");
    }

    [Fact]
    public async Task ScoreAsync_UnknownLegendEntry_ThrowsProtocol()
    {
        var act = () => SendAsync("""{"type":"score","score":1.0,"legend":{"0":"Calm","1":"Frustrated","2":"Angry","3":"Extra"},"confidence":0.5}""");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*unknown option id '3'*");
    }

    [Fact]
    public async Task ScoreAsync_MissingLegend_ThrowsProtocol()
    {
        var act = () => SendAsync("""{"type":"score","score":1.0,"confidence":0.5}""");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*answer for question 'score' is missing the 'legend' property*");
    }

    [Fact]
    public async Task ScoreAsync_ProbabilitiesNotObject_ThrowsProtocol()
    {
        var act = () => SendAsync($$"""{"type":"score","score":1.0,"legend":{{Legend}},"probabilities":[],"confidence":0.5}""");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*'probabilities' property of the answer for question 'score' must be an object*");
    }

    [Fact]
    public async Task ScoreAsync_ExplicitNullProbabilities_LeavesProbabilitiesNull()
    {
        var answer = await SendAsync($$"""{"type":"score","score":1.0,"legend":{{Legend}},"probabilities":null,"confidence":0.5}""");

        answer.Probabilities.Should().BeNull();
    }

    [Fact]
    public async Task ScoreAsync_DuplicateProbabilityLevel_ThrowsProtocol()
    {
        var act = () => SendAsync($$"""{"type":"score","score":1.0,"legend":{{Legend}},"probabilities":{"0":0.5,"0":0.5,"1":0.5},"confidence":0.5}""");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*duplicate probabilities for level id '0'*");
    }

    [Fact]
    public async Task ScoreAsync_ProbabilityNotNumber_ThrowsProtocol()
    {
        var act = () => SendAsync($$"""{"type":"score","score":1.0,"legend":{{Legend}},"probabilities":{"0":"half","1":0.5,"2":0.5},"confidence":0.5}""");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*probability for level id '0' of question 'score' must be a number*");
    }

    [Fact]
    public async Task ScoreAsync_ProbabilityOutOfRange_ThrowsProtocol()
    {
        var act = () => SendAsync($$"""{"type":"score","score":1.0,"legend":{{Legend}},"probabilities":{"0":-0.1,"1":0.5,"2":0.5},"confidence":0.5}""");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*probability for level id '0' for question 'score' must be between 0 and 1*");
    }

    [Fact]
    public async Task ScoreAsync_MissingProbabilityLevel_ThrowsProtocol()
    {
        var act = () => SendAsync($$"""{"type":"score","score":1.0,"legend":{{Legend}},"probabilities":{"0":0.5,"1":0.5},"confidence":0.5}""");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*missing the probability for level id '2'*");
    }

    [Fact]
    public async Task ScoreAsync_ScoreNotNumber_ThrowsProtocol()
    {
        var act = () => SendAsync($$"""{"type":"score","score":"high","legend":{{Legend}},"confidence":0.5}""");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*'score' property of the answer for question 'score' must be a number*");
    }

    [Fact]
    public async Task ScoreAsync_ScoreBelowRange_ThrowsProtocol()
    {
        var act = () => SendAsync($$"""{"type":"score","score":-0.5,"legend":{{Legend}},"confidence":0.5}""");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*must be between 0 and 2*");
    }

    [Fact]
    public async Task ScoreAsync_ScoreWithinTolerance_IsAccepted()
    {
        var answer = await SendAsync($$"""{"type":"score","score":2.0000005,"legend":{{Legend}},"confidence":0.5}""");

        answer.Score.Should().Be(2.0000005);
    }

    [Fact]
    public async Task ScoreAsync_ScoreAboveTolerance_ThrowsProtocol()
    {
        var act = () => SendAsync($$"""{"type":"score","score":2.0000011,"legend":{{Legend}},"confidence":0.5}""");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*must be between 0 and 2*");
    }

    private static RecordingHandler Handler(string answer) =>
        RecordingHandler.Json(FakeApi.EnvelopeWithAnswer("score", answer));

    private static async Task<ScoreAnswer> SendAsync(string answer)
    {
        using var systemOne = TestClient.Create(Handler(answer));
        return await systemOne.ScoreAsync("ticket", "How frustrated?", new[] { "Calm", "Frustrated", "Angry" });
    }
}
