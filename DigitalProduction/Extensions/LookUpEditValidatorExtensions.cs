using System.Drawing;
using System.Globalization;
using System.Text;
using DevExpress.XtraEditors;
using DigitalProduction.Validation;

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
    public static bool ValidateInput(this GridLookUpEdit lookUpEdit, ValidationType validationType)
    {
        object value = lookUpEdit.EditValue;
        bool isValid = Validator.Validate(value, validationType);

        // Highlight invalid input
        lookUpEdit.BackColor = isValid ? Color.White : Color.LightCoral;

        return isValid;
    }
    public static bool ValidateInput(this ComboBoxEdit lookUpEdit, ValidationType validationType)
    {
        object value = lookUpEdit.EditValue;
        bool isValid = Validator.Validate(value, validationType);

        // Highlight invalid input
        lookUpEdit.BackColor = isValid ? Color.White : Color.LightCoral;

        return isValid;
    }

    /// <summary>
    /// Bật tìm kiếm không dấu cho GridLookUpEdit.
    /// </summary>
    /// <param name="lookup">GridLookUpEdit cần enable.</param>
    /// <param name="displayField">Tên cột hiển thị (VD: "OperatorName").</param>

    public static string RemoveVietnameseDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        text = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (char c in text)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
