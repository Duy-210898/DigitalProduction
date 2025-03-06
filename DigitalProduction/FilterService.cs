using System;

namespace DigitalProduction
{
    public class FilterService
    {
        private static readonly Lazy<FilterService> _instance =
                new Lazy<FilterService>(() => new FilterService());

        public static FilterService Instance => _instance.Value;

        public string FilterKeyword { get; set; } = LocalizationManager.GetString("Search");
        public DateTime? FilterStartDate { get; set; } = DateTime.Today;
        public DateTime? FilterEndDate { get; set; } = DateTime.Today;
        public string FilterMachineName { get; set; }
        public string FilterSO { get; set; }
        public string FilterOperatorName { get; set; }

        private FilterService() { }
    }
}
