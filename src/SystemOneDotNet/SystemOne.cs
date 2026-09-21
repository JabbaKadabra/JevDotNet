using SystemOneDotNet.Internal;

namespace SystemOneDotNet;

/// <summary>
/// An asynchronous client for the TypeSafe System One API. The client is safe to use from multiple
/// threads and can be reused for any number of requests.
/// </summary>
public sealed class SystemOne : ISystemOneClient
{
    private readonly string apiKey;
    private readonly SystemOneSettings settings;
    private readonly HttpClient httpClient;
    private readonly bool ownsHttpClient;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemOne"/> class.
    /// </summary>
    /// <param name="apiKey">The TypeSafe API key. Sent as a bearer token on every request.</param>
    /// <param name="options">Optional configuration. Defaults are used when <see langword="null"/>.</param>
    /// <param name="httpClient">
    /// An optional HTTP client to reuse. When <see langword="null"/>, the client owns and disposes an
    /// internal <see cref="HttpClient"/>.
    /// </param>
    public SystemOne(string apiKey, SystemOneOptions? options = null, HttpClient? httpClient = null)
    {
        this.apiKey = Guard.ApiKey(apiKey);
        settings = SystemOneSettings.From(options ?? new SystemOneOptions());

        ownsHttpClient = httpClient is null;
        this.httpClient = httpClient ?? new HttpClient();
    }

    /// <summary>
    /// Evaluates a single question against a state.
    /// </summary>
    /// <typeparam name="TAnswer">The answer type of the question.</typeparam>
    /// <param name="state">The content to evaluate: a string, a POCO, or an array.</param>
    /// <param name="question">The question to evaluate.</param>
    /// <param name="cancellationToken">A token that cancels the HTTP request and response buffering.</param>
    /// <returns>The typed answer.</returns>
    public async Task<TAnswer> AskAsync<TAnswer>(
        object state,
        IQuestion<TAnswer> question,
        CancellationToken cancellationToken = default)
    {
        var result = await Query(state).Question(question).SendAsync(cancellationToken).ConfigureAwait(false);
        return result.Get(question);
    }

    /// <summary>
    /// Starts a batch of questions to evaluate against a state.
    /// </summary>
    /// <param name="state">The content to evaluate: a string, a POCO, or an array.</param>
    /// <returns>A builder that collects questions before the batch is sent.</returns>
    public SystemOneQuery Query(object state)
    {
        ThrowIfDisposed();
        return new SystemOneQuery(
            this,
            Guard.NotNull(state, nameof(state), "The state to evaluate must not be null."));
    }

    /// <summary>
    /// Evaluates a choice question against a state.
    /// </summary>
    /// <typeparam name="T">The type of the choice option values.</typeparam>
    /// <param name="state">The content to evaluate: a string, a POCO, or an array.</param>
    /// <param name="instructions">The question the model answers.</param>
    /// <param name="options">The options the model chooses from.</param>
    /// <param name="cancellationToken">A token that cancels the HTTP request and response buffering.</param>
    /// <returns>The detailed choice answer.</returns>
    public Task<ChoiceAnswer<T>> ChoiceAsync<T>(
        object state,
        string instructions,
        IEnumerable<T> options,
        CancellationToken cancellationToken = default)
    {
        var question = new ChoiceQuestion<T>("choice", instructions, options);
        return AskAsync(state, question, cancellationToken);
    }

    /// <summary>
    /// Evaluates a score question against a state.
    /// </summary>
    /// <param name="state">The content to evaluate: a string, a POCO, or an array.</param>
    /// <param name="instructions">The question the model answers.</param>
    /// <param name="levels">The ordered level descriptions. At least two levels are required.</param>
    /// <param name="cancellationToken">A token that cancels the HTTP request and response buffering.</param>
    /// <returns>The detailed score answer.</returns>
    public Task<ScoreAnswer> ScoreAsync(
        object state,
        string instructions,
        IEnumerable<string> levels,
        CancellationToken cancellationToken = default)
    {
        var question = new ScoreQuestion("score", instructions, levels);
        return AskAsync(state, question, cancellationToken);
    }

    /// <summary>
    /// Evaluates a noul (yes/no) question against a state.
    /// </summary>
    /// <param name="state">The content to evaluate: a string, a POCO, or an array.</param>
    /// <param name="instructions">The yes/no question to evaluate.</param>
    /// <param name="cancellationToken">A token that cancels the HTTP request and response buffering.</param>
    /// <returns>The detailed noul answer.</returns>
    public Task<NoulAnswer> NoulAsync(
        object state,
        string instructions,
        CancellationToken cancellationToken = default)
    {
        var question = new NoulQuestion("noul", instructions);
        return AskAsync(state, question, cancellationToken);
    }

    /// <summary>
    /// Releases the internal HTTP client when this instance owns it.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        if (ownsHttpClient)
        {
            httpClient.Dispose();
        }
    }

    internal Task<SystemOneResult> SendAsync(
        object state,
        IReadOnlyList<IQuestion> questions,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<SystemOneResult>(cancellationToken);
        }

        foreach (var question in questions)
        {
            if (question is IChoiceQuestionLimits limits)
            {
                limits.ValidatePropertyLimits(settings.MaxChoiceProperties);
            }
        }

        return SystemOneTransport.SendAsync(
            httpClient,
            apiKey,
            settings.Endpoint,
            settings.Model,
            state,
            questions,
            cancellationToken);
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(SystemOne));
        }
    }
}
