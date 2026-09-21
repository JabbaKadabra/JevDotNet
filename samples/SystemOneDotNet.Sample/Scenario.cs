namespace SystemOneDotNet.Sample;

/// <summary>
/// One runnable demonstration, selected by name on the command line.
/// </summary>
/// <param name="Name">The short name passed to <c>dotnet run -- &lt;name&gt;</c>.</param>
/// <param name="Description">A one-line summary printed in the usage and the scenario header.</param>
/// <param name="Run">Runs the demonstration against the supplied client.</param>
public sealed record Scenario(
    string Name,
    string Description,
    Func<ISystemOneClient, CancellationToken, Task> Run);
