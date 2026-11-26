using System.Globalization;
using System.Text.Json;

namespace ReconHub.Domain.Detection;

public sealed record FieldDifference(string Field, string TriggerValue, string FollowUpValue);

/// <summary>
/// Compares fields of two JSON payloads by dot-separated path (e.g. "quantity" or "line.qty").
/// <list type="bullet">
/// <item>Property names match exactly first, then case-insensitively.</item>
/// <item>Numbers compare by value (10 equals 10.0); a JSON number also equals a numeric string ("10").</item>
/// <item>Two strings compare ordinally, so "007" and "7" differ.</item>
/// <item>A field missing on either side, or an unparseable payload, is a difference.</item>
/// </list>
/// </summary>
public static class PayloadFieldComparer
{
    public const string MissingValue = "<missing>";
    private const int MaxDescribedLength = 100;

    public static IReadOnlyList<FieldDifference> Compare(string triggerPayload, string followUpPayload, IEnumerable<string> fieldPaths)
    {
        ArgumentNullException.ThrowIfNull(fieldPaths);

        using var trigger = TryParse(triggerPayload);
        using var followUp = TryParse(followUpPayload);

        var differences = new List<FieldDifference>();
        foreach (var path in fieldPaths)
        {
            var left = Resolve(trigger, path);
            var right = Resolve(followUp, path);
            if (!ValuesEqual(left, right))
            {
                differences.Add(new FieldDifference(path, Describe(left), Describe(right)));
            }
        }

        return differences;
    }

    private static JsonDocument? TryParse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static JsonElement? Resolve(JsonDocument? document, string path)
    {
        if (document is null)
        {
            return null;
        }

        var current = document.RootElement;
        foreach (var segment in path.Split('.'))
        {
            if (current.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (current.TryGetProperty(segment, out var exact))
            {
                current = exact;
                continue;
            }

            JsonElement? match = null;
            foreach (var property in current.EnumerateObject())
            {
                if (string.Equals(property.Name, segment, StringComparison.OrdinalIgnoreCase))
                {
                    match = property.Value;
                    break;
                }
            }

            if (match is null)
            {
                return null;
            }

            current = match.Value;
        }

        return current;
    }

    private static bool ValuesEqual(JsonElement? left, JsonElement? right)
    {
        if (left is not { } l || right is not { } r)
        {
            return false;
        }

        if (l.ValueKind == JsonValueKind.String && r.ValueKind == JsonValueKind.String)
        {
            return string.Equals(l.GetString(), r.GetString(), StringComparison.Ordinal);
        }

        if ((l.ValueKind == JsonValueKind.Number || r.ValueKind == JsonValueKind.Number)
            && TryGetNumber(l, out var leftNumber)
            && TryGetNumber(r, out var rightNumber))
        {
            return leftNumber == rightNumber;
        }

        return JsonElement.DeepEquals(l, r);
    }

    private static bool TryGetNumber(JsonElement element, out decimal value)
    {
        value = 0;
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetDecimal(out value),
            JsonValueKind.String => decimal.TryParse(element.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value),
            _ => false,
        };
    }

    private static string Describe(JsonElement? element)
    {
        if (element is not { } e)
        {
            return MissingValue;
        }

        var raw = e.GetRawText();
        return raw.Length <= MaxDescribedLength ? raw : raw[..MaxDescribedLength] + "...";
    }
}
