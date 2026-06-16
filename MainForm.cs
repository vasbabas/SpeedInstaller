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
        private CheckBox chkVisualEffects;
        private CheckBox chkClearType;
        private CheckBox chkPowerPlan;
        private CheckBox chkSleepTimeout;
        private CheckBox chkVisualEffectBackup; // Unused but kept for reference
        private CheckBox chkWingetUpgrade;

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
                Text = "📦 PROGRAM KURUCU (Dinamik)",
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

            // Right Card: Windows Performance Tweaks
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

            chkVisualEffects = CreateStyledCheckBox("Görsel Efektleri Kapat (En İyi Performans)", 50, true);
            chkClearType = CreateStyledCheckBox("ClearType Yazı Düzeltmeyi Açık Bırak", 85, true);
            chkPowerPlan = CreateStyledCheckBox("Güç Planını 'Yüksek Performans' Yap", 120, true);
            chkSleepTimeout = CreateStyledCheckBox("Ekran Kapatma ve Uykuyu Devre Dışı Bırak", 155, true);
            chkWingetUpgrade = CreateStyledCheckBox("Winget ile Otomatik Paketleri Güncelle", 190, true);

            pnlRightTweak.Controls.Add(lblRightTitle);
            pnlRightTweak.Controls.Add(chkVisualEffects);
            pnlRightTweak.Controls.Add(chkClearType);
            pnlRightTweak.Controls.Add(chkPowerPlan);
            pnlRightTweak.Controls.Add(chkSleepTimeout);
            pnlRightTweak.Controls.Add(chkWingetUpgrade);

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

        private CheckBox CreateStyledCheckBox(string text, int top, bool Checked)
        {
            return new CheckBox
            {
                Text = text,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(200, 200, 210),
                Location = new Point(20, top),
                Size = new Size(330, 25),
                Checked = Checked,
                FlatStyle = FlatStyle.Flat
            };
        }

        private void ScanProgramlarDirectory()
        {
            flowSoftwareList.Controls.Clear();
            dynamicProgramCheckBoxes.Clear();

            try
            {
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
                            CheckBox chk = new CheckBox
                            {
                                Text = fileName,
                                Font = new Font("Segoe UI", 9.5F),
                                ForeColor = Color.FromArgb(220, 220, 230),
                                AutoSize = true,
                                Margin = new Padding(5, 5, 5, 5),
                                Checked = true,
                                FlatStyle = FlatStyle.Flat,
                                Tag = file // Store full path
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

            Console.WriteLine("[*] Optimizasyon ve Kurulum Islemi Baslatildi...");
            Console.WriteLine("--------------------------------------------------");

            try
            {
                // Calculate steps
                int totalSteps = 0;
                List<string> selectedInstallers = new List<string>();
                foreach (var chk in dynamicProgramCheckBoxes)
                {
                    if (chk.Checked && chk.Tag != null)
                    {
                        selectedInstallers.Add(chk.Tag.ToString()!);
                    }
                }

                totalSteps += selectedInstallers.Count;
                if (chkVisualEffects.Checked) totalSteps++;
                if (chkPowerPlan.Checked) totalSteps++;
                if (chkWingetUpgrade.Checked) totalSteps++;

                int currentStep = 0;
                Action incrementProgress = () =>
                {
                    currentStep++;
                    int val = (int)(((double)currentStep / totalSteps) * 100);
                    if (val > 100) val = 100;
                    this.Invoke((MethodInvoker)(() => prgProgress.Value = val));
                };

                // 1. Run Dynamic Installers
                if (selectedInstallers.Count > 0)
                {
                    Console.WriteLine("[*] Seçilen Programların Kurulumu Başlatılıyor...");
                    foreach (string file in selectedInstallers)
                    {
                        string name = Path.GetFileName(file);
                        Console.WriteLine($"[*] Yükleniyor (Arayüz görünür durumda): {name}...");
                        
                        await Task.Run(() => RunProcess(file, ""));
                        Console.WriteLine($"[+] Kurulum penceresi açıldı/tamamlandı: {name}");
                        incrementProgress();
                    }
                }

                // 2. Performance Tuning
                if (chkVisualEffects.Checked)
                {
                    Console.WriteLine("[*] Görsel Performans Ayarları Yapılandırılıyor...");
                    await Task.Run(() => ApplyPerformanceSettings(chkClearType.Checked));
                    Console.WriteLine("[+] Görsel ayarlar optimize edildi.");
                    incrementProgress();
                }

                // 3. Power settings
                if (chkPowerPlan.Checked)
                {
                    Console.WriteLine("[*] Güç Yönetimi Yapılandırılıyor...");
                    await Task.Run(() => ApplyPowerSettings(chkSleepTimeout.Checked));
                    Console.WriteLine("[+] Güç planı ayarları tamamlandı.");
                    incrementProgress();
                }

                // 4. Winget updates
                if (chkWingetUpgrade.Checked)
                {
                    Console.WriteLine("[*] Güncelleştirmeler denetleniyor...");
                    await Task.Run(() => UpgradePackagesUsingWinget());
                    incrementProgress();
                }

                Console.WriteLine("--------------------------------------------------");
                Console.WriteLine("[+] TÜM İŞLEMLER BAŞARIYLA TAMAMLANDI!");
                System.Media.SystemSounds.Asterisk.Play();

                // Show success dialog
                var successForm = new NotificationForm();
                successForm.ShowDialog(this);
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
            }
        }

        private void RunProcess(string fileName, string arguments)
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[-] İşlem başlatılamadı ({Path.GetFileName(fileName)}): {ex.Message}");
            }
        }

        private void ApplyPerformanceSettings(bool keepFontSmoothing)
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

                using (RegistryKey? advancedKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true))
                {
                    if (advancedKey != null)
                    {
                        advancedKey.SetValue("TaskbarAnimations", 0, RegistryValueKind.DWord);
                        advancedKey.SetValue("ListviewAlphaSelect", 0, RegistryValueKind.DWord);
                        advancedKey.SetValue("ListviewShadow", 0, RegistryValueKind.DWord);
                    }
                }

                SystemParametersInfo(SPI_SETFONTSMOOTHING, keepFontSmoothing ? 1u : 0u, IntPtr.Zero, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
                SystemParametersInfo(SPI_SETDRAGFULLWINDOWS, 0, IntPtr.Zero, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);

                IntPtr result;
                SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, "Registry", SMTO_ABORTIFHUNG, 5000, out result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[-] Görsel efekt optimizasyon hatası: {ex.Message}");
            }
        }

        private void ApplyPowerSettings(bool disableSleep)
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[-] Güç ayarı hatası: {ex.Message}");
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

        private void UpgradePackagesUsingWinget()
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
                        // Read winget output lines and write them to our console redirection in real-time
                        while (!p.StandardOutput.EndOfStream)
                        {
                            string? line = p.StandardOutput.ReadLine();
                            if (line != null) Console.WriteLine($"   [Winget]: {line}");
                        }
                        p.WaitForExit();
                    }
                    Console.WriteLine("[+] Winget güncelleştirmeleri denetlendi.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[-] Winget çalıştırma hatası: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("[-] İnternet bağlantısı yok. Güncelleme denetimi atlanıyor.");
            }
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

        // GUI paint methods for smooth visual design
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

    // Helper class to redirect Console.Out to our RichTextBox control
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
