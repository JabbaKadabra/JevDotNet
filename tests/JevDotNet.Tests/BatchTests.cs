namespace JevDotNet.Tests;

public class BatchTests
{
    private static RecordingHandler MixedHandler() => RecordingHandler.Json(FakeApi.Serialize(FakeApi.Envelope(new
    {
        department = FakeApi.ChoiceAnswer("1", 0.82, new Dictionary<string, double>
        {
            ["0"] = 0.08,
            ["1"] = 0.85,
            ["2"] = 0.07,
        }),
        is_urgent = FakeApi.NoulAnswer(0.92),
        frustration = FakeApi.ScoreAnswer(
            1.6,
            0.78,
            new Dictionary<string, string> { ["0"] = "Calm", ["1"] = "Frustrated", ["2"] = "Very angry" },
            new Dictionary<string, double> { ["0"] = 0.05, ["1"] = 0.3, ["2"] = 0.65 }),
    })));

    [Fact]
    public async Task Mixed_batch_returns_every_typed_answer()
    {
        var department = new Choice<Team>(
            "department",
            "Which team should handle this?",
            new[] { Team.Billing, Team.Technical, Team.Sales });
        var urgent = new Noul("is_urgent", "Does this message convey urgency?");
        var frustration = new Score(
            "frustration",
            "How frustrated is the customer?",
            new[] { "Calm", "Frustrated", "Very angry" });

        var handler = MixedHandler();
        using var jev = TestClient.Create(handler);

        var result = await jev.Query("ticket")
            .Question(department)
            .Question(urgent)
            .Score(frustration)
            .SendAsync();

        result.Get(department).Choice.Should().Be(Team.Technical);
        result.Get(department).Confidence.Should().Be(0.82);
        result.Get(frustration).Score.Should().BeApproximately(1.6, 1e-9);
        result.Get(urgent).Noul.Should().Be(0.92);
        result.QuestionIds.Should().BeEquivalentTo(new[] { "department", "is_urgent", "frustration" });

        var request = handler.Last;
        request.QuestionIds.Should().Equal("department", "is_urgent", "frustration");
        request.Question("department").GetProperty("type").GetString().Should().Be("choice");
        request.Question("is_urgent").GetProperty("type").GetString().Should().Be("noul");
        request.Question("frustration").GetProperty("type").GetString().Should().Be("score");
    }

    [Fact]
    public async Task Typed_builder_methods_accept_matching_questions()
    {
        var handler = MixedHandler();
        using var jev = TestClient.Create(handler);

        var department = new Choice<Team>("department", "Which team?", new[] { Team.Billing, Team.Technical, Team.Sales });
        var urgent = new Noul("is_urgent", "Is it urgent?");
        var frustration = new Score("frustration", "How frustrated?", new[] { "Calm", "Frustrated", "Very angry" });

        var result = await jev.Query("ticket")
            .Choice(department)
            .Noul(urgent)
            .Score(frustration)
            .SendAsync();

        result.Get(department).Choice.Should().Be(Team.Technical);
        result.Get(urgent).Noul.Should().Be(0.92);
        result.Get(frustration).Score.Should().BeApproximately(1.6, 1e-9);
    }

    [Fact]
    public async Task Non_generic_choice_question_uses_named_options_with_descriptions()
    {
        var handler = RecordingHandler.Json(FakeApi.Serialize(FakeApi.Envelope(new
        {
            department = FakeApi.ChoiceAnswer("billing", 0.9, new Dictionary<string, double>
            {
                ["billing"] = 0.9,
                ["technical"] = 0.1,
            }),
        })));
        using var jev = TestClient.Create(handler);

        var department = new ChoiceQuestion("department", "Which team should handle this?", new Dictionary<string, string?>
        {
            ["billing"] = "Payment or subscription issues",
            ["technical"] = null,
        });

        var result = await jev.Query("ticket").Question(department).SendAsync();

        result.Get(department).Choice.Should().Be("billing");
        result.Get(department).Options[1].Option.Should().Be("technical");
        result.Get(department).Options[1].Probability.Should().Be(0.1);

        var criteria = handler.Last.Question("department").GetProperty("criteria");
        criteria.GetProperty("billing").GetString().Should().Be("Payment or subscription issues");
        criteria.GetProperty("technical").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Null);
    }

    [Fact]
    public async Task Question_ids_key_the_answers_and_are_not_used_in_inference()
    {
        var handler = MixedHandler();
        using var jev = TestClient.Create(handler);

        await jev.Query("ticket")
            .Noul(new Noul("is_urgent", "Is it urgent?"))
            .SendAsync();

        var questions = handler.Last.Json.GetProperty("questions");
        questions.EnumerateObject().Should().ContainSingle(property => property.Name == "is_urgent");
        questions.GetProperty("is_urgent").GetProperty("instructions").GetString().Should().Be("Is it urgent?");
    }

    [Fact]
    public async Task Duplicate_question_ids_are_rejected_without_sending_a_request()
    {
        var handler = MixedHandler();
        using var jev = TestClient.Create(handler);

        var query = jev.Query("ticket")
            .Noul(new Noul("same_id", "Is it urgent?"));

        var act = () => query.Noul(new Noul("same_id", "Is it important?"));

        act.Should().Throw<JevValidationException>()
            .WithMessage("*'same_id' is already used in this batch*");
        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Repeated_question_handles_are_rejected_without_sending_a_request()
    {
        var handler = MixedHandler();
        using var jev = TestClient.Create(handler);

        var urgent = new Noul("is_urgent", "Is it urgent?");
        var query = jev.Query("ticket").Question(urgent);

        var act = () => query.Question(urgent);

        act.Should().Throw<JevValidationException>()
            .WithMessage("*was already added to this batch*");
        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Empty_batches_are_rejected_without_sending_a_request()
    {
        var handler = MixedHandler();
        using var jev = TestClient.Create(handler);

        var act = () => jev.Query("ticket").SendAsync();

        await act.Should().ThrowAsync<JevValidationException>()
            .WithMessage("*at least one question*");
        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Get_throws_for_a_question_that_was_not_in_the_batch()
    {
        var handler = MixedHandler();
        using var jev = TestClient.Create(handler);

        var result = await jev.Query("ticket").Noul(new Noul("is_urgent", "Is it urgent?")).SendAsync();
        var other = new Noul("other", "Is it important?");

        var act = () => result.Get(other);

        act.Should().Throw<JevValidationException>()
            .WithMessage("*does not contain an answer for question 'other'*");
    }

    [Fact]
    public async Task Get_throws_when_the_answer_type_does_not_match_the_question()
    {
        var handler = MixedHandler();
        using var jev = TestClient.Create(handler);

        var result = await jev.Query("ticket").Noul(new Noul("is_urgent", "Is it urgent?")).SendAsync();

        var act = () => result.Get(new Score("is_urgent", "How frustrated?", new[] { "Calm", "Angry" }));

        act.Should().Throw<JevProtocolException>()
            .WithMessage("*cannot be read as*");
    }
}
