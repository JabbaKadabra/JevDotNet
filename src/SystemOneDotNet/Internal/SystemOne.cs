using SystemOneDotNet.Answers;
using SystemOneDotNet.Internal.Questions;
using SystemOneDotNet.Questions;

namespace SystemOneDotNet.Internal;

/// <summary>
/// The default <see cref="ISystemOneClient"/>. Safe to use from multiple threads and reusable for any
/// number of requests. Created through <see cref="SystemOneClient.Create"/>.
/// </summary>
internal sealed class SystemOne : ISystemOneClient
{
    private readonly string apiKey;
    private readonly SystemOneSettings settings;
    private readonly HttpClient httpClient;
    private readonly bool ownsHttpClient;
    private bool disposed;

    public SystemOne(string apiKey, SystemOneOptions? options, HttpClient? httpClient)
    {
        this.apiKey = Guard.ApiKey(apiKey);
        settings = SystemOneSettings.From(options ?? new SystemOneOptions());

        ownsHttpClient = httpClient is null;
        this.httpClient = httpClient ?? new HttpClient();
    }

    public async Task<TAnswer> AskAsync<TAnswer>(
        object state,
        IQuestion<TAnswer> question,
        CancellationToken cancellationToken = default)
    {
        var result = await Query(state).Question(question).SendAsync(cancellationToken).ConfigureAwait(false);
        return result.Get(question);
    }

    public ISystemOneQuery Query(object state)
    {
        ThrowIfDisposed();
        return new SystemOneQuery(
            this,
            Guard.NotNull(state, nameof(state), "The state to evaluate must not be null."));
    }

    public Task<ChoiceAnswer<T>> ChoiceAsync<T>(
        object state,
        string instructions,
        IEnumerable<T> options,
        CancellationToken cancellationToken = default) =>
        AskAsync(state, new ChoiceQuestion<T>("choice", instructions, options), cancellationToken);

    public Task<ScoreAnswer> ScoreAsync(
        object state,
        string instructions,
        IEnumerable<string> levels,
        CancellationToken cancellationToken = default) =>
        AskAsync(state, new ScoreQuestion("score", instructions, levels), cancellationToken);

    public Task<NoulAnswer> NoulAsync(
        object state,
        string instructions,
        CancellationToken cancellationToken = default) =>
        AskAsync(state, new NoulQuestion("noul", instructions, null, null), cancellationToken);

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

    /// <summary>Validates the batch against the client settings and sends it.</summary>
    /// <param name="state">The content to evaluate.</param>
    /// <param name="questions">The snapshot of questions in the batch.</param>
    /// <param name="cancellationToken">A token that cancels the HTTP request and response buffering.</param>
    /// <returns>The result containing one typed answer per question.</returns>
    internal Task<ISystemOneResult> SendAsync(
        object state,
        IReadOnlyList<ISystemOneQuestion> questions,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<ISystemOneResult>(cancellationToken);
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
            throw new ObjectDisposedException(nameof(ISystemOneClient));
        }
    }
}
