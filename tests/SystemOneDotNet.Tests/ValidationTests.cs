namespace SystemOneDotNet.Tests;

// Guard tests deliberately pass null through parameters that are declared non-nullable. The
// null-forgiving operator marks that intent; it is used only in this file and in FailureHandlingTests.
public sealed class ValidationTests
{
    [Fact]
    public void SystemOne_NullOrEmptyApiKey_Throws()
    {
        var nullKey = () => SystemOneClient.Create(null!);
        var emptyKey = () => SystemOneClient.Create("   ");

        nullKey.Should().Throw<ArgumentNullException>().WithParameterName("apiKey");
        emptyKey.Should().Throw<ArgumentException>().WithParameterName("apiKey");
    }

    [Fact]
    public void SystemOne_InvalidEndpointOptions_Throws()
    {
        var relative = () => SystemOneClient.Create("key", new SystemOneOptions { Endpoint = "/relative" });
        var otherScheme = () => SystemOneClient.Create("key", new SystemOneOptions { Endpoint = "ftp://example.com/systemOne" });
        var unparseable = () => SystemOneClient.Create("key", new SystemOneOptions { Endpoint = "not a uri" });
        var empty = () => SystemOneClient.Create("key", new SystemOneOptions { Endpoint = "" });

        relative.Should().Throw<ArgumentException>().WithParameterName("options");
        otherScheme.Should().Throw<ArgumentException>().WithParameterName("options");
        unparseable.Should().Throw<ArgumentException>().WithParameterName("options");
        empty.Should().Throw<ArgumentException>().WithParameterName("options");
    }

    [Fact]
    public void SystemOne_HttpEndpointOption_IsAccepted()
    {
        var act = () => SystemOneClient.Create("key", new SystemOneOptions { Endpoint = "http://localhost:8080/systemone" });

        act.Should().NotThrow();
    }

    [Fact]
    public void SystemOne_EmptyModelOption_Throws()
    {
        var act = () => SystemOneClient.Create("key", new SystemOneOptions { Model = " " });

        act.Should().Throw<ArgumentException>().WithParameterName("options");
    }

    [Fact]
    public void ChoiceQuestion_MissingIdInstructionsOrOptions_Throws()
    {
        var nullId = () => Question.Choice<string>(null!, "instructions", new[] { "a" });
        var emptyId = () => Question.Choice<string>("  ", "instructions", new[] { "a" });
        var nullInstructions = () => Question.Choice<string>("id", null!, new[] { "a" });
        var emptyInstructions = () => Question.Choice<string>("id", "", new[] { "a" });
        var nullOptions = () => Question.Choice<string>("id", "instructions", (IEnumerable<string>)null!);
        var emptyOptions = () => Question.Choice<string>("id", "instructions", Array.Empty<string>());
        var nullOption = () => Question.Choice<string?>("id", "instructions", new[] { "a", null });

        nullId.Should().Throw<ArgumentNullException>();
        emptyId.Should().Throw<ArgumentException>();
        nullInstructions.Should().Throw<ArgumentNullException>();
        emptyInstructions.Should().Throw<ArgumentException>();
        nullOptions.Should().Throw<ArgumentNullException>();
        emptyOptions.Should().Throw<SystemOneValidationException>().WithMessage("*at least one option*");
        nullOption.Should().Throw<SystemOneValidationException>().WithMessage("*null option at index 1*");
    }

    [Fact]
    public void ChoiceQuestion_MoreThan255Options_Throws()
    {
        var options = Enumerable.Range(0, 256).Select(index => "option-" + index).ToArray();

        var act = () => Question.Choice<string>("id", "instructions", options);

        act.Should().Throw<SystemOneValidationException>()
            .WithMessage("*defines 256 options; the limit is 255*");
    }

