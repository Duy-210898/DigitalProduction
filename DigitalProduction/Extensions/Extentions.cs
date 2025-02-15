using System;
using System.Data;
using System.Windows.Forms;
using System.Xml.Linq;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace DigitalProduction.Extensions
{
    public static class Extentions
    {
        private static string cloneControl = String.Empty;
        public static void showEditModeCellGridView(GridControl gridControl, GridView gridView, string control)
        {
            cloneControl = control.Trim();
            // Create the RepositoryItemButtonEdit for the action buttons
            RepositoryItemButtonEdit _commandsEdit = new RepositoryItemButtonEdit { AutoHeight = false, Name = "CommandsEdit", TextEditStyle = TextEditStyles.HideTextEditor };
            _commandsEdit.Buttons.Clear();
            if (control.Equals("ucProgress"))
            {
                _commandsEdit.Buttons.AddRange(new EditorButton[]
               {
                    new EditorButton(ButtonPredefines.Glyph, LocalizationManager.GetString("Delete"), -1, true, true, false, ImageLocation.MiddleLeft, null)
               });
            }
            else
            {
                // Add buttons with localized captions
                _commandsEdit.Buttons.AddRange(new EditorButton[]
                {
                //new EditorButton(ButtonPredefines.Glyph, LocalizationManager.GetString("Add"), -1, true, true, false, ImageLocation.MiddleLeft, null),
                new EditorButton(ButtonPredefines.Glyph, LocalizationManager.GetString("Edit"), -1, true, true, false, ImageLocation.MiddleLeft, null),
                    // new EditorButton(ButtonPredefines.Glyph, LocalizationManager.GetString("Delete"), -1, true, true, false, ImageLocation.MiddleLeft, null)
                });
            }
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
                switch (ee.Button.Index)
                {
                    case 0: // Update button
                        if (control.Equals("ucManagement"))
                        {
                            gridView.CloseEditor();
                            gridView.OptionsEditForm.ShowUpdateCancelPanel = DevExpress.Utils.DefaultBoolean.True;
                            gridView.OptionsEditForm.FormCaptionFormat = LocalizationManager.GetString("Edit");
                            //gridView.Columns["DepartmentName"].OptionsEditForm.Visible = DevExpress.Utils.DefaultBoolean.False;
                            gridView.Columns["EmployeeName"].OptionsEditForm.Caption = LocalizationManager.GetString("EmployeeName");
                            gridView.Columns["EmployeeID"].OptionsEditForm.Caption = LocalizationManager.GetString("EmployeeID");
                            gridView.Columns["Username"].OptionsEditForm.Caption = LocalizationManager.GetString("Username");
                            gridView.Columns["Username"].OptionsColumn.ReadOnly = true;
                            gridView.Columns["IsActive"].OptionsEditForm.Caption = LocalizationManager.GetString("IsActive");
                            setupDepartmentLookup(gridView, gridControl);
                            setupPositionLookup(gridView, gridControl);
                            // Subscribe to the RowUpdated event in your setup or form initialization
                            gridView.RowUpdated += GridView_RowUpdated;
                            gridView.ShowPopupEditForm();
                        }
                        if (control.Equals("ucProgress"))
                        {
                            int distributionID = 0;
                            DialogResult result = MessageBox.Show($"Are you sure you want to delete this distribution?", "Confirm Delete", MessageBoxButtons.YesNo);
                            if (result == DialogResult.Yes)
                            {
                                distributionID = (int)gridView.GetRowCellValue(gridView.FocusedRowHandle, "DistributionID");
                                bool statusDelete = DbHelper.deleteDistribution(distributionID);
                                if (statusDelete) {
                                    ShowMessage.ShowInfo($"Distribution deleted successfully with {distributionID}");
                                    gridControl.BeginInvoke(new MethodInvoker(() => gridView.DeleteRow(gridView.FocusedRowHandle)));
                                }
                                else
                                {
                                    ShowMessage.ShowError($"Error deleting distribution: {distributionID}");
                                }
                            }
                        }
                            break;

                    case 1: // Delete button
                       // var dlg = XtraMessageBox.Show($"Bạn có chắc chắn muốn xóa ?", "Cảnh báo", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (control.Equals("ucManagement"))
                        {
                            string userName = gridView.GetRowCellValue(gridView.FocusedRowHandle, "Username").ToString().Trim();
                            DialogResult result = MessageBox.Show($"Are you sure you want to delete this user {userName}?", "Confirm Delete", MessageBoxButtons.YesNo);
                            if (result == DialogResult.Yes)
                            {
                                // Call the delete function and pass the UserID
                                bool statusDelete = DbHelper.DeleteUser(userName);

                                // Provide feedback to the user based on the deletion result
                                if (statusDelete)
                                {
                                    ShowMessage.ShowInfo($"User deleted successfully with {userName}");
                                    gridControl.BeginInvoke(new MethodInvoker(() => gridView.DeleteRow(gridView.FocusedRowHandle)));
                                }
                                else
                                {
                                    ShowMessage.ShowError($"Error deleting user {userName}");
                                }
                            }
                        }
                        break;
                }
            };

            //int oldEmployeeID = 0;
            //gridView.FocusedRowChanged += (s, e) =>
            //{ 
            //    if (e.FocusedRowHandle >= 0)
            //    {
            //        oldEmployeeID = Convert.ToInt32(gridView.GetRowCellValue(e.FocusedRowHandle, "EmployeeID"));
            //    }
            //};
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

        private static void GridView_RowUpdated(object sender, RowObjectEventArgs e)
        {
            GridView gridView = sender as GridView;  // Get the GridView from sender
            if (gridView == null) return;
            if (cloneControl.Equals("ucManagement"))
            {
                try
                {
                    // Your update logic goes here
                    string username = gridView.GetRowCellValue(e.RowHandle, "Username")?.ToString();
                    int newEmployeeID = Convert.ToInt32(gridView.GetRowCellValue(e.RowHandle, "EmployeeID"));
                    int newDepartmentID = Convert.ToInt32(gridView.GetRowCellValue(e.RowHandle, "DepartmentID"));
                    int newPositionID = Convert.ToInt32(gridView.GetRowCellValue(e.RowHandle, "PositionID"));
                    string newEmployeeName = gridView.GetRowCellValue(e.RowHandle, "EmployeeName").ToString();
                    bool newIsActive = Convert.ToBoolean(gridView.GetRowCellValue(e.RowHandle, "IsActive"));

                    // Call your update function
                    bool statusUpdate = DbHelper.updateUser(username, newEmployeeName, newEmployeeID, newDepartmentID, newPositionID, newIsActive);

                    if (statusUpdate)
                    {
                        ShowMessage.ShowInfo($"Update success for user: {username}");
                    }
                    else
                    {
                        ShowMessage.ShowError($"Failed to update user: {username}");
                    }
                }
                catch (Exception ex)
                {
                    ShowMessage.ShowError($"Error updating row: {ex.Message}");
                }
                finally
                {
                    // Re-subscribe after the update logic is complete
                    gridView.RowUpdated -= GridView_RowUpdated;
                }
            }
        }
        private static void setupDepartmentLookup(GridView gridView, GridControl gridControl)
        {
            // Create LookUpEdit repository item
            RepositoryItemLookUpEdit lookupEdit = new RepositoryItemLookUpEdit();

            DataTable departments = DbHelper.getDepartments();
            // Load department data from database
            lookupEdit.DataSource = departments;
            lookupEdit.DisplayMember = "DepartmentName";
            lookupEdit.ValueMember = "DepartmentName";
            lookupEdit.SearchMode = SearchMode.AutoFilter;

            if (!gridControl.RepositoryItems.Contains(lookupEdit))
                gridControl.RepositoryItems.Add(lookupEdit);

            // Handle to apply LookUpEdit only for the current row
            gridView.CustomRowCellEditForEditing += (s, e) =>
            {
                if (e.Column.FieldName == "DepartmentName")
                {
                    e.RepositoryItem = lookupEdit;
                }
            };
            getIdFromDataTable(gridView, lookupEdit, departments, "DepartmentID", "DepartmentName");
        }

        private static void setupPositionLookup(GridView gridView, GridControl gridControl)
        {
            // Create LookUpEdit repository item
            RepositoryItemLookUpEdit lookupEdit = new RepositoryItemLookUpEdit();
            DataTable positions = DbHelper.getPositions();
            // Load pos data from database
            lookupEdit.DataSource = positions;
            lookupEdit.DisplayMember = "PositionName";
            lookupEdit.ValueMember = "PositionName";
            lookupEdit.SearchMode = SearchMode.AutoFilter;

            if (!gridControl.RepositoryItems.Contains(lookupEdit))
                gridControl.RepositoryItems.Add(lookupEdit);

            // Handle to apply LookUpEdit only for the current row
            gridView.CustomRowCellEditForEditing += (s, e) =>
            {
                if (e.Column.FieldName == "PositionName")
                {
                    e.RepositoryItem = lookupEdit;
                }
            };
            getIdFromDataTable(gridView, lookupEdit, positions, "PositionID", "PositionName");
        }
        private static void getIdFromDataTable(GridView gridView, RepositoryItemLookUpEdit lookupEdit, DataTable dt, string Id, string Name)
        {
            // Handle value change event
            lookupEdit.EditValueChanged += (s, e) =>
            {
                LookUpEdit editor = s as LookUpEdit;
                if (editor != null)
                {
                    string selectedDepartmentName = editor.EditValue as string;
                    if (!string.IsNullOrEmpty(selectedDepartmentName))
                    {
                        // Get DepartmentID from DataTable based on selected DepartmentName
                        DataRow[] rows = dt.Select($"{Name} = '{selectedDepartmentName}'");
                        if (rows.Length > 0)
                        {
                            int departmentID = Convert.ToInt32(rows[0][$"{Id}"]);

                            // Update DepartmentID field in the GridView
                            gridView.SetFocusedRowCellValue($"{Id}", departmentID);
                        }
                    }
                }
            };
        }

        public static string getNameFromDataTable(DataTable dt, int value, string id, string name)
        {
            DataRow[] rows = dt.Select($"[{id}] = '{value}'");

            if (rows.Length > 0)  // Found a match
            {
                return rows[0][name].ToString();
            }
            else  // No match found
            {
                return string.Empty;
            }
        }

        public static void GridView_EditFormPrepared(object sender, EditFormPreparedEventArgs e)
        {
            // Update the "Update" and "Cancel" button captions in the Edit Form
            Control ctrl_Update = MyExtenstions.FindControl(e.Panel, "Update");
            if (ctrl_Update != null)
            {
                 ctrl_Update.Text = LocalizationManager.GetString("Update");
                (ctrl_Update as SimpleButton).ImageOptions.Image = null;
            }

            Control ctrl_Cancel = MyExtenstions.FindControl(e.Panel, "Cancel");
            if (ctrl_Cancel != null)
            {
                ctrl_Cancel.Text = LocalizationManager.GetString("Cancel");
                (ctrl_Cancel as SimpleButton).ImageOptions.Image = null;
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