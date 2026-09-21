namespace SystemOneDotNet.Answers;

/// <summary>
/// The detailed answer to a noul (yes/no) question.
/// </summary>
/// <param name="Noul">
/// The probability that the answer is yes, from 0 (no) to 1 (yes). No automatic boolean conversion is
/// performed.
/// </param>
public sealed record NoulAnswer(
    double Noul);
