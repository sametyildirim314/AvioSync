namespace OrtakKutuphane.Loglama;

// Oynatma sırasında her bir paket için tetiklenen event'in argümanı.
public sealed class LogOynatildiEventArgs : EventArgs
{
    // Oynatılan kayıt.
    public LogKaydi Kayit { get; }

    // Dosyadaki kaçıncı kayıt (1 tabanlı).
    public int Indeks { get; }

    // Dosyadaki toplam kayıt sayısı.
    public int Toplam { get; }

    public LogOynatildiEventArgs(LogKaydi kayit, int indeks, int toplam)
    {
        Kayit = kayit;
        Indeks = indeks;
        Toplam = toplam;
    }
}
