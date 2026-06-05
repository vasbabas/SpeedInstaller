using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WDOS.Modules
{
    public partial class RepairView : UserControl
    {
        public RepairView()
        {
            InitializeComponent();
        }

        private async void BtnSfc_Click(object sender, RoutedEventArgs e)
        {
            ResetProgress("SFC Scannow");
            await RunRepairCommand("SFC Scannow", "sfc", "/scannow");
        }

        private async void BtnDism_Click(object sender, RoutedEventArgs e)
        {
            ResetProgress("DISM Restore Health");
            await RunRepairCommand("DISM Restore Health", "DISM.exe", "/Online /Cleanup-Image /RestoreHealth");
        }

        private async void BtnNetReset_Click(object sender, RoutedEventArgs e)
        {
            ResetProgress("Ağ Sıfırlama");
            await RunRepairCommand("Ağ Sıfırlama", "netsh", "winsock reset");
        }

        private async void BtnDnsFlush_Click(object sender, RoutedEventArgs e)
        {
            ResetProgress("DNS Önbellek Temizleme");
            await RunRepairCommand("DNS Önbellek Temizleme", "ipconfig", "/flushdns");
        }

        private void ResetProgress(string taskName)
        {
            BarProgress.Value = 0;
            TxtProgressPercent.Text = "%0";
            TxtProgressStatus.Text = $"Durum: {taskName} başlatılıyor...";
        }

        private async Task RunRepairCommand(string friendlyName, string fileName, string arguments)
        {
            ToggleButtons(false);
            AppendLog($"\n=== [{friendlyName}] Başlatıldı ===");

            await Task.Run(async () =>
            {
                try
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
                        if (process != null)
                        {
                            while (!process.StandardOutput.EndOfStream)
                            {
                                string? line = await process.StandardOutput.ReadLineAsync();
                                if (!string.IsNullOrWhiteSpace(line))
                                {
                                    ParseOutputLine(line.Trim(), friendlyName);
                                }
                            }

                            process.WaitForExit();
                            
                            Dispatcher.Invoke(() =>
                            {
                                BarProgress.Value = 100;
                                TxtProgressPercent.Text = "%100";
                                TxtProgressStatus.Text = $"Durum: {friendlyName} tamamlandı.";
                            });

                            AppendLog($"=== [{friendlyName}] Tamamlandı (Çıkış Kodu: {process.ExitCode}) ===");
                        }
                        else
                        {
                            AppendLog($"İşlem başlatılamadı: {fileName}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"İşlem hatası ({friendlyName}): {ex.Message}");
                }
            });

            ToggleButtons(true);
        }

        private void ParseOutputLine(string line, string friendlyName)
        {
            // Regex to find percentages like "34%" or "%34" or "24.5%"
            var match = Regex.Match(line, @"(\d+(\.\d+)?)\s*%|%\s*(\d+(\.\d+)?)");
            if (match.Success)
            {
                string rawVal = !string.IsNullOrEmpty(match.Groups[1].Value) ? match.Groups[1].Value : match.Groups[3].Value;
                if (double.TryParse(rawVal, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double percentage))
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        BarProgress.Value = percentage;
                        TxtProgressPercent.Text = $"%{percentage:F0}";
                        TxtProgressStatus.Text = $"İşlem: {friendlyName} yürütülüyor...";
                    }));
                    return; // Skip logging repetitive verification progress updates
                }
            }

            // Normal informative logs
            AppendLog(line);
        }

        private void ToggleButtons(bool enabled)
        {
            BtnSfc.IsEnabled = enabled;
            BtnDism.IsEnabled = enabled;
            BtnNetReset.IsEnabled = enabled;
            BtnDnsFlush.IsEnabled = enabled;
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
