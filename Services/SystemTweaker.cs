using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace WDOS.Services
{
    public static class SystemTweaker
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

        private const uint SPI_SETFONTSMOOTHING = 0x004B;
        private const uint SPIF_UPDATEINIFILE = 0x01;
        private const uint SPIF_SENDCHANGE = 0x02;

        public static void SetHighPerformanceAndDisableSleep(Action<string> logCallback)
        {
            try
            {
                logCallback("Güç planı 'Yüksek Performans' olarak ayarlanıyor...");
                RunCommand("powercfg", "/setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");

                logCallback("Ekran kapatma ve uyku süreleri devre dışı bırakılıyor (Hiçbir Zaman)...");
                RunCommand("powercfg", "/change monitor-timeout-ac 0");
                RunCommand("powercfg", "/change disk-timeout-ac 0");
                RunCommand("powercfg", "/change standby-timeout-ac 0");
                RunCommand("powercfg", "/change hibernate-timeout-ac 0");
                
                logCallback("Güç ve uyku optimizasyonları tamamlandı.");
            }
            catch (Exception ex)
            {
                logCallback($"Hata (Güç Ayarları): {ex.Message}");
            }
        }

        public static void ApplyVisualPerformanceTweaks(Action<string> logCallback)
        {
            try
            {
                logCallback("Görsel efektler optimize ediliyor (En İyi Performans)...");

                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", true))
                {
                    if (key != null)
                    {
                        key.SetValue("VisualFXSetting", 3, RegistryValueKind.DWord);
                    }
                }

                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
                {
                    if (key != null)
                    {
                        byte[] mask = new byte[] { 0x90, 0x12, 0x03, 0x80, 0x10, 0x00, 0x00, 0x00 };
                        key.SetValue("UserPreferencesMask", mask, RegistryValueKind.Binary);
                        key.SetValue("FontSmoothing", "2", RegistryValueKind.String);
                        key.SetValue("FontSmoothingType", 2, RegistryValueKind.DWord);
                    }
                }

                IntPtr ptr = Marshal.AllocCoTaskMem(sizeof(int));
                Marshal.WriteInt32(ptr, 1);
                SystemParametersInfo(SPI_SETFONTSMOOTHING, 1, ptr, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
                Marshal.FreeCoTaskMem(ptr);

                logCallback("Yazı tipi kenar düzeltmeleri (ClearType) hariç tüm görsel efektler kapatıldı.");
            }
            catch (Exception ex)
            {
                logCallback($"Hata (Görsel Efektler): {ex.Message}");
            }
        }

        public static void ApplyPrivacyAndDebloat(Action<string> logCallback)
        {
            try
            {
                logCallback("Gizlilik ve Telemetri engelleme ayarları uygulanıyor...");

                // 1. Disable DiagTrack service (Connected User Experiences and Telemetry)
                logCallback("Telemetri servisi (DiagTrack) kapatılıyor...");
                RunCommand("sc", "stop DiagTrack");
                RunCommand("sc", "config DiagTrack start=disabled");

                // 2. Disable Bing Search in Start Menu
                logCallback("Başlat menüsü Bing aramaları kapatılıyor...");
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Policies\Microsoft\Windows\Explorer", true) ?? 
                                         Registry.CurrentUser.CreateSubKey(@"Software\Policies\Microsoft\Windows\Explorer", true))
                {
                    key?.SetValue("DisableSearchBoxSuggestions", 1, RegistryValueKind.DWord);
                }

                // 3. Disable Advertising ID
                logCallback("Reklam kimliği takibi kapatılıyor...");
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", true))
                {
                    key?.SetValue("Enabled", 0, RegistryValueKind.DWord);
                }

                // 4. Disable Cortana
                using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search", true) ??
                                         Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search", true))
                {
                    key?.SetValue("AllowCortana", 0, RegistryValueKind.DWord);
                }

                logCallback("Gizlilik ve Debloat tweaks başarıyla uygulandı.");
            }
            catch (Exception ex)
            {
                logCallback($"Hata (Gizlilik Ayarları): {ex.Message}");
            }
        }

        public static void OptimizeFileSystemAndServices(Action<string> logCallback)
        {
            try
            {
                logCallback("Sistem servisleri ve dosya sistemi optimize ediliyor...");

                // 1. Disable last access time updates on NTFS volumes to save disk writes
                logCallback("NTFS dosya erişim izleme zaman damgaları kapatılıyor...");
                RunCommand("fsutil", "behavior set disablelastaccess 1");

                // 2. Disable Update Delivery Optimization sharing (P2P Update sharing)
                logCallback("Windows Update Teslim En İyileştirme paylaşımı kapatılıyor...");
                using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config", true) ??
                                         Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config", true))
                {
                    key?.SetValue("DODownloadMode", 0, RegistryValueKind.DWord);
                }

                // 3. Stop RemoteRegistry service
                logCallback("Uzak Kayıt Defteri (RemoteRegistry) servisi devre dışı bırakılıyor...");
                RunCommand("sc", "stop RemoteRegistry");
                RunCommand("sc", "config RemoteRegistry start=disabled");

                logCallback("Sistem servis optimizasyonları tamamlandı.");
            }
            catch (Exception ex)
            {
                logCallback($"Hata (Servis Optimizasyonu): {ex.Message}");
            }
        }

        public static void ApplyAdvancedPerformanceTweaks(Action<string> logCallback)
        {
            try
            {
                logCallback("İleri düzey bilgisayar hızlandırma tweaks uygulanıyor...");

                // 1. System Responsiveness
                logCallback("Sistem yanıt gecikmeleri optimize ediliyor...");
                using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", true))
                {
                    if (key != null)
                    {
                        key.SetValue("SystemResponsiveness", 0, RegistryValueKind.DWord);
                        key.SetValue("NetworkThrottlingIndex", 0xFFFFFFFF, RegistryValueKind.DWord);
                    }
                }

                // 2. Menu opening delay reduction
                logCallback("Menü açılış gecikmesi (MenuShowDelay) azaltılıyor...");
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
                {
                    key?.SetValue("MenuShowDelay", "50", RegistryValueKind.String);
                }

                // 3. Xbox Game DVR (Disable to prevent background lag)
                logCallback("Xbox Game DVR arka plan veri takibi kapatılıyor...");
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"System\GameConfigStore", true))
                {
                    key?.SetValue("GameDVR_Enabled", 0, RegistryValueKind.DWord);
                }
                using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\GameDVR", true) ??
                                         Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\GameDVR", true))
                {
                    key?.SetValue("AllowGameDVR", 0, RegistryValueKind.DWord);
                }

                // 4. Disable Aero Shake (drastically reduces explorer lag when moving windows)
                logCallback("Aero Shake özelliği devre dışı bırakılıyor...");
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true))
                {
                    key?.SetValue("DisallowShaking", 1, RegistryValueKind.DWord);
                }

                // 5. RAM Optimization: Disable Paging Executive (keeps Windows kernel and drivers loaded in memory instead of pagefile)
                logCallback("Çekirdek bellek yerleşimi RAM'de kilitleniyor (Paging Executive tweaks)...");
                using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", true))
                {
                    key?.SetValue("DisablePagingExecutive", 1, RegistryValueKind.DWord);
                }

                logCallback("İleri düzey optimizasyon ve hızlandırma başarıyla uygulandı.");
            }
            catch (Exception ex)
            {
                logCallback($"Hata (İleri Optimizasyonlar): {ex.Message}");
            }
        }

        public static void PerformDiskCleanup(Action<string> logCallback)
        {
            try
            {
                logCallback("Disk temizleme işlemi başlatılıyor...");

                string userTemp = Path.GetTempPath();
                logCallback($"Kullanıcı Geçici Dosyaları temizleniyor: {userTemp}");
                CleanDirectory(userTemp, logCallback);

                string sysTemp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
                logCallback($"Sistem Geçici Dosyaları temizleniyor: {sysTemp}");
                CleanDirectory(sysTemp, logCallback);

                string prefetch = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
                logCallback($"Önbellek (Prefetch) temizleniyor: {prefetch}");
                CleanDirectory(prefetch, logCallback);

                logCallback("Geçici sistem dosyaları temizliği tamamlandı.");
            }
            catch (Exception ex)
            {
                logCallback($"Hata (Disk Temizleme): {ex.Message}");
            }
        }

        private static void CleanDirectory(string path, Action<string> logCallback)
        {
            if (!Directory.Exists(path)) return;

            var dir = new DirectoryInfo(path);
            
            foreach (var file in dir.GetFiles())
            {
                try
                {
                    file.Delete();
                }
                catch
                {
                    // Ignore locked files in use
                }
            }

            foreach (var subDir in dir.GetDirectories())
            {
                try
                {
                    subDir.Delete(true);
                }
                catch
                {
                    // Ignore locked folders in use
                }
            }
        }

        private static void RunCommand(string fileName, string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (var process = Process.Start(psi))
            {
                process?.WaitForExit();
            }
        }
    }
}
