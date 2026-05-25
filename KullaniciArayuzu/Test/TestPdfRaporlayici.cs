using OrtakKutuphane.Test;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using PdfSharp.Snippets.Font;
using System.IO;

namespace KullaniciArayuzu.Test;

// TestRaporu nesnesini PDF formatında diskere yazan sınıf.
// PdfSharp 6.x'in WPF altında çalışabilmesi için sistem fontları FailsafeFontResolver
// üzerinden çözümlenir (statik kurucuda bir kere yapılır).
public static class TestPdfRaporlayici
{
    public const string VarsayilanKlasorAdi = "Test";

    // Renkler ve fontlar (DRY).
    private static readonly XColor RenkBaslik       = XColor.FromArgb(0x14, 0x58, 0xB3);
    private static readonly XColor RenkBasarili     = XColor.FromArgb(0x22, 0xA1, 0x4E);
    private static readonly XColor RenkBasarisiz    = XColor.FromArgb(0xCC, 0x33, 0x33);
    private static readonly XColor RenkAcikGri      = XColor.FromArgb(0x6B, 0x72, 0x80);
    private static readonly XColor RenkSatirArka    = XColor.FromArgb(0xF1, 0xF5, 0xF9);

    // PdfSharp 6.x: Font çözümlemesi için ihtiyaç duyulan FontResolver yalnızca bir kez set edilir.
    static TestPdfRaporlayici()
    {
        if (GlobalFontSettings.FontResolver is null)
        {
            GlobalFontSettings.FontResolver = new FailsafeFontResolver();
        }
    }

