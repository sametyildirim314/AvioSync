namespace OrtakKutuphane.Test;

// Bir test senaryosundaki adımın türü.
// JSON'da string olarak yazılır (örn. "TelemetriAkisiniDogrula").
public enum TestAdimTipi : byte
{
    // Aviyonik bağlantısını başlatır (BAĞLAN butonuna basar).
    BaglantiyiBaslat = 1,

    // Aviyonik bağlantısını keser (BAĞLANTIYI KES butonuna basar).
    BaglantiyiKes = 2,

    // Belirli bir süre içinde minimum doğru paket sayısının aşıldığını doğrular.
    TelemetriAkisiniDogrula = 3,

    // ComboBox'tan komut seçer ve KOMUT GÖNDER butonuna basar.
    // Beklenen sonuç: Son geri besleme alanı "KomutCevabi" olarak güncellenir.
    KomutGonder = 4,

    // ComboBox'tan ayar seçer, değer alanını doldurur ve AYAR GÖNDER butonuna basar.
    // Beklenen sonuç: Geri besleme paketi ilgili ayar tipiyle gelir.
    AyarGonder = 5,

    // Belirli süre bekler (sleep). Olay tetiklemez.
    Bekle = 6,

    // Hatalı paket sayacının 0 olduğunu doğrular.
    HataliPaketYok = 7
}
