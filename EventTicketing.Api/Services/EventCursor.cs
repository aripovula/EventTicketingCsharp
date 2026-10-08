using System.Buffers.Text;
using System.Text.Json;

namespace EventTicketing.Api.Services;

/// <summary>Opaque keyset cursor: the sort it belongs to, the last item's sort key, and its id as a tiebreaker.</summary>
public static class EventCursor
{
    private record Payload(string Sort, string Key, int Id);

    public static string Encode(string sort, string key, int id) =>
        Base64Url.EncodeToString(JsonSerializer.SerializeToUtf8Bytes(new Payload(sort, key, id)));

    public static bool TryDecode(string cursor, string sort, out string key, out int id)
    {
        (key, id) = (string.Empty, 0);
        try
        {
            var payload = JsonSerializer.Deserialize<Payload>(Base64Url.DecodeFromChars(cursor));
            if (payload?.Key is null || payload.Sort != sort) return false;
            (key, id) = (payload.Key, payload.Id);
            return true;
        }
        catch (Exception e) when (e is FormatException or JsonException)
        {
            return false;
        }
    }
}
