using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.LookAndFeel;
using DevExpress.XtraEditors;

namespace DigitalProduction
{
    internal static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private const int SW_RESTORE = 9;

        [STAThread]
        static void Main()
        {
            bool createdNew;
            using (Mutex mutex = new Mutex(true, "DigitalProductionAppMutex", out createdNew))
            {
                if (!createdNew)
                {
                    // Find and bring the first instance to front
                    Process current = Process.GetCurrentProcess();
                    foreach (var process in Process.GetProcessesByName(current.ProcessName))
                    {
                        if (process.Id != current.Id)
                        {
                            ShowWindow(process.MainWindowHandle, SW_RESTORE);
                            SetForegroundWindow(process.MainWindowHandle);
                            break;
                        }
                    }

                    // Exit the new instance
                    return;
                }

                // ✅ Continue normal startup
                UserLookAndFeel.Default.SetSkinStyle("WXI");
                WindowsFormsSettings.DefaultFont = new System.Drawing.Font("Arial", 10);

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Connect WebSocket asynchronously
                Task.Run(() => ConnectWithRetry());

                WindowsFormsSettings.ScrollUIMode = ScrollUIMode.Fluent;
                Application.Run(new frmLogin());
            }
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
                    await WebSocketClient.Instance.Connect("ws://10.30.0.116:8000");
                    Console.WriteLine("Connected to WebSocket server successfully.");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"WebSocket connection failed (Attempt {attempt + 1}): {ex.Message}");

                    if (attempt == maxAttempts - 1)
                    {
                        MessageBox.Show("Failed to connect after multiple attempts. Please check the server.",
                            "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    await Task.Delay(delay);
                    delay *= 2; // Exponential backoff (2s → 4s → 8s)
                    attempt++;
                }
            }
        }
    }
}
