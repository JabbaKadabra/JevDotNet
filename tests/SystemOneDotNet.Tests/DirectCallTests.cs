using System.Text.Json;

namespace SystemOneDotNet.Tests;

public sealed class DirectCallTests
{
    [Fact]
    public async Task ChoiceAsync_ValidAnswer_SendsQuestionAndReturnsTypedAnswer()
    {
        var handler = RecordingHandler.Json(FakeApi.Serialize(FakeApi.Envelope(new
        {
            choice = FakeApi.ChoiceAnswer("1", 0.82, new Dictionary<string, double>
            {
                ["0"] = 0.1,
                ["1"] = 0.8,
                ["2"] = 0.1,
            }),
        })));
        using var systemOne = TestClient.Create(handler);

        var answer = await systemOne.ChoiceAsync(
            "My running shoes arrived in the wrong size.",
            "Which team should handle this?",
            new[] { "billing", "technical", "sales" });

        answer.Choice.Should().Be("technical");
        answer.Confidence.Should().Be(0.82);
        answer.Options.Should().HaveCount(3);
        answer.Options[0].Option.Should().Be("billing");
        answer.Options[0].Probability.Should().Be(0.1);
        answer.Options[2].Probability.Should().Be(0.1);

        var request = handler.Last;
        request.Authorization.Should().Be("Bearer test-key");
        request.ContentType.Should().Be("application/json");
        request.Message.Method.Should().Be(HttpMethod.Post);
        request.Json.GetProperty("state").GetString().Should().Be("My running shoes arrived in the wrong size.");
        request.Json.GetProperty("model").GetString().Should().Be("jev-latest");

        var question = request.Question("choice");
        question.GetProperty("type").GetString().Should().Be("choice");
        question.GetProperty("instructions").GetString().Should().Be("Which team should handle this?");
        question.GetProperty("criteria").GetProperty("0").GetString().Should().Be("billing");
        question.GetProperty("criteria").GetProperty("2").GetString().Should().Be("sales");
    }

    [Fact]
    public async Task ChoiceAsync_PocoOptions_SerializesStructuredDescriptionsAndMapsByReference()
    {
        var teams = new[]
        {
            new TeamInfo("Billing", "billing@example.com"),
            new TeamInfo("Technical", "technical@example.com"),
            new TeamInfo("Sales", "sales@example.com"),
        };

        var body = FakeApi.Serialize(FakeApi.Envelope(new
        {
            choice = FakeApi.ChoiceAnswer("1", 0.71, new Dictionary<string, double>
            {
                ["0"] = 0.19,
                ["1"] = 0.71,
                ["2"] = 0.1,
            }),
        }));
        var handler = RecordingHandler.Json(body);
        using var systemOne = TestClient.Create(handler);

        var answer = await systemOne.ChoiceAsync("ticket", "Which team should handle this?", teams);

        answer.Choice.Should().BeSameAs(teams[1]);
        answer.Options[0].Option.Should().BeSameAs(teams[0]);
        answer.Options[2].Option.Should().BeSameAs(teams[2]);

        var criteria = handler.Last.Question("choice").GetProperty("criteria");
        criteria.GetProperty("1").GetProperty("Name").GetString().Should().Be("Technical");
        criteria.GetProperty("1").GetProperty("Email").GetString().Should().Be("technical@example.com");
    }

    [Fact]
    public async Task ChoiceAsync_EnumOptions_SerializesByName()
    {
        var handler = RecordingHandler.Json(FakeApi.Serialize(FakeApi.Envelope(new
        {
            choice = FakeApi.ChoiceAnswer("1", 0.9, new Dictionary<string, double>
            {
                ["0"] = 0.1,
                ["1"] = 0.9,
            }),
        })));
        using var systemOne = TestClient.Create(handler);

        var answer = await systemOne.ChoiceAsync("ticket", "Which team?", new[] { Team.Billing, Team.Technical });

        answer.Choice.Should().Be(Team.Technical);
        var criteria = handler.Last.Question("choice").GetProperty("criteria");
        criteria.GetProperty("0").GetString().Should().Be("Billing");
        criteria.GetProperty("1").GetString().Should().Be("Technical");
    }

