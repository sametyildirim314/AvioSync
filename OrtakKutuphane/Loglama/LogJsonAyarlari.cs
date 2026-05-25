using System.Text.Json;
using System.Text.Json.Serialization;

namespace OrtakKutuphane.Loglama;

// Loglama ve oynatma boyunca aynı JSON ayarlarının kullanılmasını sağlar (DRY).
// Struct (HaberlesmePaket1/2) alanlarının okunabilmesi için IncludeFields aktif edilir.
internal static class LogJsonAyarlari
{
    public static JsonSerializerOptions Varsayilan { get; } = new()
    {
        IncludeFields = true,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };
}