    [Fact]
    public void ChoiceQuestion_InvalidNamesOrDescriptions_Throws()
    {
        var nullDictionary = () => Question.NamedChoice("id", "instructions", (IReadOnlyDictionary<string, string?>)null!);
        var emptyDictionary = () => Question.NamedChoice("id", "instructions", new Dictionary<string, string?>());
        var nullName = () => Question.NamedChoice("id", "instructions", new[] { new KeyValuePair<string, string?>(null!, "description") });
        var emptyName = () => Question.NamedChoice("id", "instructions", new Dictionary<string, string?> { [""] = "description" });

        nullDictionary.Should().Throw<ArgumentNullException>();
        emptyDictionary.Should().Throw<SystemOneValidationException>().WithMessage("*at least one option*");
        nullName.Should().Throw<SystemOneValidationException>().WithMessage("*null option name*");
        emptyName.Should().Throw<SystemOneValidationException>().WithMessage("*empty option name*");
    }

    [Fact]
    public void ChoiceQuestion_DuplicateNamedOptions_Throws()
    {
        var act = () => Question.NamedChoice(
            "id",
            "instructions",
            new[] { new KeyValuePair<string, string?>("billing", null), new KeyValuePair<string, string?>("billing", "again") });

        act.Should().Throw<SystemOneValidationException>().WithMessage("*duplicate option name 'billing'*");
    }

    [Fact]
    public void ScoreQuestion_FewerThanTwoLevels_Throws()
    {
        var zero = () => Question.Score("id", "instructions", Array.Empty<string>());
        var one = () => Question.Score("id", "instructions", new[] { "Only one" });
        var two = () => Question.Score("id", "instructions", new[] { "One", "Two" });

        zero.Should().Throw<SystemOneValidationException>().WithMessage("*at least two levels*");
        one.Should().Throw<SystemOneValidationException>().WithMessage("*defines 1*");
        two.Should().NotThrow();
    }

    [Fact]
    public void ScoreQuestion_NullOrEmptyLevel_Throws()
    {
        var nullLevel = () => Question.Score("id", "instructions", new[] { "Calm", null! });
        var emptyLevel = () => Question.Score("id", "instructions", new[] { "Calm", " " });

        nullLevel.Should().Throw<SystemOneValidationException>().WithMessage("*null level at index 1*");
        emptyLevel.Should().Throw<SystemOneValidationException>().WithMessage("*empty level at index 1*");
    }

    [Fact]
    public void NoulQuestion_MissingIdOrInstructions_Throws()
    {
        var nullId = () => Question.Noul(null!, "instructions");
        var emptyInstructions = () => Question.Noul("id", "  ");

        nullId.Should().Throw<ArgumentNullException>();
        emptyInstructions.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Query_NullState_Throws()
    {
        using var systemOne = TestClient.Create(RecordingHandler.Json("{}"));

        var act = () => systemOne.Query(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("state");
    }

    [Fact]
    public void Query_NullQuestion_Throws()
    {
        using var systemOne = TestClient.Create(RecordingHandler.Json("{}"));

        var act = () => systemOne.Query("ticket").Question<ChoiceAnswer<string>>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Query_ForeignQuestionImplementation_Throws()
    {
        using var systemOne = TestClient.Create(RecordingHandler.Json("{}"));

        var act = () => systemOne.Query("ticket").Question(new ForeignQuestion());

        act.Should().Throw<SystemOneValidationException>()
            .WithMessage("*'foreign'*ForeignQuestion*Question factory*");
    }

    private sealed class ForeignQuestion : IQuestion<NoulAnswer>
    {
        public string Id => "foreign";

        public string Instructions => "Not created by the library.";
    }

    [Fact]
    public void SystemOneOptions_DefaultInstance_MatchesDocumentedDefaults()
    {
        var options = new SystemOneOptions();

        options.Endpoint.Should().Be("https://api.typesafe.ai/v1/systemone");
        options.Model.Should().Be("jev-latest");
        options.MaxChoiceProperties.Should().Be(20);
    }

    [Fact]
    public void ResultGet_NullQuestion_Throws()
    {
        var result = new SystemOneResult("jev-latest", new SystemOneUsage(1, 1), new Dictionary<string, object>());

        var act = () => result.Get<string>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SystemOneSettings_NullOptions_Throws()
    {
        var act = () => SystemOneSettings.From(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }
}