    [Fact]
    public async Task NoulAsync_PocoState_SendsJsonObject()
    {
        var handler = RecordingHandler.Json(FakeApi.Serialize(FakeApi.Envelope(new
        {
            noul = FakeApi.NoulAnswer(0.92),
        })));
        using var systemOne = TestClient.Create(handler);

        await systemOne.NoulAsync(new { id = 42, subject = "Payouts failing" }, "Is this urgent?");

        var state = handler.Last.Json.GetProperty("state");
        state.ValueKind.Should().Be(JsonValueKind.Object);
        state.GetProperty("id").GetInt32().Should().Be(42);
        state.GetProperty("subject").GetString().Should().Be("Payouts failing");
    }

    [Fact]
    public async Task NoulAsync_ArrayState_SendsJsonArray()
    {
        var handler = RecordingHandler.Json(FakeApi.Serialize(FakeApi.Envelope(new
        {
            noul = FakeApi.NoulAnswer(0.1),
        })));
        using var systemOne = TestClient.Create(handler);

        await systemOne.NoulAsync(new[] { "first message", "second message" }, "Is this urgent?");

        var state = handler.Last.Json.GetProperty("state");
        state.ValueKind.Should().Be(JsonValueKind.Array);
        state.EnumerateArray().Select(element => element.GetString())
            .Should().Equal("first message", "second message");
    }

    [Fact]
    public async Task ScoreAsync_ValidAnswer_SendsLevelsAndReturnsMetadata()
    {
        var handler = RecordingHandler.Json(FakeApi.Serialize(FakeApi.Envelope(new
        {
            score = FakeApi.ScoreAnswer(
                1.6,
                0.78,
                new Dictionary<string, string> { ["0"] = "Calm", ["1"] = "Frustrated", ["2"] = "Very angry" },
                new Dictionary<string, double> { ["0"] = 0.05, ["1"] = 0.3, ["2"] = 0.65 }),
        })));
        using var systemOne = TestClient.Create(handler);

        var answer = await systemOne.ScoreAsync(
            "ticket",
            "How frustrated is the customer?",
            new[] { "Calm", "Frustrated", "Very angry" });

        answer.Score.Should().BeApproximately(1.6, 1e-9);
        answer.Confidence.Should().Be(0.78);
        answer.Legend.Should().Equal("Calm", "Frustrated", "Very angry");
        var probabilities = answer.Probabilities
            ?? throw new InvalidOperationException("Expected the probability distribution to be present.");
        probabilities[2].LevelIndex.Should().Be(2);
        probabilities[2].Level.Should().Be("Very angry");
        probabilities[2].Probability.Should().Be(0.65);

        var question = handler.Last.Question("score");
        question.GetProperty("type").GetString().Should().Be("score");
        question.GetProperty("criteria").EnumerateArray().Select(element => element.GetString())
            .Should().Equal("Calm", "Frustrated", "Very angry");
    }

    [Fact]
    public async Task ScoreAsync_MissingProbabilities_LeavesProbabilitiesNull()
    {
        var handler = RecordingHandler.Json(FakeApi.Serialize(FakeApi.Envelope(new
        {
            score = FakeApi.ScoreAnswer(
                1.035,
                0.842,
                new Dictionary<string, string> { ["0"] = "Calm", ["1"] = "Frustrated", ["2"] = "Very angry" }),
        })));
        using var systemOne = TestClient.Create(handler);

        var answer = await systemOne.ScoreAsync("ticket", "How frustrated?", new[] { "Calm", "Frustrated", "Very angry" });

        answer.Score.Should().BeApproximately(1.035, 1e-9);
        answer.Probabilities.Should().BeNull();
    }

    [Fact]
    public async Task NoulAsync_NoDescriptions_OmitsCriteria()
    {
        var handler = RecordingHandler.Json(FakeApi.Serialize(FakeApi.Envelope(new
        {
            noul = FakeApi.NoulAnswer(0.999),
        })));
        using var systemOne = TestClient.Create(handler);

        var answer = await systemOne.NoulAsync("ticket", "Does this message convey urgency?");

        answer.Noul.Should().Be(0.999);
        var question = handler.Last.Question("noul");
        question.GetProperty("type").GetString().Should().Be("noul");
        question.TryGetProperty("criteria", out _).Should().BeFalse();
    }

