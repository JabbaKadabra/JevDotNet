using SystemOneDotNet.Answers;

namespace SystemOneDotNet.Questions;

/// <summary>
/// A choice question with named string options and optional descriptions, matching the criteria dictionary
/// of the TypeSafe quickstart.
/// </summary>
public interface INamedChoiceQuestion : IQuestion<ChoiceAnswer<string>>
{
    /// <summary>
    /// Gets the option names, in the order they were supplied.
    /// </summary>
    IReadOnlyList<string> Options { get; }

    /// <summary>
    /// Gets the optional description of each option, aligned with <see cref="Options"/>. An entry is
    /// <see langword="null"/> when the option has no extra detail.
    /// </summary>
    IReadOnlyList<string?> Descriptions { get; }
}
