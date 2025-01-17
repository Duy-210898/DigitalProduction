using System.Windows.Forms;

namespace DigitalProduction
{
    public static class ShowMessage
    {
        // Show information message
        public static void ShowInfo(string message, string caption = "Information")
        {
            MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        public static void ShowSuccessfully(string message, string caption = "Successfully")
        {
            MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        // Show warning message
        public static void ShowWarning(string message, string caption = "Warning")
        {
            MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // Show error message
        public static void ShowError(string message, string caption = "Error")
        {
            MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // Show question message with Yes/No buttons
        public static DialogResult ShowQuestion(string message, string caption = "Question")
        {
            return MessageBox.Show(message, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        }

        // Show custom message with specific buttons and icon
        public static DialogResult ShowCustomMessage(string message, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            return MessageBox.Show(message, caption, buttons, icon);
        }
    }
}
