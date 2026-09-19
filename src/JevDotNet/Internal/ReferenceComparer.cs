using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace JevDotNet.Internal
{
    /// <summary>Compares objects by reference instead of by value.</summary>
    internal sealed class ReferenceComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceComparer Instance = new ReferenceComparer();

        private ReferenceComparer()
        {
        }

        bool IEqualityComparer<object>.Equals(object? x, object? y)
        {
            return ReferenceEquals(x, y);
        }

        int IEqualityComparer<object>.GetHashCode(object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
