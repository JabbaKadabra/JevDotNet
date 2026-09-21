using System.Runtime.CompilerServices;

namespace SystemOneDotNet.Internal;

/// <summary>Compares objects by reference instead of by value.</summary>
internal sealed class ReferenceComparer : IEqualityComparer<object>
{
    public static readonly ReferenceComparer Instance = new();

    private ReferenceComparer()
    {
    }

    bool IEqualityComparer<object>.Equals(object? x, object? y) => ReferenceEquals(x, y);

    int IEqualityComparer<object>.GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
}
