using System.Text.Json;
using System.Text.Json.Nodes;

namespace SystemOneDotNet.Tests;

public sealed class PropertyLimitTests
{
    private const string ChoiceResponse = """{"model":"jev-latest","answers":{"choice":{"type":"choice","choice":"0","probabilities":{"0":1.0},"confidence":1.0}},"usage":{"input_tokens":1,"output_tokens":1}}""";

    [Fact]
    public async Task ChoiceAsync_TenProperties_AcceptedWithDefaultLimit()
    {
        var handler = RecordingHandler.Json(ChoiceResponse);
        using var systemOne = TestClient.Create(handler);

        await systemOne.ChoiceAsync("ticket", "Which option?", new[] { new TenProperties() });

        handler.CallCount.Should().Be(1);
        handler.Last.Question("choice").GetProperty("criteria").GetProperty("0").GetProperty("P1").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task ChoiceAsync_ElevenPropertiesWithLimitTen_ThrowsAndSendsNothing()
    {
        var handler = RecordingHandler.Json(ChoiceResponse);
        using var systemOne = TestClient.Create(handler, new SystemOneOptions { MaxChoiceProperties = 10 });

        var act = () => systemOne.ChoiceAsync("ticket", "Which option?", new[] { new ElevenProperties() });

        var exception = await act.Should().ThrowAsync<SystemOneValidationException>();
        exception.Which.Message.Should().Be(
            "Choice option 0 contains 11 serialized properties; the limit is 10.\n" +
            "Use a dedicated smaller POCO or increase SystemOneOptions.MaxChoiceProperties\n" +
            "when constructing SystemOne.");
        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task ChoiceAsync_NestedProperties_CountsParentAndChildren()
    {
        var handler = RecordingHandler.Json(ChoiceResponse);
        using var systemOne = TestClient.Create(handler, new SystemOneOptions { MaxChoiceProperties = 10 });

        await systemOne.ChoiceAsync("ticket", "Which option?", new[] { new NestedSmall() });
        handler.CallCount.Should().Be(1);

        var act = () => systemOne.ChoiceAsync("ticket", "Which option?", new[] { new NestedTen() });
        await act.Should().ThrowAsync<SystemOneValidationException>()
            .WithMessage("*contains 12 serialized properties; the limit is 10*");
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task ChoiceAsync_ArrayElements_CountsElementsOnly()
    {
        var handler = RecordingHandler.Json(ChoiceResponse);
        using var systemOne = TestClient.Create(handler);

        var accepted = new WithArray { Points = new[] { new Point(), new Point(), new Point() } };
        await systemOne.ChoiceAsync("ticket", "Which option?", new[] { accepted });
        handler.CallCount.Should().Be(1);

        var rejected = new WithArray
        {
            Points = new[]
            {
                new Point(),
                new Point(),
                new Point(),
                new Point(),
                new Point(),
                new Point(),
                new Point(),
                new Point(),
                new Point(),
                new Point(),
                new Point(),
            },
        };
        var act = () => systemOne.ChoiceAsync("ticket", "Which option?", new[] { rejected });
        await act.Should().ThrowAsync<SystemOneValidationException>();
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task ChoiceAsync_DictionaryEntries_CountsEntries()
    {
        var handler = RecordingHandler.Json(ChoiceResponse);
        using var systemOne = TestClient.Create(handler);

        var accepted = new WithDictionary
        {
            Points = new Dictionary<string, Point> { ["a"] = new Point(), ["b"] = new Point(), ["c"] = new Point() },
        };
        await systemOne.ChoiceAsync("ticket", "Which option?", new[] { accepted });
        handler.CallCount.Should().Be(1);

        var rejected = new WithDictionary
        {
            Points = new Dictionary<string, Point>
            {
                ["a"] = new Point(),
                ["b"] = new Point(),
                ["c"] = new Point(),
                ["d"] = new Point(),
                ["e"] = new Point(),
                ["f"] = new Point(),
                ["g"] = new Point(),
                ["h"] = new Point(),
                ["i"] = new Point(),
                ["j"] = new Point(),
                ["k"] = new Point(),
            },
        };
        var act = () => systemOne.ChoiceAsync("ticket", "Which option?", new[] { rejected });
        await act.Should().ThrowAsync<SystemOneValidationException>();
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task ChoiceAsync_IgnoredProperties_ExcludedFromCount()
    {
        var handler = RecordingHandler.Json(ChoiceResponse);
        using var systemOne = TestClient.Create(handler);

        await systemOne.ChoiceAsync("ticket", "Which option?", new[] { new WithIgnoredProperties() });

        handler.CallCount.Should().Be(1);
        var criteria = handler.Last.Question("choice").GetProperty("criteria").GetProperty("0");
        criteria.GetProperty("Counted").GetInt32().Should().Be(0);
        criteria.TryGetProperty("Ignored1", out _).Should().BeFalse();
    }

    [Fact]
    public async Task SystemOneOptions_MaxChoicePropertiesOverride_ChangesLimit()
    {
        var handler = RecordingHandler.Json(ChoiceResponse);
        using var systemOne = TestClient.Create(handler, new SystemOneOptions { MaxChoiceProperties = 10 });

        await systemOne.ChoiceAsync("ticket", "Which option?", new[] { new TenProperties() });

        var act = () => systemOne.ChoiceAsync("ticket", "Which option?", new[] { new ElevenProperties() });
        await act.Should().ThrowAsync<SystemOneValidationException>()
            .WithMessage("*the limit is 10*");
        handler.CallCount.Should().Be(1);

        var raisedHandler = RecordingHandler.Json(ChoiceResponse);
        using var raised = TestClient.Create(raisedHandler, new SystemOneOptions { MaxChoiceProperties = 11 });

        await raised.ChoiceAsync("ticket", "Which option?", new[] { new ElevenProperties() });
        raisedHandler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task ChoiceAsync_OversizedOption_ErrorIdentifiesOption()
    {
        var handler = RecordingHandler.Json(ChoiceResponse);
        using var systemOne = TestClient.Create(handler, new SystemOneOptions { MaxChoiceProperties = 10 });

        var act = () => systemOne.ChoiceAsync<object>("ticket", "Which option?", new object[] { new Point(), new ElevenProperties() });

        await act.Should().ThrowAsync<SystemOneValidationException>()
            .WithMessage("Choice option 1 contains 11 serialized properties; the limit is 10.*");
        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task ChoiceAsync_CyclicOption_ThrowsAndSendsNothing()
    {
        var handler = RecordingHandler.Json(ChoiceResponse);
        using var systemOne = TestClient.Create(handler);

        var node = new CyclicNode();
        node.Next = node;

        var act = () => systemOne.ChoiceAsync("ticket", "Which option?", new[] { node });

        await act.Should().ThrowAsync<SystemOneValidationException>()
            .WithMessage("*contains a cyclic object graph*");
        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task ChoiceAsync_RepeatedReferences_CountsNormally()
    {
        const string response = """{"model":"jev-latest","answers":{"choice":{"type":"choice","choice":"0","probabilities":{"0":1.0,"1":0.0,"2":0.0},"confidence":1.0}},"usage":{"input_tokens":1,"output_tokens":1}}""";
        var handler = RecordingHandler.Json(response);
        using var systemOne = TestClient.Create(handler);

        var shared = new Point();
        var act = () => systemOne.ChoiceAsync("ticket", "Which option?", new[] { shared, shared, shared });

        await act.Should().NotThrowAsync();
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public void SystemOneOptions_NonPositiveMaxChoiceProperties_Throws()
    {
        var handler = RecordingHandler.Json(ChoiceResponse);

        var zero = () => TestClient.Create(handler, new SystemOneOptions { MaxChoiceProperties = 0 });
        var negative = () => TestClient.Create(handler, new SystemOneOptions { MaxChoiceProperties = -5 });

        zero.Should().Throw<ArgumentException>().WithMessage("*must be positive*");
        negative.Should().Throw<ArgumentException>().WithMessage("*must be positive*");
    }

    [Fact]
    public async Task ChoiceAsync_JsonNodesAndElements_CountLikeSerializedForm()
    {
        var handler = RecordingHandler.Json(ChoiceResponse);
        using var systemOne = TestClient.Create(handler, new SystemOneOptions { MaxChoiceProperties = 10 });

        var acceptedNode = ParseNode("""{"a":1,"b":2,"c":3}""");
        await systemOne.ChoiceAsync("ticket", "Which option?", new[] { acceptedNode });

        var acceptedElement = JsonDocument.Parse("""{"a":1,"b":2,"c":3}""").RootElement;
        await systemOne.ChoiceAsync("ticket", "Which option?", new[] { acceptedElement });

        var rejectedNode = ParseNode("""{"a":1,"b":2,"c":3,"d":4,"e":5,"f":6,"g":7,"h":8,"i":9,"j":10,"k":11}""");
        var rejectNodeAct = () => systemOne.ChoiceAsync("ticket", "Which option?", new[] { rejectedNode });
        await rejectNodeAct.Should().ThrowAsync<SystemOneValidationException>()
            .WithMessage("*contains 11 serialized properties; the limit is 10*");

        var rejectedElement = JsonDocument.Parse("""{"a":1,"b":2,"c":3,"d":4,"e":5,"f":6,"g":7,"h":8,"i":9,"j":10,"k":11}""").RootElement;
        var rejectElementAct = () => systemOne.ChoiceAsync("ticket", "Which option?", new[] { rejectedElement });
        await rejectElementAct.Should().ThrowAsync<SystemOneValidationException>()
            .WithMessage("*contains 11 serialized properties; the limit is 10*");

        var nestedElement = JsonDocument.Parse("""{"outer":{"inner":{"a":1,"b":2,"c":3,"d":4,"e":5,"f":6,"g":7,"h":8,"i":9,"j":10,"k":11}}}""").RootElement;
        var nestedAct = () => systemOne.ChoiceAsync("ticket", "Which option?", new[] { nestedElement });
        await nestedAct.Should().ThrowAsync<SystemOneValidationException>()
            .WithMessage("*contains 13 serialized properties; the limit is 10*");
        handler.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task SystemOneOptions_DefaultMaxChoiceProperties_IsTwenty()
    {
        SystemOneOptions.DefaultMaxChoiceProperties.Should().Be(20);

        var handler = RecordingHandler.Json(ChoiceResponse);
        using var systemOne = TestClient.Create(handler);

        var twenty = new TwentyProperties();
        await systemOne.ChoiceAsync("ticket", "Which option?", new[] { twenty });

        var act = () => systemOne.ChoiceAsync("ticket", "Which option?", new[] { new TwentyOneProperties() });
        await act.Should().ThrowAsync<SystemOneValidationException>()
            .WithMessage("*21 serialized properties; the limit is 20*");
    }

    private static JsonNode ParseNode(string json) =>
        JsonNode.Parse(json) ?? throw new InvalidOperationException("Expected the JSON to parse to a node.");
}
