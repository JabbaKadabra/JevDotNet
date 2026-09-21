using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using SystemOneDotNet.Exceptions;

namespace SystemOneDotNet.Internal.Json;

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
        if (value is null)
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
            throw new SystemOneValidationException(
                $"{context} contains a cyclic object graph and cannot be serialized as a JSON description.");
        }

        try
        {
            return value switch
            {
                JsonElement element => CountJsonElement(element),
                JsonNode node => CountNode(node, path, context),
                IDictionary dictionary => CountDictionary(dictionary, path, context),
                IEnumerable enumerable => CountEnumerable(enumerable, path, context),
                _ => CountMembers(type, value, path, context),
            };
        }
        finally
        {
            if (trackReference)
            {
                path.Remove(value);
            }
        }
    }

    private static int CountDictionary(IDictionary dictionary, HashSet<object> path, string context)
    {
        var count = dictionary.Count;
        foreach (DictionaryEntry entry in dictionary)
        {
            count += CountValue(entry.Value, path, context);
        }

        return count;
    }

    private static int CountEnumerable(IEnumerable enumerable, HashSet<object> path, string context)
    {
        var count = 0;
        foreach (var item in enumerable)
        {
            count += CountValue(item, path, context);
        }

        return count;
    }

    private static int CountMembers(Type type, object value, HashSet<object> path, string context)
    {
        var count = 0;
        foreach (var member in GetSerializableMembers(type))
        {
            count++;
            count += CountValue(member.GetValue(value), path, context);
        }

        return count;
    }

    private static int CountNode(JsonNode node, HashSet<object> path, string context) => node switch
    {
        JsonObject jsonObject => CountJsonObject(jsonObject, path, context),
        JsonArray jsonArray => CountJsonArray(jsonArray, path, context),
        _ => 0,
    };

    private static int CountJsonObject(JsonObject jsonObject, HashSet<object> path, string context)
    {
        var count = jsonObject.Count;
        foreach (var property in jsonObject)
        {
            if (property.Value is not null)
            {
                count += CountValue(property.Value, path, context);
            }
        }

        return count;
    }

    private static int CountJsonArray(JsonArray jsonArray, HashSet<object> path, string context)
    {
        var count = 0;
        foreach (var item in jsonArray)
        {
            if (item is not null)
            {
                count += CountValue(item, path, context);
            }
        }

        return count;
    }

    private static int CountJsonElement(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => CountJsonObject(element),
        JsonValueKind.Array => CountJsonArray(element),
        _ => 0,
    };

    private static int CountJsonObject(JsonElement element)
    {
        var count = 0;
        foreach (var property in element.EnumerateObject())
        {
            count++;
            if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
            {
                count += CountJsonElement(property.Value);
            }
        }

        return count;
    }

    private static int CountJsonArray(JsonElement element)
    {
        var count = 0;
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
            {
                count += CountJsonElement(item);
            }
        }

        return count;
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
            if (getter is null || (!getter.IsPublic && !included))
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

        return type == typeof(JsonDocument);
    }

    private readonly struct SerializableMember
    {
        private readonly PropertyInfo? propertyInfo;
        private readonly FieldInfo? fieldInfo;

        public SerializableMember(PropertyInfo property)
        {
            propertyInfo = property;
            fieldInfo = null;
        }

        public SerializableMember(FieldInfo field)
        {
            propertyInfo = null;
            fieldInfo = field;
        }

        public object? GetValue(object instance) =>
            propertyInfo is not null
                ? propertyInfo.GetValue(instance, null)
                : fieldInfo?.GetValue(instance);
    }
}
