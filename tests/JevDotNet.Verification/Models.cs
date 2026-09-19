namespace JevDotNet.Verification;

public sealed class Team
{
    public Team(string name, string email)
    {
        Name = name;
        Email = email;
    }

    public string Name { get; }

    public string Email { get; }
}

public sealed class Point
{
    public int X { get; set; }

    public int Y { get; set; }
}

public sealed class WithArray
{
    public Point[] Points { get; set; } = Array.Empty<Point>();
}

public sealed class WithDictionary
{
    public Dictionary<string, Point> Points { get; set; } = new();
}

public sealed class WithIgnoredProperties
{
    public int Counted { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public int Ignored1 { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public int Ignored2 { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public int Ignored3 { get; set; }
}

public sealed class CyclicNode
{
    public int Value { get; set; }

    public CyclicNode? Next { get; set; }
}

public sealed class TenProperties
{
    public int P1 { get; set; }

    public int P2 { get; set; }

    public int P3 { get; set; }

    public int P4 { get; set; }

    public int P5 { get; set; }

    public int P6 { get; set; }

    public int P7 { get; set; }

    public int P8 { get; set; }

    public int P9 { get; set; }

    public int P10 { get; set; }
}

public sealed class ElevenProperties
{
    public int P1 { get; set; }

    public int P2 { get; set; }

    public int P3 { get; set; }

    public int P4 { get; set; }

    public int P5 { get; set; }

    public int P6 { get; set; }

    public int P7 { get; set; }

    public int P8 { get; set; }

    public int P9 { get; set; }

    public int P10 { get; set; }

    public int P11 { get; set; }
}

public sealed class NestedTen
{
    public int Root { get; set; }

    public TenProperties Child { get; set; } = new();
}

public sealed class TwentyOneProperties
{
    public int P1 { get; set; }

    public int P2 { get; set; }

    public int P3 { get; set; }

    public int P4 { get; set; }

    public int P5 { get; set; }

    public int P6 { get; set; }

    public int P7 { get; set; }

    public int P8 { get; set; }

    public int P9 { get; set; }

    public int P10 { get; set; }

    public int P11 { get; set; }

    public int P12 { get; set; }

    public int P13 { get; set; }

    public int P14 { get; set; }

    public int P15 { get; set; }

    public int P16 { get; set; }

    public int P17 { get; set; }

    public int P18 { get; set; }

    public int P19 { get; set; }

    public int P20 { get; set; }

    public int P21 { get; set; }
}
