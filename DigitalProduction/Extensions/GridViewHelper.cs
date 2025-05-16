using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using DevExpress.Utils;
using DevExpress.Utils.Menu;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.ColorPick.Picker;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Menu;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Views.Grid.ViewInfo;

namespace GridviewHelp
{
    class GridViewHelper
    {
        public static string TEMP_PATH = Path.GetTempPath() + Assembly.GetExecutingAssembly().GetName().Name + "";
        public static void SaveAndRestoreLayout(GridControl gridControl, string FormName)
        {
            gridControl.ForceInitialize();
            var path_layout_default = TEMP_PATH + "" + FormName + "" + gridControl.Name + "" + "default_layout.xml";
            var folder = TEMP_PATH + "" + FormName + "" + gridControl.Name;
            if (!Directory.Exists(folder))
            {
                DirectoryInfo di = Directory.CreateDirectory(folder);
            }
            if (!File.Exists(path_layout_default))
            {
                gridControl.MainView.SaveLayoutToXml(path_layout_default, OptionsLayoutBase.FullLayout);
            }
            var path = TEMP_PATH + "" + FormName + "" + gridControl.Name + "custom_layout.xml";
            if (File.Exists(path))
            {
                gridControl.ForceInitialize();
                gridControl.MainView.RestoreLayoutFromXml(path, OptionsLayoutBase.FullLayout);
            }
        }
        public static void GridView_CustomDrawRowIndicator(object sender, RowIndicatorCustomDrawEventArgs e, GridControl gridControl, GridView gridView)
        {
            if (!gridView.IsGroupRow(e.RowHandle)) //Nếu không phải là Group
            {
                if (e.Info.IsRowIndicator) //Nếu là dòng Indicator
                {
                    if (e.RowHandle < 0)
                    {
                        e.Info.ImageIndex = 0;
                        e.Info.DisplayText = string.Empty;
                    }
                    else
                    {
                        e.Info.ImageIndex = -1; //Không hiển thị hình
                        e.Info.DisplayText = (e.RowHandle + 1).ToString(); //Số thứ tự tăng dần
                    }
                    SizeF _Size = e.Graphics.MeasureString(e.Info.DisplayText, e.Appearance.Font); //Lấy kích thước của vùng hiển thị Text
                    Int32 _Width = Convert.ToInt32(_Size.Width) + 20;
                    gridControl.BeginInvoke(new MethodInvoker(delegate { cal(_Width, gridView); })); //Tăng kích thước nếu Text vượt quá
                }
            }
            else
            {
                e.Info.ImageIndex = -1;
                e.Info.DisplayText = string.Format("[{0}]", (e.RowHandle * -1)); //Nhân -1 để đánh lại số thứ tự tăng dần
                SizeF _Size = e.Graphics.MeasureString(e.Info.DisplayText, e.Appearance.Font);
                Int32 _Width = Convert.ToInt32(_Size.Width) + 20;
                gridControl.BeginInvoke(new MethodInvoker(delegate { cal(_Width, gridView); }));
            }
        }

        public static bool cal(Int32 _Width, GridView _View)
        {
            _View.IndicatorWidth = _View.IndicatorWidth < _Width ? _Width : _View.IndicatorWidth;
            return true;
        }

