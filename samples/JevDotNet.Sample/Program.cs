using JevDotNet;
using JevDotNet.Sample;

// The sample runs against the live TypeSafe System One API, so it needs a real key.
//
//   export JEV_API_KEY="your-key"
//   dotnet run --project samples/JevDotNet.Sample            # every scenario
//   dotnet run --project samples/JevDotNet.Sample -- batch   # one scenario
//
// Set JEV_MODEL to override the model instead of the jev-latest default.

string? requested = args.Length > 0 ? args[0] : null;
if (requested is "-h" or "--help" or "help")
{
    Output.PrintUsage();
    return 0;
}

string? apiKey = Environment.GetEnvironmentVariable("JEV_API_KEY");
if (string.IsNullOrWhiteSpace(apiKey))
{
    Output.PrintMissingApiKey();
    return 1;
}

IReadOnlyList<Scenario> scenarios = Scenarios.Find(requested);
if (scenarios.Count == 0)
{
    Console.Error.WriteLine($"Unknown scenario '{requested}'.");
    Console.Error.WriteLine();
    Output.PrintUsage();
    return 2;
}

// Ctrl+C cancels the in-flight request and lets the process shut down cleanly.
using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

// JevOptions is optional. Endpoint, Model, and MaxChoiceProperties all have
// sensible defaults; the model override here is driven by JEV_MODEL.
var options = new JevOptions
{
    Model = Environment.GetEnvironmentVariable("JEV_MODEL") ?? JevOptions.DefaultModel,
};

using var jev = new Jev(apiKey, options);
Output.Banner(options.Endpoint, options.Model);

try
{
    for (var index = 0; index < scenarios.Count; index++)
    {
        if (index > 0)
        {
            Console.WriteLine();
        }

        Output.ScenarioHeader(scenarios[index]);
        await scenarios[index].Run(jev, cancellation.Token);
    }
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine("Cancelled.");
    return 130;
}
catch (JevException exception)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine($"{exception.GetType().Name}: {exception.Message}");
    return 1;
}

return 0;
