using OrtakKutuphane.Enumlar;
using OrtakKutuphane.Modeller;
using System.Buffers.Binary;

namespace OrtakKutuphane.Protokol;

// Haberleşme protokolüne göre struct yapılarını byte dizilerine paketleyen ve
// byte dizilerini struct yapılarına dönüştüren merkezi sınıf.
// Tüm sayısal alanlar Little Endian formatında yazılır/okunur.
//
// Paket genel yapısı:
//   [Senkron1][Senkron2][PaketID][PaketBoyutu][... veri ...][CRC8]
// CRC; Senkron1+Senkron2+PaketID+PaketBoyutu+Veri byte'ları üzerinden hesaplanır.
public static class PaketSerilestirici
{
    #region Genel Paketleme (Veri dizisi -> tam paket)

    // Hazır bir veri dizisini protokol başlığı ve CRC-8 ile sarmalayarak gönderilebilir
    // tam paketi oluşturur.
    // paketTipi: Paketin başlığına yazılacak ID.
    // veri:      Paketin veri kısmını oluşturan Little Endian byte dizisi.
    // Döner:     Senkron + başlık + veri + CRC içeren paket.
    public static byte[] PaketOlustur(UnitePaketTipleri paketTipi, ReadOnlySpan<byte> veri)
    {
        if (veri.Length > ProtokolSabitleri.MaksimumVeriBoyutu)
            throw new ArgumentException(
                $"Veri boyutu {ProtokolSabitleri.MaksimumVeriBoyutu} byte'tan büyük olamaz.",
                nameof(veri));

        int toplamUzunluk = ProtokolSabitleri.BaslikBoyutu + veri.Length + ProtokolSabitleri.CrcBoyutu;
        var paket = new byte[toplamUzunluk];

        paket[0] = ProtokolSabitleri.Senkron1;
        paket[1] = ProtokolSabitleri.Senkron2;
        paket[2] = (byte)paketTipi;
        paket[3] = (byte)veri.Length;

        veri.CopyTo(paket.AsSpan(ProtokolSabitleri.BaslikBoyutu, veri.Length));

        // CRC; senkron byte'ları dahil tüm pakete uygulanır (CRC byte'ı hariç).
        byte crc = Crc8Hesaplayici.Hesapla(paket, 0, toplamUzunluk - ProtokolSabitleri.CrcBoyutu);
        paket[toplamUzunluk - 1] = crc;

        return paket;
    }

    #endregion

    #region HaberlesmePaket1

    // HaberlesmePaket1 struct'ını gönderilebilir paket byte dizisine çevirir.
    public static byte[] Serilestir(HaberlesmePaket1 paket)
    {
        Span<byte> veri = stackalloc byte[HaberlesmePaket1.VeriBoyutu];
        veri[0] = paket.Veri1_u8;
        veri[1] = paket.Veri2_u8;
        veri[2] = paket.Veri3_u8;
        BinaryPrimitives.WriteInt16LittleEndian(veri.Slice(3, 2), paket.Veri4_s16);
        BinaryPrimitives.WriteInt16LittleEndian(veri.Slice(5, 2), paket.Veri5_s16);
        BinaryPrimitives.WriteSingleLittleEndian(veri.Slice(7, 4), paket.Veri6_f32);
        BinaryPrimitives.WriteSingleLittleEndian(veri.Slice(11, 4), paket.Veri7_f32);

        return PaketOlustur(UnitePaketTipleri.HaberlesmePaket1, veri);
    }

    // HaberlesmePaket1 paketinin veri bölümünü struct'a dönüştürür.
    public static HaberlesmePaket1 HaberlesmePaket1Coz(ReadOnlySpan<byte> veri)
    {
        if (veri.Length < HaberlesmePaket1.VeriBoyutu)
            throw new ArgumentException("HaberlesmePaket1 için veri uzunluğu yetersiz.", nameof(veri));

        return new HaberlesmePaket1
        {
            Veri1_u8 = veri[0],
            Veri2_u8 = veri[1],
            Veri3_u8 = veri[2],
            Veri4_s16 = BinaryPrimitives.ReadInt16LittleEndian(veri.Slice(3, 2)),
            Veri5_s16 = BinaryPrimitives.ReadInt16LittleEndian(veri.Slice(5, 2)),
            Veri6_f32 = BinaryPrimitives.ReadSingleLittleEndian(veri.Slice(7, 4)),
            Veri7_f32 = BinaryPrimitives.ReadSingleLittleEndian(veri.Slice(11, 4))
        };
    }

    #endregion

    #region HaberlesmePaket2

