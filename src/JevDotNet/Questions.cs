namespace JevDotNet;

/// <summary>
/// A reusable choice question whose options are arbitrary values. Identical to
/// <see cref="ChoiceQuestion{T}"/> and provided so the public API reads naturally.
/// </summary>
/// <typeparam name="T">The type of the option values.</typeparam>
public sealed record Choice<T> : ChoiceQuestion<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Choice{T}"/> class.
    /// </summary>
    /// <param name="id">The identifier that matches the question with its answer inside a batch.</param>
    /// <param name="instructions">The question the model answers.</param>
    /// <param name="options">The options the model chooses from.</param>
    public Choice(string id, string instructions, IEnumerable<T> options)
        : base(id, instructions, options)
    {
    }
}

/// <summary>
/// A reusable score question. Identical to <see cref="ScoreQuestion"/> and provided so the public API
/// reads naturally.
/// </summary>
public sealed record Score : ScoreQuestion
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Score"/> class.
    /// </summary>
    /// <param name="id">The identifier that matches the question with its answer inside a batch.</param>
    /// <param name="instructions">The question the model answers.</param>
    /// <param name="levels">The ordered level descriptions. At least two levels are required.</param>
    public Score(string id, string instructions, IEnumerable<string> levels)
        : base(id, instructions, levels)
    {
    }
}

/// <summary>
/// A reusable noul (yes/no) question. Identical to <see cref="NoulQuestion"/> and provided so the public
/// API reads naturally.
/// </summary>
public sealed record Noul : NoulQuestion
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Noul"/> class.
    /// </summary>
    /// <param name="id">The identifier that matches the question with its answer inside a batch.</param>
    /// <param name="instructions">The yes/no question to evaluate.</param>
    /// <param name="trueDescription">An optional description of what a yes means.</param>
    /// <param name="falseDescription">An optional description of what a no means.</param>
    public Noul(string id, string instructions, string? trueDescription = null, string? falseDescription = null)
        : base(id, instructions, trueDescription, falseDescription)
    {
    }
}
