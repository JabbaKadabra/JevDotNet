using SystemOneDotNet.Answers;

namespace SystemOneDotNet.Questions;

/// <summary>
/// A choice question whose options are arbitrary values. Each option is sent to the model as its JSON
/// description and the answer maps back to the original value.
/// </summary>
/// <typeparam name="T">The type of the option values.</typeparam>
public interface IChoiceQuestion<T> : IQuestion<ChoiceAnswer<T>>
{
    /// <summary>
    /// Gets the original option values, in the order they were supplied.
    /// </summary>
    IReadOnlyList<T> Options { get; }
}
