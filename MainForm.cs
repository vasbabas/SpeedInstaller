using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SpeedInstaller
{
    public class MainForm : Form
    {
        // Win32 API imports for drag-and-drop window and console redirection
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

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

        // UI Controls
        private Panel pnlHeader;
        private Label lblHeaderTitle;
        private Button btnClose;
        private Button btnMinimize;
        
        private Panel pnlLeftSoftware;
        private Label lblLeftTitle;
        private FlowLayoutPanel flowSoftwareList;
        private Label lblNoSoftware;

        private Panel pnlRightTweak;
        private Label lblRightTitle;
        private FlowLayoutPanel flowTweakList;

        // Custom Tweak Checkboxes
        private CheckBox chkVisualEffects;
        private CheckBox chkClearType;
        private CheckBox chkMenuDelay;
        private CheckBox chkResponsiveness;
        private CheckBox chkAutoEndTasks;
        private CheckBox chkGameDVR;
        private CheckBox chkTelemetry;
        private CheckBox chkHibernation;
        private CheckBox chkPowerPlan;
        private CheckBox chkSleepTimeout;
        private CheckBox chkWingetUpgrade;
        private CheckBox chkSysMain;
        private CheckBox chkWindowsSearch;
        private CheckBox chkGpuScheduling;
        private CheckBox chkNetworkTweak;
        private CheckBox chkLargeSystemCache;

        private RichTextBox rchLogs;
        private ProgressBar prgProgress;
        private Button btnStart;

        private List<CheckBox> dynamicProgramCheckBoxes = new List<CheckBox>();
        private string programlarDir = "";

        public MainForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(800, 580);
            this.BackColor = Color.FromArgb(13, 13, 17); // Premium dark space background
            this.Text = "SpeedInstaller & Windows Optimizer";

            programlarDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Programlar");
            if (!Directory.Exists(programlarDir))
            {
                Directory.CreateDirectory(programlarDir);
            }

            InitializeComponents();
            
            // Redirect Console.WriteLine to our RichTextBox
            Console.SetOut(new ControlWriter(rchLogs));

            // Load dynamic checkboxes from the Programlar directory
            ScanProgramlarDirectory();
        }

        private void InitializeComponents()
        {
            // Custom Title Bar
            pnlHeader = new Panel
            {
                Size = new Size(this.Width, 40),
                Location = new Point(0, 0),
                BackColor = Color.FromArgb(21, 21, 29)
            };
            pnlHeader.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            };

            lblHeaderTitle = new Label
            {
                Text = "🚀 SPEEDINSTALLER & WINDOWS OPTIMIZER",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 242, 254),
                Location = new Point(15, 10),
                AutoSize = true
            };

            btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 200, 200),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(30, 30),
                Location = new Point(this.Width - 40, 5),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 50, 50);
            btnClose.Click += (s, e) => this.Close();

            btnMinimize = new Button
            {
                Text = "—",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 200, 200),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(30, 30),
                Location = new Point(this.Width - 75, 5),
                Cursor = Cursors.Hand
            };
            btnMinimize.FlatAppearance.BorderSize = 0;
            btnMinimize.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 45, 60);
            btnMinimize.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

            pnlHeader.Controls.Add(lblHeaderTitle);
            pnlHeader.Controls.Add(btnClose);
            pnlHeader.Controls.Add(btnMinimize);

            // Left Card: Software Installer
            pnlLeftSoftware = new Panel
            {
                Size = new Size(370, 260),
                Location = new Point(20, 60),
                BackColor = Color.FromArgb(21, 21, 29)
            };
            pnlLeftSoftware.Paint += PaintCardBorder;

            lblLeftTitle = new Label
            {
                Text = "📦 PROGRAM KURUCU (Otomatik Tespit)",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 15),
                Size = new Size(340, 25)
            };

            flowSoftwareList = new FlowLayoutPanel
            {
                Location = new Point(15, 45),
                Size = new Size(340, 200),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            lblNoSoftware = new Label
            {
                Text = "Programlar klasöründe hiç .exe bulunamadı.\n(Dosyaları buraya atıp uygulamayı açın)",
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.FromArgb(140, 140, 150),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(320, 100),
                Location = new Point(10, 50),
                Visible = false
            };

            flowSoftwareList.Controls.Add(lblNoSoftware);
            pnlLeftSoftware.Controls.Add(lblLeftTitle);
            pnlLeftSoftware.Controls.Add(flowSoftwareList);

            // Right Card: Windows Performance Tweaks (Scrolled List)
            pnlRightTweak = new Panel
            {
                Size = new Size(370, 260),
                Location = new Point(410, 60),
                BackColor = Color.FromArgb(21, 21, 29)
            };
            pnlRightTweak.Paint += PaintCardBorder;

            lblRightTitle = new Label
            {
                Text = "⚡ SİSTEM OPTİMİZASYONLARI",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 15),
                Size = new Size(340, 25)
            };

            flowTweakList = new FlowLayoutPanel
            {
                Location = new Point(15, 45),
                Size = new Size(340, 200),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            // Build Tweak Checkboxes
            chkVisualEffects = CreateTweakCheckBox("Görsel Efektleri Kapat (Performans)");
            chkClearType = CreateTweakCheckBox("ClearType Kenar Düzeltmeyi Açık Bırak");
            chkMenuDelay = CreateTweakCheckBox("Menü Açılış Gecikmesini Sıfırla (0 ms)");
            chkResponsiveness = CreateTweakCheckBox("Sistem Tepkiselliğini İyileştir (Low Latency)");
            chkAutoEndTasks = CreateTweakCheckBox("Kilitlenen Programları Otomatik Sonlandır");
            chkGameDVR = CreateTweakCheckBox("Xbox Game DVR (Arka Plan Kaydı) Kapat");
            chkTelemetry = CreateTweakCheckBox("Windows Telemetri Veri Gönderimini Kapat");
            chkHibernation = CreateTweakCheckBox("Hazırda Bekletmeyi Kapat (SSD Boş Alan)");
            chkPowerPlan = CreateTweakCheckBox("Yüksek Performans Güç Planını Etkinleştir");
            chkSleepTimeout = CreateTweakCheckBox("Ekran Kapatma ve Uykuyu ASLA Yap");
            chkSysMain = CreateTweakCheckBox("SysMain (Superfetch) RAM Tasarrufu Hizmetini Kapat");
            chkWindowsSearch = CreateTweakCheckBox("Windows Search Dizin Oluşturucuyu Kapat");
            chkGpuScheduling = CreateTweakCheckBox("GPU Donanım Hızlandırmayı (HAGS) Etkinleştir");
            chkNetworkTweak = CreateTweakCheckBox("Ağ Bant Genişliği Limitini Kaldır (%20 Limit)");
            chkLargeSystemCache = CreateTweakCheckBox("Büyük Sistem Önnbelleğini Etkinleştir");
            chkWingetUpgrade = CreateTweakCheckBox("Winget ile Otomatik Paketleri Güncelle");

            flowTweakList.Controls.Add(chkVisualEffects);
            flowTweakList.Controls.Add(chkClearType);
            flowTweakList.Controls.Add(chkMenuDelay);
            flowTweakList.Controls.Add(chkResponsiveness);
            flowTweakList.Controls.Add(chkAutoEndTasks);
            flowTweakList.Controls.Add(chkGameDVR);
            flowTweakList.Controls.Add(chkTelemetry);
            flowTweakList.Controls.Add(chkHibernation);
            flowTweakList.Controls.Add(chkPowerPlan);
            flowTweakList.Controls.Add(chkSleepTimeout);
            flowTweakList.Controls.Add(chkSysMain);
            flowTweakList.Controls.Add(chkWindowsSearch);
            flowTweakList.Controls.Add(chkGpuScheduling);
            flowTweakList.Controls.Add(chkNetworkTweak);
            flowTweakList.Controls.Add(chkLargeSystemCache);
            flowTweakList.Controls.Add(chkWingetUpgrade);

            pnlRightTweak.Controls.Add(lblRightTitle);
            pnlRightTweak.Controls.Add(flowTweakList);

            // Logging Terminal
            rchLogs = new RichTextBox
            {
                Size = new Size(760, 160),
                Location = new Point(20, 340),
                BackColor = Color.FromArgb(10, 10, 14),
                ForeColor = Color.FromArgb(180, 255, 180),
                Font = new Font("Consolas", 9F),
                BorderStyle = BorderStyle.None,
                ReadOnly = true
            };

            // Progress Bar
            prgProgress = new ProgressBar
            {
                Size = new Size(590, 24),
                Location = new Point(20, 525),
                Style = ProgressBarStyle.Continuous,
                ForeColor = Color.FromArgb(0, 242, 254),
                BackColor = Color.FromArgb(30, 30, 40)
            };

            // Start Button
            btnStart = new Button
            {
                Text = "İŞLEMİ BAŞLAT",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(0, 114, 255),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(150, 40),
                Location = new Point(630, 517),
                Cursor = Cursors.Hand
            };
            btnStart.FlatAppearance.BorderSize = 0;
            btnStart.Click += BtnStart_Click;

            // Add all controls to Form
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlLeftSoftware);
            this.Controls.Add(pnlRightTweak);
            this.Controls.Add(rchLogs);
            this.Controls.Add(prgProgress);
            this.Controls.Add(btnStart);
            
            this.Paint += MainWindow_Paint;
        }

        private CheckBox CreateTweakCheckBox(string text)
        {
            return new CheckBox
            {
                Text = text,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(200, 200, 210),
                AutoSize = true,
                Margin = new Padding(5, 4, 5, 4),
                Checked = true,
                FlatStyle = FlatStyle.Flat
            };
        }

        private void ScanProgramlarDirectory()
        {
            flowSoftwareList.Controls.Clear();
            dynamicProgramCheckBoxes.Clear();

            try
            {
                HashSet<string> installedApps = GetInstalledApps();

                if (Directory.Exists(programlarDir))
                {
                    string[] exeFiles = Directory.GetFiles(programlarDir, "*.exe", SearchOption.TopDirectoryOnly);

                    if (exeFiles.Length == 0)
                    {
                        lblNoSoftware.Visible = true;
                        flowSoftwareList.Controls.Add(lblNoSoftware);
                    }
                    else
                    {
                        lblNoSoftware.Visible = false;
                        foreach (string file in exeFiles)
                        {
                            string fileName = Path.GetFileName(file);
                            
                            string matchedName = "";
                            bool isInstalled = IsProgramInstalled(file, installedApps, out matchedName);

                            string labelText = isInstalled ? $"{fileName} (Zaten Kurulu)" : fileName;
                            Color labelColor = isInstalled ? Color.FromArgb(120, 180, 120) : Color.FromArgb(220, 220, 230);

                            CheckBox chk = new CheckBox
                            {
                                Text = labelText,
                                Font = new Font("Segoe UI", 9.5F),
                                ForeColor = labelColor,
                                AutoSize = true,
                                Margin = new Padding(5, 5, 5, 5),
                                Checked = !isInstalled,
                                FlatStyle = FlatStyle.Flat,
                                Tag = new KeyValuePair<string, bool>(file, isInstalled)
                            };
                            flowSoftwareList.Controls.Add(chk);
                            dynamicProgramCheckBoxes.Add(chk);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[-] Programlar klasörü okuma hatası: {ex.Message}");
            }
        }

        private async void BtnStart_Click(object? sender, EventArgs e)
        {
            btnStart.Enabled = false;
            flowSoftwareList.Enabled = false;
            pnlRightTweak.Enabled = false;
            prgProgress.Value = 0;
            rchLogs.Clear();

            List<string> failedOperations = new List<string>();
            List<string> alreadyInstalledList = new List<string>();

            Console.WriteLine("[*] Optimizasyon ve Kurulum Islemi Baslatildi...");
            Console.WriteLine("--------------------------------------------------");

            // Gather installers
            List<string> selectedInstallers = new List<string>();
            foreach (var chk in dynamicProgramCheckBoxes)
            {
                if (chk.Tag is KeyValuePair<string, bool> pair)
                {
                    if (chk.Checked)
                    {
                        selectedInstallers.Add(pair.Key);
                    }
                    else if (pair.Value)
                    {
                        alreadyInstalledList.Add(Path.GetFileName(pair.Key));
                    }
                }
            }

            int totalSteps = 0;
            if (chkVisualEffects.Checked) totalSteps++;
            if (chkMenuDelay.Checked || chkResponsiveness.Checked || chkAutoEndTasks.Checked || chkGameDVR.Checked || chkTelemetry.Checked) totalSteps++;
            if (chkSysMain.Checked || chkWindowsSearch.Checked || chkGpuScheduling.Checked || chkNetworkTweak.Checked || chkLargeSystemCache.Checked) totalSteps++;
            if (chkPowerPlan.Checked) totalSteps++;
            if (chkHibernation.Checked) totalSteps++;
            totalSteps += selectedInstallers.Count;
            if (chkWingetUpgrade.Checked) totalSteps++;

            int currentStep = 0;
            Action incrementProgress = () =>
            {
                currentStep++;
                int val = (int)(((double)currentStep / totalSteps) * 100);
                if (val > 100) val = 100;
                this.Invoke((MethodInvoker)(() => prgProgress.Value = val));
            };

            try
            {
                // PHASE 1: PERFORMANCE TUNING (SYSTEM TWEAKS)
                Console.WriteLine("[*] Aşama 1: Performans ve Sistem Ayarları Yapılandırılıyor...");
                
                if (chkVisualEffects.Checked)
                {
                    Console.WriteLine("[*] Görsel Efektler Kapatılıyor (Özel Performans Şablonu)...");
                    bool ok = await Task.Run(() => ApplyPerformanceSettings(chkClearType.Checked));
                    if (!ok) failedOperations.Add("Görsel Efekt Optimizasyonu");
                    incrementProgress();
                }

                // Registry tweaks split by checkboxes
                if (chkMenuDelay.Checked || chkResponsiveness.Checked || chkAutoEndTasks.Checked || chkGameDVR.Checked || chkTelemetry.Checked)
                {
                    Console.WriteLine("[*] Seçilen Performans Kayıt Defteri Ayarları Uygulanıyor...");
                    bool ok = await Task.Run(() => ApplySelectedRegistryTweaks());
                    if (!ok) failedOperations.Add("Gelişmiş Kayıt Defteri Optimizasyonu");
                    incrementProgress();
                }

                // Extra performance tweaks
                if (chkSysMain.Checked || chkWindowsSearch.Checked || chkGpuScheduling.Checked || chkNetworkTweak.Checked || chkLargeSystemCache.Checked)
                {
                    Console.WriteLine("[*] Ek Performans ve Sistem Servis Ayarları Uygulanıyor...");
                    bool ok = await Task.Run(() => ApplyExtraPerformanceTweaks());
                    if (!ok) failedOperations.Add("Ek Performans Optimizasyonları");
                    incrementProgress();
                }

                if (chkPowerPlan.Checked)
                {
                    Console.WriteLine("[*] Güç Yönetimi Yapılandırılıyor...");
                    bool ok = await Task.Run(() => ApplyPowerSettings(chkSleepTimeout.Checked));
                    if (!ok) failedOperations.Add("Yüksek Performans Güç Şeması");
                    incrementProgress();
                }

                if (chkHibernation.Checked)
                {
                    Console.WriteLine("[*] Hazırda Bekletme Modu (Hibernasyon) Kapatılıyor...");
                    bool ok = await Task.Run(() => DisableHibernation());
                    if (!ok) failedOperations.Add("Hazırda Bekletmeyi Kapatma");
                    incrementProgress();
                }

                Console.WriteLine("[+] Performans ve Sistem optimizasyon ayarları tamamlandı.");
                MessageBox.Show("Görsel ve performans ayarları başarıyla tamamlandı.\n\nŞimdi program kurulumlarına geçiliyor...", "Aşama 1 Tamamlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // PHASE 2: PROGRAM INSTALLATIONS
                if (selectedInstallers.Count > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("[*] Aşama 2: Seçilen Programların Kurulumu Başlatılıyor...");
                    foreach (string file in selectedInstallers)
                    {
                        string name = Path.GetFileName(file);
                        Console.WriteLine($"[*] Yükleniyor (Arayüz görünür): {name}...");
                        
                        bool ok = await Task.Run(() => RunProcess(file, ""));
                        if (!ok) failedOperations.Add($"Program Kurulumu: {name}");
                        Console.WriteLine($"[+] Kurulum işlemi tetiklendi: {name}");
                        incrementProgress();
                    }
                    Console.WriteLine("[+] Seçilen programların kurulumları tamamlandı.");
                    MessageBox.Show("Seçilen programların kurulumları tamamlandı.\n\nŞimdi güncelleştirmeler (winget) kontrol edilecek...", "Aşama 2 Tamamlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                // PHASE 3: WINGET UPGRADES
                if (chkWingetUpgrade.Checked)
                {
                    Console.WriteLine();
                    Console.WriteLine("[*] Aşama 3: Güncelleştirmeler Denetleniyor...");
                    MessageBox.Show("Güncelleştirme kontrolü ve winget yükseltmeleri başlatılıyor...", "Aşama 3 Başlatılıyor", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    bool ok = await Task.Run(() => UpgradePackagesUsingWinget());
                    if (!ok) failedOperations.Add("Winget Paket Güncelleme");
                    
                    Console.WriteLine("[+] Winget paket güncelleştirmeleri tamamlandı.");
                    
                    // Specific premium notification for Winget updates completion
                    var wingetNotification = new NotificationForm("GÜNCELLEME TAMAMLANDI", "Winget ile sistemdeki paketlerin güncellenmesi tamamlandı!");
                    wingetNotification.ShowDialog(this);
                    
                    incrementProgress();
                }

                // FINISH SUMMARY
                Console.WriteLine();
                Console.WriteLine("--------------------------------------------------");
                Console.WriteLine("[+] TÜM İŞLEMLER TAMAMLANDI!");
                System.Media.SystemSounds.Asterisk.Play();

                if (alreadyInstalledList.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("[i] Zaten Kurulu Olduğu İçin Atlanan Programlar:");
                    foreach (var app in alreadyInstalledList)
                    {
                        Console.WriteLine($"   ➔ {app}");
                    }
                    Console.ResetColor();
                }

                if (failedOperations.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[!] Başarısız/Atlanan İşlemler Listesi:");
                    foreach (var err in failedOperations)
                    {
                        Console.WriteLine($"   ➔ {err}");
                    }
                    Console.ResetColor();

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("İşlem tamamlandı, ancak bazı adımlarda hata oluştu veya atlandı:\n");
                    foreach (var err in failedOperations)
                    {
                        sb.AppendLine($"• {err}");
                    }
                    if (alreadyInstalledList.Count > 0)
                    {
                        sb.AppendLine("\nZaten Kurulu Olduğu İçin Atlananlar:");
                        foreach (var app in alreadyInstalledList)
                        {
                            sb.AppendLine($"• {app}");
                        }
                    }
                    MessageBox.Show(sb.ToString(), "İşlem Tamamlandı (Hatalı/Eksik)", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[+] Bütün ayarlar ve kurulumlar sorunsuz bir şekilde tamamlandı.");
                    Console.ResetColor();

                    string successMessage = "Bütün optimizasyon ayarları ve kurulumlar sorunsuz bir şekilde tamamlandı!";
                    if (alreadyInstalledList.Count > 0)
                    {
                        successMessage += $"\n\nSistemde zaten kurulu olduğu için atlanan programlar:\n• {string.Join("\n• ", alreadyInstalledList)}";
                    }

                    MessageBox.Show(successMessage, "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Show custom premium success dialog
                    string finalNotificationMsg = "İşlem Tamamlandı:\nProgramlar kuruldu ve sistem optimize edildi!";
                    if (chkWingetUpgrade.Checked)
                    {
                        finalNotificationMsg = "İşlem Tamamlandı:\nProgramlar kuruldu, sistem optimize edildi ve güncelleştirmeler uygulandı!";
                    }
                    var successForm = new NotificationForm("BAŞARILI", finalNotificationMsg);
                    successForm.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Beklenmeyen hata: {ex.Message}");
            }
            finally
            {
                btnStart.Enabled = true;
                flowSoftwareList.Enabled = true;
                pnlRightTweak.Enabled = true;
                
                // Refresh scan to update status checkboxes
                ScanProgramlarDirectory();
            }
        }

        private bool RunProcess(string fileName, string arguments)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process? p = Process.Start(psi);
                p?.WaitForExit();
                return p == null || p.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[-] İşlem başlatılamadı ({Path.GetFileName(fileName)}): {ex.Message}");
                return false;
            }
        }

        private bool ApplyPerformanceSettings(bool keepFontSmoothing)
        {
            try
            {
                using (RegistryKey? visualKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects"))
                {
                    if (visualKey != null)
                    {
                        visualKey.SetValue("VisualFXSetting", 3, RegistryValueKind.DWord);
                    }
                }

                using (RegistryKey? desktopKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
                {
                    if (desktopKey != null)
                    {
                        // Performance mask that turns off animations, transitions, and shadows
                        byte[] performanceMask = new byte[] { 0x90, 0x12, 0x01, 0x80, 0x10, 0x00, 0x00, 0x00 };
                        desktopKey.SetValue("UserPreferencesMask", performanceMask, RegistryValueKind.Binary);
                        
                        if (keepFontSmoothing)
                        {
                            desktopKey.SetValue("FontSmoothing", "2", RegistryValueKind.String);
                            desktopKey.SetValue("FontSmoothingType", 2, RegistryValueKind.DWord);
                        }
                        else
                        {
                            desktopKey.SetValue("FontSmoothing", "0", RegistryValueKind.String);
                            desktopKey.SetValue("FontSmoothingType", 0, RegistryValueKind.DWord);
                        }
                        desktopKey.SetValue("DragFullWindows", "0", RegistryValueKind.String);
                    }
                }

                using (RegistryKey? minAnimKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop\WindowMetrics", true))
                {
                    if (minAnimKey != null)
                    {
                        minAnimKey.SetValue("MinAnimate", "0", RegistryValueKind.String);
                    }
                }

                using (RegistryKey? advancedKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true))
                {
                    if (advancedKey != null)
                    {
                        advancedKey.SetValue("TaskbarAnimations", 0, RegistryValueKind.DWord);
                        advancedKey.SetValue("ListviewAlphaSelect", 0, RegistryValueKind.DWord);
                        advancedKey.SetValue("ListviewShadow", 0, RegistryValueKind.DWord);
                        advancedKey.SetValue("IconsOnly", 1, RegistryValueKind.DWord);
                    }
                }

                string[] visualEffectsKeys = {
                    "AnimateMinMax", "ComboBoxAnimation", "CursorShadow", "DragFullWindows", "DropShadow",
                    "ListBoxAnimation", "ListviewAlphaSelect", "ListviewShadow", "MenuAnimation",
                    "SelectionFade", "TaskbarAnimations", "TooltipAnimation", "WebTemplates"
                };

                foreach (var keyName in visualEffectsKeys)
                {
                    using (RegistryKey? key = Registry.CurrentUser.CreateSubKey($@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects\{keyName}"))
                    {
                        key?.SetValue("AppliedValue", 0, RegistryValueKind.DWord);
                    }
                }

                using (RegistryKey? key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects\FontSmoothing"))
                {
                    key?.SetValue("AppliedValue", keepFontSmoothing ? 1 : 0, RegistryValueKind.DWord);
                }

                SystemParametersInfo(SPI_SETFONTSMOOTHING, keepFontSmoothing ? 1u : 0u, IntPtr.Zero, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
                SystemParametersInfo(SPI_SETDRAGFULLWINDOWS, 0, IntPtr.Zero, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);

                IntPtr result;
                SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, "Registry", SMTO_ABORTIFHUNG, 5000, out result);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[-] Görsel efekt optimizasyon hatası: {ex.Message}");
                return false;
            }
        }

        private bool ApplySelectedRegistryTweaks()
        {
            try
            {
                // 1. Menu Delay
                if (chkMenuDelay.Checked)
                {
                    using (RegistryKey? desktopKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
                    {
                        desktopKey?.SetValue("MenuShowDelay", "0", RegistryValueKind.String);
                    }
                    Console.WriteLine("[+] Menü açılış gecikmesi sıfırlandı.");
                }

                // 2. System Responsiveness
                if (chkResponsiveness.Checked)
                {
                    using (RegistryKey? sysProfileKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"))
                    {
                        sysProfileKey?.SetValue("SystemResponsiveness", 0, RegistryValueKind.DWord);
                    }
                    Console.WriteLine("[+] Sistem yanıt önceliği optimize edildi (Low Latency).");
                }

                // 3. Auto End Tasks
                if (chkAutoEndTasks.Checked)
                {
                    using (RegistryKey? desktopKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
                    {
                        if (desktopKey != null)
                        {
                            desktopKey.SetValue("AutoEndTasks", "1", RegistryValueKind.String);
                            desktopKey.SetValue("HungAppTimeout", "1000", RegistryValueKind.String);
                            desktopKey.SetValue("WaitToKillAppTimeout", "2000", RegistryValueKind.String);
                        }
                    }
                    Console.WriteLine("[+] Kilitlenen uygulamaları otomatik kapatma etkinleştirildi.");
                }

                // 4. Game DVR
                if (chkGameDVR.Checked)
                {
                    using (RegistryKey? gameDVRKey = Registry.CurrentUser.CreateSubKey(@"System\GameConfigStore"))
                    {
                        gameDVRKey?.SetValue("GameDVR_Enabled", 0, RegistryValueKind.DWord);
                    }
                    using (RegistryKey? xboxDVRKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\PolicyManager\default\ApplicationManagement\AllowGameDVR"))
                    {
                        xboxDVRKey?.SetValue("value", 0, RegistryValueKind.DWord);
                    }
                    Console.WriteLine("[+] Xbox Game DVR (Arka plan ekran kaydı) kapatıldı.");
                }

                // 5. Telemetry
                if (chkTelemetry.Checked)
                {
                    using (RegistryKey? telemetryKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection"))
                    {
                        telemetryKey?.SetValue("AllowTelemetry", 0, RegistryValueKind.DWord);
                    }
                    Console.WriteLine("[+] Windows arka plan telemetri gönderimi kapatıldı.");
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[-] Kayıt defteri ince optimizasyon hatası: {ex.Message}");
                return false;
            }
        }

        private bool DisableHibernation()
        {
            try
            {
                RunPowercfg("-h off");
                Console.WriteLine("[+] Hazırda bekletme (Hibernasyon) kapatıldı ve diskte alan açıldı.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[-] Hazırda bekletmeyi kapatma hatası: {ex.Message}");
                return false;
            }
        }

        private bool ApplyExtraPerformanceTweaks()
        {
            try
            {
                // 1. SysMain Service
                if (chkSysMain.Checked)
                {
                    RunProcessHidden("sc.exe", "config SysMain start= disabled");
                    RunProcessHidden("sc.exe", "stop SysMain");
                    Console.WriteLine("[+] SysMain (Superfetch) servisi durduruldu ve devre dışı bırakıldı.");
                }

                // 2. Windows Search Service
                if (chkWindowsSearch.Checked)
                {
                    RunProcessHidden("sc.exe", "config WSearch start= disabled");
                    RunProcessHidden("sc.exe", "stop WSearch");
                    Console.WriteLine("[+] Windows Search servisi durduruldu ve devre dışı bırakıldı.");
                }

                // 3. GPU Scheduling (HAGS)
                if (chkGpuScheduling.Checked)
                {
                    using (RegistryKey? key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers"))
                    {
                        key?.SetValue("HwSchMode", 2, RegistryValueKind.DWord);
                    }
                    Console.WriteLine("[+] Donanım Hızlandırmalı GPU Zamanlaması (HAGS) kayıt defterinde etkinleştirildi.");
                }

                // 4. QoS Bandwidth Limit
                if (chkNetworkTweak.Checked)
                {
                    using (RegistryKey? key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Psched"))
                    {
                        key?.SetValue("NonBestEffortLimit", 0, RegistryValueKind.DWord);
                    }
                    Console.WriteLine("[+] Ağ Bant Genişliği Limit Rezervi kaldırıldı (%0).");
                }

                // 5. Large System Cache
                if (chkLargeSystemCache.Checked)
                {
                    using (RegistryKey? key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"))
                    {
                        key?.SetValue("LargeSystemCache", 1, RegistryValueKind.DWord);
                    }
                    Console.WriteLine("[+] Büyük Sistem Önnbelleği (Large System Cache) etkinleştirildi.");
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[-] Ek performans optimizasyon hatası: {ex.Message}");
                return false;
            }
        }

        private bool RunProcessHidden(string fileName, string arguments)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process? p = Process.Start(psi);
                p?.WaitForExit();
                return p == null || p.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private bool ApplyPowerSettings(bool disableSleep)
        {
            try
            {
                RunPowercfg("-setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");

                if (disableSleep)
                {
                    RunPowercfg("-change monitor-timeout-ac 0");
                    RunPowercfg("-change monitor-timeout-dc 0");
                    RunPowercfg("-change standby-timeout-ac 0");
                    RunPowercfg("-change standby-timeout-dc 0");
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[-] Güç ayarı hatası: {ex.Message}");
                return false;
            }
        }

        private void RunPowercfg(string args)
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
            catch {}
        }

        private bool UpgradePackagesUsingWinget()
        {
            if (IsInternetAvailable())
            {
                Console.WriteLine("[+] İnternet bağlantısı aktif. Winget güncelleştirmeleri denetleniyor...");
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "winget",
                        Arguments = "upgrade --all --accept-source-agreements --accept-package-agreements",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    Process? p = Process.Start(psi);
                    if (p != null)
                    {
                        while (!p.StandardOutput.EndOfStream)
                        {
                            string? line = p.StandardOutput.ReadLine();
                            if (line != null) Console.WriteLine($"   [Winget]: {line}");
                        }
                        p.WaitForExit();
                        return p.ExitCode == 0;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[-] Winget çalıştırma hatası: {ex.Message}");
                    return false;
                }
            }
            else
            {
                Console.WriteLine("[-] İnternet bağlantısı yok. Güncelleme denetimi atlanıyor.");
            }
            return true;
        }

        private bool IsInternetAvailable()
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = ping.Send("8.8.8.8", 2000);
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        private HashSet<string> GetInstalledApps()
        {
            HashSet<string> installedApps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            string[] uninstallKeys = {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            foreach (var keyPath in uninstallKeys)
            {
                using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(keyPath))
                {
                    if (key != null)
                    {
                        foreach (string subkeyName in key.GetSubKeyNames())
                        {
                            using (RegistryKey? subkey = key.OpenSubKey(subkeyName))
                            {
                                string? displayName = subkey?.GetValue("DisplayName") as string;
                                if (!string.IsNullOrEmpty(displayName))
                                {
                                    installedApps.Add(displayName);
                                }
                            }
                        }
                    }
                }
            }

            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
            {
                if (key != null)
                {
                    foreach (string subkeyName in key.GetSubKeyNames())
                    {
                        using (RegistryKey? subkey = key.OpenSubKey(subkeyName))
                        {
                            string? displayName = subkey?.GetValue("DisplayName") as string;
                            if (!string.IsNullOrEmpty(displayName))
                            {
                                installedApps.Add(displayName);
                            }
                        }
                    }
                }
            }

            return installedApps;
        }

        private bool IsProgramInstalled(string installerPath, HashSet<string> installedApps, out string matchedName)
        {
            matchedName = "";
            try
            {
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(installerPath);
                string productName = info.ProductName ?? "";
                string fileDescription = info.FileDescription ?? "";
                string fileName = Path.GetFileNameWithoutExtension(installerPath);

                foreach (string app in installedApps)
                {
                    if (!string.IsNullOrEmpty(productName) && app.Contains(productName, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedName = app;
                        return true;
                    }
                    if (!string.IsNullOrEmpty(fileDescription) && app.Contains(fileDescription, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedName = app;
                        return true;
                    }
                    if (fileName.Contains("chrome", StringComparison.OrdinalIgnoreCase) && app.Contains("Chrome", StringComparison.OrdinalIgnoreCase))
                    {
                        matchedName = "Google Chrome";
                        return true;
                    }
                    if ((fileName.Contains("adobe", StringComparison.OrdinalIgnoreCase) || fileName.Contains("reader", StringComparison.OrdinalIgnoreCase)) && 
                        (app.Contains("Adobe Acrobat", StringComparison.OrdinalIgnoreCase) || app.Contains("Adobe Reader", StringComparison.OrdinalIgnoreCase)))
                    {
                        matchedName = "Adobe Acrobat Reader";
                        return true;
                    }
                    if (fileName.Contains("alpemix", StringComparison.OrdinalIgnoreCase) && app.Contains("Alpemix", StringComparison.OrdinalIgnoreCase))
                    {
                        matchedName = "Alpemix";
                        return true;
                    }
                }
            }
            catch {}
            return false;
        }

        private void MainWindow_Paint(object? sender, PaintEventArgs e)
        {
            using (Pen pen = new Pen(Color.FromArgb(30, 30, 40), 1))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            }
        }

        private void PaintCardBorder(object? sender, PaintEventArgs e)
        {
            Panel? card = sender as Panel;
            if (card != null)
            {
                using (Pen pen = new Pen(Color.FromArgb(40, 40, 55), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                }
            }
        }
    }

    public class ControlWriter : TextWriter
    {
        private readonly RichTextBox textbox;
        public ControlWriter(RichTextBox textbox)
        {
            this.textbox = textbox;
        }

        public override void Write(char value)
        {
            textbox.Invoke((MethodInvoker)(() =>
            {
                textbox.AppendText(value.ToString());
                textbox.SelectionStart = textbox.Text.Length;
                textbox.ScrollToCaret();
            }));
        }

        public override void Write(string? value)
        {
            if (value == null) return;
            textbox.Invoke((MethodInvoker)(() =>
            {
                textbox.AppendText(value);
                textbox.SelectionStart = textbox.Text.Length;
                textbox.ScrollToCaret();
            }));
        }

        public override Encoding Encoding => Encoding.UTF8;
    }
}
