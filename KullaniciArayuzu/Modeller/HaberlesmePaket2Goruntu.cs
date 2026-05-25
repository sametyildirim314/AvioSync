using OrtakKutuphane.Modeller;

namespace KullaniciArayuzu.Modeller;

// HaberlesmePaket2'yi WPF DataGrid'de göstermek için kullanılan görüntü modeli.
public sealed class HaberlesmePaket2Goruntu
{
    public int SiraNo { get; set; }
    public string Zaman { get; set; } = "";

    public byte Veri1_u8 { get; set; }
    public byte Veri2_u8 { get; set; }
    public short Veri3_s16 { get; set; }
    public int Veri4_s32 { get; set; }
    public uint SistemZamani_u32 { get; set; }
    public double Veri6_f64 { get; set; }

    public static HaberlesmePaket2Goruntu Olustur(int sira, HaberlesmePaket2 paket) => new()
    {
        SiraNo = sira,
        Zaman = DateTime.Now.ToString("HH:mm:ss.fff"),
        Veri1_u8 = paket.Veri1_u8,
        Veri2_u8 = paket.Veri2_u8,
        Veri3_s16 = paket.Veri3_s16,
        Veri4_s32 = paket.Veri4_s32,
        SistemZamani_u32 = paket.SistemZamani_u32,
        Veri6_f64 = paket.Veri6_f64
    };
}
