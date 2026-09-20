namespace JevDotNet.Internal;

/// <summary>Implemented by choice questions whose options must respect <see cref="JevOptions.MaxChoiceProperties"/>.</summary>
internal interface IChoiceQuestionLimits
{
    /// <summary>Validates every option's serialized property count and throws when a limit is exceeded.</summary>
    /// <param name="maxProperties">The maximum number of serialized properties allowed per option.</param>
    void ValidatePropertyLimits(int maxProperties);
}
