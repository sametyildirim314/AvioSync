using OrtakKutuphane.Enumlar;
using OrtakKutuphane.Modeller;
using OrtakKutuphane.Protokol;
using System.Threading;

namespace OrtakKutuphane.Haberlesme;

// Gelen byte akışını tek tek inceleyerek protokol paketlerini ayıklayan state machine.
// Akış üzerinden Senkron1 -> Senkron2 -> PaketID -> Boyut -> Veri[N] -> CRC sırasını takip eder.
// Başarılı çözülen paketleri PaketYakalandi olayı, hatalı paketleri HataliPaketYakalandi olayı ile yayınlar.
// Doğru/hatalı paket sayaçlarını dahili olarak tutar (thread-safe Interlocked artışı).
public sealed class PaketYakalamaMakinesi
{
    // State machine'in olası iç durumları.
    private enum Durum
    {
        Senkron1Bekleniyor,
        Senkron2Bekleniyor,
        PaketIdBekleniyor,
        PaketBoyutuBekleniyor,
        VeriBekleniyor,
        CrcBekleniyor
    }

    // Tanımlı paket tipleri için beklenen veri boyutları (DRY: tek bir yerden yönetilir).
    private static readonly IReadOnlyDictionary<UnitePaketTipleri, int> _beklenenVeriBoyutlari =
        new Dictionary<UnitePaketTipleri, int>
        {
            [UnitePaketTipleri.HaberlesmePaket1] = HaberlesmePaket1.VeriBoyutu,
            [UnitePaketTipleri.HaberlesmePaket2] = HaberlesmePaket2.VeriBoyutu,
            [UnitePaketTipleri.KomutPaket]       = KomutPaket.VeriBoyutu,
            [UnitePaketTipleri.AyarPaket]        = AyarPaket.VeriBoyutu,
            [UnitePaketTipleri.GeriBeslemePaket] = GeriBeslemePaket.VeriBoyutu,
        };

    // Aktif durum + mevcut paketin geçici alanları.
    private Durum _durum = Durum.Senkron1Bekleniyor;
    private UnitePaketTipleri _aktifPaketTipi;
    private int _beklenenVeriBoyutu;
    private byte[]? _veriTamponu;
    private int _veriIndex;

    // Birden fazla thread (dinleyici + UI) Besle/sayaç okumayı çakıştırabilir, kilit gerekli.
    private readonly object _kilit = new();

    // İstatistik sayaçları (long; uzun süreli akış için 64 bit yeterli).
    private long _dogruPaketSayisi;
    private long _hataliPaketSayisi;

    // Şu ana kadar yakalanan doğru paket sayısı.
    public long DogruPaketSayisi => Interlocked.Read(ref _dogruPaketSayisi);

    // Şu ana kadar tespit edilen hatalı paket sayısı.
    public long HataliPaketSayisi => Interlocked.Read(ref _hataliPaketSayisi);

    // Sağlıklı paket çözüldüğünde tetiklenir.
    public event EventHandler<PaketYakalandiEventArgs>? PaketYakalandi;

    // Hatalı paket tespit edildiğinde tetiklenir.
    public event EventHandler<HataliPaketYakalandiEventArgs>? HataliPaketYakalandi;

    // Sayaçları ve dahili durumu sıfırlar.
    public void Sifirla()
    {
        lock (_kilit)
        {
            _durum = Durum.Senkron1Bekleniyor;
            _veriTamponu = null;
            _veriIndex = 0;
            Interlocked.Exchange(ref _dogruPaketSayisi, 0);
            Interlocked.Exchange(ref _hataliPaketSayisi, 0);
        }
    }

    // Tek bir byte besler. Dinleyici tarafından her gelen byte için çağrılır.
    public void Besle(byte gelenByte)
    {
        lock (_kilit)
        {
            ByteIsle(gelenByte);
        }
    }

    // Toplu byte akışını besler (örn. UDP datagramı). Bütün byte'lar sırayla state machine'e verilir.
    public void Besle(ReadOnlySpan<byte> akis)
    {
        if (akis.IsEmpty) return;

        lock (_kilit)
        {
            for (int i = 0; i < akis.Length; i++)
            {
                ByteIsle(akis[i]);
            }
        }
    }

