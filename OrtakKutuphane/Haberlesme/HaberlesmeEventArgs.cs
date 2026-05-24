namespace OrtakKutuphane.Haberlesme;

// Paket yakalama başarılı olduğunda fırlatılan event'in argümanı.
public sealed class PaketYakalandiEventArgs : EventArgs
{
    public YakalananPaket Paket { get; }

    public PaketYakalandiEventArgs(YakalananPaket paket)
    {
        Paket = paket ?? throw new ArgumentNullException(nameof(paket));
    }
}

// Hatalı paket tespit edildiğinde fırlatılan event'in argümanı.
public sealed class HataliPaketYakalandiEventArgs : EventArgs
{
    public HataliPaketSebebi Sebep { get; }

    public string Aciklama { get; }

    public HataliPaketYakalandiEventArgs(HataliPaketSebebi sebep, string aciklama)
    {
        Sebep = sebep;
        Aciklama = aciklama ?? string.Empty;
    }
}
