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

            try
            {
                // 1. RUN PROGRAM INSTALLERS
                RunInstallers();

                // 2. APPLY VISUAL & PERFORMANCE SETTINGS
                ApplyPerformanceSettings();

                // 3. APPLY POWER & SLEEP SETTINGS
                ApplyPowerSettings();

                // 4. RUN WINGET UPGRADES IF ONLINE
                UpgradePackagesUsingWinget();

                // 5. BEEP AND NOTIFICATION POPUP
                System.Media.SystemSounds.Asterisk.Play();
                Console.Beep(800, 300);

                Application.Run(new NotificationForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Bir hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void RunInstallers()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string programlarDir = Path.Combine(baseDir, "Programlar");

            if (!Directory.Exists(programlarDir))
            {
                // If directory does not exist, we just skip silently or create it
                Directory.CreateDirectory(programlarDir);
                return;
            }

            // 1. Chrome Setup
            string chromePath = Path.Combine(programlarDir, "chrome_installer.exe");
            if (File.Exists(chromePath))
            {
                RunProcess(chromePath, "/silent /install");
            }

            // 2. Adobe Reader Setup
            string adobePath = Path.Combine(programlarDir, "adobe_reader.exe");
            if (File.Exists(adobePath))
            {
                RunProcess(adobePath, "/sAll /rs EULA_ACCEPT=YES");
            }

            // 3. Alpemix Setup / Portable Move
            string alpemixPath = Path.Combine(programlarDir, "alpemix.exe");
            if (File.Exists(alpemixPath))
            {
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
                }
                catch
                {
                    // Fail silently for portable copying (e.g. if file is locked or running)
                }
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
            if (IsInternetAvailable())
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "winget",
                        Arguments = "upgrade --all --silent --accept-source-agreements --accept-package-agreements",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    Process? p = Process.Start(psi);
                    p?.WaitForExit();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Winget upgrade error: " + ex.Message);
                }
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
