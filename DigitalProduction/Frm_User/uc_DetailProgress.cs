using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using DevExpress.Utils.Menu;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.BandedGrid;
using DevExpress.XtraGrid.Views.Grid;
using DigitalProduction.Models;

namespace DigitalProduction.Frm_User
{
    public partial class uc_DetailProgress : UserControl
    {
        public uc_DetailProgress(BindingList<DistributionModelView> distributions)
        {
            InitializeComponent();
            PivotAndBind(distributions);
            SetupBandedGridViewLayout();
            TranslateHeaders();
        }

        private void PivotAndBind(BindingList<DistributionModelView> distributions)
        {
            double ParseSize(string size)
            {
                return double.Parse(size.Replace("K", ""), CultureInfo.InvariantCulture);
            }

            // Các cột khóa giữ nguyên
            string[] keyColumns = new string[]
            {
                "SO", "PartName", "VietnameseName", "MachineName", "MaterialName",
                "OperatorName", "EmployeeName", "Status", "DeviceID", "IpAddress", "CreatedAt", "UpdatedAt", "Note"
            };

            // Lấy danh sách size distinct để tạo cột size
            var sizes = distributions
                .Select(x => x.Size)
                .Distinct()
                .OrderBy(s => ParseSize(s))
                .ToList();

            DataTable pivot = new DataTable();

            // Tạo cột khóa, SO chỉ là 1 cột string bình thường
            foreach (var col in keyColumns)
            {
                if (col == "CreatedAt" || col == "UpdatedAt")
                    pivot.Columns.Add(col, typeof(DateTime));
                else if (col == "DeviceID" || col == "Note")
                    pivot.Columns.Add(col, typeof(int));
                else
                    pivot.Columns.Add(col, typeof(string));
            }

            // Tạo cột size (1K, 2K, 3K, ...)
            foreach (var s in sizes)
            {
                pivot.Columns.Add(s, typeof(int));
            }

            // Nhóm dữ liệu theo các cột khóa (SO giữ nguyên dạng cột)
            var groups = distributions
                .GroupBy(x => new
                {
                    x.SO,
                    x.PartName,
                    x.VietnameseName,
                    x.MachineName,
                    x.MaterialName,
                    x.OperatorName,
                    x.EmployeeName,
                    x.Status,
                    x.DeviceID,
                    x.IpAddress,
                    x.CreatedAt,
                    x.UpdatedAt,
                    x.Note
                });

            foreach (var g in groups)
            {
                DataRow row = pivot.NewRow();

                row["SO"] = g.Key.SO;
                row["PartName"] = g.Key.PartName;
                row["VietnameseName"] = g.Key.VietnameseName;
                row["MachineName"] = g.Key.MachineName;
                row["MaterialName"] = g.Key.MaterialName;
                row["OperatorName"] = g.Key.OperatorName;
                row["EmployeeName"] = g.Key.EmployeeName;
                row["Status"] = g.Key.Status;
                row["DeviceID"] = g.Key.DeviceID;
                row["IpAddress"] = g.Key.IpAddress;
                row["CreatedAt"] = g.Key.CreatedAt;
                row["UpdatedAt"] = g.Key.UpdatedAt;
                //row["Note"] = g.Key.Note;

                foreach (var s in sizes)
                {
                    var item = g.FirstOrDefault(x => x.Size == s);
                    row[s] = item?.ActualSizeQty ?? 0;
                }

                pivot.Rows.Add(row);
            }

            gridControlDetailDistribution.DataSource = pivot;
        }