    // State machine'in ana akış kontrolü. Tek bir byte üzerinde durum geçişlerini uygular.
    private void ByteIsle(byte b)
    {
        switch (_durum)
        {
            case Durum.Senkron1Bekleniyor:
                if (b == ProtokolSabitleri.Senkron1)
                {
                    _durum = Durum.Senkron2Bekleniyor;
                }
                // İlk senkron yakalanmadıysa byte sessizce atılır (akış başlangıcında gürültü olabilir).
                break;

            case Durum.Senkron2Bekleniyor:
                if (b == ProtokolSabitleri.Senkron2)
                {
                    _durum = Durum.PaketIdBekleniyor;
                }
                else
                {
                    // Senkron1 sonrası beklenen Senkron2 gelmedi -> hatalı paket başlangıcı.
                    HataliPaketKaydet(HataliPaketSebebi.Senkron2Eslemedi,
                        $"Senkron2 ({ProtokolSabitleri.Senkron2}) beklenirken {b} geldi.");

                    // Gelen byte yine Senkron1 olabilir -> akışı kaybetmeyelim.
                    _durum = b == ProtokolSabitleri.Senkron1
                        ? Durum.Senkron2Bekleniyor
                        : Durum.Senkron1Bekleniyor;
                }
                break;

            case Durum.PaketIdBekleniyor:
                if (Enum.IsDefined(typeof(UnitePaketTipleri), b))
                {
                    _aktifPaketTipi = (UnitePaketTipleri)b;
                    _durum = Durum.PaketBoyutuBekleniyor;
                }
                else
                {
                    HataliPaketKaydet(HataliPaketSebebi.BilinmeyenPaketId,
                        $"Tanımsız Paket ID: 0x{b:X2}");
                    SenkronaGeriDon();
                }
                break;

            case Durum.PaketBoyutuBekleniyor:
                _beklenenVeriBoyutu = b;

                // Bilinen paket tipi için boyut sabit olmalı; eşleşmiyorsa hatalı kabul edilir.
                if (_beklenenVeriBoyutlari.TryGetValue(_aktifPaketTipi, out int sabitBoyut)
                    && sabitBoyut != _beklenenVeriBoyutu)
                {
                    HataliPaketKaydet(HataliPaketSebebi.PaketBoyutuHatali,
                        $"{_aktifPaketTipi} için beklenen boyut {sabitBoyut}, gelen: {_beklenenVeriBoyutu}.");
                    SenkronaGeriDon();
                    break;
                }

                if (_beklenenVeriBoyutu == 0)
                {
                    // Veri yoksa direkt CRC aşamasına geç.
                    _veriTamponu = Array.Empty<byte>();
                    _veriIndex = 0;
                    _durum = Durum.CrcBekleniyor;
                }
                else
                {
                    _veriTamponu = new byte[_beklenenVeriBoyutu];
                    _veriIndex = 0;
                    _durum = Durum.VeriBekleniyor;
                }
                break;

            case Durum.VeriBekleniyor:
                _veriTamponu![_veriIndex++] = b;
                if (_veriIndex >= _beklenenVeriBoyutu)
                {
                    _durum = Durum.CrcBekleniyor;
                }
                break;

            case Durum.CrcBekleniyor:
                CrcDogrulaVeYayinla(b);
                SenkronaGeriDon();
                break;
        }
    }

    // CRC byte'ı geldiğinde paketin tamamı için CRC hesaplar ve sonucu doğrular.
    private void CrcDogrulaVeYayinla(byte gelenCrc)
    {
        // CRC; Senkron1+Senkron2+ID+Boyut+Veri üzerinden hesaplanır (PDF gereği).
        int crcAlaniUzunlugu = ProtokolSabitleri.BaslikBoyutu + _beklenenVeriBoyutu;
        var crcIcinTampon = new byte[crcAlaniUzunlugu];
        crcIcinTampon[0] = ProtokolSabitleri.Senkron1;
        crcIcinTampon[1] = ProtokolSabitleri.Senkron2;
        crcIcinTampon[2] = (byte)_aktifPaketTipi;
        crcIcinTampon[3] = (byte)_beklenenVeriBoyutu;

        if (_veriTamponu is { Length: > 0 })
        {
            Buffer.BlockCopy(_veriTamponu, 0, crcIcinTampon, ProtokolSabitleri.BaslikBoyutu, _beklenenVeriBoyutu);
        }

        byte hesaplananCrc = Crc8Hesaplayici.Hesapla(crcIcinTampon, 0, crcAlaniUzunlugu);
        if (hesaplananCrc != gelenCrc)
        {
            HataliPaketKaydet(HataliPaketSebebi.CrcDogrulamasiBasarisiz,
                $"{_aktifPaketTipi} CRC uyuşmadı. Beklenen: 0x{hesaplananCrc:X2}, gelen: 0x{gelenCrc:X2}.");
            return;
        }

        // Başarılı paket -> sayaç + event.
        var yakalanan = new YakalananPaket(_aktifPaketTipi, _veriTamponu ?? Array.Empty<byte>());
        Interlocked.Increment(ref _dogruPaketSayisi);
        PaketYakalandi?.Invoke(this, new PaketYakalandiEventArgs(yakalanan));
    }

    // Hatalı paket sebebi loglar, sayacı arttırır ve event tetikler.
    private void HataliPaketKaydet(HataliPaketSebebi sebep, string aciklama)
    {
        Interlocked.Increment(ref _hataliPaketSayisi);
        HataliPaketYakalandi?.Invoke(this, new HataliPaketYakalandiEventArgs(sebep, aciklama));
    }

    // Geçici alanları temizleyip baştan senkron aramaya geri döner.
    private void SenkronaGeriDon()
    {
        _durum = Durum.Senkron1Bekleniyor;
        _veriTamponu = null;
        _veriIndex = 0;
        _beklenenVeriBoyutu = 0;
    }
}
