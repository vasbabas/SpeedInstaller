using System;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WDOS.Modules;

namespace WDOS
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer? _statusTimer;

        // Win32 API structure for memory status
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;

            public MEMORYSTATUSEX()
            {
                dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Load physical specifications
            LoadSystemSpecs();

            // Set up background timer for status updates
            _statusTimer = new DispatcherTimer();
            _statusTimer.Interval = TimeSpan.FromSeconds(2);
            _statusTimer.Tick += StatusTimer_Tick;
            _statusTimer.Start();

            // Perform initial tick
            UpdateDiagnostics();

            // Default load
            LoadViewFor("BtnOffline");
        }

        private async void LoadSystemSpecs()
        {
            var specs = await Task.Run(() =>
            {
                string osName = "Windows OS";
                string cpuName = "Genuine Intel / AMD";
                ulong totalRamBytes = 0;

                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem"))
                    using (var collection = searcher.Get())
                    {
                        foreach (var obj in collection)
                        {
                            osName = obj["Caption"]?.ToString() ?? osName;
                            break;
                        }
                    }

                    using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor"))
                    using (var collection = searcher.Get())
                    {
                        foreach (var obj in collection)
                        {
                            cpuName = obj["Name"]?.ToString() ?? cpuName;
                            break;
                        }
                    }
                    cpuName = cpuName.Trim();
                }
                catch
                {
                    // Fail-safe default
                }

                try
                {
                    var mem = new MEMORYSTATUSEX();
                    if (GlobalMemoryStatusEx(mem))
                    {
                        totalRamBytes = mem.ullTotalPhys;
                    }
                }
                catch { }

                return new { osName, cpuName, totalRamBytes };
            });

            TxtStatusOs.Text = specs.osName;
        }

        private void StatusTimer_Tick(object? sender, EventArgs e)
        {
            UpdateDiagnostics();
        }

        private async void UpdateDiagnostics()
        {
            // Retrieve CPU and Memory updates on a background task to prevent UI stutter
            var diag = await Task.Run(() =>
            {
                int cpuUsage = 0;
                double totalRam = 0;
                double usedRam = 0;
                int ramPct = 0;

                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT PercentProcessorTime FROM Win32_PerfFormattedData_PerfOS_Processor WHERE Name='_Total'"))
                    using (var collection = searcher.Get())
                    {
                        foreach (var obj in collection)
                        {
                            cpuUsage = Convert.ToInt32(obj["PercentProcessorTime"]);
                            break;
                        }
                    }
                }
                catch
                {
                    cpuUsage = 0;
                }

                try
                {
                    var mem = new MEMORYSTATUSEX();
                    if (GlobalMemoryStatusEx(mem))
                    {
                        totalRam = mem.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
                        usedRam = (mem.ullTotalPhys - mem.ullAvailPhys) / (1024.0 * 1024.0 * 1024.0);
                        ramPct = (int)mem.dwMemoryLoad;
                    }
                }
                catch { }

                return new { cpuUsage, totalRam, usedRam, ramPct };
            });

            TxtStatusCpu.Text = $"CPU: {diag.cpuUsage}%";
            TxtStatusRam.Text = $"RAM: {diag.usedRam:F1} / {diag.totalRam:F1} GB ({diag.ramPct}%)";
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton)
            {
                // Reset active tag on all buttons in the Sidebar
                ResetNavButtons();

                // Highlight clicked button
                clickedButton.Tag = "Active";

                // Route content based on selected panel
                string buttonName = clickedButton.Name;
                LoadViewFor(buttonName);
            }
        }

        private void ResetNavButtons()
        {
            BtnOffline.Tag = null;
            BtnOnline.Tag = null;
            BtnRepair.Tag = null;
            BtnHardware.Tag = null;
        }

        private void LoadViewFor(string navButtonName)
        {
            switch (navButtonName)
            {
                case "BtnOffline":
                    ActiveModulePresenter.Content = new OfflineView();
                    break;
                case "BtnOnline":
                    ActiveModulePresenter.Content = new OnlineView();
                    break;
                case "BtnRepair":
                    ActiveModulePresenter.Content = new RepairView();
                    break;
                case "BtnHardware":
                    ActiveModulePresenter.Content = new HardwareView();
                    break;
                default:
                    ActiveModulePresenter.Content = null;
                    break;
            }
        }
    }
}
