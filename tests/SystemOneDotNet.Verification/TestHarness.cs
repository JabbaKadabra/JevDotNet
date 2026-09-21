using System.Net;
using System.Text;
using System.Text.Json;

namespace SystemOneDotNet.Verification;

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
    private readonly Func<HttpRequestMessage, string, CancellationToken, Task<HttpResponseMessage>> handler;
    private readonly List<RecordedRequest> requests = new();

    public FakeHandler(Func<HttpRequestMessage, string, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        this.handler = handler;
    }

    public IReadOnlyList<RecordedRequest> Requests => requests;

    public int CallCount => requests.Count;

    public RecordedRequest Last => requests[^1];

    public static FakeHandler Json(string body, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new((_, _, _) => Task.FromResult(Message(body, statusCode)));

    public static HttpResponseMessage Message(string body, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(statusCode) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);
        requests.Add(new RecordedRequest(request, body));
        return await handler(request, body, cancellationToken);
    }
}

/// <summary>A minimal check runner that prints results and tracks failures.</summary>
public sealed class CheckRunner
{
    private int passed;
    private int failed;

    public async Task CheckAsync(string name, Func<Task> check)
    {
        try
        {
            await check();
            passed++;
            Console.WriteLine($"  PASS  {name}");
        }
        catch (Exception exception)
        {
            failed++;
            Console.WriteLine($"  FAIL  {name}");
            Console.WriteLine($"        {exception.GetType().Name}: {exception.Message}");
        }
    }

    public int Complete()
    {
        Console.WriteLine();
        Console.WriteLine($"{passed} passed, {failed} failed.");
        return failed == 0 ? 0 : 1;
    }
}

/// <summary>Assertions used by the verification checks.</summary>
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
