using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NuGetFeed.Serialization;

/// <summary>
/// Source generated JSON serialization for the types the server reads and writes outside of MVC.
/// </summary>
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(JsonElement))]
internal partial class ServerJsonContext : JsonSerializerContext
{
    /// <summary>
    /// Serialization for values stored in the database. This only escapes what JSON requires,
    /// so stored values read the same as they did when the server used Json.NET.
    /// </summary>
    public static ServerJsonContext Database { get; } = new(new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    });
}
