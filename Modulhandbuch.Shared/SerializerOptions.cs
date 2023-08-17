using System.Text.Json;
using System.Text.Json.Serialization;

namespace Modulhandbuch.Shared; 

public static class SerializerOptions {
    public static readonly JsonSerializerOptions Default = new() {
        Converters = {
            new JsonStringEnumConverter(),
        },
        WriteIndented = true,
    };
}
