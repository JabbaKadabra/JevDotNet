namespace JevDotNet;

/// <summary>
/// A question whose answer is of type <typeparamref name="TAnswer"/>.
/// </summary>
/// <typeparam name="TAnswer">The type of the answer produced for this question.</typeparam>
public interface IQuestion<TAnswer> : IQuestion
{
}
