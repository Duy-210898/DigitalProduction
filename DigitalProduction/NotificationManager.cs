using System;
using System.Drawing;
using System.Windows.Forms;

public class NotificationManager
{
    private static NotifyIcon notifyIcon;

    static NotificationManager()
    {
        notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Information,
            Visible = true
        };

        // Tạo menu chuột phải
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Open", null, (sender, e) => MessageBox.Show("Open clicked"));

        notifyIcon.ContextMenuStrip = contextMenu;
    }

    public static void ShowNotification(string title, string message)
    {
        if (notifyIcon != null)
        {
            notifyIcon.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
        }
    }

    public static void HideNotification()
    {
        if (notifyIcon != null)
        {
            notifyIcon.Visible = false;
        }
    }
}
