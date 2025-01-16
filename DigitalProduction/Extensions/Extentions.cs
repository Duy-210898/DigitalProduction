using System;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using static DigitalProduction.frmMain;

namespace DigitalProduction.Extensions
{
    public static class Extentions
    {
        public static void showEditModeCellGridView(GridControl gridControl, GridView gridView, string filedName)
        {
            // Create the RepositoryItemButtonEdit for the action buttons
            RepositoryItemButtonEdit _commandsEdit = new RepositoryItemButtonEdit { AutoHeight = false, Name = "CommandsEdit", TextEditStyle = TextEditStyles.HideTextEditor };
            _commandsEdit.Buttons.Clear();

            // Add buttons with localized captions
            _commandsEdit.Buttons.AddRange(new EditorButton[]
            {
                new EditorButton(ButtonPredefines.Glyph, LocalizationManager.GetString("Add"), -1, true, true, false, ImageLocation.MiddleLeft, null),
                new EditorButton(ButtonPredefines.Glyph, LocalizationManager.GetString("Edit"), -1, true, true, false, ImageLocation.MiddleLeft, null),
                new EditorButton(ButtonPredefines.Glyph, LocalizationManager.GetString("Delete"), -1, true, true, false, ImageLocation.MiddleLeft, null)
            });

            // Add "Action" column to GridView if not already present
            GridColumn _commandsColumn = gridView.Columns["Action"];
            if (_commandsColumn == null)
            {
                _commandsColumn = gridView.Columns.AddField("Action");
                _commandsColumn.UnboundDataType = typeof(object);
                _commandsColumn.Visible = true;
                _commandsColumn.Width = 150;
                _commandsColumn.OptionsEditForm.Visible = DevExpress.Utils.DefaultBoolean.False;
            }

            // Set the RepositoryItemButtonEdit for the "Action" column
            _commandsColumn.ColumnEdit = _commandsEdit;

            // Handle button clicks
            _commandsEdit.ButtonClick += (s, ee) =>
            {
                switch (ee.Button.Caption)
                {
                    case "Add":
                    case "Thêm": // Add button
                        ShowMessage.ShowInfo("You clicked Add");
                        break;

                    case "Edit":
                    case "Sửa": // Update button
                        gridView.CloseEditor();
                        gridView.ShowEditForm();
                        break;

                    case "Delete":
                    case "Xóa": // Delete button
                        var fullname = gridView.GetFocusedDataRow()[filedName]?.ToString();
                        if (string.IsNullOrEmpty(fullname)) return;

                        var dlg = XtraMessageBox.Show($"Bạn có chắc chắn muốn xóa {fullname} ?", "Cảnh báo", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (dlg == DialogResult.Yes)
                            gridControl.BeginInvoke(new MethodInvoker(() => gridView.DeleteRow(gridView.FocusedRowHandle)));
                        break;
                }
            };

            // Update captions based on language change
            LanguageSettings.LanguageChanged += () =>
            {
                LocalizationManager.SetLanguage(LanguageSettings.CurrentLanguage);
                // Ensure the buttons are updated with new captions
                _commandsEdit.Buttons[0].Caption = LocalizationManager.GetString("Add");
                _commandsEdit.Buttons[1].Caption = LocalizationManager.GetString("Edit");
                _commandsEdit.Buttons[2].Caption = LocalizationManager.GetString("Delete");
            };

            // Ensure that the "Action" column is editable in the grid
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
                // Only allow editing when the "Action" column is focused
                ee.Cancel = gridView.FocusedColumn != _commandsColumn;
            };

            // Prevent the editor from showing on double-click, enter key, or F2 key
            gridView.OptionsEditForm.ShowOnDoubleClick = DevExpress.Utils.DefaultBoolean.False;
            gridView.OptionsEditForm.ShowOnEnterKey = DevExpress.Utils.DefaultBoolean.False;
            gridView.OptionsEditForm.ShowOnF2Key = DevExpress.Utils.DefaultBoolean.False;
            gridView.OptionsBehavior.EditingMode = GridEditingMode.EditFormInplace;
        }

        public static void GridView_EditFormPrepared(object sender, EditFormPreparedEventArgs e)
        {
            // Update the "Update" and "Cancel" button captions in the Edit Form
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
        // Utility method to find controls by their text
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
