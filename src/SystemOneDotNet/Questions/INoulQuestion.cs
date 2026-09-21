using SystemOneDotNet.Answers;

namespace SystemOneDotNet.Questions;

/// <summary>
/// A noul (yes/no) question. The answer is the probability that the answer is yes.
/// </summary>
public interface INoulQuestion : IQuestion<NoulAnswer>
{
    /// <summary>
    /// Gets the optional description of what a yes (value near 1) means.
    /// </summary>
    string? TrueDescription { get; }

    /// <summary>
    /// Gets the optional description of what a no (value near 0) means.
    /// </summary>
    string? FalseDescription { get; }
}
