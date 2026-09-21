using System.Collections;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace SystemOneDotNet.Tests;

public sealed class PropertyCounterTests
{
    private const string Context = "Choice option 0";

    public static TheoryData<object> ScalarValues => new()
    {
        "text",
        Team.Technical,
        42,
        42L,
        1.5d,
        2.75m,
        true,
        new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
        new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.FromHours(2)),
        TimeSpan.FromMinutes(5),
        Guid.NewGuid(),
        new Uri("https://example.test/systemone"),
        new byte[] { 1, 2, 3 },
        JsonDocument.Parse("""{"Counted":1,"Nested":{"Value":2}}"""),
        JsonDocument.Parse("null").RootElement,
    };

    [Fact]
    public void Count_NullValue_IsZero()
    {
        PropertyCounter.Count(null, Context).Should().Be(0);
    }

    [Theory]
    [MemberData(nameof(ScalarValues))]
    public void Count_ScalarValues_AreZero(object value)
    {
        PropertyCounter.Count(value, Context).Should().Be(0);
    }

    [Fact]
    public void Count_PocoWithNullMember_CountsPropertyNameOnly()
    {
        var value = new WithNullableMember { Note = null, Value = 1 };

        PropertyCounter.Count(value, Context).Should().Be(2);
    }

    [Fact]
    public void Count_JsonObjectWithNullProperty_CountsTheNullEntry()
    {
        PropertyCounter.Count(Node("""{"a":null,"b":1}"""), Context).Should().Be(2);
    }

    [Fact]
    public void Count_JsonArrayOfObjects_CountsEveryNestedProperty()
    {
        PropertyCounter.Count(Node("""[{"a":1},{"b":2},{"c":3}]"""), Context).Should().Be(3);
    }

    [Fact]
    public void Count_JsonArrayWithNullItems_IgnoresNulls()
    {
        PropertyCounter.Count(Node("""[null,{"a":1},null]"""), Context).Should().Be(1);
    }

    [Fact]
    public void Count_JsonValueNode_IsZero()
    {
        PropertyCounter.Count(Node("42"), Context).Should().Be(0);
    }

    [Fact]
    public void Count_JsonElementArray_CountsNestedValues()
    {
        var element = JsonDocument.Parse("""[{"a":1},[{"b":2}],3,null]""").RootElement;

        PropertyCounter.Count(element, Context).Should().Be(2);
    }

    [Fact]
    public void Count_JsonElementObject_CountsNestedValues()
    {
        var element = JsonDocument.Parse("""{"a":{"b":1,"c":2},"d":[{"e":3}]}""").RootElement;

        PropertyCounter.Count(element, Context).Should().Be(5);
    }

    [Fact]
    public void Count_PocoArrayElements_CountsNestedProperties()
    {
        var value = new object[] { new Point(), new Point() };

        PropertyCounter.Count(value, Context).Should().Be(4);
    }

    [Fact]
    public void Count_NonGenericDictionary_CountsEntriesAndValues()
    {
        var value = new Hashtable { ["a"] = new Point(), ["b"] = 2 };

        PropertyCounter.Count(value, Context).Should().Be(4);
    }

    [Fact]
    public void Count_Fields_CountsOnlyJsonIncludedFields()
    {
        var value = new WithFields { Included = 1, Excluded = 2, Plain = 3 };

        PropertyCounter.Count(value, Context).Should().Be(1);
    }

    [Fact]
    public void Count_IndexerAndWriteOnlyMembers_AreSkipped()
    {
        var value = new WithUnsupportedMembers { Regular = 1 };

        PropertyCounter.Count(value, Context).Should().Be(1);
    }

    [Fact]
    public void Count_NonPublicGetter_SkippedUnlessJsonIncluded()
    {
        var value = new WithAccessorShapes { Counted = 1, Included = 2 };

        PropertyCounter.Count(value, Context).Should().Be(2);
    }

    private static JsonNode Node(string json) =>
        JsonNode.Parse(json) ?? throw new InvalidOperationException("Expected the JSON to parse to a node.");

    private sealed class WithNullableMember
    {
        public string? Note { get; set; }

        public int Value { get; set; }
    }

    private sealed class WithFields
    {
        [JsonInclude]
        public int Included = 1;

        [JsonInclude]
        [JsonIgnore]
        public int Excluded = 2;

        public int Plain = 3;
    }

    private sealed class WithUnsupportedMembers
    {
        public int Regular { get; set; }

        public int WriteOnly
        {
            set
            {
            }
        }

        public int this[int index] => index;
    }

    private sealed class WithAccessorShapes
    {
        public int Counted { get; set; }

        private int Hidden { get; set; }

        [JsonInclude]
        public int Included { private get; set; }
    }
}
