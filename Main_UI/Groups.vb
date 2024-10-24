Imports System.IO
Imports System.Runtime
Public Class Groups
    Public homeDir As DirectoryInfo
    Private Sub Groups_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Width = 960
        Me.Height = 560
        GroupDataView.Columns.Clear()
        GroupDataView.Rows.Clear()

        Me.Text = "Click on an algorithm group to jump to the first algorithm in that group."
        GroupDataView.CellBorderStyle = DataGridViewCellBorderStyle.None
        GroupDataView.ColumnHeadersVisible = False
        GroupDataView.RowHeadersVisible = False
        Dim grplines = File.ReadAllLines(homeDir.FullName + "Data/GroupButtonList.txt")

        Dim colsPerRow = 10
        Dim rowsPerCol = 26
        For i = 0 To colsPerRow - 1
            Dim column As New DataGridViewTextBoxColumn()
            column.Name = "Column" & i
            column.HeaderText = "     "
            GroupDataView.Columns.Add(column)
        Next

        Dim rowCount = 0
        For i = 0 To grplines.Count - 1
            Dim row As String() = New String(colsPerRow - 1) {}
            Dim index As Integer = 0
            For j = i To grplines.Count - 1 Step rowsPerCol
                If index >= row.Length Then Exit For
                row(index) = grplines(j)
                index += 1
            Next
            GroupDataView.Rows.Add(row)
            rowCount += 1
            If rowCount >= rowsPerCol Then Exit For
        Next
    End Sub
    Private Sub GroupDataView_CellContentClick(sender As Object, e As DataGridViewCellEventArgs) Handles GroupDataView.CellContentClick
        Me.Hide()
        Main_UI.groupButtonSelection = GroupDataView.Rows(e.RowIndex).Cells(e.ColumnIndex).Value
    End Sub
End Class