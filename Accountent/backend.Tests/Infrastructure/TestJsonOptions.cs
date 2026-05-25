using System.Text.Json;
using System.Text.Json.Serialization;

namespace backend.Tests.Infrastructure;

public static class TestJsonOptions
{
    public static JsonSerializerOptions Default { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };
}
