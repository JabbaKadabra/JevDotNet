using System.Text.Json;

namespace SystemOneDotNet.Tests;

public sealed class AnswerReaderTests
{
    private const string QuestionId = "q";

    [Theory]
    [InlineData("[]")]
    [InlineData("5")]
    [InlineData("\"text\"")]
    [InlineData("null")]
    [InlineData("true")]
    public void RequireObject_NonObject_Throws(string json)
    {
        var act = () => AnswerReader.RequireObject(Parse(json), QuestionId);

        act.Should().Throw<SystemOneProtocolException>()
            .WithMessage($"*answer for question '{QuestionId}' must be a JSON object*");
    }

    [Fact]
    public void RequireObject_Object_ReturnsTheElement()
    {
        var element = Parse("""{"type":"noul"}""");

        AnswerReader.RequireObject(element, QuestionId).ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public void RequireProperty_Missing_Throws()
    {
        var act = () => AnswerReader.RequireProperty(Parse("{}"), "noul", QuestionId);

        act.Should().Throw<SystemOneProtocolException>()
            .WithMessage($"*answer for question '{QuestionId}' is missing the 'noul' property*");
    }

    [Fact]
    public void RequireProperty_Present_ReturnsTheValue()
    {
        var value = AnswerReader.RequireProperty(Parse("""{"noul":0.5}"""), "noul", QuestionId);

        value.GetDouble().Should().Be(0.5);
    }

    [Fact]
    public void RequireType_MatchingType_DoesNotThrow()
    {
        var act = () => AnswerReader.RequireType(Parse("""{"type":"noul"}"""), "noul", QuestionId);

        act.Should().NotThrow();
    }

    [Fact]
    public void RequireType_NonStringType_Throws()
    {
        var act = () => AnswerReader.RequireType(Parse("""{"type":5}"""), "noul", QuestionId);

        act.Should().Throw<SystemOneProtocolException>()
            .WithMessage($"*'type' property of the answer for question '{QuestionId}' must be a string*");
    }

    [Fact]
    public void RequireType_MismatchedType_Throws()
    {
        var act = () => AnswerReader.RequireType(Parse("""{"type":"score"}"""), "noul", QuestionId);

        act.Should().Throw<SystemOneProtocolException>()
            .WithMessage("*has type 'score' but 'noul' was expected*");
    }

    [Fact]
    public void RequireString_String_ReturnsTheValue()
    {
        AnswerReader.RequireString(Parse("""{"model":"jev-latest"}"""), "model", QuestionId)
            .Should().Be("jev-latest");
    }

    [Theory]
    [InlineData("""{"model":5}""")]
    [InlineData("""{"model":null}""")]
    [InlineData("""{"model":[]}""")]
    public void RequireString_NonString_Throws(string json)
    {
        var act = () => AnswerReader.RequireString(Parse(json), "model", QuestionId);

        act.Should().Throw<SystemOneProtocolException>()
            .WithMessage($"*'model' property of the answer for question '{QuestionId}' must be a string*");
    }

    [Fact]
    public void RequireNumber_Number_ReturnsTheValue()
    {
        AnswerReader.RequireNumber(Parse("""{"confidence":0.5}"""), "confidence", QuestionId)
            .Should().Be(0.5);
        AnswerReader.RequireNumber(Parse("""{"confidence":1}"""), "confidence", QuestionId)
            .Should().Be(1.0);
    }

    [Theory]
    [InlineData("""{"confidence":"0.5"}""")]
    [InlineData("""{"confidence":null}""")]
    [InlineData("""{"confidence":{}}""")]
    public void RequireNumber_NonNumber_Throws(string json)
    {
        var act = () => AnswerReader.RequireNumber(Parse(json), "confidence", QuestionId);

        act.Should().Throw<SystemOneProtocolException>()
            .WithMessage($"*'confidence' property of the answer for question '{QuestionId}' must be a number*");
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(0.5d)]
    [InlineData(1d)]
    public void RequireUnitInterval_InRange_ReturnsTheValue(double value)
    {
        AnswerReader.RequireUnitInterval(value, "The 'confidence' value", QuestionId).Should().Be(value);
    }

    [Theory]
    [InlineData(-0.001d)]
    [InlineData(1.001d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void RequireUnitInterval_OutOfRange_Throws(double value)
    {
        var act = () => AnswerReader.RequireUnitInterval(value, "The 'confidence' value", QuestionId);

        act.Should().Throw<SystemOneProtocolException>()
            .WithMessage("*must be between 0 and 1*");
    }

    [Theory]
    [InlineData(-0.0000005d)]
    [InlineData(0.5d)]
    [InlineData(1.0000005d)]
    public void RequireRange_WithinTolerance_ReturnsTheValue(double value)
    {
        AnswerReader.RequireRange(value, 0, 1, "The 'score' value", QuestionId).Should().Be(value);
    }

    [Theory]
    [InlineData(-0.001d)]
    [InlineData(1.001d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void RequireRange_OutOfRange_Throws(double value)
    {
        var act = () => AnswerReader.RequireRange(value, 0, 1, "The 'score' value", QuestionId);

        act.Should().Throw<SystemOneProtocolException>()
            .WithMessage("*must be between 0 and 1*");
    }

    [Theory]
    [InlineData("0", 2, 0)]
    [InlineData("1", 2, 1)]
    [InlineData("254", 255, 254)]
    public void ParseOptionIndex_CanonicalIndex_ReturnsTheIndex(string name, int optionCount, int expected)
    {
        AnswerReader.ParseOptionIndex(name, optionCount, QuestionId).Should().Be(expected);
    }

    [Theory]
    [InlineData("01", 2)]
    [InlineData(" 0", 2)]
    [InlineData("+0", 2)]
    [InlineData("-0", 2)]
    [InlineData("-1", 2)]
    [InlineData("1.0", 2)]
    [InlineData("", 2)]
    [InlineData("abc", 2)]
    [InlineData("2", 2)]
    [InlineData("99999999999999999999", 2)]
    public void ParseOptionIndex_UnknownOptionId_Throws(string name, int optionCount)
    {
        var act = () => AnswerReader.ParseOptionIndex(name, optionCount, QuestionId);

        act.Should().Throw<SystemOneProtocolException>()
            .WithMessage($"*unknown option id '{name}'*");
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
