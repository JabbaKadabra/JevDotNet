using System.Net;
using System.Text;
using System.Text.Json;

namespace JevDotNet.Verification;

/// <summary>A request captured by <see cref="FakeHandler"/>.</summary>
public sealed class RecordedRequest
{
    public RecordedRequest(HttpRequestMessage message, string body)
    {
        Message = message;
        Body = body;
    }

    public HttpRequestMessage Message { get; }

    public string Body { get; }

    public JsonElement Json => JsonDocument.Parse(Body).RootElement;

    public string? Authorization =>
        Message.Headers.TryGetValues("Authorization", out var values) ? values.FirstOrDefault() : null;

    public JsonElement Question(string id) => Json.GetProperty("questions").GetProperty(id);
}

/// <summary>A fake <see cref="HttpMessageHandler"/> used to verify the library without a live API.</summary>
public sealed class FakeHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, string, CancellationToken, Task<HttpResponseMessage>> _handler;

    public FakeHandler(Func<HttpRequestMessage, string, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    public List<RecordedRequest> Requests { get; } = new();

    public int CallCount => Requests.Count;

    public RecordedRequest Last => Requests[^1];

    public static FakeHandler Json(string body, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new((_, _, _) => Task.FromResult(Message(body, statusCode)));

    public static HttpResponseMessage Message(string body, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(statusCode) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content == null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add(new RecordedRequest(request, body));
        return await _handler(request, body, cancellationToken);
    }
}

/// <summary>A minimal check runner that prints results and tracks failures.</summary>
public sealed class CheckRunner
{
    private int _passed;
    private int _failed;

    public async Task CheckAsync(string name, Func<Task> check)
    {
        try
        {
            await check();
            _passed++;
            Console.WriteLine($"  PASS  {name}");
        }
        catch (Exception exception)
        {
            _failed++;
            Console.WriteLine($"  FAIL  {name}");
            Console.WriteLine($"        {exception.GetType().Name}: {exception.Message}");
        }
    }

    public int Complete()
    {
        Console.WriteLine();
        Console.WriteLine($"{_passed} passed, {_failed} failed.");
        return _failed == 0 ? 0 : 1;
    }
}

public static class Expect
{
    public static void True(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message} Expected '{expected}' but found '{actual}'.");
        }
    }

    public static void Same(object expected, object actual, string message)
    {
        if (!ReferenceEquals(expected, actual))
        {
            throw new InvalidOperationException($"{message} The instances are different.");
        }
    }

    public static void Contains(string expectedPart, string actual, string message)
    {
        if (!actual.Contains(expectedPart, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{message} '{expectedPart}' was not found in '{actual}'.");
        }
    }

    public static async Task ThrowsAsync<TException>(Func<Task> action, string message)
        where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException)
        {
            return;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"{message} Expected {typeof(TException).Name} but found {exception.GetType().Name}.");
        }

        throw new InvalidOperationException($"{message} Expected {typeof(TException).Name} but no exception was thrown.");
    }
}
