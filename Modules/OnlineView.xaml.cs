using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WDOS.Modules
{
    public partial class OnlineView : UserControl
    {
        private bool _isInternetAvailable = false;
        private bool _isWingetAvailable = false;

        public OnlineView()
        {
            InitializeComponent();
            Loaded += OnlineView_Loaded;
        }

        private async void OnlineView_Loaded(object sender, RoutedEventArgs e)
        {
            await RunPreChecks();
        }

        private async Task RunPreChecks()
        {
            AppendLog("Sistem ön kontrolleri başlatılıyor...");
            BtnStartDeploy.IsEnabled = false;

            _isInternetAvailable = await Task.Run(() => CheckInternetConnection());
            UpdateCheckIndicator(DotNetStatus, TxtNetStatus, _isInternetAvailable, "İnternet Bağlantısı: OK ✔", "İnternet Bağlantısı: Yok ❌");

            _isWingetAvailable = await Task.Run(() => CheckWingetPresence());
            UpdateCheckIndicator(DotWingetStatus, TxtWingetStatus, _isWingetAvailable, "Winget: Hazır ✔", "Winget: Bulunamadı ❌");

            if (_isInternetAvailable && _isWingetAvailable)
            {
                AppendLog("Ön kontroller başarılı. Dağıtıma hazırsınız.");
                BtnStartDeploy.IsEnabled = true;
            }
            else
            {
                if (!_isInternetAvailable)
                {
                    AppendLog("UYARI: İnternet bağlantısı algılanamadı. Çevrimiçi kurulum yapılamaz!");
                }
                if (!_isWingetAvailable)
                {
                    AppendLog("UYARI: Sisteminizde 'winget' paket yöneticisi bulunamadı. Lütfen Windows App Installer'ı güncelleyin.");
                }
            }
        }

        private bool CheckInternetConnection()
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = ping.Send("8.8.8.8", 1500);
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        private bool CheckWingetPresence()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = "--version",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                using (var process = Process.Start(psi))
                {
                    if (process == null) return false;
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private void UpdateCheckIndicator(Ellipse dot, TextBlock text, bool success, string successMsg, string failMsg)
        {
            Dispatcher.Invoke(() =>
            {
                if (success)
                {
                    dot.Fill = (SolidColorBrush)Application.Current.Resources["AccentGreen"];
                    text.Text = successMsg;
                    text.Foreground = (SolidColorBrush)Application.Current.Resources["TextPrimary"];
                }
                else
                {
                    dot.Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Slate Red
                    text.Text = failMsg;
                    text.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                }
            });
        }

        private void Profile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string profileName)
            {
                ClearAllChecks();

                switch (profileName)
                {
                    case "Home":
                        ChkChrome.IsChecked = true;
                        ChkWinRar.IsChecked = true;
                        ChkVlc.IsChecked = true;
                        ChkAdobe.IsChecked = true;
                        ChkDiscord.IsChecked = true;
                        ChkTelegram.IsChecked = true;
                        ChkSteam.IsChecked = true;
                        ChkSpotify.IsChecked = true;
                        ChkOneDrive.IsChecked = true;
                        ChkAnyDesk.IsChecked = true;
                        break;
                    case "Office":
                        ChkChrome.IsChecked = true;
                        ChkEdge.IsChecked = true;
                        Chk7Zip.IsChecked = true;
                        ChkAdobe.IsChecked = true;
                        ChkLibreOffice.IsChecked = true;
                        ChkSlack.IsChecked = true;
                        ChkZoom.IsChecked = true;
                        ChkTeams.IsChecked = true;
                        ChkPdf24.IsChecked = true;
                        ChkOneDrive.IsChecked = true;
                        break;
                    case "Dev":
                        ChkChrome.IsChecked = true;
                        ChkFirefox.IsChecked = true;
                        ChkVsCode.IsChecked = true;
                        ChkGit.IsChecked = true;
                        ChkNotepad.IsChecked = true;
                        ChkPython.IsChecked = true;
                        ChkNode.IsChecked = true;
                        ChkPutty.IsChecked = true;
                        ChkWinSCP.IsChecked = true;
                        ChkDBeaver.IsChecked = true;
                        ChkHeidi.IsChecked = true;
                        ChkPostman.IsChecked = true;
                        Chk7Zip.IsChecked = true;
                        break;
                    case "Gamer":
                        ChkChrome.IsChecked = true;
                        ChkDiscord.IsChecked = true;
                        ChkSteam.IsChecked = true;
                        ChkEpic.IsChecked = true;
                        ChkEA.IsChecked = true;
                        ChkUbisoft.IsChecked = true;
                        ChkGog.IsChecked = true;
                        ChkWinRar.IsChecked = true;
                        ChkSpotify.IsChecked = true;
                        break;
                }
                AppendLog($"'{button.Content}' profili seçildi.");
            }
        }

        private void ClearSelection_Click(object sender, RoutedEventArgs e)
        {
            ClearAllChecks();
            AppendLog("Tüm seçimler temizlendi.");
        }

        private void ClearAllChecks()
        {
            // Tab 1
            ChkChrome.IsChecked = false;
            ChkFirefox.IsChecked = false;
            ChkBrave.IsChecked = false;
            ChkEdge.IsChecked = false;
            ChkOpera.IsChecked = false;
            ChkVivaldi.IsChecked = false;
            ChkWaterfox.IsChecked = false;
            ChkTor.IsChecked = false;
            ChkGDrive.IsChecked = false;
            ChkOneDrive.IsChecked = false;
            ChkDropbox.IsChecked = false;
            ChkQbit.IsChecked = false;
            ChkTransmission.IsChecked = false;
            ChkFileZilla.IsChecked = false;

            // Tab 2
            ChkWinRar.IsChecked = false;
            Chk7Zip.IsChecked = false;
            ChkWizTree.IsChecked = false;
            ChkTreeSize.IsChecked = false;
            ChkCrystalDisk.IsChecked = false;
            ChkCrystalMark.IsChecked = false;
            ChkCpuZ.IsChecked = false;
            ChkGpuZ.IsChecked = false;
            ChkHwMonitor.IsChecked = false;
            ChkAida64.IsChecked = false;
            ChkAnyDesk.IsChecked = false;
            ChkTeamViewer.IsChecked = false;
            ChkRufus.IsChecked = false;
            ChkBleachBit.IsChecked = false;
            ChkMalwarebytes.IsChecked = false;
            ChkAdwCleaner.IsChecked = false;

            // Tab 3
            ChkAdobe.IsChecked = false;
            ChkSumatra.IsChecked = false;
            ChkPdf24.IsChecked = false;
            ChkLibreOffice.IsChecked = false;
            ChkThunderbird.IsChecked = false;
            ChkWpsOffice.IsChecked = false;
            ChkVlc.IsChecked = false;
            ChkPotPlayer.IsChecked = false;
            ChkKLite.IsChecked = false;
            ChkSpotify.IsChecked = false;
            ChkAudacity.IsChecked = false;
            ChkHandbrake.IsChecked = false;
            ChkShareX.IsChecked = false;
            ChkObs.IsChecked = false;
            ChkGimp.IsChecked = false;
            ChkInkscape.IsChecked = false;
            ChkBlender.IsChecked = false;

            // Tab 4
            ChkVsCode.IsChecked = false;
            ChkGit.IsChecked = false;
            ChkNotepad.IsChecked = false;
            ChkSublime.IsChecked = false;
            ChkPython.IsChecked = false;
            ChkNode.IsChecked = false;
            ChkIntelliJ.IsChecked = false;
            ChkEclipse.IsChecked = false;
            ChkWinSCP.IsChecked = false;
            ChkPutty.IsChecked = false;
            ChkDBeaver.IsChecked = false;
            ChkHeidi.IsChecked = false;
            ChkPostman.IsChecked = false;
            ChkVirtualBox.IsChecked = false;
            ChkDocker.IsChecked = false;

            // Tab 5
            ChkDiscord.IsChecked = false;
            ChkTelegram.IsChecked = false;
            ChkSlack.IsChecked = false;
            ChkZoom.IsChecked = false;
            ChkTeams.IsChecked = false;
            ChkSkype.IsChecked = false;
            ChkSteam.IsChecked = false;
            ChkEpic.IsChecked = false;
            ChkEA.IsChecked = false;
            ChkGog.IsChecked = false;
            ChkUbisoft.IsChecked = false;
        }

        private async void BtnStartDeploy_Click(object sender, RoutedEventArgs e)
        {
            var packageList = new List<Tuple<string, string>>();

            // Tab 1
            if (ChkChrome.IsChecked == true) packageList.Add(Tuple.Create("Google Chrome", ChkChrome.Tag?.ToString() ?? ""));
            if (ChkFirefox.IsChecked == true) packageList.Add(Tuple.Create("Mozilla Firefox", ChkFirefox.Tag?.ToString() ?? ""));
            if (ChkBrave.IsChecked == true) packageList.Add(Tuple.Create("Brave Browser", ChkBrave.Tag?.ToString() ?? ""));
            if (ChkEdge.IsChecked == true) packageList.Add(Tuple.Create("Microsoft Edge", ChkEdge.Tag?.ToString() ?? ""));
            if (ChkOpera.IsChecked == true) packageList.Add(Tuple.Create("Opera", ChkOpera.Tag?.ToString() ?? ""));
            if (ChkVivaldi.IsChecked == true) packageList.Add(Tuple.Create("Vivaldi", ChkVivaldi.Tag?.ToString() ?? ""));
            if (ChkWaterfox.IsChecked == true) packageList.Add(Tuple.Create("Waterfox", ChkWaterfox.Tag?.ToString() ?? ""));
            if (ChkTor.IsChecked == true) packageList.Add(Tuple.Create("Tor Browser", ChkTor.Tag?.ToString() ?? ""));
            if (ChkGDrive.IsChecked == true) packageList.Add(Tuple.Create("Google Drive", ChkGDrive.Tag?.ToString() ?? ""));
            if (ChkOneDrive.IsChecked == true) packageList.Add(Tuple.Create("OneDrive", ChkOneDrive.Tag?.ToString() ?? ""));
            if (ChkDropbox.IsChecked == true) packageList.Add(Tuple.Create("Dropbox", ChkDropbox.Tag?.ToString() ?? ""));
            if (ChkQbit.IsChecked == true) packageList.Add(Tuple.Create("qBittorrent", ChkQbit.Tag?.ToString() ?? ""));
            if (ChkTransmission.IsChecked == true) packageList.Add(Tuple.Create("Transmission", ChkTransmission.Tag?.ToString() ?? ""));
            if (ChkFileZilla.IsChecked == true) packageList.Add(Tuple.Create("FileZilla", ChkFileZilla.Tag?.ToString() ?? ""));

            // Tab 2
            if (ChkWinRar.IsChecked == true) packageList.Add(Tuple.Create("WinRAR", ChkWinRar.Tag?.ToString() ?? ""));
            if (Chk7Zip.IsChecked == true) packageList.Add(Tuple.Create("7-Zip", Chk7Zip.Tag?.ToString() ?? ""));
            if (ChkWizTree.IsChecked == true) packageList.Add(Tuple.Create("WizTree", ChkWizTree.Tag?.ToString() ?? ""));
            if (ChkTreeSize.IsChecked == true) packageList.Add(Tuple.Create("TreeSize", ChkTreeSize.Tag?.ToString() ?? ""));
            if (ChkCrystalDisk.IsChecked == true) packageList.Add(Tuple.Create("CrystalDiskInfo", ChkCrystalDisk.Tag?.ToString() ?? ""));
            if (ChkCrystalMark.IsChecked == true) packageList.Add(Tuple.Create("CrystalDiskMark", ChkCrystalMark.Tag?.ToString() ?? ""));
            if (ChkCpuZ.IsChecked == true) packageList.Add(Tuple.Create("CPU-Z", ChkCpuZ.Tag?.ToString() ?? ""));
            if (ChkGpuZ.IsChecked == true) packageList.Add(Tuple.Create("GPU-Z", ChkGpuZ.Tag?.ToString() ?? ""));
            if (ChkHwMonitor.IsChecked == true) packageList.Add(Tuple.Create("HWMonitor", ChkHwMonitor.Tag?.ToString() ?? ""));
            if (ChkAida64.IsChecked == true) packageList.Add(Tuple.Create("AIDA64 Extreme", ChkAida64.Tag?.ToString() ?? ""));
            if (ChkAnyDesk.IsChecked == true) packageList.Add(Tuple.Create("AnyDesk", ChkAnyDesk.Tag?.ToString() ?? ""));
            if (ChkTeamViewer.IsChecked == true) packageList.Add(Tuple.Create("TeamViewer", ChkTeamViewer.Tag?.ToString() ?? ""));
            if (ChkRufus.IsChecked == true) packageList.Add(Tuple.Create("Rufus", ChkRufus.Tag?.ToString() ?? ""));
            if (ChkBleachBit.IsChecked == true) packageList.Add(Tuple.Create("BleachBit", ChkBleachBit.Tag?.ToString() ?? ""));
            if (ChkMalwarebytes.IsChecked == true) packageList.Add(Tuple.Create("Malwarebytes", ChkMalwarebytes.Tag?.ToString() ?? ""));
            if (ChkAdwCleaner.IsChecked == true) packageList.Add(Tuple.Create("AdwCleaner", ChkAdwCleaner.Tag?.ToString() ?? ""));

            // Tab 3
            if (ChkAdobe.IsChecked == true) packageList.Add(Tuple.Create("Adobe Reader", ChkAdobe.Tag?.ToString() ?? ""));
            if (ChkSumatra.IsChecked == true) packageList.Add(Tuple.Create("SumatraPDF", ChkSumatra.Tag?.ToString() ?? ""));
            if (ChkPdf24.IsChecked == true) packageList.Add(Tuple.Create("PDF24 Creator", ChkPdf24.Tag?.ToString() ?? ""));
            if (ChkLibreOffice.IsChecked == true) packageList.Add(Tuple.Create("LibreOffice", ChkLibreOffice.Tag?.ToString() ?? ""));
            if (ChkThunderbird.IsChecked == true) packageList.Add(Tuple.Create("Thunderbird", ChkThunderbird.Tag?.ToString() ?? ""));
            if (ChkWpsOffice.IsChecked == true) packageList.Add(Tuple.Create("WPS Office", ChkWpsOffice.Tag?.ToString() ?? ""));
            if (ChkVlc.IsChecked == true) packageList.Add(Tuple.Create("VLC Media Player", ChkVlc.Tag?.ToString() ?? ""));
            if (ChkPotPlayer.IsChecked == true) packageList.Add(Tuple.Create("PotPlayer", ChkPotPlayer.Tag?.ToString() ?? ""));
            if (ChkKLite.IsChecked == true) packageList.Add(Tuple.Create("K-Lite Codec Pack Mega", ChkKLite.Tag?.ToString() ?? ""));
            if (ChkSpotify.IsChecked == true) packageList.Add(Tuple.Create("Spotify", ChkSpotify.Tag?.ToString() ?? ""));
            if (ChkAudacity.IsChecked == true) packageList.Add(Tuple.Create("Audacity", ChkAudacity.Tag?.ToString() ?? ""));
            if (ChkHandbrake.IsChecked == true) packageList.Add(Tuple.Create("HandBrake", ChkHandbrake.Tag?.ToString() ?? ""));
            if (ChkShareX.IsChecked == true) packageList.Add(Tuple.Create("ShareX", ChkShareX.Tag?.ToString() ?? ""));
            if (ChkObs.IsChecked == true) packageList.Add(Tuple.Create("OBS Studio", ChkObs.Tag?.ToString() ?? ""));
            if (ChkGimp.IsChecked == true) packageList.Add(Tuple.Create("GIMP", ChkGimp.Tag?.ToString() ?? ""));
            if (ChkInkscape.IsChecked == true) packageList.Add(Tuple.Create("Inkscape", ChkInkscape.Tag?.ToString() ?? ""));
            if (ChkBlender.IsChecked == true) packageList.Add(Tuple.Create("Blender", ChkBlender.Tag?.ToString() ?? ""));

            // Tab 4
            if (ChkVsCode.IsChecked == true) packageList.Add(Tuple.Create("VS Code", ChkVsCode.Tag?.ToString() ?? ""));
            if (ChkGit.IsChecked == true) packageList.Add(Tuple.Create("Git", ChkGit.Tag?.ToString() ?? ""));
            if (ChkNotepad.IsChecked == true) packageList.Add(Tuple.Create("Notepad++", ChkNotepad.Tag?.ToString() ?? ""));
            if (ChkSublime.IsChecked == true) packageList.Add(Tuple.Create("Sublime Text", ChkSublime.Tag?.ToString() ?? ""));
            if (ChkPython.IsChecked == true) packageList.Add(Tuple.Create("Python 3", ChkPython.Tag?.ToString() ?? ""));
            if (ChkNode.IsChecked == true) packageList.Add(Tuple.Create("Node.js LTS", ChkNode.Tag?.ToString() ?? ""));
            if (ChkIntelliJ.IsChecked == true) packageList.Add(Tuple.Create("IntelliJ IDEA", ChkIntelliJ.Tag?.ToString() ?? ""));
            if (ChkEclipse.IsChecked == true) packageList.Add(Tuple.Create("Eclipse IDE", ChkEclipse.Tag?.ToString() ?? ""));
            if (ChkWinSCP.IsChecked == true) packageList.Add(Tuple.Create("WinSCP", ChkWinSCP.Tag?.ToString() ?? ""));
            if (ChkPutty.IsChecked == true) packageList.Add(Tuple.Create("PuTTY", ChkPutty.Tag?.ToString() ?? ""));
            if (ChkDBeaver.IsChecked == true) packageList.Add(Tuple.Create("DBeaver CE", ChkDBeaver.Tag?.ToString() ?? ""));
            if (ChkHeidi.IsChecked == true) packageList.Add(Tuple.Create("HeidiSQL", ChkHeidi.Tag?.ToString() ?? ""));
            if (ChkPostman.IsChecked == true) packageList.Add(Tuple.Create("Postman", ChkPostman.Tag?.ToString() ?? ""));
            if (ChkVirtualBox.IsChecked == true) packageList.Add(Tuple.Create("VirtualBox", ChkVirtualBox.Tag?.ToString() ?? ""));
            if (ChkDocker.IsChecked == true) packageList.Add(Tuple.Create("Docker Desktop", ChkDocker.Tag?.ToString() ?? ""));

            // Tab 5
            if (ChkDiscord.IsChecked == true) packageList.Add(Tuple.Create("Discord", ChkDiscord.Tag?.ToString() ?? ""));
            if (ChkTelegram.IsChecked == true) packageList.Add(Tuple.Create("Telegram Desktop", ChkTelegram.Tag?.ToString() ?? ""));
            if (ChkSlack.IsChecked == true) packageList.Add(Tuple.Create("Slack", ChkSlack.Tag?.ToString() ?? ""));
            if (ChkZoom.IsChecked == true) packageList.Add(Tuple.Create("Zoom", ChkZoom.Tag?.ToString() ?? ""));
            if (ChkTeams.IsChecked == true) packageList.Add(Tuple.Create("Microsoft Teams", ChkTeams.Tag?.ToString() ?? ""));
            if (ChkSkype.IsChecked == true) packageList.Add(Tuple.Create("Skype", ChkSkype.Tag?.ToString() ?? ""));
            if (ChkSteam.IsChecked == true) packageList.Add(Tuple.Create("Steam", ChkSteam.Tag?.ToString() ?? ""));
            if (ChkEpic.IsChecked == true) packageList.Add(Tuple.Create("Epic Games Launcher", ChkEpic.Tag?.ToString() ?? ""));
            if (ChkEA.IsChecked == true) packageList.Add(Tuple.Create("EA App", ChkEA.Tag?.ToString() ?? ""));
            if (ChkGog.IsChecked == true) packageList.Add(Tuple.Create("GOG Galaxy", ChkGog.Tag?.ToString() ?? ""));
            if (ChkUbisoft.IsChecked == true) packageList.Add(Tuple.Create("Ubisoft Connect", ChkUbisoft.Tag?.ToString() ?? ""));

            if (packageList.Count == 0)
            {
                MessageBox.Show("Lütfen kurulacak en az bir uygulama seçin.", "Seçim Yapılmadı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnStartDeploy.IsEnabled = false;
            TxtLogs.Text = $"[{DateTime.Now:HH:mm:ss}] Çevrimiçi Dağıtım Başlatıldı...\n";

            await Task.Run(async () =>
            {
                foreach (var package in packageList)
                {
                    await InstallWingetPackage(package.Item1, package.Item2);
                }
                AppendLog("Çevrimiçi dağıtım tamamlandı! 🚀");
            });

            BtnStartDeploy.IsEnabled = true;
        }

        private async Task InstallWingetPackage(string appName, string packageId)
        {
            AppendLog($"\n{appName} ({packageId}) yükleniyor...");

            try
            {
                var psi = new ProcessStartInfo
                    {
                        FileName = "winget",
                        Arguments = $"install --id {packageId} --silent --accept-source-agreements --accept-package-agreements",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                using (var process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        while (!process.StandardOutput.EndOfStream)
                        {
                            string? line = await process.StandardOutput.ReadLineAsync();
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                string cleanLine = line.Trim();
                                if (!IsWingetProgressSpam(cleanLine))
                                {
                                    AppendLog($"[winget] {cleanLine}");
                                }
                            }
                        }

                        process.WaitForExit();

                        if (process.ExitCode == 0)
                        {
                            AppendLog($"{appName} başarıyla kuruldu.");
                        }
                        else
                        {
                            AppendLog($"{appName} yüklemesi tamamlanamadı. (Kod: {process.ExitCode})");
                        }
                    }
                    else
                    {
                        AppendLog("İşlem başlatılamadı.");
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"{appName} hatası: {ex.Message}");
            }
        }

        private bool IsWingetProgressSpam(string line)
        {
            if (string.IsNullOrEmpty(line)) return true;

            // Filter single character spinner animations
            if (line == "/" || line == "\\" || line == "|" || line == "-") return true;

            // Filter Unicode progress bar blocks (and their corrupted UTF-8 encodings)
            if (line.Contains("â–") || line.Contains("█") || line.Contains("░") || line.Contains("▒") || line.Contains("▓")) return true;

            // Filter pure numeric progress indicators (e.g. "34%")
            if (System.Text.RegularExpressions.Regex.IsMatch(line, @"^\s*\d+%\s*$")) return true;

            return false;
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
