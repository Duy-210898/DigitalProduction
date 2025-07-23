using System;
using System.ComponentModel;

namespace DigitalProduction.Models
{
    public class DeviceOutput : INotifyPropertyChanged
    {
        public bool _isRecentlyUpdated { get; set; } // Temporary flag
        private string _machineName;
        private string _so;
        private string _size;
        private bool _isLeather;
        private string _partName;
        private string _operatorName;
        private int? _sizeQty;
        private int? _piecesPerPair;
        private int? _materialLayer;
        private int? _cuttingDieQty;
        public int? _actualCut;
        public int? _actualSizeQty;
        public int? _actualPieces;
        public int? _inventoryQty;
        private int? _totalPiecesPerPair;
        private bool _isGroupHeader;
        private string _materialType;
        public DateTime? Timestamp { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsRecentlyUpdated
        {
            get => _isRecentlyUpdated;
            set { _isRecentlyUpdated = value; OnPropertyChanged(nameof(IsRecentlyUpdated)); }
        }
        public string MachineName
        {
            get => _machineName;
            set { _machineName = value; OnPropertyChanged(nameof(MachineName)); }
        }
        public string OperatorName
        {
            get => _operatorName;
            set { _operatorName = value; OnPropertyChanged(nameof(OperatorName)); }
        }
        public string SO
        {
            get => _so;
            set { _so = value; OnPropertyChanged(nameof(SO)); }
        }

        public bool IsLeather
        {
            get => _isLeather;
            set { _isLeather = value; OnPropertyChanged(nameof(IsLeather)); }
        }
        public string MaterialType
        {
            get => _materialType;
            set { _materialType = value; OnPropertyChanged(nameof(MaterialType)); }
        }
        public string PartName
        {
            get => _partName;
            set { _partName = value; OnPropertyChanged(nameof(PartName)); }
        }

        public string Size
        {
            get => _size;
            set { _size = value; OnPropertyChanged(nameof(Size)); }
        }

        public int? SizeQty
        {
            get => _sizeQty ?? 0;
            set { _sizeQty = value; OnPropertyChanged(nameof(SizeQty)); }
        }

        public int? PiecesPerPair
        {
            get => _piecesPerPair ?? 0;
            set { _piecesPerPair = value; OnPropertyChanged(nameof(PiecesPerPair)); }
        }
        public int? TotalPiecesPerPair
        {
            get => _totalPiecesPerPair ?? 0;
            set { _totalPiecesPerPair = value; OnPropertyChanged(nameof(_totalPiecesPerPair)); }
        }

        public int? MaterialLayer
        {
            get => _materialLayer ?? 0;
            set { _materialLayer = value; OnPropertyChanged(nameof(MaterialLayer)); }
        }

        public int? CuttingDieQty
        {
            get => _cuttingDieQty ?? 0;
            set { _cuttingDieQty = value; OnPropertyChanged(nameof(CuttingDieQty)); }
        }

        public int? ActualCut
        {
            get => _actualCut;
            set
            {
                if (_actualCut != value)
                {
                    _actualCut = value;
                    OnPropertyChanged(nameof(ActualCut));
                }
            }
        }

        public int? ActualSizeQty
        {
            get => _actualSizeQty;
            set
            {
                if (_actualSizeQty != value)
                {
                    _actualSizeQty = value;
                    OnPropertyChanged(nameof(ActualSizeQty));
                }
            }
        }

        public int? ActualPieces
        {
            get => _actualPieces;
            set
            {
                if (_actualPieces != value)
                {
                    _actualPieces = value;
                    OnPropertyChanged(nameof(ActualPieces));
                }
            }
        }

        public bool IsGroupHeader
        {
            get => _isGroupHeader;
            set { _isGroupHeader = value; OnPropertyChanged(nameof(IsGroupHeader)); }
        }

        public int? InventoryQty
        {
            get => _inventoryQty;
            set
            {
                if (_inventoryQty != value)
                {
                    _inventoryQty = value;
                    OnPropertyChanged(nameof(InventoryQty));
                }
            }
        }
        public string UniqueKey()
        {
            Console.WriteLine($"KEY: {SO}-{PartName}-{Size}-{MachineName}-{OperatorName}-{IsLeather}");
            return $"{SO}-{PartName}-{Size}-{MachineName}-{OperatorName}-{IsLeather}";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
