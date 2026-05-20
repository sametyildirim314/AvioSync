namespace OrtakKutuphane.Enumlar;

// Kullanıcı arayüzünden aviyoniğe gönderilebilecek komut türlerini içerir.
// Komut paketi tek byte uzunluğunda olduğundan u8 ile sınırlandırılmıştır.
public enum KomutTipi : byte
{
    // Aviyonik sistemi devre dışı bırakır.
    SistemKapat = 0x10,

    // Aviyonik sistemi etkinleştirir.
    SistemAc = 0x11,

    // Aviyonik sistemini yeniden başlatır.
    SistemYenidenBaslat = 0x12,

    // Aviyoniğin acil durum modunu tetikler.
    AcilDurum = 0x13,

    // Aviyoniğin telemetri akışını duraklatır.
    TelemetriDurdur = 0x14,

    // Aviyoniğin telemetri akışını yeniden başlatır.
    TelemetriBaslat = 0x15
}
