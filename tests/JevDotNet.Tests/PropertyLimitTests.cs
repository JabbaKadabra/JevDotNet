using System.Text.Json;
using System.Text.Json.Nodes;

namespace JevDotNet.Tests;

public class PropertyLimitTests
{
    private const string ChoiceResponse = """{"model":"jev-latest","answers":{"choice":{"type":"choice","choice":"0","probabilities":{"0":1.0},"confidence":1.0}},"usage":{"input_tokens":1,"output_tokens":1}}""";

    [Fact]
    public async Task Ten_properties_are_accepted_with_the_default_limit()
    {
        var handler = RecordingHandler.Json(ChoiceResponse);
        using var jev = TestClient.Create(handler);

        await jev.ChoiceAsync("ticket", "Which option?", new[] { new TenProperties() });

        handler.CallCount.Should().Be(1);
        handler.Last.Question("choice").GetProperty("criteria").GetProperty("0").GetProperty("P1").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task Eleven_properties_are_rejected_with_a_limit_of_ten_and_no_request_is_sent()
    {
        var handler = RecordingHandler.Json(ChoiceResponse);
        using var jev = TestClient.Create(handler, new JevOptions { MaxChoiceProperties = 10 });

        var act = () => jev.ChoiceAsync("ticket", "Which option?", new[] { new ElevenProperties() });

        var exception = await act.Should().ThrowAsync<JevValidationException>();
        exception.Which.Message.Should().Be(
            "Choice option 0 contains 11 serialized properties; the limit is 10.\n" +
            "Use a dedicated smaller POCO or increase JevOptions.MaxChoiceProperties\n" +
            "when constructing Jev.");
        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Nested_properties_are_counted_including_the_parent_property()
    {
        var handler = RecordingHandler.Json(ChoiceResponse);
        using var jev = TestClient.Create(handler, new JevOptions { MaxChoiceProperties = 10 });

        await jev.ChoiceAsync("ticket", "Which option?", new[] { new NestedSmall() });
        handler.CallCount.Should().Be(1);

        var act = () => jev.ChoiceAsync("ticket", "Which option?", new[] { new NestedTen() });
        await act.Should().ThrowAsync<JevValidationException>()
            .WithMessage("*contains 12 serialized properties; the limit is 10*");
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Array_elements_are_counted_without_counting_the_array_itself()
    {
        var handler = RecordingHandler.Json(ChoiceResponse);
        using var jev = TestClient.Create(handler);

        var accepted = new WithArray { Points = new[] { new Point(), new Point(), new Point() } };
        await jev.ChoiceAsync("ticket", "Which option?", new[] { accepted });
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
        var act = () => jev.ChoiceAsync("ticket", "Which option?", new[] { rejected });
        await act.Should().ThrowAsync<JevValidationException>();
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Dictionary_entries_are_counted()
    {
        var handler = RecordingHandler.Json(ChoiceResponse);
        using var jev = TestClient.Create(handler);

        var accepted = new WithDictionary
        {
            Points = new Dictionary<string, Point> { ["a"] = new Point(), ["b"] = new Point(), ["c"] = new Point() },
        };
        await jev.ChoiceAsync("ticket", "Which option?", new[] { accepted });
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
        var act = () => jev.ChoiceAsync("ticket", "Which option?", new[] { rejected });
        await act.Should().ThrowAsync<JevValidationException>();
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Ignored_properties_are_not_counted()
    {
        var handler = RecordingHandler.Json(ChoiceResponse);
        using var jev = TestClient.Create(handler);

        await jev.ChoiceAsync("ticket", "Which option?", new[] { new WithIgnoredProperties() });

        handler.CallCount.Should().Be(1);
        var criteria = handler.Last.Question("choice").GetProperty("criteria").GetProperty("0");
        criteria.GetProperty("Counted").GetInt32().Should().Be(0);
        criteria.TryGetProperty("Ignored1", out _).Should().BeFalse();
    }

    [Fact]
    public async Task The_limit_can_be_lowered_and_raised_with_an_override()
    {
        var handler = RecordingHandler.Json(ChoiceResponse);
        using var jev = TestClient.Create(handler, new JevOptions { MaxChoiceProperties = 10 });

        await jev.ChoiceAsync("ticket", "Which option?", new[] { new TenProperties() });

        var act = () => jev.ChoiceAsync("ticket", "Which option?", new[] { new ElevenProperties() });
        await act.Should().ThrowAsync<JevValidationException>()
            .WithMessage("*the limit is 10*");
        handler.CallCount.Should().Be(1);

        var raisedHandler = RecordingHandler.Json(ChoiceResponse);
        using var raised = TestClient.Create(raisedHandler, new JevOptions { MaxChoiceProperties = 11 });

        await raised.ChoiceAsync("ticket", "Which option?", new[] { new ElevenProperties() });
        raisedHandler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task The_error_identifies_the_offending_option()
    {
        var handler = RecordingHandler.Json(ChoiceResponse);
        using var jev = TestClient.Create(handler, new JevOptions { MaxChoiceProperties = 10 });

        var act = () => jev.ChoiceAsync<object>("ticket", "Which option?", new object[] { new Point(), new ElevenProperties() });

        await act.Should().ThrowAsync<JevValidationException>()
            .WithMessage("Choice option 1 contains 11 serialized properties; the limit is 10.*");
        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Cyclic_option_graphs_are_rejected_before_the_request_is_sent()
    {
        var handler = RecordingHandler.Json(ChoiceResponse);
        using var jev = TestClient.Create(handler);

        var node = new CyclicNode();
        node.Next = node;

        var act = () => jev.ChoiceAsync("ticket", "Which option?", new[] { node });

        await act.Should().ThrowAsync<JevValidationException>()
            .WithMessage("*contains a cyclic object graph*");
        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Repeated_references_that_are_not_cyclic_are_counted_normally()
    {
        const string response = """{"model":"jev-latest","answers":{"choice":{"type":"choice","choice":"0","probabilities":{"0":1.0,"1":0.0,"2":0.0},"confidence":1.0}},"usage":{"input_tokens":1,"output_tokens":1}}""";
        var handler = RecordingHandler.Json(response);
        using var jev = TestClient.Create(handler);

        var shared = new Point();
        var act = () => jev.ChoiceAsync("ticket", "Which option?", new[] { shared, shared, shared });

        await act.Should().NotThrowAsync();
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Non_positive_property_limits_are_rejected()
    {
        var handler = RecordingHandler.Json(ChoiceResponse);

        var zero = () => TestClient.Create(handler, new JevOptions { MaxChoiceProperties = 0 });
        var negative = () => TestClient.Create(handler, new JevOptions { MaxChoiceProperties = -5 });

        zero.Should().Throw<ArgumentException>().WithMessage("*must be positive*");
        negative.Should().Throw<ArgumentException>().WithMessage("*must be positive*");
    }

    [Fact]
    public async Task Json_nodes_and_json_elements_are_counted_like_their_serialized_form()
    {
        var handler = RecordingHandler.Json(ChoiceResponse);
        using var jev = TestClient.Create(handler, new JevOptions { MaxChoiceProperties = 10 });

        var acceptedNode = JsonNode.Parse("""{"a":1,"b":2,"c":3}""")!;
        await jev.ChoiceAsync("ticket", "Which option?", new[] { acceptedNode });

        var acceptedElement = JsonDocument.Parse("""{"a":1,"b":2,"c":3}""").RootElement;
        await jev.ChoiceAsync("ticket", "Which option?", new[] { acceptedElement });

        var rejectedNode = JsonNode.Parse("""{"a":1,"b":2,"c":3,"d":4,"e":5,"f":6,"g":7,"h":8,"i":9,"j":10,"k":11}""")!;
        var rejectNodeAct = () => jev.ChoiceAsync("ticket", "Which option?", new[] { rejectedNode });
        await rejectNodeAct.Should().ThrowAsync<JevValidationException>()
            .WithMessage("*contains 11 serialized properties; the limit is 10*");

        var rejectedElement = JsonDocument.Parse("""{"a":1,"b":2,"c":3,"d":4,"e":5,"f":6,"g":7,"h":8,"i":9,"j":10,"k":11}""").RootElement;
        var rejectElementAct = () => jev.ChoiceAsync("ticket", "Which option?", new[] { rejectedElement });
        await rejectElementAct.Should().ThrowAsync<JevValidationException>()
            .WithMessage("*contains 11 serialized properties; the limit is 10*");

        var nestedElement = JsonDocument.Parse("""{"outer":{"inner":{"a":1,"b":2,"c":3,"d":4,"e":5,"f":6,"g":7,"h":8,"i":9,"j":10,"k":11}}}""").RootElement;
        var nestedAct = () => jev.ChoiceAsync("ticket", "Which option?", new[] { nestedElement });
        await nestedAct.Should().ThrowAsync<JevValidationException>()
            .WithMessage("*contains 13 serialized properties; the limit is 10*");
        handler.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task The_default_limit_is_twenty()
    {
        JevOptions.DefaultMaxChoiceProperties.Should().Be(20);

        var handler = RecordingHandler.Json(ChoiceResponse);
        using var jev = TestClient.Create(handler);

        var twenty = new TwentyProperties();
        await jev.ChoiceAsync("ticket", "Which option?", new[] { twenty });

        var act = () => jev.ChoiceAsync("ticket", "Which option?", new[] { new TwentyOneProperties() });
        await act.Should().ThrowAsync<JevValidationException>()
            .WithMessage("*21 serialized properties; the limit is 20*");
    }
}