    // HaberlesmePaket2 struct'ını gönderilebilir paket byte dizisine çevirir.
    public static byte[] Serilestir(HaberlesmePaket2 paket)
    {
        Span<byte> veri = stackalloc byte[HaberlesmePaket2.VeriBoyutu];
        veri[0] = paket.Veri1_u8;
        veri[1] = paket.Veri2_u8;
        BinaryPrimitives.WriteInt16LittleEndian(veri.Slice(2, 2), paket.Veri3_s16);
        BinaryPrimitives.WriteInt32LittleEndian(veri.Slice(4, 4), paket.Veri4_s32);
        BinaryPrimitives.WriteUInt32LittleEndian(veri.Slice(8, 4), paket.SistemZamani_u32);
        BinaryPrimitives.WriteDoubleLittleEndian(veri.Slice(12, 8), paket.Veri6_f64);

        return PaketOlustur(UnitePaketTipleri.HaberlesmePaket2, veri);
    }

    // HaberlesmePaket2 paketinin veri bölümünü struct'a dönüştürür.
    public static HaberlesmePaket2 HaberlesmePaket2Coz(ReadOnlySpan<byte> veri)
    {
        if (veri.Length < HaberlesmePaket2.VeriBoyutu)
            throw new ArgumentException("HaberlesmePaket2 için veri uzunluğu yetersiz.", nameof(veri));

        return new HaberlesmePaket2
        {
            Veri1_u8 = veri[0],
            Veri2_u8 = veri[1],
            Veri3_s16 = BinaryPrimitives.ReadInt16LittleEndian(veri.Slice(2, 2)),
            Veri4_s32 = BinaryPrimitives.ReadInt32LittleEndian(veri.Slice(4, 4)),
            SistemZamani_u32 = BinaryPrimitives.ReadUInt32LittleEndian(veri.Slice(8, 4)),
            Veri6_f64 = BinaryPrimitives.ReadDoubleLittleEndian(veri.Slice(12, 8))
        };
    }

    #endregion

    #region KomutPaket

    // KomutPaket struct'ını gönderilebilir paket byte dizisine çevirir.
    public static byte[] Serilestir(KomutPaket paket)
    {
        Span<byte> veri = stackalloc byte[KomutPaket.VeriBoyutu];
        veri[0] = (byte)paket.Komut;
        return PaketOlustur(UnitePaketTipleri.KomutPaket, veri);
    }

    // KomutPaket paketinin veri bölümünü struct'a dönüştürür.
    public static KomutPaket KomutPaketCoz(ReadOnlySpan<byte> veri)
    {
        if (veri.Length < KomutPaket.VeriBoyutu)
            throw new ArgumentException("KomutPaket için veri uzunluğu yetersiz.", nameof(veri));

        return new KomutPaket { Komut = (KomutTipi)veri[0] };
    }

    #endregion

    #region AyarPaket

    // AyarPaket struct'ını gönderilebilir paket byte dizisine çevirir.
    public static byte[] Serilestir(AyarPaket paket)
    {
        Span<byte> veri = stackalloc byte[AyarPaket.VeriBoyutu];
        veri[0] = (byte)paket.Ayar;
        BinaryPrimitives.WriteSingleLittleEndian(veri.Slice(1, 4), paket.Deger_f32);
        return PaketOlustur(UnitePaketTipleri.AyarPaket, veri);
    }

    // AyarPaket paketinin veri bölümünü struct'a dönüştürür.
    public static AyarPaket AyarPaketCoz(ReadOnlySpan<byte> veri)
    {
        if (veri.Length < AyarPaket.VeriBoyutu)
            throw new ArgumentException("AyarPaket için veri uzunluğu yetersiz.", nameof(veri));

        return new AyarPaket
        {
            Ayar = (AyarTipi)veri[0],
            Deger_f32 = BinaryPrimitives.ReadSingleLittleEndian(veri.Slice(1, 4))
        };
    }

    #endregion

    #region GeriBeslemePaket

    // GeriBeslemePaket struct'ını gönderilebilir paket byte dizisine çevirir.
    public static byte[] Serilestir(GeriBeslemePaket paket)
    {
        Span<byte> veri = stackalloc byte[GeriBeslemePaket.VeriBoyutu];
        veri[0] = (byte)paket.CevapTipi;
        BinaryPrimitives.WriteSingleLittleEndian(veri.Slice(1, 4), paket.Deger_f32);
        return PaketOlustur(UnitePaketTipleri.GeriBeslemePaket, veri);
    }

    // GeriBeslemePaket paketinin veri bölümünü struct'a dönüştürür.
    public static GeriBeslemePaket GeriBeslemePaketCoz(ReadOnlySpan<byte> veri)
    {
        if (veri.Length < GeriBeslemePaket.VeriBoyutu)
            throw new ArgumentException("GeriBeslemePaket için veri uzunluğu yetersiz.", nameof(veri));

        return new GeriBeslemePaket
        {
            CevapTipi = (AyarSorguTipi)veri[0],
            Deger_f32 = BinaryPrimitives.ReadSingleLittleEndian(veri.Slice(1, 4))
        };
    }

    #endregion
}
