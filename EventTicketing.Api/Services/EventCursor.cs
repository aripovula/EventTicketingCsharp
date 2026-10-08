using System.Buffers.Text;
using System.Text.Json;

namespace EventTicketing.Api.Services;

/// <summary>Opaque keyset cursor: the last item's sort key plus its id as a tiebreaker.</summary>
public static class EventCursor
{
    private record Payload(string Key, int Id);

    public static string Encode(string key, int id) =>
        Base64Url.EncodeToString(JsonSerializer.SerializeToUtf8Bytes(new Payload(key, id)));

    public static bool TryDecode(string cursor, out string key, out int id)
    {
        (key, id) = (string.Empty, 0);
        try
        {
            var payload = JsonSerializer.Deserialize<Payload>(Base64Url.DecodeFromChars(cursor));
            if (payload?.Key is null) return false;
            (key, id) = (payload.Key, payload.Id);
            return true;
        }
        catch (Exception e) when (e is FormatException or JsonException)
        {
            return false;
        }
    }
}
