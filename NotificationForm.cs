using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SpeedInstaller
{
    public class NotificationForm : Form
    {
        private System.Windows.Forms.Timer fadeTimer;
        private double opacityTarget = 1.0;
        private Label lblTitle;
        private Label lblMessage;
        private Button btnOk;
        private Panel pnlHeader;
        
        public NotificationForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(450, 260);
            this.BackColor = Color.FromArgb(18, 18, 24); // Dark space/purple-black background
            this.Opacity = 0.0;
            this.ShowInTaskbar = true;
            this.TopMost = true;

            // Draw a subtle border and rounded corners
            this.Paint += NotificationForm_Paint;

            // Header Panel with accent gradient
            pnlHeader = new Panel
            {
                Size = new Size(this.Width, 6),
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(79, 172, 254) // Start of gradient color
            };
            pnlHeader.Paint += (s, e) =>
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    pnlHeader.ClientRectangle,
                    Color.FromArgb(79, 172, 254), // Cyan
                    Color.FromArgb(0, 242, 254),  // Neon Cyan/Teal
                    0F))
                {
                    e.Graphics.FillRectangle(brush, pnlHeader.ClientRectangle);
                }
            };

            // Custom Checkmark Control (Drawn using GDI+)
            Panel pnlIcon = new Panel
            {
                Size = new Size(60, 60),
                Location = new Point((this.Width - 60) / 2, 30),
                BackColor = Color.Transparent
            };
            pnlIcon.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                
                // Draw circular background
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(20, 0, 242, 254)))
                {
                    e.Graphics.FillEllipse(brush, 2, 2, 56, 56);
                }

                // Draw circle outline
                using (Pen pen = new Pen(Color.FromArgb(0, 242, 254), 2f))
                {
                    e.Graphics.DrawEllipse(pen, 2, 2, 56, 56);
                }

                // Draw checkmark
                using (Pen checkPen = new Pen(Color.FromArgb(0, 242, 254), 4f))
                {
                    checkPen.StartCap = LineCap.Round;
                    checkPen.EndCap = LineCap.Round;
                    
                    // First line of checkmark
                    e.Graphics.DrawLine(checkPen, 18, 30, 26, 38);
                    // Second line of checkmark
                    e.Graphics.DrawLine(checkPen, 26, 38, 42, 20);
                }
            };

            // Title Label
            lblTitle = new Label
            {
                Text = "BAŞARILI",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 242, 254),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(10, 100),
                Size = new Size(this.Width - 20, 25),
                BackColor = Color.Transparent
            };

            // Message Label
            lblMessage = new Label
            {
                Text = "İşlem Tamamlandı:\nProgramlar kuruldu ve sistem optimize edildi!",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(200, 200, 210),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, 135),
                Size = new Size(this.Width - 40, 50),
                BackColor = Color.Transparent
            };

            // Button Flat Design
            btnOk = new Button
            {
                Text = "Tamam",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(25, 25, 35),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 36),
                Location = new Point((this.Width - 120) / 2, 195),
                Cursor = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderSize = 1;
            btnOk.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 90);
            btnOk.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 40, 60);
            btnOk.FlatAppearance.MouseDownBackColor = Color.FromArgb(15, 15, 25);
            btnOk.Click += (s, e) => { this.Close(); };

            // Add Controls
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlIcon);
            this.Controls.Add(lblTitle);
            this.Controls.Add(lblMessage);
            this.Controls.Add(btnOk);

            // Fade in animation timer
            fadeTimer = new System.Windows.Forms.Timer
            {
                Interval = 10
            };
            fadeTimer.Tick += FadeTimer_Tick;
            this.Load += (s, e) => { fadeTimer.Start(); };
        }

        private void FadeTimer_Tick(object? sender, EventArgs e)
        {
            if (this.Opacity < opacityTarget)
            {
                this.Opacity += 0.05;
            }
            else
            {
                fadeTimer.Stop();
            }
        }

        private void NotificationForm_Paint(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw a stylish dark border
            using (Pen borderPen = new Pen(Color.FromArgb(40, 40, 50), 1))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);
            }
        }
    }
}
