using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.LookAndFeel;
using DevExpress.XtraEditors;

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

           UserLookAndFeel.Default.SetSkinStyle("DevExpress Style"); 
           WindowsFormsSettings.DefaultFont = new System.Drawing.Font("Arial", 9);
            // Enable visual styles for the application
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Start WebSocket connection with retry logic
            Task.Run(() => ConnectWithRetry());

            // Run the main application form
            WindowsFormsSettings.ScrollUIMode = ScrollUIMode.Fluent;
            Application.Run(new frmLogin());
        }

        static async Task ConnectWithRetry()
        {
            int attempt = 0;
            int maxAttempts = 5;
            int delay = 2000; // Initial retry delay (2 seconds)

            while (attempt < maxAttempts)
            {
                try
                {
                    await WebSocketClient.Instance.Connect("ws://10.30.4.106:8000");
                    Console.WriteLine("Connected to WebSocket server successfully.");
                    return; // Exit loop when successful
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"WebSocket connection failed (Attempt {attempt + 1}): {ex.Message}");

                    if (attempt == maxAttempts - 1)
                    {
                        MessageBox.Show("Failed to connect after multiple attempts. Please check the server.", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return; // Stop retrying after max attempts
                    }

                    await Task.Delay(delay);
                    delay *= 2; // Exponential backoff (2s → 4s → 8s)
                    attempt++;
                }
            }
        }
    }
}
