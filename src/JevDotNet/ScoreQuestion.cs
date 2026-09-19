using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using JevDotNet.Internal;

namespace JevDotNet
{
    /// <summary>A score question that rates the state along an ordered list of level descriptions.</summary>
    public class ScoreQuestion : IQuestion<ScoreAnswer>, IJevQuestion
    {
        private readonly string[] _levels;

        /// <summary>Initializes a new instance of the <see cref="ScoreQuestion"/> class.</summary>
        /// <param name="id">The identifier that matches the question with its answer inside a batch.</param>
        /// <param name="instructions">The question the model answers.</param>
        /// <param name="levels">The ordered level descriptions. At least two levels are required.</param>
        public ScoreQuestion(string id, string instructions, IEnumerable<string> levels)
        {
            Id = Guard.Id(id);
            Instructions = Guard.Instructions(instructions);
            Guard.NotNull(levels, nameof(levels), "Score levels must not be null.");

            _levels = levels.ToArray();
            if (_levels.Length < 2)
            {
                throw new JevValidationException(
                    $"Score question '{Id}' must define at least two levels but defines {_levels.Length}.");
            }

            for (var index = 0; index < _levels.Length; index++)
            {
                if (_levels[index] == null)
                {
                    throw new JevValidationException(
                        $"Score question '{Id}' contains a null level at index {index}.");
                }

                if (string.IsNullOrWhiteSpace(_levels[index]))
                {
                    throw new JevValidationException(
                        $"Score question '{Id}' contains an empty level at index {index}.");
                }
            }
        }

        /// <inheritdoc />
        public string Id { get; }

        /// <inheritdoc />
        public string Instructions { get; }

        /// <summary>Gets the level descriptions, in the order they were supplied.</summary>
        public IReadOnlyList<string> Levels => _levels;

        string IJevQuestion.QuestionType => "score";

        void IJevQuestion.WriteQuestion(Utf8JsonWriter writer)
        {
            writer.WriteString("type", "score");
            writer.WriteString("instructions", Instructions);
            writer.WriteStartArray("criteria");
            foreach (var level in _levels)
            {
                writer.WriteStringValue(level);
            }

            writer.WriteEndArray();
        }

        object IJevQuestion.ReadAnswer(JsonElement answer)
        {
            var answerObject = AnswerReader.RequireObject(answer, Id);
            AnswerReader.RequireType(answerObject, "score", Id);

            var score = AnswerReader.RequireNumber(answerObject, "score", Id);
            AnswerReader.RequireRange(
                score,
                0,
                _levels.Length - 1,
                "The 'score' value",
                Id);
            var confidence = AnswerReader.RequireUnitInterval(
                AnswerReader.RequireNumber(answerObject, "confidence", Id),
                "The 'confidence' value",
                Id);

            var legend = AnswerReader.RequireObject(
                AnswerReader.RequireProperty(answerObject, "legend", Id),
                Id);

            var legendByIndex = new string?[_levels.Length];
            var legendFound = new bool[_levels.Length];
            foreach (var property in legend.EnumerateObject())
            {
                var index = AnswerReader.ParseOptionIndex(property.Name, _levels.Length, Id);
                if (legendFound[index])
                {
                    throw new JevProtocolException(
                        $"The answer for question '{Id}' contains duplicate legend entries for level id '{property.Name}'.");
                }

                if (property.Value.ValueKind != JsonValueKind.String)
                {
                    throw new JevProtocolException(
                        $"The legend entry '{property.Name}' of question '{Id}' must be a string.");
                }

                legendByIndex[index] = property.Value.GetString();
                legendFound[index] = true;
            }

            for (var index = 0; index < _levels.Length; index++)
            {
                if (!legendFound[index])
                {
                    throw new JevProtocolException(
                        $"The answer for question '{Id}' is missing the legend entry for level id '{index.ToString(CultureInfo.InvariantCulture)}'.");
                }
            }

            List<ScoreProbability>? probabilities = null;
            if (answerObject.TryGetProperty("probabilities", out var probabilitiesElement) &&
                probabilitiesElement.ValueKind != JsonValueKind.Null)
            {
                if (probabilitiesElement.ValueKind != JsonValueKind.Object)
                {
                    throw new JevProtocolException(
                        $"The 'probabilities' property of the answer for question '{Id}' must be an object.");
                }

                var probabilityByIndex = new double[_levels.Length];
                var probabilityFound = new bool[_levels.Length];
                foreach (var property in probabilitiesElement.EnumerateObject())
                {
                    var index = AnswerReader.ParseOptionIndex(property.Name, _levels.Length, Id);
                    if (probabilityFound[index])
                    {
                        throw new JevProtocolException(
                            $"The answer for question '{Id}' contains duplicate probabilities for level id '{property.Name}'.");
                    }

                    if (property.Value.ValueKind != JsonValueKind.Number)
                    {
                        throw new JevProtocolException(
                            $"The probability for level id '{property.Name}' of question '{Id}' must be a number.");
                    }

                    probabilityByIndex[index] = AnswerReader.RequireUnitInterval(
                        property.Value.GetDouble(),
                        $"The probability for level id '{property.Name}'",
                        Id);
                    probabilityFound[index] = true;
                }

                probabilities = new List<ScoreProbability>(_levels.Length);
                for (var index = 0; index < _levels.Length; index++)
                {
                    if (!probabilityFound[index])
                    {
                        throw new JevProtocolException(
                            $"The answer for question '{Id}' is missing the probability for level id '{index.ToString(CultureInfo.InvariantCulture)}'.");
                    }

                    probabilities.Add(new ScoreProbability(index, legendByIndex[index]!, probabilityByIndex[index]));
                }
            }

            return new ScoreAnswer(score, legendByIndex!, confidence, probabilities);
        }
    }
}
