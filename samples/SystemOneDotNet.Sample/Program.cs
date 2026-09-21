using SystemOneDotNet;
using SystemOneDotNet.Sample;

// The sample runs against the live TypeSafe System One API, so it needs a real key.
//
//   export SYSTEMONE_API_KEY="your-key"
//   dotnet run --project samples/SystemOneDotNet.Sample            # every scenario
//   dotnet run --project samples/SystemOneDotNet.Sample -- batch   # one scenario
//
// Set SYSTEMONE_MODEL to override the model instead of the jev-latest default.

string? requested = args.Length > 0 ? args[0] : null;
if (requested is "-h" or "--help" or "help")
{
    Output.PrintUsage();
    return 0;
}

string? apiKey = Environment.GetEnvironmentVariable("SYSTEMONE_API_KEY");
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

// SystemOneOptions is optional. Endpoint, Model, and MaxChoiceProperties all have
// sensible defaults; the model override here is driven by SYSTEMONE_MODEL.
var options = new SystemOneOptions
{
    Model = Environment.GetEnvironmentVariable("SYSTEMONE_MODEL") ?? SystemOneOptions.DefaultModel,
};

using ISystemOneClient systemOne = SystemOneClient.Create(apiKey, options);
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
        await scenarios[index].Run(systemOne, cancellation.Token);
    }
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine("Cancelled.");
    return 130;
}
catch (SystemOneException exception)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine($"{exception.GetType().Name}: {exception.Message}");
    return 1;
}

return 0;
