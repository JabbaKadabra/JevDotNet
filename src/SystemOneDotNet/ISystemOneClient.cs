namespace SystemOneDotNet;

/// <summary>
/// An asynchronous client for the TypeSafe System One API. Implementations are safe to use from multiple
/// threads and can be reused for any number of requests. <see cref="SystemOne"/> is the default
/// implementation; depend on this interface to keep callers decoupled from it.
/// </summary>
public interface ISystemOneClient : IDisposable
{
    /// <summary>
    /// Evaluates a single question against a state.
    /// </summary>
    /// <typeparam name="TAnswer">The answer type of the question.</typeparam>
    /// <param name="state">The content to evaluate: a string, a POCO, or an array.</param>
    /// <param name="question">The question to evaluate.</param>
    /// <param name="cancellationToken">A token that cancels the HTTP request and response buffering.</param>
    /// <returns>The typed answer.</returns>
    Task<TAnswer> AskAsync<TAnswer>(
        object state,
        IQuestion<TAnswer> question,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a batch of questions to evaluate against a state.
    /// </summary>
    /// <param name="state">The content to evaluate: a string, a POCO, or an array.</param>
    /// <returns>A builder that collects questions before the batch is sent.</returns>
    SystemOneQuery Query(object state);

    /// <summary>
    /// Evaluates a choice question against a state.
    /// </summary>
    /// <typeparam name="T">The type of the choice option values.</typeparam>
    /// <param name="state">The content to evaluate: a string, a POCO, or an array.</param>
    /// <param name="instructions">The question the model answers.</param>
    /// <param name="options">The options the model chooses from.</param>
    /// <param name="cancellationToken">A token that cancels the HTTP request and response buffering.</param>
    /// <returns>The detailed choice answer.</returns>
    Task<ChoiceAnswer<T>> ChoiceAsync<T>(
        object state,
        string instructions,
        IEnumerable<T> options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates a score question against a state.
    /// </summary>
    /// <param name="state">The content to evaluate: a string, a POCO, or an array.</param>
    /// <param name="instructions">The question the model answers.</param>
    /// <param name="levels">The ordered level descriptions. At least two levels are required.</param>
    /// <param name="cancellationToken">A token that cancels the HTTP request and response buffering.</param>
    /// <returns>The detailed score answer.</returns>
    Task<ScoreAnswer> ScoreAsync(
        object state,
        string instructions,
        IEnumerable<string> levels,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates a noul (yes/no) question against a state.
    /// </summary>
    /// <param name="state">The content to evaluate: a string, a POCO, or an array.</param>
    /// <param name="instructions">The yes/no question to evaluate.</param>
    /// <param name="cancellationToken">A token that cancels the HTTP request and response buffering.</param>
    /// <returns>The detailed noul answer.</returns>
    Task<NoulAnswer> NoulAsync(
        object state,
        string instructions,
        CancellationToken cancellationToken = default);
}
