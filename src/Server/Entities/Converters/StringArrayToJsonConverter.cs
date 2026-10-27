using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NuGetFeed.Serialization;

namespace NuGetFeed.Entities.Converters;

public class StringArrayToJsonConverter : ValueConverter<string[], string>
{
    public static readonly StringArrayToJsonConverter Instance = new StringArrayToJsonConverter();

    public StringArrayToJsonConverter()
        : base(
            v => JsonSerializer.Serialize(v, ServerJsonContext.Database.StringArray),
            v => !string.IsNullOrEmpty(v) ? JsonSerializer.Deserialize(v, ServerJsonContext.Database.StringArray) : new string[0])
    {
    }
}
