namespace SystemOneDotNet.Sample;

/// <summary>
/// Console formatting shared by the scenarios.
/// </summary>
public static class Output
{
    /// <summary>
    /// Prints the banner that names the endpoint and model in use.
    /// </summary>
    /// <param name="endpoint">The configured System One endpoint.</param>
    /// <param name="model">The configured model.</param>
    public static void Banner(string endpoint, string model)
    {
        Console.WriteLine("SystemOneDotNet sample - live calls against TypeSafe System One");
        Console.WriteLine($"endpoint {endpoint}");
        Console.WriteLine($"model    {model}");
    }

    /// <summary>
    /// Prints the header for a scenario.
    /// </summary>
    /// <param name="scenario">The scenario about to run.</param>
    public static void ScenarioHeader(Scenario scenario)
    {
        Console.WriteLine(new string('-', 78));
        Console.WriteLine(scenario.Name);
        Console.WriteLine(scenario.Description);
        Console.WriteLine(new string('-', 78));
    }

    /// <summary>
    /// Prints how to run the sample and lists every scenario.
    /// </summary>
    public static void PrintUsage()
    {
        Console.WriteLine("Usage: dotnet run --project samples/SystemOneDotNet.Sample [scenario]");
        Console.WriteLine();
        Console.WriteLine("Calls the live TypeSafe System One API and requires SYSTEMONE_API_KEY.");
        Console.WriteLine("Set SYSTEMONE_MODEL to override the default model.");
        Console.WriteLine();
        Console.WriteLine("Scenarios:");
        foreach (var scenario in Scenarios.All)
        {
            Console.WriteLine($"  {scenario.Name,-18} {scenario.Description}");
        }

        Console.WriteLine($"  {"all",-18} Run every scenario (default).");
    }

    /// <summary>
    /// Prints the environment setup needed before the sample can call the API.
    /// </summary>
    public static void PrintMissingApiKey()
    {
        Console.Error.WriteLine("SYSTEMONE_API_KEY is not set. The sample calls the live TypeSafe API.");
        Console.Error.WriteLine();
        Console.Error.WriteLine("  export SYSTEMONE_API_KEY=\"your-key\"");
        Console.Error.WriteLine("  dotnet run --project samples/SystemOneDotNet.Sample");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Optionally set SYSTEMONE_MODEL to use a model other than the default.");
    }
}
