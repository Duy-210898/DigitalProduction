using DevExpress.XtraEditors;
using DigitalProduction.Validation;
using System.Drawing;

public static class LookUpEditValidatorExtensions
{
    public static bool ValidateInput(this LookUpEdit lookUpEdit, ValidationType validationType)
    {
        object value = lookUpEdit.EditValue;
        bool isValid = Validator.Validate(value, validationType);

        // Highlight invalid input
        lookUpEdit.BackColor = isValid ? Color.White : Color.LightCoral;

        return isValid;
    }
}
