using System;
using System.IO;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WDOS.Modules
{
    public partial class HardwareView : UserControl
    {
        private string _osName = "Sorgulanmadı";
        private string _cpuName = "Sorgulanmadı";
        private string _ramText = "Sorgulanmadı";
        private string _gpuName = "Sorgulanmadı";
        private string _motherboardText = "Sorgulanmadı";
        private string _diskText = "Sorgulanmadı";

        public HardwareView()
        {
            InitializeComponent();
            Loaded += HardwareView_Loaded;
        }

        private async void HardwareView_Loaded(object sender, RoutedEventArgs e)
        {
            await RunHardwareScan();
        }

        private async void RefreshScan_Click(object sender, RoutedEventArgs e)
        {
            await RunHardwareScan();
        }

        private async Task RunHardwareScan()
        {
            AppendLog("WMI donanım taraması başlatılıyor...");
            TxtLogs.Text += "[WMI] Sistem bilgileri okunuyor...\n";

            await Task.Run(() =>
            {
                try
                {
                    // 1. Operating System
                    using (var searcher = new ManagementObjectSearcher("SELECT Caption, OSArchitecture FROM Win32_OperatingSystem"))
                    using (var collection = searcher.Get())
                    {
                        foreach (var obj in collection)
                        {
                            _osName = $"{obj["Caption"]} ({obj["OSArchitecture"]})";
                            break;
                        }
                    }

                    // 2. CPU
                    using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor"))
                    using (var collection = searcher.Get())
                    {
                        foreach (var obj in collection)
                        {
                            _cpuName = obj["Name"]?.ToString()?.Trim() ?? "Bilinmiyor";
                            break;
                        }
                    }

                    // 3. RAM
                    using (var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
                    using (var collection = searcher.Get())
                    {
                        foreach (var obj in collection)
                        {
                            ulong bytes = Convert.ToUInt64(obj["TotalPhysicalMemory"]);
                            double gb = bytes / (1024.0 * 1024.0 * 1024.0);
                            _ramText = $"{gb:F1} GB";
                            break;
                        }
                    }

                    // 4. GPU
                    using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController"))
                    using (var collection = searcher.Get())
                    {
                        var gpuList = new StringBuilder();
                        foreach (var obj in collection)
                        {
                            gpuList.AppendLine(obj["Name"]?.ToString());
                        }
                        _gpuName = gpuList.ToString().Trim();
                    }

                    // 5. Motherboard
                    using (var searcher = new ManagementObjectSearcher("SELECT Manufacturer, Product FROM Win32_BaseBoard"))
                    using (var collection = searcher.Get())
                    {
                        foreach (var obj in collection)
                        {
                            _motherboardText = $"{obj["Manufacturer"]} {obj["Product"]}";
                            break;
                        }
                    }

                    // 6. Disk Drive
                    using (var searcher = new ManagementObjectSearcher("SELECT Model, Size, Status FROM Win32_DiskDrive"))
                    using (var collection = searcher.Get())
                    {
                        var diskList = new StringBuilder();
                        foreach (var obj in collection)
                        {
                            ulong sizeBytes = Convert.ToUInt64(obj["Size"]);
                            double sizeGb = sizeBytes / (1024.0 * 1024.0 * 1024.0);
                            string model = obj["Model"]?.ToString() ?? "Bilinmeyen Disk";
                            string status = obj["Status"]?.ToString() ?? "OK";
                            diskList.AppendLine($"{model} ({sizeGb:F0} GB) - Sağlık: {status}");
                        }
                        _diskText = diskList.ToString().Trim();
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"WMI Hatası: {ex.Message}");
                }
            });

            // Update UI fields
            TxtOs.Text = _osName;
            TxtCpu.Text = _cpuName;
            TxtRam.Text = _ramText;
            TxtGpu.Text = _gpuName;
            TxtMotherboard.Text = _motherboardText;
            TxtDisk.Text = _diskText;

            AppendLog("WMI donanım taraması tamamlandı.");
        }

        private void CreateReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string reportPath = Path.Combine(desktopPath, "WDOS_Donanim_Raporu.html");

                string htmlContent = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>WDOS Donanım Tanılama Raporu</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: #0F172A;
            color: #F8FAFC;
            margin: 0;
            padding: 40px;
        }}
        .container {{
            max-width: 800px;
            margin: 0 auto;
            background-color: #1E293B;
            border-radius: 8px;
            padding: 30px;
            box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
            border: 1px solid #475569;
        }}
        h1 {{
            color: #0EA5E9;
            margin-top: 0;
            border-bottom: 2px solid #334155;
            padding-bottom: 10px;
        }}
        .info-row {{
            display: flex;
            padding: 12px 0;
            border-bottom: 1px solid #334155;
        }}
        .info-label {{
            width: 200px;
            font-weight: bold;
            color: #94A3B8;
        }}
        .info-value {{
            flex: 1;
            white-space: pre-line;
        }}
        .footer {{
            margin-top: 30px;
            text-align: center;
            font-size: 12px;
            color: #94A3B8;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>WDOS Donanım Tanılama Raporu</h1>
        <div class='info-row'>
            <div class='info-label'>Rapor Tarihi:</div>
            <div class='info-value'>{DateTime.Now:dd.MM.yyyy HH:mm:ss}</div>
        </div>
        <div class='info-row'>
            <div class='info-label'>İşletim Sistemi:</div>
            <div class='info-value'>{_osName}</div>
        </div>
        <div class='info-row'>
            <div class='info-label'>İşlemci (CPU):</div>
            <div class='info-value'>{_cpuName}</div>
        </div>
        <div class='info-row'>
            <div class='info-label'>Bellek (RAM):</div>
            <div class='info-value'>{_ramText}</div>
        </div>
        <div class='info-row'>
            <div class='info-label'>Ekran Kartı (GPU):</div>
            <div class='info-value'>{_gpuName}</div>
        </div>
        <div class='info-row'>
            <div class='info-label'>Anakart:</div>
            <div class='info-value'>{_motherboardText}</div>
        </div>
        <div class='info-row'>
            <div class='info-label'>Disk Sürücüleri:</div>
            <div class='info-value'>{_diskText}</div>
        </div>
        <div class='footer'>
            Windows Deployment & Optimization Suite (WDOS) v1.0.0
        </div>
    </div>
</body>
</html>";

                File.WriteAllText(reportPath, htmlContent, Encoding.UTF8);
                AppendLog($"Rapor başarıyla kaydedildi: {reportPath}");
                MessageBox.Show($"Donanım raporu masaüstüne kaydedildi:\n{reportPath}", "Rapor Oluşturuldu", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppendLog($"Rapor yazma hatası: {ex.Message}");
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