        public static Image createImage(Color color)
        {
            Bitmap flag = new Bitmap(16, 16);
            Graphics flagGraphics = Graphics.FromImage(flag);
            Pen blackPen = new Pen(Color.Black, 2);
            Rectangle rect = new Rectangle(0, 0, 16, 16);
            flagGraphics.DrawRectangle(blackPen, rect);
            flagGraphics.FillRectangle(new SolidBrush(color), 1, 1, 14, 14);
            return flag;
        }
        public static void AddFontAndColortoPopupMenuShowing(object sender, PopupMenuShowingEventArgs e, GridControl gridcontrol, string FormName)
        {
            //nếu sử dụng thì tích hợp save layout.          
            if (e.MenuType == GridMenuType.Column)
            {
                GridViewColumnMenu menu = e.Menu as GridViewColumnMenu;
                //menu.Items.Clear();
                if (menu.Column != null)
                {
                    // font chữ

                    DXMenuCheckItem font = new DXMenuCheckItem("Fonts", true, DigitalProduction.Properties.Resources.font_icon, new EventHandler(OnFixedClick));
                    font.Tag = new MenuInfo(menu.Column, FixedStyle.None);
                    menu.Items.Add(font);

                    // Màu nền
                    DXSubMenuItem sItem = new DXSubMenuItem("Back Ground");
                    sItem.ImageOptions.Image = DigitalProduction.Properties.Resources.backGround_icon;
                    Color mauhong = ColorTranslator.FromHtml("#FFC2BE");
                    Color mauxanh = ColorTranslator.FromHtml("#A8D5FF");
                    Color xanhduong = ColorTranslator.FromHtml("#C1F49C");
                    Color mauvang = ColorTranslator.FromHtml("#FEF7A5");
                    Color mautim = ColorTranslator.FromHtml("#E0CFE9");
                    Color xanhlam = ColorTranslator.FromHtml("#8DE9DF");
                    Color mautrang = ColorTranslator.FromHtml("#FFFFFF");
                    Color mauden = ColorTranslator.FromHtml("#000000");

                    sItem.Items.Add(CreateCheckItem("Color White", menu.Column, FixedStyle.None, mautrang));
                    sItem.Items.Add(CreateCheckItem("Color Black", menu.Column, FixedStyle.None, mauden));
                    sItem.Items.Add(CreateCheckItem("Color Pink", menu.Column, FixedStyle.None, mauhong));
                    sItem.Items.Add(CreateCheckItem("Color Blue", menu.Column, FixedStyle.None, mauxanh));
                    sItem.Items.Add(CreateCheckItem("Color Green", menu.Column, FixedStyle.None, xanhduong));
                    sItem.Items.Add(CreateCheckItem("Color Yellow", menu.Column, FixedStyle.None, mauvang));
                    sItem.Items.Add(CreateCheckItem("Color Purple", menu.Column, FixedStyle.None, mautim));
                    sItem.Items.Add(CreateCheckItem("Color Cyan", menu.Column, FixedStyle.None, xanhlam));

                    sItem.Items.Add(CreateCheckItem("Color Customize...", menu.Column, FixedStyle.None, Color.Transparent));
                    menu.Items.Add(sItem);

                    // màu chữ
                    var mauchu = new DXSubMenuItem("Fore Color");
                    mauchu.ImageOptions.Image = DigitalProduction.Properties.Resources.foreColor_icon;
                    Color Red = Color.Red;
                    Color pink = ColorTranslator.FromHtml("#E91E63");
                    Color purple = ColorTranslator.FromHtml("#9C27B0");
                    Color DeepPurple = ColorTranslator.FromHtml("#673AB7");
                    Color Indigo = ColorTranslator.FromHtml("#E0CFE9");
                    Color blue = ColorTranslator.FromHtml("#3F51B5");
                    Color cyan = ColorTranslator.FromHtml("#00BCD4");
                    Color Teal = ColorTranslator.FromHtml("#009688");
                    Color green = ColorTranslator.FromHtml("#4CAF50");
                    Color Lime = ColorTranslator.FromHtml("#CDDC39");
                    Color Amber = ColorTranslator.FromHtml("#FFC107");
                    Color Orange = ColorTranslator.FromHtml("#FF9800");
                    Color depOrange = ColorTranslator.FromHtml("#FF5722");
                    Color brown = ColorTranslator.FromHtml("#795548");
                    Color grey = ColorTranslator.FromHtml("#9E9E9E");
                    Color BlueGrey = ColorTranslator.FromHtml("#607D8B");
                    Color Black = ColorTranslator.FromHtml("#000000");
                    Color White = ColorTranslator.FromHtml("#FFFFFF");

                    mauchu.Items.Add(CreateCheckItem("Black", menu.Column, FixedStyle.None, Black));
                    mauchu.Items.Add(CreateCheckItem("White", menu.Column, FixedStyle.None, White));
                    mauchu.Items.Add(CreateCheckItem("Pink", menu.Column, FixedStyle.None, pink));
                    mauchu.Items.Add(CreateCheckItem("Purple", menu.Column, FixedStyle.None, purple));
                    mauchu.Items.Add(CreateCheckItem("Deep Purple", menu.Column, FixedStyle.None, DeepPurple));
                    mauchu.Items.Add(CreateCheckItem("Indigo", menu.Column, FixedStyle.None, Indigo));
                    // mauchu.Items.Add(CreateCheckItem("Red", menu.Column, FixedStyle.None, Red));
                    mauchu.Items.Add(CreateCheckItem("Blue", menu.Column, FixedStyle.None, blue));
                    mauchu.Items.Add(CreateCheckItem("Cyan", menu.Column, FixedStyle.None, cyan));
                    mauchu.Items.Add(CreateCheckItem("Teal", menu.Column, FixedStyle.None, Teal));
                    mauchu.Items.Add(CreateCheckItem("Green", menu.Column, FixedStyle.None, green));
                    mauchu.Items.Add(CreateCheckItem("Lime", menu.Column, FixedStyle.None, Lime));
                    mauchu.Items.Add(CreateCheckItem("Amber", menu.Column, FixedStyle.None, Amber));
                    mauchu.Items.Add(CreateCheckItem("Orange", menu.Column, FixedStyle.None, Orange));
                    mauchu.Items.Add(CreateCheckItem("Deep Orange", menu.Column, FixedStyle.None, depOrange));
                    mauchu.Items.Add(CreateCheckItem("Brown", menu.Column, FixedStyle.None, brown));
                    mauchu.Items.Add(CreateCheckItem("Grey", menu.Column, FixedStyle.None, grey));
                    mauchu.Items.Add(CreateCheckItem("Blue Grey", menu.Column, FixedStyle.None, BlueGrey));

                    mauchu.Items.Add(CreateCheckItem("ForeColor Customize...", menu.Column, FixedStyle.None, Color.Transparent));
                    menu.Items.Add(mauchu);

                    //DXMenuCheckItem save_layout = new DXMenuCheckItem("Save Layout", true);
                    //save_layout.ImageOptions.Image = DigitalProduction.Properties.Resources.save_icon;
                    //save_layout.CheckedChanged += (ss, ee) => {
                    //    var path = TEMP_PATH + "" + FormName + "" + gridcontrol.Name + "custom_layout.xml";
                    //    gridcontrol.MainView.SaveLayoutToXml(path, OptionsLayoutBase.FullLayout);
                    //    //FrmMain.Instance.ShowMessageInfo("Đã lưu cấu hình layout.");
                    //};
                    //DXMenuCheckItem reset_layout = new DXMenuCheckItem("Reset Layout", true);
                    //reset_layout.CheckedChanged += (ss, ee) =>
                    //{
                    //    var path = TEMP_PATH + "" + FormName + "" + gridcontrol.Name + "default_layout.xml";
                    //    var path_custom = TEMP_PATH + "" + FormName + "" + gridcontrol.Name + "custom_layout.xml";
                    //    if (File.Exists(path))
                    //    {
                    //        gridcontrol.MainView.RestoreLayoutFromXml(path, OptionsLayoutBase.FullLayout);
                    //        //FrmMain.Instance.ShowMessageInfo("Reset layout thành công.");
                    //    }

                    //    if (File.Exists(path_custom))
                    //    {
                    //        File.Delete(path_custom);
                    //    }

                    //};
                    //reset_layout.ImageOptions.Image = DigitalProduction.Properties.Resources.reset_icon;
                    //menu.Items.Add(save_layout);
                    //menu.Items.Add(reset_layout);
                }
            }
        }
        public static void CustomizeGroupText(GridView gridView)
        {
            gridView.CustomDrawGroupRow += (s, e) =>
            {
                GridView view = s as GridView;
                GridGroupRowInfo info = e.Info as GridGroupRowInfo;
                int groupRowHandle = e.RowHandle;

                string field = info.Column?.FieldName;
                string value = info.GroupValueText;

                if (field == "SO" || field == "PartName" || field == "Size")
                {
                    bool hasPending = HasPendingStatus(view, groupRowHandle);
                    string displayStatus = hasPending ? "Pending" : "Completed";

                    // Label for each field
                    string label;
                    switch (field)
                    {
                        case "SO":
                            label = "SO";
                            break;
                        case "Size":
                            label = "Size";
                            break;
                        default:
                            label = "Part";
                            break;
                    }

                    // Only set text for the current group field
                    info.GroupText = $"{label}: {value} | Status: {displayStatus}";
                    e.Appearance.ForeColor = hasPending ? Color.Gray : Color.Green;
                    e.Painter.DrawObject(info);
                    e.Handled = true;

                }
            };
        }

