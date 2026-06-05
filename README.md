# 🚀 WDOS (Windows Deployment & Optimization Suite) - SpeedInstaller

[![OS](https://img.shields.io/badge/OS-Windows%2010%20%2F%2011-blue?style=for-the-badge&logo=windows)](https://microsoft.com/windows)
[![Framework](https://img.shields.io/badge/.NET-8.0%20WPF-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/en-us/apps/desktop/wpf)
[![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)](LICENSE)
[![GitHub release](https://img.shields.io/badge/Release-v1.0.0-orange?style=for-the-badge)](https://github.com/)

**WDOS (Windows Deployment & Optimization Suite)**, Windows işletim sisteminizi kurduktan sonra veya günlük kullanımda sisteminizi en yüksek performansa ulaştırmak, gereksiz arka plan hizmetlerinden arındırmak ve ihtiyacınız olan tüm programları tek tıkla kurmak için geliştirilmiş **profesyonel ve modern** bir masaüstü yardımcı aracıdır.

WPF tabanlı şık arayüzü, gelişmiş arka plan mimarisi ve zengin optimizasyon kütüphanesiyle WDOS, Windows deneyiminizi baştan tanımlar.

---

## ✨ Öne Çıkan Özellikler

### 1. 🌐 Çevrimiçi Yazılım Dağıtımı (Online Deployer)
* **Winget Altyapısı:** Windows Paket Yöneticisi (`winget`) ile entegre, tamamen sessiz (`--silent`) kurulum.
* **Akıllı Profiller:** İhtiyacınıza en uygun profili tek tıkla seçin:
  * 🏠 **Ev (Home):** Tarayıcılar, arşivciler, medya oynatıcılar ve iletişim araçları.
  * 🏢 **Ofis (Office):** Doküman editörleri, PDF araçları, toplantı ve bulut servisleri.
  * 💻 **Yazılımcı (Dev):** Editörler, Git, Python, Node.js, veritabanı istemcileri ve Docker.
  * 🎮 **Oyuncu (Gamer):** Steam, Epic Games, EA App, Discord ve sistem izleme araçları.
* **Kapsamlı Uygulama Havuzu:** Tarayıcılar, sistem araçları, geliştirici araçları, ofis ve multimedya kategorilerinde onlarca popüler program.

### 2. ⚡ Çevrimdışı İnce Ayarlar (Offline & Tweaks)
* **Güç Optimizasyonu:** Yüksek Performans planını etkinleştirme ve otomatik uyku modlarını devre dışı bırakma.
* **Görsel Efekt Optimizasyonu:** ClearType yazı tipi pürüzsüzleştirmesini koruyarak tüm gereksiz Windows pencere animasyonlarını kapatma.
* **Gizlilik & Güvenlik:** DiagTrack (Telemetri) servisini durdurma, Başlat menüsünde Bing aramalarını engelleme, Reklam kimliğini ve Cortana'yı kapatma.
* **Gelişmiş Performans Tweaks:** 
  * Sistem yanıt süresi (`SystemResponsiveness`) optimizasyonu.
  * Ağ bant genişliği sınırlamasını (`NetworkThrottlingIndex`) kaldırma.
  * Menü açılış hızlandırma (`MenuShowDelay`).
  * Aero Shake ve Xbox Game DVR kapatarak oyunlarda gecikmeyi en aza indirme.
  * **RAM Optimizasyonu:** Windows çekirdeğini disk yerine doğrudan RAM üzerinde kilitleme (`DisablePagingExecutive`).
* **Disk Temizliği:** Kullanıcı ve Sistem Geçici Dosyaları (Temp) ile önbellek (Prefetch) dizinlerini otomatik temizleme.

### 3. 🖥️ Donanım Analizi ve Teşhis (Hardware Diagnostics)
* **Anlık Sistem Durumu:** CPU kullanımı, RAM doluluk oranı ve işletim sistemi detayları ana ekranda anlık olarak güncellenir.
* **Donanım Detayları:** İşlemci, anakart, bellek, grafik kartı ve depolama birimlerinin marka, model ve sıcaklık/sağlık verilerini raporlama.

---

## 🛠️ Sistem Gereksinimleri

* **İşletim Sistemi:** Windows 10 (Derleme 1809 ve üzeri) veya Windows 11
* **Çalışma Zamanı:** [.NET Desktop Runtime 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
* **Bağlantı:** Çevrimiçi kurulumlar için aktif internet bağlantısı ve `winget` paket yöneticisi.

---

## 🚀 Başlangıç & Kurulum

Proje **Self-Contained Single-File** (Tüm bağımlılıklar dahil, tek exe) olarak derlenebilecek şekilde yapılandırılmıştır.

### Derleme Adımları

1. Depoyu bilgisayarınıza kopyalayın veya indirin:
   ```bash
   git clone https://github.com/KULLANICI_ADI/SpeedInstaller.git
   cd SpeedInstaller
   ```

2. Projeyi yayınlamak (Publish) ve tek dosya haline getirmek için Terminal'de aşağıdaki komutu çalıştırın:
   ```bash
   dotnet publish WDOS.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true
   ```

3. Çıktı dosyası `bin/Release/net8.0-windows/win-x64/publish/` klasöründe `WDOS.exe` adıyla oluşturulacaktır. Yönetici olarak çalıştırıp kullanmaya başlayabilirsiniz.

---

## 📂 Proje Yapısı

```text
SpeedInstaller/
│
├── Modules/                  # Kullanıcı Arayüzü Görünümleri (WPF UserControls)
│   ├── HardwareView.xaml     # Donanım Bilgileri Ekranı
│   ├── OfflineView.xaml      # Çevrimdışı İnce Ayarlar ve Tweaks Ekranı
│   ├── OnlineView.xaml       # Çevrimiçi Program Yükleyici Ekranı
│   └── RepairView.xaml       # Sistem Onarım ve Sorun Giderme Ekranı
│
├── Services/                 # Arka Plan Servisleri ve Çekirdek Mantık
│   └── SystemTweaker.cs      # Windows Kayıt Defteri (Registry) ve Güç Ayarları
│
├── App.xaml                  # Genel WPF Kaynakları ve Renk Paletleri
├── MainWindow.xaml           # Ana Pencere ve Kenar Çubuğu (Sidebar)
├── WDOS.csproj               # .NET 8.0 Proje Dosyası
└── app.manifest              # Yönetici (Administrator) yetkisi istemek için manifest dosyası
```

---

## 🔒 Güvenlik Notu

Uygulama; Windows Kayıt Defteri (Registry), güç planları ve sistem servisleri üzerinde değişiklik yapmaktadır. Bu ayarların sorunsuz uygulanabilmesi için uygulamanın **Yönetici Olarak Çalıştırılması** gerekmektedir. Uygulanan tüm ince ayarlar, Windows'un resmi API'leri ve güvenli kayıt defteri yolları kullanılarak yapılmaktadır.

---

## 📄 Lisans

Bu proje **MIT Lisansı** altında lisanslanmıştır. Detaylar için [LICENSE](LICENSE) dosyasına göz atabilirsiniz.
