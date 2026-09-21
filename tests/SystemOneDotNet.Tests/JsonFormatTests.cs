using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SystemOneDotNet.Tests;

public sealed class JsonFormatTests
{
    [Fact]
    public void WriteDescription_Null_WritesNullLiteral()
    {
        Render(null).Should().Be("null");
    }

    [Fact]
    public void WriteDescription_String_WritesString()
    {
        Render("billing").Should().Be("\"billing\"");
    }

    [Fact]
    public void WriteDescription_Enum_WritesName()
    {
        Render(Team.Technical).Should().Be("\"Technical\"");
    }

    [Theory]
    [InlineData(true, "\"true\"")]
    [InlineData(false, "\"false\"")]
    public void WriteDescription_Boolean_WritesLowercaseString(bool value, string expected)
    {
        Render(value).Should().Be(expected);
    }

    [Theory]
    [InlineData((byte)7, "7")]
    [InlineData((sbyte)-7, "-7")]
    [InlineData((short)-300, "-300")]
    [InlineData((ushort)300, "300")]
    [InlineData(42, "42")]
    [InlineData(42u, "42")]
    [InlineData(42L, "42")]
    [InlineData(42ul, "42")]
    [InlineData(1.5f, "1.5")]
    [InlineData(-1.5d, "-1.5")]
    public void WriteDescription_Numbers_WriteInvariantStrings(object value, string expected)
    {
        Render(value).Should().Be($"\"{expected}\"");
    }

    [Fact]
    public void WriteDescription_Decimal_WritesInvariantString()
    {
        Render(2.75m).Should().Be("\"2.75\"");
        Render(1000.5m).Should().Be("\"1000.5\"");
    }

    [Fact]
    public void WriteDescription_Poco_WritesObject()
    {
        var value = Render(new SmallPoco { Name = "billing", Flag = true });

        var json = JsonDocument.Parse(value).RootElement;
        json.ValueKind.Should().Be(JsonValueKind.Object);
        json.GetProperty("Name").GetString().Should().Be("billing");
        json.GetProperty("Flag").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void WriteDescription_JsonNode_WritesStructuredValue()
    {
        var value = Render(JsonNode.Parse("""{"Counted":1,"Nested":{"Value":"x"}}"""));

        var json = JsonDocument.Parse(value).RootElement;
        json.GetProperty("Counted").GetInt32().Should().Be(1);
        json.GetProperty("Nested").GetProperty("Value").GetString().Should().Be("x");
    }

    [Fact]
    public void WriteDescription_JsonElement_WritesStructuredValue()
    {
        var value = Render(JsonDocument.Parse("""{"Counted":1}""").RootElement);

        JsonDocument.Parse(value).RootElement.GetProperty("Counted").GetInt32().Should().Be(1);
    }

    private static string Render(object? value)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            JsonFormat.WriteDescription(writer, value);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
