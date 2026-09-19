using System;
using System.Collections.Generic;

namespace JevDotNet
{
    /// <summary>Token usage reported for a completed request.</summary>
    public sealed class JevUsage
    {
        internal JevUsage(int inputTokens, int outputTokens)
        {
            InputTokens = inputTokens;
            OutputTokens = outputTokens;
        }

        /// <summary>Gets the number of input tokens consumed by the request.</summary>
        public int InputTokens { get; }

        /// <summary>Gets the number of output tokens produced by the request.</summary>
        public int OutputTokens { get; }
    }

    /// <summary>The answers, model, and token usage returned for a batch.</summary>
    public sealed class JevResult
    {
        private readonly Dictionary<string, object> _answers;

        internal JevResult(string model, JevUsage usage, Dictionary<string, object> answers)
        {
            Model = model;
            Usage = usage;
            _answers = answers;
        }

        /// <summary>Gets the model that performed the evaluation.</summary>
        public string Model { get; }

        /// <summary>Gets the token usage for the request.</summary>
        public JevUsage Usage { get; }

        /// <summary>Gets the ids of the questions contained in this result.</summary>
        public IReadOnlyCollection<string> QuestionIds => _answers.Keys;

        /// <summary>Gets the typed answer for a question that was part of the batch.</summary>
        /// <typeparam name="TAnswer">The answer type of the question.</typeparam>
        /// <param name="question">A question that was added to the batch.</param>
        /// <returns>The typed answer.</returns>
        /// <exception cref="JevValidationException">The question was not part of the batch that produced this result.</exception>
        /// <exception cref="JevProtocolException">The stored answer does not match the question's answer type.</exception>
        public TAnswer Get<TAnswer>(IQuestion<TAnswer> question)
        {
            if (question == null)
            {
                throw new ArgumentNullException(nameof(question));
            }

            if (!_answers.TryGetValue(question.Id, out var answer))
            {
                throw new JevValidationException(
                    $"The result does not contain an answer for question '{question.Id}'. Only questions added to the batch can be read.");
            }

            if (answer is TAnswer typed)
            {
                return typed;
            }

            throw new JevProtocolException(
                $"The answer for question '{question.Id}' is a {answer.GetType().Name} and cannot be read as {typeof(TAnswer).Name}.");
        }
    }
}
