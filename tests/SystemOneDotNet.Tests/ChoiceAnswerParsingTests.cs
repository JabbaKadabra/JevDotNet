namespace SystemOneDotNet.Tests;

public sealed class ChoiceAnswerParsingTests
{
    [Fact]
    public void ChoiceQuestion_ExposesIdInstructionsAndOptions()
    {
        var question = Question.Choice("choice", "Which team?", new[] { "billing", "technical" });

        question.Id.Should().Be("choice");
        question.Instructions.Should().Be("Which team?");
        question.Options.Should().Equal("billing", "technical");
    }

    [Fact]
    public async Task ChoiceAsync_MaximumOptionCount_IsAccepted()
    {
        var options = Enumerable.Range(0, 255).Select(index => "option-" + index).ToArray();
        var probabilities = string.Join(",", Enumerable.Range(0, 255).Select(index => $"\"{index}\":0.5"));
        var handler = Handler($$"""{"type":"choice","choice":"254","probabilities":{{{probabilities}}},"confidence":1.0}""");
        using var systemOne = TestClient.Create(handler);

        var answer = await systemOne.ChoiceAsync("ticket", "Which option?", options);

        answer.Choice.Should().Be("option-254");
        answer.Options.Should().HaveCount(255);
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task ChoiceAsync_AnswerNotObject_ThrowsProtocol()
    {
        var act = () => SendAsync("42");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*answer for question 'choice' must be a JSON object*");
    }

    [Fact]
    public async Task ChoiceAsync_ChoiceNotString_ThrowsProtocol()
    {
        var act = () => SendAsync("""{"type":"choice","choice":0,"probabilities":{"0":0.5,"1":0.5},"confidence":1.0}""");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*'choice' property of the answer for question 'choice' must be a string*");
    }

    [Fact]
    public async Task ChoiceAsync_TypeNotString_ThrowsProtocol()
    {
        var act = () => SendAsync("""{"type":5,"choice":"0","probabilities":{"0":0.5,"1":0.5},"confidence":1.0}""");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*'type' property of the answer for question 'choice' must be a string*");
    }

    [Fact]
    public async Task ChoiceAsync_MissingProbabilities_ThrowsProtocol()
    {
        var act = () => SendAsync("""{"type":"choice","choice":"0","confidence":1.0}""");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*answer for question 'choice' is missing the 'probabilities' property*");
    }

    [Fact]
    public async Task ChoiceAsync_ProbabilitiesNotObject_ThrowsProtocol()
    {
        var act = () => SendAsync("""{"type":"choice","choice":"0","probabilities":[],"confidence":1.0}""");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*answer for question 'choice' must be a JSON object*");
    }

    [Fact]
    public async Task ChoiceAsync_MissingConfidence_ThrowsProtocol()
    {
        var act = () => SendAsync("""{"type":"choice","choice":"0","probabilities":{"0":0.5,"1":0.5}}""");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*answer for question 'choice' is missing the 'confidence' property*");
    }

    [Fact]
    public async Task ChoiceAsync_ConfidenceNotNumber_ThrowsProtocol()
    {
        var act = () => SendAsync("""{"type":"choice","choice":"0","probabilities":{"0":0.5,"1":0.5},"confidence":"high"}""");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*'confidence' property of the answer for question 'choice' must be a number*");
    }

    [Fact]
    public async Task ChoiceAsync_DuplicateProbabilityIndex_ThrowsProtocol()
    {
        var act = () => SendAsync("""{"type":"choice","choice":"0","probabilities":{"0":0.5,"0":0.5},"confidence":1.0}""");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*duplicate probabilities for option id '0'*");
    }

    [Fact]
    public async Task ChoiceAsync_ProbabilityNotNumber_ThrowsProtocol()
    {
        var act = () => SendAsync("""{"type":"choice","choice":"0","probabilities":{"0":"half","1":0.5},"confidence":1.0}""");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*probability for option id '0' of question 'choice' must be a number*");
    }

    [Fact]
    public async Task ChoiceAsync_ProbabilityOutOfRange_ThrowsProtocol()
    {
        var act = () => SendAsync("""{"type":"choice","choice":"0","probabilities":{"0":1.2,"1":0.5},"confidence":1.0}""");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*probability for option id '0' for question 'choice' must be between 0 and 1*");
    }

    private static RecordingHandler Handler(string answer) =>
        RecordingHandler.Json(FakeApi.EnvelopeWithAnswer("choice", answer));

    private static async Task<ChoiceAnswer<string>> SendAsync(string answer)
    {
        using var systemOne = TestClient.Create(Handler(answer));
        return await systemOne.ChoiceAsync("ticket", "Which team?", new[] { "billing", "technical" });
    }
}