        private static bool HasPendingStatus(GridView view, int groupRowHandle)
        {
            int childCount = view.GetChildRowCount(groupRowHandle);
            for (int i = 0; i < childCount; i++)
            {
                int childHandle = view.GetChildRowHandle(groupRowHandle, i);

                if (view.IsGroupRow(childHandle))
                {
                    // Recursive call for subgroup
                    if (HasPendingStatus(view, childHandle))
                        return true;
                }
                else
                {
                    string status = view.GetRowCellDisplayText(childHandle, view.Columns["Status"]);
                    if (status == "Pending")
                        return true;
                }
            }
            return false;
        }

        //Create a menu item 
        public static DXMenuCheckItem CreateCheckItem(string caption, GridColumn column, FixedStyle style, Color color)
        {
            Image image = createImage(color);
            DXMenuCheckItem item = new DXMenuCheckItem(caption, column.Fixed == style, image, new EventHandler(OnFixedClick));
            item.Tag = new MenuInfo(column, style);
            return item;
        }

        //Menu item click handler 
        public static void OnFixedClick(object sender, EventArgs e)
        {
            DXMenuItem item = sender as DXMenuItem;
            MenuInfo info = item.Tag as MenuInfo;
            if (info == null) return;

            if (item.Caption.Substring(0, 3) == "Col")
            {
                if (item.Caption == "Color Customize...")
                {
                    ColorPickEdit colorPickerEdit = new ColorPickEdit();
                    FrmColorPicker frm = new FrmColorPicker(colorPickerEdit.Properties);
                    frm.StartPosition = FormStartPosition.CenterScreen;
                    frm.TopMost = true;
                    if (frm.ShowDialog(colorPickerEdit.FindForm()) == DialogResult.OK)
                    {
                        info.Column.AppearanceCell.BackColor = frm.SelectedColor;
                    }

                }
                else
                {
                    info.Column.AppearanceCell.BackColor = ((Bitmap)item.Image).GetPixel(5, 5);
                }
            }
            else if (item.Caption.Substring(0, 4) == "Font")
            {
                FontDialog fontDialog = new FontDialog();
                fontDialog.ShowDialog();
                info.Column.AppearanceCell.Font = fontDialog.Font;
            }
            else
            {
                if (item.Caption == "ForeColor Customize...")
                {
                    ColorPickEdit colorPickerEdit = new ColorPickEdit();
                    FrmColorPicker frm = new FrmColorPicker(colorPickerEdit.Properties);
                    frm.StartPosition = FormStartPosition.CenterScreen;
                    frm.TopMost = true;
                    if (frm.ShowDialog(colorPickerEdit.FindForm()) == System.Windows.Forms.DialogResult.OK)
                    {
                        info.Column.AppearanceCell.ForeColor = frm.SelectedColor;
                    }

                }
                else
                {
                    info.Column.AppearanceCell.ForeColor = ((Bitmap)item.Image).GetPixel(5, 5);
                }
            }
        }
        class MenuInfo
        {
            public MenuInfo(GridColumn column, FixedStyle style)
            {
                this.Column = column;
                this.Style = style;
            }
            public FixedStyle Style;
            public GridColumn Column;
        }
    }
}
