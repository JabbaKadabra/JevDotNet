using System.Text.Json.Serialization;

namespace JevDotNet.Sample;

/// <summary>
/// The content that is evaluated: a support ticket, sent as structured state.
/// </summary>
public sealed record Ticket(string Subject, string Body, string CustomerTier);

/// <summary>
/// A routing target offered as a structured choice option.
/// </summary>
public sealed record Team
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Team"/> class.
    /// </summary>
    /// <param name="name">The team name. Sent to the model as part of the option description.</param>
    /// <param name="email">The team email address. Sent to the model as part of the option description.</param>
    /// <param name="slackChannel">The Slack channel to notify after routing. Never sent to the model.</param>
    public Team(string name, string email, string slackChannel)
    {
        Name = name;
        Email = email;
        SlackChannel = slackChannel;
    }

    /// <summary>
    /// Gets the team name. Sent to the model as part of the option description.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Gets the team email address. Sent to the model as part of the option description.
    /// </summary>
    public string Email { get; init; }

    /// <summary>
    /// Gets the Slack channel to notify after routing. Members marked with <see cref="JsonIgnoreAttribute"/>
    /// are never sent to the model and do not count toward <see cref="JevOptions.MaxChoiceProperties"/>.
    /// </summary>
    [JsonIgnore]
    public string SlackChannel { get; init; }
}

/// <summary>
/// The example state and options shared by the scenarios.
/// </summary>
public static class SampleData
{
    /// <summary>
    /// A realistic ticket used by every scenario.
    /// </summary>
    public static Ticket ExampleTicket { get; } = new(
        "Stripe connection keeps failing",
        "Hi, I've been trying to connect my Stripe account for 3 days and it keeps " +
        "failing. I'm losing sales. Please help ASAP.",
        "Pro");

    /// <summary>
    /// The teams used by the choice scenarios.
    /// </summary>
    public static IReadOnlyList<Team> Teams { get; } =
    [
        new Team("Billing", "billing@example.com", "#billing"),
        new Team("Technical", "tech@example.com", "#technical"),
        new Team("Sales", "sales@example.com", "#sales"),
    ];
}
