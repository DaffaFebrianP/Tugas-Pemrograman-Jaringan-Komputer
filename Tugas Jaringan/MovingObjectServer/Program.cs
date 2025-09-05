using System;
using System.Windows.Forms;

namespace MovingObjectServer
{
    static class Program
    {
        /// <summary>
        /// Entry point aplikasi.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1()); // jalankan Form1
        }
    }
}
