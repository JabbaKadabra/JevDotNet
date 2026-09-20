namespace JevDotNet.Sample;

/// <summary>
/// The scenario catalog and the demonstrations themselves.
/// </summary>
public static class Scenarios
{
    /// <summary>
    /// Every scenario, in the order they are presented.
    /// </summary>
    public static IReadOnlyList<Scenario> All { get; } =
    [
        new Scenario(
            "quickstart",
            "Direct calls: ChoiceAsync, ScoreAsync, and NoulAsync.",
            QuickStartAsync),
        new Scenario(
            "structured-options",
            "A choice over POCO options that are sent as structured criteria.",
            StructuredOptionsAsync),
        new Scenario(
            "batch",
            "Reusable questions sent in one request with typed answers and token usage.",
            BatchAsync),
        new Scenario(
            "named-options",
            "The criteria dictionary of the TypeSafe quickstart with named options.",
            NamedOptionsAsync),
        new Scenario(
            "errors",
            "Local validation, cancellation, and the exception types to catch.",
            ErrorsAsync),
    ];

    /// <summary>
    /// Finds a scenario by name. An empty name or <c>all</c> selects every scenario.
    /// </summary>
    /// <param name="name">The scenario name, or <see langword="null"/> for everything.</param>
    /// <returns>The matching scenarios, or an empty list when the name is unknown.</returns>
    public static IReadOnlyList<Scenario> Find(string? name)
    {
        if (string.IsNullOrEmpty(name) || string.Equals(name, "all", StringComparison.OrdinalIgnoreCase))
        {
            return All;
        }

        var scenario = All.FirstOrDefault(
            candidate => string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));
        return scenario is null ? [] : [scenario];
    }

    private static async Task QuickStartAsync(Jev jev, CancellationToken cancellationToken)
    {
        // The state can be a string, a POCO, or an array. Every direct call evaluates
        // one question and returns its detailed answer.
        string ticket = SampleData.ExampleTicket.Body;

        ChoiceAnswer<string> department = await jev.ChoiceAsync(
            ticket,
            "Which team should handle this ticket?",
            new[] { "billing", "technical", "sales" },
            cancellationToken);

        Console.WriteLine($"department  {department.Choice} ({department.Confidence:P0} confidence)");
        foreach (var option in department.Options)
        {
            Console.WriteLine($"              {option.Probability,7:P1}  {option.Option}");
        }

        ScoreAnswer frustration = await jev.ScoreAsync(
            ticket,
            "How frustrated is the customer?",
            new[] { "Calm", "Mildly frustrated", "Frustrated", "Very angry" },
            cancellationToken);

        Console.WriteLine();
        Console.WriteLine($"frustration {frustration.Score:F2} ({frustration.Confidence:P0} confidence)");
        if (frustration.Probabilities is not null)
        {
            foreach (var level in frustration.Probabilities)
            {
                Console.WriteLine($"              {level.Probability,7:P1}  {level.Level}");
            }
        }

        NoulAnswer urgency = await jev.NoulAsync(
            ticket,
            "Does this ticket convey urgency?",
            cancellationToken);

        // Noul reports the probability that the answer is yes, never a bool.
        // Pick the threshold deliberately for your use case.
        const double urgencyThreshold = 0.5;
        Console.WriteLine();
        Console.WriteLine(
            $"urgency     {urgency.Noul:P1} -> {(urgency.Noul >= urgencyThreshold ? "urgent" : "not urgent")} " +
            $"(threshold {urgencyThreshold:P0})");
    }

    private static async Task StructuredOptionsAsync(Jev jev, CancellationToken cancellationToken)
    {
        // Option values can be POCOs. Each option is sent to the model as its JSON
        // description, so the model reasons about fields instead of opaque labels.
        ChoiceAnswer<Team> routing = await jev.ChoiceAsync(
            SampleData.ExampleTicket,
            "Which team should handle this ticket?",
            SampleData.Teams,
            cancellationToken);

        Console.WriteLine($"department  {routing.Choice.Name} ({routing.Confidence:P0} confidence)");
        foreach (var option in routing.Options)
        {
            Console.WriteLine($"              {option.Probability,7:P1}  {option.Option.Name,-10} {option.Option.Email}");
        }

        // The answer carries the exact Team instance that was supplied, so members
        // that were never sent to the model (see Team.SlackChannel) are still available.
        Console.WriteLine();
        Console.WriteLine($"notify      {routing.Choice.SlackChannel}");
    }

    private static async Task BatchAsync(Jev jev, CancellationToken cancellationToken)
    {
        // Reusable questions carry no response state and can be mixed in one batch.
        var department = new Choice<Team>(
            "department",
            "Which team should handle this ticket?",
            SampleData.Teams);

        var urgency = new Noul(
            "urgency",
            "Does this ticket convey urgency?",
            trueDescription: "The customer is blocked or losing money.",
            falseDescription: "The request can wait for the next business day.");

        var frustration = new Score(
            "frustration",
            "How frustrated is the customer?",
            new[] { "Calm", "Mildly frustrated", "Frustrated", "Very angry" });

        var churnRisk = new Noul(
            "churn_risk",
            "Is the customer at risk of churning?");

        JevResult result = await jev.Query(SampleData.ExampleTicket)
            .Question(department)
            .Question(urgency)
            .Question(frustration)
            .Question(churnRisk)
            .SendAsync(cancellationToken);

        Console.WriteLine($"model       {result.Model}");
        Console.WriteLine($"tokens      {result.Usage.InputTokens} in / {result.Usage.OutputTokens} out");
        Console.WriteLine($"questions   {string.Join(", ", result.QuestionIds)}");
        Console.WriteLine();

        // Answers are read back with the typed question handles used in the batch.
        var routed = result.Get(department);
        var urgent = result.Get(urgency);
        var anger = result.Get(frustration);
        var churn = result.Get(churnRisk);

        Console.WriteLine($"department  {routed.Choice.Name} ({routed.Confidence:P0} confidence)");
        Console.WriteLine($"urgency     {urgent.Noul:P1}");
        Console.WriteLine($"frustration {anger.Score:F2} of {anger.Legend.Count - 1} ({anger.Confidence:P0} confidence)");
        Console.WriteLine($"churn risk  {churn.Noul:P1}");
    }

    private static async Task NamedOptionsAsync(Jev jev, CancellationToken cancellationToken)
    {
        // ChoiceQuestion matches the criteria dictionary of the TypeSafe quickstart:
        // each key is the value returned for that option, and the value describes it.
        var department = new ChoiceQuestion(
            "department",
            "Which team should handle this ticket?",
            new Dictionary<string, string?>
            {
                ["billing"] = "Payment, invoice, and subscription problems",
                ["technical"] = "Bugs, errors, and integration problems",
                ["sales"] = null,
            });

        // AskAsync is the single-question shortcut for Query(...).Question(...).SendAsync().
        ChoiceAnswer<string> answer = await jev.AskAsync(
            SampleData.ExampleTicket,
            department,
            cancellationToken);

        Console.WriteLine($"department  {answer.Choice} ({answer.Confidence:P0} confidence)");
        foreach (var option in answer.Options)
        {
            Console.WriteLine($"              {option.Probability,7:P1}  {option.Option}");
        }
    }

    private static async Task ErrorsAsync(Jev jev, CancellationToken cancellationToken)
    {
        Ticket ticket = SampleData.ExampleTicket;

        // Local validation fails before any HTTP request is sent.
        try
        {
            await jev.ChoiceAsync(
                ticket,
                "Which team should handle this ticket?",
                Array.Empty<string>(),
                cancellationToken);
        }
        catch (JevValidationException exception)
        {
            Console.WriteLine($"JevValidationException: {exception.Message}");
        }

        try
        {
            await jev.Query(ticket)
                .Noul(new Noul("urgency", "Does this ticket convey urgency?"))
                .Noul(new Noul("urgency", "Does this ticket convey urgency?"))
                .SendAsync(cancellationToken);
        }
        catch (JevValidationException exception)
        {
            Console.WriteLine($"JevValidationException: {exception.Message}");
        }

        // Cancellation propagates the original token through the HTTP request
        // and response buffering. This timeout is deliberately tiny to force it.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMilliseconds(1));
        try
        {
            await jev.NoulAsync(ticket, "Does this ticket convey urgency?", timeout.Token);
            Console.WriteLine("The request completed before the 1 ms timeout.");
        }
        catch (OperationCanceledException exception)
        {
            Console.WriteLine(
                $"OperationCanceledException: cancelled={exception.CancellationToken.IsCancellationRequested}");
        }

        // Handle these at the edge of your application:
        Console.WriteLine();
        Console.WriteLine("catch (JevApiException ex)      // non-success status; ex.StatusCode, ex.ResponseBody");
        Console.WriteLine("catch (JevProtocolException ex)  // malformed or mismatched answer");
        Console.WriteLine("catch (JevException ex)          // base class for both");
    }
}
