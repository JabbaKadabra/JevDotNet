namespace System.Runtime.CompilerServices;

/// <summary>
/// Declares the marker type that C# records and <c>init</c> accessors require. The compiler emits a
/// reference to it, but netstandard2.0 predates the type, so the library supplies it.
/// </summary>
internal static class IsExternalInit
{
}
