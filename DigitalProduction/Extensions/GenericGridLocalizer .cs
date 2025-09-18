using System.Collections.Generic;
using DevExpress.XtraGrid.Localization;

namespace DigitalProduction.Extensions
{
    public class GenericGridLocalizer : GridLocalizer
    {
        private readonly Dictionary<GridStringId, string> _translations;

        // Constructor accepts a dictionary of translations
        public GenericGridLocalizer(Dictionary<GridStringId, string> translations)
        {
            _translations = translations ?? new Dictionary<GridStringId, string>();
        }

        public override string GetLocalizedString(GridStringId id)
        {
            if (_translations.ContainsKey(id))
                return _translations[id];
            return base.GetLocalizedString(id);
        }
    }
}
