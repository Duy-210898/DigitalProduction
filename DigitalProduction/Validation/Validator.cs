using DigitalProduction.Validation;

public static class Validator
{
    public static bool Validate(object value, ValidationType validationType)
    {
        if (value == null)
        {
            return validationType == ValidationType.NotNull ? false : true;
        }

        switch (validationType)
        {
            case ValidationType.NotNull:
                return value != null;

            case ValidationType.NotEmptyString:
                return value is string str && !string.IsNullOrWhiteSpace(str);

            case ValidationType.Integer:
                return value is int || int.TryParse(value.ToString(), out _);

            case ValidationType.NonZeroInteger:
                int intValue;
                return int.TryParse(value.ToString(), out intValue) && intValue != 0;

            case ValidationType.Double:
                return value is double || double.TryParse(value.ToString(), out _);

            case ValidationType.ValidDouble:
                double doubleValue;
                return double.TryParse(value.ToString(), out doubleValue)
                       && !double.IsNaN(doubleValue)
                       && !double.IsInfinity(doubleValue);

            default:
                return false;
        }
    }
}
