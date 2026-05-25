using OrtakKutuphane.Modeller;

namespace KullaniciArayuzu.Modeller;

// HaberlesmePaket1'i WPF DataGrid'de göstermek için kullanılan görüntü modeli.
// Sade POCO yeterli; ObservableCollection ekleme/silme bildirimlerini kendisi yayınlar.
public sealed class HaberlesmePaket1Goruntu
{
    public int SiraNo { get; set; }
    public string Zaman { get; set; } = "";

    public byte Veri1_u8 { get; set; }
    public byte Veri2_u8 { get; set; }
    public byte Veri3_u8 { get; set; }
    public short Veri4_s16 { get; set; }
    public short Veri5_s16 { get; set; }
    public float Veri6_f32 { get; set; }
    public float Veri7_f32 { get; set; }

    public static HaberlesmePaket1Goruntu Olustur(int sira, HaberlesmePaket1 paket) => new()
    {
        SiraNo = sira,
        Zaman = DateTime.Now.ToString("HH:mm:ss.fff"),
        Veri1_u8 = paket.Veri1_u8,
        Veri2_u8 = paket.Veri2_u8,
        Veri3_u8 = paket.Veri3_u8,
        Veri4_s16 = paket.Veri4_s16,
        Veri5_s16 = paket.Veri5_s16,
        Veri6_f32 = paket.Veri6_f32,
        Veri7_f32 = paket.Veri7_f32
    };
}
