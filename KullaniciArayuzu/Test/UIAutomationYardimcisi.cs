using System.Windows;
using System.Windows.Automation;
using System.Windows.Interop;

namespace KullaniciArayuzu.Test;

// Bir WPF penceresi üzerinde UI Automation Client API'lerini kullanarak
// AutomationId üzerinden eleman bulma, buton tıklama, metin girme ve ComboBox
// seçimi gibi işlemleri yapan yardımcı sınıf.
//
// Test motoru, bu sınıfı bir Task üzerinden (ThreadPool worker) çağırır.
// UI Automation çağrıları kendi içlerinde Dispatcher.BeginInvoke ile UI thread'ine
// güvenli şekilde yönlenir; böylece deadlock oluşmadan butonlar tetiklenir.
public sealed class UIAutomationYardimcisi
{
    private readonly AutomationElement _pencere;

    // Pencere referansından AutomationElement türetir.
    public UIAutomationYardimcisi(Window pencere)
    {
        if (pencere is null) throw new ArgumentNullException(nameof(pencere));

        IntPtr hwnd = new WindowInteropHelper(pencere).Handle;
        if (hwnd == IntPtr.Zero)
            throw new InvalidOperationException("Pencerenin HWND'si henüz oluşmamış.");

        _pencere = AutomationElement.FromHandle(hwnd)
            ?? throw new InvalidOperationException("AutomationElement penceresi oluşturulamadı.");
    }

    // AutomationId'ye göre eleman bulur (alt ağaçta tüm seviyeleri arar).
    public AutomationElement BulById(string automationId)
    {
        var sart = new PropertyCondition(AutomationElement.AutomationIdProperty, automationId);
        var elem = _pencere.FindFirst(TreeScope.Descendants, sart);
        if (elem is null)
            throw new InvalidOperationException($"AutomationId bulunamadı: '{automationId}'.");
        return elem;
    }

    // InvokePattern ile butonu tıklar.
    public void Tikla(string automationId)
    {
        var elem = BulById(automationId);
        if (!elem.TryGetCurrentPattern(InvokePattern.Pattern, out var nesne) ||
            nesne is not InvokePattern ip)
        {
            throw new InvalidOperationException(
                $"'{automationId}' InvokePattern desteklemiyor (Button olmalı).");
        }

        ip.Invoke();
    }

    // ValuePattern ile TextBox değerini ayarlar.
    public void DegerYaz(string automationId, string deger)
    {
        var elem = BulById(automationId);
        if (!elem.TryGetCurrentPattern(ValuePattern.Pattern, out var nesne) ||
            nesne is not ValuePattern vp)
        {
            throw new InvalidOperationException(
                $"'{automationId}' ValuePattern desteklemiyor.");
        }

        vp.SetValue(deger);
    }

    // ComboBox'ı açıp belirli bir öğeyi seçer (item Name'ine göre).
    public void ComboOgesiSec(string comboAutomationId, string ogeAdi)
    {
        var combo = BulById(comboAutomationId);

        // ComboBox'ı genişlet ki item'ları aratılabilsin.
        if (combo.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var ecObj) &&
            ecObj is ExpandCollapsePattern ecp)
        {
            ecp.Expand();
        }

        // Adına göre öğeyi bul (ComboBoxItem.Content -> Name).
        var item = combo.FindFirst(TreeScope.Subtree | TreeScope.Descendants,
            new PropertyCondition(AutomationElement.NameProperty, ogeAdi));

        if (item is null)
        {
            // Bazı durumlarda fall-back: tüm pencerede ara
            item = _pencere.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.NameProperty, ogeAdi));
        }

        if (item is null)
            throw new InvalidOperationException(
                $"'{comboAutomationId}' içinde '{ogeAdi}' öğesi bulunamadı.");

        if (!item.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var sObj) ||
            sObj is not SelectionItemPattern sip)
        {
            throw new InvalidOperationException(
                $"'{ogeAdi}' SelectionItemPattern desteklemiyor.");
        }

        sip.Select();

        // Combo'yu kapat (görsel).
        if (combo.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var ecObj2) &&
            ecObj2 is ExpandCollapsePattern ecp2)
        {
            ecp2.Collapse();
        }
    }

    // Bir elemanın "Name" özelliğini okur (TextBlock için Text içeriği döner).
    public string MetinOku(string automationId)
    {
        var elem = BulById(automationId);
        return elem.Current.Name ?? string.Empty;
    }

    // AutomationId'ye sahip bir elemanın etkin (enabled) olup olmadığını döner.
    public bool EtkinMi(string automationId)
    {
        var elem = BulById(automationId);
        return elem.Current.IsEnabled;
    }
}
