using DevExpress.LookAndFeel;
using DevExpress.Skins;
using DevExpress.UserSkins;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DigitalProduction
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Enable visual styles for the application
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Task.Run(async () =>
            {
                try
                {
                    await WebSocketClient.Instance.Connect("ws://10.30.4.121:8000");
                    Console.WriteLine("Connected to WebSocket server successfully.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to connect to WebSocket server: {ex.Message}");
                }
            }).Wait();

            Application.Run(new frmMain());
        }
    }
}