    private void SetupBandedGridViewLayout()
        {
            var bandedView = new BandedGridView(gridControlDetailDistribution);
            gridControlDetailDistribution.MainView = bandedView;
            gridControlDetailDistribution.ViewCollection.Add(bandedView);

            string[] keyColumns = new string[] { "SO", "PartName", "VietnameseName", "MachineName", "MaterialName",
                "OperatorName", "EmployeeName", "Status", "DeviceID", "IpAddress", "CreatedAt", "UpdatedAt", "Note" };

            bandedView.Columns.Clear();
            bandedView.Bands.Clear();

            // =========================
            // Band SO (Group Header)
            // =========================
            GridBand soBand = new GridBand() { Caption = "SO" };
            var colSO = bandedView.Columns.AddVisible("SO", "SO");
            colSO.OptionsColumn.ReadOnly = true;
            colSO.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            soBand.Columns.Add(colSO);
            bandedView.Bands.Add(soBand);

            // =========================
            // Band con: Operator | PartName | MachineName
            // =========================
            GridBand detailBand = new GridBand() { Caption = Lang.Detail };

            string[] childCols = new string[] { "OperatorName", "PartName", "MachineName" };
            foreach (var colName in childCols)
            {
                var col = bandedView.Columns.AddVisible(colName, colName);
                col.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                col.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                col.OptionsColumn.ReadOnly = true;
                detailBand.Columns.Add(col);
            }
            bandedView.Bands.Add(detailBand);

            // =========================
            // Band Sizes
            // =========================
            var dt = gridControlDetailDistribution.DataSource as DataTable;
            var sizeColumns = dt.Columns.Cast<DataColumn>()
                .Where(c => !keyColumns.Contains(c.ColumnName))
                .OrderBy(c => double.Parse(c.ColumnName.Replace("K", ""), CultureInfo.InvariantCulture))
                .ToList();

            GridBand sizeBand = new GridBand() { Caption = "Size" };
            foreach (var s in sizeColumns)
            {
                var col = bandedView.Columns.AddVisible(s.ColumnName, s.ColumnName);
                col.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                col.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                col.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                col.DisplayFormat.FormatString = "n0";
                col.Width = 50;
                col.OptionsColumn.FixedWidth = true;
                sizeBand.Columns.Add(col);
            }
            bandedView.Bands.Add(sizeBand);

            // =========================
            // Group theo SO
            // =========================
            colSO.GroupIndex = 0;
            bandedView.ExpandAllGroups();

            // =========================
            // Ẩn cột không cần thiết
            // =========================
            string[] hideCols = { "DeviceID", "Note", "IpAddress", "MaterialName", "EmployeeName", "CreatedAt" };
            foreach (var h in hideCols)
                if (bandedView.Columns.ColumnByFieldName(h) != null)
                    bandedView.Columns[h].Visible = false;

            // =========================
            // Ẩn 0 -> rỗng
            // =========================
            bandedView.CustomColumnDisplayText += (s, e) =>
            {
                if (!keyColumns.Contains(e.Column.FieldName))
                {
                    if (int.TryParse(Convert.ToString(e.Value), out int v) && v == 0)
                        e.DisplayText = "";
                }
            };

            // =========================
            // Highlight theo Status
            // =========================
            bandedView.RowCellStyle += (s, e) =>
            {
                var view = s as BandedGridView;
                string status = Convert.ToString(view.GetRowCellValue(e.RowHandle, "Status"));

                if (string.IsNullOrEmpty(status))
                    return;

                switch (status)
                {
                    case "Complete":
                        e.Appearance.ForeColor = Color.DarkGreen;
                        e.Appearance.Font = new Font(e.Appearance.Font, FontStyle.Bold);
                        break;
                    case "Pending":
                        e.Appearance.ForeColor = Color.Orange;
                        break;
                }
            };

            // =========================
            // Layout
            // =========================
            bandedView.OptionsView.ColumnAutoWidth = false;
            bandedView.OptionsBehavior.Editable = false;
            bandedView.OptionsView.ShowGroupPanel = true;
            bandedView.BestFitColumns();
}


        private void TranslateHeaders()
        {
            var bandedView = gridControlDetailDistribution.MainView as BandedGridView;
            if (bandedView == null) return;

            // =========================
            // Dịch tiêu đề cột
            // =========================
            foreach (BandedGridColumn col in bandedView.Columns)
            {
                string translatedText = LocalizationManager.GetString(col.FieldName);
                if (!string.IsNullOrEmpty(translatedText))
                {
                    col.Caption = translatedText;
                }
            }

            // =========================
            // Ẩn cột không cần thiết
            // =========================
            string[] hideCols = { "DeviceID", "Note", "IpAddress", "MaterialName", "EmployeeName", "CreatedAt" };
            foreach (var h in hideCols)
            {
                if (bandedView.Columns.ColumnByFieldName(h) != null)
                    bandedView.Columns[h].Visible = false;
            }

            // =========================
            // Format cột UpdatedAt
            // =========================
            bandedView.CustomColumnDisplayText += (s, e) =>
            {
                if (e.Column.FieldName == "UpdatedAt" && e.Value != null && e.Value != DBNull.Value)
                {
                    try
                    {
                        DateTimeOffset dto = DateTimeOffset.Parse(e.Value.ToString());
                        e.DisplayText = dto.LocalDateTime.ToString("dd/MM/yyyy HH:mm:ss");
                    }
                    catch
                    {
                        e.DisplayText = "";
                    }
                }
            };

            // =========================
            // AutoFit columns
            // =========================
            bandedView.BestFitColumns();

            // =========================
            // Width cố định cho một số cột quan trọng
            // =========================
            if (bandedView.Columns.ColumnByFieldName("SO") != null)
                bandedView.Columns["SO"].Width = 100;
            if (bandedView.Columns.ColumnByFieldName("MachineName") != null)
                bandedView.Columns["MachineName"].Width = 110;
            if (bandedView.Columns.ColumnByFieldName("OperatorName") != null)
                bandedView.Columns["OperatorName"].Width = 150;
            if (bandedView.Columns.ColumnByFieldName("PartName") != null)
                bandedView.Columns["PartName"].Width = 130;

            // =========================
            // Popup menu localization
            // =========================
            bandedView.PopupMenuShowing += (s, e) =>
            {
                if (e.MenuType == GridMenuType.Column)
                {
                    // e.Menu là DXPopupMenu
                    if (e.Menu is DXPopupMenu menu)
                    {
                        foreach (DXMenuItem item in menu.Items)
                        {
                            string translated = LocalizationManager.GetString(item.Caption);
                            if (!string.IsNullOrEmpty(translated))
                                item.Caption = translated;
                        }
                    }
                }
            };

        }
    }
}
