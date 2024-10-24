Imports System.IO
Imports System.Runtime
Public Class Groups
    Public homeDir As DirectoryInfo
    Private Sub Groups_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Width = 960
        Me.Height = 540
        GroupDataView.Columns.Clear()
        GroupDataView.Rows.Clear()

        Me.Text = "Click on an algorithm group to jump to the first algorithm in that group."
        GroupDataView.CellBorderStyle = DataGridViewCellBorderStyle.None
        GroupDataView.ColumnHeadersVisible = False
        GroupDataView.RowHeadersVisible = False
        Dim grplines = File.ReadAllLines(homeDir.FullName + "Data/GroupButtonList.txt")

        Dim colsPerRow = 10
        For i = 0 To colsPerRow - 1
            Dim column As New DataGridViewTextBoxColumn()
            column.Name = "Column" & i
            column.HeaderText = "     "
            GroupDataView.Columns.Add(column)
        Next

        For i = 0 To grplines.Count - 1 Step colsPerRow
            Dim row As String() = New String(colsPerRow - 1) {}
            For j = i To Math.Min(i + colsPerRow, grplines.Count) - 1
                row(j - i) = grplines(j)
            Next
            GroupDataView.Rows.Add(row)
        Next
    End Sub
    Private Sub GroupDataView_CellContentClick(sender As Object, e As DataGridViewCellEventArgs) Handles GroupDataView.CellContentClick
        Me.Hide()
        Main_UI.groupButtonSelection = GroupDataView.Rows(e.RowIndex).Cells(e.ColumnIndex).Value
    End Sub
End Class