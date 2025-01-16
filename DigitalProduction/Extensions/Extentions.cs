
using System;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;

namespace DigitalProduction.Extensions
{
    public static class Extentions
    {
        public static void showEditModeCellGridView(GridControl gridControl, GridView gridView, string filedName)
        {
             RepositoryItemButtonEdit _commandsEdit = new RepositoryItemButtonEdit { AutoHeight = false, Name = "CommandsEdit", TextEditStyle = TextEditStyles.HideTextEditor };
            _commandsEdit.Buttons.Clear();
            // Add buttons with localized captions
            _commandsEdit.Buttons.AddRange(new EditorButton[]
            {
            new EditorButton(ButtonPredefines.Glyph, LocalizationManager.GetString("Add"), -1, true, true, false, ImageLocation.MiddleLeft, null),
            new EditorButton(ButtonPredefines.Glyph, LocalizationManager.GetString("Update"), -1, true, true, false, ImageLocation.MiddleLeft, null),
            new EditorButton(ButtonPredefines.Glyph, LocalizationManager.GetString("Delete"), -1, true, true, false, ImageLocation.MiddleLeft, null)
            });

            // Add "Action" column to GridView if not already present
            GridColumn _commandsColumn = gridView.Columns.AddField("Action");
            _commandsColumn.UnboundDataType = typeof(object);
            _commandsColumn.Visible = true;
            _commandsColumn.Width = 150;
            _commandsColumn.OptionsEditForm.Visible = DevExpress.Utils.DefaultBoolean.False;

            gridView.CustomRowCellEdit += (s, ee) =>
            {
                if (ee.RowHandle == gridView.FocusedRowHandle && ee.Column == _commandsColumn)
                    ee.RepositoryItem = _commandsEdit;
            };

            gridView.CustomRowCellEditForEditing += (s, ee) =>
            {
                if (ee.RowHandle == gridView.FocusedRowHandle && ee.Column == _commandsColumn)
                    ee.RepositoryItem = _commandsEdit;
            };

            gridView.ShowingEditor += (s, ee) =>
            {
                ee.Cancel = gridView.FocusedColumn != _commandsColumn;
            };

            gridView.OptionsEditForm.ShowOnDoubleClick = DevExpress.Utils.DefaultBoolean.False;
            gridView.OptionsEditForm.ShowOnEnterKey = DevExpress.Utils.DefaultBoolean.False;
            gridView.OptionsEditForm.ShowOnF2Key = DevExpress.Utils.DefaultBoolean.False;
            gridView.OptionsBehavior.EditingMode = GridEditingMode.EditFormInplace;

            // Button click handler
            _commandsEdit.ButtonClick += (s, ee) =>
            {
                switch (ee.Button.Caption)
                {
                    case "Add":
                    case "Thêm": // Add button
                        ShowMessage.ShowInfo("You clicked Add");
                        break;

                    case "Cập nhật":
                    case "Update": // Update button
                        gridView.CloseEditor();
                        gridView.ShowEditForm();
                        break;

                    case "Xóa":
                    case "Delete": // Delete button
                        var fullname = gridView.GetFocusedDataRow()[filedName]?.ToString();
                        if (string.IsNullOrEmpty(fullname)) return;

                        var dlg = XtraMessageBox.Show($"Bạn có chắc chắn muốn xóa {fullname} ?", "Cảnh báo", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (dlg == DialogResult.Yes)
                            gridControl.BeginInvoke(new MethodInvoker(() => gridView.DeleteRow(gridView.FocusedRowHandle)));
                        break;
                }
            };
        }
        public static void GridView_EditFormPrepared(object sender, EditFormPreparedEventArgs e)
        {
            Control ctrl = MyExtenstions.FindControl(e.Panel, "Update");
            if (ctrl != null)
            {
                ctrl.Text = "Cập nhật";
                (ctrl as SimpleButton).ImageOptions.Image = null;
            }

            ctrl = MyExtenstions.FindControl(e.Panel, "Cancel");
            if (ctrl != null)
            {
                (ctrl as SimpleButton).ImageOptions.Image = null;
                ctrl.Text = "Đóng";
            }
        }
    }

    public static class MyExtenstions
    {
        public static Control FindControl(this Control root, string text)
        {
            if (root == null) throw new ArgumentNullException("root");
            foreach (Control child in root.Controls)
            {
                if (child.Text == text) return child;
                Control found = FindControl(child, text);
                if (found != null) return found;
            }
            return null;
        }
    }
}
