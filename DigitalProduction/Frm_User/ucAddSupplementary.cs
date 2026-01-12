using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DigitalProduction.Models;
using DigitalProduction.Validation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DigitalProduction.Frm_User
{
    public partial class ucAddSupplementary : UserControl
    {
        private int? selectedYear = System.DateTime.Today.Year;
        private WebSocketClient _webSocketClient;
        private string partField = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "vi"
                    ? "VietnameseName"
                    : "PartName";

        public ucAddSupplementary()
        {

            InitializeComponent();
            InitializeMonthFilter();

            // datetime select format
            dateTimePickerSchedule.Properties.VistaCalendarViewStyle = VistaCalendarViewStyle.YearsGroupView;
            dateTimePickerSchedule.Properties.Mask.EditMask = "yyyy";
            dateTimePickerSchedule.Properties.DisplayFormat.FormatString = "yyyy";
            dateTimePickerSchedule.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            dateTimePickerSchedule.Properties.EditFormat.FormatString = "yyyy";
            dateTimePickerSchedule.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;

            // SO grid
            gridLookUpEditSO.Properties.View = new GridView();
            gridLookUpEditSO.Properties.PopupFormSize = new Size(400, 300);
            gridLookUpEditSO.Properties.TextEditStyle = TextEditStyles.Standard;

            GridView view = gridLookUpEditSO.Properties.View as GridView;
            view.Columns.Clear();

            view.Columns.AddVisible(Constants.SO, Constants.SO);
            view.Columns.AddVisible("CreatedAt", LocalizationManager.GetString("CreatedAt"));
            view.Columns["CreatedAt"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            view.Columns["CreatedAt"].DisplayFormat.FormatString = "dd/MM/yyyy";
            gridLookUpEditSO.Properties.PopupFilterMode = PopupFilterMode.Contains;

            // filtering UI
            view.OptionsView.ShowAutoFilterRow = true;
            view.Columns[Constants.SO].OptionsFilter.AutoFilterCondition = AutoFilterCondition.Contains;

            // Setup checkbox selection in GridView
            view.OptionsView.ShowIndicator = false;
            view.OptionsView.ShowColumnHeaders = true;
            view.OptionsView.ShowGroupPanel = false;
            view.OptionsBehavior.Editable = false;
            view.BestFitColumns();

            // Make popup open immediately and not editable
            gridLookUpEditSO.Properties.ImmediatePopup = true;
            gridLookUpEditSO.Properties.PopupView.OptionsBehavior.Editable = false;

            //cbxPart.EditValueChanged += ComboxPart_EditValueChanged;
            gridLookUpEditSO.EditValueChanged += gridLookUpEditSO_EditValueChanged;
            cbxPart.EditValueChanged += cbxPart_EditValueChanged;
            TranslationText();

            // adjust min max quantity
            txt_Quantity.Properties.IsFloatValue = false;
            txt_Quantity.Properties.MinValue = 10;
            txt_Quantity.Properties.MaxValue = 10000;
        }
        private void gridLookUpEditSO_EditValueChanged(object sender, EventArgs e)
        {
            string selectedSO = gridLookUpEditSO.EditValue?.ToString();
            LoadPartItemsToCheckedComboBox(cbxPart, selectedSO);
        }
        private void LoadPartItemsToCheckedComboBox(ComboBoxEdit comboBoxPart, string selectedSO)
        {
            comboBoxPart.Properties.BeginUpdate();
            comboBoxPart.Properties.Items.Clear();

            if (string.IsNullOrEmpty(selectedSO))
            {
                comboBoxPart.Properties.EndUpdate();
                return;
            }
            string sql = String.Empty;
            if (partField == "VietnameseName")
            {
                sql = @"
                SELECT DISTINCT  ISNULL(pa.VietnameseName, pa.PartName) AS PartName
                    FROM PartSizeOrder ps
                    JOIN Part pa ON pa.PartID = ps.PartID
                    JOIN ProductOrder pr ON ps.OrderId = pr.OrderID
                    WHERE pr.SO = @SO
                    ORDER BY PartName;";
            }
            else {
                sql = @"
                SELECT DISTINCT pa.PartName
                    FROM PartSizeOrder ps
                    JOIN Part pa ON pa.PartID = ps.PartID
                    JOIN ProductOrder pr ON ps.OrderId = pr.OrderID
                    WHERE pr.SO = @SO
                    ORDER BY pa.PartName;";
            }

            DataTable dt = DbHelper.ExecuteQuery(sql, new SqlParameter("@SO", selectedSO));

            foreach (DataRow row in dt.Rows)
            {
                string partName = row["PartName"].ToString();
                comboBoxPart.Properties.Items.Add(partName);
            }

            // Optional: chỉnh UI
            comboBoxPart.Properties.DropDownRows = Math.Min(10, comboBoxPart.Properties.Items.Count);
            comboBoxPart.Properties.PopupFormMinSize = new Size(200, 350);

            comboBoxPart.Properties.EndUpdate();
            comboBoxPart.ShowPopup();
        }

        private void cbxPart_EditValueChanged(object sender, EventArgs e)
        {
            cbxSize.Properties.BeginUpdate();
            cbxSize.Properties.Items.Clear();

            // Lấy SO đang chọn
            string selectedSO = gridLookUpEditSO.EditValue?.ToString();
            if (string.IsNullOrEmpty(selectedSO))
            {
                cbxSize.Properties.EndUpdate();
                return;
            }

            // Lấy Part đã chọn (chỉ 1 giá trị)
            string selectedPart = cbxPart.EditValue?.ToString();
            if (string.IsNullOrEmpty(selectedPart))
            {
                cbxSize.Properties.EndUpdate();
                return;
            }

            // SQL query: chỉ dùng 1 tham số @Part
            string sql = String.Empty;
            if (partField == "VietnameseName")
            {
                sql = @"
                    SELECT s.Size
                        FROM PartSizeOrder ps
                        JOIN Size s ON s.SizeID = ps.SizeID
                        JOIN Part pa ON pa.PartID = ps.PartID
                        JOIN ProductOrder pr ON ps.OrderId = pr.OrderID
                        WHERE pr.SO = @SO
                            AND (
                                   (pa.VietnameseName IS NOT NULL AND pa.VietnameseName <> '' AND pa.VietnameseName = @Part)
                                OR (ISNULL(pa.VietnameseName, '') = '' AND pa.PartName = @Part)
                                )
                        ORDER BY TRY_CAST(s.Size AS INT), s.Size;
                        ";
            }
            else {
                sql = @"
                     SELECT s.Size
                        FROM PartSizeOrder ps
                        JOIN Size s ON s.SizeID = ps.SizeID
                        JOIN Part pa ON pa.PartID = ps.PartID
                        JOIN ProductOrder pr ON ps.OrderId = pr.OrderID
                        WHERE pr.SO = @SO
                            AND pa.PartName = @Part
                        ORDER BY TRY_CAST(s.Size AS INT), s.Size;";
            }
            DataTable dt = DbHelper.ExecuteQuery(
                sql,
                new SqlParameter("@SO", selectedSO),
                new SqlParameter("@Part", selectedPart)
            );

            // Add size vào combobox
            if (dt != null && dt.Rows.Count > 0)
            {
                cbxSize.Properties.BeginUpdate();
                cbxSize.Properties.Items.Clear();

                var sizes = new List<string>();

                foreach (DataRow row in dt.Rows)
                {
                    string size = row["Size"].ToString();
                    if (!string.IsNullOrWhiteSpace(size))
                    {
                        sizes.Add(size);
                    }
                }

                // Phân loại numeric vs non-numeric
                var numericSizes = sizes
                    .Where(s => double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                    .Select(s => new { Value = s, Num = double.Parse(s, CultureInfo.InvariantCulture) })
                    .OrderBy(x => x.Num)
                    .Select(x => x.Value)
                    .ToList();

                var nonNumericSizes = sizes
                    .Where(s => !double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                    .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                // Add numeric trước rồi tới non-numeric
                foreach (var size in numericSizes.Concat(nonNumericSizes))
                {
                    if (!cbxSize.Properties.Items.Contains(size))
                        cbxSize.Properties.Items.Add(size);
                }

                cbxSize.Properties.DropDownRows = Math.Min(10, cbxSize.Properties.Items.Count);
                cbxSize.Properties.PopupFormMinSize = new Size(200, 350);

                cbxSize.Properties.EndUpdate();

                if (cbxSize.Properties.Items.Count > 0)
                {
                    cbxSize.ShowPopup();
                }
            }
        }


        private void InitializeMonthFilter()
        {
            dateTimePickerSchedule.EditValueChanged += async (sender, e) =>
            {
                selectedYear = dateTimePickerSchedule.DateTime.Year;

                await GetListOfSOsByYearAsync();
            };
        }

        public void SetWebSocketClient(WebSocketClient webSocketClient)
        {
            if (_webSocketClient != null)
            {
                _webSocketClient.OnResponseReceived -= WebSocket_OnMessageAsync;
            }

            _webSocketClient = webSocketClient ?? WebSocketClient.Instance;
            _webSocketClient.OnResponseReceived += WebSocket_OnMessageAsync;

            _ = GetListOfSOsByYearAsync();
        }
        public async Task GetListOfSOsByYearAsync()
        {
            var request = new
            {
                app = Global.App,
                action = "getListOfSOsByYear",
                year = selectedYear
            };

            string jsonRequest = JsonConvert.SerializeObject(request);
            await _webSocketClient.SendAsync(jsonRequest);
        }
        private int _soRetryCount = 0;
        private const int MaxSoRetries = 3;
        private async void WebSocket_OnMessageAsync(string jsonData)
        {
            try
            {
                string action = JObject.Parse(jsonData)["action"]?.ToString();
                if (!string.IsNullOrEmpty(action))
                {
                    switch (action)
                    {
                        case "getListOfSOsByYear":
                            await Task.Delay(1000); // small delay before retry+
                            var soListResponse = JsonConvert.DeserializeObject<ResponseMessage<List<SalesOrder>>>(jsonData);
                            if (soListResponse?.Data != null && soListResponse.Data.Count > 0)
                            {
                                // ✅ Got data
                                _soRetryCount = 0; // reset retry count
                                gridLookUpEditSO.Properties.DataSource = soListResponse.Data;
                                gridLookUpEditSO.Properties.DisplayMember = "SO";
                                gridLookUpEditSO.Properties.ValueMember = "SO";
                            }
                            else
                            {
                                // ❌ No data, retry if under max retries
                                if (_soRetryCount < MaxSoRetries)
                                {
                                    _soRetryCount++;
                                    await GetListOfSOsByYearAsync();
                                }
                                else
                                {
                                    // Final fallback
                                    gridLookUpEditSO.Properties.DataSource = null;
                                    ShowMessage.ShowInfo("No Sales Orders found after retries.");
                                }
                            }
                            break;

                        default:
                            break;
                    }
                }
                else
                {
                    ShowMessage.ShowInfo("Cant not processing get WebSocket data");
                }
            }
            catch (Exception ex)
            {
                ConnectionManager.Instance.IsReconnecting = true;
                MessageBox.Show($"Error processing WebSocket data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void InsertPartSizeOrder(int partId, int sizeId, int orderId, int materialId, int sizeQty, int targetCut, string unit, double unitUsage)
        {
            string sql = @"
        INSERT INTO PartSizeOrder (PartId, SizeId, OrderId, MaterialID, SizeQty, TargetCut, Unit, UnitUsage)
        VALUES (@PartId, @SizeId, @OrderId, @MaterialID, @SizeQty, @TargetCut, @Unit, @UnitUsage)";

            int rows = DbHelper.ExecuteNonQuery(sql,
                new SqlParameter("@PartId", partId),
                new SqlParameter("@SizeId", sizeId),
                new SqlParameter("@OrderId", orderId),
                new SqlParameter("@MaterialID", materialId),
                new SqlParameter("@SizeQty", sizeQty),
                new SqlParameter("@TargetCut", targetCut),
                new SqlParameter("@Unit", unit),
                new SqlParameter("@UnitUsage", unitUsage));

            if (rows > 0)
            {
                MessageBox.Show("✅ New size added successfully (copied data from base PartSizeOrder).");
            }
            else
            {
                MessageBox.Show("⚠️ Insert failed: no matching base PartSizeOrder found.");
            }
        }


        private int EnsureSizeExists(string sizeName)
        {
            string sqlCheck = "SELECT SizeID FROM Size WHERE Size = @Size";
            DataTable dt = DbHelper.ExecuteQuery(sqlCheck, new SqlParameter("@Size", sizeName));
            if (dt.Rows.Count > 0)
            {
                return Convert.ToInt32(dt.Rows[0]["SizeID"]);
            }

            string sqlInsert = "INSERT INTO Size (Size) OUTPUT INSERTED.SizeID VALUES (@Size)";
            DataTable result = DbHelper.ExecuteQuery(sqlInsert, new SqlParameter("@Size", sizeName));
            return Convert.ToInt32(result.Rows[0]["SizeID"]);
        }
        private void btnAddSupplement_Click(object sender, EventArgs e)
        {
            try
            {
                if (!gridLookUpEditSO.ValidateInput(ValidationType.NotEmptyString) ||
                    !cbxPart.ValidateInput(ValidationType.NotEmptyString) ||
                    !cbxSize.ValidateInput(ValidationType.NotEmptyString) ||
                    !txt_Quantity.ValidateInput(ValidationType.NotEmptyString))
                {
                    MessageBox.Show("Invalid input! Please correct the highlighted fields.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Lấy SO
                string selectedSO = gridLookUpEditSO.EditValue?.ToString();

                // Lấy Part
                string selectedPart = cbxPart.EditValue?.ToString();

                // Lấy Size gốc
                string baseSize = cbxSize.EditValue?.ToString();

                // Lấy OrderId + PartId từ DB
                string sql = @"
                    SELECT ps.OrderId, pa.PartId, ps.MaterialID, ps.TargetCut, ps.Unit, ps.UnitUsage
                    FROM PartSizeOrder ps
                    JOIN Part pa ON pa.PartID = ps.PartID
                    JOIN ProductOrder pr ON pr.OrderID = ps.OrderID
                    WHERE pr.SO = @SO AND pa.PartName = @Part AND ps.SizeId = (SELECT SizeID FROM Size WHERE Size = @Size)";

                DataTable dt = DbHelper.ExecuteQuery(sql,
                    new SqlParameter("@SO", selectedSO),
                    new SqlParameter("@Part", selectedPart),
                    new SqlParameter("@Size", baseSize));

                if (dt.Rows.Count == 0)
                {
                    ShowMessage.ShowInfo("Could not find PartSizeOrder for selected Part/Size.");
                    return;
                }

                int orderId = Convert.ToInt32(dt.Rows[0]["OrderId"]);
                int partId = Convert.ToInt32(dt.Rows[0]["PartId"]);
                int materialId = Convert.ToInt32(dt.Rows[0]["MaterialID"]);
                int targetCut = Convert.ToInt32(dt.Rows[0]["TargetCut"]);
                string unit = dt.Rows[0]["Unit"].ToString();
                double unitUsage = Convert.ToDouble(dt.Rows[0]["UnitUsage"]);

                // Tạo size mới
                string newSize = baseSize + "_bu";

                // Đảm bảo Size tồn tại
                int newSizeId = EnsureSizeExists(newSize);

                int qty = int.TryParse(txt_Quantity.Text, out var q) ? q : 0;
                // Insert PartSizeOrder
                InsertPartSizeOrder(partId, newSizeId, orderId, materialId, qty, targetCut, unit, unitUsage);

                ShowMessage.ShowInfo($"New size '{newSize}' added for Part '{selectedPart}' in SO '{selectedSO}'.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Add Supplementary", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TranslationText()
        {
            layoutControlItem8.Text = Lang.SelectSO;
            layoutControlItem2.Text = Lang.SelectPart;
            layoutControlItem3.Text = Lang.SelectSize;
            layoutControlItem7.Text = Lang.FilterDate;
            layoutControlItem1.Text = Lang.Quantity;
            btnAddSupplement.Text = Lang.Add;
        }
        public class SalesOrder
        {
            public string SO { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }
}
