namespace SystemOneDotNet.Tests;

public sealed class NamedChoiceAnswerParsingTests
{
    [Fact]
    public void NamedChoiceQuestion_ExposesIdInstructionsOptionsAndDescriptions()
    {
        var question = Question.NamedChoice("department", "Which team?", new Dictionary<string, string?>
        {
            ["billing"] = "Payment or subscription issues",
            ["technical"] = null,
        });

        question.Id.Should().Be("department");
        question.Instructions.Should().Be("Which team?");
        question.Options.Should().Equal("billing", "technical");
        question.Descriptions.Should().Equal("Payment or subscription issues", null);
    }

    [Fact]
    public void NamedChoiceQuestion_MoreThan255Options_Throws()
    {
        var options = Enumerable.Range(0, 256).ToDictionary(index => "option-" + index, _ => (string?)null);

        var act = () => Question.NamedChoice("department", "Which team?", options);

        act.Should().Throw<SystemOneValidationException>()
            .WithMessage("*defines 256 options; the limit is 255*");
    }

    [Fact]
    public async Task SendAsync_DuplicateProbabilityName_ThrowsProtocol()
    {
        var act = () => SendAsync("""{"type":"choice","choice":"billing","probabilities":{"billing":0.9,"billing":0.1},"confidence":1.0}""");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*duplicate probabilities for the option 'billing'*");
    }

    [Fact]
    public async Task SendAsync_ProbabilityNotNumber_ThrowsProtocol()
    {
        var act = () => SendAsync("""{"type":"choice","choice":"billing","probabilities":{"billing":"high","technical":0.1},"confidence":1.0}""");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*probability for the option 'billing' of question 'department' must be a number*");
    }

    [Fact]
    public async Task SendAsync_ProbabilityOutOfRange_ThrowsProtocol()
    {
        var act = () => SendAsync("""{"type":"choice","choice":"billing","probabilities":{"billing":1.5,"technical":0.1},"confidence":1.0}""");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*probability for the option 'billing' for question 'department' must be between 0 and 1*");
    }

    [Fact]
    public async Task SendAsync_MissingProbabilityForOption_ThrowsProtocol()
    {
        var act = () => SendAsync("""{"type":"choice","choice":"billing","probabilities":{"billing":0.9},"confidence":1.0}""");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*missing the probability for the option 'technical'*");
    }

    [Fact]
    public async Task SendAsync_UnknownProbabilityOption_ThrowsProtocol()
    {
        var act = () => SendAsync("""{"type":"choice","choice":"billing","probabilities":{"billing":0.9,"technical":0.1,"sales":0.0},"confidence":1.0}""");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*contains the unknown option 'sales'*");
    }

    [Fact]
    public async Task SendAsync_UnknownSelectedOption_ThrowsProtocol()
    {
        var act = () => SendAsync("""{"type":"choice","choice":"sales","probabilities":{"billing":0.9,"technical":0.1},"confidence":1.0}""");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*selected the unknown option 'sales'*");
    }

    [Fact]
    public async Task SendAsync_ConfidenceOutOfRange_ThrowsProtocol()
    {
        var act = () => SendAsync("""{"type":"choice","choice":"billing","probabilities":{"billing":0.9,"technical":0.1},"confidence":-0.2}""");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*must be between 0 and 1*");
    }

    private static RecordingHandler Handler(string answer) =>
        RecordingHandler.Json(FakeApi.EnvelopeWithAnswer("department", answer));

    private static async Task<ChoiceAnswer<string>> SendAsync(string answer)
    {
        using var systemOne = TestClient.Create(Handler(answer));
        var question = Question.NamedChoice("department", "Which team?", new Dictionary<string, string?>
        {
            ["billing"] = "Payment or subscription issues",
            ["technical"] = null,
        });

        return await systemOne.AskAsync("ticket", question);
    }
}
