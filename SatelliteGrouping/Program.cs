using System;
using System.Windows.Forms;
using SatelliteGrouping.Views;

namespace SatelliteGrouping
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}