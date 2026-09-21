using SystemOneDotNet.Internal;

namespace SystemOneDotNet;

/// <summary>
/// Creates <see cref="ISystemOneClient"/> instances for the TypeSafe System One API.
/// </summary>
public static class SystemOneClient
{
    /// <summary>
    /// Creates a client that authenticates with the supplied API key.
    /// </summary>
    /// <param name="apiKey">The TypeSafe API key. Sent as a bearer token on every request.</param>
    /// <param name="options">Optional configuration. Defaults are used when <see langword="null"/>.</param>
    /// <param name="httpClient">
    /// An optional HTTP client to reuse. When <see langword="null"/>, the client owns and disposes an
    /// internal <see cref="HttpClient"/>. A supplied client is never disposed by the library.
    /// </param>
    /// <returns>A thread-safe client that can be reused for any number of requests.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="apiKey"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="apiKey"/> is empty, or <paramref name="options"/> contains an invalid value.</exception>
    public static ISystemOneClient Create(
        string apiKey,
        SystemOneOptions? options = null,
        HttpClient? httpClient = null) 
        => new SystemOne(apiKey, options, httpClient);
}
