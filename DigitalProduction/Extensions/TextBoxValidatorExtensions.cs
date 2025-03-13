using DevExpress.XtraEditors;
using DigitalProduction.Validation;
using System.Drawing;

public static class TextEditValidatorExtensions
{
    public static bool ValidateInput(this TextEdit textEdit, ValidationType validationType, bool shouldNotContain = false)
    {
        bool isValid = Validator.Validate(textEdit.Text, validationType);

        // If shouldNotContain is true, invert the validation result
        if (shouldNotContain)
        {
            isValid = !isValid;
        }

        // Highlight invalid input
        textEdit.BackColor = isValid ? Color.White : Color.LightCoral;

        return isValid;
    }
}
