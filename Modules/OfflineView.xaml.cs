using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WDOS.Services;

namespace WDOS.Modules
{
    public partial class OfflineView : UserControl
    {
        private readonly string _installersPath;
        
        // Target File Names
        private string _chromeFile = "";
        private string _adobeFile = "";
        private string _winrarFile = "";
        private string _alpemixFile = "";
        private string _anydeskFile = "";
        private string _vlcFile = "";
        private string _7zipFile = "";

        public OfflineView()
        {
            InitializeComponent();
            _installersPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Installers");
            Loaded += OfflineView_Loaded;
        }

        private void OfflineView_Loaded(object sender, RoutedEventArgs e)
        {
            CheckLocalInstallers();
        }

        private void CheckLocalInstallers()
        {
            try
            {
                if (!Directory.Exists(_installersPath))
                {
                    Directory.CreateDirectory(_installersPath);
                }

                // Check for Google Chrome Setup
                _chromeFile = FindInstallerFile("chrome");
                UpdateStatus(TxtStatusChrome, ChkChrome, !string.IsNullOrEmpty(_chromeFile));

                // Check for Adobe Reader Setup
                _adobeFile = FindInstallerFile("adobe");
                UpdateStatus(TxtStatusAdobe, ChkAdobe, !string.IsNullOrEmpty(_adobeFile));

                // Check for WinRAR Setup
                _winrarFile = FindInstallerFile("winrar");
                UpdateStatus(TxtStatusWinRar, ChkWinRar, !string.IsNullOrEmpty(_winrarFile));

                // Check for Alpemix Setup
                _alpemixFile = FindInstallerFile("alpemix");
                UpdateStatus(TxtStatusAlpemix, ChkAlpemix, !string.IsNullOrEmpty(_alpemixFile));

                // Check for AnyDesk Setup
                _anydeskFile = FindInstallerFile("anydesk");
                UpdateStatus(TxtStatusAnyDesk, ChkAnyDesk, !string.IsNullOrEmpty(_anydeskFile));

                // Check for VLC Setup
                _vlcFile = FindInstallerFile("vlc");
                UpdateStatus(TxtStatusVlc, ChkVlc, !string.IsNullOrEmpty(_vlcFile));

                // Check for 7-Zip Setup
                _7zipFile = FindInstallerFile("7z");
                UpdateStatus(TxtStatus7Zip, Chk7Zip, !string.IsNullOrEmpty(_7zipFile));
            }
            catch (Exception ex)
            {
                AppendLog($"Kurulum dosyaları taranırken hata oluştu: {ex.Message}");
            }
        }

        private string FindInstallerFile(string prefix)
        {
            if (!Directory.Exists(_installersPath)) return "";

            var files = Directory.GetFiles(_installersPath, "*.exe");
            foreach (var file in files)
            {
                string name = Path.GetFileName(file).ToLowerInvariant();
                if (name.Contains(prefix) || 
                    (prefix == "chrome" && name.Contains("setup")) || 
                    (prefix == "adobe" && (name.Contains("acro") || name.Contains("reader"))) || 
                    (prefix == "winrar" && name.Contains("wrar")) ||
                    (prefix == "7z" && name.Contains("7zip")))
                {
                    return file;
                }
            }
            return "";
        }

        private void UpdateStatus(TextBlock textBlock, CheckBox checkBox, bool found)
        {
            if (found)
            {
                textBlock.Text = "Bulundu ✔";
                textBlock.Foreground = (SolidColorBrush)Application.Current.Resources["AccentGreen"];
                checkBox.IsEnabled = true;
                checkBox.IsChecked = true;
            }
            else
            {
                textBlock.Text = "Eksik ❌";
                textBlock.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Slate-Red
                checkBox.IsChecked = false;
                checkBox.IsEnabled = false;
            }
        }