    // PDF dosyasını oluşturur ve oluşturulan dosya yolunu döner.
    public static string PdfUret(TestRaporu rapor, string hedefKlasor)
    {
        if (rapor is null) throw new ArgumentNullException(nameof(rapor));
        Directory.CreateDirectory(hedefKlasor);

        string ad = $"TestRaporu_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        string yol = Path.Combine(hedefKlasor, ad);

        using var doc = new PdfDocument();
        doc.Info.Title = rapor.TestAdi;
        doc.Info.Author = "AvioSync";
        doc.Info.Subject = "Otonom UI Test Raporu";
        doc.Info.CreationDate = DateTime.Now;

        var fontBaslik    = new XFont("Arial", 18, XFontStyleEx.Bold);
        var fontAltBaslik = new XFont("Arial", 12, XFontStyleEx.Bold);
        var fontGovde     = new XFont("Arial", 10, XFontStyleEx.Regular);
        var fontKucuk     = new XFont("Arial", 9, XFontStyleEx.Regular);

        // Sayfa kurulum
        var sayfa = doc.AddPage();
        sayfa.Size = PdfSharp.PageSize.A4;
        var gfx = XGraphics.FromPdfPage(sayfa);
        var tf = new XTextFormatter(gfx);

        const double marginX = 40;
        double y = 40;
        double sayfaGenisligi = sayfa.Width.Point;
        double icerikGenisligi = sayfaGenisligi - 2 * marginX;

        // BAŞLIK
        gfx.DrawString("AVIOSYNC", new XFont("Arial", 22, XFontStyleEx.Bold),
            new XSolidBrush(RenkBaslik), new XPoint(marginX, y));
        y += 26;
        gfx.DrawString("OTONOM TEST RAPORU", fontAltBaslik,
            new XSolidBrush(RenkAcikGri), new XPoint(marginX, y));
        y += 14;
        gfx.DrawLine(new XPen(RenkBaslik, 1.4),
            new XPoint(marginX, y + 4),
            new XPoint(marginX + icerikGenisligi, y + 4));
        y += 18;

        // META BİLGİ
        gfx.DrawString($"Test Adı: {rapor.TestAdi}", fontAltBaslik,
            XBrushes.Black, new XPoint(marginX, y));
        y += 18;

        if (!string.IsNullOrWhiteSpace(rapor.Aciklama))
        {
            var rect = new XRect(marginX, y, icerikGenisligi, 50);
            tf.DrawString(rapor.Aciklama, fontKucuk,
                new XSolidBrush(RenkAcikGri), rect, XStringFormats.TopLeft);
            y += 44;
        }

        gfx.DrawString($"Başlangıç: {rapor.BaslangicZamani.ToLocalTime():dd.MM.yyyy HH:mm:ss}", fontGovde,
            XBrushes.Black, new XPoint(marginX, y));
        y += 14;
        gfx.DrawString($"Bitiş: {rapor.BitisZamani.ToLocalTime():dd.MM.yyyy HH:mm:ss}", fontGovde,
            XBrushes.Black, new XPoint(marginX, y));
        y += 14;
        gfx.DrawString($"Toplam Süre: {rapor.ToplamSure.TotalSeconds:F2} sn", fontGovde,
            XBrushes.Black, new XPoint(marginX, y));
        y += 22;

        // ÖZET ROZETLERİ
        OzetKutusuCiz(gfx, marginX, y, 100, "TOPLAM", rapor.ToplamAdim.ToString(), RenkAcikGri);
        OzetKutusuCiz(gfx, marginX + 110, y, 100, "BAŞARILI", rapor.BasariliAdimSayisi.ToString(), RenkBasarili);
        OzetKutusuCiz(gfx, marginX + 220, y, 100, "BAŞARISIZ", rapor.BasarisizAdimSayisi.ToString(), RenkBasarisiz);
        y += 60;

        // NİHAİ SONUÇ
        string nihai = rapor.NihaiSonuc ? "NİHAİ SONUÇ: BAŞARILI" : "NİHAİ SONUÇ: BAŞARISIZ";
        XColor nihaiRenk = rapor.NihaiSonuc ? RenkBasarili : RenkBasarisiz;

        gfx.DrawRectangle(new XSolidBrush(nihaiRenk),
            new XRect(marginX, y, icerikGenisligi, 28));
        gfx.DrawString(nihai, new XFont("Arial", 14, XFontStyleEx.Bold),
            XBrushes.White, new XRect(marginX + 12, y + 6, icerikGenisligi, 24),
            XStringFormats.TopLeft);
        y += 40;

        // ADIM BAŞLIĞI
        gfx.DrawString("TEST ADIMLARI", fontAltBaslik,
            new XSolidBrush(RenkBaslik), new XPoint(marginX, y));
        y += 6;
        gfx.DrawLine(new XPen(RenkAcikGri, 0.5),
            new XPoint(marginX, y + 8), new XPoint(marginX + icerikGenisligi, y + 8));
        y += 18;

        // ADIM TABLOSU
        foreach (var sonuc in rapor.Sonuclar)
        {
            // Yeni sayfa kontrolü
            if (y > sayfa.Height.Point - 90)
            {
                sayfa = doc.AddPage();
                sayfa.Size = PdfSharp.PageSize.A4;
                gfx = XGraphics.FromPdfPage(sayfa);
                tf = new XTextFormatter(gfx);
                y = 40;
            }

            // Satır arka planı (alternatif)
            if (sonuc.SiraNo % 2 == 0)
            {
                gfx.DrawRectangle(new XSolidBrush(RenkSatirArka),
                    new XRect(marginX - 4, y - 4, icerikGenisligi + 8, 50));
            }

            // Durum rozeti
            string durumYazi = sonuc.Basarili ? "BAŞARILI" : "BAŞARISIZ";
            XColor durumRenk = sonuc.Basarili ? RenkBasarili : RenkBasarisiz;

            gfx.DrawRectangle(new XSolidBrush(durumRenk),
                new XRect(marginX, y, 70, 18));
            gfx.DrawString(durumYazi, new XFont("Arial", 9, XFontStyleEx.Bold),
                XBrushes.White, new XRect(marginX, y + 2, 70, 18), XStringFormats.TopCenter);

            // Adım başlık
            gfx.DrawString($"#{sonuc.SiraNo}  {sonuc.AdimOzeti}", fontGovde,
                XBrushes.Black, new XPoint(marginX + 78, y + 4));

            // Süre
            gfx.DrawString($"{sonuc.Sure.TotalSeconds:F2} sn", fontKucuk,
                new XSolidBrush(RenkAcikGri),
                new XRect(marginX, y + 4, icerikGenisligi, 14),
                XStringFormats.TopRight);

            // Mesaj
            var rectMsj = new XRect(marginX + 78, y + 22, icerikGenisligi - 78, 30);
            tf.DrawString($"Mesaj: {sonuc.Mesaj}", fontKucuk,
                new XSolidBrush(RenkAcikGri), rectMsj, XStringFormats.TopLeft);

            y += 50;
        }

        // ALT BİLGİ
        if (y > sayfa.Height.Point - 40)
        {
            sayfa = doc.AddPage();
            gfx = XGraphics.FromPdfPage(sayfa);
            y = 40;
        }

        y = sayfa.Height.Point - 30;
        gfx.DrawLine(new XPen(RenkAcikGri, 0.4),
            new XPoint(marginX, y - 6), new XPoint(marginX + icerikGenisligi, y - 6));
        gfx.DrawString("Rapor PdfSharp ile otomatik üretildi - AvioSync Test Motoru",
            fontKucuk, new XSolidBrush(RenkAcikGri),
            new XRect(marginX, y, icerikGenisligi, 14), XStringFormats.TopLeft);
        gfx.DrawString(DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
            fontKucuk, new XSolidBrush(RenkAcikGri),
            new XRect(marginX, y, icerikGenisligi, 14), XStringFormats.TopRight);

        doc.Save(yol);
        return yol;
    }

    // Üst kısımdaki özet rozetlerini çizer.
    private static void OzetKutusuCiz(XGraphics gfx, double x, double y, double genislik,
        string etiket, string deger, XColor renk)
    {
        gfx.DrawRectangle(new XPen(renk, 1), new XSolidBrush(XColor.FromArgb(245, 245, 250)),
            new XRect(x, y, genislik, 50));

        gfx.DrawString(etiket, new XFont("Arial", 9, XFontStyleEx.Bold),
            new XSolidBrush(RenkAcikGri),
            new XRect(x, y + 6, genislik, 14), XStringFormats.TopCenter);

        gfx.DrawString(deger, new XFont("Arial", 18, XFontStyleEx.Bold),
            new XSolidBrush(renk),
            new XRect(x, y + 20, genislik, 28), XStringFormats.TopCenter);
    }
}
