using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using JevDotNet.Internal;

namespace JevDotNet
{
    /// <summary>
    /// A choice question whose options are arbitrary values. Generic options are materialized once and each
    /// option is sent to the model as its JSON description.
    /// </summary>
    /// <typeparam name="T">The type of the option values.</typeparam>
    public class ChoiceQuestion<T> : IQuestion<ChoiceAnswer<T>>, IJevQuestion, IChoiceQuestionLimits
    {
        /// <summary>The maximum number of options accepted by the API.</summary>
        internal const int MaxOptionCount = 255;

        private readonly T[] _options;

        /// <summary>Initializes a new instance of the <see cref="ChoiceQuestion{T}"/> class.</summary>
        /// <param name="id">The identifier that matches the question with its answer inside a batch.</param>
        /// <param name="instructions">The question the model answers.</param>
        /// <param name="options">The options the model chooses from.</param>
        public ChoiceQuestion(string id, string instructions, IEnumerable<T> options)
        {
            Id = Guard.Id(id);
            Instructions = Guard.Instructions(instructions);
            Guard.NotNull(options, nameof(options), "Choice options must not be null.");

            _options = options.ToArray();
            if (_options.Length == 0)
            {
                throw new JevValidationException(
                    $"Choice question '{Id}' must define at least one option.");
            }

            if (_options.Length > MaxOptionCount)
            {
                throw new JevValidationException(
                    $"Choice question '{Id}' defines {_options.Length} options; the limit is {MaxOptionCount}.");
            }

            for (var index = 0; index < _options.Length; index++)
            {
                if (_options[index] is null)
                {
                    throw new JevValidationException(
                        $"Choice question '{Id}' contains a null option at index {index}.");
                }
            }
        }

        /// <inheritdoc />
        public string Id { get; }

        /// <inheritdoc />
        public string Instructions { get; }

        /// <summary>Gets the original option values, in the order they were supplied.</summary>
        public IReadOnlyList<T> Options => _options;

        internal static string OptionId(int index)
        {
            return index.ToString(CultureInfo.InvariantCulture);
        }

        string IJevQuestion.QuestionType => "choice";

        void IJevQuestion.WriteQuestion(Utf8JsonWriter writer)
        {
            writer.WriteString("type", "choice");
            writer.WriteString("instructions", Instructions);
            writer.WriteStartObject("criteria");
            for (var index = 0; index < _options.Length; index++)
            {
                writer.WritePropertyName(OptionId(index));
                JsonFormat.WriteDescription(writer, _options[index]);
            }

            writer.WriteEndObject();
        }

        void IChoiceQuestionLimits.ValidatePropertyLimits(int maxProperties)
        {
            for (var index = 0; index < _options.Length; index++)
            {
                var count = PropertyCounter.Count(_options[index], $"Choice option {index}");
                if (count > maxProperties)
                {
                    throw new JevValidationException(
                        $"Choice option {index} contains {count} serialized properties; the limit is {maxProperties}.\n" +
                        "Use a dedicated smaller POCO or increase JevOptions.MaxChoiceProperties\n" +
                        "when constructing Jev.");
                }
            }
        }

        object IJevQuestion.ReadAnswer(JsonElement answer)
        {
            var answerObject = AnswerReader.RequireObject(answer, Id);
            AnswerReader.RequireType(answerObject, "choice", Id);

            var choiceId = AnswerReader.RequireString(answerObject, "choice", Id);
            var confidence = AnswerReader.RequireUnitInterval(
                AnswerReader.RequireNumber(answerObject, "confidence", Id),
                "The 'confidence' value",
                Id);

            var probabilities = AnswerReader.RequireObject(
                AnswerReader.RequireProperty(answerObject, "probabilities", Id),
                Id);

            var probabilitiesByIndex = new double[_options.Length];
            var found = new bool[_options.Length];
            foreach (var property in probabilities.EnumerateObject())
            {
                var index = AnswerReader.ParseOptionIndex(property.Name, _options.Length, Id);
                if (found[index])
                {
                    throw new JevProtocolException(
                        $"The answer for question '{Id}' contains duplicate probabilities for option id '{property.Name}'.");
                }

                if (property.Value.ValueKind != JsonValueKind.Number)
                {
                    throw new JevProtocolException(
                        $"The probability for option id '{property.Name}' of question '{Id}' must be a number.");
                }

                probabilitiesByIndex[index] = AnswerReader.RequireUnitInterval(
                    property.Value.GetDouble(),
                    $"The probability for option id '{property.Name}'",
                    Id);
                found[index] = true;
            }

            var selectedIndex = -1;
            var ordered = new List<ChoiceProbability<T>>(_options.Length);
            for (var index = 0; index < _options.Length; index++)
            {
                if (!found[index])
                {
                    throw new JevProtocolException(
                        $"The answer for question '{Id}' is missing the probability for option id '{OptionId(index)}'.");
                }

                ordered.Add(new ChoiceProbability<T>(_options[index], probabilitiesByIndex[index]));
                if (string.Equals(choiceId, OptionId(index), StringComparison.Ordinal))
                {
                    selectedIndex = index;
                }
            }

            if (selectedIndex < 0)
            {
                throw new JevProtocolException(
                    $"The answer for question '{Id}' selected the unknown option id '{choiceId}'.");
            }

            return new ChoiceAnswer<T>(_options[selectedIndex], confidence, ordered);
        }
    }

    /// <summary>
    /// A choice question with named string options and optional descriptions, matching the criteria dictionary
    /// of the TypeSafe quickstart.
    /// </summary>
    public class ChoiceQuestion : IQuestion<ChoiceAnswer<string>>, IJevQuestion
    {
        private readonly string[] _names;
        private readonly string?[] _descriptions;

