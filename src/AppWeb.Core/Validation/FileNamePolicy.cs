using System.Text;
using System.Text.RegularExpressions;

namespace AppWeb.Core.Validation;

public static partial class FileNamePolicy
{
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    public static string Sanitize(string? value, string fallback = "Web App", int maxLength = 60)
    {
        if (maxLength < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLength));
        }

        var source = (value ?? string.Empty).Normalize(NormalizationForm.FormC);
        var invalidChars = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(source.Length);

        foreach (var character in source)
        {
            builder.Append(invalidChars.Contains(character) || char.IsControl(character) ? ' ' : character);
        }

        var result = WhitespaceRegex().Replace(builder.ToString(), " ").Trim(' ', '.');
        if (result.Length > maxLength)
        {
            result = result[..maxLength].TrimEnd(' ', '.');
        }

        if (ReservedNames.Contains(result))
        {
            result += " App";
        }

        return string.IsNullOrWhiteSpace(result) ? fallback : result;
    }

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();
}
