# AvioSync - Platform Arayüzleri Geliştirme Vaka Çalışması

Bu proje, bir gömülü sistem (aviyonik) simülasyonu ile kullanıcı arayüzü arasında çift yönlü, gerçek zamanlı veri haberleşmesini sağlamak ve bu iletişimi otonom test scriptleri ile doğrulamak amacıyla geliştirilmiştir. 

Proje, nesne yönelimli programlama (OOP), SOLID, DRY, KISS ve YAGNI prensipleri gözetilerek **.NET 8** ve **C# 12** teknolojileriyle inşa edilmiştir.

## 🚀 Teknolojiler ve Mimari
* **Platform:** .NET 10
* **Dil:** C# 14
* **Arayüz Framework:** WPF (Windows Presentation Foundation)
* **Haberleşme Protokolü:** UDP / TCP Sockets (Little Endian mimarisi)
* **Tasarım Deseni:** MVVM, State Machine (Paket Yakalama), Singleton

## 📦 Proje Yapısı ve Özellikler

Proje iki ana etaptan oluşmaktadır:

### 1. Haberleşme ve Veri İşleme (I. Etap)
* **Simülasyon ve Kullanıcı Arayüzü:** İki bağımsız uygulama eşzamanlı çalışarak birbirleriyle haberleşir.
* **Özel Protokol:** Veriler 5 Hz hızında, özel bir paket formatıyla (Senkron byte'ları, Paket ID, Boyut, Payload ve CRC-8) iletilir.
* **Paket Yakalama Makinesi (State Machine):** Gelen byte akışını asenkron olarak işler, hatalı paketleri ayıklar ve geçerli paket sayısını tutar.
* **Loglama ve MATLAB Entegrasyonu:** Alınan veriler `/Release/Log Kayıtları` dizinine kaydedilir. Log oynatma (playback) özelliği sunulur ve istenildiğinde loglar MATLAB (`.mat`) formatına dönüştürülerek `/Release/MATLAB Donusumleri` dizinine aktarılır.
* **Kısıtlamalar:** UI performansını artırmak ve thread çakışmalarını önlemek amacıyla UI tarafında **Timer kullanımı tamamen yasaklanmış**; veri akışı Event/Task/Thread (Maksimum 3 Thread) yapılarıyla asenkron olarak yönetilmiştir.

### 2. Otonom Test Yazılımı (II. Etap)
* **Otomatik UI Testleri:** Geliştirilen WPF arayüzü, C# `AutomationProperties.AutomationId` özellikleri üzerinden test scripti (JSON/XML) ile otomatik olarak test edilir.
* **Test Akışı:** Paket geliş durumları, komut/ayar gönderimi ve geri besleme (feedback) döngüleri simüle edilir.
* **Raporlama:** Test tamamlandığında adım adım başarılı/başarısız durumlarını içeren nihai bir PDF raporu üretilir.

## ⚙️ Kurulum ve Çalıştırma

1. Projeyi klonlayın veya zip dosyasından çıkarın.
2. Çözüm (Solution) dosyasını (.sln) **Visual Studio 2022** veya uyumlu bir IDE ile açın.
3. Projeyi derleyerek gerekli NuGet paketlerinin yüklenmesini sağlayın.
4. Çözüm üzerinden **Çoklu Başlangıç Projesi (Multiple Startup Projects)** ayarını aktif ederek hem `KullaniciArayuzu` hem de `SimulasyonArayuzu` projelerini aynı anda başlatın.

## 📂 Dosya Yolları (Derleme Sonrası)
Proje çalıştırıldığında gerekli okuma/yazma işlemleri uygulama dizinindeki `Release` klasörü üzerinden yapılır:
* **Log Dosyaları:** `.../Release/Log Kayıtları/`
* **MATLAB Çıktıları:** `.../Release/MATLAB Donusumleri/`
* **Test Scriptleri:** `.../Release/Test/`

## 📝 Notlar
* Arayüz tasarımı vektörel (XAML) olup, farklı çözünürlüklerde veri kaybı veya kayma yaşatmayacak şekilde tasarlanmıştır.
* Kod içerisindeki tüm isimlendirmeler ve açıklamalar Türkçe olarak standartlara uygun şekilde yapılmıştır.