        private void OpenInstallersFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(_installersPath))
                {
                    Directory.CreateDirectory(_installersPath);
                }
                Process.Start("explorer.exe", _installersPath);
            }
            catch (Exception ex)
            {
                AppendLog($"Klasör açılamadı: {ex.Message}");
            }
        }

        private async void BtnStartDeploy_Click(object sender, RoutedEventArgs e)
        {
            BtnStartDeploy.IsEnabled = false;
            TxtLogs.Text = $"[{DateTime.Now:HH:mm:ss}] Çevrimdışı Dağıtım Başlatıldı...\n";

            bool doPower = ChkTweakPower.IsChecked == true;
            bool doVisuals = ChkTweakVisual.IsChecked == true;
            bool doPrivacy = ChkTweakPrivacy.IsChecked == true;
            bool doSystem = ChkTweakSystem.IsChecked == true;
            bool doAdvanced = ChkTweakAdvanced.IsChecked == true;
            bool doCleanup = ChkCleanup.IsChecked == true;
            
            bool runChrome = ChkChrome.IsChecked == true && !string.IsNullOrEmpty(_chromeFile);
            bool runAdobe = ChkAdobe.IsChecked == true && !string.IsNullOrEmpty(_adobeFile);
            bool runWinrar = ChkWinRar.IsChecked == true && !string.IsNullOrEmpty(_winrarFile);
            bool runAlpemix = ChkAlpemix.IsChecked == true && !string.IsNullOrEmpty(_alpemixFile);
            bool runAnydesk = ChkAnyDesk.IsChecked == true && !string.IsNullOrEmpty(_anydeskFile);
            bool runVlc = ChkVlc.IsChecked == true && !string.IsNullOrEmpty(_vlcFile);
            bool run7zip = Chk7Zip.IsChecked == true && !string.IsNullOrEmpty(_7zipFile);

            await Task.Run(() =>
            {
                // 1. Perform Tweaks
                if (doPower)
                {
                    SystemTweaker.SetHighPerformanceAndDisableSleep(AppendLog);
                }

                if (doVisuals)
                {
                    SystemTweaker.ApplyVisualPerformanceTweaks(AppendLog);
                }

                if (doPrivacy)
                {
                    SystemTweaker.ApplyPrivacyAndDebloat(AppendLog);
                }

                if (doSystem)
                {
                    SystemTweaker.OptimizeFileSystemAndServices(AppendLog);
                }

                if (doAdvanced)
                {
                    SystemTweaker.ApplyAdvancedPerformanceTweaks(AppendLog);
                }

                if (doCleanup)
                {
                    SystemTweaker.PerformDiskCleanup(AppendLog);
                }

                // 2. Install Apps
                if (runChrome)
                {
                    InstallAppSilent("Google Chrome", _chromeFile, "/silent /install");
                }

                if (runAdobe)
                {
                    InstallAppSilent("Adobe Reader", _adobeFile, "/sAll /rs");
                }

                if (runWinrar)
                {
                    InstallAppSilent("WinRAR", _winrarFile, "/S");
                }

                if (runAlpemix)
                {
                    // Alpemix portable execution or silent installation
                    InstallAppSilent("Alpemix", _alpemixFile, "/S");
                }

                if (runAnydesk)
                {
                    // AnyDesk silent parameters
                    InstallAppSilent("AnyDesk", _anydeskFile, "--install \"C:\\Program Files (x86)\\AnyDesk\" --start-with-win --silent");
                }

                if (runVlc)
                {
                    InstallAppSilent("VLC Media Player", _vlcFile, "/S");
                }

                if (run7zip)
                {
                    InstallAppSilent("7-Zip", _7zipFile, "/S");
                }

                AppendLog("Çevrimdışı dağıtım ve optimizasyon süreci tamamlandı! ⚡");
            });

            BtnStartDeploy.IsEnabled = true;
        }

        private void InstallAppSilent(string appName, string filePath, string arguments)
        {
            try
            {
                AppendLog($"{appName} kuruluyor... (Sessiz mod)");
                var psi = new ProcessStartInfo
                {
                    FileName = filePath,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                using (var process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        process.WaitForExit();
                        AppendLog($"{appName} kuruldu. (Kodu: {process.ExitCode})");
                    }
                    else
                    {
                        AppendLog($"{appName} işlemi başlatılamadı.");
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"{appName} yükleme hatası: {ex.Message}");
            }
        }

        private void AppendLog(string message)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                TxtLogs.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
                ScrollerLogs.ScrollToEnd();
            }));
        }
    }
}
