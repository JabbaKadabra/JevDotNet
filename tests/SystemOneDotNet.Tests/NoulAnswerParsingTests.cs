namespace SystemOneDotNet.Tests;

public sealed class NoulAnswerParsingTests
{
    [Fact]
    public void NoulQuestion_ExposesIdInstructionsAndDescriptions()
    {
        var question = Question.Noul("is_urgent", "Is it urgent?", "Yes, immediately", "No");

        question.Id.Should().Be("is_urgent");
        question.Instructions.Should().Be("Is it urgent?");
        question.TrueDescription.Should().Be("Yes, immediately");
        question.FalseDescription.Should().Be("No");
    }

    [Fact]
    public void NoulQuestion_DefaultDescriptions_AreNull()
    {
        var question = Question.Noul("is_urgent", "Is it urgent?");

        question.TrueDescription.Should().BeNull();
        question.FalseDescription.Should().BeNull();
    }

    [Fact]
    public async Task NoulAsync_TrueDescriptionOnly_SendsOnlyTrueCriteria()
    {
        var handler = Handler(FakeApi.Serialize(FakeApi.NoulAnswer(0.9)), "is_urgent");
        using var systemOne = TestClient.Create(handler);

        await systemOne.AskAsync("ticket", Question.Noul("is_urgent", "Is it urgent?", trueDescription: "Explicitly time-sensitive"));

        var criteria = handler.Last.Question("is_urgent").GetProperty("criteria");
        criteria.GetProperty("true").GetString().Should().Be("Explicitly time-sensitive");
        criteria.TryGetProperty("false", out _).Should().BeFalse();
    }

    [Fact]
    public async Task NoulAsync_FalseDescriptionOnly_SendsOnlyFalseCriteria()
    {
        var handler = Handler(FakeApi.Serialize(FakeApi.NoulAnswer(0.1)), "is_urgent");
        using var systemOne = TestClient.Create(handler);

        await systemOne.AskAsync("ticket", Question.Noul("is_urgent", "Is it urgent?", falseDescription: "No urgency expressed"));

        var criteria = handler.Last.Question("is_urgent").GetProperty("criteria");
        criteria.GetProperty("false").GetString().Should().Be("No urgency expressed");
        criteria.TryGetProperty("true", out _).Should().BeFalse();
    }

    [Theory]
    [InlineData(1.5)]
    [InlineData(-0.1)]
    public async Task NoulAsync_NoulOutOfRange_ThrowsProtocol(double noul)
    {
        using var systemOne = TestClient.Create(Handler(FakeApi.Serialize(FakeApi.NoulAnswer(noul))));

        var act = () => systemOne.NoulAsync("ticket", "Is it urgent?");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*must be between 0 and 1*");
    }

    [Fact]
    public async Task NoulAsync_NoulNotNumber_ThrowsProtocol()
    {
        using var systemOne = TestClient.Create(Handler("""{"type":"noul","noul":"high"}"""));

        var act = () => systemOne.NoulAsync("ticket", "Is it urgent?");

        await act.Should().ThrowAsync<SystemOneProtocolException>()
            .WithMessage("*'noul' property of the answer for question 'noul' must be a number*");
    }

    private static RecordingHandler Handler(string answerJson, string questionId = "noul") =>
        RecordingHandler.Json(FakeApi.EnvelopeWithAnswer(questionId, answerJson));
}
