using System.Buffers.Text;
using System.Text.Json;

namespace EventTicketing.Api.Services;

/// <summary>Opaque keyset cursor: the last item's sort key plus its id as a tiebreaker.</summary>
public static class EventCursor
{
    private record Payload(string Key, int Id);

    public static string Encode(string key, int id) =>
        Base64Url.EncodeToString(JsonSerializer.SerializeToUtf8Bytes(new Payload(key, id)));

    public static (string Key, int Id) Decode(string cursor)
    {
        var payload = JsonSerializer.Deserialize<Payload>(Base64Url.DecodeFromChars(cursor))!;
        return (payload.Key, payload.Id);
    }
}
