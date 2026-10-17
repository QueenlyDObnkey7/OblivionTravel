using System.Text.Json;
using System.Text.Json.Serialization;

namespace OblivionTravel;

public enum MapTravel { Block, Allow, Unmanaged }

public sealed record TravelSettings
{
    public bool Enabled { get; init; } = true;
    public MapTravel MapFastTravel { get; init; } = MapTravel.Block;
    public string Discovery { get; init; } = "PreserveNative";
    public double ReapplySeconds { get; init; } = 60;
    public double ReadyDelaySeconds { get; init; } = 2;

    public void Validate()
    {
        if (!Enum.IsDefined(MapFastTravel)) throw new InvalidDataException("Unknown MapFastTravel policy.");
        if (Discovery != "PreserveNative")
            throw new InvalidDataException("Discovery must be PreserveNative. No supported default-save discovery replay API has been verified.");
        if (!double.IsFinite(ReapplySeconds) || ReapplySeconds < 1 || ReapplySeconds > 3600)
            throw new InvalidDataException("ReapplySeconds must be between 1 and 3600.");
        if (!double.IsFinite(ReadyDelaySeconds) || ReadyDelaySeconds < 0 || ReadyDelaySeconds > 30)
            throw new InvalidDataException("ReadyDelaySeconds must be between 0 and 30.");
    }

    public static TravelSettings Parse(string json)
    {
        var settings = JsonSerializer.Deserialize<TravelSettings>(json, new JsonSerializerOptions
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            Converters = { new JsonStringEnumConverter<MapTravel>(allowIntegerValues: false) }
        }) ?? throw new InvalidDataException("Config cannot be null.");
        settings.Validate();
        return settings;
    }
}
