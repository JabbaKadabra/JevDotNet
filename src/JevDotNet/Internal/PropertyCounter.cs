using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace JevDotNet.Internal
{
    /// <summary>
    /// Counts the properties in the serialized object tree of a choice option. Nested properties, dictionary
    /// entries, and properties inside array elements are counted. Members marked with
    /// <see cref="JsonIgnoreAttribute"/> are not counted. This is a property limit, not a text-length limit.
    /// </summary>
    internal static class PropertyCounter
    {
        /// <summary>Counts the serialized properties in <paramref name="value"/>'s object tree.</summary>
        /// <param name="value">The option value to inspect.</param>
        /// <param name="context">A description of the option used in error messages.</param>
        /// <returns>The number of serialized properties.</returns>
        public static int Count(object? value, string context)
        {
            var path = new HashSet<object>(ReferenceComparer.Instance);
            return CountValue(value, path, context);
        }

        private static int CountValue(object? value, HashSet<object> path, string context)
        {
            if (value == null)
            {
                return 0;
            }

            var type = value.GetType();
            if (IsScalar(type))
            {
                return 0;
            }

            var trackReference = !type.IsValueType;
            if (trackReference && !path.Add(value))
            {
                throw new JevValidationException(
                    $"{context} contains a cyclic object graph and cannot be serialized as a JSON description.");
            }

            try
            {
                if (value is JsonNode node)
                {
                    return CountNode(node, path, context);
                }

                if (value is IDictionary dictionary)
                {
                    var dictionaryCount = dictionary.Count;
                    foreach (DictionaryEntry entry in dictionary)
                    {
                        dictionaryCount += CountValue(entry.Value, path, context);
                    }

                    return dictionaryCount;
                }

                if (value is IEnumerable enumerable)
                {
                    var enumerableCount = 0;
                    foreach (var item in enumerable)
                    {
                        enumerableCount += CountValue(item, path, context);
                    }

                    return enumerableCount;
                }

                var count = 0;
                foreach (var member in GetSerializableMembers(type))
                {
                    count++;
                    count += CountValue(member.GetValue(value), path, context);
                }

                return count;
            }
            finally
            {
                if (trackReference)
                {
                    path.Remove(value);
                }
            }
        }

        private static int CountNode(JsonNode node, HashSet<object> path, string context)
        {
            switch (node)
            {
                case JsonObject jsonObject:
                    var objectCount = jsonObject.Count;
                    foreach (var property in jsonObject)
                    {
                        if (property.Value != null)
                        {
                            objectCount += CountValue(property.Value, path, context);
                        }
                    }

                    return objectCount;
                case JsonArray jsonArray:
                    var arrayCount = 0;
                    foreach (var item in jsonArray)
                    {
                        if (item != null)
                        {
                            arrayCount += CountValue(item, path, context);
                        }
                    }

                    return arrayCount;
                default:
                    return 0;
            }
        }

        private static IEnumerable<SerializableMember> GetSerializableMembers(Type type)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (var property in type.GetProperties(flags))
            {
                if (property.GetIndexParameters().Length != 0)
                {
                    continue;
                }

                var getter = property.GetMethod;
                var included = property.IsDefined(typeof(JsonIncludeAttribute), inherit: true);
                if (getter == null || (!getter.IsPublic && !included))
                {
                    continue;
                }

                if (property.IsDefined(typeof(JsonIgnoreAttribute), inherit: true))
                {
                    continue;
                }

                yield return new SerializableMember(property);
            }

            foreach (var field in type.GetFields(flags))
            {
                if (field.IsDefined(typeof(JsonIgnoreAttribute), inherit: true))
                {
                    continue;
                }

                if (!field.IsDefined(typeof(JsonIncludeAttribute), inherit: true))
                {
                    continue;
                }

                yield return new SerializableMember(field);
            }
        }

        private static bool IsScalar(Type type)
        {
            if (type == typeof(string) || type.IsPrimitive || type.IsEnum || type == typeof(decimal))
            {
                return true;
            }

            if (type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan) ||
                type == typeof(Guid) || type == typeof(Uri) || type == typeof(byte[]))
            {
                return true;
            }

            return type == typeof(JsonElement) || type == typeof(JsonDocument);
        }

        private readonly struct SerializableMember
        {
            private readonly PropertyInfo? _property;
            private readonly FieldInfo? _field;

            public SerializableMember(PropertyInfo property)
            {
                _property = property;
                _field = null;
            }

            public SerializableMember(FieldInfo field)
            {
                _property = null;
                _field = field;
            }

            public object? GetValue(object instance)
            {
                return _property != null ? _property.GetValue(instance, null) : _field!.GetValue(instance);
            }
        }
    }
}
