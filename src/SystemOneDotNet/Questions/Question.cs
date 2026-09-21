using SystemOneDotNet.Internal;
using SystemOneDotNet.Internal.Questions;

namespace SystemOneDotNet.Questions;

/// <summary>
/// Creates the questions that an <see cref="ISystemOneClient"/> can evaluate. Questions are immutable,
/// validated at creation, carry no response state, and can be reused across any number of batches.
/// </summary>
public static class Question
{
    /// <summary>
    /// Creates a choice question whose options are arbitrary values. Each option is sent to the model as its
    /// JSON description and the answer maps back to the original value.
    /// </summary>
    /// <typeparam name="T">The type of the option values.</typeparam>
    /// <param name="id">The identifier that matches the question with its answer inside a batch.</param>
    /// <param name="instructions">The question the model answers.</param>
    /// <param name="options">The options the model chooses from. At least one option is required.</param>
    /// <returns>The validated choice question.</returns>
    public static IChoiceQuestion<T> Choice<T>(string id, string instructions, IEnumerable<T> options) =>
        new ChoiceQuestion<T>(id, instructions, options);

    /// <summary>
    /// Creates a choice question with named string options and optional descriptions, matching the criteria
    /// dictionary of the TypeSafe quickstart.
    /// </summary>
    /// <param name="id">The identifier that matches the question with its answer inside a batch.</param>
    /// <param name="instructions">The question the model answers.</param>
    /// <param name="options">The named options with optional descriptions. Use <see langword="null"/> when an option needs no extra detail.</param>
    /// <returns>The validated choice question.</returns>
    public static INamedChoiceQuestion NamedChoice(
        string id,
        string instructions,
        IReadOnlyDictionary<string, string?> options) =>
        new NamedChoiceQuestion(
            id,
            instructions,
            Guard.NotNull(options, nameof(options), "Choice options must not be null."));

    /// <summary>
    /// Creates a choice question with named string options and optional descriptions, matching the criteria
    /// dictionary of the TypeSafe quickstart.
    /// </summary>
    /// <param name="id">The identifier that matches the question with its answer inside a batch.</param>
    /// <param name="instructions">The question the model answers.</param>
    /// <param name="options">The named options with optional descriptions. Use <see langword="null"/> when an option needs no extra detail.</param>
    /// <returns>The validated choice question.</returns>
    public static INamedChoiceQuestion NamedChoice(
        string id,
        string instructions,
        IEnumerable<KeyValuePair<string, string?>> options) =>
        new NamedChoiceQuestion(id, instructions, options);

    /// <summary>
    /// Creates a score question that rates the state along an ordered list of level descriptions.
    /// </summary>
    /// <param name="id">The identifier that matches the question with its answer inside a batch.</param>
    /// <param name="instructions">The question the model answers.</param>
    /// <param name="levels">The ordered level descriptions. At least two levels are required.</param>
    /// <returns>The validated score question.</returns>
    public static IScoreQuestion Score(string id, string instructions, IEnumerable<string> levels) =>
        new ScoreQuestion(id, instructions, levels);

    /// <summary>
    /// Creates a noul (yes/no) question. The answer is the probability that the answer is yes.
    /// </summary>
    /// <param name="id">The identifier that matches the question with its answer inside a batch.</param>
    /// <param name="instructions">The yes/no question to evaluate.</param>
    /// <param name="trueDescription">An optional description of what a yes means.</param>
    /// <param name="falseDescription">An optional description of what a no means.</param>
    /// <returns>The validated noul question.</returns>
    public static INoulQuestion Noul(
        string id,
        string instructions,
        string? trueDescription = null,
        string? falseDescription = null) =>
        new NoulQuestion(id, instructions, trueDescription, falseDescription);
}
