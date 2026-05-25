using OrtakKutuphane.Enumlar;
using OrtakKutuphane.Modeller;
using System.IO;
using System.Text;

namespace OrtakKutuphane.Loglama;

// JSONL log dosyasını MATLAB Level 4 binary MAT formatına dönüştürür.
//
// MAT v4 yapısı (her matris için):
//   Header (20 byte):
//     type (int32 LE)  : M*1000 + O*100 + P*10 + T
//                        M=0 (Little Endian/Intel)
//                        O=0 (rezerve)
//                        P=0 (double precision)
//                        T=0 (numeric) veya 1 (text)
//     mrows  (int32 LE)
//     ncols  (int32 LE)
//     imagf  (int32 LE) : 0 = sadece reel
//     namlen (int32 LE) : isim uzunluğu (null sonlandırıcı dahil)
//   Name  : ASCII, null terminator dahil
//   Data  : mrows*ncols * sizeof(double), COLUMN-MAJOR sırayla
//
// MAT v4 tercih edilmesinin sebebi; formatın sade olması ve MATLAB, Octave ile
// SciPy gibi araçların bu formatı sorunsuz okuyabilmesidir.
public static class MatlabDonusturucu
{
    public const string VarsayilanKlasorAdi = "MATLAB Dönüşümleri";

    // HaberlesmePaket1 verisi için kullanılan MATLAB başlık dizesi.
    private const string Paket1Basliklari =
        "Veri1_u8,Veri2_u8,Veri3_u8,Veri4_s16,Veri5_s16,Veri6_f32,Veri7_f32";

    // HaberlesmePaket2 verisi için kullanılan MATLAB başlık dizesi.
    private const string Paket2Basliklari =
        "Veri1_u8,Veri2_u8,Veri3_s16,Veri4_s32,SistemZamani_u32,Veri6_f64";

    // MATLAB değişken adı (Türkçe karakter taşımamalı, alt çizgi kuralına uyulmalı).
    private const string Paket1MatrisAdi = "Haberlesme_Paket_1_Verileri";
    private const string Paket1BaslikAdi = "Haberlesme_Paket_1_Veri_Basliklari";
    private const string Paket2MatrisAdi = "Haberlesme_Paket_2_Verileri";
    private const string Paket2BaslikAdi = "Haberlesme_Paket_2_Veri_Basliklari";

    // Verilen JSONL log dosyasını okuyup hedef klasörde aynı isimli .mat olarak kaydeder.
    // Dönüş: oluşturulan .mat dosyasının tam yolu.
    public static string Donustur(string logDosyaYolu, string hedefKlasor)
    {
        if (string.IsNullOrWhiteSpace(logDosyaYolu))
            throw new ArgumentException("Log dosya yolu boş olamaz.", nameof(logDosyaYolu));
        if (string.IsNullOrWhiteSpace(hedefKlasor))
            throw new ArgumentException("Hedef klasör boş olamaz.", nameof(hedefKlasor));

        Directory.CreateDirectory(hedefKlasor);

        var kayitlar = TelemetriLogOkuyucu.TumKayitlariOku(logDosyaYolu);

        var paket1Satirlar = new List<double[]>();
        var paket2Satirlar = new List<double[]>();

        foreach (var kayit in kayitlar)
        {
            switch (kayit.PaketTipi)
            {
                case UnitePaketTipleri.HaberlesmePaket1 when kayit.Paket1.HasValue:
                    paket1Satirlar.Add(Paket1Satira(kayit.Paket1.Value));
                    break;
                case UnitePaketTipleri.HaberlesmePaket2 when kayit.Paket2.HasValue:
                    paket2Satirlar.Add(Paket2Satira(kayit.Paket2.Value));
                    break;
            }
        }

        string matAd = Path.GetFileNameWithoutExtension(logDosyaYolu) + ".mat";
        string matYol = Path.Combine(hedefKlasor, matAd);

        using var fs = new FileStream(matYol, FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(fs);

        SayisalMatrisYaz(bw, Paket1MatrisAdi, paket1Satirlar, sutunSayisi: 7);
        MetinMatrisYaz(bw, Paket1BaslikAdi, Paket1Basliklari);
        SayisalMatrisYaz(bw, Paket2MatrisAdi, paket2Satirlar, sutunSayisi: 6);
        MetinMatrisYaz(bw, Paket2BaslikAdi, Paket2Basliklari);

        bw.Flush();
        return matYol;
    }

    #region Yardımcı: Paket -> double satır

    private static double[] Paket1Satira(HaberlesmePaket1 p) =>
    [
        p.Veri1_u8,
        p.Veri2_u8,
        p.Veri3_u8,
        p.Veri4_s16,
        p.Veri5_s16,
        p.Veri6_f32,
        p.Veri7_f32
    ];

    private static double[] Paket2Satira(HaberlesmePaket2 p) =>
    [
        p.Veri1_u8,
        p.Veri2_u8,
        p.Veri3_s16,
        p.Veri4_s32,
        p.SistemZamani_u32,
        p.Veri6_f64
    ];

    #endregion

    #region MAT v4 Yazıcılar

    // Sayısal matris: NxC boyutunda, double precision, MATLAB column-major formatında.
    private static void SayisalMatrisYaz(BinaryWriter bw, string ad, List<double[]> satirlar, int sutunSayisi)
    {
        int mrows = satirlar.Count;
        int ncols = mrows == 0 ? 0 : sutunSayisi;

        // type = 0 (LE/double/numeric)
        BaslikYaz(bw, type: 0, mrows: mrows, ncols: ncols, imagf: 0, ad: ad);

        // Column-major: önce sütun sabit, satır artar.
        for (int c = 0; c < ncols; c++)
        {
            for (int r = 0; r < mrows; r++)
            {
                bw.Write(satirlar[r][c]);
            }
        }
    }

    // Metin matrisi: 1xN boyutunda, her karakter ASCII kodunun double değeri olarak yazılır.
    private static void MetinMatrisYaz(BinaryWriter bw, string ad, string metin)
    {
        int mrows = metin.Length == 0 ? 0 : 1;
        int ncols = metin.Length;

        // type = 1 (LE/double/text)
        BaslikYaz(bw, type: 1, mrows: mrows, ncols: ncols, imagf: 0, ad: ad);

        for (int i = 0; i < metin.Length; i++)
        {
            bw.Write((double)metin[i]);
        }
    }

    // Tek bir MAT v4 başlığı yazar.
    private static void BaslikYaz(BinaryWriter bw, int type, int mrows, int ncols, int imagf, string ad)
    {
        bw.Write(type);
        bw.Write(mrows);
        bw.Write(ncols);
        bw.Write(imagf);

        byte[] adBytes = Encoding.ASCII.GetBytes(ad);
        bw.Write(adBytes.Length + 1); // namlen (null sonlandırıcı dahil)
        bw.Write(adBytes);
        bw.Write((byte)0);            // null terminator
    }

    #endregion
}
