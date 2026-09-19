using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JevDotNet.Internal;

namespace JevDotNet
{
    /// <summary>
    /// A mutable batch of questions that is sent with <see cref="SendAsync"/>. Builders are not thread-safe;
    /// use one builder per batch. Inputs are snapshotted when the batch is sent.
    /// </summary>
    public sealed class JevQuery
    {
        private readonly Jev _jev;
        private readonly object _state;
        private readonly List<IQuestion> _questions = new List<IQuestion>();
        private readonly HashSet<string> _questionIds = new HashSet<string>(System.StringComparer.Ordinal);
        private readonly HashSet<object> _questionHandles = new HashSet<object>(ReferenceComparer.Instance);

        internal JevQuery(Jev jev, object state)
        {
            _jev = jev;
            _state = state;
        }

        /// <summary>Adds any question to the batch and returns this builder.</summary>
        /// <typeparam name="TAnswer">The answer type of the question.</typeparam>
        /// <param name="question">The question to add.</param>
        /// <returns>This builder, so calls can be chained.</returns>
        public JevQuery Question<TAnswer>(IQuestion<TAnswer> question)
        {
            Guard.NotNull(question, nameof(question), "Questions must not be null.");
            Add(question);
            return this;
        }

        /// <summary>Adds a choice question to the batch and returns this builder.</summary>
        /// <typeparam name="T">The type of the choice option values.</typeparam>
        /// <param name="question">The choice question to add.</param>
        /// <returns>This builder, so calls can be chained.</returns>
        public JevQuery Choice<T>(IQuestion<ChoiceAnswer<T>> question)
        {
            Guard.NotNull(question, nameof(question), "Questions must not be null.");
            Add(question);
            return this;
        }

        /// <summary>Adds a score question to the batch and returns this builder.</summary>
        /// <param name="question">The score question to add.</param>
        /// <returns>This builder, so calls can be chained.</returns>
        public JevQuery Score(IQuestion<ScoreAnswer> question)
        {
            Guard.NotNull(question, nameof(question), "Questions must not be null.");
            Add(question);
            return this;
        }

        /// <summary>Adds a noul question to the batch and returns this builder.</summary>
        /// <param name="question">The noul question to add.</param>
        /// <returns>This builder, so calls can be chained.</returns>
        public JevQuery Noul(IQuestion<NoulAnswer> question)
        {
            Guard.NotNull(question, nameof(question), "Questions must not be null.");
            Add(question);
            return this;
        }

        /// <summary>Sends the batch and returns the typed answers.</summary>
        /// <param name="cancellationToken">A token that cancels the HTTP request and response buffering.</param>
        /// <returns>The result containing one typed answer per question.</returns>
        public Task<JevResult> SendAsync(CancellationToken cancellationToken = default)
        {
            var questions = _questions.ToArray();
            if (questions.Length == 0)
            {
                throw new JevValidationException("A Jev batch must contain at least one question.");
            }

            return _jev.SendAsync(_state, questions, cancellationToken);
        }

        private void Add(IQuestion question)
        {
            if (!_questionHandles.Add(question))
            {
                throw new JevValidationException(
                    $"The question with id '{question.Id}' was already added to this batch.");
            }

            if (!_questionIds.Add(question.Id))
            {
                _questionHandles.Remove(question);
                throw new JevValidationException(
                    $"The question id '{question.Id}' is already used in this batch. Question ids must be unique.");
            }

            _questions.Add(question);
        }
    }
}
