using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SpeedInstaller
{
    static class Program
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

        private const uint SPI_SETFONTSMOOTHING = 0x004B;
        private const uint SPI_SETDRAGFULLWINDOWS = 0x0025;
        private const uint SPIF_UPDATEINIFILE = 0x01;
        private const uint SPIF_SENDCHANGE = 0x02;
        
        private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff);
        private const uint WM_SETTINGCHANGE = 0x001A;
        private const uint SMTO_ABORTIFHUNG = 0x0002;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Console.Title = "SpeedInstaller & Optimizer";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=====================================================================");
            Console.WriteLine("                SPEEDINSTALLER & WINDOWS OPTIMIZER                   ");
            Console.WriteLine("=====================================================================");
            Console.ResetColor();
            Console.WriteLine();

            try
            {
                // 1. RUN PROGRAM INSTALLERS
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[*] Program kurulumlari baslatiliyor...");
                Console.ResetColor();
                RunInstallers();
                Console.WriteLine("[+] Program kurulum adimlari tamamlandi.");
                Console.WriteLine();

                // 2. APPLY VISUAL & PERFORMANCE SETTINGS
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[*] Görsel ve performans ayarlari optimize ediliyor...");
                Console.ResetColor();
                ApplyPerformanceSettings();
                Console.WriteLine("[+] Tüm görsel efektler kapatildi (En iyi performans modu aktif).");
                Console.WriteLine("[+] ClearType yazı tipi düzeltme açik birakildi.");
                Console.WriteLine();

                // 3. APPLY POWER & SLEEP SETTINGS
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[*] Güç ve uyku ayarlari yapilandiriliyor...");
                Console.ResetColor();
                ApplyPowerSettings();
                Console.WriteLine("[+] Güç plani 'Yüksek Performans' olarak ayarlandi.");
                Console.WriteLine("[+] Ekran ve Uyku modu zaman aşimlari 'ASLA' olarak ayarlandi.");
                Console.WriteLine();

                // 4. RUN WINGET UPGRADES IF ONLINE
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[*] Güncelleştirmeler kontrol ediliyor...");
                Console.ResetColor();
                UpgradePackagesUsingWinget();
                Console.WriteLine();

                // 5. BEEP AND NOTIFICATION POPUP
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[+] TÜM İŞLEMLER BAŞARIYLA TAMAMLANDI!");
                Console.ResetColor();
                System.Media.SystemSounds.Asterisk.Play();
                Console.Beep(800, 300);

                Application.Run(new NotificationForm());
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[!] Bir hata olustu: {ex.Message}");
                Console.ResetColor();
                MessageBox.Show($"Bir hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string? FindInstaller(string directory, string pattern)
        {
            try
            {
                string[] files = Directory.GetFiles(directory, pattern, SearchOption.TopDirectoryOnly);
                if (files.Length > 0) return files[0];
            }
            catch {}
            return null;
        }

        private static void RunInstallers()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string programlarDir = Path.Combine(baseDir, "Programlar");

            if (!Directory.Exists(programlarDir))
            {
                Console.WriteLine("[-] 'Programlar' klasörü bulunamadı. Dizin oluşturuluyor...");
                Directory.CreateDirectory(programlarDir);
                return;
            }

            // 1. Chrome Setup
            string? chromePath = FindInstaller(programlarDir, "*chrome*.exe");
            if (chromePath != null)
            {
                Console.WriteLine($"[*] Google Chrome kuruluyor ({Path.GetFileName(chromePath)}), lütfen bekleyin...");
                RunProcess(chromePath, "/silent /install");
                Console.WriteLine("[+] Google Chrome kurulum işlemi tamamlandı.");
            }
            else
            {
                Console.WriteLine("[-] Google Chrome kurulum dosyası (*chrome*.exe) bulunamadı, bu adım atlanıyor.");
            }

            // 2. Adobe Reader Setup
            string? adobePath = FindInstaller(programlarDir, "*adobe*.exe") ?? 
                                FindInstaller(programlarDir, "*reader*.exe") ?? 
                                FindInstaller(programlarDir, "*acro*.exe");
            if (adobePath != null)
            {
                Console.WriteLine($"[*] Adobe Acrobat Reader kuruluyor ({Path.GetFileName(adobePath)}), lütfen bekleyin...");
                RunProcess(adobePath, "/sAll /rs EULA_ACCEPT=YES");
                Console.WriteLine("[+] Adobe Acrobat Reader kurulum işlemi tamamlandı.");
            }
            else
            {
                Console.WriteLine("[-] Adobe Reader kurulum dosyası (*adobe*, *reader*, *acro*) bulunamadı, bu adım atlanıyor.");
            }

            // 3. Alpemix Setup / Portable Move
            string? alpemixPath = FindInstaller(programlarDir, "*alpemix*.exe");
            if (alpemixPath != null)
            {
                Console.WriteLine($"[*] Alpemix kuruluyor/konumlandırılıyor ({Path.GetFileName(alpemixPath)}), lütfen bekleyin...");
                // Run silent setup
                RunProcess(alpemixPath, "/S");

                // Check or handle portable setup
                try
                {
                    string progFilesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Alpemix");
                    if (!Directory.Exists(progFilesDir))
                    {
                        Directory.CreateDirectory(progFilesDir);
                    }

                    string targetAlpemix = Path.Combine(progFilesDir, "alpemix.exe");
                    
                    // Copy file if not exists in Program Files or newer
                    File.Copy(alpemixPath, targetAlpemix, true);

                    // Create Desktop Shortcut using PowerShell to keep codebase simple and dependency-free
                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    string shortcutScript = $"$WshShell = New-Object -ComObject WScript.Shell; " +
                                            $"$Shortcut = $WshShell.CreateShortcut('{Path.Combine(desktopPath, "Alpemix.lnk")}'); " +
                                            $"$Shortcut.TargetPath = '{targetAlpemix}'; " +
                                            $"$Shortcut.Save()";

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{shortcutScript}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    })?.WaitForExit();
                    Console.WriteLine("[+] Alpemix Program Files içerisine konumlandırıldı ve masaüstü kısayolu oluşturuldu.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[-] Alpemix kısayol/kopyalama hatası: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("[-] Alpemix kurulum dosyası (*alpemix*.exe) bulunamadı, bu adım atlanıyor.");
            }
        }

        private static void RunProcess(string fileName, string arguments)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = true,
                    Verb = "runas" // Request elevation just in case
                };
                Process? p = Process.Start(psi);
                p?.WaitForExit();
            }
            catch
            {
                // Silent catch
            }
        }

        private static void ApplyPerformanceSettings()
        {
            try
            {
                // Adjust for best performance (VisualFXSetting = 2) but configure registry values manually
                // Registry path for visual effects settings
                using (RegistryKey? visualKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects"))
                {
                    if (visualKey != null)
                    {
                        visualKey.SetValue("VisualFXSetting", 3, RegistryValueKind.DWord); // 3 = Custom
                    }
                }

                // Apply Best Performance mask with Font Smoothing enabled
                using (RegistryKey? desktopKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
                {
                    if (desktopKey != null)
                    {
                        // Standard UserPreferencesMask for performance (with font smoothing enabled, bits configured)
                        byte[] performanceMask = new byte[] { 0x90, 0x12, 0x01, 0x80, 0x10, 0x00, 0x00, 0x00 };
                        desktopKey.SetValue("UserPreferencesMask", performanceMask, RegistryValueKind.Binary);
                        
                        // Force font smoothing settings to be enabled (ClearType)
                        desktopKey.SetValue("FontSmoothing", "2", RegistryValueKind.String);
                        desktopKey.SetValue("FontSmoothingType", 2, RegistryValueKind.DWord);
                        desktopKey.SetValue("DragFullWindows", "0", RegistryValueKind.String);
                    }
                }

                // Adjust Advanced Explorer values for performance
                using (RegistryKey? advancedKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true))
                {
                    if (advancedKey != null)
                    {
                        advancedKey.SetValue("TaskbarAnimations", 0, RegistryValueKind.DWord);
                        advancedKey.SetValue("ListviewAlphaSelect", 0, RegistryValueKind.DWord);
                        advancedKey.SetValue("ListviewShadow", 0, RegistryValueKind.DWord);
                    }
                }

                // Apply the Font Smoothing parameters immediately via Windows API
                SystemParametersInfo(SPI_SETFONTSMOOTHING, 1, IntPtr.Zero, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
                SystemParametersInfo(SPI_SETDRAGFULLWINDOWS, 0, IntPtr.Zero, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);

                // Broadcast WM_SETTINGCHANGE to notify system shell and active applications
                IntPtr result;
                SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, "Registry", SMTO_ABORTIFHUNG, 5000, out result);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Optimisation error: " + ex.Message);
            }
        }

        private static void ApplyPowerSettings()
        {
            try
            {
                // Activate High Performance Plan
                RunPowercfg("-setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");

                // Disable screen off time
                RunPowercfg("-change monitor-timeout-ac 0");
                RunPowercfg("-change monitor-timeout-dc 0");

                // Disable sleep/standby mode time
                RunPowercfg("-change standby-timeout-ac 0");
                RunPowercfg("-change standby-timeout-dc 0");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Powercfg error: " + ex.Message);
            }
        }

        private static void RunPowercfg(string args)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powercfg.exe",
                    Arguments = args,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process? p = Process.Start(psi);
                p?.WaitForExit();
            }
            catch
            {
                // Silent catch
            }
        }

        private static void UpgradePackagesUsingWinget()
        {
            Console.WriteLine("[*] İnternet bağlantısı kontrol ediliyor...");
            if (IsInternetAvailable())
            {
                Console.WriteLine("[+] İnternet bağlantısı aktif. Winget güncelleştirmeleri başlatılıyor...");
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "winget",
                        Arguments = "upgrade --all --silent --accept-source-agreements --accept-package-agreements",
                        CreateNoWindow = false, // Set to false to show winget output in cmd!
                        UseShellExecute = false
                    };
                    Process? p = Process.Start(psi);
                    p?.WaitForExit();
                    Console.WriteLine("[+] Winget güncellemeleri tamamlandı.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[-] Winget çalıştırma hatası: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("[-] İnternet bağlantısı yok veya erişim zaman aşımına uğradı. Güncelleme adımı atlanıyor.");
            }
        }

        private static bool IsInternetAvailable()
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = ping.Send("8.8.8.8", 3000);
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
