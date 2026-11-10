using System.Text.Encodings.Web;

namespace FreeBook.JavaScript;

/// <summary>
/// Encodes strings for safe use inside JavaScript string literals.
/// </summary>
public static class Js
{
    private static readonly JavaScriptEncoder Encoder = JavaScriptEncoder.Default;

    /// <summary>
    /// Encodes a value so it can be safely embedded in a JavaScript string literal.
    /// Returns an empty string if <paramref name="value"/> is null.
    /// </summary>
    public static string Encode(string? value)
    {
        return Encoder.Encode(value ?? string.Empty);
    }
}
