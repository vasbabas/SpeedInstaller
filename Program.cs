using System;
using System.Windows.Forms;

namespace SpeedInstaller
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Run the main graphical user interface
            Application.Run(new MainForm());
        }
    }
}