        /// <summary>Initializes a new instance of the <see cref="ChoiceQuestion"/> class.</summary>
        /// <param name="id">The identifier that matches the question with its answer inside a batch.</param>
        /// <param name="instructions">The question the model answers.</param>
        /// <param name="options">The named options with optional descriptions. Use <see langword="null"/> when an option needs no extra detail.</param>
        public ChoiceQuestion(string id, string instructions, IReadOnlyDictionary<string, string?> options)
            : this(id, instructions, RequirePairs(options))
        {
        }

        /// <summary>Initializes a new instance of the <see cref="ChoiceQuestion"/> class.</summary>
        /// <param name="id">The identifier that matches the question with its answer inside a batch.</param>
        /// <param name="instructions">The question the model answers.</param>
        /// <param name="options">The named options with optional descriptions. Use <see langword="null"/> when an option needs no extra detail.</param>
        public ChoiceQuestion(string id, string instructions, IEnumerable<KeyValuePair<string, string?>> options)
        {
            Id = Guard.Id(id);
            Instructions = Guard.Instructions(instructions);
            Guard.NotNull(options, nameof(options), "Choice options must not be null.");

            var names = new List<string>();
            var descriptions = new List<string?>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var option in options)
            {
                if (option.Key == null)
                {
                    throw new JevValidationException(
                        $"Choice question '{Id}' contains a null option name at index {names.Count}.");
                }

                if (string.IsNullOrWhiteSpace(option.Key))
                {
                    throw new JevValidationException(
                        $"Choice question '{Id}' contains an empty option name at index {names.Count}.");
                }

                if (!seen.Add(option.Key))
                {
                    throw new JevValidationException(
                        $"Choice question '{Id}' contains the duplicate option name '{option.Key}'.");
                }

                names.Add(option.Key);
                descriptions.Add(option.Value);
            }

            if (names.Count == 0)
            {
                throw new JevValidationException(
                    $"Choice question '{Id}' must define at least one option.");
            }

            if (names.Count > ChoiceQuestion<string>.MaxOptionCount)
            {
                throw new JevValidationException(
                    $"Choice question '{Id}' defines {names.Count} options; the limit is {ChoiceQuestion<string>.MaxOptionCount}.");
            }

            _names = names.ToArray();
            _descriptions = descriptions.ToArray();
        }

        /// <inheritdoc />
        public string Id { get; }

        /// <inheritdoc />
        public string Instructions { get; }

        /// <summary>Gets the option names, in the order they were supplied.</summary>
        public IReadOnlyList<string> Options => _names;

        string IJevQuestion.QuestionType => "choice";

        void IJevQuestion.WriteQuestion(Utf8JsonWriter writer)
        {
            writer.WriteString("type", "choice");
            writer.WriteString("instructions", Instructions);
            writer.WriteStartObject("criteria");
            for (var index = 0; index < _names.Length; index++)
            {
                writer.WritePropertyName(_names[index]);
                if (_descriptions[index] == null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    writer.WriteStringValue(_descriptions[index]);
                }
            }

            writer.WriteEndObject();
        }

        object IJevQuestion.ReadAnswer(JsonElement answer)
        {
            var answerObject = AnswerReader.RequireObject(answer, Id);
            AnswerReader.RequireType(answerObject, "choice", Id);

            var choice = AnswerReader.RequireString(answerObject, "choice", Id);
            var confidence = AnswerReader.RequireUnitInterval(
                AnswerReader.RequireNumber(answerObject, "confidence", Id),
                "The 'confidence' value",
                Id);

            var probabilities = AnswerReader.RequireObject(
                AnswerReader.RequireProperty(answerObject, "probabilities", Id),
                Id);

            var probabilitiesByName = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (var property in probabilities.EnumerateObject())
            {
                if (probabilitiesByName.ContainsKey(property.Name))
                {
                    throw new JevProtocolException(
                        $"The answer for question '{Id}' contains duplicate probabilities for the option '{property.Name}'.");
                }

                if (property.Value.ValueKind != JsonValueKind.Number)
                {
                    throw new JevProtocolException(
                        $"The probability for the option '{property.Name}' of question '{Id}' must be a number.");
                }

                probabilitiesByName.Add(
                    property.Name,
                    AnswerReader.RequireUnitInterval(
                        property.Value.GetDouble(),
                        $"The probability for the option '{property.Name}'",
                        Id));
            }

            var ordered = new List<ChoiceProbability<string>>(_names.Length);
            foreach (var name in _names)
            {
                if (!probabilitiesByName.TryGetValue(name, out var probability))
                {
                    throw new JevProtocolException(
                        $"The answer for question '{Id}' is missing the probability for the option '{name}'.");
                }

                ordered.Add(new ChoiceProbability<string>(name, probability));
            }

            foreach (var name in probabilitiesByName.Keys)
            {
                if (Array.IndexOf(_names, name) < 0)
                {
                    throw new JevProtocolException(
                        $"The answer for question '{Id}' contains the unknown option '{name}'.");
                }
            }

            if (Array.IndexOf(_names, choice) < 0)
            {
                throw new JevProtocolException(
                    $"The answer for question '{Id}' selected the unknown option '{choice}'.");
            }

            return new ChoiceAnswer<string>(choice, confidence, ordered);
        }

        private static IEnumerable<KeyValuePair<string, string?>> RequirePairs(IReadOnlyDictionary<string, string?> options)
        {
            Guard.NotNull(options, nameof(options), "Choice options must not be null.");
            return options;
        }
    }
}