    [Fact]
    public async Task NoulAsync_DescriptionsSupplied_SendsCriteria()
    {
        var handler = RecordingHandler.Json(FakeApi.Serialize(FakeApi.Envelope(new
        {
            is_urgent = FakeApi.NoulAnswer(0.8),
        })));
        using var systemOne = TestClient.Create(handler);

        var question = new Noul(
            "is_urgent",
            "Does this message convey urgency?",
            trueDescription: "Explicitly time-sensitive",
            falseDescription: "No urgency expressed");

        await systemOne.AskAsync("ticket", question);

        var criteria = handler.Last.Question("is_urgent").GetProperty("criteria");
        criteria.GetProperty("true").GetString().Should().Be("Explicitly time-sensitive");
        criteria.GetProperty("false").GetString().Should().Be("No urgency expressed");
    }

    [Fact]
    public async Task AskAsync_ReusableQuestion_ReturnsTypedAnswer()
    {
        var department = new Choice<Team>(
            "department",
            "Which team should handle this?",
            new[] { Team.Billing, Team.Technical, Team.Sales });

        var handler = RecordingHandler.Json(FakeApi.Serialize(FakeApi.Envelope(new
        {
            department = FakeApi.ChoiceAnswer("2", 0.6, new Dictionary<string, double>
            {
                ["0"] = 0.1,
                ["1"] = 0.3,
                ["2"] = 0.6,
            }),
        })));
        using var systemOne = TestClient.Create(handler);

        var answer = await systemOne.AskAsync("ticket", department);

        answer.Choice.Should().Be(Team.Sales);
        handler.Last.QuestionIds.Should().Equal("department");
    }

    [Fact]
    public async Task SendAsync_CompletedBatch_ExposesModelAndUsage()
    {
        var handler = RecordingHandler.Json(FakeApi.Serialize(
            FakeApi.Envelope(new { q = FakeApi.NoulAnswer(0.5) }, model: "systemOne-2026-01", inputTokens: 700, outputTokens: 120)));
        using var systemOne = TestClient.Create(handler);

        var result = await systemOne.Query("ticket").Noul(new Noul("q", "Is it urgent?")).SendAsync();

        result.Model.Should().Be("systemOne-2026-01");
        result.Usage.InputTokens.Should().Be(700);
        result.Usage.OutputTokens.Should().Be(120);
    }

    [Fact]
    public async Task SystemOneOptions_CustomValues_OverrideEndpointAndModel()
    {
        var handler = RecordingHandler.Json(FakeApi.Serialize(FakeApi.Envelope(new
        {
            noul = FakeApi.NoulAnswer(0.5),
        })));
        using var systemOne = TestClient.Create(handler, new SystemOneOptions
        {
            Endpoint = "https://example.test/systemOne",
            Model = "systemOne-2026-01",
        });

        await systemOne.NoulAsync("ticket", "Is it urgent?");

        handler.Last.Message.RequestUri.Should().Be(new Uri("https://example.test/systemOne"));
        handler.Last.Json.GetProperty("model").GetString().Should().Be("systemOne-2026-01");
    }

    [Fact]
    public async Task SendAsync_ReusedBuilder_SnapshotsQuestionsPerSend()
    {
        var handler = RecordingHandler.Json(FakeApi.Serialize(FakeApi.Envelope(new
        {
            q = FakeApi.NoulAnswer(0.5),
            q2 = FakeApi.NoulAnswer(0.5),
        })));
        using var systemOne = TestClient.Create(handler);

        var query = systemOne.Query("first");
        await query.Noul(new Noul("q", "Is it urgent?")).SendAsync();
        await query.Noul(new Noul("q2", "Is it urgent?")).SendAsync();

        handler.CallCount.Should().Be(2);
        handler.Requests[0].Json.GetProperty("state").GetString().Should().Be("first");
        handler.Requests[0].QuestionIds.Should().Equal("q");
        handler.Requests[1].QuestionIds.Should().Equal("q", "q2");
    }
}
