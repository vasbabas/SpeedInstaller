# 🚀 SpeedInstaller & Windows Optimizer

SpeedInstaller, Windows işletim sistemlerinde sık kullanılan temel programları tamamen çevrimdışı (offline) ve katılımsız (sessiz/silent) şekilde kuran, aynı zamanda sistemi en iyi performans ayarlarına getirip güç yönetimini optimize eden güçlü, hafif ve şık tasarımlı bir yardımcı araçtır.

---

## 🌟 Özellikler

### 1. 📦 Katılımsız Program Kurulumları (Sessiz & Çevrimdışı)
Uygulama, kendi dizinindeki `Programlar` klasörünü kontrol ederek aşağıdaki yazılımları tamamen kullanıcı etkileşimi gerektirmeden kurar:
* **Google Chrome:** `/silent /install` parametresiyle arka planda sessizce kurulur.
* **Adobe Acrobat Reader:** `/sAll /rs EULA_ACCEPT=YES` parametreleriyle tüm lisans sözleşmeleri otomatik onaylanarak kurulur.
* **Alpemix:** `/S` parametresiyle kurulur. Ayrıca taşınabilir (portable) sürümü algılanırsa otomatik olarak `Program Files` içerisine konumlandırılır ve masaüstüne kısayolu oluşturulur.

### 2. ⚡ Görsel ve Performans Optimizasyonları
Windows arayüzündeki gereksiz yükleri kaldırarak performansı maksimuma çıkarır:
* Tüm pencere animasyonları, menü geçiş efektleri, gölgelendirmeler kapatılır (**En İyi Performans** modu).
* 📝 **ClearType (Ekran yazı tipi kenarlarını düzeltme)** ayarı korunur, yazı netliği bozulmaz.
* Pencereleri sürüklerken içerik gösterme kapatılarak ekran kartı yükü azaltılır.

### 3. 🔋 Güç ve Uyku Planı Yönetimi
* Sistem aktif güç şeması otomatik olarak **Yüksek Performans (High Performance)** moduna geçirilir.
* Hem prizde (AC) hem de pilde (DC) **Ekran Kapatma Süresi** ve **Uyku Moduna Geçme Süresi** **"ASLA" (0)** olarak yapılandırılır.

### 4. 🔄 Otomatik Güncelleme (Winget Entegrasyonu)
* Kurulum ve optimizasyon işlemlerinin sonunda internet bağlantısı kontrol edilir.
* Bağlantı aktifse, Windows Package Manager (**winget**) aracılığıyla kurulan tüm programlar arka planda en son sürümlerine sessizce yükseltilir.

### 5. 🔔 Modern Başarı Bildirimi
* İşlemler bittiğinde sesli uyarı verir ve özel tasarlanmış modern, karanlık temalı bir pop-up bildirim penceresi gösterir.

---

## 🛠️ Nasıl Kullanılır?

1. **[SpeedInstaller.exe](SpeedInstaller.exe)** dosyasını indirin.
2. Executable dosyasının bulunduğu dizinde **`Programlar`** adında bir klasör açın.
3. Klasörün içerisine çevrimdışı yükleyicileri yerleştirin:
   * `chrome_installer.exe`
   * `adobe_reader.exe`
   * `alpemix.exe`
4. `SpeedInstaller.exe` dosyasını **Yönetici Olarak Çalıştırın** (Uygulama zaten yetki isteyecektir).
5. Kurulum ve optimizasyon otomatik başlayacaktır. Bittiğinde başarı ekranı görünecektir.

---

## 🔧 Teknik Detaylar (Kayıt Defteri & API)

Uygulama, Windows ayarlarını kararlı hale getirmek için aşağıdaki kayıt defteri anahtarlarını ve Win32 API'lerini kullanır:

* **Kayıt Defteri Düzenlemeleri:**
  * `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects\VisualFXSetting` ➔ `3` (Özel)
  * `HKCU\Control Panel\Desktop\UserPreferencesMask` ➔ `90 12 01 80 10 00 00 00` (Performans Maskesi)
  * `HKCU\Control Panel\Desktop\FontSmoothing` ➔ `2` (ClearType Açık)
* **Win32 API Çağrıları:**
  * Değişikliklerin oturumu kapatmadan veya Explorer'ı yeniden başlatmadan hemen yansıması için `SystemParametersInfo` ile `SPI_SETFONTSMOOTHING` ve `SPI_SETDRAGFULLWINDOWS` parametreleri tetiklenir, ardından `SendMessageTimeout` ile sisteme `WM_SETTINGCHANGE` bildirimi yayınlanır.
