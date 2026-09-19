namespace JevDotNet.Tests;

public class ValidationTests
{
    [Fact]
    public void Api_keys_are_required()
    {
        var nullKey = () => new Jev(null!);
        var emptyKey = () => new Jev("   ");

        nullKey.Should().Throw<ArgumentNullException>().WithParameterName("apiKey");
        emptyKey.Should().Throw<ArgumentException>().WithParameterName("apiKey");
    }

    [Fact]
    public void Endpoints_must_be_absolute_http_urls()
    {
        var relative = () => new Jev("key", new JevOptions { Endpoint = "/relative" });
        var otherScheme = () => new Jev("key", new JevOptions { Endpoint = "ftp://example.com/jev" });
        var empty = () => new Jev("key", new JevOptions { Endpoint = "" });

        relative.Should().Throw<ArgumentException>().WithParameterName("options");
        otherScheme.Should().Throw<ArgumentException>().WithParameterName("options");
        empty.Should().Throw<ArgumentException>().WithParameterName("options");
    }

    [Fact]
    public void Models_must_not_be_empty()
    {
        var act = () => new Jev("key", new JevOptions { Model = " " });

        act.Should().Throw<ArgumentException>().WithParameterName("options");
    }

    [Fact]
    public void Choice_questions_require_an_id_instructions_and_options()
    {
        var nullId = () => new Choice<string>(null!, "instructions", new[] { "a" });
        var emptyId = () => new Choice<string>("  ", "instructions", new[] { "a" });
        var nullInstructions = () => new Choice<string>("id", null!, new[] { "a" });
        var emptyInstructions = () => new Choice<string>("id", "", new[] { "a" });
        var nullOptions = () => new Choice<string>("id", "instructions", (IEnumerable<string>)null!);
        var emptyOptions = () => new Choice<string>("id", "instructions", Array.Empty<string>());
        var nullOption = () => new Choice<string?>("id", "instructions", new[] { "a", null });

        nullId.Should().Throw<ArgumentNullException>();
        emptyId.Should().Throw<ArgumentException>();
        nullInstructions.Should().Throw<ArgumentNullException>();
        emptyInstructions.Should().Throw<ArgumentException>();
        nullOptions.Should().Throw<ArgumentNullException>();
        emptyOptions.Should().Throw<JevValidationException>().WithMessage("*at least one option*");
        nullOption.Should().Throw<JevValidationException>().WithMessage("*null option at index 1*");
    }

    [Fact]
    public void Choice_questions_reject_more_than_255_options()
    {
        var options = Enumerable.Range(0, 256).Select(index => "option-" + index).ToArray();

        var act = () => new Choice<string>("id", "instructions", options);

        act.Should().Throw<JevValidationException>()
            .WithMessage("*defines 256 options; the limit is 255*");
    }

    [Fact]
    public void Named_choice_questions_validate_names_and_descriptions()
    {
        var nullDictionary = () => new ChoiceQuestion("id", "instructions", (IReadOnlyDictionary<string, string?>)null!);
        var emptyDictionary = () => new ChoiceQuestion("id", "instructions", new Dictionary<string, string?>());
        var nullName = () => new ChoiceQuestion("id", "instructions", new[] { new KeyValuePair<string, string?>(null!, "description") });
        var emptyName = () => new ChoiceQuestion("id", "instructions", new Dictionary<string, string?> { [""] = "description" });

        nullDictionary.Should().Throw<ArgumentNullException>();
        emptyDictionary.Should().Throw<JevValidationException>().WithMessage("*at least one option*");
        nullName.Should().Throw<JevValidationException>().WithMessage("*null option name*");
        emptyName.Should().Throw<JevValidationException>().WithMessage("*empty option name*");
    }

    [Fact]
    public void Duplicate_named_options_are_rejected()
    {
        var act = () => new ChoiceQuestion(
            "id",
            "instructions",
            new[] { new KeyValuePair<string, string?>("billing", null), new KeyValuePair<string, string?>("billing", "again") });

        act.Should().Throw<JevValidationException>().WithMessage("*duplicate option name 'billing'*");
    }

    [Fact]
    public void Score_questions_require_at_least_two_levels()
    {
        var zero = () => new Score("id", "instructions", Array.Empty<string>());
        var one = () => new Score("id", "instructions", new[] { "Only one" });
        var two = () => new Score("id", "instructions", new[] { "One", "Two" });

        zero.Should().Throw<JevValidationException>().WithMessage("*at least two levels*");
        one.Should().Throw<JevValidationException>().WithMessage("*defines 1*");
        two.Should().NotThrow();
    }

    [Fact]
    public void Score_levels_must_not_be_null_or_empty()
    {
        var nullLevel = () => new Score("id", "instructions", new[] { "Calm", null! });
        var emptyLevel = () => new Score("id", "instructions", new[] { "Calm", " " });

        nullLevel.Should().Throw<JevValidationException>().WithMessage("*null level at index 1*");
        emptyLevel.Should().Throw<JevValidationException>().WithMessage("*empty level at index 1*");
    }

    [Fact]
    public void Noul_questions_require_an_id_and_instructions()
    {
        var nullId = () => new Noul(null!, "instructions");
        var emptyInstructions = () => new Noul("id", "  ");

        nullId.Should().Throw<ArgumentNullException>();
        emptyInstructions.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Null_states_are_rejected()
    {
        using var jev = TestClient.Create(RecordingHandler.Json("{}"));

        var act = () => jev.Query(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("state");
    }

    [Fact]
    public void Builders_reject_null_questions()
    {
        using var jev = TestClient.Create(RecordingHandler.Json("{}"));

        var act = () => jev.Query("ticket").Question<ChoiceAnswer<string>>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Default_options_match_the_documented_defaults()
    {
        var options = new JevOptions();

        options.Endpoint.Should().Be("https://api.typesafe.ai/v1/systemone");
        options.Model.Should().Be("jev-latest");
        options.MaxChoiceProperties.Should().Be(20);
    }

    [Fact]
    public void Result_get_rejects_null_questions()
    {
        var result = new JevResult("jev-latest", new JevUsage(1, 1), new Dictionary<string, object>());

        var act = () => result.Get<string>(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
