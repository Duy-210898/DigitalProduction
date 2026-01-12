using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.Utils;
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
    public partial class ucAddAutoDataCutting : UserControl
    {
        private int? selectedYear = System.DateTime.Today.Year;
        private WebSocketClient _webSocketClient;
        private string partField = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "vi"
                  ? "VietnameseName"
                  : "PartName";
        public ucAddAutoDataCutting()
        {
            InitializeComponent();
            LoadAllLookups();
            InitializeMonthFilter();
            LoadTranslations();
            ApplyNoNegative(txtPiecesPerPair);
            ApplyNoNegative(txtMaterialLayer);
            ApplyNoNegative(txtTotalPiecesPerPair);
            cbxSize.Properties.TextEditStyle = TextEditStyles.DisableTextEditor;
            cbxSize.Properties.ShowDropDown = ShowDropDown.SingleClick;
            cbxSize.Properties.AllowNullInput = DefaultBoolean.True;
            // Gán Action dựa trên CheckEdit
            isLeather.CheckedChanged += IsLeather_CheckedChanged;

            rdRawMaterial_Checked();
            // datetime select format
            dateTimePickerSchedule.Properties.VistaCalendarViewStyle = VistaCalendarViewStyle.YearsGroupView;
            dateTimePickerSchedule.Properties.Mask.EditMask = "yyyy";
            dateTimePickerSchedule.Properties.DisplayFormat.FormatString = "yyyy";
            dateTimePickerSchedule.Properties.DisplayFormat.FormatType = FormatType.DateTime;
            dateTimePickerSchedule.Properties.EditFormat.FormatString = "yyyy";
            dateTimePickerSchedule.Properties.EditFormat.FormatType = FormatType.DateTime;

            // SO grid
            gridLookUpSO.Properties.View = new GridView();
            gridLookUpSO.Properties.PopupFormSize = new Size(400, 300);
            gridLookUpSO.Properties.TextEditStyle = TextEditStyles.Standard;

            GridView view = gridLookUpSO.Properties.View as GridView;
            view.Columns.Clear();

            view.Columns.AddVisible(Constants.SO, Constants.SO);
            view.Columns.AddVisible("CreatedAt", LocalizationManager.GetString("CreatedAt"));
            view.Columns["CreatedAt"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            view.Columns["CreatedAt"].DisplayFormat.FormatString = "dd/MM/yyyy";
            gridLookUpSO.Properties.PopupFilterMode = PopupFilterMode.Contains;

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
            gridLookUpSO.Properties.ImmediatePopup = true;
            gridLookUpSO.Properties.PopupView.OptionsBehavior.Editable = false;

            //cbxPart.EditValueChanged += ComboxPart_EditValueChanged;
            gridLookUpSO.EditValueChanged += gridLookUpEditSO_EditValueChanged;
            gridLookUpPart.EditValueChanged += cbxPart_EditValueChanged;

            EnableContainsAutoFilter(gridLookUpEmployee);
            EnableContainsAutoFilter(gridLookUpPlant);
            EnableContainsAutoFilter(gridLookUpMachine);
        }

        private void EnableContainsAutoFilter(GridLookUpEdit lookUp)
        {
            GridView view = lookUp.Properties.PopupView as GridView;
            if (view == null) return;

            view.OptionsView.ShowAutoFilterRow = true;

            foreach (GridColumn col in view.Columns)
            {
                col.OptionsFilter.AutoFilterCondition = AutoFilterCondition.Contains;
            }
        }
        private void IsLeather_CheckedChanged(object sender, EventArgs e)
        {
            if (isLeather.Checked)
            {
                rdLeather_Checked();
            }
            else
            {
                rdRawMaterial_Checked();
            }
        }
        private void rdLeather_Checked()
        {
            SetLayoutItemVisibility(false, lblPiecesPerPair, lblMaterialLayer);
            SetLayoutItemVisibility(true, lblTotalPiecesPerPair);
        }
        private void rdRawMaterial_Checked()
        {
            SetLayoutItemVisibility(false, lblTotalPiecesPerPair);
            SetLayoutItemVisibility(true, lblPiecesPerPair, lblMaterialLayer);
        }
        private void gridLookUpEditSO_EditValueChanged(object sender, EventArgs e)
        {
            string selectedSO = gridLookUpSO.EditValue?.ToString();
            LoadPartItemsToGridLookup(gridLookUpPart, selectedSO);
        }
        private void LoadPartItemsToGridLookup(GridLookUpEdit gridLookupPart, string selectedSO)
        {
            gridLookupPart.Properties.BeginUpdate();

            if (string.IsNullOrEmpty(selectedSO))
            {
                gridLookupPart.Properties.DataSource = null;
                gridLookupPart.Properties.EndUpdate();
                return;
            }

            string sql = @"
        SELECT DISTINCT
            pa.PartCode,
            ISNULL(pa.VietnameseName, pa.PartName) AS PartName
        FROM PartSizeOrder ps
        JOIN Part pa ON pa.PartID = ps.PartID
        JOIN ProductOrder pr ON ps.OrderId = pr.OrderID
        WHERE pr.SO = @SO
        ORDER BY PartName;
    ";

            DataTable dt = DbHelper.ExecuteQuery(sql, new SqlParameter("@SO", selectedSO));

            gridLookupPart.Properties.DataSource = dt;
            gridLookupPart.Properties.DisplayMember = "PartName";   // HIỂN THỊ
            gridLookupPart.Properties.ValueMember = "PartCode";     // GIÁ TRỊ
            gridLookupPart.Properties.NullText = "";

            // Grid view settings
            GridView view = gridLookupPart.Properties.View;
            view.Columns.Clear();
            view.OptionsView.ShowIndicator = false;
            view.OptionsView.ShowColumnHeaders = true;
            view.OptionsBehavior.Editable = false;
            view.BestFitColumns();

            view.Columns.AddVisible("PartName", "Part Name");
            view.Columns.AddVisible("PartCode", "Part Code");

            // Optional: ẩn PartCode nếu không muốn show
            // view.Columns["PartCode"].Visible = false;

            gridLookupPart.Properties.EndUpdate();
        }


        private List<SizeQtyDto> GetSizeQtyBySOAndPart(string so, string partCode)
        {
            if (string.IsNullOrEmpty(so) || string.IsNullOrEmpty(partCode))
                return new List<SizeQtyDto>();

            string sql = @"
        SELECT 
            s.Size,
            ps.SizeQty
        FROM PartSizeOrder ps
        JOIN Size s ON s.SizeID = ps.SizeID
        JOIN Part pa ON pa.PartID = ps.PartID
        JOIN ProductOrder pr ON ps.OrderID = pr.OrderID
        WHERE pr.SO = @SO
          AND pa.PartCode = @PartCode
        ORDER BY TRY_CAST(s.Size AS INT), s.Size;
    ";

            DataTable dt = DbHelper.ExecuteQuery(
                sql,
                new SqlParameter("@SO", so),
                new SqlParameter("@PartCode", partCode)
            );

            return dt.AsEnumerable()
                     .Select(r => new SizeQtyDto
                     {
                         Size = r["Size"].ToString(),
                         SizeQty = Convert.ToInt32(r["SizeQty"])
                     })
                     .ToList();
        }

        private void cbxPart_EditValueChanged(object sender, EventArgs e)
        {
            cbxSize.Properties.BeginUpdate();
            cbxSize.Properties.Items.Clear();

            string so = gridLookUpSO.EditValue?.ToString();
            string part = gridLookUpPart.EditValue?.ToString();

            if (string.IsNullOrEmpty(so) || string.IsNullOrEmpty(part))
            {
                cbxSize.Properties.EndUpdate();
                return;
            }

            var sizeQtyList = GetSizeQtyBySOAndPart(so, part);

            foreach (var item in sizeQtyList)
            {
                // Hiển thị Size + Qty
                cbxSize.Properties.Items.Add(
                    new ImageComboBoxItem(
                        $"{item.Size} {LocalizationManager.GetString("LastSizeQty")} {item.SizeQty}",
                        item                                 
                    )
                );
            }

            cbxSize.Properties.DropDownRows = Math.Min(10, cbxSize.Properties.Items.Count);
            cbxSize.Properties.PopupFormMinSize = new Size(200, 350);

            cbxSize.Properties.EndUpdate();

            if (cbxSize.Properties.Items.Count > 0)
                cbxSize.ShowPopup();
        }


        private void LoadAllLookups()
        {
            LoadEmployees();
            LoadPlants();
        }

        //================ EMPLOYEE ======================
        private void LoadEmployees()
        {
            gridLookUpEmployee.Properties.DataSource = DbHelper.getEmployees();
            gridLookUpEmployee.Properties.DisplayMember = "EmployeeName";
            gridLookUpEmployee.Properties.ValueMember = "OperatorID";
            gridLookUpEmployee.Properties.PopulateViewColumns();
            gridLookUpEmployee.Properties.View.Columns["OperatorID"].Visible = false;
            gridLookUpEmployee.EditValue = null;
        }

        //================ PLANT ======================
        private void LoadPlants()
        {
            gridLookUpPlant.Properties.DataSource = DbHelper.getPlants();
            gridLookUpPlant.Properties.DisplayMember = "PlantName";
            gridLookUpPlant.Properties.ValueMember = "PlantID";
            gridLookUpPlant.Properties.PopulateViewColumns();
            gridLookUpPlant.Properties.View.Columns["PlantID"].Visible = false;
            gridLookUpPlant.EditValue = null;

            // Event: Load machine theo Plant
            gridLookUpPlant.EditValueChanged += (s, e) =>
            {
                if (gridLookUpPlant.EditValue != null)
                {
                    int p = Convert.ToInt32(gridLookUpPlant.EditValue);
                    LoadMachines(p);
                }
            };
        }

        //================ MACHINE THEO PLANT ======================
        private void LoadMachines(int plantID)
        {
            gridLookUpMachine.Properties.DataSource = DbHelper.getMachines(plantID);
            gridLookUpMachine.Properties.DisplayMember = "MachineName";
            gridLookUpMachine.Properties.ValueMember = "DeviceID";
            gridLookUpMachine.Properties.PopulateViewColumns();
            gridLookUpMachine.Properties.View.Columns["DeviceID"].Visible = false;
            gridLookUpMachine.EditValue = null;
        }

        //================ ISO ======================
        private void InitializeMonthFilter()
        {
            dateTimePickerSchedule.EditValueChanged += async (sender, e) =>
            {
                selectedYear = dateTimePickerSchedule.DateTime.Year;

                await GetListOfSOsByYearAsync();
            };
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ValidateForm())
                    return;

                // 1️⃣ Validate required selections
                if (gridLookUpEmployee.EditValue == null ||
                    gridLookUpMachine.EditValue == null ||
                    gridLookUpSO.EditValue == null ||
                    gridLookUpPart.EditValue == null ||
                    cbxSize.EditValue == null)
                {
                    XtraMessageBox.Show(
                        "Please fill in all required fields.",
                        "Validation Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }

                // 2️⃣ Parse required IDs
                int employeeId = Convert.ToInt32(gridLookUpEmployee.EditValue);
                int userId = Convert.ToInt32(Global.CurrentUser.UserID);
                int deviceId = Convert.ToInt32(gridLookUpMachine.EditValue);

                // 3️⃣ Safe parse helper (empty or invalid => 0)
                int ParseOrZero(string value)
                {
                    return int.TryParse(value, out int result) ? result : 0;
                }

                // 4️⃣ Parse numeric fields safely
                int inventoryQty = ParseOrZero(txtInventory.Text);
                int actualSizeQty = ParseOrZero(txtActualSizeQty.Text);
                int piecesPerPair = ParseOrZero(txtPiecesPerPair.Text);
                int materialLayer = ParseOrZero(txtMaterialLayer.Text);
                int totalPiecesPerPair = ParseOrZero(txtTotalPiecesPerPair.Text);
                
                int? partId = DbHelper.GetPartID(gridLookUpPart.EditValue?.ToString());
                int? sizeId = DbHelper.GetSizeID(cbxSize.EditValue.ToString());
                int? orderId = DbHelper.GetOrderID(gridLookUpSO.EditValue?.ToString());

                int? partSizeOrderId = DbHelper.GetPartSizeOrderID(orderId, partId, sizeId);

                if (!partSizeOrderId.HasValue || !partId.HasValue || !sizeId.HasValue || !orderId.HasValue)
                {
                    XtraMessageBox.Show("Failed to resolve part, size, or order IDs.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var distribution = new DistributionDto
                {
                    UserID = userId,
                    DeviceID = deviceId,
                    PartSizeOrderID = partSizeOrderId.Value,
                    OperatorID = employeeId,
                    InventoryQty = inventoryQty,
                    IsLeather = isLeather.Checked,
                    IsAutoCutting = isAutoCutting.Checked,
                    Status = "Complete"
                };
                var output = new DeviceOutputSaveDto
                {
                    OperatorID = (int)DbHelper.getEmployeeIDByOperatorID(employeeId),
                    PartID = partId.Value,
                    SizeID = sizeId.Value,
                    OrderID = orderId.Value,
                    ActualCut = 0,
                    ActualPieces = 0,
                    ActualSizeQty = actualSizeQty,
                    InventoryQty = inventoryQty,
                    PiecesPerPair = piecesPerPair,
                    MaterialLayer = materialLayer,
                    IsLeather = isLeather.Checked,
                    CuttingDieQty = 0,
                    TotalPiecesPerPair = totalPiecesPerPair,
                };

                // Call upsert method
                DbHelper.SaveDistributionAndDeviceOutputs(distribution, new List<DeviceOutputSaveDto> { output });

                XtraMessageBox.Show("Saved successfully!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Reset form
               // BtnReset_Click(null, null);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"Error saving data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void BtnReset_Click(object sender, EventArgs e)
        {
            gridLookUpEmployee.EditValue = null;
            gridLookUpPlant.EditValue = null;
            gridLookUpMachine.EditValue = null;
            gridLookUpSO.EditValue = null;
            gridLookUpPart.EditValue = null;
            cbxSize.EditValue = null;
            txtActualSizeQty.Text = "";
            txtInventory.Text = "";
            //txtPiecesPerPair.Text = "";
            //txtMaterialLayer.Text = "";
            //txtCuttingDieQty.Text = "";
            //txtTotalPiecesPerPair.Text = "";
            isLeather.Checked = false;
            isAutoCutting.Checked = false;
        }

        private bool ValidateForm()
        {
            bool isValid =
                gridLookUpEmployee.ValidateInput(ValidationType.NotNull) &
                gridLookUpPlant.ValidateInput(ValidationType.NotNull) &
                gridLookUpMachine.ValidateInput(ValidationType.NotNull) &
                gridLookUpSO.ValidateInput(ValidationType.NotNull) &
                gridLookUpPart.ValidateInput(ValidationType.NotNull) &
                cbxSize.ValidateInput(ValidationType.NotNull) &
                txtActualSizeQty.ValidateInput(ValidationType.NotEmptyString);

            if (!isValid)
            {
                XtraMessageBox.Show(
                    LocalizationManager.GetString("InvalidInput"),
                    LocalizationManager.GetString("ValidationError"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }

            return isValid;
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
                                gridLookUpSO.Properties.DataSource = soListResponse.Data;
                                gridLookUpSO.Properties.DisplayMember = "SO";
                                gridLookUpSO.Properties.ValueMember = "SO";
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
                                    gridLookUpSO.Properties.DataSource = null;
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
        private void LoadTranslations()
        {
            lblDate.Text = LocalizationManager.GetString("FilterDate");
            lblEmployee.Text = LocalizationManager.GetString("NameOperator");
            lblPlant.Text = LocalizationManager.GetString("Plant");
            lblMachine.Text = LocalizationManager.GetString("SelectMachine");
            lblSO.Text = LocalizationManager.GetString("SO");
            lblPart.Text = LocalizationManager.GetString("SelectPart");
            lblSize.Text = LocalizationManager.GetString("Size");

            lblInventory.Text = LocalizationManager.GetString("InventoryQty");
            lblActualSizeQty.Text = LocalizationManager.GetString("ActualSizeQty");
            lblPiecesPerPair.Text = LocalizationManager.GetString("PiecesPerPair");
            lblMaterialLayer.Text = LocalizationManager.GetString("MaterialLayer");
            lblTotalPiecesPerPair.Text = LocalizationManager.GetString("TotalPiecesPerPair");

            btnSave.Text = LocalizationManager.GetString("Save");
            btnReset.Text = LocalizationManager.GetString("Reset");

            isLeather.Text = Lang.LeatherMaterial;
            isAutoCutting.Text = LocalizationManager.GetString("IsSkivingknife");
        }
        public static void ApplyNoNegative(DevExpress.XtraEditors.SpinEdit spin)
        {
            spin.Properties.MinValue = 0;
            spin.Properties.AllowNullInput = DevExpress.Utils.DefaultBoolean.False;
            spin.Properties.IsFloatValue = false;
            spin.Properties.Mask.EditMask = "n0";

            spin.EditValueChanging += (s, e) =>
            {
                if (e.NewValue == null) return;

                decimal value;
                if (decimal.TryParse(e.NewValue.ToString(), out value))
                {
                    if (value < 0)
                        e.Cancel = true;
                }
            };
        }
        private void SetLayoutItemVisibility(bool isVisible, params DevExpress.XtraLayout.LayoutControlItem[] items)
        {
            foreach (var item in items)
            {
                if (item != null)
                    item.Visibility = isVisible
                        ? DevExpress.XtraLayout.Utils.LayoutVisibility.Always
                        : DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
            }
        }
    }
    public class SalesOrder
    {
        public string SO { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    public class SizeQtyDto
    {
        public string Size { get; set; }
        public int SizeQty { get; set; }

        public override string ToString()
        {
            return $"{Size} - {LocalizationManager.GetString("LastSizeQty")}: {SizeQty}";
        }
    }
}
