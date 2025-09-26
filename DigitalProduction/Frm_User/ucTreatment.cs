using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace DigitalProduction.Frm_User
{
    public partial class ucTreatment : UserControl
    {
        public ucTreatment()
        {
            InitializeComponent();
        }

        private DataTable JsonArrayToDataTable(JArray array)
        {
            var dt = new DataTable();

            if (array == null || array.Count == 0)
                return dt;

            // Lấy tất cả các cột (key) có trong JSON
            var columnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (JObject obj in array.Children<JObject>())
            {
                foreach (var prop in obj.Properties())
                    columnNames.Add(prop.Name);
            }

            foreach (var col in columnNames)
                dt.Columns.Add(col, typeof(string)); // dùng string

            // Fill rows
            foreach (JObject obj in array.Children<JObject>())
            {
                var row = dt.NewRow();
                foreach (var col in columnNames)
                {
                    JToken token;
                    if (obj.TryGetValue(col, StringComparison.OrdinalIgnoreCase, out token))
                        row[col] = token.Type == JTokenType.Null ? "" : token.ToString();
                    else
                        row[col] = "";
                }
                dt.Rows.Add(row);
            }

            return dt;
        }
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        public async Task<DataTable> FetchDataTableFromUrlAsync(string url, CancellationToken ct = default)
        {
            // Gọi GET
            using (var req = new HttpRequestMessage(HttpMethod.Get, url))
            {
                // req.Headers.Add("Authorization", "Bearer ...");

                using (var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct))
                {
                    resp.EnsureSuccessStatusCode();
                    var content = await resp.Content.ReadAsStringAsync();

                    // Parse JSON: thường server trả mảng JSON (JArray) hoặc object { data: [...] }
                    JToken root = JToken.Parse(content);

                    JArray array = null;

                    if (root.Type == JTokenType.Array)
                        array = (JArray)root;
                    else if (root.Type == JTokenType.Object)
                    {
                        // nếu server trả { data: [...] } hoặc { result: [...] }
                        var obj = (JObject)root;
                        // ưu tiên trường "data", "result", "items", hoặc lấy first array field
                        if (obj.TryGetValue("data", StringComparison.OrdinalIgnoreCase, out JToken t) && t.Type == JTokenType.Array)
                            array = (JArray)t;
                        else
                        {
                            // fallback: tìm property đầu tiên có mảng
                            foreach (var p in obj.Properties())
                            {
                                if (p.Value.Type == JTokenType.Array)
                                {
                                    array = (JArray)p.Value;
                                    break;
                                }
                            }
                        }
                    }

                    if (array == null)
                    {
                        // Nếu không có array, nhưng là object đơn => convert thành 1-row table
                        if (root.Type == JTokenType.Object)
                        {
                            var dt = new DataTable();
                            var jo = (JObject)root;
                            foreach (var prop in jo.Properties())
                                dt.Columns.Add(prop.Name, typeof(string));
                            var row = dt.NewRow();
                            foreach (var prop in jo.Properties())
                                row[prop.Name] = prop.Value.Type == JTokenType.Null ? "" : prop.Value.ToString();
                            dt.Rows.Add(row);
                            return dt;
                        }

                        // Không parse được
                        throw new Exception("Không thể parse JSON response thành danh sách (không có array).");
                    }

                    return JsonArrayToDataTable(array);
                }
            }
        }
        public async Task LoadAndBindToDevExpressGridAsync(string so)
        {
            string url = $"http://10.30.0.36:3100/getDataTechnologyPlanPC?so={Uri.EscapeDataString(so)}";
            DataTable dt = null;

            try
            {
                dt = await FetchDataTableFromUrlAsync(url);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}");
                return;
            }

            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() =>
                {
                    ApplyData(dt);
                }));
            }
            else
            {
                ApplyData(dt);
            }
        }
        private void ApplyData(DataTable dt)
        {
            gridControlTreatment.DataSource = dt;
            gridControlTreatment.RefreshDataSource();

            // Dictionary ánh xạ tên cột gốc -> tiêu đề hiển thị
            var captions = new Dictionary<string, string>
            {
                { "SO", LocalizationManager.GetString("SO") },
                { "PART_NO", LocalizationManager.GetString("PartCode") },
                { "MAIN_PRODUCTION_ORDER", LocalizationManager.GetString("MAIN_PRODUCTION_ORDER") },
                { "PRODUCTION_ORDER", LocalizationManager.GetString("OrderID") },
                { "SIZE_NO", LocalizationManager.GetString("Size") },
                { "QTY", LocalizationManager.GetString("Quantity") },
                { "OPERATION_TYPE", LocalizationManager.GetString("OPERATION_TYPE") }
            };
            foreach (var kv in captions)
            {
                if (gridView1.Columns[kv.Key] != null)
                    gridView1.Columns[kv.Key].Caption = kv.Value;
            }
            gridView1.BestFitColumns();
        }
        private async void btnLoadTreatment_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            btnLoadTreatment.Enabled = false;
            try
            {
                await LoadAndBindToDevExpressGridAsync(btnLoadTreatment.Text.Trim());
            }
            finally
            {
                btnLoadTreatment.Enabled = true;
            }
        }
    }
}
